using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Common
{
    public class CatalogOrder
    {
        public string CustomerID { get; set; }
        public string ShippingID { get; set; }
        public ObservableCollection<Person> SelectedPeople { get; set; }
    }
}
