using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace Casimodo.Lib.Web.Pdf.Auth
{
    public static class MyBasicAuthHelper
    {
        public static MyBasicAuthResult Evaluate(HttpRequest request, MyBasicAuthUserInfo userInfo)
        {
            try
            {
                var authHeaderValue = request.Headers[HeaderNames.Authorization];
                if (string.IsNullOrEmpty(authHeaderValue))
                    return MyBasicAuthResult.NotAttempted;

                var authorization = AuthenticationHeaderValue.Parse(authHeaderValue);
                if (authorization.Scheme != "Basic")
                    return MyBasicAuthResult.NotAttempted;

                if (string.IsNullOrEmpty(authorization.Parameter))
                    return MyBasicAuthResult.MissingCredentials;

                var userNameAndPassword = ExtractUserNameAndPassword(authorization.Parameter);
                if (userNameAndPassword == null)
                    return MyBasicAuthResult.InvalidCredentials;

                string userName = userNameAndPassword.Item1;
                string password = userNameAndPassword.Item2;

                if (string.Equals(userName, userInfo.UserName, StringComparison.Ordinal) &&
                    string.Equals(password, userInfo.UserPassword, StringComparison.Ordinal))
                    return MyBasicAuthResult.Ok;

                return MyBasicAuthResult.InvalidUserNameOrPassword;
            }
            catch
            {
                return MyBasicAuthResult.InvalidHeader;
            }
        }

        static Tuple<string, string> ExtractUserNameAndPassword(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return null;
            }

            var encoding = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            string decodedCredentials;
            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(decodedCredentials))
                return null;

            int colonIndex = decodedCredentials.IndexOf(':');
            if (colonIndex < 0)
                return null;

            string userName = decodedCredentials.Substring(0, colonIndex);
            string password = decodedCredentials.Substring(colonIndex + 1);

            return new Tuple<string, string>(userName, password);
        }
    }
}

