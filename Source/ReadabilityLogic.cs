using System;
using System.Collections.Generic;

namespace RimDroid.Readability
{
    // Saved values 0/1/2 are part of the 0.1.0 settings format.
    internal enum ItemFilter { Off = 0, Medicine = 1, Wood = 2, All = 3, Weapon = 4, Food = 5, Resource = 6, Apparel = 7 }

    internal static class SettingsRules
    {
        internal static PreferenceValues Load(ItemFilter filter, int font, int? marker = null, bool? group = null)
        { return new PreferenceValues { Filter = Filter(filter), Font = Font(font), Marker = Marker(marker ?? 16), Group = group ?? true }; }
        internal static int Font(int value) { return value == 18 || value == 22 || value == 26 || value == 32 ? value : 22; }
        internal static int Marker(int value) { return value == 8 || value == 16 || value == 24 ? value : 16; }
        internal static ItemFilter Filter(ItemFilter value) { return Enum.IsDefined(typeof(ItemFilter), value) ? value : ItemFilter.Off; }
        internal static readonly ItemFilter[] FilterOrder = { ItemFilter.Off, ItemFilter.All, ItemFilter.Medicine,
            ItemFilter.Wood, ItemFilter.Weapon, ItemFilter.Food, ItemFilter.Resource, ItemFilter.Apparel };
        internal static string FilterName(ItemFilter value)
        { return ModText.Get("Filter" + Filter(value)); }
        internal static ItemFilter NextFilter(ItemFilter value)
        { return FilterOrder[(Array.IndexOf(FilterOrder, value) + 1) % FilterOrder.Length]; }
    }

    internal struct PreferenceValues
    {
        internal ItemFilter Filter;
        internal int Font, Marker;
        internal bool Group;
    }

    internal struct TargetFacts
    {
        internal bool PlayingMap, Destroyed, Spawned, SameMap, Fogged, Colonist, Item, Haulable, InView;
    }

    internal static class TargetRules
    {
        // Camera visibility deliberately does not participate in catalogue/navigation eligibility.
        internal static bool Eligible(TargetFacts facts)
        {
            return facts.PlayingMap && !facts.Destroyed && facts.Spawned && facts.SameMap && !facts.Fogged
                && (facts.Colonist || (facts.Item && facts.Haulable));
        }
    }

    internal struct ItemFacts
    {
        internal bool Medicine, Wood, Weapon, Food, Resource, Apparel;
    }

    internal static class ItemRules
    {
        internal static bool Matches(ItemFilter filter, ItemFacts facts, string name, string search)
        {
            bool category;
            switch (filter) {
                case ItemFilter.All: category = true; break;
                case ItemFilter.Medicine: category = facts.Medicine; break;
                case ItemFilter.Wood: category = facts.Wood; break;
                case ItemFilter.Weapon: category = facts.Weapon; break;
                case ItemFilter.Food: category = facts.Food; break;
                case ItemFilter.Resource: category = facts.Resource || facts.Wood; break;
                case ItemFilter.Apparel: category = facts.Apparel; break;
                default: return false;
            }
            return category && (name ?? "").IndexOf((search ?? "").Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal sealed class TargetData
    {
        internal int Id, Amount, CellX, CellZ;
        internal string TypeKey, Name;
        internal bool Colonist, Selected, OnScreen;
        internal float X, Y;
    }

    internal sealed class QueryClock
    {
        private object map, game, language;
        private string key;
        private float due;
        internal void Clear() { map = null; game = null; language = null; key = null; due = 0; }
        internal bool Refresh(object currentMap, object currentGame, string currentKey, float now, object currentLanguage = null)
        {
            if (currentMap == null || currentGame == null) { Clear(); return false; }
            if (ReferenceEquals(map, currentMap) && ReferenceEquals(game, currentGame) && ReferenceEquals(language, currentLanguage)
                && key == currentKey && now < due) return false;
            map = currentMap; game = currentGame; language = currentLanguage; key = currentKey; due = now + 0.25f;
            return true;
        }
    }

    internal static class PageRules
    {
        internal static int Count(int count, int size) { return Math.Max(1, (count + Math.Max(1, size) - 1) / Math.Max(1, size)); }
        internal static List<T> Slice<T>(List<T> source, ref int page, int size)
        {
            size = Math.Max(1, size); page = Math.Max(0, Math.Min(page, Count(source.Count, size) - 1));
            int start = page * size;
            return source.GetRange(start, Math.Min(size, source.Count - start));
        }
    }

    internal sealed class TargetGroup
    {
        internal readonly List<TargetData> Members = new List<TargetData>();
        internal int Id { get { return Members[0].Id; } }
        internal string TypeKey { get { return Members[0].TypeKey; } }
        internal string Name { get { return Members[0].Name; } }
        internal bool Colonist { get { return Members[0].Colonist; } }
        internal bool Selected;
        internal long Amount;
        internal float X, Y;
        internal void Add(TargetData target)
        {
            Members.Add(target); Amount += target.Amount; Selected |= target.Selected;
            X += (target.X - X) / Members.Count; Y += (target.Y - Y) / Members.Count;
        }
        internal string Summary
        { get { return Colonist ? ModText.Get("ColonistLabel", ModText.Get("ColonistShort"), Name) : Members.Count > 1
            ? ModText.Get("ItemGroupLabel", ModText.Get("ItemShort"), Name, Amount, ModText.Get("Stacks"), Members.Count)
            : ModText.Get("ItemLabel", ModText.Get("ItemShort"), Name, Amount); } }
    }

    internal static class GroupRules
    {
        internal static int Compare(TargetGroup left, TargetGroup right)
        {
            if (left.Selected != right.Selected) return left.Selected ? -1 : 1;
            return AnnotationRules.Compare(left.Colonist, left.Id, right.Colonist, right.Id);
        }
        internal static List<TargetGroup> Visual(List<TargetData> targets, bool grouped)
        { return Build(targets, grouped, true); }
        internal static List<TargetGroup> Types(List<TargetData> targets)
        { return Build(targets, true, false); }
        private static List<TargetGroup> Build(List<TargetData> targets, bool grouped, bool visual)
        {
            List<TargetData> ordered = new List<TargetData>(targets);
            ordered.Sort(delegate(TargetData a, TargetData b) { return a.Id.CompareTo(b.Id); });
            Dictionary<string, TargetGroup> lookup = new Dictionary<string, TargetGroup>(StringComparer.Ordinal);
            HashSet<int> seen = new HashSet<int>();
            List<TargetGroup> groups = new List<TargetGroup>();
            foreach (TargetData target in ordered)
            {
                if (!seen.Add(target.Id) || (visual ? !target.OnScreen : target.Colonist)) continue;
                string key = !grouped || target.Colonist ? "id:" + target.Id : "type:" + target.TypeKey;
                if (visual && grouped && !target.Colonist)
                    key += ":" + Math.Floor(target.X / 64) + ":" + Math.Floor(target.Y / 64);
                TargetGroup group;
                if (!lookup.TryGetValue(key, out group))
                { group = new TargetGroup(); lookup.Add(key, group); groups.Add(group); }
                group.Add(target);
            }
            groups.Sort(visual ? (Comparison<TargetGroup>)Compare : delegate(TargetGroup a, TargetGroup b) {
                int name = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
                return name != 0 ? name : StringComparer.Ordinal.Compare(a.TypeKey, b.TypeKey);
            });
            return groups;
        }
    }

    internal sealed class PlannedMarker
    {
        internal TargetGroup Group;
        internal LabelRect Rect;
    }

    internal static class OverlayBudget
    {
        internal static List<PlannedMarker> Select(List<TargetGroup> groups, LabelRect bounds, LabelRect control, int markerSize,
            LabelRect secondControl = default(LabelRect))
        {
            List<TargetGroup> ordered = new List<TargetGroup>(groups); ordered.Sort(GroupRules.Compare);
            List<PlannedMarker> result = new List<PlannedMarker>(AnnotationRules.MaximumMarkers);
            foreach (TargetGroup group in ordered)
            {
                LabelRect marker;
                if (result.Count == AnnotationRules.MaximumMarkers) break;
                if (!AnnotationRules.TryMarker(group.X, group.Y, bounds, markerSize, out marker)
                    || (control.IsFinite && marker.Overlaps(control, 3))
                    || (secondControl.IsFinite && marker.Overlaps(secondControl, 3))) continue;
                result.Add(new PlannedMarker { Group = group, Rect = marker });
            }
            return result;
        }
        internal static int Omitted(int groups, int labelled) { return Math.Max(0, groups - labelled); }
    }

    internal static class FinderGeometry
    {
        internal const int Touch = 48, RowHeight = 64;
        internal static LabelRect Window(float width, float height)
        { return new LabelRect(8, 8, Math.Min(900, width - 16), Math.Min(850, height - 16)); }
        internal static LabelRect Control(float width, float height)
        { return new LabelRect(12, Math.Max(12, height - 150), Math.Min(200, width - 24), 56); }
        internal static int Rows(float contentHeight, bool items)
        { return Math.Max(1, Math.Min(12, (int)((contentHeight - (items ? 312 : 200)) / RowHeight))); }
        internal static bool FitsActions(float contentWidth)
        { return contentWidth >= 2 * Touch + 8 + 20; }
    }

    internal static class FilterShortcutGeometry
    {
        internal const float Width = 192, Height = 48, VisibilityPanelWidth = 141, BottomBarHeight = 35, Gap = 8;
        internal static LabelRect Bounds(float screenWidth, float screenHeight)
        {
            LabelRect viewport = new LabelRect(Gap, Gap, screenWidth - 2 * Gap, screenHeight - 2 * Gap);
            LabelRect button = new LabelRect(screenWidth - VisibilityPanelWidth - Gap - Width,
                screenHeight - BottomBarHeight - Gap - Height, Width, Height);
            return viewport.IsFinite && button.IsFinite && viewport.Contains(button)
                && !button.Overlaps(FinderGeometry.Control(screenWidth, screenHeight), 3) ? button : default(LabelRect);
        }
    }

    internal static class MapControlRules
    {
        internal static bool BlockedByWindow(bool ownMapControl, bool immediateHud, bool dialogOrHigher)
        { return !ownMapControl && !immediateHud && dialogOrHigher; }
    }

    internal sealed class FinderState
    {
        internal bool Items;
        internal string TypeKey;
        internal int Page;
        internal void ResetNavigation() { TypeKey = null; Page = 0; }
        internal void SetTab(bool items) { if (Items == items) return; Items = items; ResetNavigation(); }
        internal void OpenGroup(string key) { TypeKey = key; Page = 0; }
    }

    internal sealed class FinderRow
    {
        internal int Id;
        internal string TypeKey, Name, Detail;
        internal bool Group;
    }

    internal static class FinderRows
    {
        internal static List<FinderRow> Build(List<TargetData> targets, FinderState state)
        {
            List<FinderRow> rows = new List<FinderRow>();
            if (state.Items && state.TypeKey == null)
            {
                foreach (TargetGroup group in GroupRules.Types(targets))
                    rows.Add(new FinderRow { Group = true, TypeKey = group.TypeKey, Name = group.Name,
                        Detail = ModText.Get("GroupTotal", ModText.Get("Quantity"), group.Amount, ModText.Get("Stacks"), group.Members.Count) });
            }
            else
            {
                foreach (TargetData target in targets)
                    if (state.Items ? !target.Colonist && target.TypeKey == state.TypeKey : target.Colonist)
                        rows.Add(new FinderRow { Id = target.Id, Name = target.Name, Detail = target.Colonist ? ModText.Get("Colonist") :
                            ModText.Get("StackDetails", ModText.Get("Quantity"), target.Amount, target.CellX, target.CellZ, target.Id) });
                rows.Sort(delegate(FinderRow a, FinderRow b) { return a.Id.CompareTo(b.Id); });
            }
            return rows;
        }
    }

    internal sealed class HookLifecycle
    {
        internal bool Active { get; private set; }
        private bool attempted;
        internal void Install(int count, Action<int> patch, Action rollback)
        {
            Active = false; attempted = true;
            try { for (int i = 0; i < count; i++) patch(i); Active = true; }
            catch { Remove(rollback); throw; }
        }
        internal void Remove(Action rollback)
        {
            Active = false;
            if (!attempted) return;
            attempted = false; rollback();
        }
    }

    internal sealed class SessionScope
    {
        private object map, game;
        internal bool Matches(object currentMap, object currentGame)
        { return map != null && game != null && ReferenceEquals(map, currentMap) && ReferenceEquals(game, currentGame); }
        internal void Bind(object currentMap, object currentGame) { map = currentMap; game = currentGame; }
        internal void Clear() { map = null; game = null; }
    }
}
