using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;

namespace weatherapi.integration.tests
{
    public class WeatherApiIntegrationTests
    : IClassFixture<WebApplicationFactory<weatherapi.Startup>>
    {
        private readonly WebApplicationFactory<weatherapi.Startup> _factory;

        public WeatherApiIntegrationTests(WebApplicationFactory<weatherapi.Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/", "NotFound")]
        [InlineData("/weatherforecast", "Unauthorized")]
        [InlineData("/swagger/v1/swagger.json", "OK")]
        public async Task Get_EndpointsWithCorrectResponseCodes(string url, string responseCode)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(responseCode, response.StatusCode.ToString());
        }
    }
}
