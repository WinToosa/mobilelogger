﻿using Newtonsoft.Json.Linq;
using System;

namespace MobileLoggerApp.src.mobilelogger.Handlers
{
    class SearchDataHandler : AbstractLogHandler
    {
        string[] searchResultRemovableItems = {"htmlFormattedUrl", "htmlSnippet", "htmlTitle", "pagemap"};
        int localDBStringMaxLength = DeviceTools.GetDeviceLocalDBStringMaxLength();
        DateTime timestamp;

        public override void SaveSensorLog()
        {
        }

        public void StartSearchDataHandler()
        {
            GoogleCustomSearch.searchDataEvent += new GoogleCustomSearch.SearchDataHandler(SearchData);
        }

        private void SearchData(JObject searchData)
        {
            this.timestamp = DateTime.UtcNow;
            JArray searchResults = (JArray)searchData["items"];

            ProcessSearchData(searchData);

            if (searchResults != null)
            {
                JToken queries = searchData.GetValue("queries");
                JToken request = queries.Value<JToken>("request")[0];
                int offset = request.Value<int>("startIndex");

                ProcessSearchResults(searchResults, offset);
            }
        }

        private void ProcessSearchData(JObject searchData)
        {
            searchData.Remove("items");
            searchData.Add("time", DeviceTools.GetUnixTime(this.timestamp));
            searchData.Add("index", 0);
            SaveLogToDB(searchData, "/log/google");
        }

        private void ProcessSearchResults(JArray searchResults, int offset)
        {
            int index = 0;

            foreach (JToken result in searchResults)
            {
                JObject resultObj = result.ToObject<JObject>();

                resultObj.Add("index", index + offset);
                index++;
                resultObj.Add("time", DeviceTools.GetUnixTime(this.timestamp));

                ParseSearchResult(resultObj);
                SaveLogToDB(resultObj, "/log/google");
            }
        }

        /// <summary>
        /// This method parses the search result because device's local DB can only handle strings shorter than 4000 characters.
        /// </summary>
        private void ParseSearchResult(JObject resultObj)
        {
            int removableItemIndex = 0;

            while (resultObj.ToString().Length > localDBStringMaxLength)
            {
                resultObj.Remove(searchResultRemovableItems[removableItemIndex]);
                if (removableItemIndex < searchResultRemovableItems.Length)
                    removableItemIndex++;
                else
                    break;
            }
        }
    }
}
