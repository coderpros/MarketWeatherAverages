namespace MarketWeatherAverages.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The county data model.
    /// </summary>
    public class County
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state (must be two characters).
        /// </summary>
        [MinLength(2), MaxLength(2)]
        public string State { get; set; }
    }
}
