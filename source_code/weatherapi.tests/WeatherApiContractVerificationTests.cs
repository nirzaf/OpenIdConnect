using System.Collections.Generic;
using API.Tests.PactSetup;
using Microsoft.Extensions.Configuration;
using PactNet;
using Xunit;
using Xunit.Abstractions;

namespace API.Tests
{
    public class WeatherApiContractVerificationTests : ContractTestBase
    {
        public WeatherApiContractVerificationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void EnsureWeatherAPIHonorsPactWithConsumer1()
        {

            new PactVerifier(_pactVerifierConfig)
                .ProviderState($"{ProviderUri}/provider-states")
                // this is the name os the provider, we can't have space for some reason
                .ServiceProvider("WeatherAPI", ProviderUri)
                .HonoursPactWith("react-client")
                // point to the contract file
                // not this is coming from bin folder of .net 

                // .PactUri($"..\\..\\..\\..\\pacts\\react-client-weatherapi.json")
                // .PactUri($"{PactFlowServerUrl}/pacts/provider/WeatherAPI/consumer/react-client/latest", PactFlowServer)

                // or we can pick all from broker
                // .PactBroker(PactFlowServerUrl, uriOptions: PactFlowServer, enablePending: true,
                //     consumerVersionTags: new List<string> { "test","prod" },
                //     providerVersionTags: new List<string> { "test","prod" })
                .PactBroker(PactFlowServerUrl, uriOptions: PactFlowServer, enablePending: true,
                    providerVersionTags: new List<string> { "test", "prod" })

                // .PactBroker(PactFlowServerUrl, uriOptions: PactFlowServer)
                .Verify();
        }
    }
}