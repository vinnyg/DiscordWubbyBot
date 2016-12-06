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
        private ObservableCollection<WarframeItem> _warframeItems;
        public ObservableCollection<WarframeItem> WarframeItems
        {
            get
            {
                return _warframeItems;
            }
            set
            {
                _warframeItems = value;
                RaisePropertyChanged("WarframeItem");
            }
        }

        private WarframeItem _selectedWFItem;
        public WarframeItem SelectedWFItem
        {
            get
            {
                return _selectedWFItem;
            }
            set
            {
                _selectedWFItem = value;
                RaisePropertyChanged("SelectedWFItem");
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
                WarframeItems = unit.WarframeItems.GetAll().ToObservableCollection();
            }
        }

        public string ButtonContent
        {
            get
            {
                return "View";
            }
        }
    }
}
