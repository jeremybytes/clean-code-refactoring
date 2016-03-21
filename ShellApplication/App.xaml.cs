using Common;
using Microsoft.Practices.Unity;
using Services.MyService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace ShellApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IUnityContainer container;

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            ConfigureContainer();
            Application.Current.MainWindow = container.Resolve<MainWindow>();
            Application.Current.MainWindow.Show();
        }

        void App_DispatcherUnhandledException(object sender, 
            DispatcherUnhandledExceptionEventArgs e)
        {
            // This is where we catch any unhandled exceptions.
            // Log them to the system, then provide a generic message to the user.
            try
            {
                // Log.AddException(e.Exception.Message);
                e.Handled = true;
                MessageBox.Show("Something bad happened. Please contact the Help Desk for more information.");
                Application.Current.Shutdown();
            }
            catch
            {
                // If we get an exception in our unhandled exception handler, there's
                // not much we can do.
            }
        }

        private void ConfigureContainer()
        {
            container = new UnityContainer();

            // Instantiate and register the Person Service
            var personService = new PersonServiceClient();
            container.RegisterInstance<IPersonService>(personService);

            // Instantiate and register our (fake) model
            var order = new CatalogOrder()
            {
                SelectedPeople = new ObservableCollection<Person>()
            };
            container.RegisterInstance<CatalogOrder>("CurrentOrder", order);
        }

        
    }
}
