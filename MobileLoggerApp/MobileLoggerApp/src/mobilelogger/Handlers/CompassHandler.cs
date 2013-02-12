﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Devices.Sensors;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework;

namespace MobileLoggerApp.src.mobilelogger.Handlers
{
    public class CompassHandler : AbstractLogHandler
    {
        Compass compass;
        //DispatcherTimer timer;
        JObject joCompass;

        double magneticHeading;
        double trueHeading;
        double headingAccuracy;
        Vector3 rawMagnetometerReading;
        bool isDataValid;

        bool calibrating = false;

        public override void SaveSensorLog()
        {
            SaveLogToDB(joCompass, "/log/compass");
        }
        public void startCompassWatcher()
        {
            if (compass == null)
            {
                // Instantiate the Compass.
                compass = new Compass();
                //compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
                //compass.Calibrate += new EventHandler<CalibrationEventArgs>(compass_Calibrate);
            }
            compass.Start();

            if (joCompass == null)
            {
                joCompass = new JObject();
            }

        }


        void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            if (joCompass["trueHeading"] == null)
            {
                joCompass.Add("trueHeading", (float)e.SensorReading.TrueHeading);
            }
            else
            {
                joCompass["trueHeading"].Replace((float)e.SensorReading.TrueHeading);
            }

            if (joCompass["magneticHeading"] == null)
            {
                joCompass.Add("magneticHeading", (float)e.SensorReading.MagneticHeading);
            }
            else
            {
                joCompass["magneticHeading"].Replace((float)e.SensorReading.MagneticHeading);
            }

            if (joCompass["headingAccuracy"] == null)
            {
                joCompass.Add("headingAccuracy", (float)Math.Abs(e.SensorReading.HeadingAccuracy));
            }
            else
            {
                joCompass["headingAccuracy"].Replace((float)Math.Abs(e.SensorReading.HeadingAccuracy));
            }
            if (joCompass["rawMagneticReadingX"] == null)
            {
                joCompass.Add("rawMagneticReadingX", (float)e.SensorReading.MagnetometerReading.X);
            }
            else
            {
                joCompass["rawMagneticReadingX"].Replace((float)e.SensorReading.MagnetometerReading.X);
            }

            if (joCompass["rawMagneticReadingY"] == null)
            {
                joCompass.Add("rawMagneticReadingY", (float)e.SensorReading.MagnetometerReading.Y);
            }
            else
            {
                joCompass["rawMagneticReadingY"].Replace((float)e.SensorReading.MagnetometerReading.Y);
            }
            if (joCompass["rawMagneticReadingZ"] == null)
            {
                joCompass.Add("rawMagneticReadingZ", (float)e.SensorReading.MagnetometerReading.Z);
            }
            else
            {
                joCompass["rawMagneticReadingZ"].Replace((float)e.SensorReading.MagnetometerReading.Z);
            }

        }

    }
}
