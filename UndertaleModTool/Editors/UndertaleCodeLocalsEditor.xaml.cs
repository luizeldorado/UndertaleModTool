using System.Collections.Generic;
using System.Windows.Controls;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleCodeLocalsEditor.xaml
    /// </summary>
    public partial class UndertaleCodeLocalsEditor : DataUserControl
    {
        public UndertaleCodeLocalsEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleCodeLocals.LocalVar obj = new UndertaleCodeLocals.LocalVar();
            obj.Index = (uint)((sender as DataGrid).ItemsSource as IList<UndertaleCodeLocals.LocalVar>).Count;
            e.NewItem = obj;
        }
    }
}
