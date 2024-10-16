using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool RunUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            RunScript(path);
            if (!ScriptExecutionSuccess)
                ScriptError("An error of type \"" + ScriptErrorType + "\" occurred. The error is:\n\n" + ScriptErrorMessage, ScriptErrorType);
            return ScriptExecutionSuccess;
        }
        public void InitializeScriptDialog()
        {
            if (scriptDialog == null)
            {
                scriptDialog = new LoaderDialog("Script in progress...", "Please wait...");
                scriptDialog.Owner = this;
                scriptDialog.PreventClose = true;
            }
        }
    }
}
