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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Ktos.AspNetCore.Authentication.ApiKeyHeader
{
    /// <summary>
    /// Function returning if provided API key is valid or not
    /// </summary>
    /// <param name="apiKey">API key sent along with HTTP request</param>
    /// <returns>Pair of bool, string, where bool defines if API key was valid and string
    /// will be used as principal name in provided claims</returns>
    public delegate (bool, string) CustomApiKeyHandlerDelegate(string apiKey);

    /// <summary>
    /// Handles ApiKeyHeader authentication scheme
    /// </summary>
    public class ApiKeyHeaderAuthenticationHandler : AuthenticationHandler<ApiKeyHeaderAuthenticationOptions>
    {
        /// <summary>
        /// Initializes a new instance of ApiKeyHeaderAuthenticationHandler
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        public ApiKeyHeaderAuthenticationHandler(IOptionsMonitor<ApiKeyHeaderAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        /// <summary>
        /// Handles authentication by checking if there is proper api key set in HTTP header
        /// </summary>
        /// <returns>Returns Claim with name if authentication was successful or NoResult of not</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var headerKey = Context.Request.Headers[Options.Header].FirstOrDefault();
            if (headerKey == null)
            {
                return AuthenticateResult.NoResult();
            }
            else if (Options.CustomAuthenticatorType != null || Options.CustomAuthenticationHandler != null)
            {
                bool result = false;
                string claimName = null;

                if (Options.CustomAuthenticatorType != null)
                {
                    var service = Context.RequestServices.GetService(Options.CustomAuthenticatorType) as IApiKeyCustomAuthenticator;
                    if (service == null)
                    {
                        throw new InvalidCastException("Failed to use provided custom authenticator type as IApiKeyCustomAuthenticator");
                    }

                    (result, claimName) = service.CustomAuthenticationHandler(headerKey);
                }

                if (Options.CustomAuthenticationHandler != null)
                {
                    (result, claimName) = Options.CustomAuthenticationHandler(headerKey);
                }

                if (result)
                {
                    return AuthenticateResult.Success(CreateAuthenticationTicket(claimName));
                }
                else
                {
                    return AuthenticateResult.NoResult();
                }
            }
            else if (headerKey == Options.ApiKey)
            {
                return AuthenticateResult.Success(CreateAuthenticationTicket());
            }
            else
            {
                return AuthenticateResult.NoResult();
            }
        }

        private AuthenticationTicket CreateAuthenticationTicket(string claimName = ApiKeyHeaderAuthenticationDefaults.AuthenticationClaimName)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, claimName) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var at = new AuthenticationTicket(principal, ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme);
            Context.User.AddIdentity(new ClaimsIdentity(ApiKeyHeaderAuthenticationDefaults.AuthenticationScheme));
            return at;
        }
    }
}