namespace MarketWeatherAverages.Mappers
{
    using TinyCsvParser.Mapping;

    public class CsvLocationMapping : CsvMapping<Models.Location.Result>
    {
        public CsvLocationMapping() : base()
        {
            this.MapProperty(0, x => x.Id);
            this.MapProperty(1, x => x.Name);
            this.MapProperty(2, x => x.DataCoverage);
            this.MapProperty(3, x => x.MaxDate);
            this.MapProperty(4, x => x.MinDate);
        }
    }
}