using GoogleMaps.LocationServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Pages
{
    public class WeatherModel(IConfiguration configuration) : PageModel
    {
        private readonly IConfiguration _config = configuration;
        public WeatherParse? Weather { get; private set; }
        public WeatherData[]? WeatherResults { get; private set; }
        [BindProperty]
        public string City { get; set; } = "";

        private Dictionary<int, string> _weatherCode;
        private double? _latitude = null;
        private double? _longitude = null;
        private const string BaseApiUrl = "https://api.open-meteo.com/v1/";


        private void GetLocation()
        {
            if (!string.IsNullOrEmpty(City))
            {
                string? googleApiKey = _config.GetSection("Google:ApiKey")?.Value;
                try
                {
                    var locationService = new GoogleLocationService(googleApiKey);
                    var point = locationService.GetLatLongFromAddress(City);
                    if (point is not null)
                    {
                        _latitude = point.Latitude;
                        _longitude = point.Longitude;
                    }
                    else
                    {
                        _latitude = null;
                        _longitude = null;
                        ViewData["ErrorMessage"] = "Could not retrieve weather data. Please check the city name.";
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket exception: {ex.Message}");
                }
            }
        }

        public IActionResult OnPost()
        {
            GetLocation();

            if (_latitude != null && _longitude != null)
            {
                string url = CreateApiEndPoint();
                try
                {
                    using HttpClient client = new();
                    client.BaseAddress = new Uri(BaseApiUrl);
                    var result = client.GetAsync(url).Result;

                    if (result.IsSuccessStatusCode)
                    {
                        var stringResult = result.Content.ReadAsStringAsync().Result;
                        Weather = JsonSerializer.Deserialize<WeatherParse>(stringResult);
                        ProcessWeatherData();
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "Cannot load data from weather API.";
                    Console.WriteLine(ex.Message + "\nCant load data from weather api");
                    return Page();
                }

                return Page();
            }
            return Page();
        }

        private void ProcessWeatherData()
        {
            int range = Weather.Daily.Time.Length;
            WeatherResults = Enumerable.Range(0, range)
                           .Select(_ => new WeatherData())
                           .ToArray();

            if (Weather.Current is not null)
            {
                WeatherResults[0].Temperature = Weather.Current.Temperature;
                WeatherResults[0].Humidity = Weather.Current.Humidity;
                WeatherResults[0].WeatherCondition = GetWeatherCode(Weather.Current.Code);
            }

            for (int i = 1; i < range; i++)
            {
                WeatherResults[i].Time = Weather.Daily.Time[i];
                WeatherResults[i].TemperatureMax = Weather.Daily.TemperatureMax[i];
                WeatherResults[i].TemperatureMin = Weather.Daily.TemperatureMin[i];
                WeatherResults[i].WeatherCondition = _weatherCode[Weather.Daily.Code[i]];
            }
        }

        private string CreateApiEndPoint()
        {
            StringBuilder builder = new();
            builder.Append("forecast?latitude=" + _latitude + "&longitude=" + _longitude + "&current=temperature_2m,relative_humidity_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min");
            return builder.ToString();
        }
        private string GetWeatherCode(int number)
        {
            _weatherCode = new()
            {
                      { 0, "Clear sky" },
                      { 1, "Mainly clear" },
                      { 2, "Partly cloudy" },
                      { 3, "Overcast" },
                      { 45, "Fog" },
                      { 48, "Depositing rime fog" },
                      { 51, "Drizzle: Light intensity" },
                      { 53, "Drizzle: Moderate intensity" },
                      { 55, "Drizzle: Dense intensity" },
                      { 56, "Freezing Drizzle: Light intensity" },
                      { 57, "Freezing Drizzle: Dense intensity" },
                      { 61, "Rain: Slight intensity" },
                      { 63, "Rain: Moderate intensity" },
                      { 65, "Rain: Heavy intensity" },
                      { 66, "Freezing Rain: Light intensity" },
                      { 67, "Freezing Rain: Heavy intensity" },
                      { 71, "Snow fall: Slight intensity" },
                      { 73, "Snow fall: Moderate intensity" },
                      { 75, "Snow fall: Heavy intensity" },
                      { 77, "Snow grains" },
                      { 80, "Rain showers: Slight intensity" },
                      { 81, "Rain showers: Moderate intensity" },
                      { 82, "Rain showers: Violent intensity" },
                      { 85, "Snow showers: Slight intensity" },
                      { 86, "Snow showers: Heavy intensity" },
                      { 95, "Thunderstorm: Slight or moderate" },
                      { 96, "Thunderstorm with slight hail" },
                      { 99, "Thunderstorm with heavy hail" }
            };

            return _weatherCode[number];
        }

        public class WeatherData
        {
            public DateTime Time { get; set; }

            [JsonPropertyName("temperature_2m")]
            public float Temperature { get; set; }
            public float TemperatureMax { get; set; }
            public float TemperatureMin { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public float Humidity { get; set; }

            [JsonPropertyName("weather_code")]
            public byte Code { get; set; }

            public string? WeatherCondition { get; set; }
        }
        public class DailyWeather
        {
            [JsonPropertyName("time")]
            public DateTime[]? Time { get; set; }

            [JsonPropertyName("temperature_2m_max")]
            public float[]? TemperatureMax { get; set; }

            [JsonPropertyName("temperature_2m_min")]
            public float[]? TemperatureMin { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public float[]? Humidity { get; set; }
            [JsonPropertyName("weather_code")]
            public short[]? Code { get; set; }

        }

        public class WeatherParse
        {
            [JsonPropertyName("current")]
            public WeatherData? Current { get; set; }

            [JsonPropertyName("daily")]
            public DailyWeather? Daily { get; set; }

        }

    }
}
