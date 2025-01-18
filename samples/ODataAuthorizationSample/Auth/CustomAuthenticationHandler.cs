using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Principal;

namespace ODataAuthorizationSample.Auth
{
    // our customer authentication handler
    internal class CustomAuthenticationHandler : AuthenticationHandler<CustomAuthenticationOptions>
    {
        public CustomAuthenticationHandler(IOptionsMonitor<CustomAuthenticationOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new System.Security.Principal.GenericIdentity("Me");
            // in this dummy authentication scheme, we assume that the permissions granted
            // to the user are stored as a comma-separate list in a header called Permissions
            var scopeValues = Request.Headers["Permissions"];
            if (scopeValues.Count != 0)
            {
                var scopes = scopeValues.ToArray()[0].Split(",").Select(s => s.Trim());
                var claims = scopes.Select(scope => new Claim("Scope", scope));
                identity.AddClaims(claims);
            }

            var principal = new GenericPrincipal(identity, Array.Empty<string>());
            // we use the same auhentication scheme as the one specified in the OData model permissions
            var ticket = new AuthenticationTicket(principal, "AuthScheme");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    internal class CustomAuthenticationOptions : AuthenticationSchemeOptions
    {
    }
}