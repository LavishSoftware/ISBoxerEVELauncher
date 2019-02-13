using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ISBoxerEVELauncher.Enums;

namespace ISBoxerEVELauncher.InnerSpace
{


    public class InnerSpaceGameProfile
    {
        public InnerSpaceGameProfile()
        {

        }

        public string Game { get; set; }
        public string GameProfile { get; set; }

        [XmlIgnore]
        public RelatedExecutable Executable
        {
            get
            {
                Set set = App.FindGameProfileSet(Game, GameProfile);
                if (set == null)
                    return RelatedExecutable.InvalidGameProfile;

                Setting executableSetting = set.FindSetting("Executable");
                if (executableSetting == null)
                    return RelatedExecutable.InvalidGameProfile;

                switch(executableSetting.Value.ToLowerInvariant())
                {
                    case "eve.exe":
                        return RelatedExecutable.EVELauncher;
                    case "evelauncher.exe":
                    case "eve launcher.exe":
                        return RelatedExecutable.EVELauncher;
                    case "exefile.exe":
                        return RelatedExecutable.EXEFile;
                    case "isboxerevelauncher.exe":
                    case "isboxer eve launcher.exe":
                        return RelatedExecutable.ISBoxerEVELauncher;
                    case "innerspace.exe":
                        return RelatedExecutable.InnerSpace;
                }
                return RelatedExecutable.Other;
            }
        }

        public override string ToString()
        {
            return GameProfile;
        }
    }
}
