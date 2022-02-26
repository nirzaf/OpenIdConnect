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
using static Duende.IdentityServer.IdentityServerConstants;

namespace idsserver
{
    public class DataSeeder
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public static void SeedIdentityServer(IServiceProvider serviceProvider)
        {
            WriteLine("Seeding data for Identity server");

            var context = serviceProvider
                .GetRequiredService<ConfigurationDbContext>();

            var userMng = serviceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            SeedData(context);

            SeedTestUsers(userMng);
        }

        private static async Task SeedTestUsers(UserManager<IdentityUser> manager)
        {
            var basicUser = manager.FindByNameAsync("fazrin@gmail.com").Result;
            if (basicUser is null)
            {
                basicUser = new IdentityUser
                {
                    UserName = "fazrin@gmail.com",
                    Email = "fazrin@gmail.com",
                    EmailConfirmed = true
                };
                var result = manager.CreateAsync(basicUser, "123@Pa$$word!").Result;

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
            var adminUser = await manager.FindByNameAsync("mfmfazrin1986@gmail.com");
            if (adminUser is null)
            {
                adminUser = new IdentityUser
                {
                    UserName = "mfmfazrin1986@gmail.com",
                    Email = "mfmfazrin1986@gmail.com",
                    EmailConfirmed = true
                };
                var result = await manager.CreateAsync(adminUser, "123@Pa$$word!");

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

                    var raka = await manager.ResetAuthenticatorKeyAsync(adminUser);
                    var unformattedKey = await manager.GetAuthenticatorKeyAsync(adminUser);
                    var sharedKey = HelperClass.FormatKey(unformattedKey);
                    var email = await manager.GetEmailAsync(adminUser);
                    var authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);
                    raka = await manager.SetTwoFactorEnabledAsync(adminUser, true);
                    WriteLine("Enabled 2FA with Authenticator URL: " + authenticatorUri);
                }
            }
            else
            {
                var unformattedKey = await manager.GetAuthenticatorKeyAsync(adminUser);
                var isEnabled = await manager.GetTwoFactorEnabledAsync(adminUser);
                var sharedKey = HelperClass.FormatKey(unformattedKey);
                var email = await manager.GetEmailAsync(adminUser);
                var authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);
                WriteLine("Admin already created");
                WriteLine($"2FA enabled = {isEnabled}, with Authenticator URL: " + authenticatorUri);
            }
        }

        private static void SeedData(ConfigurationDbContext context)
        {
            if (!context.Clients.Any())
            {
                var clients = new List<Client>
                {
                    new()
                    {
                        ClientId = "m2m.client",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        ClientSecrets = { new Secret("SuperSecretPassword".Sha256()) },
                        AllowedScopes = { "weatherapi.read" }
                    },
                    new()
                    {
                        ClientId = "interactive.public",

                        AllowedGrantTypes = GrantTypes.Code,
                        // this client is SPA, and therefore doesn't need client secret
                        RequireClientSecret = false,

                        RedirectUris = { "https://oauth.pstmn.io/v1/callback", "http://localhost:3000/signin-oidc" },
                        PostLogoutRedirectUris = { "http://localhost:3000" },
                        AllowOfflineAccess = true,

                        AllowedScopes = { "openid", "profile", "weatherapi.read" }
                    },
                    new()
                    {
                        // e.g. MVC apps (or any other client apps that can secure the client secret)
                        ClientId = "interactive.private",

                        AllowedGrantTypes = GrantTypes.Code,
                        ClientSecrets = { new Secret("SuperSecretPassword".Sha256()) },

                        RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                        PostLogoutRedirectUris = { "http://localhost:3000" },
                        AllowOfflineAccess = true,
                        AllowedScopes = { "openid", "profile", "weatherapi.read" }
                    },
                    new()
                    {
                        ClientId = "MvcClient",
                        AllowedGrantTypes = GrantTypes.Code,
                        ClientSecrets = { new Secret("password".Sha256()) },

                        PostLogoutRedirectUris =
                            { "https://localhost:5005/signout-callback-oidc", "https://oauth.pstmn.io/v1/callback" },
                        RedirectUris = { "https://localhost:5005/signin-oidc" },

                        FrontChannelLogoutUri = "https://localhost:5005/signout-oidc",

                        AllowOfflineAccess = true,
                        // how long the refresh token should live
                        RefreshTokenExpiration = TokenExpiration.Sliding,
                        AbsoluteRefreshTokenLifetime = 600,

                        // this will dictate the Session Cookie on the client app (e.g. MVC)
                        IdentityTokenLifetime = 30,
                        AllowedScopes =
                        {
                            "openid", "profile", "weatherapi.read"
                        }
                    },
                    new()
                    {
                        ClientId = "clayAdmin",
                        ClientName = "Clay Solutions",
                        ClientSecrets = { new Secret("clay".Sha256()) },
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        AllowOfflineAccess = true,
                        AllowedScopes = new List<string>
                        {
                            "unlockapi.read",
                            "unlockapi.write",
                            StandardScopes.OpenId,
                            StandardScopes.Profile,
                            StandardScopes.Email
                        },

                        RedirectUris = { "https://localhost:44377/api/identity/token" }
                    },
                    new()
                    {
                        ClientId = "clayUser",
                        ClientName = "Clay Employees",
                        ClientSecrets = { new Secret("bricks".Sha256()) },
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        RedirectUris = { "https://localhost:44377/api/identity/token" },
                        AllowOfflineAccess = true,
                        AllowedScopes = new List<string>
                        {
                            "unlockapi.read",
                            "unlockapi.write",
                            StandardScopes.OpenId,
                            StandardScopes.Profile,
                            StandardScopes.Email
                        }
                    }
                };

                foreach (var client in clients)
                {
                    context.Clients.Add(client.ToEntity());
                }

                context.SaveChanges();
                WriteLine($"Added {clients.Count()} clients");
            }
            else
            {
                WriteLine("clients already added..");
            }

            if (!context.ApiResources.Any())
            {
                var apiResources = new List<ApiResource>()
                {
                    new("weatherapi")
                    {
                        Scopes = { "weatherapi.read" }
                    }
                };

                foreach (var apiRrc in apiResources)
                {
                    context.ApiResources.Add(apiRrc.ToEntity());
                }

                context.SaveChanges();
                WriteLine($"Added {apiResources.Count()} api resources");
            }
            else
            {
                WriteLine("api resources already added..");
            }


            if (!context.ApiScopes.Any())
            {
                var scopes = new List<ApiScope>
                {
                    new("weatherapi.read", "Read Access to API"),
                    new("weatherapi.write", "Write Access to API")
                };

                foreach (var scope in scopes)
                {
                    context.ApiScopes.Add(scope.ToEntity());
                }

                context.SaveChanges();
                WriteLine($"Added {scopes.Count()} api scopes");
            }
            else
            {
                WriteLine("api scopes already added..");
            }

            if (!context.IdentityResources.Any())
            {
                var identityResources = new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email()
                };

                foreach (var identity in identityResources)
                {
                    context.IdentityResources.Add(identity.ToEntity());
                }

                context.SaveChanges();
                WriteLine($"Added {identityResources.Count()} identity Resources");
            }
            else
            {
                WriteLine("api scopes already added..");
            }
        }
    }
}