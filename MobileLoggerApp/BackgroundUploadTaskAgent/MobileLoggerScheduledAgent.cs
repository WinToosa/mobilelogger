﻿using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MobileLoggerScheduledAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;
        public static readonly string serverRoot = "http://t-jonimake.users.cs.helsinki.fi/MobileLoggerServerDev";

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            System.Diagnostics.Debug.WriteLine("Initializing background task agent");
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Unhandled exception"); 
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                //System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            
            System.Diagnostics.Debug.WriteLine("OnInvoke");
            //TODO: Add code to perform your task in background
            SendMessages();
            
        }


        private void SendMessages()
        {
            System.Diagnostics.Debug.WriteLine("SendMessages");

            using (LogEventDataContext logDb = new LogEventDataContext(LogEventDataContext.ConnectionString))
            {
                if (!logDb.DatabaseExists())
                {
                    System.Diagnostics.Debug.WriteLine(this.GetType().Name + ": DB does not exist");
                    return;
                }

                //logDb.addEvent("{\"phoneId\":string,\"text\":string,\"timestamp\":" + DeviceTools.GetUnixTime(DateTime.UtcNow) + ",\"lat\":12.3,\"lon\":12.3,\"alt\":12.3}", "/log/gps");
                //System.Diagnostics.Debug.WriteLine(this.GetType().Name + ": GetLogEvents().size() " + logDb.GetLogEvents().Count);
                LogEvent e = new LogEvent();
                e.sensorEvent = "{\"lat\":1.0,\"lon\":2.0,\"alt\":0.0,\"phoneId\":\"123456789012345\",\"timestamp\":1361264436365}";
                System.Diagnostics.Debug.WriteLine(e.sensorEvent);
                e.relativeUrl = "/log/gps";
                //foreach (LogEvent e in logDb.GetLogEvents())
               // {
                    SendMessage(e);
                //}
            }
            System.Diagnostics.Debug.WriteLine(this.GetType().Name + ".AsyncSendMessages event handler finished");
        }

        private void SendMessagesWorkComplete(object sender, EventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("finished send messages work");
            System.Diagnostics.Debug.WriteLine(sender.ToString() + " " + args.ToString());
        }


        private void SendMessage(LogEvent logevent)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverRoot + logevent.relativeUrl);
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.BeginGetRequestStream(asynchronousResult =>
            {
                SendData(logevent, request, asynchronousResult);
            }, request);

        }

        private void SendData(LogEvent logevent, HttpWebRequest request, IAsyncResult asynchronousResult)
        {
            HttpWebRequest requestStream = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream putStream = requestStream.EndGetRequestStream(asynchronousResult);

            //Console.WriteLine("Please enter the input data to be posted:");
            string putData = logevent.sensorEvent;

            // Convert the string into a byte array. 
            byte[] byteArray = Encoding.UTF8.GetBytes(putData);

            // Write to the request stream.
            putStream.Write(byteArray, 0, putData.Length);
            putStream.Close();

            // Start the asynchronous operation to get the response
            requestStream.BeginGetResponse(response =>
            {
                GetResponse(response);

            }, request);//new AsyncCallback(GetResponseCallback), request);
        }

        private void GetResponse(IAsyncResult response)
        {
            try
            {
                HttpWebRequest request1 = (HttpWebRequest)response.AsyncState;

                // End the operation
                HttpWebResponse finalresponse = (HttpWebResponse)request1.EndGetResponse(response);
                Stream streamResponse = finalresponse.GetResponseStream();
                StreamReader streamRead = new StreamReader(streamResponse);
                string responseString = streamRead.ReadToEnd();
                Console.WriteLine(responseString);
                // Close the stream object
                streamResponse.Close();
                streamRead.Close();

                // Release the HttpWebResponse
                finalresponse.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }       
        
    }
}