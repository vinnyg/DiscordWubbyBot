using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WarframeDatabaseNet;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Persistence;
//using WFDBApplication.Converters;

namespace WFDBApplication
{
    /// <summary>
    /// Interaction logic for WFDBResultsPage.xaml
    /// </summary>
    public partial class WFDBResultsPage : Page
    {
        public WarframeItem SelectedItem { get; set; }
        private string _selectedItemURI { get; set; }
        public WFDBResultsPage()
        {
            InitializeComponent();

            //LoadData();
        }

        public WFDBResultsPage(object data) : this()
        {
            //Pass the object to the results page.
            //We are binding the data here.

            //SelectedItem = (WarframeItem)data;
            DataContext = data;
            Loaded += WFDBResultsPage_Loaded;
        }

        void WFDBResultsPage_Loaded(object sender, RoutedEventArgs e)
        {
            //DataContext = SelectedItem;
        }

        private void Butt_Finish_Click(object sender, RoutedEventArgs e)
        {
            using (var unit = new UnitOfWork(new WarframeDataContext()))
            {
                SelectedItem = unit.WarframeItems.GetItemByURI(_selectedItemURI);
                SelectedItem.ItemURI = ItemURITextBox.Text;
                SelectedItem.Name = ItemNameTextBox.Text;
                SelectedItem.Ignore = (!IgnoreChkBox.IsChecked.HasValue) ? 0 : (bool)(IgnoreChkBox.IsChecked) ? 1 : 0;
                unit.Complete();
            }
            NavigationService.GoBack();
        }
    }
}
