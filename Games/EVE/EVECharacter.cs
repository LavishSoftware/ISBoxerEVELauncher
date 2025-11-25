using ISBoxerEVELauncher.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

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

            string uri = string.Format("https://esi.evetech.net/latest/universe/ids/?categories=character&datasource={0}&language=en-us&search={1}&strict=true", (sisi ? "singularity" : "tranquility"), WebUtility.UrlEncode(characterName));

            using (WebClient wc = new WebClient())
            {
                try
                {

                    var charjson = string.Format("[\"{0}\"]",characterName);
                    wc.Headers[HttpRequestHeader.ContentType] = "application/json";

                    string outputString = wc.UploadString(uri,"POST",charjson);

                    var returnJson = JObject.Parse(outputString);
                    //{"characters":[{"id":2112625428,"name":"CCP Zoetrope"}]}

                    var id = (long)returnJson["characters"][0]["id"];


                    return id;
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
