## OpenID Connect, OAuth2 with Duende Identity Server

This sample repo contains the minimum code to demonstrate OpenId Connect and Duende Identity Server. It contains the following projects:

1. `ids-server`: Duende IdentityServer using In Memory provider, this is listening on https://localhost:5001
1. `weatherapi`: API which is protected by the IdentityServer
1. `SPA-client`: a Angular Client which allows user to login using Open Id connect

## Step 1: Add new empty, in Memory Identity Server

1. run `dotnet new web` in the `ids-server` folder
1. run `dotnet add package Duende.IdentityServer` 
1. import Duende packages and add `services.AddIdentityServer()` 
1. run `dotnet watch run` and open `https://localhost:5001/.well-known/openid-configuration` to see the open Id connect discovery doc

## Step 2: Add new Web API project

1. run `dotnet new webapi` in the `weatherapi` folder
1. change `launchSettings.json` and update applicationUrl to `https://localhost:5002;http://localhost:5003`
1. run `dotnet watch run` and open `https://localhost:5002/WeatherForecast` to see the json weather data 

## Step 3: Add Authorize on weatherapi

1. add `services.AddAuthentication("Bearer")` and `app.UseAuthentication()` to startup.cs and `[Authorize]` on the controller
1. add clients, resource and scope in the `startup.cs` of the IdentityServer
1. get new access token by calling `POST https://localhost:5001/connect/token`
1. use the token and call weatherapi (in Authorization header)

## Step 4: Add Interactive Client

1. add test user in `.AddTestUsers()` 
1. add AddInMemoryIdentityResources `.AddTestUsers()` 
1. add new interactive client `.AddInMemoryClients()` 
1. check `https://localhost:5001/.well-known/openid-configuration`


## Step 5: Add standard login UI from Duende Quick start UI

1. run `curl -L https://raw.githubusercontent.com/DuendeSoftware/IdentityServer.Quickstart.UI/main/getmain.sh | bash` inside `ids-server`, this will add 3 new folders to the project: `QuickStart`,`Views` and `wwwroot`
1. add `services.AddControllersWithViews();`
1. add `app.UseStaticFiles();`
1. add `app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());`
1. open https://localhost:5001/account/login and login using `alice` and `alice`

## Step 6: Add React client app

1. enable Cors on both ids_server and weather API by adding `services.AddCors();` and 

```csharp
app.UseCors(config => config
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);
```

1. create new React app by running `npx create-react-app react-client` 
1. cd into the `react-client` and run `npm i oidc-client react-router-dom`
1. replace `App()` with the following 2 routes 

```jsx

// App.js
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

1. add OpenID connect config object

```javascript
const IDENTITY_CONFIG = {
  authority: "https://localhost:5001",
  client_id: "interactive",
  redirect_uri: "http://localhost:3000/signin-oidc",
  post_logout_redirect_uri: "http://localhost:3000",
  response_type: "code",
  scope: "openid weatherapi",
};
```
1. in the same file add HomePage() component, this component will display `login` button if user is not logged in


```javascript
// App.js
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
          <button onClick={() => mgr.signoutRedirect()}>Log out</button>
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

1. in the same file add Redirect() component, this will handle OAuth2 redirect


```javascript
// App.js
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

1. open http://localhost:3000, click on `Login` button
1. login with `alice` and `alice`
1. you should be redirected back to the React app with your name and weather information
1. click `Logout`, you should be redirected back to the Identity Server, with a link to take you back to the React app