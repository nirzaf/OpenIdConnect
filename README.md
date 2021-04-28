## OpenID Connect, OAuth2 with Duende Identity Server + React

This sample repo contains the minimum code to demonstrate OpenId Connect and Duende Identity Server. It contains the following projects:

-   `ids-server`: Duende IdentityServer using In Memory provider
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

-   Add test user

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
	scope: "openid weatherapi",
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

-   now inject `IConfiguration` into startup.cs class of the IDS
-   remove InMemory providers with the following:

```csharp
var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
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
-   then run `dotnet ef migrations add InitialIdsMigration -c PersistedGrantDbContext` to add Initial Migration
-   you will see the initial migration code for the database which is creating 3 tables
-   now run `dotnet ef database update -c PersistedGrantDbContext` to create the DB file
-   now add the tables for the ConfigurationContext
-   then run `dotnet ef migrations add InitialIdsMigration -c ConfigurationDbContext` to add Initial Migration
-   now run `dotnet ef database update -c ConfigurationDbContext` to create the DB file
-   open the sqlite DB (you can install the sqlite VSCode Extension to view the data), you should see all the tables it created
-   now run `dotnet run` again and open https://localhost:5001/.well-known/openid-configuration again
-   you should see it is empty again (no scopes, no clients)

## Step 7: Setup data seeder class

- add new file `.\Data\SeedData.cs` like this

```csharp
public class DataSeeder
{
    public static void SeedIdentityServer(IServiceProvider serviceProvider)
    {
        Console.WriteLine("Seeding data for Identity server");
    }
}
```

- modify program.cs file like this

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

- add new config in appsettings.json file

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

- run `dotnet run` and make sure you can see the console log saying "Seeding data for Identity server" 

## Step 8: Add seed data to Identity Server

- add the following code to the `SeedData.cs` file

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

- run `dotnet run` and open http://localhost:5000/.well-known/openid-configuration again
- you should see the support_scopes now contains your newly added scopes above


## Step 9: Use ASP.NET Core Identity instead of In memory test users
- install the following nuget packages

```bash
dotnet add package Duende.IdentityServer.AspNetIdentity --version 5.1.0
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 5.0.5
```

- in the `startup.cs` file, add the following before UseIdentityServer() method

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectStr, opt => opt.MigrationsAssembly(migrationAssembly));
});

services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
```

- remove `.UseTestUsers()` and replace with `.AddAspNetIdentity<IdentityUser>();`
- now run the migration to add the ASP.NET Core Identity tables to the database

```bash
dotnet ef migrations add InitialIdsMigration -c ApplicationDbContext
dotnet ef database update -c ApplicationDbContext
```

- update `SeedData.cs` and add new method to seed the test users

```csharp
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
```

- run `dotnet run` again and query the ASPNetUsers table to make sure you have the test user there

## Step 10: Update Account controller to user ASP.NET Core Identity
- update AccountController.cs and replace TestUsers with `SignInManager<IdentityUser> manager`
- replace the logic for checking for username and password in the Login() method as follow

```csharp
var user = await manager.UserManager.FindByNameAsync(model.Username);
// validate username/password against in-memory store
if (user != null && await manager.CheckPasswordSignInAsync(user, model.Password, true) == Microsoft.AspNetCore.Identity.SignInResult.Success)
{
    .... codes ....
}
```

- replace `user.SubjectId` with `user.Id` and `user.Username`  with `user.UserName`
- run `dotnet run` again and navigate to http://localhost:5000/account/login and login using `alice` and `Password1!` as password


## Step 11: Add 2FA to the solution
- add the following packages by running the following

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Identity.UI
dotnet aspnet-codegenerator identity -dc idsserver.ApplicationDbContext --files "Account.Manage.EnableAuthenticator;Account.LoginWith2fa"
```