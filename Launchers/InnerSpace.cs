using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.InnerSpace;
using ISBoxerEVELauncher.Interface;
using System;


namespace ISBoxerEVELauncher.Launchers
{

    public class InnerSpace : ILauncher
    {
        public InnerSpace(InnerSpaceGameProfile gameProfile, DirectXVersion dxVersion, bool useSingularity)
        {
            GameProfile = gameProfile;
            UseDirectXVersion = dxVersion;
            UseSingularity = useSingularity;
        }

        public InnerSpaceGameProfile GameProfile
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
            return launchTarget.EVEAccount.Launch(GameProfile.Game, GameProfile.GameProfile, App.Settings.UseSingularity, UseDirectXVersion, launchTarget.CharacterID);

            throw new NotImplementedException();
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