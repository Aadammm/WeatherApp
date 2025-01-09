using GoogleMaps.LocationServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace WeatherApp.Pages
{
    public class WeatherModel : PageModel
    {
        readonly IConfiguration _config;
        [BindProperty]
        public string City { get; set; }

        private double Latitude;
        private double Longitude;
        private const string weatherApiUrl = "https://api.open-meteo.com/v1/forecast?";
        public string rresponse = "x";
        private WeatherData weatherData;

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

                Latitude = point.Latitude;
                Longitude = point.Longitude;
            }
        }

        public IActionResult OnPost()
        {
            GetLocation();

            string url = CreateEndPoint();

            using HttpClient client = new();
            var response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                rresponse = response.Content.ReadAsStringAsync().Result;
            }

            Console.WriteLine(response.Content);

            return Page();
        }

        private string CreateEndPoint()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(weatherApiUrl).Append("latitude=" + Latitude + "&longitude=" + Longitude + "&current=temperature_2m");
            //return weatherApiUrl + "latitude=" + Latitude + "&longitude=" + Longitude + "&current=temperature_2m";
            return builder.ToString();
        }



        public class WeatherData
        {

        }
        //public WeatherData WeatherDataa { get; private set; }
        //public string ErrorMessage { get; private set; }

        public void OnGet()
        {

        }

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
