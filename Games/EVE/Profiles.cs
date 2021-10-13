using System.Collections.Generic;

namespace ISBoxerEVELauncher.Games.EVE
{
    public class Profiles
    {
        List<string> profiles;

        public Profiles()
        {
            profiles = new List<string>();
            profiles.Add("Default");
        }

        public void LoadProfiles()
        {
            var path = App.Settings.EVESharedCachePath;
        }

    }
}
