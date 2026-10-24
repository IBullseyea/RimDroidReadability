using System;
using System.Collections.Generic;
using RimDroid.Readability;
using UnityEngine;
using Verse;

// Original API fixtures only. The real TargetCatalog adapter is compiled against these types;
// no game or Unity assembly is linked into the standalone test executable.
internal static class CatalogTests
{
    internal static void Run(Action<bool, string> check)
    {
        Map map = new Map();
        Current.Game = new Game(); Current.ProgramState = ProgramState.Playing;
        Find.CurrentMap = map; WorldView(true); Time.realtimeSinceStartup = 1;
        ReadabilityMod.Settings.Filter = ItemFilter.Wood; TargetCatalog.Search = ""; TargetCatalog.Clear();
        Pawn first = Pawn(1, map), second = Pawn(2, map);
        map.mapPawns.Source.Add(first); map.mapPawns.Source.Add(second);
        Thing wood = new Thing { thingIDNumber = 3, Map = map, def = RimWorld.ThingDefOf.WoodLog,
            stackCount = 20, Position = new IntVec3(5, 8) };
        Thing extra = new Thing { thingIDNumber = 4, Map = map, def = RimWorld.ThingDefOf.WoodLog,
            stackCount = 30, Position = new IntVec3(6, 8) };
        map.listerThings.Items.Add(wood); map.listerThings.Items.Add(extra);

        // The fixture deliberately returns and rebuilds the SAME borrowed list on each getter call.
        // Eligible() re-enters this getter while Ensure() is iterating its query results.
        List<TargetData> snapshot = TargetCatalog.Snapshot(true);
        check(snapshot.Count == 4, "real catalogue adapter survives re-entrant borrowed colonist list rebuild");
        check(map.mapPawns.GetterCalls > 1, "fixture exercised nested getter access, not a fresh list shortcut");
        check(ReferenceEquals(map.mapPawns.FreeColonistsSpawned, map.mapPawns.FreeColonistsSpawned), "fixture matches shared scratch-list ownership");
        check(snapshot[0].Id == 1 && snapshot[3].Id == 4, "adapter catalogue stays sorted by stable identifier");
        check(GroupRules.Types(snapshot)[0].Amount == 50, "real adapter preserves 20+30 item total");
        int scans = map.listerThings.Scans;
        for (int frame = 1; frame <= 120; frame++)
        {
            Time.realtimeSinceStartup = 1 + frame / 60f;
            snapshot = TargetCatalog.Snapshot(frame % 2 == 0);
            check(snapshot.Count == 4, "repeated redraw/finder snapshot remains complete after scratch-list mutations " + frame);
            check(TargetCatalog.Resolve(1) == first, "navigation lookup remains valid through repeated refresh " + frame);
        }
        check(map.listerThings.Scans > scans, "test crossed periodic membership refresh boundaries");

        // Live validity and amounts are still checked; copying a query must not admit stale targets.
        wood.stackCount = 9; extra.Destroyed = true;
        map.mapPawns.Source.Remove(first);
        snapshot = TargetCatalog.Snapshot(false);
        check(snapshot.Count == 2, "removed colonist and destroyed item excluded without waiting for refresh");
        check(TargetCatalog.Resolve(1) == null && TargetCatalog.Resolve(4) == null, "Go rejects now-invalid entries");
        check(GroupRules.Types(snapshot)[0].Amount == 9, "current item amount survives owned query copy");
        map.fogGrid.Fogged.Add(wood.Position);
        check(TargetCatalog.Snapshot(false).Count == 1 && TargetCatalog.Resolve(3) == null, "fog hides copied items before render and action");

        scans = map.listerThings.Scans; ReadabilityMod.Settings.Filter = ItemFilter.Off;
        check(TargetCatalog.Snapshot(false).Count == 1 && map.listerThings.Scans == scans, "Off skips item registry scan");
        Find.CurrentMap = new Map();
        check(TargetCatalog.Snapshot(false).Count == 0 && TargetCatalog.Resolve(2) == null, "map switch clears old catalogue references");
        WorldView(false); TargetCatalog.Clear(); TargetCatalog.Search = "";
    }

    private static void WorldView(bool drawingMap) { RimWorld.Planet.WorldRendererUtility.DrawingMap = drawingMap; }
    private static Pawn Pawn(int id, Map map)
    { return new Pawn { thingIDNumber = id, Map = map, LabelShort = "Kolonist " + id,
        def = new ThingDef { defName = "Human", label = "İnsan", category = ThingCategory.Pawn }, Position = new IntVec3(id, id) }; }
}

namespace UnityEngine
{
    internal struct Vector2
    {
        internal float x, y;
        internal Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    internal static class Time { internal static float realtimeSinceStartup; }
}

namespace RimWorld
{
    internal sealed class IngestibleProperties { internal float CachedNutrition = 1; }
    internal static class ThingDefOf
    {
        internal static readonly Verse.ThingDef WoodLog = new Verse.ThingDef {
            defName = "WoodLog", label = "Odun", category = Verse.ThingCategory.Item, EverHaulable = true, CountAsResource = true };
    }
}

namespace RimWorld.Planet
{
    internal static class WorldRendererUtility { internal static bool DrawingMap; }
}

namespace Verse
{
    internal enum ProgramState { Entry, Playing }
    internal enum ThingCategory { Pawn, Item }
    internal enum ThingRequestGroup { HaulableEver }
    internal sealed class Game { }
    internal static class Current { internal static Game Game; internal static ProgramState ProgramState; }
    internal struct IntVec3
    {
        internal int x, z;
        internal IntVec3(int x, int z) { this.x = x; this.z = z; }
    }
    internal struct CellRect { internal bool Contains(IntVec3 cell) { return true; } }
    internal sealed class ThingDef
    {
        internal string defName, label;
        internal ThingCategory category;
        internal bool EverHaulable, IsMedicine = false, IsWeapon = false, IsApparel = false, CountAsResource;
        internal RimWorld.IngestibleProperties ingestible = null;
    }
    internal class Thing
    {
        internal bool Destroyed, Spawned = true;
        internal Map Map;
        internal ThingDef def;
        internal int thingIDNumber, stackCount;
        internal IntVec3 Position;
    }
    internal sealed class Pawn : Thing { internal string LabelShort; }
    internal sealed class Map
    {
        internal readonly MapPawns mapPawns = new MapPawns();
        internal readonly ListerThings listerThings = new ListerThings();
        internal readonly FogGrid fogGrid = new FogGrid();
    }
    internal sealed class MapPawns
    {
        internal readonly List<Pawn> Source = new List<Pawn>();
        private readonly List<Pawn> scratch = new List<Pawn>();
        internal int GetterCalls;
        internal List<Pawn> FreeColonistsSpawned
        { get { GetterCalls++; scratch.Clear(); scratch.AddRange(Source); return scratch; } }
    }
    internal sealed class ListerThings
    {
        internal readonly List<Thing> Items = new List<Thing>();
        internal int Scans;
        internal List<Thing> ThingsInGroup(ThingRequestGroup group) { Scans++; return Items; }
    }
    internal sealed class FogGrid
    {
        internal readonly HashSet<IntVec3> Fogged = new HashSet<IntVec3>();
        internal bool IsFogged(IntVec3 cell) { return Fogged.Contains(cell); }
    }
    internal sealed class CameraDriver { internal CellRect CurrentViewRect = new CellRect(); }
    internal sealed class Selector { internal bool IsSelected(Thing thing) { return false; } }
    internal static class Find
    {
        internal static Map CurrentMap;
        internal static readonly CameraDriver CameraDriver = new CameraDriver();
        internal static readonly Selector Selector = new Selector();
    }
    internal static class GenMapUI
    { internal static Vector2 LabelDrawPosFor(Thing thing, float offset) { return new Vector2(thing.Position.x * 10, thing.Position.z * 10); } }
}

namespace RimDroid.Readability
{
    internal sealed class ReadabilitySettings { internal ItemFilter Filter; }
    internal static class ReadabilityMod { internal static readonly ReadabilitySettings Settings = new ReadabilitySettings(); }
}
