# Ktos.AspNetCore.Authentication.ApiKeyHeader

[![Build Status](https://dev.azure.com/ktos/Ktos.AspNetCore.Authentication.ApiKeyHeader/_apis/build/status/ApiKeyHeader%20Tag?branchName=master)](https://dev.azure.com/ktos/Ktos.AspNetCore.Authentication.ApiKeyHeader/_build/latest?definitionId=8&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/Ktos.AspNetCore.Authentication.ApiKeyHeader.svg)](https://www.nuget.org/packages/Ktos.AspNetCore.Authentication.ApiKeyHeader/)

Authentication for ASP.NET Core Security using HTTP header like `X-APIKEY` and
simple single provided key.

This authentication method, while very simple, is of course not suitable for
production systems due to potential insecurities. However, as fast and simple
method, still could be useful in situations you don't wan't to use token-based
security.

## Sample usage

Configure your authentication method in `ConfigureServices()`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ...
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
    })
        .AddApiKeyHeaderAuthentication(options => options.ApiKey = "my-secret-api-key");

    // ...
}
```

And use it in `Configure()`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    app.UseAuthentication();
    app.UseAuthorization();

    // ...
}
```

It requires you to authenticate to sent `X-APIKEY` header along with your
request, with a value equal to the secret key you set in options, or the request
will fail with `401 Unauthorized` HTTP error.

Of course, *you* have to ensure your controller or actions are expecting user to
be authenticated, for example you can use `[Authorize]`.

[See Microsoft docs](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-3.1) for more.

### Custom header

You can use any header, by configuring options, e.g. you can set you want
`x-api-key`, not `X-APIKEY`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ...
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
    })
        .AddApiKeyHeaderAuthentication(options => { options.ApiKey = "my-secret-api-key"; options.Header = "x-api-key"; });

    // ...
}
```

### Custom logic (simple)

You may use option CustomAuthenticationHandler to provide a function which will be used

### Custom logic

In Startup.cs you may provide a type, implementing an
`IApiKeyCustomAuthenticator` interface, which will be acquired from current
request services to be used for authentication.

In that case, you may use it for database connection, checking users api key
usage limits or similar things before authentication.

```csharp
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme;
})
.AddApiKeyHeaderAuthentication(options => options.CustomAuthenticatorType = typeof(TestApiKeyService));

services.AddSingleton<TestApiKeyService>();
```

The class implementing `IApiKeyCustomAuthenticator` is being created with
dependency injection, so it may access database or other services useful for
your own authentication logic.

The interface requires for you implementation of a method returning a pair of
bool and string, where bool indicates if the authentication was successful, and
in the string you may provide a name, which will be used in created
`AuthenticationTicket.Claims`, so you may differentiate between different users.

In this example, a simple `ILogger<T>` is used:

```csharp
internal class TestApiKeyService : IApiKeyCustomAuthenticator
{
    public TestApiKeyService(ILogger<TestApiKeyService> logging)
    {
        logging.LogInformation("Created a test authenticator");
    }

    // returns true on "testapi", returns uppercase key as name, false in any other case
    public CustomApiKeyHandlerDelegate CustomAuthenticationHandler => (key) => key == "testapi" ? (true, key.ToUpper()) : (false, null);
}
```

Using both `CustomAuthenticatorType` and `ApiKey` options means only custom
logic is used.

If both `CustomAuthenticationHandler` and `CustomAuthenticatorType` are defined,
only **type** is used.

## License

Licensed under MIT

Have a lot of fun.
--ktos
