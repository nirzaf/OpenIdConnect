using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

namespace idsserver
{
    public static class DataSeeder
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public static async Task SeedIdentityServer(IServiceProvider serviceProvider)
        {
            WriteLine("Seeding data for Identity server");

            ConfigurationDbContext context = serviceProvider
                .GetRequiredService<ConfigurationDbContext>();

            UserManager<IdentityUser> userMng = serviceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            await SeedTestUsers(userMng);
            await SeedData(context);
        }

        private static async Task SeedTestUsers(UserManager<IdentityUser> manager)
        {
            IdentityUser basicUser = manager.FindByNameAsync("fazrin@gmail.com").Result;
            if (basicUser is null)
            {
                basicUser = new IdentityUser()
                {
                    UserName = "fazrin@gmail.com",
                    Email = "fazrin@gmail.com",
                    EmailConfirmed = true
                };
                IdentityResult result = manager.CreateAsync(basicUser, "123@Pa$$word!").Result;

                if (result.Succeeded)
                {
                    result = await manager.AddClaimsAsync(basicUser, new Claim[]
                    {
                        new(JwtClaimTypes.Name, "M.F.M Fazrin"),
                        new(JwtClaimTypes.GivenName, "Fazrin"),
                        new(JwtClaimTypes.FamilyName, "Farook"),
                        new(JwtClaimTypes.PhoneNumber, "+94772049123"),
                        new(JwtClaimTypes.WebSite, "https://nirzaf.github.io")
                    });

                    if (result.Succeeded)
                    {
                        WriteLine("Basic user added");
                    }
                }
            }
            else
            {
                WriteLine("Basic already created");
            }

            // bob need 2FA
            IdentityUser adminUser = await manager.FindByNameAsync("mfmfazrin1986@gmail.com");
            if (adminUser is null)
            {
                adminUser = new IdentityUser()
                {
                    UserName = "mfmfazrin1986@gmail.com",
                    Email = "mfmfazrin1986@gmail.com",
                    EmailConfirmed = true
                };
                IdentityResult result = await manager.CreateAsync(adminUser, "123@Pa$$word!");

                if (result.Succeeded)
                {
                    result = await manager.AddClaimsAsync(adminUser, new Claim[]
                    {
                        new(JwtClaimTypes.Name, "Super Admin"),
                        new(JwtClaimTypes.GivenName, "Admin"),
                        new(JwtClaimTypes.FamilyName, "Administrator"),
                        new(JwtClaimTypes.WebSite, "https://my-clay.com")
                    });

                    WriteLine("Admin user added");
                    IdentityResult raka = await manager.ResetAuthenticatorKeyAsync(adminUser);
                    string unformattedKey = await manager.GetAuthenticatorKeyAsync(adminUser);
                    string sharedKey = HelperClass.FormatKey(unformattedKey);
                    string email = await manager.GetEmailAsync(adminUser);
                    string authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);
                    raka = await manager.SetTwoFactorEnabledAsync(adminUser, true);
                    WriteLine("Enabled 2FA with Authenticator URL: " + authenticatorUri);
                }
            }
            else
            {
                string unformattedKey = await manager.GetAuthenticatorKeyAsync(adminUser);
                bool isEnabled = await manager.GetTwoFactorEnabledAsync(adminUser);
                string sharedKey = HelperClass.FormatKey(unformattedKey);
                string email = await manager.GetEmailAsync(adminUser);
                string authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);
                WriteLine("Admin already created");
                WriteLine($"2FA enabled = {isEnabled}, with Authenticator URL: " + authenticatorUri);
            }
        }

        private static async Task SeedData(ConfigurationDbContext context)
        {
            if (!context.Clients.Any())
            {
                List<Client> clients = new()
                {
                    new Client
                    {
                        ClientId = "clayUser",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("bricks".Sha256()) },
                        AllowedScopes = { "unlockapi.read" }
                    },
                    new Client
                    {
                        ClientId = "m2m.client",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("SuperSecretPassword".Sha256()) },
                        AllowedScopes = { "weatherapi.read" }
                    }
                };

                foreach (Client client in clients)
                {
                    await context.Clients.AddAsync(client.ToEntity());
                }
                await context.SaveChangesAsync();
                WriteLine($"Added {clients.Count} clients");
            }
            else
            {
                WriteLine("clients already added..");
            }

            if (!context.ApiResources.Any())
            {
                List<ApiResource> apiResources = new()
                {
                    new ApiResource("weatherapi")
                    {
                        Scopes = { "weatherapi.read" }
                    },
                    new ApiResource("unlockapi")
                    {
                        Scopes = { "unlockapi.read" }
                    }
                };

                foreach (ApiResource apiRrc in apiResources)
                {
                    context.ApiResources.Add(apiRrc.ToEntity());
                }
                await context.SaveChangesAsync();
                WriteLine($"Added {apiResources.Count} api resources");
            }
            else
            {
                WriteLine("api resources already added..");
            }


            if (!context.ApiScopes.Any())
            {
                List<ApiScope> scopes = new()
                {
                    new ApiScope("weatherapi.read", "Read Access to API"),
                    new ApiScope("weatherapi.write", "Write Access to API"),
                    new ApiScope("unlockapi.read", "Read Access to API"),
                    new ApiScope("unlockapi.write", "Write Access to API")
                };

                foreach (ApiScope scope in scopes)
                {
                    context.ApiScopes.Add(scope.ToEntity());
                }
                await context.SaveChangesAsync();
                WriteLine($"Added {scopes.Count()} api scopes");
            }
            else
            {
                WriteLine("api scopes already added..");
            }

            if (!context.IdentityResources.Any())
            {
                List<IdentityResource> identityResources = new()
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email()
                };

                foreach (IdentityResource identity in identityResources)
                {
                    context.IdentityResources.Add(identity.ToEntity());
                }

                await context.SaveChangesAsync();
                WriteLine($"Added {identityResources.Count()} identity Resources");
            }
            else
            {
                WriteLine("api scopes already added..");
            }
        }
    }
}