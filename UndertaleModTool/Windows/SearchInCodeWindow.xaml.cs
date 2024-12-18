﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualBasic.Devices;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for SearchInCodeWindow.xaml
    /// </summary>
    public partial class SearchInCodeWindow : Window
    {
        private ContextMenuDark linkContextMenu;

        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        bool isCaseSensitive;
        bool isRegexSearch;
        string text;

        bool usingGMLCache;

        int progressValue;

        ConcurrentDictionary<string, List<(int, string)>> resultsDict;
        ConcurrentBag<string> failedList;
        IOrderedEnumerable<string> failedSorted;                                     //failedList.OrderBy()
        IOrderedEnumerable<KeyValuePair<string, List<(int, string)>>> resultsSorted; //resultsDict.OrderBy()
        int resultCount = 0;

        Regex keywordRegex;

        ThreadLocal<GlobalDecompileContext> decompileContext;

        LoaderDialog loaderDialog;

        private UndertaleCodeEditor.CodeEditorTab editorTab = UndertaleCodeEditor.CodeEditorTab.Decompiled;

        readonly record struct CodeLine(string Code, int Line);

        public SearchInCodeWindow()
        {
            InitializeComponent();

            linkContextMenu = FindResource("linkContextMenu") as ContextMenuDark;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await Search();
        }

        async Task Search()
        {
            // TODO: Allow this be cancelled, probably make loader inside this window itself.

            if (mainWindow.Data == null)
            {
                this.ShowError("No data.win loaded.");
                return;
            }

            if (mainWindow.Data.IsYYC())
            {
                this.ShowError("Can't search code in YYC game, there's no code to search.");
                return;
            }

            text = SearchTextBox.Text;

            if (String.IsNullOrEmpty(text))
                return;

            isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
            isRegexSearch = RegexSearchCheckBox.IsChecked ?? false;

            if (isRegexSearch)
            {
                keywordRegex = new(text, isCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            mainWindow.IsEnabled = false;
            this.IsEnabled = false;

            loaderDialog = new("Searching...", null);
            loaderDialog.PreventClose = true;
            loaderDialog.Show();

            decompileContext = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(mainWindow.Data, false));

            // TODO: This creates another loader dialog. Fix this.
            usingGMLCache = await mainWindow.GenerateGMLCache(decompileContext);
            
            resultsDict = new();
            failedList = new();
            resultCount = 0;
            progressValue = 0;

            // If we run script before opening any code
            if (!usingGMLCache && mainWindow.Data.KnownSubFunctions is null)
            {
                loaderDialog.Maximum = null;
                loaderDialog.Update("Building the cache of all sub-functions...");

                await Task.Run(() => Decompiler.BuildSubFunctionCache(mainWindow.Data));
            }

            loaderDialog.SavedStatusText = "Code entries";
            loaderDialog.Update(null, "Code entries", 0, mainWindow.Data.Code.Count);

            if (usingGMLCache)
            {
                await Task.Run(() => Parallel.ForEach(mainWindow.Data.GMLCache, SearchInGMLCache));
            }
            else
            {
                await Task.Run(() => Parallel.ForEach(mainWindow.Data.Code, SearchInUndertaleCode));
            }

            await Task.Run(SortResults);

            loaderDialog.Maximum = null;
            loaderDialog.Update("Generating result list...");

            //await Task.Run(GenerateResults);
            await Dispatcher.InvokeAsync(GenerateResults);

            //mainWindow.PlayInformationSound();

            loaderDialog.PreventClose = false;
            loaderDialog.Close();
            loaderDialog = null;

            mainWindow.IsEnabled = true;
            this.IsEnabled = true;
        }

        void SearchInGMLCache(KeyValuePair<string, string> code)
        {
            SearchInCodeText(code.Key, code.Value);

            Interlocked.Increment(ref resultCount);
            Dispatcher.InvokeAsync(() => loaderDialog.ReportProgress(resultCount));
        }

        void SearchInUndertaleCode(UndertaleCode code)
        {
            try
            {
                if (code is not null && code.ParentEntry is null)
                SearchInCodeText(code.Name.Content, Decompiler.Decompile(code, decompileContext.Value));
            }
            // TODO: Look at specific exceptions
            catch (Exception e)
            {
                failedList.Add(code.Name.Content);
            }

            Interlocked.Increment(ref resultCount);
            Dispatcher.InvokeAsync(() => loaderDialog.ReportProgress(resultCount));
        }

        void SearchInCodeText(string codeName, string codeText)
        {
            try
            {
                var lineNumber = 0;
                StringReader codeTextReader = new(codeText);
                bool nameWritten = false;
                string lineText;
                while ((lineText = codeTextReader.ReadLine()) is not null)
                {
                    lineNumber += 1;
                    if (lineText == string.Empty)
                        continue;

                    if (((isRegexSearch && keywordRegex.Match(lineText).Success) || ((!isRegexSearch && isCaseSensitive) ? lineText.Contains(text) : lineText.Contains(text, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        if (nameWritten == false)
                        {
                            resultsDict[codeName] = new List<(int, string)>();
                            nameWritten = true;
                        }
                        resultsDict[codeName].Add((lineNumber, lineText));
                        Interlocked.Increment(ref resultCount);
                    }
                }
            }
            // TODO look at specific exceptions
            catch (Exception e)
            {
                failedList.Add(codeName);
            }
        }

        void SortResults()
        {
            string[] codeNames = mainWindow.Data.Code.Select(x => x.Name.Content).ToArray();

            if (mainWindow.Data.GMLCacheFailed?.Count > 0)
                failedSorted = failedList.Concat(mainWindow.Data.GMLCacheFailed).OrderBy(c => Array.IndexOf(codeNames, c));
            else if (failedList.Count > 0)
                failedSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));

            resultsSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
        }

        public void GenerateResults()
        {
            //(Not used because it has bad performance)
            /*MemoryStream docStream = new();
            ProcessResults(ref docStream);
            docStream.Seek(0, SeekOrigin.Begin);

            Dispatcher.Invoke(() =>
            {
                OutTextBox.Document = XamlReader.Load(docStream) as FlowDocument;
            });
            
            docStream.Dispose();*/

            FlowDocument doc = new();

            if (failedList is not null)
            {
                int failedCount = failedList.Count;
                if (failedCount > 0)
                {
                    string errorStr;
                    Paragraph errPara = new() { Foreground = Brushes.OrangeRed };
                    InlineCollection errLines = errPara.Inlines;

                    if (failedCount == 1)
                    {
                        errorStr = "There is 1 code entry that encountered an error while searching:";
                        errLines.Add(new Run(errorStr) { FontWeight = FontWeights.Bold });
                        errLines.Add(new LineBreak());
                        errLines.Add(new Run(failedList.First()));
                    }
                    else
                    {
                        errorStr = $"There are {failedCount} code entries that encountered an error while searching:";
                        errLines.Add(new Run(errorStr) { FontWeight = FontWeights.Bold });
                        errLines.Add(new LineBreak());

                        int i = 1;
                        foreach (string entry in failedList)
                        {
                            if (i < failedCount)
                            {
                                errLines.Add(new Run(entry + ','));
                                errLines.Add(new LineBreak());
                            }
                            else
                                errLines.Add(new Run(entry));

                            i++;
                        }
                    }
                    errLines.Add(new LineBreak());
                    errLines.Add(new LineBreak());

                    doc.Blocks.Add(errPara);
                }
            }

            int resCount = resultsDict.Count;
            Paragraph headerPara = new(new Run($"{resultCount} results in {resCount} code entries for \"{text}\".")) { FontWeight = FontWeights.Bold };
            headerPara.Inlines.Add(new LineBreak());
            doc.Blocks.Add(headerPara);

            foreach (KeyValuePair<string, List<(int lineNum, string codeLine)>> result in resultsDict)
            {
                int lineCount = result.Value.Count;
                Paragraph resPara = new();

                Underline resHeader = new();
                resHeader.Inlines.Add(new Run("Results in "));
                resHeader.Inlines.Add(new Hyperlink(new Run(result.Key)));
                resHeader.Inlines.Add(new Run(":"));
                resHeader.Inlines.Add(new LineBreak());
                resPara.Inlines.Add(resHeader);

                int i = 1;
                foreach (var (lineNum, codeLine) in result.Value)
                {
                    Hyperlink lineLink = new(new Run($"Line {lineNum}")
                    {
                        Tag = new CodeLine(result.Key, lineNum)
                    });

                    resPara.Inlines.Add(lineLink);
                    resPara.Inlines.Add(new Run($": {codeLine}"));

                    if (i < lineCount)
                        resPara.Inlines.Add(new LineBreak());

                    i++;
                }
                resPara.Inlines.Add(new LineBreak());

                doc.Blocks.Add(resPara);
            }

            ResultsRichTextBox.Document = doc;
        }

        private void ResultsRichTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mainWindow is null)
                return;
            if (e.OriginalSource is not Run linkRun || linkRun.Parent is not Hyperlink
                || String.IsNullOrEmpty(linkRun.Text))
                return;

            if (linkRun.Text.StartsWith("Line "))
            {
                var (codeName, lineNum) = (CodeLine)linkRun.Tag;
                if (String.IsNullOrEmpty(codeName))
                {
                    e.Handled = true;
                    return;
                }

                if (e.ChangedButton == System.Windows.Input.MouseButton.Right && linkContextMenu is not null)
                {
                    linkContextMenu.DataContext = (lineNum, codeName);
                    linkContextMenu.IsOpen = true;
                }
                else
                    mainWindow.OpenCodeEntry(codeName, lineNum, editorTab, e.ChangedButton == System.Windows.Input.MouseButton.Middle);
            }
            else
            {
                string codeName = linkRun.Text;
                if (e.ChangedButton == System.Windows.Input.MouseButton.Right && linkContextMenu is not null)
                {
                    linkContextMenu.DataContext = (1, codeName);
                    linkContextMenu.IsOpen = true;
                }
                else
                    mainWindow.OpenCodeEntry(codeName, editorTab, e.ChangedButton == System.Windows.Input.MouseButton.Middle);
            }

            e.Handled = true;
        }

        private void OpenInNewTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ValueTuple<int, string> codeNamePair
                || String.IsNullOrEmpty(codeNamePair.Item2))
                return;

            mainWindow.OpenCodeEntry(codeNamePair.Item2, codeNamePair.Item1, editorTab, true);
        }

        private void copyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string outText = ResultsRichTextBox.Selection.Text;

            if (outText.Length > 0)
                Clipboard.SetText(outText, TextDataFormat.Text);
        }

        private void copyAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string outText = new TextRange(ResultsRichTextBox.Document.ContentStart, ResultsRichTextBox.Document.ContentEnd).Text;

            if (outText.Length > 0)
                Clipboard.SetText(outText, TextDataFormat.Text);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = (loaderDialog is not null);
        }

        private void OnCopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            copyMenuItem_Click(null, null);
        }
    }
}
