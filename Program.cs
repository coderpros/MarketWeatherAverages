namespace MarketWeatherAverages
{
    #region USINGS
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;

    using TinyCsvParser;
    #endregion

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        private static IConfigurationRoot Configuration { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static async Task Main(string[] args)
        {
            var sourceFileName = string.Empty;
            var destinationFileName = string.Empty;
            var startDate = string.Empty;
            var endDate = string.Empty;
            var unitOfMeasure = "standard";
            var dateFormat = "yyyyMMdd";

            var locations = new List<Models.Location.Result>();
            var builder = new ConfigurationBuilder();

            var dataPath = $"{AppContext.BaseDirectory}data";
            var locationsFileName = $"{dataPath}\\locations.csv";

            var csvParserOptions = new CsvParserOptions(false, '|');
            var csvParserCounty = new CsvParser<Models.County>(csvParserOptions, new Mappers.CsvCountyMapping());
            var csvParserLocations = new CsvParser<Models.Location.Result>(
                csvParserOptions,
                new Mappers.CsvLocationMapping());

            builder.AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true);

            builder.AddUserSecrets<Program>();

            Configuration = builder.Build();

            if (args.Contains("/source"))
            {
                sourceFileName = args[Array.IndexOf(args, "/source") + 1];
            }

            // ReSharper disable once UnusedVariable
            if (args.Length > 1 && DateTime.TryParse(args[0], out var temp0))
            {
                startDate = args[0];
            }

            // ReSharper disable once UnusedVariable
            if (args.Length > 1 && DateTime.TryParse(args[1], out var temp2))
            {
                endDate = args[1];
            }

            if (args.Contains("/destination"))
            {
                destinationFileName = args[Array.IndexOf(args, "/destination") + 1];

                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
            }

            if (args.Contains("/metric"))
            {
                unitOfMeasure = "metric";
            }

            if (args.Contains("/dateFormat"))
            {
                dateFormat = args[Array.IndexOf(args, "/dateFormat") + 1];
            }

            IServiceCollection services = new ServiceCollection();

            services.Configure<Models.SecretStuff>(Configuration.GetSection(nameof(Models.SecretStuff))).AddOptions()
                .AddSingleton<ISecretRevealer, SecretRevealer>().BuildServiceProvider();

            var serviceProvider = services.BuildServiceProvider();
            var revealer = serviceProvider.GetService<ISecretRevealer>();

            if (args.Contains("/?"))
            {
                PrintHelp();
            }

            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate) || string.IsNullOrEmpty(sourceFileName) || string.IsNullOrEmpty(destinationFileName))
            {
                Console.WriteLine("*** Error: You must specify the start date, end date, input filename, and destination filename \nin order for this application to function.\n\n");
                PrintHelp();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            else
            {
                // If we don't have a locations cache or it is outdated, then create it from NOAA data.
                // Otherwise, pull it in from the local cache.
                if (!File.Exists(locationsFileName)
                    || File.GetLastWriteTime(locationsFileName).CompareTo(DateTime.Now) > 30)
                {
                    // Our locations file doesn't exist or needs to be refreshed. 
                    Console.WriteLine("Building locations cache from NCDC...");

                    var offset = 0;
                    int locationCount;

                    do
                    {
                        // Download all of the locations.
                        var locationResponse = await CallApi(
                                                   new Uri(
                                                       $"{Configuration["BaseApiUrl"]}locations/?datasetid=GHCND&datatypeid=AWND&datatypeid=TMIN&datatypeid=TMAX&datatypeid=PRCP&locationcategoryid=CNTY&limit=1000&offset={offset}"),
                                                   revealer.GetApiKey()).ConfigureAwait(false);

                        locations.AddRange(JsonConvert.DeserializeObject<Models.Location.Root>(locationResponse).Results);

                        offset += 1000;
                        locationCount = locations.Count;
                    }
                    while (locationCount % 1000 == 0);

                    Console.WriteLine("Finished building location cache.");

                    // Save the locations to a CSV file.
                    var csv = new StringBuilder();
                    var allLines = (from location in locations
                                    select new object[]
                                               {
                                               location.Id, location.Name, location.DataCoverage, location.MaxDate,
                                               location.MinDate
                                               }).ToList();

                    allLines.ForEach(line => { csv.AppendLine(string.Join("|", line)); });

                    if (!Directory.Exists(dataPath))
                    {
                        Directory.CreateDirectory(dataPath);
                        Console.WriteLine("Created data directory.");
                    }

                    await File.WriteAllTextAsync(locationsFileName, csv.ToString()).ConfigureAwait(false);
                    Console.WriteLine($"Updated {locationsFileName}.");
                }
                else
                {
                    Console.WriteLine("Loading locations from cache.");

                    locations = csvParserLocations.ReadFromFile(locationsFileName, Encoding.UTF8).Select(x => x.Result)
                        .ToList();

                    Console.WriteLine("Finished loading locations from cache.");
                }

                // Get the list of counties that we are interested in.
                var desiredCounties = csvParserCounty.ReadFromFile(sourceFileName, Encoding.UTF8);

                var csvOutput = new StringBuilder();

                csvOutput.AppendLine(string.Join("|", "County", "FIPS", "Date", "TAVG", "TMAX", "TMIN", "PRCP", "AWND"));

                // Get the data for each county that we are interested in.
                foreach (var county in desiredCounties)
                {
                    // Get the NOAA location data.
                    var location =
                        locations.FirstOrDefault(x => x.Name == $"{county.Result.Name} County, {county.Result.State}");

                    if (location != null)
                    {
                        Console.WriteLine($"Processing {location.Name}");

                        var offset = 0;
                        var recordCount = -1;
                        var climateData = new List<Models.ClimateData.Result>();

                        do
                        {
                            string rawClimateData;

                            // Pull the weather data for the specified location.
                            try
                            {
                                rawClimateData = await CallApi(
                                                         new Uri(
                                                             $"{Configuration["BaseApiUrl"]}data?datasetid=GHCND&"
                                                             + "datatypeid=AWND&datatypeid=TMIN&datatypeid=TMAX&datatypeid=TAVG&datatypeid=PRCP&"
                                                             + $"locationid={location.Id}&"
                                                             + $"startdate={startDate}&"
                                                             + $"enddate={endDate}&" + $"units={unitOfMeasure}&"
                                                             + $"offset={offset}&" + "limit=1000&" + "includemetadata=false"),
                                                         revealer.GetApiKey()).ConfigureAwait(true);


                            }
                            catch (HttpRequestException e)
                            {
                                // Connection to host was lost. Wait 30 seconds and try again. 
                                Console.WriteLine("*** Error: Connection to host has been lost. Trying again...");
                                System.Threading.Thread.Sleep(30000);

                                rawClimateData = await CallApi(
                                                         new Uri(
                                                             $"{Configuration["BaseApiUrl"]}data?datasetid=GHCND&"
                                                             + "datatypeid=AWND&datatypeid=TMIN&datatypeid=TMAX&datatypeid=TAVG&datatypeid=PRCP&"
                                                             + $"locationid={location.Id}&"
                                                             + $"startdate={startDate}&"
                                                             + $"enddate={endDate}&" + $"units={unitOfMeasure}&"
                                                             + $"offset={offset}&" + "limit=1000&" + "includemetadata=false"),
                                                         revealer.GetApiKey()).ConfigureAwait(true);
                            }

                            if (rawClimateData != null && rawClimateData != "{}")
                            {
                                climateData.AddRange(
                                    JsonConvert.DeserializeObject<Models.ClimateData.Root>(rawClimateData).Results);

                                offset += 1000;
                                recordCount = locations.Count;
                            }
                            else
                            {
                                Console.WriteLine($"*** Notice: {location.Name} returned no data.");
                            }

                        }

                        while (recordCount % 1000 == 0);

                        // Transform climate data to desired format..
                        var fips = location.Id;

                        foreach (var date in climateData.Select(x => x.Date).Distinct())
                        {
                            var wind = "-";
                            var precipitation = "-";
                            var averageTemperature = "-";
                            var maximumTemperature = "-";
                            var minimumTemperature = "-";

                            if (climateData.Any(x => x.DataType == "AWND" && x.Date == date))
                            {
                                wind = climateData.Where(x => x.DataType == "AWND" && x.Date == date).Average(x => x.Value)
                                    .ToString(CultureInfo.InvariantCulture);
                            }

                            if (climateData.Any(x => x.DataType == "PRCP" && x.Date == date))
                            {
                                precipitation = climateData.Where(x => x.DataType == "PRCP" && x.Date == date)
                                    .Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
                            }

                            if (climateData.Any(x => x.DataType == "TAVG" && x.Date == date))
                            {
                                averageTemperature = climateData.Where(x => x.DataType == "TAVG" && x.Date == date)
                                    .Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
                            }

                            if (climateData.Any(x => x.DataType == "TMAX" && x.Date == date))
                            {
                                maximumTemperature = climateData.Where(x => x.DataType == "TMAX" && x.Date == date)
                                    .Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
                            }

                            if (climateData.Any(x => x.DataType == "TMIN" && x.Date == date))
                            {
                                minimumTemperature = climateData.Where(x => x.DataType == "TMIN" && x.Date == date)
                                    .Average(x => x.Value).ToString(CultureInfo.InvariantCulture);
                            }

                            if (averageTemperature == "-" && minimumTemperature != "-" && maximumTemperature != "-")
                            {
                                averageTemperature = ((Convert.ToDecimal(maximumTemperature) + Convert.ToDecimal(minimumTemperature)) / 2)
                                    .ToString(CultureInfo.InvariantCulture);
                            }

                            // Append the line for the current date
                            csvOutput.AppendLine(
                                string.Join(
                                    "|",
                                    $"{county.Result.Name}, {county.Result.State}",
                                    fips,
                                    date.ToString(dateFormat),
                                    averageTemperature,
                                    maximumTemperature,
                                    minimumTemperature,
                                    precipitation,
                                    wind));
                        }

                        // Write all of the data for the location to the destination CSV file.
                        await File.AppendAllTextAsync(destinationFileName, csvOutput.ToString(), Encoding.UTF8)
                            .ConfigureAwait(true);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"*** ERROR: Desired county was not found in the NCDC API: {county.Result.Name} County, {county.Result.State}.");
                    }

                    csvOutput = new StringBuilder();
                }

                Console.WriteLine("Processing completed!");
                Console.WriteLine($"Your destination file is located here: {destinationFileName}.\n");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        #region HELPERS
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="requestUri">
        /// The full URL to the API action with the desired data.
        /// </param>
        /// <param name="apiKey">Your API key.</param>
        /// <returns>
        /// The <see cref="string"/> with the data from the NCDC.
        /// </returns>
        private static async Task<string> CallApi(Uri requestUri, string apiKey)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("token", apiKey);

            using var response = await httpClient.GetAsync(requestUri).ConfigureAwait(true);

            response.EnsureSuccessStatusCode();

            return response.StatusCode switch
            {
                HttpStatusCode.OK => await response.Content.ReadAsStringAsync().ConfigureAwait(true),
                HttpStatusCode.BadRequest => throw new HttpRequestException(
                                                 "Error reading locations.",
                                                 new HttpRequestException(
                                                     await response.Content.ReadAsStringAsync()
                                                         .ConfigureAwait(true))),
                _ => null
            };
        }

        /// <summary>
        /// Displays documentation on the program to the console. 
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Retrieves climate data from NOAA and saves them in CSV format.\n");
            Console.WriteLine(
                $"{AppDomain.CurrentDomain.FriendlyName} YYYY-MM-DD YYYY-MM-DD /source [drive:][path][filename].csv \n"
                + " /destination [drive:][path][filename].csv [/dateFormat YYYYMMDD] [/metric || /standard] \n\n");
            Console.WriteLine(
                $"MM/DD/YYYY\t\t The date format for the start and end dates expressed in the following format: {DateTime.Now.ToString("d", new CultureInfo("en-US"))}");
            Console.WriteLine(
                "/source\t\t\t The location of the the CSV file containing the counties and states that you are interested in.");
            Console.WriteLine(
                "/destination\t\t The location that that you want your results saved to in CSV format.");
            Console.WriteLine(
                "/dateFormat\t\t Overrides the default date format used in the output file.");
            Console.WriteLine(
                "/metric\t\t Retrieve results using the metric system of measurement.");
            Console.WriteLine(
                "/standard\t\t Retrieve results using the standard system of measurement.");
            // Console.WriteLine(
            // "/attributes\t\t The comma separated list of field names that you want returned from NOAA.");
            Console.WriteLine("/?\t\t\t This help file.");
        }
        #endregion
    }
}
