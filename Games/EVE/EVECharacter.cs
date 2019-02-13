using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ISBoxerEVELauncher.Games.EVE
{
    /// <summary>
    /// An EVE Online Character 
    /// </summary>
    public class EVECharacter : ISBoxerEVELauncher.Launchers.ILaunchTarget
    {
        /// <summary>
        /// Name of the Character
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the EVE Account this Character is on
        /// </summary>
        public string EVEAccountName { get; set; }

        /// <summary>
        /// EVE Account this Character is on
        /// </summary>
        [XmlIgnore]
        public EVEAccount EVEAccount
        {
            get 
            {
                return App.Settings.FindEVEAccount(EVEAccountName);
            }

            set
            {
                if (value == null)
                    EVEAccountName = null;
                else
                    EVEAccountName = value.Username;
            }
        }

        /// <summary>
        /// Character ID as reported by ESI ...
        /// </summary>
        public long CharacterID { get; set; }

        /// <summary>
        /// Use Singularity for this Character
        /// </summary>
        public bool UseSingularity { get; set; }

        public long GetCharacterID()
        {
            CharacterID = GetCharacterID(UseSingularity, Name);
            return CharacterID;
        }

        public static long GetCharacterID(bool sisi, string characterName)
        {

            string uri = string.Format("https://esi.evetech.net/v1/search/?categories=character&datasource={0}&language=en-us&search={1}&strict=true", (sisi?"singularity":"tranquility"), WebUtility.UrlEncode(characterName));

            using (WebClient wc = new WebClient())
            {
                try
                {
                    string outputString = wc.DownloadString(uri);
                    if (outputString.Equals("{}"))
                        return 0;// Character does not exist

                    // Response is JSON, but since it's not complex we'll just strip the formatting instead of using a JSON parser.
//                     {"character":[90664221]}

                    string prefix = "{\"character\":[";
                    string suffix = "]}";

                    if (!outputString.StartsWith(prefix))
                    {
                        throw new FormatException("Expected {\"character\":[#####]} but got " + outputString);
                    }

                    if (!outputString.EndsWith(suffix))
                    {
                        throw new FormatException("Expected {\"character\":[#####]} but got " + outputString);
                    }

                    outputString = outputString.Substring(prefix.Length);
                    outputString = outputString.Substring(0, outputString.Length - suffix.Length);

                    return long.Parse(outputString);
                }
                catch
                {
                    return 0;
                }
            }
        }

        [XmlIgnore]
        EVEAccount Launchers.ILaunchTarget.EVEAccount
        {
            get { return EVEAccount; }
        }
    
    }
}
