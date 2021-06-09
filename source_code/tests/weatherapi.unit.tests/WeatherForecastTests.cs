using System;
using Xunit;
using weatherapi;

namespace weatherapi.unit.tests
{
    public class WeatherForecastTests
    {
        [Theory]
        [InlineData(0, 32)]
        [InlineData(2, 35)]
        [InlineData(100, 211)]
        [InlineData(-1, 31)]
        public void CanReturnCorrectFahrenheit(int inputInC, int expectedInF)
        {
            // Arrange
            var temp = new WeatherForecast();
            // Act
            temp.TemperatureC = inputInC;
            // Assert
            Assert.Equal(expectedInF, temp.TemperatureF);
        }
    }
}
