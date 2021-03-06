﻿using MobileLoggerScheduledAgent.Devicetools;
using Newtonsoft.Json.Linq;

namespace MobileLoggerApp.Handlers
{
    class WeatherDataHandler : AbstractLogHandler
    {
        public WeatherDataHandler()
        {
        }

        public override void SaveSensorLog()
        {
            //handle saving in the event handler method below
        }

        public override void StartWatcher()
        {
            WeatherInformationSearch.weatherDataEvent += new WeatherInformationSearch.WeatherDataHandler(WeatherData);
            this.IsEnabled = true;
        }

        public override void StopWatcher()
        {
            WeatherInformationSearch.weatherDataEvent -= WeatherData;
            this.IsEnabled = false;
        }

        private void WeatherData(JObject weatherData)
        {
            JObject forecast = new JObject(weatherData.GetValue("data").ToObject<JObject>());
            JObject currentCondition = new JObject(forecast.GetValue("current_condition")[0].ToObject<JObject>());
            JObject weatherDescription = new JObject(currentCondition.GetValue("weatherDesc")[0].ToObject<JObject>());

            AddJOValue("temperature", currentCondition.GetValue("temp_C"));
            AddJOValue("windspeed", currentCondition.GetValue("windspeedKmph"));
            AddJOValue("weatherdescription", weatherDescription.GetValue("value"));

            AddJOValue("timestamp", DeviceTools.GetUnixTime());
            System.Diagnostics.Debug.WriteLine(this.data);
            SaveLogToDB(this.data, "/log/weather");
        }
    }
}
