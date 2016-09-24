using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Launchers
{
    public interface ILauncher
    {
        EVEAccount.LoginResult LaunchAccount(EVEAccount account);
        string LauncherText { get; }
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

        public EVEAccount.LoginResult LaunchAccount(EVEAccount account)
        {
            return account.Launch(GameProfile.Game, GameProfile.GameProfile, App.Settings.UseSingularity, UseDirectXVersion);

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

        public EVEAccount.LoginResult LaunchAccount(EVEAccount account)
        {
            return account.Launch(SharedCachePath, App.Settings.UseSingularity, UseDirectXVersion);
        }

        public string LauncherText
        {
            get { throw new NotImplementedException(); }
        }
    }
}
