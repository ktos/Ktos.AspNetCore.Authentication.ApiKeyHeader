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
    /// Some defaults for the ApiKey Header Authentication schemea
    /// </summary>
    public static class ApiKeyHeaderAuthenticationDefaults
    {
        /// <summary>
        /// Default name for the Authentication Scheme
        /// </summary>
        public const string AuthenticationScheme = "ApiKeyHeader";

        /// <summary>
        /// Default header which is being checked for the key
        /// </summary>
        public const string AuthenticationHeader = "X-APIKEY";

        /// <summary>
        /// Name set for claim when authentication is successful
        /// </summary>
        public const string AuthenticationClaimName = "ApiKeyHeader User";
    }

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

    /// <summary>
    /// Defines the type of the function the custom API key authentication logic must follow
    /// </summary>
    public interface IApiKeyCustomAuthenticator
    {
        /// <summary>
        /// <para>Custom function used for checking if the provided API key should be authenticated.</para>
        /// <para>
        /// Must return a tuple of string and bool, which are name of the authenticate user used in created
        /// ticket and result of the authentication. May be used for checking multiple authentication keys
        /// for multiple users or for adding custom logic along with authentication, like additional logging.
        /// </para>
        Func<string, (string, bool)> CustomAuthenticationHandler { get; }
    }

    /// <summary>
    /// Options for the ApiKeyHeader authentication scheme
    /// </summary>
    public class ApiKeyHeaderAuthenticationOptions : AuthenticationSchemeOptions, IApiKeyCustomAuthenticator
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
        public Func<string, (string, bool)> CustomAuthenticationHandler { get; set; }

        /// <summary>
        /// Defines a custom authentication type implementing IApiKeyCustomAuthenticator which will be accessed
        /// from the current services library and used to authenticate the request
        /// </summary>
        public Type CustomAuthenticatorType { get; set; }
    }

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
            else if (Options.CustomAuthenticatorType != null)
            {
                var service = Context.RequestServices.GetService(Options.CustomAuthenticatorType) as IApiKeyCustomAuthenticator;
                if (service == null)
                {
                    throw new InvalidCastException("Failed to use provided custom authenticator type as IApiKeyCustomAuthenticator");
                }

                (var claimName, var result) = service.CustomAuthenticationHandler(headerKey);
                if (result)
                {
                    return AuthenticateResult.Success(CreateAuthenticationTicket(claimName));
                }
                else
                {
                    return AuthenticateResult.NoResult();
                }
            }
            else if (Options.CustomAuthenticationHandler != null)
            {
                (var claimName, var result) = Options.CustomAuthenticationHandler(headerKey);
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