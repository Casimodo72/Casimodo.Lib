using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Casimodo.Lib.Web.Auth
{
    public class BasicSingleUserAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        readonly BasicSingleUserAuthInfo _userInfo;

        public BasicSingleUserAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            BasicSingleUserAuthInfo userInfo)
            : base(options, logger, encoder, clock)
        {
            _userInfo = userInfo;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authResult = BasicSingleUserAuthHelper.Evaluate(Request, _userInfo);
            if (authResult != BasicAuthResult.Ok)
            {
                string errorMsg = "";
                if (authResult == BasicAuthResult.MissingCredentials)
                    errorMsg = "Missing credentials";
                else if (authResult == BasicAuthResult.InvalidCredentials)
                    errorMsg = "Invalid credentials";
                else if (authResult == BasicAuthResult.InvalidUserNameOrPassword)
                    errorMsg = "Invalid user name or password";
                else if (authResult == BasicAuthResult.InvalidHeader)
                    errorMsg = "Invalid authorization header";

                return Task.FromResult(AuthenticateResult.Fail(errorMsg));
            }

            var identity = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, _userInfo.UserId),
                    new Claim(ClaimTypes.Name, _userInfo.UserName),
                },
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

