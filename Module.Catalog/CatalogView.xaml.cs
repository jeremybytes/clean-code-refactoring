using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Module.Catalog
{
    public partial class CatalogView : UserControl
    {
        public CatalogView(CatalogViewModel viewModel)
        {
            InitializeComponent();
            viewModel.Initialize();
            ViewModel = viewModel;
        }

        public CatalogViewModel ViewModel
        {
            get { return (CatalogViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }

        private void RefreshCatalogButton_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshCatalog();
        }

        private void PersonListBox_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox.SelectedItem != null)
                ViewModel.AddToSelection(listbox.SelectedItem);
        }

        private void SelectedPersonListBox_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox.SelectedItem != null)
                ViewModel.RemoveFromSelection(listbox.SelectedItem);
        }

        private void ClearSelectionButton_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearSelection();
        }

    }
}
