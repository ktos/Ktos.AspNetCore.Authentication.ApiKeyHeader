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
using System.Threading.Tasks;
using Xunit;

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader.Tests
{
    public class ApiKeyHeaderAuthenticationHandlerTests
    {
        [Fact]
        public async Task EmptyApiKeyReturns401()
        {
            var client = TestBed.GetClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsReturns401()
        {
            var client = TestBed.GetClient();
            client.SetApiKey("wrongkey");
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAuthorize()
        {
            const string key = "testapi";
            var client = TestBed.GetClient();
            client.SetApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ApiKeyHeaderAuthenticationDefaults.AuthenticationClaimName, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomHeaderAuthorize()
        {
            const string key = "testapi";
            const string header = "X-API-KEY";

            var client = TestBed.GetClient(options => { options.ApiKey = key; options.Header = header; });
            client.SetApiKey(key, header);
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

            var client = TestBed.GetClient(options => { options.ApiKey = key; options.Header = header; });
            client.SetApiKey(wrongkey, header);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicAuthorize()
        {
            const string key = "goodkey";
            const string key2 = "goodkey2";

            var client = TestBed.GetClient(options => { options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.SetApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.SetApiKey(key2);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key2, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicProperlySetClaims()
        {
            const string key = "goodkey";
            const string claimName = "John";

            var client = TestBed.GetClient(options => { options.CustomAuthenticationHandler = _ => (true, claimName); });
            client.SetApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(claimName, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationLogicReturningNullNameThrows()
        {
            const string key = "goodkey";

            var client = TestBed.GetClient(options => options.CustomAuthenticationHandler = (_) => (true, null));
            client.SetApiKey(key);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetAsync("/"));
        }

        [Fact]
        public async Task ValidCredentialsAndCustomAuthenticationServiceAuthorize()
        {
            const string key = "testapi";

            var client = TestBed.GetClient(options => options.CustomAuthenticatorType = typeof(TestApiKeyService));
            client.SetApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key.ToUpper(), await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ValidCredentialsAndBadTypeOfAuthenticationServiceThrows()
        {
            var client = TestBed.GetClient(options => options.CustomAuthenticatorType = typeof(object));
            client.SetApiKey("testapi");
            await Assert.ThrowsAsync<InvalidCastException>(async () => await client.GetAsync("/"));
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomAuthenticationServiceReturns401()
        {
            const string key = "badapi";

            var client = TestBed.GetClient(options => options.CustomAuthenticatorType = typeof(TestApiKeyService));
            client.SetApiKey(key);
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

            var client = TestBed.GetClient(options => { options.Header = customHeader; options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.SetApiKey(key, customHeader);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.SetApiKey(key2, customHeader);
            response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key2, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCredentialsAndCustomAuthenticationLogicReturns401()
        {
            const string key = "goodkey";
            const string key2 = "badkey";

            var client = TestBed.GetClient(options => { options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic; });
            client.SetApiKey(key);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.SetApiKey(key2);
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

            var client = TestBed.GetClient(options =>
            {
                options.Header = customHeader;
                options.CustomAuthenticationHandler = SimpleCustomAuthenticationLogic;
            });

            client.SetApiKey(key, customHeader);
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(key, await response.Content.ReadAsStringAsync());

            client.SetApiKey(key2, customHeader);
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