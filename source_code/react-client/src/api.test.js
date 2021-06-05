import { WeatherAPI } from "./api";
it("init API client", async () => {
  var api = new WeatherAPI("http://localhost:5003");
	expect(api).toBeDefined();
});
