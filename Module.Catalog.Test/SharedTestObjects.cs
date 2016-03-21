using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Module.Catalog.Test
{
    public class TestAsyncResult : IAsyncResult
    {
        public object AsyncState
        {
            get { return null; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return null; }
        }

        public bool CompletedSynchronously
        {
            get { return true; }
        }

        public bool IsCompleted
        {
            get { return true; }
        }
    }

    public class PropertyChangeTracker
    {
        private List<string> notifications = new List<string>();

        public PropertyChangeTracker(INotifyPropertyChanged changer)
        {
            changer.PropertyChanged += (o, e) => { notifications.Add(e.PropertyName); };
        }

        public string[] ChangedProperties
        {
            get { return notifications.ToArray(); }
        }

        public bool WaitForChange(string propertyName, int maxWaitSeconds)
        {
            var startTime = DateTime.Now;
            while (!notifications.Contains(propertyName))
            {
                var diff = DateTime.Now - startTime;
                if (diff.TotalSeconds > maxWaitSeconds)
                    return false;
            }
            return true;
        }

        public void Reset()
        {
            notifications.Clear();
        }
    }

}
