using System;
using System.Collections.Generic;

using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace ISBoxerEVELauncher
{
    [Serializable]
    public class Set
    {
        public Set()
        {
            Sets = new List<Set>();
            Settings = new List<Setting>();
        }

        public Set(string name)
            : this()
        {
            Name = name;
        }
        [XmlAttribute]
        public string Name;

        [XmlElement(typeof(Set), ElementName = "Set")]
        public List<Set> Sets { get; set; }
        [XmlElement(typeof(Setting), ElementName = "Setting")]
        public List<Setting> Settings { get; set; }

        Dictionary<string, Set> setsDictionary;
        /// <summary>
        /// It is recommended only to use this convenience dictionary when treating the Set as read-only
        /// </summary>
        [XmlIgnore]       
        public Dictionary<string, Set> SetsDictionary
        {
            get
            {
                if (setsDictionary == null)
                {
                    setsDictionary = new Dictionary<string, Set>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (Set set in Sets)
                    {
                        setsDictionary.Add(set.Name, set);
                    }
                }
                return setsDictionary;
            }
        }

        Dictionary<string, Setting> settingsDictionary;
        /// <summary>
        /// It is recommended only to use this convenience dictionary when treating the Set as read-only
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, Setting> SettingsDictionary
        {
            get
            {
                if (settingsDictionary == null)
                {
                    settingsDictionary = new Dictionary<string, Setting>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (Setting setting in Settings)
                    {
                        settingsDictionary.Add(setting.Name, setting);
                    }
                }
                return settingsDictionary;
            }
        }


        public Set FindSet(string name)
        {
            if (name == null || Sets == null)
                return null;
            foreach (Set set in Sets)
            {
                if (set.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return set;
            }
            return null;
        }
        public Setting FindSetting(string name)
        {
            if (name == null || Settings == null)
                return null;
            foreach (Setting setting in Settings)
            {
                if (string.IsNullOrEmpty(setting.Name))
                    continue;
                if (setting.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return setting;
            }
            return null;
        }

        public bool Add(Set set)
        {
            if (set == null)
                return false;
            if (FindSet(set.Name) != null)
                return false;
            Sets.Add(set);

            if (setsDictionary != null)
            {
                setsDictionary.Add(set.Name, set);
            }
            return true;
        }
        public bool Add(Setting setting)
        {
            if (setting == null || setting.Name == null)
                return false;
            if (FindSetting(setting.Name) != null)
                return false;
            Settings.Add(setting);

            if (settingsDictionary != null)
            {
                settingsDictionary.Add(setting.Name, setting);
            }
            return true;
        }

        public bool Store(string filename)
        {
            try
            {
                InnerSpaceSettings issettings = new InnerSpaceSettings(this);
                using (TextWriter w = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                {
                    XmlSerializer s = new XmlSerializer(typeof(InnerSpaceSettings));
                    s.Serialize(w, issettings);
                    return true;
                }
            }
            catch (Exception e)
            {
//                MessageBox.Show(e.ToString());
//                return false;
                throw;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Attribute
    {
        public Attribute()
        {
        }
        public Attribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public Attribute(string name, bool value)
            : this(name, value ? "TRUE" : "FALSE")
        {
        }
        public Attribute(string name, int value)
            : this(name, value.ToString())
        {
        }
        public Attribute(string name, uint value)
            : this(name, value.ToString())
        {
        }
        public Attribute(string name, float value)
            : this(name, value.ToString())
        {
        }

        public string Name;
        public string Value;
    }

    public class Setting //: IXmlSerializable
    {
        public Setting()
        {
        }
        public Setting(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public Setting(string name, bool value)
            : this(name, value ? "TRUE" : "FALSE")
        {
        }
        public Setting(string name, int value)
            : this(name, value.ToString())
        {
        }
        public Setting(string name, uint value)
            : this(name, value.ToString())
        {
        }
        public Setting(string name, float value)
            : this(name, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo))
        {
        }
        [XmlAttribute]
        public string Name;

        [XmlText]
        public string Value;

        public bool GetValue(out int val)
        {
            return int.TryParse(Value, out val);
        }

        public bool GetValue(out bool val)
        {
            int i;
            if (GetValue(out i))
            {
                if (i != 0)
                    val = true;
                else
                    val = false;
                return true;
            }
            return bool.TryParse(Value.ToLower(), out val);
        }
    }

    [XmlRoot("InnerSpaceSettings")]
    [Serializable]
    public class InnerSpaceSettings : Set
    {
        public InnerSpaceSettings()
        {
        }
        public InnerSpaceSettings(Set copyfrom)
        {
            this.Sets = copyfrom.Sets;
            this.Settings = copyfrom.Settings;
        }
        public static Set Load(string filename)
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(InnerSpaceSettings));
                using (TextReader r = new StreamReader(filename, System.Text.Encoding.UTF8))
                {
                    Set set = (InnerSpaceSettings)s.Deserialize(r);
                    return set;
                }
            }
            catch (System.IO.FileNotFoundException e)
            {
                throw;
            }
            catch (Exception e)
            {
//                MessageBox.Show("Error loading file " + filename + "... " + Environment.NewLine + e.ToString());
//                return null;
                throw;
            }
        }
    }
}
