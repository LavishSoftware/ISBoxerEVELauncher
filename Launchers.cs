using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Launchers
{
    public interface ILauncher
    {
        EVEAccount.LoginResult Launch(ILaunchTarget launchTarget);
        string LauncherText { get; }
    }

    public interface ILaunchTarget
    {
        EVEAccount EVEAccount { get; }
        long CharacterID { get; }
    }

    public class InnerSpaceLauncher : ILauncher
    {
        public InnerSpaceLauncher(InnerSpaceGameProfile gameProfile, DirectXVersion dxVersion, bool useSingularity)
        {
            GameProfile = gameProfile;
            UseDirectXVersion = dxVersion;
            UseSingularity = useSingularity;
        }

        public InnerSpaceGameProfile GameProfile { get; set; }
        public DirectXVersion UseDirectXVersion { get; set; }
        public bool UseSingularity { get; set; }

        public EVEAccount.LoginResult Launch(ILaunchTarget launchTarget)
        {
            return launchTarget.EVEAccount.Launch(GameProfile.Game, GameProfile.GameProfile, App.Settings.UseSingularity, UseDirectXVersion, launchTarget.CharacterID);

            throw new NotImplementedException();
        }

        public string LauncherText
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class DirectLauncher : ILauncher
    {
        public DirectLauncher(string sharedCachePath, DirectXVersion dxVersion, bool useSingularity)
        {
            SharedCachePath = sharedCachePath;
            UseDirectXVersion = dxVersion;
            UseSingularity = useSingularity;
        }

        public string SharedCachePath { get; set; }
        public DirectXVersion UseDirectXVersion { get; set; }
        public bool UseSingularity { get; set; }

        public EVEAccount.LoginResult Launch(ILaunchTarget launchTarget)
        {
            return launchTarget.EVEAccount.Launch(SharedCachePath, App.Settings.UseSingularity, UseDirectXVersion, launchTarget.CharacterID);
        }

        public string LauncherText
        {
            get { throw new NotImplementedException(); }
        }
    }
}
