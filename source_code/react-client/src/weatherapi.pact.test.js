import { pactWith } from "jest-pact";
import { Matchers } from "@pact-foundation/pact";
import { WeatherAPI } from "./weatherapi.service";
const path = require("path");

pactWith(
	{
		consumer: "react-client",
		provider: "WeatherAPI",
		// make sure this is set to true otherwise it will failed the tests
		cors: true,
		// point this to a location which can be accessed by the backend api
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
						// https://github.com/pact-foundation/pact-js#match-based-on-type
						// we can match exact or base on shape
						body: Matchers.eachLike({
							temperatureC: 3,
							temperatureF: 30,
							summary: "hot",
							// area: "CA",
							// country: "CA",
							// state: "CA",
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
