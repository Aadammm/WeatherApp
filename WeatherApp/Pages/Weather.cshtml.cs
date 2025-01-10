using GoogleMaps.LocationServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Pages
{
    public class WeatherModel : PageModel
    {
        readonly IConfiguration _config;
        public WeatherData Weather;
        [BindProperty]
        public string City { get; set; }
        public string Response { get; set; } = "No data";

        private double? Latitude = null;
        private double? Longitude = null;
        private const string BaseApiUrl = "https://api.open-meteo.com/v1/";

        public WeatherModel(IConfiguration configuration)
        {
            _config = configuration;
        }


        void GetLocation()
        {
            if (!string.IsNullOrEmpty(City))
            {
                string? googleApiKey = _config.GetSection("Google:ApiKey")?.Value;

                var locationService = new GoogleLocationService(googleApiKey);
                var point = locationService.GetLatLongFromAddress(City);
                if (point != null)
                {
                    Latitude = point.Latitude;
                    Longitude = point.Longitude;
                }
                else
                {
                    Latitude = null;
                    Longitude = null;
                }
            }
        }

        public IActionResult OnPost()
        {
            GetLocation();


            if (Latitude != null && Longitude != null)
            {
                string url = CreateEndPoint();
                using HttpClient client = new();
                client.BaseAddress = new Uri(BaseApiUrl);
                var result = client.GetAsync(url).Result;

                if (result.IsSuccessStatusCode)
                {
                    Response = result.Content.ReadAsStringAsync().Result;
                    Weather = JsonSerializer.Deserialize<WeatherData>(Response);

                }
            }

            return Page();
        }

        private string CreateEndPoint()
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

        //public void OnGet()
        //{

        //}

        //public async Task<IActionResult> OnPostAsync()
        //{
        //    if (string.IsNullOrWhiteSpace(City))
        //    {
        //        ErrorMessage = "Please enter a city.";
        //        return Page();
        //    }

        //    try
        //    {
        //        string apiKey = "YOUR_API_KEY"; // Nahraï svojím API k¾úèom
        //        string url = $"https://api.openweathermap.org/data/2.5/weather?q={City}&units=metric&appid={apiKey}";

        //        var response = await _httpClient.GetStringAsync(url);
        //        var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(response);

        //        WeatherDataa = new WeatherData
        //        {
        //            Temperature = weatherResponse.Main.Temp,
        //            Description = weatherResponse.Weather[0].Description
        //        };
        //    }
        //    catch (Exception)
        //    {
        //        ErrorMessage = "Could not retrieve weather data. Please check the city name.";
        //    }

        //    return Page();
        //}
        //public class WeatherApiResponse
        //{
        //    public WeatherMain Main { get; set; }
        //    public WeatherDescription[] Weather { get; set; }
        //}
        //public class WeatherMain
        //{
        //    public double Temp { get; set; }
        //}

        //public class WeatherDescription
        //{
        //    public string Description { get; set; }
        //}

        //public class WeatherData
        //{
        //    public double Temperature { get; set; }
        //    public string Description { get; set; }
        //}
    }
}
