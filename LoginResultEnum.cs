using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher
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
