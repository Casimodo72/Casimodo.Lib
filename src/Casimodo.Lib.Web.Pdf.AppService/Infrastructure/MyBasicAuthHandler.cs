using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Casimodo.Lib.Web.Pdf.Auth
{
    public class MyBasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        readonly MyBasicAuthUserInfo _userInfo;

        public MyBasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            MyBasicAuthUserInfo userInfo)
            : base(options, logger, encoder, clock)
        {
            _userInfo = userInfo;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authResult = MyBasicAuthHelper.Evaluate(Request, _userInfo);
            if (authResult != MyBasicAuthResult.Ok)
            {
                string errorMsg = "";
                if (authResult == MyBasicAuthResult.MissingCredentials)
                    errorMsg = "Missing credentials";
                else if (authResult == MyBasicAuthResult.InvalidCredentials)
                    errorMsg = "Invalid credentials";
                else if (authResult == MyBasicAuthResult.InvalidUserNameOrPassword)
                    errorMsg = "Invalid user name or password";
                else if (authResult == MyBasicAuthResult.InvalidHeader)
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

