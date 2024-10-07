using System;
using System.IO;
using System.Reflection;
using System.Windows;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject);
        }

        static void HandleException(Exception e)
        {
            string text = e.ToString();
            try
            {
                text = "This is an error log file! Consider creating an issue about this error on GitHub: https://github.com/UnderminersTeam/UndertaleModTool/issues/new" +
                    $"\n\nVersion: {Assembly.GetExecutingAssembly().GetName().Version} - {GitVersion.GetGitVersion()}" +
                    $"\nOS: {Environment.OSVersion}" +
                    $"\nGame info: {(Application.Current.MainWindow as MainWindow).Data.GeneralInfo}" +
                    "\n\n" + text;
            }
            catch { }

            File.WriteAllText(Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "Error.txt"), text);

            MessageBox.Show("An unexpected error has ocurred, UndertaleModTool will now close." +
                "\nFor more information about the error, check the file \"Error.txt\" in the application's folder." +
                "\nConsider creating an issue about this error on GitHub:" +
                "\nhttps://github.com/UnderminersTeam/UndertaleModTool/issues/new",
                "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
