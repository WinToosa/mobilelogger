﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using MobileLogger;
using MobileLoggerApp.pages;
using MobileLoggerScheduledAgent.Database;
using Newtonsoft.Json.Linq;
using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MobileLoggerApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        bool _isNewPageInstance = false;
        bool _isFirstRun;

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
        //Button nextPageButton;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

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
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
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
            InitializeApplication();

            Touch.FrameReported += Touch_FrameReported;
            this.weatherInfo = new WeatherInformationSearch();
            this.search = new GoogleCustomSearch();
            GoogleCustomSearch.searchPerformedEvent += new GoogleCustomSearch.SearchPerformedHandler(SearchPerformed);
            ToggleNextPageButton();
        }

        private void InitializeApplication()
        {
            if (_isFirstRun)
            {
                MessageBoxResult result = MessageBox.Show(
                    "This app will collect personal data, including location and other sensor data for research purposes. " +
                    "To use this feature, you need to give permission to access and share your personal data. " +
                    "You can later decide, what kind of data this application is able to collect. " +
                    "Press OK to enable data collecting, press Cancel to disable data collecting.",
                    "Personal data",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    StateUtilities.StartHandlers = true;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    StateUtilities.StartHandlers = false;
                }
                App.StartHandlers();
                GetApplicationState();
            }
        }

        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            if (screenTouch != null)
                screenTouch(this, e);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchTextBox.SelectAll();

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

                if (!SearchTextBox.Text.Equals("") && !this.SearchTextBox.Text.Equals(this.searchTerm))
                {
                    this.searchTerm = SearchTextBox.Text;
                    StateUtilities.NewSearch = true;
                    search.Search(this.searchTerm);
                    weatherInfo.GetForecast();
                }
            }
            HandlersManager.SaveSensorLog();
        }

        private void SearchPerformed()
        {
            if (StateUtilities.NewSearch)
            {
                ScrollSearchListToTop();
                StateUtilities.NewSearch = false;
            }
            ToggleNextPageButton();
        }

        private void ScrollSearchListToTop()
        {
            ScrollViewer searchListScrollView = FindVisualChild<ScrollViewer>(SearchListBox);

            if (searchListScrollView != null)
            {
                searchListScrollView.UpdateLayout();
                searchListScrollView.ScrollToVerticalOffset(0.0);
            }
        }

        private void ToggleNextPageButton()
        {
            Button nextPageButton = FindVisualChild<Button>(SearchListBox);

            if (nextPageButton != null)
            {
                if (!StateUtilities.NewSearch &&
                    App.ViewModel.Results.Count > 0 && App.ViewModel.Results.Count < 100)
                {
                    if (!nextPageButton.Visibility.Equals(Visibility.Visible))
                    {
                        nextPageButton.Visibility = Visibility.Visible;
                        nextPageButton.IsEnabled = true;
                    }
                }
                else
                {
                    if (!nextPageButton.Visibility.Equals(Visibility.Collapsed))
                    {
                        nextPageButton.Visibility = Visibility.Collapsed;
                        nextPageButton.IsEnabled = false;
                    }
                }
            }
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
            {
                JObject tappedResult = JObject.Parse(searchResult.SearchResult);
                searchResultTap(tappedResult);
            }
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
                CheckBox ch = sender as CheckBox;

                ch.IsChecked = HandlersManager.EnableHandler(GetHandlerName(checkedHandlerItem));
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

        private void ServerTextButton_Clicked(object sender, RoutedEventArgs e)
        {
            ShowServerChangeMessage();
        }

        private void ServerTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                ShowServerChangeMessage();
                this.Focus();
            }
        }

        private void ShowServerChangeMessage()
        {
            MessageBoxResult result = MessageBox.Show(
                "This will change the server. Make sure that you have entered a valid server url. " +
                "Click OK to continue, Cancel to go back.",
                "Server url",
                MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("ServerRoot"))
                    IsolatedStorageSettings.ApplicationSettings["ServerRoot"] = ServerTextBox.Text;
                else
                    IsolatedStorageSettings.ApplicationSettings.Add("ServerRoot", ServerTextBox.Text);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                this.ServerTextBox.Text = IsolatedStorageSettings.ApplicationSettings["ServerRoot"] as string;
            }
        }

        private void ServerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ServerTextButton.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
        }

        private void ServerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ServerTextButton.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            appSettings.TryGetValue("FirstRun", out _isFirstRun);

            if (!_isFirstRun)
            {
                StateUtilities.StartHandlers = true;
                GetApplicationState();
            }
        }

        private void GetApplicationState()
        {
            if (_isNewPageInstance)
            {
                if (this.State.ContainsKey("ViewModel"))
                {
                    App.ViewModel = this.State["ViewModel"] as MainViewModel;
                }
                DataContext = App.ViewModel;
                App.ViewModel.GetHandlerSettings();
                App.ViewModel.GetAppInfo();
            }

            if (this.State.ContainsKey("ServerRoot"))
                ServerTextBox.Text = this.State["ServerRoot"] as string;
            else if (IsolatedStorageSettings.ApplicationSettings.Contains("ServerRoot"))
                ServerTextBox.Text = IsolatedStorageSettings.ApplicationSettings["ServerRoot"] as string;

            if (this.State.ContainsKey("SearchTerm"))
            {
                this.searchTerm = this.State["SearchTerm"] as string;
                SearchTextBox.Text = this.searchTerm;
            }

            if (this.State.ContainsKey("SearchPerformed"))
                StateUtilities.NewSearch = State["SearchPerformed"].Equals("True");

            _isNewPageInstance = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode != NavigationMode.Back)
            {
                SetApplicationState();
            }
        }

        private void SetApplicationState()
        {
            App.ViewModel.SaveHandlerSettings();

            if (this.State.ContainsKey("ViewModel"))
                this.State["ViewModel"] = App.ViewModel;
            else
                this.State.Add("ViewModel", App.ViewModel);

            if (SearchTextBox.Text != null)
            {
                if (this.State.ContainsKey("SearchTerm"))
                    this.State["SearchTerm"] = SearchTextBox.Text;
                else
                    this.State.Add("SearchTerm", SearchTextBox.Text);
            }
            if (this.State.ContainsKey("SearchPerformed"))
                this.State["SearchPerformed"] = StateUtilities.NewSearch.ToString();
            else
                this.State.Add("SearchPerformed", StateUtilities.NewSearch.ToString());

            if (this.State.ContainsKey("ServerRoot"))
                this.State["ServerRoot"] = ServerTextBox.Text;
            else
                this.State.Add("ServerRoot", ServerTextBox.Text);
        }

        public static ChildItem FindVisualChild<ChildItem>(DependencyObject obj)
            where ChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildItem)
                    return (ChildItem)child;
                else
                {
                    ChildItem childOfChild = FindVisualChild<ChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}
