using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //path = var System.IO.Directory = App.Settings.EVESharedCachePath
        }

    }
}
