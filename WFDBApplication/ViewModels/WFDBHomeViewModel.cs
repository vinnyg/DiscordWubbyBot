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
using WFDBApplication.Extensions;

namespace WFDBApplication.ViewModels
{
    public class WFDBHomeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //ObservableCollection implements INotifyCollectionChanged and INotifyPropertyChanged
        //Useful when you want to know when the collection has changed. Event is triggered which identifies what entries have been added/removed.
        private ObservableCollection<WarframeItem> _warframeItem;
        public ObservableCollection<WarframeItem> WarframeItem
        {
            get
            {
                return _warframeItem;
            }
            set
            {
                _warframeItem = value;
                RaisePropertyChanged("WarframeItem");
            }
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

        public WFDBHomeViewModel()
        {
            LoadData();
        }

        void LoadData()
        {
            using (var unit = new UnitOfWork(new WarframeDataContext()))
            {
                WarframeItem = unit.WarframeItems.GetAll().ToObservableCollection();
            }
        }
    }
}
