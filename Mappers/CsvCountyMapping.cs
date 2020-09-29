namespace MarketWeatherAverages.Mappers
{
    using TinyCsvParser.Mapping;

    public class CsvCountyMapping : CsvMapping<Models.County>
    {
        public CsvCountyMapping() : base()
        {
            this.MapProperty(0, x => x.Name);
            this.MapProperty(1, x => x.State);
        }
    }
}
