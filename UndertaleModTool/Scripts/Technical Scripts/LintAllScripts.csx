using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UndertaleModLib;
using UndertaleModLib.Scripting;

ScriptOptions cSharpScriptOptions = ScriptOptions.Default
.AddReferences(
    typeof(UndertaleObject).GetTypeInfo().Assembly,
    typeof(MainWindow).GetTypeInfo().Assembly,
    typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly,
    typeof(ImageMagick.MagickImage).GetTypeInfo().Assembly,
    typeof(Underanalyzer.Decompiler.DecompileContext).Assembly)
.AddImports(
    "System",
    "System.Collections.Generic",
    "System.IO",
    "System.Text.RegularExpressions",
    "UndertaleModLib",
    "UndertaleModLib.Compiler",
    "UndertaleModLib.Decompiler",
    "UndertaleModLib.Models",
    "UndertaleModLib.Scripting",
    "UndertaleModTool")
.WithEmitDebugInformation(true)
.WithFileEncoding(Encoding.UTF8);

string umtBaseDir = Path.Join(ExePath, "Scripts");
string[] scripts = GetFilesRecursively(umtBaseDir);

int scriptsCount = scripts.Length;
int errorCount = 0;
int warningCount = 0;

List<(string, StringBuilder)> scriptOutputs = new List<(string, StringBuilder)>();
foreach (string script in scripts)
{
    scriptOutputs.Add((script, new StringBuilder($"-- Script {Path.GetRelativePath(umtBaseDir, script)} --\n")));
}

SetProgressBar(null, "Files", 0, scriptsCount);
StartProgressBarUpdater();

// Lint each script and create the output.
await Task.Run(() => Parallel.ForEach(scriptOutputs, t =>
{
    (string file, StringBuilder output) = t;

    Script script = CSharpScript.Create(File.ReadAllText(file), cSharpScriptOptions.WithFilePath(file), typeof(IScriptInterface));
    var diagnostics = script.Compile();

    bool hasErrors = false;
    bool hasWarnings = false;

    if (diagnostics.Length > 0)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                output.Append($"{diagnostic}\n");
                hasErrors = true;
            }
        }

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error)
            {
                output.Append($"{diagnostic}\n");
                hasWarnings = true;
            }
        }
    }
    else
    {
        output.Append("No problems.\n");
    }

    output.Append('\n');

    if (hasErrors)
        errorCount++;
    if (hasWarnings)
        warningCount++;

    IncrementProgressParallel();
}));

await StopProgressBarUpdater();

// Write output either in results file
string outputLogLocation = Path.Combine(ExePath, "LintResults.txt");

StringBuilder finalOutput = new StringBuilder();
foreach ((string file, StringBuilder output) in scriptOutputs)
{
    finalOutput.Append(output);
}

File.WriteAllText(outputLogLocation, finalOutput.ToString());

HideProgressBar();

if (ScriptQuestion($"{errorCount} of {scriptsCount} scripts have errors.\n" +
    $"{warningCount} of {scriptsCount} scripts have warnings.\n\n" +
    $"Open {outputLogLocation} for more information?"))
{
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(outputLogLocation) { UseShellExecute = true });
}

// Searches for all csx files recursively and returns them.
string[] GetFilesRecursively(string directoryPath)
{
    List<string> files = new List<string>();
    DirectoryInfo directory = new DirectoryInfo(directoryPath);

    // Call this recursively for all directories
    foreach (DirectoryInfo subDir in directory.GetDirectories())
        files.AddRange(GetFilesRecursively(subDir.FullName));

    // Add all csx files
    foreach (FileInfo file in directory.GetFiles())
    {
        if (file.Extension != ".csx")
            continue;
        files.Add(file.FullName);
    }

    return files.ToArray();
}