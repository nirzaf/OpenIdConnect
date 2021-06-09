# Types of Software Testing

# Unit Test

-   Should only test 1 function at a time
-   All external dependencies should be mocked
-   Unit tests are test of (small) "units of code"
-   The customer of most “units of codes” are other programmers.
-   Part of the reason for having a unit test is to provide an example of how to call the code.
-   examples:

```csharp
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
```

# Integration Test

-   https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0
-   Integration tests ensure that an app's components function correctly at a level that includes the app's supporting infrastructure, such as the database, file system, and network. ASP.NET Core supports integration tests using a unit test framework with a test web host and an in-memory test server.
-   using `WebApplicationFactory` from `Microsoft.AspNetCore.Mvc.Testing`, you can have a integration test like this

```csharp
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
```

# BDD Test (Specflow)

-   mimick the business/domain requirements of the project
-   focus on user stories (e.g. `As a user, I want ..., In order to...`)
-   example

```specflow
Feature: Add

Scenario: Add two numbers
	When i add 2 and 3
	Then the result should be 5

Scenario: Add two zeros
	When i add 0 and 0
	Then the result should be 0

Scenario: Add one negative and one positive
	When i add -1 and 3
	Then the result should be 2

```

-   the above can be parsed by using `SpecFlow.Nunit` package as follow

```csharp
[When("i add (.*) and (.*)")]
public void WhenTheTwoNumbersAreAdded(int number1, int number2)
{
    num1 = number1;
    num2 = number2;
}

[Then("the result should be (.*)")]
public void ThenTheResultShouldBe(int result)
{
    Assert.AreEqual((num1 + num2), result);
}
```

# Consumer Contract Test (PACT)

-   focus on testing interaction points between consumers (Angular, MVC, microservice) and providers (API, Microservice)
-   consumer can write unit test (e.g. using `Jest`) against provided mocked http server

```js
import { pactWith } from "jest-pact";
import { Matchers } from "@pact-foundation/pact";
import { WeatherAPI } from "./weatherapi.service";
const path = require("path");

pactWith(
	{
		consumer: "react-client",
		provider: "WeatherAPI",
		cors: true,
		dir: path.resolve(process.cwd(), "../pacts"),
	},
	(provider) => {
		let client;

		beforeEach(() => {
			client = new WeatherAPI(provider.mockService.baseUrl);
		});

		describe("weather endpoint", () => {
			test("should be able to get WeatherForcast", async () => {
				const interaction = {
					state: "weather API",
					uponReceiving: "A request for weather API",
					withRequest: {
						method: "GET",
						path: "/weatherforecast",
						headers: {
							Authorization: "Bearer token",
						},
					},
					willRespondWith: {
						headers: {
							"Content-Type": "application/json; charset=utf-8",
						},
						body: Matchers.eachLike({
							temperatureC: 3,
							temperatureF: 30,
							summary: "hot",
						}),
						status: 200,
					},
				};
				await provider.addInteraction(interaction);

				await client.getWeatherForecast("token").then((result) => {
					expect(result.length).toEqual(1);
					expect(result[0].temperatureC).toEqual(3);
					expect(result[0].temperatureF).toEqual(30);
					expect(result[0].summary).toEqual("hot");
				});
			});
		});
	}
);
```

-   the above then generate a contract file which can be accessed by providers (API)
-   then providers (WebApi) can write its unit tests and make sure it can conform to the generated contracts (using `XUnit` and `Pact.NET`)

```csharp
[Fact]
public void EnsureWeatherAPIHonorsPactWithConsumer1()
{
    new PactVerifier(_pactVerifierConfig)
        .ProviderState($"{ProviderUri}/provider-states")
        .ServiceProvider("WeatherAPI", ProviderUri)
        .HonoursPactWith("react-client")
        .PactUri($"..\\..\\..\\..\\..\\pacts\\react-client-weatherapi.json")
        .Verify();
}
```
