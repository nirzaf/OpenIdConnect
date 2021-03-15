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


