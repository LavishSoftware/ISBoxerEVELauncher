using ISBoxerEVELauncher.Enums;

namespace ISBoxerEVELauncher.Interface
{
    public interface ILauncher
    {
        LoginResult Launch(ILaunchTarget launchTarget);
        string LauncherText
        {
            get;
        }
    }
}
