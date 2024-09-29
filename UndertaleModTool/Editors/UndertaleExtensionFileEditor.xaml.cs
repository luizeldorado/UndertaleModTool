using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleExtensionFileEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionFileEditor : DataUserControl
    {
        public UndertaleExtensionFileEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            var itemList = (sender as DataGrid).ItemsSource as IList<UndertaleExtensionFunction>;
            int lastItem = itemList.Count;

            UndertaleExtensionFunction obj = new UndertaleExtensionFunction()
            {
                Name = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}"),
                ExtName = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}_ext"),
                RetType = UndertaleExtensionVarType.Double,
                Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
                Kind = 11, // ???
                ID = (Application.Current.MainWindow as MainWindow).Data.ExtensionFindLastId()
            };

            e.NewItem = obj;
        }
    }
}
