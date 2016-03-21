using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public partial class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime StartDate { get; set; }
        public int Rating { get; set; }

        public int StartDecade
        {
            get { return StartDate.Year / 10 * 10; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}
