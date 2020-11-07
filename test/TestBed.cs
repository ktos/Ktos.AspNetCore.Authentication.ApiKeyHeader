#region License

/*
 * Ktos.AspNetCore.Authentication.ApiKeyHeader
 *
 * Copyright (C) Marcin Badurowicz <m at badurowicz dot net> 2018
 *
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files
 * (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
 * BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
 * ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#endregion License

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// based on https://github.com/bruno-garcia/Bazinga.AspNetCore.Authentication.Basic/blob/master/test/TestBed.cs

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader.Tests
{
    internal class TestApiKeyService : IApiKeyCustomAuthenticator
    {
        private readonly ILogger logger;

        public TestApiKeyService(ILogger<TestApiKeyService> logger)
        {
            this.logger = logger;
            this.logger.LogInformation("Created a test authenticator");
        }

        // returns true on "testapi", returns uppercase key as name, false in any other case
        public CustomApiKeyHandlerDelegate CustomAuthenticationHandler => (key) =>
        {
            logger.LogDebug($"Someone tried to authenticate with API key: {key}");
            return key == "testapi" ? (true, key.ToUpper()) : (false, null);
        };
    }

    internal class CustomFullTicketHandler : IApiKeyCustomAuthenticationTicketHandler
    {
        public const string TestUserName = "John";
        public const string TestRole = "testrole";
        public const string Redirect = "http://localhost";

        public AuthenticateResult CustomAuthenticationHandler(string key)
        {
            if (key == "goodkey")
                return AuthenticateResult.Success(CreateAuthenticationTicket(TestUserName));

            if (key == "badkey")
                return AuthenticateResult.Fail("failed");

            return AuthenticateResult.NoResult();
        }

        private AuthenticationTicket CreateAuthenticationTicket(string claimName = ApiKeyHeaderAuthenticationDefaults.AuthenticationClaimName)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, claimName), new Claim(ClaimTypes.Role, TestRole) };
            var identity = new ClaimsIdentity(claims, ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);

            ticket.Properties.RedirectUri = Redirect;

            return ticket;
        }
    }

    internal static class TestBed
    {
        public const string FullUserPath = "/fulluser";
        public const string FullTicketPrincipalClaimsPath = "/fullticketprincipalclaims";
        public const string FullTicketPropertiesPath = "/fullticketproperties";

        public static void UseApiKey(this HttpClient client, string apikey, string header = ApiKeyHeaderAuthenticationDefaults.AuthenticationHeader)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add(header, apikey);
        }

        public static HttpClient GetClientWithBuilder(Action<AuthenticationBuilder> builderAction)
        {
            return CreateServer(builderAction).CreateClient();
        }

        public static HttpClient GetClientWithOptions(Action<ApiKeyHeaderAuthenticationOptions> options)
        {
            return CreateServer(b => b.AddApiKeyHeaderAuthentication(options)).CreateClient();
        }

        public static TestServer CreateServer(Action<AuthenticationBuilder> builderAction)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == new PathString("/"))
                        {
                            var result = await context.AuthenticateAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            if (!result.Succeeded)
                            {
                                await context.ChallengeAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            }
                            else
                            {
                                await context.Response.WriteAsync(result.Ticket.Principal.Identity.Name);
                            }
                        }
                        else if (context.Request.Path == new PathString(FullUserPath))
                        {
                            var result = await context.AuthenticateAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            if (!result.Succeeded)
                            {
                                await context.ChallengeAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            }
                            else
                            {
                                await context.Response.WriteAsync(JsonSerializer.Serialize(context.User.Identity));
                            }
                        }
                        else if (context.Request.Path == new PathString(FullTicketPrincipalClaimsPath))
                        {
                            var result = await context.AuthenticateAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            if (!result.Succeeded)
                            {
                                await context.ChallengeAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            }
                            else
                            {
                                await context.Response.WriteAsync(JsonSerializer.Serialize(result.Ticket.Principal.Claims.Select(x => new { x.Type, x.Value })));
                            }
                        }
                        else if (context.Request.Path == new PathString(FullTicketPropertiesPath))
                        {
                            var result = await context.AuthenticateAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            if (!result.Succeeded)
                            {
                                await context.ChallengeAsync(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
                            }
                            else
                            {
                                await context.Response.WriteAsync(JsonSerializer.Serialize(result.Ticket.Properties));
                            }
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    builderAction(services.AddAuthentication(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme));
                });

            return new TestServer(builder);
        }
    }
}