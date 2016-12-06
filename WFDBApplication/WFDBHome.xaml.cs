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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // View Expense Report
            //Pass in the data from the selected item to the page constructor.
            WFDBResultsPage wfdbResultsPage = new WFDBResultsPage(ItemListBox.SelectedItem);
            NavigationService.Navigate(wfdbResultsPage);
        }
    }
}
