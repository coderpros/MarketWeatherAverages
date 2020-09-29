namespace MarketWeatherAverages.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The climate data model with data from the NCDC.
    /// </summary>
    public class ClimateData
    {
        /// <summary>
        /// The result.
        /// </summary>
        public class Result
        {
            /// <summary>
            /// The date.
            /// </summary>
            [JsonProperty("date")]
            public DateTime Date;

            /// <summary>
            /// The data type.
            /// </summary>
            [JsonProperty("datatype")]
            public string DataType;

            /// <summary>
            /// The station.
            /// </summary>
            [JsonProperty("station")]
            private string Station;

            /// <summary>
            /// The attributes.
            /// </summary>
            [JsonProperty("attributes")]
            public string Attributes;

            /// <summary>
            /// The value.
            /// </summary>
            [JsonProperty("value")]
            public double Value;
        }

        /// <summary>
        /// The root.
        /// </summary>
        public class Root
        {
            [JsonProperty("results")]
            public List<Result> Results;
        }
    }
}
