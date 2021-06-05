import { WeatherAPI } from "./weatherapi.service";
import * as Pact from "@pact-foundation/pact";

describe("HeroService API", () => {
	const weatherApi = new WeatherAPI(`http://localhost:${global.port}`);

	describe("getWeatherForecast()", () => {
		beforeEach((done) => {
			// ...
		});

		it("sends a request according to contract", (done) => {
			// ...
		});
	});
});
