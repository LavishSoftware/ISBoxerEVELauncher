namespace ISBoxerEVELauncher.Extensions
{
    public static class ProcessExtension
    {
        /// <summary>
        /// Check if a process name matches, ignoring any ".vshost" extension from Visual Studio debugging...
        /// </summary>
        /// <param name="processA"></param>
        /// <param name="processB"></param>
        /// <returns></returns>
        public static bool NameMatches(this System.Diagnostics.Process processA, System.Diagnostics.Process processB)
        {
            string cleanA = processA.ProcessName.ToLowerInvariant().Replace(".vshost", string.Empty);
            string cleanB = processB.ProcessName.ToLowerInvariant().Replace(".vshost", string.Empty);

            return cleanA == cleanB;
        }

        /// <summary>
        /// Check if a process MainModule FileName matches, ignoring any ".vshost" extension from Visual Studio debugging...
        /// </summary>
        /// <param name="processA"></param>
        /// <param name="processB"></param>
        /// <returns></returns>
        public static bool MainModuleNameMatches(this System.Diagnostics.Process processA, System.Diagnostics.Process processB)
        {
            string cleanA = processA.MainModule.FileName.ToLowerInvariant().Replace(".vshost", string.Empty);
            string cleanB = processB.MainModule.FileName.ToLowerInvariant().Replace(".vshost", string.Empty);

            return cleanA == cleanB;
        }



    }
}
