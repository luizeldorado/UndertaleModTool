﻿using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Underanalyzer.Decompiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModLib.Tests
{
    public abstract class GameScriptTestBase : GameTestBase, IScriptInterface
    {
        protected GameScriptTestBase(string path, string md5) : base(path, md5)
        {
        }

        public UndertaleData Data => data;
        public string FilePath => path;
        public object Highlighted => throw new NotImplementedException();
        public object Selected => throw new NotImplementedException();
        public bool CanSave => throw new NotImplementedException();
        public string ScriptPath => throw new NotImplementedException();
        public bool ScriptExecutionSuccess => throw new NotImplementedException();
        public string ScriptErrorMessage => throw new NotImplementedException();
        public string ExePath => throw new NotImplementedException();
        public string ScriptErrorType => throw new NotImplementedException();

        public bool IsAppClosed => throw new NotImplementedException();

        protected async Task<object> RunScript(string path)
        {
            string scriptpath = Path.Combine("../../../../UndertaleModTool/Scripts/Builtin Scripts/", path);
            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);

                var script = CSharpScript.Create<object>(File.ReadAllText(scriptpath), ScriptOptions.Default
                    .WithImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "System", "System.IO", "System.Collections.Generic")
                    .WithReferences(
                        typeof(UndertaleObject).GetTypeInfo().Assembly,
                        typeof(Underanalyzer.Decompiler.DecompileContext).GetTypeInfo().Assembly
                        ),
                    typeof(IScriptInterface), loader);

                var result = await script.RunAsync(this);
                return result;
            }
        }

        public void ChangeSelection(object newSelection, bool inNewTab = false)
        {
        }

        public void EnsureDataLoaded()
        {
        }
        public async Task<bool> MakeNewDataFile()
        {
            await Task.Delay(1); //dummy await
            return true;
        }
        public void ReapplyProfileCode()
        {
        }
        public void InitializeScriptDialog()
        {
        }
        public bool RunUMTScript(string path)
        {
            Console.WriteLine(path);
            return true;
        }

        public void ScriptMessage(string message)
        {
            Console.WriteLine(message);
        }

        public bool ScriptQuestion(string message)
        {
            Console.WriteLine(message);
            return true;
        }
        public void ScriptOpenURL(string url)
        {
            Console.WriteLine("Open: " + url);
        }
        public void NukeProfileGML(string codeName)
        {
            Console.WriteLine("NukeProfileGML(): " + codeName);
        }
        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            Console.WriteLine("UpdateProgressBar(): " + progressValue + " / " + maxValue + ", Message: " + message + ", Status: " + status);
        }
        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            Console.WriteLine("SetProgressBar(): " + progressValue + " / " + maxValue + ", Message: " + message + ", Status: " + status);
        }
        public void SetProgressBar()
        {
            Console.WriteLine("SetProgressBar()");
        }
        public void UpdateProgressValue(double progressValue)
        {
            Console.WriteLine($"UpdateProgressValue(): {progressValue}");
        }
        public void UpdateProgressStatus(string status)
        {
            Console.WriteLine($"UpdateProgressStatus(): {status}");
        }

        public void AddProgress(int amount)
        {
            Console.WriteLine($"AddProgress(): {amount}");
        }
        public void IncrementProgress()
        {
            Console.WriteLine("IncrementProgress()");
        }
        public void AddProgressParallel(int amount)
        {
            Console.WriteLine($"AddProgressParallel(): {amount}");
        }
        public void IncrementProgressParallel()
        {
            Console.WriteLine("IncrementProgressParallel()");
        }
        public int GetProgress()
        {
            Console.WriteLine("GetProgress()");
            return -1;
        }
        public void SetProgress(int value)
        {
            Console.WriteLine($"SetProgress(): {value}");
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            Console.Write(label + " ");
            // Probably a bad idea
            //string ret = Console.ReadLine();

            //return ret;
            return "";
        }
        public string SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline, bool showDialog = true)
        {
            return ScriptInputDialog(titleText, labelText, defaultInputBoxText, "Cancel", "Submit", isMultiline, false);
        }
        public void SimpleTextOutput(string titleText, string labelText, string message, bool isMultiline)
        {
            Console.WriteLine($"SimpleTextOutput(): \"{titleText}\", \"{labelText}\", *defaultInputBoxText* (length - {message.Length}), {isMultiline}");
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string>? failedList = null)
        {
            Console.WriteLine($"ClickableSearchOutput(): \"{title}\", \"{query}\", {resultsCount}, *resultsDict* (length - {resultsDict.Count()}), {showInDecompiledView.ToString().ToLower()}"
                              + failedList is not null ? $", *failedList* (length - {failedList?.Count()})" : string.Empty);
            await Task.Delay(1); //dummy await
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string>? failedList = null)
        {
            Console.WriteLine($"ClickableSearchOutput(): \"{title}\", \"{query}\", {resultsCount}, *resultsDict* (length - {resultsDict.Count}), {showInDecompiledView.ToString().ToLower()}"
                              + failedList is not null ? $", *failedList* (length - {failedList?.Count()})" : string.Empty);
            await Task.Delay(1); //dummy await
        }

        public void SetUMTConsoleText(string message)
        {
            Console.Write("SetUMTConsoleText(): " + message);
        }
        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
        {
            Console.Write("ReplaceTextInGML(): " + codeName + ", " + keyword + ", " + replacement + ", " + caseSensitive.ToString() + ", " + isRegex.ToString() + ", " + context?.ToString() + ", " + settings?.ToString());
        }
        public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
        {
            Console.Write("ReplaceTextInGML(): " + code.ToString() + ", " + keyword + ", " + replacement + ", " + caseSensitive.ToString() + ", " + isRegex.ToString() + ", " + context?.ToString() + ", " + settings?.ToString());
        }
        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool checkDecompiler = false)
        {
            Console.Write("ImportGMLString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false)
        {
            Console.Write("ImportASMString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportGMLFile(string fileName, bool doParse = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            Console.Write($"ImportGMLFile(): \"{fileName}\", {doParse}, {checkDecompiler}, {throwOnError}");
        }
        public void ImportASMFile(string fileName, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            Console.Write($"ImportASMFile(): \"{fileName}\", {doParse}, {nukeProfile}, {checkDecompiler}, {throwOnError}");
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            Console.Write("SetFinishedMessage(): " + isFinishedMessageEnabled.ToString());
        }

        public void HideProgressBar()
        {
            Console.WriteLine("Hiding Progress Bar.");
        }

        public void EnableUI()
        {
            Console.WriteLine("Enabling UI.");
        }
        public void SyncBinding(string resourceType, bool enable)
        {
            Console.WriteLine($"SyncBinding(): \"{resourceType}\", {enable}");
        }
        public void DisableAllSyncBindings()
        {
            Console.WriteLine($"Disabling all enabled synced bindings.");
        }
        public void StartProgressBarUpdater()
        {
            Console.WriteLine("Starting progress bar updater...");
        }
        public async Task StopProgressBarUpdater()
        {
            Console.WriteLine("Stopping progress bar updater...");
            await Task.Delay(1); //dummy await
        }

        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            throw new NotImplementedException();
        }

        public string PromptChooseDirectory()
        {
            throw new NotImplementedException();
        }

        public string GetDecompiledText(string codeName, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
        {
            string output = "GetDecompiledText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
        {
            string output = "GetDecompiledText(): " + code?.ToString();
            Console.Write(output);
            return output;
        }
        public string GetDisassemblyText(string codeName)
        {
            string output = "GetDisassemblyText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public string GetDisassemblyText(UndertaleCode code)
        {
            string output = "GetDisassemblyText(): " + code?.ToString();
            Console.Write(output);
            return output;
        }
        public bool AreFilesIdentical(string file1, string file2)
        {
            string output = "AreFilesIdentical(): " + file1 + ", " + file2;
            Console.Write(output);
            return true;
        }
        public string PromptLoadFile(string defaultExt, string filter)
        {
            throw new NotImplementedException();
        }
    }

    public class UndertaleScriptTest : GameScriptTestBase
    {
        public UndertaleScriptTest() : base(GamePaths.UNDERTALE_PATH, GamePaths.UNDERTALE_MD5)
        {
        }

        [Theory]
        [InlineData("EnableDebug.csx")]
        [InlineData("DebugToggler.csx")]
        [InlineData("GoToRoom.csx")]
        [InlineData("ShowRoomName.csx")]
        [InlineData("BorderEnabler.csx")]
        [InlineData("testing.csx", Skip = "Script does not exist")]
        [InlineData("RoomOfDetermination.csx")]
        [InlineData("TTFFonts.csx")]
        [InlineData("MixMod.csx", Skip = "Script not working")]
        public async Task RunScriptTest(string scriptName)
        {
            await RunScript(scriptName);
        }
    }

    public class UndertaleSwitchScriptTest : GameScriptTestBase
    {
        public UndertaleSwitchScriptTest() : base(GamePaths.UNDERTALE_SWITCH_PATH, GamePaths.UNDERTALE_SWITCH_MD5)
        {
        }

        [Theory]
        [InlineData("EnableDebug.csx")]
        [InlineData("DebugToggler.csx")]
        [InlineData("GoToRoom.csx")]
        [InlineData("ShowRoomName.csx")]
        public async Task RunScriptTest(string scriptName)
        {
            await RunScript(scriptName);
        }
    }

    public class DeltaruneScriptTest : GameScriptTestBase
    {
        public DeltaruneScriptTest() : base(GamePaths.DELTARUNE_PATH, GamePaths.DELTARUNE_MD5)
        {
        }

        [Theory]
        [InlineData("EnableDebug.csx")]
        [InlineData("DebugToggler.csx")]
        [InlineData("GoToRoom.csx")]
        [InlineData("ShowRoomName.csx")]
        [InlineData("DeltaHATE.csx")]
        [InlineData("DeltaMILK.csx")]
        [InlineData("TheWholeWorldRevolving.csx")]
        [InlineData("DebugMsg.csx")]
        [InlineData("HeCanBeEverywhere.csx")]
        public async Task RunScriptTest(string scriptName)
        {
            await RunScript(scriptName);
        }
    }
}