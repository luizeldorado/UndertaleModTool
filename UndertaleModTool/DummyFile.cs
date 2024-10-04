using System.ComponentModel;
using System.Windows;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    // Test code here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool DummyBool()
        {
            return true;
        }

        public void DummyVoid()
        {
        }
        public string DummyString()
        {
            return "";
        }
    }
}
