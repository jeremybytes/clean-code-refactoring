using Common;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Services.MyService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Threading;

namespace Module.Catalog.Test
{
    [TestClass]
    public class CatalogViewModelTest
    {
        #region Test Initialization

        Person _70sPerson = new Person()
        {
            FirstName = "Mary",
            LastName = "Thomas",
            Rating = 9,
            StartDate = new DateTime(1971, 07, 23)
        };
        Person _00sPerson = new Person()
        {
            FirstName = "John",
            LastName = "Smith",
            Rating = 7,
            StartDate = new DateTime(2000, 10, 01)
        };

        private CatalogViewModel GetStandardViewModel()
        {
            var container = GetPopulatedContainer();
            var viewModel = new CatalogViewModel(container);
            return viewModel;
        }

        private CatalogViewModel GetCustomViewModel(IPersonService service, CatalogOrder currentOrder)
        {
            var container = GetPopulatedContainer(service, currentOrder);
            var viewModel = new CatalogViewModel(container);
            return viewModel;
        }

        private IUnityContainer GetPopulatedContainer()
        {
            IPersonService serviceMock = GetFakePersonService();
            CatalogOrder currentOrder = GetFakeCatalogOrder();
            IUnityContainer container = GetPopulatedContainer(serviceMock, currentOrder);
            return container;
        }

        private IUnityContainer GetPopulatedContainer(IPersonService service, CatalogOrder currentOrder)
        {
            IUnityContainer container = new UnityContainer();
            if (service != null)
                container.RegisterInstance<IPersonService>(service);
            if (currentOrder != null)
                container.RegisterInstance<CatalogOrder>("CurrentOrder", currentOrder);
            return container;
        }

        private Mock<IPersonService> GetMockPersonService()
        {
            var people = new List<Person>()
                {
                    _70sPerson,
                    _00sPerson,
                };

            IAsyncResult ar = new TestAsyncResult();

            var serviceMock = new Mock<IPersonService>();
            serviceMock.Setup(rs => rs.BeginGetPeople(It.IsAny<AsyncCallback>(), null))
                .Returns(ar);

            serviceMock.Setup(rs => rs.EndGetPeople(It.IsAny<IAsyncResult>()))
                .Returns(people);

            return serviceMock;
        }

        private IPersonService GetFakePersonService()
        {
            var serviceFake = GetMockPersonService();
            return serviceFake.Object;
        }

        private IPersonService GetFakeExceptionGeneratingPersonService()
        {
            IAsyncResult ar = new TestAsyncResult();

            var serviceMock = new Mock<IPersonService>();
            serviceMock.Setup(rs => rs.BeginGetPeople(It.IsAny<AsyncCallback>(), null))
                .Returns(ar);

            serviceMock.Setup(rs => rs.EndGetPeople(It.IsAny<IAsyncResult>()))
                .Throws(new NotImplementedException("Service Error"));

            return serviceMock.Object;
        }

        private CatalogOrder GetFakeCatalogOrder()
        {
            var currentOrder = new CatalogOrder()
            {
                SelectedPeople = new ObservableCollection<Person>(),
            };
            return currentOrder;
        }

        #endregion

        #region Model Initialization

        [TestMethod]
        public void CatalogViewModel_OnInitialization_ModelIsPopulated()
        {
            // Arrange
            var viewModel = GetStandardViewModel();

            // Act
            viewModel.Initialize();

            // Assert
            Assert.IsNotNull(viewModel.Model);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingFieldException))]
        public void CatalogViewModel_OnInitializationAndCurrentOrderMissing_ThrowsException()
        {
            // Arrange
            var serviceMock = GetFakePersonService();
            var viewModel = GetCustomViewModel(serviceMock, null);

            // Act
            viewModel.Initialize();

            // Assert
            Assert.Fail("No Exception thrown when Current Order is missing");
        }

        #endregion

        #region Catalog Population

        [TestMethod]
        public void CatalogViewModel_OnInitialization_CatalogIsPopulated()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var tracker = new PropertyChangeTracker(viewModel);

            // Act
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Assert
            Assert.IsNotNull(viewModel.Catalog);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingFieldException))]
        public void CatalogViewModel_OnInitializationAndPersonServiceMissing_ThrowsException()
        {
            // Arrange
            CatalogOrder currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(null, currentOrder);

            // Act
            viewModel.Initialize();

            // Assert
            Assert.Fail("No Exception thrown when IPersonService is missing");
        }

        #endregion

        #region Service Exception

        [TestMethod]
        public void CatalogViewModel_OnInitializationWithServiceException_ThrowsExceptionOnCurrentThread()
        {
            // Arrange
            var serviceFake = GetFakeExceptionGeneratingPersonService();
            var currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(serviceFake, currentOrder);
            var frame = new DispatcherFrame();

            // Timer ensures that the DispatcherFrame will not get stuck in a Contine=true state
            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (s, e) => { frame.Continue = false; timer.Stop(); };

            // Act / Assert
            try
            {
                viewModel.Initialize();
                timer.Start();
                Dispatcher.PushFrame(frame);
                Assert.Fail("Exception not thrown");
            }
            catch (TargetInvocationException ex)
            {
                Assert.AreEqual(typeof(NotImplementedException), ex.InnerException.GetType());
                Assert.AreEqual("Service Error", ex.InnerException.Message);
            }
        }

        [TestMethod]
        public void CatalogViewModel_OnInitializationWithNoServiceException_DoesNotThrowException()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var frame = new DispatcherFrame();

            // Timer ensures that the DispatcherFrame will not get stuck in a Contine=true state
            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            timer.Tick += (s, e) => { frame.Continue = false; timer.Stop(); };

            // Act / Assert
            try
            {
                viewModel.Initialize();
                timer.Start();
                Dispatcher.PushFrame(frame);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception not expected", ex.Message);
            }
        }

        #endregion

        #region Catalog Caching

        [TestMethod]
        public void CatalogService_OnRefreshAndCacheExpired_ServiceIsCalledTwice()
        {
            // Arrange
            var serviceMock = GetMockPersonService();
            var currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(serviceMock.Object, currentOrder);
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.LastUpdateTime = DateTime.Now.AddHours(-1);
            tracker.Reset();
            viewModel.RefreshCatalog();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Assert
            serviceMock.Verify(s => s.EndGetPeople(It.IsAny<IAsyncResult>()),
                Times.Exactly(2));
        }

        [TestMethod]
        public void CatalogService_OnRefreshAndCacheNotExpired_ServiceIsCalledOnce()
        {
            // Arrange
            var serviceMock = GetMockPersonService();
            var currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(serviceMock.Object, currentOrder);
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            tracker.Reset();
            viewModel.RefreshCatalog();
            var result = tracker.WaitForChange("LastUpdateTime", 1);

            // Assert
            serviceMock.Verify(s => s.EndGetPeople(It.IsAny<IAsyncResult>()),
                Times.Once());
        }

        #endregion

        #region Filters

        [TestMethod]
        public void Catalog_FilterIncludes70s_70sRecordIsIncluded()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.Include70s = true;

            // Assert
            Assert.IsTrue(viewModel.Catalog.Contains(_70sPerson));
        }

        [TestMethod]
        public void Catalog_FilterDoesNotInclude70s_70sRecordIsNotIncluded()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.Include70s = false;

            // Assert
            Assert.IsFalse(viewModel.Catalog.Contains(_70sPerson));
        }

        [TestMethod]
        public void Catalog_FilterIncludes00s_00sRecordIsIncluded()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.Include00s = true;

            // Assert
            Assert.IsTrue(viewModel.Catalog.Contains(_00sPerson));
        }

        [TestMethod]
        public void Catalog_FilterDoesNotInclude00s_00sRecordIsNotIncluded()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.Include00s = false;

            // Assert
            Assert.IsFalse(viewModel.Catalog.Contains(_00sPerson));
        }

        #endregion

        #region Filter Reset

        [TestMethod]
        public void Filters_OnRefreshAndCacheExpired_AreResetToDefaults()
        {
            // Arrange
            var serviceMock = GetMockPersonService();
            var currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(serviceMock.Object, currentOrder);
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Include70s = false;
            viewModel.Include80s = false;
            viewModel.Include90s = false;
            viewModel.Include00s = false;
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);

            // Act
            viewModel.LastUpdateTime = DateTime.Now.AddHours(-1);
            tracker.Reset();
            viewModel.RefreshCatalog();
            tracker.WaitForChange("LastUpdateTime", 1);
            serviceMock.Verify(s => s.EndGetPeople(It.IsAny<IAsyncResult>()), Times.Exactly(2),
                "Service was not called twice");

            // Assert
            Assert.IsTrue(viewModel.Include70s, "Include70s filter was not reset");
            Assert.IsTrue(viewModel.Include80s, "Include80s filter was not reset");
            Assert.IsTrue(viewModel.Include90s, "Include90s filter was not reset");
            Assert.IsTrue(viewModel.Include00s, "Include00s filter was not reset");
        }

        [TestMethod]
        public void Filters_OnRefreshAndCacheNotExpired_AreResetToDefaults()
        {
            // Arrange
            var serviceMock = GetMockPersonService();
            var currentOrder = GetFakeCatalogOrder();
            var viewModel = GetCustomViewModel(serviceMock.Object, currentOrder);
            var tracker = new PropertyChangeTracker(viewModel);
            viewModel.Initialize();
            tracker.WaitForChange("LastUpdateTime", 1);
            viewModel.Include70s = false;
            viewModel.Include80s = false;
            viewModel.Include90s = false;
            viewModel.Include00s = false;

            // Act
            tracker.Reset();
            viewModel.RefreshCatalog();
            tracker.WaitForChange("LastUpdateTime", 1);
            serviceMock.Verify(s => s.EndGetPeople(It.IsAny<IAsyncResult>()), Times.Once(),
                "Service was not called one (and only one) time");

            // Assert
            Assert.IsTrue(viewModel.Include70s, "Include70s filter was not reset");
            Assert.IsTrue(viewModel.Include80s, "Include80s filter was not reset");
            Assert.IsTrue(viewModel.Include90s, "Include90s filter was not reset");
            Assert.IsTrue(viewModel.Include00s, "Include00s filter was not reset");
        }

        #endregion

        #region Catalog Item Selection

        [TestMethod]
        public void ModelSelectedItems_AddToSelectionWithNewPerson_PersonAdded()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            viewModel.Initialize();
            var newPerson = new Person()
            {
                LastName = "Rogers",
                FirstName = "Peter",
                StartDate = DateTime.Parse("01/01/2013"),
                Rating = 5
            };
            Assert.IsFalse(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Invalid Arrangement: Person already in list");

            // Act
            viewModel.AddToSelection(newPerson);

            // Assert
            Assert.IsTrue(viewModel.Model.SelectedPeople.Contains(newPerson),
                "New Person was not added to Model Selection");
        }

        [TestMethod]
        public void ModelSelectedItems_AddToSelectionWithExistingPerson_SelectionIsUnchanged()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            viewModel.Initialize();
            var newPerson = new Person()
            {
                LastName = "Rogers",
                FirstName = "Peter",
                StartDate = DateTime.Parse("01/01/2013"),
                Rating = 5
            };
            viewModel.AddToSelection(newPerson);
            var oldCount = viewModel.Model.SelectedPeople.Count;
            Assert.IsTrue(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Invalid Arrangement: Person is not in list");

            // Act
            viewModel.AddToSelection(newPerson);
            var newCount = viewModel.Model.SelectedPeople.Count;

            // Assert
            Assert.AreEqual(oldCount, newCount);
            Assert.IsTrue(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Existing Person was not found in Model Selection");
        }

        [TestMethod]
        public void ModelSelectedItems_RemoveFromSelectionWithExistingPerson_PersonRemoved()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            viewModel.Initialize();
            var newPerson = new Person() { LastName = "Rogers", FirstName = "Peter", StartDate = DateTime.Parse("01/01/2013"), Rating = 5 };
            viewModel.AddToSelection(newPerson);

            // Act
            Assert.IsTrue(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Invalid Arrangement: Person is not in list");
            viewModel.RemoveFromSelection(newPerson);

            // Assert
            Assert.IsFalse(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Existing Person was not removed from Model Selection");
        }

        [TestMethod]
        public void ModelSelectedItems_RemoveFromSelectionWithNewPerson_SelectionIsUnchanged()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            viewModel.Initialize();
            var newPerson = new Person() { LastName = "Rogers", FirstName = "Peter", StartDate = DateTime.Parse("01/01/2013"), Rating = 5 };
            var oldCount = viewModel.Model.SelectedPeople.Count;

            // Act
            Assert.IsFalse(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Invalid Arrangement: Person is in list");
            viewModel.RemoveFromSelection(newPerson);
            var newCount = viewModel.Model.SelectedPeople.Count;

            // Assert
            Assert.AreEqual(oldCount, newCount);
            Assert.IsFalse(viewModel.Model.SelectedPeople.Contains(newPerson),
                "Existing Person was found in Model Selection");
        }

        [TestMethod]
        public void ModelSelectedPeople_OnClearSelection_IsEmpty()
        {
            // Arrange
            var viewModel = GetStandardViewModel();
            viewModel.Initialize();
            viewModel.AddToSelection(_70sPerson);
            viewModel.AddToSelection(_00sPerson);
            var oldCount = viewModel.Model.SelectedPeople.Count;
            Assert.AreNotEqual(0, oldCount,
                "Invalid Arrangement: Model Selection is empty.");

            // Act
            viewModel.ClearSelection();
            var newCount = viewModel.Model.SelectedPeople.Count;

            // Assert
            Assert.AreEqual(0, newCount);
        }

        #endregion
    }
}
