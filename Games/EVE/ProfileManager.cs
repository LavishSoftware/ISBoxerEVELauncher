using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ISBoxerEVELauncher.Games.EVE
{
    public static class ProfileManager
    {
        public static void MigrateSettingsToISBEL()

        {
            var sharedCachePath = App.Settings.EVESharedCachePath;
            if (!string.IsNullOrWhiteSpace(sharedCachePath))
            {
                var ccpLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\ccp\eve";

                foreach (string d in System.IO.Directory.GetDirectories(sharedCachePath))
                {
                    if (System.IO.Path.GetFileName(d) == "duality")
                    {
                        var p = System.IO.Path.Combine(ccpLocal, sharedCachePath.Replace('\\', '_').Replace(":", "") + "du_duality");
                        CopyFile(p);
                    }
                    if (System.IO.Path.GetFileName(d) == "sisi")
                    {
                        var p = System.IO.Path.Combine(ccpLocal, sharedCachePath.Replace('\\', '_').Replace(":", "") + "sisi_singularity");
                        CopyFile(p);
                    }
                    if (System.IO.Path.GetFileName(d) == "tq")
                    {
                        var p = System.IO.Path.Combine(ccpLocal, sharedCachePath.Replace('\\', '_').Replace(":", "") + "tq_tranquility");
                        CopyFile(p);
                    }
                }
            }
        }

        private static void CopyFile(string p)
        {
            if (Directory.Exists(Path.Combine(p, "settings")) && !Directory.Exists(Path.Combine(p, "settings_ISBEL")))
            {
                Directory.CreateDirectory(Path.Combine(p, "settings_ISBEL"));
                foreach (string f in Directory.GetFiles(Path.Combine(p, "settings")))
                {
                    var dest = Path.Combine(p, "settings_ISBEL", Path.GetFileName(f));
                    File.Copy(f, dest);
                }
            }
        }
    }
}
