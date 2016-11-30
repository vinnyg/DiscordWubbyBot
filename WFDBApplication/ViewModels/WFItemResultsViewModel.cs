using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Persistence;

namespace WFDBApplication.ViewModels
{
    public class WFItemResultsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private WarframeItem _selectedItem;
        public WarframeItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        public WFItemResultsViewModel()
        {

        }

        /*WFItemResultsViewModel()
        {
            LoadData();
        }

        void LoadData()
        {
            using (var unit = new UnitOfWork(new WarframeDataContext()))
            {
                SelectedItem = unit.WarframeItems.GetAll().ToObservableCollection();
            }
        }*/
    }
}
