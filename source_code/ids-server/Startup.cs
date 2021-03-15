using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;

namespace idsserver
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddInMemoryApiScopes(new List<ApiScope> {
                    new ApiScope("weatherapi.read", "Read Access to API"),
                    new ApiScope("weatherapi.write", "Write Access to API")
                })
                .AddInMemoryApiResources(new List<ApiResource>() {
                    new ApiResource("weatherapi") {
                        Scopes = { "weatherapi.read" },
                    }
                })
                .AddInMemoryClients(new List<Client> {
                    new Client
                    {
                        ClientId = "m2m.client",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("SuperSecretPassword".Sha256())},
                        AllowedScopes = { "weatherapi.read" }
                    },
                    new Client
                    {
                        ClientId = "interactive",
                        ClientSecrets = { new Secret("SuperSecretPassword2".Sha256()) },

                        AllowedGrantTypes = GrantTypes.Code,

                        RedirectUris = { "http://localhost:3000/signin-oidc" },

                        AllowedScopes = { "openid", "profile", "weatherapi.read" }
                    },
                })
                .AddInMemoryIdentityResources(new List<IdentityResource>() {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile()
                })
                .AddTestUsers(new List<TestUser>() {
                    new TestUser
                        {
                            SubjectId = "alice_123",
                            Username = "alice",
                            Password = "alice"
                        }
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseIdentityServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
