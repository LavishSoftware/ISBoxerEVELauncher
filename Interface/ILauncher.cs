using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISBoxerEVELauncher.Enums;

namespace ISBoxerEVELauncher.Interface
{
    public interface ILauncher
    {
        LoginResult Launch(ILaunchTarget launchTarget);
        string LauncherText { get; }
    }
}
