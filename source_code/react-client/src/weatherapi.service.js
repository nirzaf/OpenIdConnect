export class WeatherAPI {
	baseUrl;
	constructor(baseUrl) {
		this.baseUrl = baseUrl;
	}

	getWeatherForecast(token) {
		return fetch(`${this.baseUrl}/weatherforecast`, {
			headers: {
				Authorization: "Bearer " + token,
			},
		}).then((resp) => resp.json());
	}
}
