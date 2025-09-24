using HarmonyLib;
using Verse;

namespace LocalizationExtractor
{
    [HarmonyPatch(typeof(OptionListingUtility), "DrawOptionListing")]
    public class Patch_AddTranslateButton
    {
        [HarmonyPrefix]
        private static bool PreFix(ref List<ListableOption> optList)
        {
            // If the ListableOption_WebLink is not in the list, then it is a main menu list with buttons.
            if (optList.Find((ListableOption x) => x is ListableOption_WebLink) == null)
                optList.Add(new ListableOption("LocalizationExtractor".Translate(), delegate { Find.WindowStack.Add(new ExtractWindow()); }));
            return true;
        }
    }
}
