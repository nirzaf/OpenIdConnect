import React, { useState, useEffect } from "react";
import { BrowserRouter, Switch, Route } from "react-router-dom";
import { UserManager } from "oidc-client";
import { WeatherAPI } from "./api";

// 1. add 2 routes
function App() {
	return (
		<BrowserRouter>
			<Switch>
				<Route path="/signin-oidc" component={Callback} />
				<Route path="/" component={HomePage} />
			</Switch>
		</BrowserRouter>
	);
}

// 2. add IDS server config and Home component

function HomePage() {
	const [state, setState] = useState(null);
	var mgr = new UserManager({
		authority: "https://localhost:5001",
		client_id: "interactive.public",
		redirect_uri: "http://localhost:3000/signin-oidc",
		post_logout_redirect_uri: "http://localhost:3000",
		response_type: "code",
		scope: "openid profile weatherapi.read",
	});

	useEffect(() => {
		mgr.getUser().then((user) => {
			if (user) {
				var api = new WeatherAPI("https://localhost:5002");
				api.getWeatherForecast(user.access_token).then((data) =>
					setState({ user, data })
				);
			}
		});
	}, []);

	return (
		<div>
			{state ? (
				<>
					<h3>Welcome {state?.user?.profile?.sub}</h3>
					<pre>{JSON.stringify(state?.data, null, 2)}</pre>
					<button onClick={() => mgr.signoutRedirect()}>
						Log out
					</button>
				</>
			) : (
				<>
					<h3>React Weather App</h3>
					<button onClick={() => mgr.signinRedirect()}>Login</button>
				</>
			)}
		</div>
	);
}

// 3. callback component
function Callback() {
	useEffect(() => {
		var mgr = new UserManager({
			response_mode: "query",
		});

		mgr.signinRedirectCallback().then(() => (window.location.href = "/"));
	}, []);

	return <p>Loading...</p>;
}

export default App;
