namespace Casimodo.Lib.Web.Auth
{
    public enum BasicAuthResult
    {
        Ok,
        NotAttempted,
        MissingCredentials,
        InvalidCredentials,
        InvalidUserNameOrPassword,
        InvalidHeader
    }
}

