# Ktos.AspNetCore.Authentication.ApiKeyHeader
[![Build status](https://dev.azure.com/ktos/Ktos.AspNetCore.Authentication.ApiKeyHeader/_apis/build/status/Ktos.AspNetCore.Authentication.ApiKeyHeader-CI)](https://dev.azure.com/ktos/Ktos.AspNetCore.Authentication.ApiKeyHeader/_build/latest?definitionId=4)
[![NuGet](https://img.shields.io/nuget/v/Ktos.AspNetCore.Authentication.ApiKeyHeader.svg)](https://www.nuget.org/packages/Ktos.AspNetCore.Authentication.ApiKeyHeader/)

Authentication for ASP.NET Core Security using HTTP header and simple key

This authentication method, while very simple, is of course not suitable for 
production systems due to potential insecurities. However, as fast and simple 
method, still could be useful in situations you don't wan't to use token-based
security.

Sample usage:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
    })
        .AddApiKeyHeaderAuthentication(options => options.ApiKey = "my-secret-api-key");
}
```

It requires you to authenticate to sent `X-APIKEY` header along with your 
request, with a value equal to the secret key you set in options.

Requesting usage of custom header (`x-api-key`, not `X-APIKEY`):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
    })
        .AddApiKeyHeaderAuthentication(options => { options.ApiKey = "my-secret-api-key"; options.Header = "x-api-key"; );
}
```

Of course, you have to ensure your controller or actions are expecting user to 
be authenticated, for example you can use `[Authorize]` attribute, as well as 
remember about using `app.UseAuthentication()` before your `app.UseMvc()`. 
[See Microsoft docs](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-2.1) for more.

# License

Licensed under MIT

Have a lot of fun.
--ktos