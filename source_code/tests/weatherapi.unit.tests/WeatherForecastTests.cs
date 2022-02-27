using Xunit;

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
            WeatherForecast temp = new()
            {
                // Act
                TemperatureC = inputInC
            };
            // Assert
            Assert.Equal(expectedInF, temp.TemperatureF);
        }
    }
}