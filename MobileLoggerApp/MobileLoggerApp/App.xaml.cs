﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MobileLogger;
using MobileLoggerApp.Handlers;
using MobileLoggerApp.pages;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;

namespace MobileLoggerApp
{
    public partial class App : Application
    {
        //special case, we don't update this like other handlers, only on startup and exit so don't add this to the list
        public static SessionHandler sessionHandler;
        static HandlersManager handlers;

        private static MainViewModel viewModel = null;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                {
                    viewModel = new MainViewModel();
                }
                return viewModel;
            }
            set
            {
                viewModel = value;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = false;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                //PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("FirstRun"))
            {
                IsolatedStorageSettings.ApplicationSettings.Add("FirstRun", (bool)true);
                IsolatedStorageSettings.ApplicationSettings["ServerRoot"] = "http://t-jonimake.users.cs.helsinki.fi/MobileLoggerServer";
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["FirstRun"] = (bool)false;
                StateUtilities.StartHandlers = true;
                StartHandlers();
            }
        }

        public static void StartHandlers()
        {
            if (sessionHandler == null)
                sessionHandler = new SessionHandler();

            sessionHandler.Start();

            if (handlers == null)
                handlers = new HandlersManager();

            handlers.InitHandlers();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["FirstRun"] = (bool)false;

            if (e.IsApplicationInstancePreserved)
            {
                return;
            }
            StateUtilities.StartHandlers = true;
            StartHandlers();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("App.xaml.cs.Application_Deactivated");
            sessionHandler.End();
            LogEventSaver.Instance.SaveAll();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("App.xaml.cs.Application_Closing");
            sessionHandler.End();
            LogEventSaver.Instance.SaveAll();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debug.WriteLine(e.ToString());
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            MessageBox.Show("An unhandled exception has occurred. Message: " + e.ToString() + " Application will now close.");
        }
        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
