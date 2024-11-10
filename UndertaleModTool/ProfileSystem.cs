using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    // Handles a majority of profile-system functionality

    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            if (code is null)
                return "";
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", decompile that instead.";

            try
            {
                //return code.GetGML(Data, context, settings); // Use profile mode
                return code.GetDecompiledGML(Data, context, settings);
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
            }
        }

        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            return GetDecompiledText(Data.Code.ByName(codeName), context, settings);
        }

        public string GetDisassemblyText(UndertaleCode code)
        {
            if (code is null)
                return "";
            if (code.ParentEntry is not null)
                return $"; This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", disassemble that instead.";

            try
            {
                return code.Disassemble(Data.Variables, Data.CodeLocals?.For(code));
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"; // Please don't
            }
        }

        public string GetDisassemblyText(string codeName)
        {
            return GetDisassemblyText(Data.Code.ByName(codeName));
        }

        public void CreateUMTLastEdited(string filename)
        {
            try
            {
                File.WriteAllText(Path.Combine(ProfilesFolder, "LastEdited.txt"), Data.ToolInfo.CurrentMD5 + "\n" + filename);
            }
            catch (Exception exc)
            {
                this.ShowError("CreateUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }

        public void DestroyUMTLastEdited()
        {
            try
            {
                string path = Path.Combine(ProfilesFolder, "LastEdited.txt");
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exc)
            {
                this.ShowError("DestroyUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }

        public void CloseProfile()
        {
            string profile = Path.Join(ProfilesFolder, Data.ToolInfo.CurrentMD5);
            string profileTemp = Path.Join(profile, "Temp");

            // Delete temp to not waste space.
            if (Directory.Exists(profileTemp))
                Directory.Delete(profileTemp, true);
        }

        public async Task LoadProfile(string filename)
        {
            FileMessageEvent?.Invoke("Calculating MD5 hash...");

            try
            {
                await Task.Run(() =>
                {
                    Data.ToolInfo.CurrentMD5 = GenerateMD5(filename);
                });

                string profile = Path.Join(ProfilesFolder, Data.ToolInfo.CurrentMD5);
                string profileMain = Path.Join(profile, "Main");
                string profileTemp = Path.Join(profile, "Temp");

                // If temp still exists, then the program probably crashed.
                if (Directory.Exists(profileTemp))
                {
                    this.ShowMessage($"Error: There are unsaved profile mode files for the data file you are currently loading, move or delete these files before loading this profile again. Save them now before losing them.\n\nLocation: {profileTemp}");

                    throw new Exception("Temp folder already exists");
                }

                // Create temp so it can be modified when you access code.
                CopyDirectoryHard(profileMain, profileTemp);

                if (!SettingsWindow.ProfileModeEnabled)
                    return;

                // Create LastEdited.txt (for crash detection)
                CreateUMTLastEdited(filename);

                // Show message
                if (!SettingsWindow.ProfileMessageShown)
                {
                    this.ShowMessage(@"The profile for your game loaded successfully!

UndertaleModTool now uses the ""Profile"" system by default for code.
Using the profile system, many new features are available to you!
For example, the code is fully editable (you can even add comments)
and it will be saved exactly as you wrote it. In addition, if the
program crashes or your computer loses power during editing, your
code edits will be recovered automatically the next time you start
the program.

The profile system can be toggled on or off at any time by going
to the ""File"" tab at the top and then opening the ""Settings""
(the ""Enable profile mode"" option toggles it on or off).
You may wish to disable it for purposes such as collaborative
modding projects, or when performing technical operations.
For more in depth information, please read ""About_Profile_Mode.txt"".

It should be noted that this system is somewhat experimental, so
should you encounter any problems, please let us know or leave
an issue on GitHub.");
                    SettingsWindow.ProfileMessageShown = true;
                }
            }
            catch (Exception exc)
            {
                this.ShowError("LoadProfile error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }
        public async Task SaveProfile(string filename)
        {
            FileMessageEvent?.Invoke("Calculating MD5 hash...");

            try
            {
                string previousMD5 = Data.ToolInfo.CurrentMD5;

                await Task.Run(() =>
                {
                    Data.ToolInfo.CurrentMD5 = GenerateMD5(filename);
                });

                bool copyProfile = (previousMD5 != Data.ToolInfo.CurrentMD5);

                // The profile saving happens even if profile mode is disabled so data isn't lost.
                // If there's no profile or it's empty, nothing new is created.

                string oldProfile = Path.Join(ProfilesFolder, previousMD5);
                string oldProfileMain = Path.Join(oldProfile, "Main");
                string oldProfileTemp = Path.Join(oldProfile, "Temp");
                string newProfile = Path.Join(ProfilesFolder, Data.ToolInfo.CurrentMD5);
                string newProfileMain = Path.Join(newProfile, "Main");
                string newProfileTemp = Path.Join(newProfile, "Temp");

                if (copyProfile)
                {
                    // Copy from where temp files currently are (old temp) to where main files should be (new main)
                    CopyDirectoryHard(oldProfileTemp, newProfileMain);

                    if (!SettingsWindow.DeleteOldProfileOnSave)
                    {
                        // In old profile, delete temp files, since changes weren't saved there
                        if (Directory.Exists(oldProfileTemp))
                            Directory.Delete(oldProfileTemp, true);
                    }
                    else
                    {
                        // Delete old profile entirely.
                        if (Directory.Exists(oldProfile))
                            Directory.Delete(oldProfile, true);
                    }

                    // Copy main to temp so it can be used currently
                    CopyDirectoryHard(newProfileMain, newProfileTemp);
                }
                else
                {
                    // Just save temp stuff into main
                    CopyDirectoryHard(oldProfileTemp, oldProfileMain);
                }
            }
            catch (Exception exc)
            {
                this.ShowError("SaveProfile error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
            }
        }

        // Copies source directory to destination directory.
        // If destination already exists, delete it first.
        // If source doesn't exist or is empty, stop.
        static void CopyDirectoryHard(string source, string destination)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);

            var sourceInfo = new DirectoryInfo(source);

            if (!sourceInfo.Exists)
                return;
            if (!sourceInfo.EnumerateFileSystemInfos().Any())
                return;

            Directory.CreateDirectory(destination);

            foreach (FileInfo file in sourceInfo.EnumerateFiles())
            {
                file.CopyTo(Path.Join(destination, file.Name));
            }
            foreach (DirectoryInfo dir in sourceInfo.EnumerateDirectories())
            {
                CopyDirectoryHard(dir.FullName, Path.Join(destination, dir.Name));
            }
        }
    }
}
