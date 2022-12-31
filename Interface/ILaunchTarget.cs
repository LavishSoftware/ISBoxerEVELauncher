using ISBoxerEVELauncher.Games.EVE;

namespace ISBoxerEVELauncher.Interface
{
    public interface ILaunchTarget
    {
        EVEAccount EVEAccount
        {
            get;
        }
        long CharacterID
        {
            get;
        }
    }
}
