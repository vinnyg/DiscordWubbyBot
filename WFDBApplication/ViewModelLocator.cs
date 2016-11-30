using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WFDBApplication.ViewModels;

namespace WFDBApplication
{
    public class ViewModelLocator
    {
        private static WFDBHomeViewModel _wfdbHomeViewModel = new WFDBHomeViewModel();
        private static WFItemResultsViewModel _wfItemResultsViewModel = new WFItemResultsViewModel();

        public static WFDBHomeViewModel WFDBHomeViewModel
        {
            get
            {
                return _wfdbHomeViewModel;
            }
        }

        public static WFItemResultsViewModel WFItemResultsViewModel
        {
            get
            {
                return _wfItemResultsViewModel;
            }
        }
    }
}
