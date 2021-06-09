require("dotenv").config();
var pact = require("@pact-foundation/pact-node");

var opts = {
	pactFilesOrDirs: [`../pacts`],
	pactBroker: process.env.PACT_SERVER,
	// this can be the fix version (e.g. release version)
	// or this could be git commit SHA
	consumerVersion: "1.0.1.103",
	pactBrokerToken: process.env.PACT_SERVER_TOKEN,
	// which environemtn is this?
	tags: ["test", "prod"],
	// verbose: true
};

pact.publishPacts(opts).then(function () {
	// do something
	console.log("pact published");
});
