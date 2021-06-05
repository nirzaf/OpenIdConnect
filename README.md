## OpenID Connect, OAuth2 with Duende Identity Server, React client and Angular Login UI

This sample repo contains the minimum code to demonstrate OpenId Connect and Duende Identity Server. It contains the following projects:

-   `ids-server`: Duende IdentityServer with EF + Angular login UI
-   `weatherapi`: API which is protected by the IdentityServer
-   `react-client`: a React Client which allows user to login using Identity Server and communicate with the Weather API

## Step 1: Add new empty InMemory Duende IdentityServer

-   create new Duende Identity Server project

```bash
mkdir ids-server
cd ids-server
dotnet new web
dotnet add package Duende.IdentityServer
```

-   add `services.AddIdentityServer()`

```csharp
// startup.cs
// using section (>idsImportDuende)

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;

// then inside ConfigureServices() method (>idsAddIdsEmpty)
services.AddIdentityServer()
  .AddInMemoryClients(new List<Client>())
  .AddInMemoryIdentityResources(new List<IdentityResource>())
  .AddInMemoryApiResources(new List<ApiResource>())
  .AddInMemoryApiScopes(new List<ApiScope>())
  .AddTestUsers(new List<TestUser>());

// in Configure(), after UseRouting()
app.UseIdentityServer();
```

-   run `dotnet watch run` and open `https://localhost:5001/.well-known/openid-configuration` to see the open Id connect discovery doc
-   compare it with https://accounts.google.com/.well-known/openid-configuration

## Step 2: Add new Web API project

-   add new Web API project in the `weatherapi` folder

```bash
# on root folder
mkdir weatherapi
cd weatherapi
dotnet new webapi
```

-   change `launchSettings.json` and update applicationUrl to `https://localhost:5002;http://localhost:5003`
-   run `dotnet watch run` and open `https://localhost:5002/WeatherForecast` to see the json weather data

## Step 3: protect weatherapi

-   Add the Jwt Bearer package

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

-   add `services.AddAuthentication("Bearer")` and `app.UseAuthentication()` to startup.cs and `[Authorize]` on the `WeatherForecastController.cs`

```csharp

// inside ConfigureServices() method of weather API
// >idsAddJwtAuth

services.AddAuthentication("Bearer")
  .AddJwtBearer("Bearer", options =>
  {
      options.Audience = "weatherapi";
      options.Authority = "https://localhost:5001";

      // ignore self-signed ssl
      options.BackchannelHttpHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = delegate { return true; } };
  });


// inside Configure() method, after UseRouting, before UseAuthorization()

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


// WeatherForecastController.cs
[ApiController]
[Authorize]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase

```

-   try open https://localhost:5002/WeatherForecast, you will get `401`

-   add clients, resource and scope in the `startup.cs` of the IdentityServer

```csharp
// ConfigureServices(), startup.cs of ids-server
// >idsAddM2mClient

services.AddIdentityServer()
  .AddInMemoryApiScopes(new List<ApiScope> {
      new ApiScope("weatherapi.read", "Read Access to API"),
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
      }
  });
```

-   get new access token by calling `POST https://localhost:5001/connect/token`

```curl
POST https://localhost:5001/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&scope=weatherapi.read&client_id=m2m.client&client_secret=SuperSecretPassword
```

-   use the token and call weatherapi (in Authorization header), you should now get data back

```curl
GET https://localhost:5002/WeatherForecast
Authorization: Bearer <TOKEN>
```

## Step 4: Add Quick start UI from Duende Quick start UI

-   in ids-server folder
-   run `curl -L https://raw.githubusercontent.com/DuendeSoftware/IdentityServer.Quickstart.UI/main/getmain.sh | bash` inside `ids-server`, this will download 3 new folders to the project: `QuickStart`,`Views` and `wwwroot`
-   add `services.AddControllersWithViews();`
-   add `app.UseStaticFiles();`
-   add `app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());`

```csharp
// inside ConfigureServices() method
services.AddControllersWithViews();


// inside Configure() method
// before UseRouting()
app.UseStaticFiles();

...
// after UseIdentityServer() >idsMapDefaultController
app.UseAuthorization();
app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
```

-   Add test user to the end of services.AddIdentityServer()

```csharp

// >idsAddTestUser
.AddTestUsers(new List<TestUser>() {
    new TestUser
        {
            SubjectId = "Alice",
            Username = "alice",
            Password = "alice"
        }
});
```

-   open https://localhost:5001/account/login and login using `alice` and `alice`

## Step 5: Add React Interactive Client

-   in `ids-server` and `AddInMemoryIdentityResources()` and add new client `.AddInMemoryClients()`

```csharp
// >idsAddInteractiveClient
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

        AllowedGrantTypes = GrantTypes.Code,
        RequireClientSecret = false,

        RedirectUris = { "http://localhost:3000/signin-oidc" },
        PostLogoutRedirectUris = { "http://localhost:3000" },

        AllowedScopes = { "openid", "profile", "weatherapi.read" }
    },
})
.AddInMemoryIdentityResources(new List<IdentityResource>() {
    new IdentityResources.OpenId(),
    new IdentityResources.Profile()
})
```

-   enable Cors on **both** ids_server and weather API by adding `services.AddCors();`, this will make sure React app can communicate with both backend servers

```csharp
// inside ConfigureServices() method for BOTH weatherapi and ids-server
services.AddCors();

// inside Configure() method  for BOTH weatherapi and ids-server
// Before UseRouting(), >addCors
app.UseCors(config => config
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);
app.UseRouting();
```

-   create new React app by running `npx create-react-app react-client`

```bash
# from root
npx create-react-app react-client
cd react-client
npm i oidc-client react-router-dom
```
-   import the required components and libraries for the changes we are going to make to App.js

```jsx
// App.js >idsImportComponentsAndLibraries
import React, { useState, useEffect } from "react";
import { BrowserRouter, Switch, Route } from "react-router-dom";
import { UserManager } from "oidc-client";
```

-   replace content of `App()` function with the following

```jsx
// App.js >idsReactApp
function App() {
	return (
		<BrowserRouter>
			<Switch>
				<Route path="/signin-oidc" component={Callback} />
				<Route path="/" component={HomePage} />
			</Switch>
		</BrowserRouter>
	);
}
```

-   in the same file add HomePage() component, this component will display `login` button if user is not logged in

```javascript
// App.js >idsReactAppHomePage

const IDENTITY_CONFIG = {
	authority: "https://localhost:5001",
	client_id: "interactive",
	redirect_uri: "http://localhost:3000/signin-oidc",
	post_logout_redirect_uri: "http://localhost:3000",
	response_type: "code",
	scope: "openid profile weatherapi.read",
};

function HomePage() {
	const [state, setState] = useState(null);
	var mgr = new UserManager(IDENTITY_CONFIG);

	useEffect(() => {
		mgr.getUser().then((user) => {
			if (user) {
				fetch("https://localhost:5002/weatherforecast", {
					headers: {
						Authorization: "Bearer " + user.access_token,
					},
				})
					.then((resp) => resp.json())
					.then((data) => setState({ user, data }));
			}
		});
	}, []);

	return (
		<div>
			{state ? (
				<>
					<h3>Welcome {state?.user?.profile?.sub}</h3>
					<pre>{JSON.stringify(state?.data, null, 2)}</pre>
					<button onClick={() => mgr.signoutRedirect()}>
						Log out
					</button>
				</>
			) : (
				<>
					<h3>React Weather App</h3>
					<button onClick={() => mgr.signinRedirect()}>Login</button>
				</>
			)}
		</div>
	);
}
```

-   in the same file add Redirect() component, this will handle OAuth2 redirect

```javascript
// App.js >idsReactAppCallback
function Callback() {
	useEffect(() => {
		var mgr = new UserManager({
			response_mode: "query",
		});

		mgr.signinRedirectCallback().then(() => (window.location.href = "/"));
	}, []);

	return <p>Loading...</p>;
}
```

-   run `npm start`
-   open http://localhost:3000, click on `Login` button
-   login with `alice` and `alice`
-   you should be redirected back to the React app with your name and weather information
-   click `Logout`, you should be redirected back to the Identity Server, with a link to take you back to the React app

## Step 6: Replace in memory provider with EntityFrameworkCore

-   now that we have Identity Server 5 working with React and a API client, let's see how we can replace the in memory provider with Entity framework (using Sqlite)
-   first, install the follow Nuget package into the Duende Identity Server

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
dotnet add package Duende.IdentityServer.EntityFramework
```

-   now add a default connection string to the `appsettings.json` file

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=IdentityServer.db"
	},
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft": "Warning",
			"Microsoft.Hosting.Lifetime": "Information"
		}
	},
	"AllowedHosts": "*"
}
```
-   Add the required dependency using statements to startup.cs

```csharp
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.Extensions.Configuration;
```

-   now inject `IConfiguration` into startup.cs class of the IDS

```csharp
public IConfiguration Configuration { get; }

public Startup(IConfiguration configuration)
{
	Configuration = configuration;
}
```

-Add the connection string and migration assembly to ConfigureServices()

```csharp
var connectStr = Configuration.GetConnectionString("DefaultConnection");

var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
```

-   replace the InMemory providers on the services.AddIdentityServer() command with the following:

```csharp
services.AddIdentityServer()
  .AddConfigurationStore(options =>
  {
      options.ConfigureDbContext = builder => builder.UseSqlite(connectStr, opt => opt.MigrationsAssembly(migrationAssembly));
  })
  .AddOperationalStore(options =>
  {
      options.ConfigureDbContext = builder => builder.UseSqlite(connectStr, opt => opt.MigrationsAssembly(migrationAssembly));
  })
  .AddTestUsers(new List<TestUser>() {
    new TestUser
      {
          SubjectId = "Alice",
          Username = "alice",
          Password = "alice"
      }
  });
```

-   where `connectStr` is coming from `Configuration.GetConnectionString("DefaultConnection")`
-   Stop running the ids-server application
-   If you do not have it already then install the Entity Framework Core tools CLI at https://docs.microsoft.com/en-us/ef/core/cli/dotnet
-   then run `dotnet ef migrations add InitialIdsMigration -c PersistedGrantDbContext` to add Initial Migration
-   you will see the initial migration code for the database which is creating 3 tables
-   now run `dotnet ef database update -c PersistedGrantDbContext` to create the DB file
-   now add the tables for the ConfigurationContext
-   then run `dotnet ef migrations add InitialIdsMigration -c ConfigurationDbContext` to add Initial Migration
-   now run `dotnet ef database update -c ConfigurationDbContext` to create the DB file
-   open the sqlite DB (you can install the sqlite VSCode Extension to view the data), you should see all the tables it created
-   now run `dotnet run` again and open https://localhost:5001/.well-known/openid-configuration again
-   you should see it is empty again (no scopes, no clients)

## Step 7: Setup data seeder class in the ids-server project

-   add new file `.\Data\SeedData.cs` like this

```csharp
using System;

public class DataSeeder
{
    public static void SeedIdentityServer(IServiceProvider serviceProvider)
    {
        Console.WriteLine("Seeding data for Identity server");
    }
}
```

-add dependency using statements to program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
```

- modify the main method of program.cs like this

```csharp
public static void Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();

    using (var serviceScope = host.Services.CreateScope())
    {
        var conf = serviceScope.ServiceProvider.GetService<IConfiguration>();
        if (conf.GetValue("SeedData", true))
            DataSeeder.SeedIdentityServer(serviceScope.ServiceProvider);
    }

    host.Run();
}
```

-   add new config in appsettings.json file

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=IdentityServer.db"
	},
	"SeedData": true,
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft": "Warning",
			"Microsoft.Hosting.Lifetime": "Information"
		}
	},
	"AllowedHosts": "*"
}
```

-   run `dotnet run` and make sure you can see the console log saying "Seeding data for Identity server"

## Step 8: Add seed data to Identity Server

-   add the following code to the `SeedData.cs` file

```csharp
public class DataSeeder
    {
        public static void SeedIdentityServer(IServiceProvider serviceProvider)
        {
            Console.WriteLine("Seeding data for Identity server");

            var context = serviceProvider
                .GetRequiredService<ConfigurationDbContext>();

            DataSeeder.SeedData(context);
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

                        RedirectUris = { "http://localhost:3000/signin-oidc" },
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
                    new ApiScope("weatherapi.write", "Write Access to API")
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
        }
    }
```

-   run `dotnet run` and open http://localhost:5000/.well-known/openid-configuration again
-   you should see the support_scopes now contains your newly added scopes above

## Step 9: Use ASP.NET Core Identity instead of In memory test users

-   install the following nuget packages

```bash
dotnet add package Duende.IdentityServer.AspNetIdentity --version 5.1.0
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 5.0.5
```

- add new file `.\Data\ApplicationDbContext.cs` like this

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext
{
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
	}
}
```

- in the `startup.cs` file, add the following before the .AddIdentityServer() method

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectStr, opt => opt.MigrationsAssembly(migrationAssembly));
});

services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
```

-   remove `.UseTestUsers()` and replace with `.AddAspNetIdentity<IdentityUser>();`
-   now run the migration to add the ASP.NET Core Identity tables to the database

```bash
dotnet ef migrations add InitialIdsMigration -c ApplicationDbContext
dotnet ef database update -c ApplicationDbContext
```

- add using statements to `SeedData.cs` 

```csharp
using IdentityModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
```

- Modify the `SeedData.cs` SeedIdentityServer method to look this this

```csharp
using 

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
```

- Modify `SeedData.cs` by adding a new method to seed the test users

```csharp
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
}
```

-   run `dotnet run` again and query the ASPNetUsers table to make sure you have the test user there

## Step 10: Update Account controller to user ASP.NET Core Identity

- add using statements to `AccountController.cs` 

```csharp
using Microsoft.AspNetCore.Identity;
```

- update AccountController.cs and replace `private readonly TestUserStore _users;` with `private readonly SignInManager<IdentityUser> manager;` then update the constructor accordingly.
- replace the logic for checking for username and password in the `public async Task<IActionResult> Login(LoginInputModel model, string button)` method as follow

```csharp
var user = await manager.UserManager.FindByNameAsync(model.Username);
// validate username/password against in-memory store
if (user != null && await manager.CheckPasswordSignInAsync(user, model.Password, true) == Microsoft.AspNetCore.Identity.SignInResult.Success)
{
	await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));
    	.... code ....
}
```

-   replace `user.SubjectId` with `user.Id` and `user.Username` with `user.UserName`
-   run `dotnet run` again and navigate to http://localhost:5000/account/login and login using `alice` and `alice` as password

## Step 11: Add Angular login UIs (instead of using QuickStart UI)

-   run `npx ng new ClientApp` and turn on routing and scss support
-   add nuget package `Microsoft.AspNetCore.SpaServices.Extensions` to the idsserver
-   modify the `idsserver.csproj` and add the following

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
    <TypeScriptToolsVersion>Latest</TypeScriptToolsVersion>
    <IsPackable>false</IsPackable>
    <SpaRoot>ClientApp\</SpaRoot>
    <DefaultItemExcludes>$(DefaultItemExcludes);$(SpaRoot)node_modules\**</DefaultItemExcludes>
    <BuildServerSideRenderer>false</BuildServerSideRenderer>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Duende.IdentityServer" Version="5.1.0"/>
    <PackageReference Include="Duende.IdentityServer.AspNetIdentity" Version="5.1.0"/>
    <PackageReference Include="Duende.IdentityServer.EntityFramework" Version="5.1.0"/>
    <PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="5.0.5"/>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="5.0.5"/>
    <PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="5.0.5"/>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="5.0.5"/>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="5.0.5"/>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="5.0.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="5.0.2"/>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="5.0.5"/>
    <PackageReference Include="Microsoft.AspNetCore.SpaServices.Extensions" Version="5.0.5"/>
  </ItemGroup>
  <ItemGroup>
    <Content Remove="$(SpaRoot)**" />
    <None Remove="$(SpaRoot)**" />
    <None Include="$(SpaRoot)**" Exclude="$(SpaRoot)node_modules\**" />
  </ItemGroup>
  <Target Name="DebugEnsureNodeEnv" BeforeTargets="Build" Condition=" '$(Configuration)' == 'Debug' And !Exists('$(SpaRoot)node_modules') ">
    <Exec Command="node --version" ContinueOnError="true">
      <Output TaskParameter="ExitCode" PropertyName="ErrorCode" />
    </Exec>
    <Exec WorkingDirectory="$(SpaRoot)" Command="npm install" />
    <Error Condition="'$(ErrorCode)' != '0'" Text="Node.js is required to build and run this project. To continue, please install Node.js from https://nodejs.org/, and then restart your command prompt or IDE." />
    <Message Importance="high" Text="Restoring dependencies using 'npm'. This may take several minutes..." />
  </Target>
  <Target Name="PublishRunWebpack" AfterTargets="ComputeFilesToPublish">
    <Exec WorkingDirectory="$(SpaRoot)" Command="npm install" />
    <Exec WorkingDirectory="$(SpaRoot)" Command="npm run build -- --prod" />
    <Exec WorkingDirectory="$(SpaRoot)" Command="npm run build:ssr -- --prod" Condition=" '$(BuildServerSideRenderer)' == 'true' " />
    <ItemGroup>
      <DistFiles Include="$(SpaRoot)dist\**; $(SpaRoot)dist-server\**" />
      <DistFiles Include="$(SpaRoot)node_modules\**" Condition="'$(BuildServerSideRenderer)' == 'true'" />
      <ResolvedFileToPublish Include="@(DistFiles->'%(FullPath)')" Exclude="@(ResolvedFileToPublish)">
        <RelativePath>%(DistFiles.Identity)</RelativePath>
        <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
        <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
      </ResolvedFileToPublish>
    </ItemGroup>
  </Target>
</Project>
```

-   modify `startup.cs` file as follow

```csharp
public void ConfigureServices(IServiceCollection services) {
    // current codes

    // add static angular
    services.AddSpaStaticFiles(configuration =>
    {
        configuration.RootPath = "ClientApp/dist";
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    // current codes
    // use spa
    app.UseSpa(spa =>
    {
        // To learn more about options for serving an Angular SPA from ASP.NET Core,
        // see https://go.microsoft.com/fwlink/?linkid=864501

        spa.Options.SourcePath = "ClientApp";

        if (env.IsDevelopment())
        {
            spa.UseAngularCliServer(npmScript: "start");
        }
    });
}
```

-   update `HomeController.cs` and comment out the Index() method
-   run `dotnet run` again and open https://localhost:5001, you should see the Angular Starting page now
-   navigate to https://localhost:5001/account/login and you should see the QuickStart UI for the IdentityServer

## Step 12: Add required Angular login pages

-   add 4 components to the angular app: Home, Login, Logout, MFA, LoggedOut and NotFound
-   add 4 routes to `app-routing.module.ts`

```typescript
const routes: Routes = [
	{ path: "", component: LoginComponent },
	{ path: "login", component: LoginComponent },
	{ path: "logout", component: LogoutComponent },
	{ path: "loggedout", component: LoggedoutComponent },
	{ path: "mfa", component: MfaComponent },
	{ path: "home", component: HomeComponent },
	{ path: "**", component: NotFoundComponent },
];
```

-   add tailwind by running `npx ng add @ngneat/tailwind`
-   go to https://tailwindcomponents.com/ and search for the templates you want to use and paste into each angular component
-   edit `startup.cs` and add this option to the .AddIdentityServer() method

```csharp
services.AddIdentityServer(options =>
{
    // login page is now on the Angular SPA
    options.UserInteraction.LoginUrl = "~/";
})
```

-   use postman and trigger Authorization Code flow and make sure you can see login screen in Angular

## Step 13: Add login API and hook up Angular code

-   add the following to `login.component.ts` file

```typescript
import { HttpClient } from "@angular/common/http";
import { Component, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
	selector: "app-login",
	templateUrl: "./login.component.html",
	styleUrls: ["./login.component.scss"],
})
export class LoginComponent implements OnInit {
	username: string;
	password: string;
	returnUrl: string;
	error: string;

	constructor(private http: HttpClient, private router: ActivatedRoute) {}

	ngOnInit(): void {
		this.returnUrl = this.router.snapshot.queryParams["ReturnUrl"];
	}

	login() {
		this.error = "";
		this.http
			.post("/auth/login", {
				username: this.username,
				password: this.password,
				rememberLogin: false,
				returnUrl: this.returnUrl,
			})
			.subscribe(
				(rsp) => {
					window.location.href = (rsp as any).returnUrl;
				},
				(_) => {
					this.error = `Login failed!`;
				}
			);
	}
}
```

-   add the following to the import array of the `app.module.ts` file

```typescript
// app.module.ts
imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule
  ],
```

-   use 2ways binding to username, password input field
-   call `login()` method when form is submitted

```html
<form (ngSubmit)="login()">
	<input [(ngModel)]="username" ... />
	<input [(ngModel)]="password" ... />
	<button type="submit" .. />
</form>
```

-   add new folder called `Controllers` and add new file called `AuthController.cs`
-   copy the codes from AccountController.cs and modify as follow:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Identity;
using IdentityServerHost.Quickstart.UI;

namespace idsserver
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly SignInManager<IdentityUser> _manager;

        public AuthController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            SignInManager<IdentityUser> manager)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _manager = manager;
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginInputModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (string.IsNullOrEmpty(model?.Username) || string.IsNullOrEmpty(model?.Password))
                return BadRequest("invalid request payload");

            var user = await _manager.UserManager.FindByNameAsync(model.Username);

            if (user != null && await _manager.CheckPasswordSignInAsync(user, model.Password, true) == Microsoft.AspNetCore.Identity.SignInResult.Success)
            {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                AuthenticationProperties props = null;
                if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                {
                    props = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                    };
                };

                await _manager.SignInAsync(user, props);

                if (context != null)
                {
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return Ok(new
                        {
                            ReturnUrl = model.ReturnUrl
                        });
                    }

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Ok(new
                    {
                        ReturnUrl = model.ReturnUrl
                    });
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Ok(new
                    {
                        ReturnUrl = model.ReturnUrl
                    });
                }
                else if (string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return Ok(new
                    {
                        ReturnUrl = "/"
                    });
                }
                else
                {
                    // user might have clicked on a malicious link - should be logged
                    return BadRequest("invalid return URL");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            return BadRequest("Something went wrong");
        }
    }
}
```

- use Postman and test the login flow (using Authorization Code flow with PKCE)
- login using `alice` and `alice` => you should be able to get access token in Postman