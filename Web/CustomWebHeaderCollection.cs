using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Web
{
    public class CustomWebHeaderCollection : WebHeaderCollection
    {
        private readonly Dictionary<string, string> _customHeaders;

        public CustomWebHeaderCollection(Dictionary<string, string> customHeaders)
        {
            _customHeaders = customHeaders;
        }

        public override string ToString()
        {
            var allheaders = this.AllKeys.ToDictionary(k => k, k => this.Get(k));

            // Overwrite the old with custom
            foreach (var customValue in _customHeaders)
            {
                allheaders[customValue.Key] = customValue.Value;
            }
            // Could call base.ToString() split on Newline and sort as needed
            // Bubble host to top
            var lines = allheaders
                .OrderBy(h => h.Key == "Host" ? -1 : 0)
                .Select(kvp => $"{kvp.Key}: {kvp.Value}")
                // These two new lines are needed after the HTTP header
                .Concat(new[] { string.Empty, string.Empty });

            var headers = string.Join("\r\n", lines);

            return headers;
        }
    }
}
