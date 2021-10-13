namespace ISBoxerEVELauncher.Enums
{
    public enum LoginResult
    {
        Success,
        Error,
        Timeout,
        InvalidUsernameOrPassword,
        InvalidCharacterChallenge,
        InvalidAuthenticatorChallenge,
        InvalidEmailVerificationChallenge,
        EULADeclined,
        EmailVerificationRequired,
        SecurityWarningClosed,
        TokenFailure,
    }
}
