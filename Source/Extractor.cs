using UnityEngine;
using Verse;
using RimWorld;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using HarmonyLib;

namespace LocalizationExtractor
{
    public class Extractor
    {
        ModMetaData modMetaData;
        DirectoryInfo actionLanguageFolder;
        DirectoryInfo englishLanguageFolder;
        public Extractor(ModMetaData modMetaData)
        {
            this.modMetaData = modMetaData;
            actionLanguageFolder = new DirectoryInfo(TranslationFilesCleaner.GetLanguageFolderPath(LanguageDatabase.activeLanguage, modMetaData.RootDir.FullName));
            englishLanguageFolder = new DirectoryInfo(TranslationFilesCleaner.GetLanguageFolderPath(LanguageDatabase.defaultLanguage, modMetaData.RootDir.FullName));
        }
        public void ExtractTranslate()
        {
            // Clear
            if (actionLanguageFolder.Exists) actionLanguageFolder.Delete(recursive: true);
            actionLanguageFolder.Create();

            ExtractTranslateDef();
            ExtractTranslateKeyed();
        }

        private void ExtractTranslateDef()
        {
            string pathToDefInjected = Path.Combine(actionLanguageFolder.FullName, "DefInjected");

            foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases())
            {
                var collectionFileToXML = new Dictionary<string, XDocument>();

                DefInjectionUtility.ForEachPossibleDefInjection(defType, delegate (string suggestedPath, string normalizedPath, bool isCollection, string curValue, IEnumerable<string> collection, bool translationAllowed, bool fullListTranslationAllowed, FieldInfo fi, Def def)
                {
                    if (!translationAllowed) return;

                    XElement getXElement(string message)
                    {
                        if (!collectionFileToXML.TryGetValue(def.fileName, out var xDocument))
                        {
                            xDocument = new XDocument(new XElement("LanguageData"));
                            collectionFileToXML.Add(def.fileName, xDocument);
                        }

                        return xDocument.Root!;
                    }

                    if (isCollection)
                    {
                        string listAsString = TranslationFilesCleaner2.ListToLiNodesString(collection);
                        if (DefInjectionUtility.ShouldCheckMissingInjection(listAsString, fi, def))
                        {
                            var xElement = getXElement(def.fileName);
                            xElement.Add(new XComment(TranslationFilesCleaner2.SanitizeXComment($" EN:\n{listAsString.Indented()}\n  ")));
                            xElement.Add(TranslationFilesCleaner2.ListToXElement(collection, suggestedPath));
                        }
                    }
                    else
                    {
                        if (curValue.NullOrEmpty()) curValue = TranslationFilesCleaner2.TryGetAlternativeValue(def);

                        if (DefInjectionUtility.ShouldCheckMissingInjection(curValue, fi, def))
                        {
                            var xElement = getXElement(def.fileName);
                            xElement.Add(new XComment(TranslationFilesCleaner2.SanitizeXComment($" EN: {curValue.Replace("\n", "\\n")} ")));
                            xElement.Add(new XElement(suggestedPath, curValue.Replace("\n", "\\n")));
                        }
                    }
                }, modMetaData);


                foreach (var pair in collectionFileToXML)
                {
                    string fileName = pair.Key;
                    XDocument xDocument = pair.Value;

                    string pathToTargetDef = Path.Combine(pathToDefInjected, defType.Name);
                    Directory.CreateDirectory(pathToTargetDef);

                    TranslationFilesCleaner2.SaveXMLDocumentWithProcessedNewlineTags(Path.Combine(pathToTargetDef, fileName), xDocument);
                }

            }
        }

        private void ExtractTranslateKeyed()
        {
            string pathToEnglishKeyed = Path.Combine(englishLanguageFolder.FullName, "Keyed");
            string pathToActionKeyed = Path.Combine(actionLanguageFolder.FullName, "Keyed");

            Directory.CreateDirectory(pathToActionKeyed);

            DirectoryInfo englishKeyedDir = new DirectoryInfo(pathToEnglishKeyed);

            var xmlFiles = englishKeyedDir.GetFiles("*.xml", SearchOption.AllDirectories);

            foreach (var file in xmlFiles)
            {
                var doc = XDocument.Load(file.FullName);
                var root = doc.Root;

                if (root != null)
                    foreach (var elem in root.Elements().ToList())
                        if (!string.IsNullOrEmpty(elem.Value))
                            elem.AddBeforeSelf(new XComment(TranslationFilesCleaner2.SanitizeXComment($" EN: {elem.Value} ")));

                doc.Save(Path.Combine(pathToActionKeyed, file.Name));
            }
        }

    }
}


class TranslationFilesCleaner2
{
    public static string SanitizeXComment(string comment)
    {
        while (comment.Contains("-----"))
        {
            comment = comment.Replace("-----", "- - -");
        }
        while (comment.Contains("--"))
        {
            comment = comment.Replace("--", "- -");
        }
        return comment;
    }
    public static string ListToLiNodesString(IEnumerable<string> list)
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (string item in list)
        {
            stringBuilder.Append("<li>");
            if (!item.NullOrEmpty())
            {
                stringBuilder.Append(item.Replace("\n", "\\n"));
            }
            stringBuilder.Append("</li>");
            stringBuilder.AppendLine();
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }
    public static XElement ListToXElement(IEnumerable<string> list, string name)//, List<Pair<int, string>> comments)
    {
        XElement xElement = new XElement(name);

        int num = 0;
        foreach (string item in list)
        {
            // if (comments != null)
            // {
            //     for (int i = 0; i < comments.Count; i++)
            //     {
            //         if (comments[i].First == num)
            //         {
            //             xElement.Add(new XComment(comments[i].Second));
            //         }
            //     }
            // }
            XElement xElement2 = new XElement("li");
            if (!item.NullOrEmpty()) xElement2.Add(new XText(item.Replace("\n", "\\n")));
            xElement.Add(xElement2);
            num++;
        }
        // if (comments != null)
        // {
        //     for (int j = 0; j < comments.Count; j++)
        //     {
        //         if (comments[j].First == num)
        //         {
        //             xElement.Add(new XComment(comments[j].Second));
        //         }
        //     }
        // }

        return xElement;
    }
    public static void SaveXMLDocumentWithProcessedNewlineTags(string path, XNode doc)
    {
        File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + doc.ToString().Replace("&gt;", ">"), Encoding.UTF8);
    }
    public static string TryGetAlternativeValue(object obj)
    {
        if (obj is StuffProperties stuffProperties)
        {
            return stuffProperties.parent.LabelAsStuff;
        }

        return string.Empty;
    }
}