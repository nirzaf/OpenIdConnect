import { pactWith } from 'jest-pact';
import { WeatherAPI } from './weatherapi.service';

pactWith({ consumer: 'react-client', provider: 'WeatherAPI' }, provider => {
	let client = new WeatherAPI(provider.mockService.baseUrl);

	beforeEach(() => {
		client = api(provider.mockService.baseUrl)
	});

	describe('weather endpoint', () => {
		test("should be able to access /helloworld when authenticated", async () => {
			const apiPath = "/helloworld";
			const interaction = {
				state: "weather API",
				uponReceiving: "A request for weather API",
				withRequest: {
					method: "GET",
					path: "/weatherforecast"
				},
				willRespondWith: {
					headers: {
						"Content-Type": "application/json"
					},
					body: [{
						temperatureC: 3,
						temperatureF: 30,
						message: 'sample',
					}],
					status: 200
				}
			};

			await provider.addInteraction(interaction);

			await client.getWeatherForecast()
				.expect(200).then(result => {
					expect(result.length).toEqual(1);
				});
		});
	});
});
