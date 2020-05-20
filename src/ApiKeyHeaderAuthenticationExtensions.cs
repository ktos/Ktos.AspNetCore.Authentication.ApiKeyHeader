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
using System;

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader
{
    /// <summary>
    /// Class with extensions for AuthenticationBuilder
    /// </summary>
    public static class ApiKeyHeaderAuthenticationExtensions
    {
        /// <summary>
        /// Adds a ApiKeyHeader authentication method, where user must provide
        /// a valid key in X-APIKEY request header or similar to be authenticaed
        /// successfully.
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        /// <param name="configureOptions">Options for the ApiKeyHeader authentication</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddApiKeyHeaderAuthentication(this AuthenticationBuilder builder, Action<ApiKeyHeaderAuthenticationOptions> configureOptions) => builder.AddScheme<ApiKeyHeaderAuthenticationOptions, ApiKeyHeaderAuthenticationHandler>(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme, ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme, configureOptions);
    }
}