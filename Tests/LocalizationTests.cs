using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimDroid.Readability;

internal static class LocalizationTests
{
    private static readonly Dictionary<string, string> english = Load("English");
    private static Dictionary<string, string> selected;
    private static object language;

    private static Dictionary<string, string> Load(string folder)
    {
        string file = Path.Combine("1.6", "Languages", folder, "Keyed", "Readability.xml");
        return File.Exists(file) ? XDocument.Load(file).Root.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value)
            : new Dictionary<string, string>();
    }
    private static void Select(string folder)
    {
        selected = Load(folder); language = new object();
        ModText.Lookup = delegate(string key) {
            string value;
            return selected.TryGetValue(key, out value) || english.TryGetValue(key, out value) ? value : key;
        };
        ModText.Language = delegate { return language; };
    }

    internal static void Run(Action<bool, string> check)
    {
        Select("English");
        LocalizedText status = ModText.Message("StatusReady");
        LocalizedText failure = ModText.Error("ContextDraw", new InvalidOperationException("fixture detail"));
        LocalizedText nativeFailure = ModText.Error("ContextStartup", new LocalizedException(ModText.Message("HarmonyMissing")));
        check(SettingsRules.FilterName(ItemFilter.Wood) == "Wood" && ModText.Get("Items") == "Items", "English UI fields");
        check(status.ToString().Contains("checks passed") && failure.ToString().StartsWith("Drawing error"), "English lazy status/error context");
        Select("Turkish");
        check(SettingsRules.FilterName(ItemFilter.Wood) == "Odun" && ModText.Get("Items") == "Eşya", "Turkish UI fields");
        check(status.ToString().Contains("kontrolü geçti") && failure.ToString().StartsWith("Çizim hatası"), "stored messages follow new game language");
        check(nativeFailure.ToString().Contains("yüklenmedi"), "mod-owned exception reason follows language change");
        Select("ChineseSimplified");
        check(ModText.Get("Items") == "物品" && SettingsRules.FilterName(ItemFilter.Medicine) == "药品", "Simplified Chinese UI fields");
        check(status.ToString().Contains("检查通过") && failure.ToString().StartsWith("绘制错误"), "Chinese lazy status/error context");
        check(nativeFailure.ToString().Contains("未加载"), "mod-owned exception reason becomes Chinese");
        Select("ChineseTraditional");
        check(ModText.Get("Close") == "關閉" && SettingsRules.FilterName(ItemFilter.Medicine) == "藥品", "Traditional Chinese UI fields");
        foreach (string directory in Directory.GetDirectories(Path.Combine("1.6", "Languages")))
        {
            string folder = Path.GetFileName(directory); Select(folder);
            check(selected.Count == english.Count, "complete language " + folder);
            foreach (string key in english.Keys)
            {
                string text = ModText.Get(key.Substring(4), "A", "B", "C", "D", "E", "F", "G");
                check(!string.IsNullOrWhiteSpace(text) && !text.StartsWith("RDR_"), "resolved formatted translation " + folder + "/" + key);
            }
            TargetGroup group = new TargetGroup();
            group.Add(new TargetData { Id = 1, Name = "Custom 漢字", TypeKey = "Type", Amount = 20 });
            group.Add(new TargetData { Id = 2, Name = "Custom 漢字", TypeKey = "Type", Amount = 30 });
            check(group.Summary.Contains("Custom 漢字") && group.Summary.Contains("50") && group.Summary.Contains("2"), "translated quantity preserves target name/count " + folder);
            check(failure.ToString().Contains("fixture detail") && !failure.ToString().Contains("\n"), "technical exception detail stays exact, GUI error stays short " + folder);
        }
        Select("CommunityLanguageWithoutModTranslation");
        check(ModText.Get("Find") == "Find" && SettingsRules.FilterName(ItemFilter.Medicine) == "Medicine", "English fallback for unlisted community language");
        QueryClock clock = new QueryClock(); object map = new object(), game = new object();
        check(clock.Refresh(map, game, "same", 1, ModText.CurrentLanguage), "language-aware first query");
        check(!clock.Refresh(map, game, "same", 1.01f, ModText.CurrentLanguage), "same-language query stays cached");
        Select("Turkish");
        check(clock.Refresh(map, game, "same", 1.02f, ModText.CurrentLanguage), "language switch invalidates catalogue immediately");
        Select("English");
    }
}
