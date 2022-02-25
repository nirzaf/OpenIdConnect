using System;

namespace weatherapi
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string Summary { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
    }
}
