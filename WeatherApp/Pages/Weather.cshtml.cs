using GoogleMaps.LocationServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Pages
{
    public class WeatherModel(IConfiguration configuration) : PageModel
    {
        readonly IConfiguration _config = configuration;
        public WeatherData Weather { get; private set; }
        [BindProperty]
        public string City { get; set; } = "";

        private double? Latitude = null;
        private double? Longitude = null;
        private const string BaseApiUrl = "https://api.open-meteo.com/v1/";

        void GetLocation()
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
                        Latitude = point.Latitude;
                        Longitude = point.Longitude;
                    }
                    else
                    {
                        Latitude = null;
                        Longitude = null;
                        ViewData["ErrorMessage"] = "Could not retrieve weather data. Please check the city name.";
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public IActionResult OnPost()
        {
            GetLocation();

            if (Latitude != null && Longitude != null)
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
                        Weather = JsonSerializer.Deserialize<WeatherData>(stringResult);
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nCant load data from weather api");
                    return Page();
                }
            }
            return Page();
        }
        private string CreateApiEndPoint()
        {
            StringBuilder builder = new();
            builder.Append("forecast?latitude=" + Latitude + "&longitude=" + Longitude + "&current=temperature_2m,relative_humidity_2m");
            return builder.ToString();
        }
        public class CurrentWeather
        {
            [JsonPropertyName("temperature_2m")]
            public double Temperature { get; set; }
            [JsonPropertyName("relative_humidity_2m")]
            public double Humidity { get; set; }
        }

        public class WeatherData
        {
            [JsonPropertyName("current")]
            public CurrentWeather Current { get; set; }
        }
    }
}
