require("dotenv").config();
var pact = require("@pact-foundation/pact-node");
var sha = require("child_process").execSync("git rev-parse HEAD").toString();
var opts = {
	pactFilesOrDirs: [`../pacts`],
	pactBroker: process.env.PACT_SERVER,
	// this can be the fix version (e.g. release version)
	// or this could be git commit SHA
	consumerVersion: sha,
	pactBrokerToken: process.env.PACT_SERVER_TOKEN,
	// which environemtn is this?
	tags: ["test", "prod"],
	// verbose: true
};

pact.publishPacts(opts).then(function () {
	// do something
	console.log("pact published version:" + sha);
});
