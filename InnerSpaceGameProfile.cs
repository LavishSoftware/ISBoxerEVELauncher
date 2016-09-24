using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher
{
    public class InnerSpaceGameProfile
    {
        public InnerSpaceGameProfile()
        {

        }

        public string Game { get; set; }
        public string GameProfile { get; set; }

        public override string ToString()
        {
            return GameProfile;
        }
    }
}
