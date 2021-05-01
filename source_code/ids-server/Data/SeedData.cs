using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace idsserver
{
    public class DataSeeder
    {
        public static void SeedIdentityServer(IServiceProvider serviceProvider)
        {
            Console.WriteLine("Seeding data for Identity server");

            var context = serviceProvider
                .GetRequiredService<ConfigurationDbContext>();

            var userMng = serviceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            DataSeeder.SeedData(context);

            DataSeeder.SeedTestUsers(userMng);
        }

        private static void SeedTestUsers(UserManager<IdentityUser> manager)
        {
            var alice = manager.FindByNameAsync("alice").Result;
            if (alice == null)
            {
                alice = new IdentityUser
                {
                    UserName = "alice",
                    Email = "alice@test.com",
                    EmailConfirmed = true
                };
                var result = manager.CreateAsync(alice, "Password1!").Result;

                if (result.Succeeded)
                {
                    result = manager.AddClaimsAsync(alice, new Claim[] {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.WebSite, "Website"),
                    }).Result;

                    Console.WriteLine("added alice user");
                }
            }
            else
            {
                Console.WriteLine("alice already created");
            }
        }
        private static void SeedData(ConfigurationDbContext context)
        {
            if (!context.Clients.Any())
            {
                var clients = new List<Client> {
                    new Client
                    {
                        ClientId = "m2m.client",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("SuperSecretPassword".Sha256()) },
                        AllowedScopes = { "weatherapi.read" }
                    },
                    new Client
                    {
                        ClientId = "interactive",

                        AllowedGrantTypes = GrantTypes.Code,
                        RequireClientSecret = false,
                        RequirePkce = true,

                        RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                        PostLogoutRedirectUris = { "http://localhost:3000" },

                        AllowedScopes = { "openid", "profile", "weatherapi.read" }
                    },
                };

                foreach (var client in clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
                Console.WriteLine($"Added {clients.Count()} clients");
            }
            else
            {
                Console.WriteLine("clients already added..");
            }

            if (!context.ApiResources.Any())
            {
                var apiResources = new List<ApiResource>() {
                    new ApiResource("weatherapi") {
                        Scopes = { "weatherapi.read" },
                    }
                };

                foreach (var apiRrc in apiResources)
                {
                    context.ApiResources.Add(apiRrc.ToEntity());
                }
                context.SaveChanges();
                Console.WriteLine($"Added {apiResources.Count()} api resources");
            }
            else
            {
                Console.WriteLine("api resources already added..");
            }


            if (!context.ApiScopes.Any())
            {
                var scopes = new List<ApiScope> {
                    new ApiScope("weatherapi.read", "Read Access to API"),
                    new ApiScope("weatherapi.write", "Write Access to API"),
                };

                foreach (var scope in scopes)
                {
                    context.ApiScopes.Add(scope.ToEntity());
                }
                context.SaveChanges();
                Console.WriteLine($"Added {scopes.Count()} api scopes");
            }
            else
            {
                Console.WriteLine("api scopes already added..");
            }

            if (!context.IdentityResources.Any())
            {
                var identityResources = new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email(),
                };

                foreach (var identity in identityResources)
                {
                    context.IdentityResources.Add(identity.ToEntity());
                }
                context.SaveChanges();
                Console.WriteLine($"Added {identityResources.Count()} identity Resources");
            }
            else
            {
                Console.WriteLine("api scopes already added..");
            }
        }
    }
}