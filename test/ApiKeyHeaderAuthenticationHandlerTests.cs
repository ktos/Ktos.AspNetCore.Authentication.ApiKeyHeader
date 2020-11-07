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
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader.Tests
{
    public class ApiKeyHeaderAuthenticationHandlerTests
    {
        public const string TestApiKey = "testapi";

        [Fact]
        public async Task EmptyApiKeyReturns401()
        {
            var client = TestBed.GetClientWithOptions(options => options.ApiKey = TestApiKey);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsReturns401()
        {
            var client = TestBed.GetClientWithOptions(options => options.ApiKey = TestApiKey);
            client.UseApiKey("wrongkey");
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAuthorize()
        {
            var client = TestBed.GetClientWithOptions(options => options.ApiKey = TestApiKey);
            client.UseApiKey(TestApiKey);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ApiKeyHeaderAuthenticationDefaults.AuthenticationClaimName, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomHeaderAuthorize()
        {
            const string key = "testapi";
            const string header = "X-API-KEY";

            var client = TestBed.GetClientWithOptions(options => { options.ApiKey = key; options.Header = header; });
            client.UseApiKey(key, header);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ApiKeyHeaderAuthenticationDefaults.AuthenticationClaimName, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomHeaderReturns401()
        {
            const string key = "testapi";
            const string wrongkey = "wrongkey";
            const string header = "X-API-KEY";

            var client = TestBed.GetClientWithOptions(options => { options.ApiKey = key; options.Header = header; });
            client.UseApiKey(wrongkey, header);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicAuthorize()
        {
            const string key = "goodkey";
            const string key2 = "goodkey2";

            var client = TestBed.GetClientWithOptions(options => { options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.UseApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.UseApiKey(key2);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key2, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicProperlySetClaims()
        {
            const string key = "goodkey";
            const string claimName = "John";

            var client = TestBed.GetClientWithOptions(options => { options.CustomAuthenticationHandler = _ => (true, claimName); });
            client.UseApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(claimName, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicProperlySetClaimsInContext()
        {
            const string key = "goodkey";
            const string claimName = "John";

            var client = TestBed.GetClientWithOptions(options => { options.CustomAuthenticationHandler = _ => (true, claimName); });
            client.UseApiKey(key);
            var response = await client.GetAsync(TestBed.FullUserPath);

            var content = await response.Content.ReadAsStringAsync();

            var user = JsonDocument.Parse(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(claimName, user.RootElement.GetProperty("Name").GetString());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationFullTicketProperlySetOtherClaimsInTicket()
        {
            const string key = "goodkey";

            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
                builder.Services.AddSingleton<IApiKeyCustomAuthenticationTicketHandler, CustomFullTicketHandler>();
            });

            client.UseApiKey(key);
            var response = await client.GetAsync(TestBed.FullTicketPrincipalClaimsPath);

            var content = await response.Content.ReadAsStringAsync();

            var claims = JsonDocument.Parse(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", claims.RootElement[0].GetProperty("Type").GetString());
            Assert.Equal(CustomFullTicketHandler.TestUserName, claims.RootElement[0].GetProperty("Value").GetString());
            Assert.Equal("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", claims.RootElement[1].GetProperty("Type").GetString());
            Assert.Equal(CustomFullTicketHandler.TestRole, claims.RootElement[1].GetProperty("Value").GetString());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationFullTicketProperlySetTicketProperties()
        {
            const string key = "goodkey";

            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
                builder.Services.AddSingleton<IApiKeyCustomAuthenticationTicketHandler, CustomFullTicketHandler>();
            });
            client.UseApiKey(key);

            var response = await client.GetAsync(TestBed.FullTicketPropertiesPath);

            var content = await response.Content.ReadAsStringAsync();

            var claims = JsonDocument.Parse(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(CustomFullTicketHandler.Redirect, claims.RootElement.GetProperty("Items").GetProperty(".redirect").GetString());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationFullTicketProperlySetClaimsInContext()
        {
            const string key = "goodkey";
            const string claimName = "John";

            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
                builder.Services.AddSingleton<IApiKeyCustomAuthenticationTicketHandler, CustomFullTicketHandler>();
            });

            client.UseApiKey(key);
            var response = await client.GetAsync(TestBed.FullUserPath);
            var content = await response.Content.ReadAsStringAsync();

            var user = JsonDocument.Parse(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(claimName, user.RootElement.GetProperty("Name").GetString());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicReturningNullNameThrows()
        {
            const string key = "goodkey";

            var client = TestBed.GetClientWithOptions(options => options.CustomAuthenticationHandler = (_) => (true, null));
            client.UseApiKey(key);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetAsync("/"));
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationServiceAuthorize()
        {
            const string key = "testapi";

            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
                builder.Services.AddSingleton<IApiKeyCustomAuthenticator, TestApiKeyService>();
            });

            client.UseApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key.ToUpper(), await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndNoRegisteredAuthenticationServiceReturns401()
        {
            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
            });

            client.UseApiKey("testapi");
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomAuthenticationServiceReturns401()
        {
            const string key = "badapi";

            var client = TestBed.GetClientWithBuilder(builder =>
            {
                builder.AddApiKeyHeaderAuthentication(options => options.UseRegisteredAuthenticationHandler = true);
                builder.Services.AddSingleton<IApiKeyCustomAuthenticator, TestApiKeyService>();
            });

            client.UseApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicAndCustomHeaderAuthorize()
        {
            const string key = "goodkey";
            const string key2 = "goodkey2";
            const string customHeader = "X-CUSTOM-HEADER";

            var client = TestBed.GetClientWithOptions(options => { options.Header = customHeader; options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.UseApiKey(key, customHeader);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.UseApiKey(key2, customHeader);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key2, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomAuthenticationLogicReturns401()
        {
            const string key = "goodkey";
            const string key2 = "badkey";

            var client = TestBed.GetClientWithOptions(options => { options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.UseApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.UseApiKey(key2);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomAuthenticationLogicAndCustomHeaderReturns401()
        {
            const string key = "goodkey";
            const string key2 = "badkey";
            const string customHeader = "X-CUSTOM-HEADER";

            var client = TestBed.GetClientWithOptions(options =>
            {
                options.Header = customHeader;
                options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic;
            });

            client.UseApiKey(key, customHeader);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.UseApiKey(key2, customHeader);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        private (bool, string) SimpleCustomAuthenticationLogic(string apiKey)
        {
            return (apiKey.StartsWith("good"), apiKey);
        }
    }
}