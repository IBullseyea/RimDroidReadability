using System;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    // Exact signatures, including one verified read-only private camera snapshot accessor.
    internal static class ApiContract
    {
        internal static PropertyInfo CameraPosition { get; private set; }
        internal static MethodInfo Method(Type owner, string name, Type result, bool isStatic, params Type[] args)
        {
            MethodInfo method = owner.GetMethod(name, BindingFlags.Public |
                (isStatic ? BindingFlags.Static : BindingFlags.Instance), null, args, null);
            if (method == null || method.ReturnType != result) throw new MissingMethodException(owner.FullName, name);
            return method;
        }

        internal static void Property(Type owner, string name, Type value)
        {
            PropertyInfo property = owner.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || property.PropertyType != value || property.GetGetMethod() == null)
                throw new MissingMemberException(owner.FullName, name);
        }

        internal static void Field(Type owner, string name, Type value)
        {
            FieldInfo field = owner.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.FieldType != value) throw new MissingMemberException(owner.FullName, name);
        }

        internal static void Verify()
        {
            Method(typeof(Translator), "TranslateSimple", typeof(string), true, typeof(string));
            FieldInfo language = typeof(LanguageDatabase).GetField("activeLanguage", BindingFlags.Public | BindingFlags.Static);
            if (language == null || language.FieldType != typeof(LoadedLanguage)) throw new MissingMemberException("LanguageDatabase.activeLanguage");
            Method(typeof(ThingOverlays), "ThingOverlaysOnGUI", typeof(void), false);
            Method(typeof(MapInterface), "MapInterfaceOnGUI_AfterMainTabs", typeof(void), false);
            Method(typeof(CameraJumper), "TryJumpAndSelect", typeof(void), true, typeof(GlobalTargetInfo), typeof(CameraJumper.MovementMode));
            if (!Enum.IsDefined(typeof(CameraJumper.MovementMode), "Cut")) throw new MissingMemberException("MovementMode.Cut");
            Property(typeof(CameraDriver), "RootSize", typeof(float));
            Property(typeof(CameraDriver), "MapPosition", typeof(IntVec3));
            Method(typeof(CameraDriver), "SetRootSize", typeof(void), false, typeof(float));
            Method(typeof(CameraDriver), "JumpToCurrentMapLoc", typeof(void), false, typeof(IntVec3));
            Method(typeof(CameraDriver), "JumpToCurrentMapLoc", typeof(void), false, typeof(Vector3));
            CameraPosition = typeof(CameraDriver).GetProperty("CurrentRealPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            if (CameraPosition == null || CameraPosition.PropertyType != typeof(Vector3) || CameraPosition.GetGetMethod(true) == null)
                throw new MissingMemberException("CameraDriver.CurrentRealPosition");
            Method(typeof(WindowStack), "Add", typeof(void), false, typeof(Window));
            Method(typeof(WindowStack), "TryRemove", typeof(bool), false, typeof(Window), typeof(bool));
            Method(typeof(Window), "DoWindowContents", typeof(void), false, typeof(Rect));
            Property(typeof(Window), "InitialSize", typeof(Vector2));
            Field(typeof(Window), "absorbInputAroundWindow", typeof(bool));
            Field(typeof(Window), "preventCameraMotion", typeof(bool));
            Field(typeof(Window), "windowRect", typeof(Rect));
            Method(typeof(Widgets), "ButtonText", typeof(bool), true, typeof(Rect), typeof(string), typeof(bool),
                typeof(bool), typeof(bool), typeof(TextAnchor?));
            Method(typeof(Widgets), "TextField", typeof(string), true, typeof(Rect), typeof(string));
            Method(typeof(ListerThings), "ThingsInGroup", typeof(System.Collections.Generic.List<Thing>), false, typeof(ThingRequestGroup));
            Method(typeof(GenMapUI), "LabelDrawPosFor", typeof(Vector2), true, typeof(Thing), typeof(float));
            Property(typeof(MapPawns), "FreeColonistsSpawned", typeof(System.Collections.Generic.List<Pawn>));
            Field(typeof(Map), "listerThings", typeof(ListerThings));
            Field(typeof(Map), "mapPawns", typeof(MapPawns));
            Field(typeof(ThingDef), "category", typeof(ThingCategory));
            Field(typeof(ThingDef), "ingestible", typeof(IngestibleProperties));
            foreach (string name in new[] { "IsMedicine", "IsWeapon", "IsApparel", "CountAsResource" })
                Property(typeof(ThingDef), name, typeof(bool));
            Property(typeof(IngestibleProperties), "CachedNutrition", typeof(float));
        }
    }
}
