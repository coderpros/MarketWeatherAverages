namespace MarketWeatherAverages.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The location data model from NCDC.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// The result set.
        /// </summary>
        public class ResultSet
        {
            /// <summary>
            /// The offset.
            /// </summary>
            [JsonProperty("offset")]
            public int Offset;

            /// <summary>
            /// The count.
            /// </summary>
            [JsonProperty("count")]
            public int Count;

            /// <summary>
            /// The maximum number of records to return.
            /// </summary>
            [JsonProperty("limit")]
            public int Limit;
        }

        /// <summary>
        /// The metadata.
        /// </summary>
        public class Metadata
        {
            /// <summary>
            /// The result set.
            /// </summary>
            [JsonProperty("resultset")]
            public ResultSet ResultSet;
        }

        /// <summary>
        /// The result.
        /// </summary>
        public class Result
        {
            /// <summary>
            /// Gets or sets the minimum date.
            /// </summary>
            [JsonProperty("mindate")]
            public string MinDate { get; set; }

            /// <summary>
            /// Gets or sets the maximum date.
            /// </summary>
            [JsonProperty("maxdate")]
            public string MaxDate { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the data coverage.
            /// </summary>
            [JsonProperty("datacoverage")]
            public double DataCoverage { get; set; }

            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }
        }

        /// <summary>
        /// The root.
        /// </summary>
        public class Root
        {
            /// <summary>
            /// The metadata.
            /// </summary>
            [JsonProperty("metadata")]
            public Metadata Metadata;

            /// <summary>
            /// The results.
            /// </summary>
            [JsonProperty("results")]
            public List<Result> Results;
        }
    }

}
