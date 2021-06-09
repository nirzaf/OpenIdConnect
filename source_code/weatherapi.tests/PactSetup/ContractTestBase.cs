using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PactNet;
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace API.Tests.PactSetup
{
    public abstract class ContractTestBase : IDisposable
    {
        protected const string ProviderUri = "https://127.0.0.1:9310";
        protected readonly PactVerifierConfig _pactVerifierConfig;
        private bool _disposedValue;
        private readonly ITestOutputHelper _output;
        private readonly IWebHost _webHost;
        protected readonly PactUriOptions PactFlowServer;
        protected readonly IConfiguration Configuration;
        protected readonly string PactFlowServerUrl;

        protected ContractTestBase(ITestOutputHelper output)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Pact.json")
                .Build();

            this.Configuration = config;
            this.PactFlowServerUrl = config.GetValue<string>("PactServer");
            this.PactFlowServer = new PactUriOptions(config.GetValue<string>("PactServerToken"));


            _output = output;
            _webHost = WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    configuration.Sources.Clear();
                    // overwrite with his config
                    configuration.AddJsonFile("appsettings.Pact.json", false);
                })
                .UseStartup<TestStartup>()
                .UseKestrel()
                .UseUrls(ProviderUri)
                .Build();

            _webHost.Start();

            _pactVerifierConfig = new PactVerifierConfig
            {
                //NOTE: We default to using a ConsoleOutput, however xUnit 2 does not capture
                //the console output, so a custom outputter is required.
                Outputters = new List<IOutput>
                    {
                        new XUnitOutput(_output)
                    },
                //This allows the user to set request headers that will be sent with every request the verifier

                CustomHeaders = new Dictionary<string, string>
                {
                    // you can generate long live token from here http://jwtbuilder.jamiekurtz.com
                    // or you can generate one yourself, make sure the expiry is not to far in the future
                    {"Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJwYWN0dGVzdCIsImlhdCI6MTYyMjk1ODIxMSwiZXhwIjoxNzQ5MTg4NjExLCJhdWQiOiJ3ZWF0aGVyYXBpIiwic3ViIjoidGVzdCJ9.YdjgDJClh1dsDRFPKOpdDF_C6hXJSV_AEYONVopjnhA"}
                },

                //sends to the provider
                Verbose = true, //Output verbose verification logs to the test output
                // the version of this provider, can come from GIT SHA
                ProviderVersion = "1.0.4",
                PublishVerificationResults = true
            };
        }

        private void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                _webHost.StopAsync().GetAwaiter().GetResult();
                _webHost.Dispose();
            }

            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}