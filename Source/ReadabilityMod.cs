using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using RimWorld;
using UnityEngine;
using Verse;

[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]
[assembly: AssemblyVersion("0.2.0.0")]

namespace RimDroid.Readability
{
    public sealed class ReadabilitySettings : ModSettings
    {
        internal bool Enabled;
        internal bool Colonists = true;
        internal ItemFilter Filter = ItemFilter.Off;
        internal int FontSize = 22;
        internal int MarkerSize = 16;
        internal bool GroupItems = true;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", false);
            Scribe_Values.Look(ref Colonists, "colonists", true);
            Scribe_Values.Look(ref Filter, "itemFilter", ItemFilter.Off);
            Scribe_Values.Look(ref FontSize, "fontSize", 22);
            Scribe_Values.Look(ref MarkerSize, "markerSize", 16);
            Scribe_Values.Look(ref GroupItems, "groupItems", true);
            PreferenceValues values = SettingsRules.Load(Filter, FontSize, MarkerSize, GroupItems);
            Filter = values.Filter; FontSize = values.Font; MarkerSize = values.Marker; GroupItems = values.Group;
        }
    }

    public sealed class ReadabilityMod : Mod
    {
        internal const string ReferenceHash = "39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076";
        internal const string ReferenceMvid = "c337411a-5cb6-49b6-8f4c-0609acf16d96";
        private const string HarmonyId = "erenalp.rimdroidreadability.overlay";
        internal static ReadabilitySettings Settings;
        private static readonly AssemblyGate gate = new AssemblyGate();
        internal static bool Ready { get { return gate.Ready; } }
        private static bool reported;
        private static bool initializationAttempted;
        private static Assembly boundGame;
        private static readonly HookLifecycle hooks = new HookLifecycle();
        private static object harmonyInstance;
        private static MethodInfo unpatch;
        private static string status = "Başlangıç kontrolü bekleniyor.";

        public ReadabilityMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ReadabilitySettings>();
            LongEventHandler.ExecuteWhenFinished(Initialize);
        }

        public override string SettingsCategory() { return "RimDroid Readability"; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            bool oldEnabled = Settings.Enabled, oldColonists = Settings.Colonists;
            ItemFilter oldFilter = Settings.Filter;
            int oldFont = Settings.FontSize, oldMarker = Settings.MarkerSize;
            bool oldGroup = Settings.GroupItems;
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("RimWorld 1.6 / RimDroid Native — 0.2.0");
            list.Label(status);
            list.Gap();
            list.CheckboxLabeled("İşaretleri aç", ref Settings.Enabled);
            list.CheckboxLabeled("Kolonistleri göster", ref Settings.Colonists);
            string filter = SettingsRules.FilterName(Settings.Filter);
            if (list.ButtonText("Eşya filtresi: " + filter)) Settings.Filter = SettingsRules.NextFilter(Settings.Filter);
            if (list.ButtonText("Yazı boyutu: " + Settings.FontSize))
                Settings.FontSize = Settings.FontSize == 18 ? 22 : Settings.FontSize == 22 ? 26 : Settings.FontSize == 26 ? 32 : 18;
            if (list.ButtonText("İşaret boyutu: " + Settings.MarkerSize))
                Settings.MarkerSize = Settings.MarkerSize == 8 ? 16 : Settings.MarkerSize == 16 ? 24 : 8;
            list.CheckboxLabeled("Yakın eşya yığınlarını grupla", ref Settings.GroupItems);
            list.Gap();
            list.Label("Bir eşya kategorisi, en fazla 40 etiket ve 120 işaret. Bul ile kolonist veya yığına Git; yakınlık korunur. Kapatınca bütün mod kontrolleri kaldırılır.");
            list.Label("Telefon DLL eşleşmesi ve gerçek cihaz performansı henüz doğrulanmadı.");
            list.End();
            if (oldEnabled != Settings.Enabled || oldColonists != Settings.Colonists || oldFilter != Settings.Filter
                || oldFont != Settings.FontSize || oldMarker != Settings.MarkerSize || oldGroup != Settings.GroupItems)
            {
                if (!Settings.Enabled) FinderController.Reset();
                else OverlayRenderer.Invalidate();
            }
        }

        private static void Initialize()
        {
            if (initializationAttempted) return;
            initializationAttempted = true;
            try
            {
                if (!AnnotationRules.NativeAllowed(Application.platform == RuntimePlatform.Android,
                    Environment.GetEnvironmentVariable("RIMDROID_IL2MONO_MANAGED")))
                { Disable("Yalnız RimDroid Native Android motoru desteklenir."); return; }
                if (!AnnotationRules.VersionAllowed(VersionControl.CurrentVersionString))
                { Disable("Bu mod yalnız RimWorld 1.6 içindir."); return; }
                boundGame = typeof(Find).Assembly;
                // Subscribe before any snapshot. A load before subscription is covered by the snapshot;
                // a load after it is covered by the sticky gate, which initialization cannot overwrite.
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                if (boundGame.ManifestModule.ModuleVersionId.ToString() != ReferenceMvid)
                { Disable("Oyun MVID değeri incelenen DLL ile eşleşmiyor; API doğrulaması gerekli."); return; }
                if (string.IsNullOrEmpty(boundGame.Location) || !File.Exists(boundGame.Location))
                { Disable("Yüklenen oyun DLL dosyasının kimliği okunamadı."); return; }
                string actualHash;
                using (SHA256 hash = SHA256.Create())
                using (FileStream file = File.OpenRead(boundGame.Location))
                    actualHash = BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                if (actualHash != ReferenceHash)
                { Disable("Oyun DLL SHA-256 eşleşmedi: " + actualHash + ". Bu build yeniden doğrulanmalı."); return; }
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if (assembly.GetName().Name == "Assembly-CSharp" && assembly != boundGame)
                        gate.ObserveReplacement();
                if (HandleAssemblyReplacement()) return;

                ApiContract.Verify();
                MethodInfo target = ApiContract.Method(typeof(ThingOverlays), "ThingOverlaysOnGUI", typeof(void), false);
                InstallHarmony(new[] { target, ApiContract.Method(typeof(MapInterface), "MapInterfaceOnGUI_AfterMainTabs", typeof(void), false) });
                if (!gate.MarkInitialized(hooks.Active)) { HandleAssemblyReplacement(); return; }
                status = "API/DLL kontrolü geçti. Göstergeler ve Bul açılabilir; cihaz testi bekleniyor.";
                Log.Message("[RimDroid Readability] Native 1.6 reference DLL verified; overlay and finder patches installed, enabled="
                    + Settings.Enabled + "; SHA256=" + actualHash + "; MVID=" + ReferenceMvid
                    + ". Android visual/performance acceptance still pending.");
            }
            catch (Exception error) { Disable("Başlatma kontrolü başarısız: " + error.GetType().Name + ": " + error.Message); }
        }

        private static void InstallHarmony(MethodInfo[] originals)
        {
            // No 0Harmony is loaded from disk or distributed. Bind the existing runtime like RimDroid's bridge.
            Assembly runtime = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == "0Harmony")
                {
                    if (runtime != null && runtime != assembly) throw new InvalidOperationException("Multiple Harmony assemblies");
                    runtime = assembly;
                }
            if (runtime == null) throw new InvalidOperationException("RimDroid Native Harmony is not loaded");
            Version version = runtime.GetName().Version;
            if (version.Major != 2) throw new InvalidOperationException("Unsupported Harmony " + version);
            Type harmony = runtime.GetType("HarmonyLib.Harmony", true);
            Type harmonyMethod = runtime.GetType("HarmonyLib.HarmonyMethod", true);
            MethodInfo patch = null;
            foreach (MethodInfo method in harmony.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] p = method.GetParameters();
                if (method.Name == "Patch" && p.Length >= 3 && p[0].ParameterType == typeof(MethodBase)
                    && p[1].ParameterType == harmonyMethod && p[2].ParameterType == harmonyMethod)
                { patch = method; break; }
            }
            if (patch == null) throw new MissingMethodException("Harmony.Patch postfix signature");
            unpatch = harmony.GetMethod("UnpatchAll", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            if (unpatch == null || unpatch.ReturnType != typeof(void)) throw new MissingMethodException("Harmony.UnpatchAll(string)");
            harmonyInstance = Activator.CreateInstance(harmony, new object[] { HarmonyId });
            Type[] callbacks = { typeof(OverlayRenderer), typeof(FinderController) };
            hooks.Install(originals.Length, delegate(int i)
            {
                object postfix = Activator.CreateInstance(harmonyMethod,
                    new object[] { callbacks[i].GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static) });
                object[] parameters = new object[patch.GetParameters().Length];
                parameters[0] = originals[i]; parameters[2] = postfix;
                patch.Invoke(harmonyInstance, parameters);
            }, RollbackHarmony);
            Log.Message("[RimDroid Readability] Existing Harmony " + version + " used through runtime reflection; no bundled Harmony.");
        }

        private static void RollbackHarmony()
        {
            if (unpatch != null && harmonyInstance != null)
                unpatch.Invoke(unpatch.IsStatic ? null : harmonyInstance, new object[] { HarmonyId });
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName().Name == "Assembly-CSharp" && args.LoadedAssembly != boundGame)
                gate.ObserveReplacement(); // No rendering, logging or list mutation on the loader thread.
        }

        internal static bool HandleAssemblyReplacement()
        {
            if (!gate.ConsumeReplacement()) return false;
            Disable("Oyun assembly'si değişti veya birden fazla kopya var; overlay yeniden doğrulama için kapatıldı.");
            return true;
        }

        internal static void Disable(string reason)
        {
            gate.Disable();
            try { FinderController.Reset(); }
            catch (Exception error) { reason += "; pencere temizliği: " + error.GetType().Name; }
            try { hooks.Remove(RollbackHarmony); }
            catch (Exception error) { reason += "; kanca temizliği: " + error.GetType().Name; }
            status = reason;
            if (reported) return;
            reported = true;
            Log.Warning("[RimDroid Readability] Disabled: " + reason);
        }
    }
}
