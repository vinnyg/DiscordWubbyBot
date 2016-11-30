using System;
using System.Collections.Generic;
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

namespace WFDBApplication
{
    /// <summary>
    /// Interaction logic for WFDBHome.xaml
    /// </summary>
    public partial class WFDBHome : Page
    {
        //private List<WarframeItem> _warframeItems;
        public WFDBHome()
        {
            InitializeComponent();
            //LoadData();
        }

        /*private void LoadData()
        {
            //Create a unit of work and load all the entities into the list
            using (var unit = new UnitOfWork(new WarframeDataContext()))
            {
                _warframeItems = unit.WarframeItems.GetAll().ToList();
            }
            //Then we specify the source for data
            ItemListBox.ItemsSource = _warframeItems;
        }*/

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // View Expense Report
            //Pass in the data from the selected item to the page constructor.
            WFDBResultsPage wfdbResultsPage = new WFDBResultsPage(ItemListBox.SelectedItem);
            NavigationService.Navigate(wfdbResultsPage);
        }
    }
}
