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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

// based on https://github.com/bruno-garcia/Bazinga.AspNetCore.Authentication.Basic/blob/master/test/TestBed.cs

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader.Tests
{
    internal static class TestBed
    {
        public static void SetApiKey(this HttpClient client, string apikey, string header = ApiKeyHeaderAuthenticationDefaults.AuthenticationHeader)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add(header, apikey);
        }

        public static HttpClient GetClient()
        {
            return GetClient((ApiKeyHeaderAuthenticationOptions options) => { options.ApiKey = "testapi"; });
        }

        public static HttpClient GetClient(Action<AuthenticationBuilder> builderAction)
        {
            return CreateServer(builderAction).CreateClient();
        }

        public static HttpClient GetClient(Action<ApiKeyHeaderAuthenticationOptions> options)
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