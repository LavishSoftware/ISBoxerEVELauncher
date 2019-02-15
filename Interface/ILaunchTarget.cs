using ISBoxerEVELauncher.Games.EVE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Interface
{
    public interface ILaunchTarget
    {
        EVEAccount EVEAccount { get; }
        long CharacterID { get; }
    }
}
