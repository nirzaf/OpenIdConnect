var pact = require("@pact-foundation/pact-node");
var opts = {
	// https://github.com/pact-foundation/pact-js-core#pact-broker-deployment-check
	pacticipants: [
		{
			name: "react-client",
			// which version we want to check
			// this could be the version that we just published
			// latest: "test",
			latest: "test",
		},
		{
			name: "WeatherAPI",
			// which version we want to check
			// this could be the version that we just published
			// latest: "test",
			latest: "prod",
		},
	],
	pactBroker: "https://ssw.pactflow.io",
	pactBrokerToken: "xRy0RdbaDE1oSvyxEfuT6Q",
	output: "table",
	// verbose: true,
};

pact.canDeploy(opts)
	.then(function (result) {
		// You can deploy this
		// If output is not specified or is json, result describes the result of the check.
		// If outout is 'table', it is the human readable string returned by the check
		// console.log(result)
	})
	.catch(function (error) {
		console.log(error);
		// You can't deploy this
		// if output is not specified, or is json, error will be an object describing
		// the result of the check (if the check failed),
		// if output is 'table', then the error will be a string describing the output from the binary,
		// In both cases, `error` will be an Error object if something went wrong during the check.
	});
