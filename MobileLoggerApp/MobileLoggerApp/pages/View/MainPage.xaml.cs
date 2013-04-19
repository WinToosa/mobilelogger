﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using MobileLogger;
using MobileLoggerApp.pages;
using MobileLoggerScheduledAgent.Database;
using Newtonsoft.Json.Linq;
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MobileLoggerApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        MainViewModel _viewModel;
        bool _isNewPageInstance = false;

        public delegate void KeyPressEventHandler(object sender, KeyEventArgs e);
        public delegate void KeyboardFocusHandler();
        public delegate void TouchEventHandler(MainPage mainPage, TouchFrameEventArgs e);
        public delegate void SearchResultTapped(JObject searchResult);
        public delegate void CustomPressedEventHandler(object sender, EventArgs e);

        public static event KeyPressEventHandler keyUp;
        public static event KeyboardFocusHandler keyboardGotFocus;
        public static event KeyboardFocusHandler keyboardLostFocus;
        public static event TouchEventHandler screenTouch;
        public static event SearchResultTapped searchResultTap;

        public const string ConnectionString = @"Data Source = 'isostore:/LogEventDB.sdf';";
        private const string TASK_NAME = "MobileLoggerScheduledAgent";
        private string searchTerm;

        GoogleCustomSearch search;
        WeatherInformationSearch weatherInfo;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeAppSettings();

            _isNewPageInstance = true;

            //start background agent
            StartAgent();
            using (LogEventDataContext logDBContext = new LogEventDataContext(ConnectionString))
            {
                if (!logDBContext.DatabaseExists())
                {
                    // create database if it does not exist
                    try
                    {
                        logDBContext.CreateDatabase();
                    }
                    catch (InvalidOperationException ioe)
                    {
                        System.Diagnostics.Debug.WriteLine("InvalidOperationException while creating database..." + ioe);
                    }
                }
            }

            // Set the data context of the listbox control to the data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            string appVersion = (from manifest in System.Xml.Linq.XElement.Load("WMAppManifest.xml").Descendants("App")
                                 select manifest).SingleOrDefault().Attribute("Version").Value;

            VersionInfo.Text = "Version: " + appVersion;
        }

        private void InitializeAppSettings()
        {
            if (!appSettings.Contains("FirstRun"))
            {
                appSettings.Add("FirstRun", (bool)true);

                MessageBox.Show("This app will collect personal data, including location and other sensor data for research purposes. " +
                "To use this application, you need to give permission to access and share your personal data. " +
                "You can later decide, what kind of data this application is able to collect. " +
                "Press OK to continue.",
                "Personal data", MessageBoxButton.OK);
            }
            else
            {
                appSettings["FirstRun"] = (bool)false;
            }
        }

        private void StartAgent()
        {
            try
            {
                StopAgentIfStarted();

                PeriodicTask task = new PeriodicTask(TASK_NAME);
                task.ExpirationTime = DateTime.Now.AddDays(14);
                task.Description = "This is the background upload agent for MobileLoggerApp";
                // Place the call to Add in a try block in case the user has disabled agents.

                ScheduledActionService.Add(task);
            }
            catch (InvalidOperationException exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }
                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
            catch (SchedulerServiceException)
            {
                // No user action required.
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine("DEBUG START AGENT");
            // If we're debugging, attempt to start the task immediately 
            try
            {
                ScheduledActionService.LaunchForTest(TASK_NAME, new TimeSpan(0, 0, 0));
            }
            catch (InvalidOperationException exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }
#endif
        }

        private void StopAgentIfStarted()
        {
            try
            {
                if (ScheduledActionService.Find(TASK_NAME) != null)
                {
                    ScheduledAction task = ScheduledActionService.Find(TASK_NAME);
                    ScheduledActionService.Remove(TASK_NAME);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if (!App.ViewModel.IsLogDataLoaded)
                App.ViewModel.GetLogData();
#endif

            if (!App.ViewModel.IsSettingsLoaded)
                App.ViewModel.GetHandlerSettings();

            Touch.FrameReported += Touch_FrameReported;
            this.search = new GoogleCustomSearch();
            this.weatherInfo = new WeatherInformationSearch();
        }

        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            if (screenTouch != null)
                screenTouch(this, e);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (keyboardGotFocus != null)
                keyboardGotFocus();
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (keyboardLostFocus != null)
                keyboardLostFocus();
        }

        /// <summary>
        /// Event handler for search box, sends the search query to be queried from Google Custom Search
        /// </summary>
        /// <param name="sender">The object that initiated this event</param>
        /// <param name="e">Arguments for the key event that initiated this event</param>
        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (keyUp != null)
                keyUp(sender, e);

            if (e.Key.Equals(Key.Enter))
            {
                this.Focus();
                searchTerm = SearchTextBox.Text;

                if (!searchTerm.Equals(""))
                {
                    search.Search(searchTerm, true);
                    weatherInfo.GetForecast();
                }
            }
            HandlersManager.SaveSensorLog();
        }

        /// <summary>
        /// An event handler for tapping a result, opens the result in Internet Explorer
        /// </summary>
        /// <param name="sender">The StackPanel object that was tapped</param>
        /// <param name="e">Event arguments of the tap event</param>
        private void searchResultItemTappedEvent(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (StackPanel)sender;
            SearchResults searchResult = (SearchResults)stackPanel.DataContext;

            if (searchResultTap != null)
                searchResultTap(searchResult.SearchResult);
            search.OpenBrowser(searchResult.SearchResultLink.ToString());
        }

        /// <summary>
        /// Loads the next page of Google search results, up to 10 pages can be viewed
        /// </summary>
        /// <param name="sender">The button that initiated this event</param>
        /// <param name="e">Event arguments of the tap event</param>
        private void LoadNextPage(object sender, RoutedEventArgs e)
        {
            search.Search(searchTerm);
        }

        private void HandlerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ListBoxItem checkedHandlerItem = this.SettingsListBox.ItemContainerGenerator.ContainerFromItem((sender as CheckBox).DataContext) as ListBoxItem;

            if (checkedHandlerItem != null)
            {
                HandlersManager.EnableHandler(GetHandlerName(checkedHandlerItem));
            }
        }

        private void HandlerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ListBoxItem checkedHandlerItem = this.SettingsListBox.ItemContainerGenerator.ContainerFromItem((sender as CheckBox).DataContext) as ListBoxItem;

            if (checkedHandlerItem != null)
            {
                HandlersManager.DisableHandler(GetHandlerName(checkedHandlerItem));
            }
        }

        private string GetHandlerName(ListBoxItem checkedHandlerItem)
        {
            HandlerSettings handlerItem = checkedHandlerItem.DataContext as HandlerSettings;
            return handlerItem.HandlerName.ToString();
        }

        private void debugButton_Click(object sender, RoutedEventArgs e)
        {
            StartAgent();
        }

        private void currentPivotItem(object sender, SelectionChangedEventArgs e)
        {
#if DEBUG
            Pivot pivot = sender as Pivot;

            // Data PivotItem
            if (pivot.SelectedIndex == 1)
            {
                App.ViewModel.GetLogData();
            }
#endif
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode != NavigationMode.Back)
            {
                if (this.State.ContainsKey("ViewModel"))
                    this.State["ViewModel"] = _viewModel;
                else
                    this.State.Add("ViewModel", _viewModel);
            }

            StateUtilities.IsLaunching = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_isNewPageInstance)
            {
                if (_viewModel == null)
                {
                    if (State.ContainsKey("ViewModel"))
                    {
                        _viewModel = State["ViewModel"] as MainViewModel;
                    }
                    else
                    {
                        _viewModel = App.ViewModel;
                    }
                }
                DataContext = _viewModel;
            }
            _viewModel.GetHandlerSettings();
            _isNewPageInstance = false;
        }
    }
}