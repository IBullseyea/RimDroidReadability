using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal static class TargetCatalog
    {
        private sealed class Entry
        {
            internal Thing Thing;
            internal bool Colonist;
        }
        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly Dictionary<int, Entry> byId = new Dictionary<int, Entry>();
        private static readonly QueryClock clock = new QueryClock();
        internal static string Search = "";

        internal static bool Matches(Thing thing)
        {
            ThingDef def = thing.def;
            return ItemRules.Matches(ReadabilityMod.Settings.Filter, new ItemFacts {
                Medicine = def.IsMedicine, Wood = def == ThingDefOf.WoodLog, Weapon = def.IsWeapon,
                Food = def.ingestible != null && def.ingestible.CachedNutrition > 0,
                Resource = def.CountAsResource, Apparel = def.IsApparel
            }, def.label, Search);
        }

        internal static bool InMap
        {
            get { return Current.ProgramState == ProgramState.Playing && WorldRendererUtility.DrawingMap
                && Current.Game != null && Find.CurrentMap != null && Find.CameraDriver != null; }
        }

        internal static void Clear()
        { entries.Clear(); byId.Clear(); clock.Clear(); }

        internal static bool Eligible(Thing thing, bool colonist)
        {
            if (thing == null || !InMap || thing.Destroyed || !thing.Spawned || thing.Map != Find.CurrentMap) return false;
            Pawn pawn = thing as Pawn;
            return TargetRules.Eligible(new TargetFacts {
                PlayingMap = InMap, Destroyed = thing.Destroyed, Spawned = thing.Spawned, SameMap = thing.Map == Find.CurrentMap,
                Fogged = Find.CurrentMap.fogGrid.IsFogged(thing.Position),
                InView = Find.CameraDriver.CurrentViewRect.Contains(thing.Position),
                Colonist = colonist && pawn != null && Find.CurrentMap.mapPawns.FreeColonistsSpawned.Contains(pawn),
                Item = !colonist && thing.def.category == ThingCategory.Item,
                Haulable = !colonist && thing.def.EverHaulable
            });
        }

        internal static void Ensure()
        {
            if (!InMap) { Clear(); return; }
            Map map = Find.CurrentMap;
            if (!clock.Refresh(map, Current.Game, (int)ReadabilityMod.Settings.Filter + ":" + Search,
                Time.realtimeSinceStartup, ModText.CurrentLanguage)) return;
            entries.Clear(); byId.Clear();
            // MapPawns rebuilds and returns a shared scratch list. Eligible() queries it again,
            // so iterating the borrowed list itself invalidates its enumerator on the first pawn.
            Pawn[] colonists = map.mapPawns.FreeColonistsSpawned.ToArray();
            foreach (Pawn pawn in colonists) Add(pawn, true);
            if (ReadabilityMod.Settings.Filter != ItemFilter.Off)
            {
                Thing[] items = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver).ToArray();
                foreach (Thing thing in items) Add(thing, false);
            }
            entries.Sort(delegate(Entry a, Entry b) { return a.Thing.thingIDNumber.CompareTo(b.Thing.thingIDNumber); });
        }

        private static void Add(Thing thing, bool colonist)
        {
            if (!Eligible(thing, colonist) || byId.ContainsKey(thing.thingIDNumber)) return;
            Entry entry = new Entry { Thing = thing, Colonist = colonist };
            entries.Add(entry); byId.Add(thing.thingIDNumber, entry);
        }

        internal static List<TargetData> Snapshot(bool project)
        {
            Ensure();
            List<TargetData> result = new List<TargetData>(entries.Count);
            foreach (Entry entry in entries)
            {
                Thing thing = entry.Thing;
                if (!Eligible(thing, entry.Colonist) || (!entry.Colonist && !Matches(thing))) continue;
                TargetData data = new TargetData { Id = thing.thingIDNumber, Colonist = entry.Colonist,
                    TypeKey = thing.def.defName, Name = entry.Colonist ? ((Pawn)thing).LabelShort : thing.def.label,
                    Amount = entry.Colonist ? 1 : thing.stackCount, CellX = thing.Position.x, CellZ = thing.Position.z,
                    Selected = Find.Selector.IsSelected(thing) };
                if (project && Find.CameraDriver.CurrentViewRect.Contains(thing.Position))
                {
                    Vector2 anchor = GenMapUI.LabelDrawPosFor(thing, 0f);
                    data.OnScreen = true; data.X = anchor.x; data.Y = anchor.y;
                }
                result.Add(data);
            }
            return result;
        }

        internal static Thing Resolve(int id)
        {
            Ensure();
            Entry entry;
            return byId.TryGetValue(id, out entry) && Eligible(entry.Thing, entry.Colonist)
                && (entry.Colonist || Matches(entry.Thing)) ? entry.Thing : null;
        }
    }
}
