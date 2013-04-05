﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MobileLoggerApp.Handlers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;

namespace MobileLoggerApp
{
    public partial class App : Application
    {
        public static List<AbstractLogHandler> logHandlers;

        //special case, we don't update this like other handlers, only on startup and exit so don't add this to the list
        public static SessionHandler sessionHandler;

        private static pages.MainViewModel viewModel = null;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static pages.MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                {
                    viewModel = new pages.MainViewModel();
                }
                return viewModel;
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
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        private void InitHandlers()
        {           
            //always create new to ensure no duplicates
            logHandlers = new List<AbstractLogHandler>();

            AccelHandler accelerometer = new AccelHandler();
            //Application.Current.Resources.Add("accelHandler", accelerometer);
            accelerometer.StartAccelWatcher();
            logHandlers.Add(accelerometer);

            CompassHandler compass = new CompassHandler();
            //Application.Current.Resources.Add("compassHandler", compass);
            compass.StartCompassWatcher();
            logHandlers.Add(compass);

            GpsHandler gps = new GpsHandler();
            //Application.Current.Resources.Add("gpsHandler", gps);
            gps.StartCoordinateWatcher();
            logHandlers.Add(gps);

            GyroHandler gyroscope = new GyroHandler();
            //Application.Current.Resources.Add("gyroHandler", gyroscope);
            gyroscope.StartGyroWatcher();
            logHandlers.Add(gyroscope);

            KeyboardHandler keyboard = new KeyboardHandler();
            //Application.Current.Resources.Add("keyboardHandler", keyboard);
            keyboard.StartKeyBoardWatcher();
            logHandlers.Add(keyboard);

            KeyPressHandler keyPress = new KeyPressHandler();
            //Application.Current.Resources.Add("keyPressHandler", keyPress);
            keyPress.StartKeyPressWatcher();
            logHandlers.Add(keyPress);

            NetworkHandler network = new NetworkHandler();
           // Application.Current.Resources.Add("networkHandler", network);
            network.StartNetwork();
            logHandlers.Add(network);

            ScreenTouchHandler screenTouch = new ScreenTouchHandler();
            Application.Current.Resources.Add("touchHandler", screenTouch);
            screenTouch.StartScreenTouchWatcher();
            logHandlers.Add(screenTouch);

            SearchDataHandler searchData = new SearchDataHandler();
            //Application.Current.Resources.Add("searchDataHandler", searchData);
            searchData.StartSearchDataHandler();
            logHandlers.Add(searchData);
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            sessionHandler = new SessionHandler();
            sessionHandler.Start();
            InitHandlers();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Ensure that application state is restored appropriately
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
            sessionHandler.Start();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            sessionHandler.End();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            sessionHandler.End();
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
