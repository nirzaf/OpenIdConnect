using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
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
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        public static void SeedIdentityServer(IServiceProvider serviceProvider)
        {
            Console.WriteLine("Seeding data for Identity server");

            var context = serviceProvider
                .GetRequiredService<ConfigurationDbContext>();

            var userMng = serviceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            SeedData(context);

            SeedTestUsers(userMng);
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
                    EmailConfirmed = true,
                };
                var result = manager.CreateAsync(alice, "alice").Result;

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

            // bob need 2FA
            var bob = manager.FindByNameAsync("bob").Result;
            if (bob == null)
            {
                bob = new IdentityUser
                {
                    UserName = "bob",
                    Email = "bob@test.com",
                    EmailConfirmed = true,
                };
                var result = manager.CreateAsync(bob, "bob").Result;

                if (result.Succeeded)
                {
                    result = manager.AddClaimsAsync(bob, new Claim[] {
                        new Claim(JwtClaimTypes.Name, "bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.WebSite, "Website"),
                    }).Result;

                    Console.WriteLine("added bob user");

                    var t = manager.ResetAuthenticatorKeyAsync(bob).Result;
                    var unformattedKey = manager.GetAuthenticatorKeyAsync(bob).Result;

                    var sharedKey = HelperClass.FormatKey(unformattedKey);

                    var email = manager.GetEmailAsync(bob).Result;

                    var authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);

                    t = manager.SetTwoFactorEnabledAsync(bob, enabled: true).Result;

                    Console.WriteLine("Enabled 2FA with Authenticator URL: " + authenticatorUri);
                }
            }
            else
            {
                var unformattedKey = manager.GetAuthenticatorKeyAsync(bob).Result;
                var isEnabled = manager.GetTwoFactorEnabledAsync(bob).Result;

                var sharedKey = HelperClass.FormatKey(unformattedKey);

                var email = manager.GetEmailAsync(bob).Result;

                var authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);

                Console.WriteLine("bob already created");
                Console.WriteLine($"2FA enabled = {isEnabled}, with Authenticator URL: " + authenticatorUri);
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
                        ClientId = "interactive.public",

                        AllowedGrantTypes = GrantTypes.Code,
                        // this client is SPA, and therefore doesn't need client secret
                        RequireClientSecret = false,

                        RedirectUris = { "https://oauth.pstmn.io/v1/callback", "http://localhost:3000/signin-oidc" },
                        PostLogoutRedirectUris = { "http://localhost:3000" },
                        AllowOfflineAccess = true,

                        AllowedScopes = { "openid", "profile", "weatherapi.read" }
                    },
                    new Client
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
                    new Client
                {
                    ClientId = "MvcClient",
                    AllowedGrantTypes = GrantTypes.Code,
                    ClientSecrets = { new Secret("password".Sha256()) },

                    PostLogoutRedirectUris = { "https://localhost:5005/signout-callback-oidc", "https://oauth.pstmn.io/v1/callback" },
                    RedirectUris = { "https://localhost:5005/signin-oidc" },

                    FrontChannelLogoutUri = "https://localhost:5005/signout-oidc",

                    AllowOfflineAccess = true,
                    // how long the refresh token should live
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    AbsoluteRefreshTokenLifetime = 600, 

                    // this will dictate the Session Cookie on the client app (e.g. MVC)
                    IdentityTokenLifetime = 30,
                    AllowedScopes = {
                        "openid", "profile", "weatherapi.read"
                    },
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