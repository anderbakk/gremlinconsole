using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;

namespace gremlinconsole
{
    public class Program
    {
        private static ConsoleColor _normalBackgroundColor;
        private static ConsoleColor _normalForegroundColor;

        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                Culture = CultureInfo.GetCultureInfo("en-US"),
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatParseHandling = FloatParseHandling.Decimal,
            };
            _normalBackgroundColor = Console.BackgroundColor;
            _normalForegroundColor = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(@"
                                          _ _                                 _            
                  __ _ _ __ ___ _ __ ___ | (_)_ __   ___ ___  _ __  ___  ___ | | ___       
                 / _` | '__/ _ \ '_ ` _ \| | | '_ \ / __/ _ \| '_ \/ __|/ _ \| |/ _ \      
                | (_| | | |  __/ | | | | | | | | | | (_| (_) | | | \__ \ (_) | |  __/      
                 \__, |_|  \___|_| |_| |_|_|_|_| |_|\___\___/|_| |_|___/\___/|_|\___|      
                 |___/                                                                     
");
                                                                                                                                                    
            Console.BackgroundColor = _normalBackgroundColor;
            Console.ForegroundColor = _normalForegroundColor;

            var p = new Program();
            
            RunAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
        
        private static async Task RunAsync()
        {
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));

            var endpoint = ConfigurationManager.AppSettings["Endpoint"];
            var authKey = ConfigurationManager.AppSettings["AuthKey"];
            var database = ConfigurationManager.AppSettings["Database"];
            var collection = ConfigurationManager.AppSettings["GraphCollection"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(database) ||
                string.IsNullOrEmpty(collection))
            {
                Console.WriteLine("Configuration is missing - please enter Endpoint, AuthKey, Database and GraphCollection. All values can be found in the Azure Portal");
                return;
            }
            
            var client = new DocumentClient(
                new Uri(endpoint),
                authKey,
                new ConnectionPolicy {ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp});
            
            DocumentCollection graph = await client.ReadDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(database, collection));

            Console.WriteLine($"Connected to {endpoint}");
            Console.WriteLine($"Database: {database} Collection : {collection}");
            Console.WriteLine();
            Console.WriteLine("Waiting for query...");
            var stopWatch = new Stopwatch();
            while (true)
            {
                stopWatch.Stop();
                stopWatch.Reset();
                
                var query = Console.ReadLine();

                if (string.IsNullOrEmpty(query))
                    continue;
                stopWatch.Start();

                var gremlinQuery = client.CreateGremlinQuery(graph, query);

                var resultIndex = 0;
                while (gremlinQuery.HasMoreResults)
                {
                    try
                    {
                        var feedResponse = await gremlinQuery.ExecuteNextAsync();
                        foreach (var value in feedResponse)
                        {
                            resultIndex++;
                            Console.WriteLine($"{resultIndex} -> {JsonConvert.SerializeObject(value)}");
                        }

                        stopWatch.Stop();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Time:{stopWatch.ElapsedMilliseconds} ms RequestCharge:{feedResponse.RequestCharge}");
                        Console.ForegroundColor = _normalForegroundColor;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        break;
                    }
                }
            }
        }
    }
}
