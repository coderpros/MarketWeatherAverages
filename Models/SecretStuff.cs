namespace MarketWeatherAverages.Models
{
    /// <summary>
    /// The secret stuff model for getting user secrets.
    /// </summary>
    public class SecretStuff
    {
        /// <summary>
        /// Gets the NOAA API token.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public string NOAAToken { get; set; }
    }
}
