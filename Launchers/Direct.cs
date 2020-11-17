using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Interface;
using System;

namespace ISBoxerEVELauncher.Launchers
{
    public class Direct : ILauncher
    {
        public Direct(string sharedCachePath, DirectXVersion dxVersion, bool useSingularity)
        {
            SharedCachePath = sharedCachePath;
            UseDirectXVersion = dxVersion;
            UseSingularity = useSingularity;
        }

        public string SharedCachePath
        {
            get; set;
        }
        public DirectXVersion UseDirectXVersion
        {
            get; set;
        }
        public bool UseSingularity
        {
            get; set;
        }

        public LoginResult Launch(ILaunchTarget launchTarget)
        {
            return launchTarget.EVEAccount.Launch(SharedCachePath, App.Settings.UseSingularity, UseDirectXVersion, launchTarget.CharacterID);
        }

        public string LauncherText
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
