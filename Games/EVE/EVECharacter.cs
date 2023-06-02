using ISBoxerEVELauncher.Interface;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using static ISBoxerEVELauncher.Games.EVE.EVEAccount;

namespace ISBoxerEVELauncher.Games.EVE
{
    /// <summary>
    /// An EVE Online Character 
    /// </summary>
    public class EVECharacter : ILaunchTarget
    {
        /// <summary>
        /// Name of the Character
        /// </summary>
        public string Name
        {
            get; set;
        }

        /// <summary>
        /// Name of the EVE Account this Character is on
        /// </summary>
        public string EVEAccountName
        {
            get; set;
        }

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
        public long CharacterID
        {
            get; set;
        }

        /// <summary>
        /// Use Singularity for this Character
        /// </summary>
        public bool UseSingularity
        {
            get; set;
        }

        public long GetCharacterID()
        {
            CharacterID = GetCharacterID(UseSingularity, Name);
            return CharacterID;
        }

        public static long GetCharacterID(bool sisi, string characterName)
        {

            string uri = string.Format("https://esi.evetech.net/latest/universe/ids/?datasource={0}&language=en", (sisi ? "singularity" : "tranquility"));
            string postData = string.Format("[ \"{0}\" ]", characterName);
            byte[] byteArray = Encoding.ASCII.GetBytes(postData);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    byte[] result = wc.UploadData(uri, "POST", byteArray);
                    string outputString = Encoding.ASCII.GetString(result);
                    if (outputString.Equals("{}"))
                        return 0;// Character does not exist

                    var getResult = JObject.Parse(outputString);
                    var id = getResult["characters"][0]["id"];
                    return long.Parse(id.ToString());
                    
                }
                catch
                {
                    return 0;
                }
            }
        }

        [XmlIgnore]
        EVEAccount ILaunchTarget.EVEAccount
        {
            get
            {
                return EVEAccount;
            }
        }

    }
}
