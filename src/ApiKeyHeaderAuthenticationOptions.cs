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

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader
{
    /// <summary>
    /// Options for the ApiKeyHeader authentication scheme
    /// </summary>
    public class ApiKeyHeaderAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// The key user must provide in X-APIKEY header
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The header which is being checked for valid key, by default is X-APIKEY
        /// </summary>
        public string Header { get; set; } = ApiKeyHeaderAuthenticationDefaults.AuthenticationHeader;

        /// <summary>
        /// <para>Custom function used for checking if the provided API key should be authenticated.</para>
        /// <para>
        /// Must return a tuple of string and bool, which are name of the authenticate user used in created
        /// ticket and result of the authentication. May be used for checking multiple authentication keys
        /// for multiple users or for adding custom logic along with authentication, like additional logging.
        /// </para>
        /// <para>Using this option overrides usage of ApiKey, which is not being checked at all, only custom
        /// logic is fired.
        /// </para>
        /// </summary>
        public CustomApiKeyHandlerDelegate CustomAuthenticationHandler { get; set; }

        /// <summary>
        /// Defines if the logic should search for registered implementation of <see cref="IApiKeyCustomAuthenticator"/>
        /// or <see cref="IApiKeyCustomAuthenticationTicketHandler"/> to handle authentication.
        /// <para>
        /// This option overrides usage of <see cref="CustomAuthenticationHandler"/>.
        /// </para>
        /// </summary>
        public bool UseRegisteredAuthenticationHandler { get; set; }
    }
}