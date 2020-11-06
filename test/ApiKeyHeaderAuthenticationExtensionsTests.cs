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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader.Tests
{
    public class ApiKeyHeaderAuthenticationExtensionsTests
    {
        [Fact]
        public Task AddBasicAuthentication_VerifyValuesOnlyApiKeySet()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddApiKeyHeaderAuthentication(options => options.ApiKey = "test");
            return AssertConfiguration(services);
        }

        [Fact]
        public Task AddBasicAuthentication_VerifyValuesCustomHeaderSet()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddApiKeyHeaderAuthentication(options => { options.ApiKey = "test"; options.Header = "X-API-KEY"; });
            return AssertConfiguration(services);
        }

        private async Task<IServiceProvider> AssertConfiguration(
            IServiceCollection services,
            string expectedScheme = ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme,
            Type handlerType = null)
        {
            handlerType = handlerType ?? typeof(ApiKeyHeaderAuthenticationHandler);

            var sp = services.BuildServiceProvider();

            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(expectedScheme);
            Assert.NotNull(scheme);
            Assert.Same(handlerType, scheme.HandlerType);

            return sp;
        }
    }
}