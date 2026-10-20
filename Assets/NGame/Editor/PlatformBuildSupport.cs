using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Callbacks;

namespace NGame.EditorTools
{
    /// <summary>Privacy prompts belong in the actual built bundle, including the Mac playtest.</summary>
    public static class PlatformBuildSupport
    {
        [PostProcessBuild(90)]
        public static void AddApplePrivacyDescriptions(BuildTarget target, string buildPath)
        {
            string plist;
            if (target == BuildTarget.StandaloneOSX)
                plist = Path.Combine(buildPath, "Contents", "Info.plist");
            else if (target == BuildTarget.iOS)
                plist = Path.Combine(buildPath, "Info.plist");
            else return;
            if (!File.Exists(plist)) throw new FileNotFoundException("The built Apple bundle needs its privacy descriptions.", plist);
            var document = XDocument.Load(plist, LoadOptions.PreserveWhitespace);
            // XDocument preserves the parsed empty DTD subset as [] when saving.
            // Apple's plist parser rejects that form; the optional DTD can be omitted.
            document.DocumentType?.Remove();
            var dictionary = document.Root?.Element("dict");
            if (dictionary == null) throw new InvalidDataException("The built Apple Info.plist has no dictionary.");
            Set(dictionary, "NSMicrophoneUsageDescription",
                "Le micro permet de parler aux autres joueurs pendant le vote de jour à distance, seulement quand vous l’activez.");
            Set(dictionary, "NSLocalNetworkUsageDescription",
                "N’GAME peut rejoindre le serveur de votre soirée sur le même réseau Wi-Fi.");
            document.Save(plist);
        }

        private static void Set(XElement dictionary, string key, string value)
        {
            var element = dictionary.Elements("key").FirstOrDefault(item => item.Value == key);
            if (element == null) dictionary.Add(new XElement("key", key), new XElement("string", value));
            else
            {
                var existing = element.ElementsAfterSelf().FirstOrDefault();
                if (existing == null) element.AddAfterSelf(new XElement("string", value));
                else existing.ReplaceWith(new XElement("string", value));
            }
        }
    }
}
