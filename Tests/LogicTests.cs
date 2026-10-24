using System;
using System.Collections.Generic;
using RimDroid.Readability;

internal static class LogicTests
{
    private static int assertions;
    private static void Check(bool condition, string name)
    { assertions++; if (!condition) throw new Exception("FAILED: " + name); }
    private static void Main()
    {
        CatalogTests.Run(Check);
        Check((int)ItemFilter.Off == 0 && (int)ItemFilter.Medicine == 1 && (int)ItemFilter.Wood == 2, "legacy filter numeric values");
        Check(SettingsRules.Filter(ItemFilter.Wood) == ItemFilter.Wood && SettingsRules.Font(26) == 26, "legacy Wood/26 unchanged");
        Check(SettingsRules.Marker(16) == 16 && SettingsRules.Font(32) == 32, "new defaults and largest font");
        Check(SettingsRules.Font(-1) == 22 && SettingsRules.Marker(100) == 16 && SettingsRules.Filter((ItemFilter)90) == ItemFilter.Off,
            "corrupt values independently normalize");
        PreferenceValues migrated = SettingsRules.Load(ItemFilter.Wood, 26);
        Check(migrated.Filter == ItemFilter.Wood && migrated.Font == 26 && migrated.Marker == 16 && migrated.Group,
            "old Wood/26 fixture with missing new fields migrates to marker16/group true");
        migrated = SettingsRules.Load((ItemFilter)99, 26, -30, false);
        Check(migrated.Filter == ItemFilter.Off && migrated.Font == 26 && migrated.Marker == 16 && !migrated.Group,
            "corrupt fields do not overwrite independent valid preferences");
        for (int mask = 0; mask < 512; mask++)
        {
            TargetFacts f = new TargetFacts { PlayingMap = (mask & 1) != 0, Destroyed = (mask & 2) != 0,
                Spawned = (mask & 4) != 0, SameMap = (mask & 8) != 0, Fogged = (mask & 16) != 0,
                Colonist = (mask & 32) != 0, Item = (mask & 64) != 0, Haulable = (mask & 128) != 0, InView = (mask & 256) != 0 };
            bool expected = (mask & 31) == 13 && ((mask & 32) != 0 || (mask & 192) == 192);
            Check(TargetRules.Eligible(f) == expected, "catalogue eligibility combinations " + mask);
        }
        Check(!TargetRules.Eligible(new TargetFacts { PlayingMap = true, Spawned = true, SameMap = true }), "animal enemy building excluded");
        Check(!TargetRules.Eligible(new TargetFacts { PlayingMap = true, SameMap = true, Item = true, Haulable = true }), "inventory item excluded");
        TargetFacts navigation = new TargetFacts { PlayingMap = true, Spawned = true, SameMap = true, Colonist = true, InView = false };
        Check(TargetRules.Eligible(navigation), "Go accepts offscreen valid colonist");
        navigation.Destroyed = true; Check(!TargetRules.Eligible(navigation), "Go rejects destroyed target");
        navigation.Destroyed = false; navigation.Fogged = true; Check(!TargetRules.Eligible(navigation), "Go rejects newly fogged target");
        navigation.Fogged = false; navigation.SameMap = false; Check(!TargetRules.Eligible(navigation), "Go rejects target on another map");
        ItemFacts wood = new ItemFacts { Wood = true };
        Check(ItemRules.Matches(ItemFilter.Wood, wood, "Odun", " oDu "), "trimmed ordinal case insensitive search");
        Check(ItemRules.Matches(ItemFilter.Resource, wood, "Odun", ""), "wood also resource");
        Check(!ItemRules.Matches(ItemFilter.Off, wood, "Odun", ""), "off suppresses all items");
        Check(!ItemRules.Matches(ItemFilter.Medicine, wood, "Odun", ""), "wrong category excluded");
        Check(ItemRules.Matches(ItemFilter.All, wood, "çelik", "ÇEL"), "Turkish accented letters ordinal case matching");
        Check(!ItemRules.Matches(ItemFilter.All, wood, "İlaç", "ilaç"), "ordinal comparison preserves dotted I distinction");
        Check(ItemRules.Matches(ItemFilter.All, wood, "İlaç", "İLAÇ"), "Turkish name with identical dotted I matches");
        Check(ItemRules.Matches(ItemFilter.All, wood, new string('a', 600) + "eşya", "EŞYA"), "long names searchable in full");
        Check(!ItemRules.Matches(ItemFilter.All, wood, "Odun", "yok"), "no search match");
        ItemFacts allKinds = new ItemFacts { Medicine = true, Weapon = true, Food = true, Resource = true, Apparel = true };
        foreach (ItemFilter category in SettingsRules.FilterOrder)
            Check(ItemRules.Matches(category, allKinds, "Tür", null) == (category != ItemFilter.Off && category != ItemFilter.Wood), "category fixture " + category);
        List<TargetData> stacks = new List<TargetData> {
            new TargetData { Id = 2, TypeKey = "Wood", Name = "Odun", Amount = 30, OnScreen = true, X = 20, Y = 20 },
            new TargetData { Id = 1, TypeKey = "Wood", Name = "Odun", Amount = 20, OnScreen = true, X = 30, Y = 30 },
            new TargetData { Id = 3, TypeKey = "Wood", Name = "Odun", Amount = 7, OnScreen = false },
            new TargetData { Id = 4, TypeKey = "Med", Name = "İlaç", Amount = 5, OnScreen = true, X = 20, Y = 20 }
        };
        List<TargetGroup> groups = GroupRules.Visual(stacks, true);
        Check(groups.Count == 2 && groups[0].Amount == 50 && groups[0].Members.Count == 2, "20+30 is 50 items and two stacks; other types separate");
        Check(groups[0].Id == 1 && groups[0].X == 25 && groups[0].Y == 25, "stable minimum identifier and average anchor");
        Check(GroupRules.Visual(stacks, false).Count == 3, "ungrouped stacks exclude offscreen");
        groups = GroupRules.Types(stacks);
        Check(groups.Count == 2 && groups[0].Amount == 57 && groups[0].Members.Count == 3, "finder totals include offscreen, not marker budget");
        stacks[0].X = 64; stacks[1].X = 63;
        Check(GroupRules.Visual(stacks, true).Count == 3, "cell boundary splits groups");
        stacks.RemoveAt(2); // adapter removes fogged/destroyed members before pure grouping
        Check(GroupRules.Types(stacks)[0].Amount == 50, "ineligible member removed from total");
        QueryClock clock = new QueryClock(); object mapKey = new object(), gameKey = new object();
        Check(clock.Refresh(mapKey, gameKey, "wood", 1), "first query refresh");
        Check(!clock.Refresh(mapKey, gameKey, "wood", 1.24f), "query cached below 250ms");
        Check(clock.Refresh(mapKey, gameKey, "wood", 1.25f), "query refreshes at 250ms");
        Check(clock.Refresh(mapKey, gameKey, "search", 1.26f), "search/category change refreshes immediately");
        Check(clock.Refresh(new object(), gameKey, "search", 1.27f), "map switch invalidates");
        Check(clock.Refresh(mapKey, new object(), "search", 1.28f), "save change invalidates even with same map");
        Check(!clock.Refresh(null, null, "search", 1.29f), "world/no map clears cache");
        Check(clock.Refresh(mapKey, gameKey, "search", 1.30f), "map return refreshes");
        clock.Clear(); Check(clock.Refresh(mapKey, gameKey, "search", 1.31f), "explicit disable/settings reset refreshes");
        List<int> manyTargets = new List<int>(); for (int i = 0; i < 153; i++) manyTargets.Add(i);
        HashSet<int> reachable = new HashSet<int>();
        for (int pageIndex = 0; pageIndex < PageRules.Count(manyTargets.Count, 12); pageIndex++)
        {
            int p = pageIndex;
            foreach (int id in PageRules.Slice(manyTargets, ref p, 12)) Check(reachable.Add(id), "pages never duplicate a target");
        }
        Check(reachable.Count == 153, "finder reaches more than 120 targets without truncation");
        int stalePage = 50; manyTargets.RemoveRange(3, 150);
        Check(PageRules.Slice(manyTargets, ref stalePage, 12).Count == 3 && stalePage == 0, "deleted results clamp stale page");
        List<TargetData> denseTargets = new List<TargetData>();
        for (int i = 0; i < 200; i++) denseTargets.Add(new TargetData {
            Id = i, TypeKey = "Wood", Name = "Odun", Amount = 1, X = 100 + i % 20 * 80, Y = 100 + i / 20 * 80,
            OnScreen = true, Selected = i == 199, Colonist = i == 198 });
        LabelRect largeView = new LabelRect(0, 0, 2000, 2000);
        List<TargetGroup> denseGroups = GroupRules.Visual(denseTargets, false);
        List<PlannedMarker> budgeted = OverlayBudget.Select(denseGroups, largeView, default(LabelRect), 24);
        Check(budgeted.Count == 120 && budgeted[0].Group.Id == 199 && budgeted[1].Group.Id == 198, "marker budget prioritizes selected target then colonist");
        denseTargets.Reverse();
        List<PlannedMarker> reordered = OverlayBudget.Select(GroupRules.Visual(denseTargets, false), largeView, default(LabelRect), 24);
        for (int i = 0; i < budgeted.Count; i++) Check(budgeted[i].Group.Id == reordered[i].Group.Id, "input reordering preserves dense output " + i);
        Check(OverlayBudget.Omitted(200, 40) == 160 && OverlayBudget.Omitted(3, 1) == 2, "omitted groups count each missing label once");
        TargetGroup selectedGroup = denseGroups[0];
        LabelRect blockedControl = new LabelRect(selectedGroup.X - 30, selectedGroup.Y - 30, 60, 60);
        List<PlannedMarker> withControl = OverlayBudget.Select(denseGroups, largeView, blockedControl, 24);
        Check(withControl.Count == 120 && withControl[0].Group.Id == 198, "control obstruction does not consume marker budget");
        foreach (PlannedMarker marker in withControl) Check(!marker.Rect.Overlaps(blockedControl, 0), "no marker overlaps actual control");
        foreach (LabelRect screen in new[] { new LabelRect(0, 0, 640, 360), new LabelRect(0, 0, 360, 640),
            new LabelRect(0, 0, 1280, 720), new LabelRect(0, 0, 1920, 1080) })
        {
            LabelRect window = FinderGeometry.Window(screen.Width, screen.Height);
            LabelRect control = FinderGeometry.Control(screen.Width, screen.Height);
            Check(screen.Contains(window) && screen.Contains(control), "finder/control fit screen " + screen.Width);
            Check(control.Width >= 48 && control.Height >= 48, "finder control touch area minimum");
            int rows = FinderGeometry.Rows(window.Height - 36, true);
            Check(rows >= 1 && rows <= 12, "responsive page size bounded");
        }
        Check(FinderGeometry.Rows(300, true) < FinderGeometry.Rows(1000, true), "short screens reduce rows without reducing touch size");
        FinderState finderState = new FinderState(); finderState.SetTab(true);
        List<FinderRow> finderRows = FinderRows.Build(stacks, finderState);
        Check(finderRows.Count == 2 && finderRows[0].Group, "finder starts with type groups");
        finderState.OpenGroup("Wood"); finderRows = FinderRows.Build(stacks, finderState);
        Check(finderRows.Count == 2 && !finderRows[0].Group && finderRows[0].Id == 1 && finderRows[1].Id == 2,
            "type opens every concrete stack with stable identifiers");
        Check(finderRows[0].Detail.Contains("20 adet") && finderRows[0].Detail.Contains("#1"), "stack amount/coordinate/id disambiguation");
        finderState.Page = 4; finderState.ResetNavigation();
        Check(finderState.Page == 0 && finderState.TypeKey == null, "filter/search clear resets group and page");
        finderState.OpenGroup("deleted");
        Check(FinderRows.Build(stacks, finderState).Count == 0, "removed group becomes empty instead of stale target");
        finderState.SetTab(false);
        Check(finderState.TypeKey == null && FinderRows.Build(stacks, finderState).Count == 0, "tab reset and empty colonist state");
        finderState.SetTab(true); finderState.OpenGroup("Wood");
        List<TargetData> largeGroup = new List<TargetData>();
        for (int i = 0; i < 153; i++) largeGroup.Add(new TargetData { Id = i, Name = "Odun", TypeKey = "Wood", Amount = 2 });
        finderRows = FinderRows.Build(largeGroup, finderState); reachable.Clear();
        for (int pageIndex = 0; pageIndex < PageRules.Count(finderRows.Count, 3); pageIndex++)
        {
            int p = pageIndex;
            foreach (FinderRow row in PageRules.Slice(finderRows, ref p, 3)) reachable.Add(row.Id);
        }
        Check(reachable.Count == 153, "small-screen type details reach every stack");
        HookLifecycle hookSet = new HookLifecycle(); List<int> installedHooks = new List<int>(); int rollbacks = 0;
        bool partialFailed = false;
        try
        {
            hookSet.Install(2, delegate(int index) { if (index == 1) throw new MissingMethodException(); installedHooks.Add(index); },
                delegate { installedHooks.Clear(); rollbacks++; });
        }
        catch (MissingMethodException) { partialFailed = true; }
        Check(partialFailed && !hookSet.Active && installedHooks.Count == 0 && rollbacks == 1, "second hook failure rolls back partial install");
        hookSet.Remove(delegate { rollbacks++; });
        Check(rollbacks == 1, "cleanup is idempotent after failed install");
        hookSet.Install(2, installedHooks.Add, delegate { installedHooks.Clear(); });
        Check(hookSet.Active && installedHooks.Count == 2, "both hooks needed before feature readiness");
        hookSet.Remove(delegate { installedHooks.Clear(); rollbacks++; });
        Check(!hookSet.Active && installedHooks.Count == 0 && rollbacks == 2, "runtime failure removes owned hooks");
        SessionScope session = new SessionScope(); session.Bind(mapKey, gameKey);
        Check(session.Matches(mapKey, gameKey), "active window binds current map and game");
        Check(!session.Matches(new object(), gameKey) && !session.Matches(mapKey, new object()), "window rejects changed map/save before input");
        session.Clear(); Check(!session.Matches(mapKey, gameKey), "disabled/reset window loses stale scope");
        LabelRect placed;
        LabelLayout layout = new LabelLayout();
        LabelRect viewport = new LabelRect(8, 8, 500, 400);
        layout.Reset(viewport);
        LabelRect blockingControl = new LabelRect(150, 110, 200, 60); layout.SetControl(blockingControl);
        Check(layout.TryPlace(250, 200, 100, 40, out placed, 24) && !placed.Overlaps(blockingControl, 0), "label switches placement when preferred position hits control");
        foreach (int size in new[] { 8, 16, 24 })
        {
            layout.Reset(viewport);
            LabelRect bigMarker;
            Check(AnnotationRules.TryMarker(250, 200, viewport, size, out bigMarker) && bigMarker.Width == size, "independent marker dimension " + size);
            layout.AddObstacle(bigMarker);
            LabelRect control = new LabelRect(8, 250, 200, 56); layout.SetControl(control);
            Check(layout.TryPlace(250, 200, 210, 44, out placed, size), "large font label can fit near marker " + size);
            Check(!placed.Overlaps(bigMarker, 0) && !placed.Overlaps(control, 0), "label avoids real marker and control " + size);
            Check(!AnnotationRules.TryMarker(8, 8, viewport, size, out bigMarker), "partly clipped enlarged marker rejected " + size);
            Check(!layout.TryPlace(100, 300, 2000, 44, out placed, size), "long label rejected without overflowing " + size);
        }
        layout.Reset(viewport);
        Check(layout.TryPlace(100, 100, 80, 24, out placed), "normal placement");
        LabelRect first = placed;
        Check(viewport.Contains(first), "normal label in viewport");
        Check(layout.TryPlace(100, 100, 80, 24, out placed), "alternate placement at same anchor");
        Check(!first.Overlaps(placed, 0), "labels do not overlap");
        layout.Reset(viewport);
        Check(layout.TryPlace(100, 100, 80, 24, out placed)
            && placed.X == first.X && placed.Y == first.Y, "stable ordering gives deterministic position");
        layout.Reset(viewport);
        Check(layout.TryPlace(12, 100, 80, 24, out placed), "edge can choose a fitting alternative");
        Check(viewport.Contains(placed), "edge label fully contained");
        Check(!layout.TryPlace(12, 12, 80, 24, out placed), "corner without space omits label");
        Check(!layout.TryPlace(100, 100, 1000, 1000, out placed), "oversized label rejected");
        Check(!layout.TryPlace(float.NaN, 1, 20, 20, out placed), "NaN anchor rejected");
        Check(!layout.TryPlace(20, 20, float.PositiveInfinity, 20, out placed), "infinite dimensions rejected");
        Check(!layout.TryPlace(20, 20, -1, 20, out placed), "negative width rejected");
        layout.Reset(new LabelRect(0, 0, 10000, 10000));
        for (int i = 0; i < LabelLayout.MaximumLabels; i++)
            Check(layout.TryPlace(100 + i * 100, 100, 40, 20, out placed), "cap placement " + i);
        Check(!layout.TryPlace(6000, 100, 40, 20, out placed), "label cap enforced");
        layout.Reset(new LabelRect(8, 8, 500, 400));
        Check(layout.Count == 0, "layout reset clears occupied rectangles");
        layout.Reset(new LabelRect(0, 0, -1, 10));
        Check(!layout.TryPlace(0, 0, 10, 10, out placed), "invalid viewport rejected");
        for (int mask = 0; mask < 32; mask++)
        {
            bool destroyed = (mask & 1) != 0, spawned = (mask & 2) != 0;
            bool sameMap = (mask & 4) != 0, inView = (mask & 8) != 0, fogged = (mask & 16) != 0;
            Check(AnnotationRules.Visible(destroyed, spawned, sameMap, inView, fogged)
                == (mask == 14), "visibility truth table " + mask);
        }
        Check(AnnotationRules.Compare(true, 99, false, 1) < 0, "colonists take priority over items");
        Check(AnnotationRules.Compare(false, 1, false, 2) < 0, "stable item identifiers");
        Check(AnnotationRules.Compare(true, 1, true, 1) == 0, "same identifier stable");
        Check(!AnnotationRules.NativeAllowed(false, true, "Linux 6.8", "/managed"), "desktop Linux rejected even with Native marker");
        Check(!AnnotationRules.NativeAllowed(true, false, "Android OS 16", null), "Android without Native marker rejected");
        Check(!AnnotationRules.NativeAllowed(true, false, "Android OS 16", ""), "empty Native marker rejected");
        Check(AnnotationRules.NativeAllowed(true, false, "Android OS 16", "/managed"), "ordinary Android Native route admitted");
        Check(AnnotationRules.NativeAllowed(false, true, "Android OS 16 / API-36", "/overrides:/game/Managed"),
            "regression: RimDroid 0.3.0 Native LinuxPlayer on Android is admitted");
        Check(!AnnotationRules.NativeAllowed(false, true, "Android OS 16", null), "Box64/native Mono cannot pass without Native engine marker");
        Check(!AnnotationRules.NativeAllowed(false, true, "Linux", null), "emulated Linux player rejected");
        Check(!AnnotationRules.NativeAllowed(false, false, "Android OS 16", "/managed"), "unrelated platform rejected even with Android text and marker");
        Check(!AnnotationRules.NativeAllowed(false, true, null, "/managed"), "missing Android host evidence fails closed on LinuxPlayer");
        Check(!AnnotationRules.NativeAllowed(false, true, "Androidish", "/managed"), "OS name lookalike rejected");
        Check(!AnnotationRules.NativeAllowed(false, true, "Android OS 16", "  "), "blank Native marker rejected");
        Check(AnnotationRules.NativeAllowed(true, false, null, "/managed"), "genuine Android player remains supported without OS fallback");
        Check(AnnotationRules.NativeAllowed(false, true, " android OS 16 ", "/managed"), "Android OS case and surrounding whitespace normalized");
        Check(AnnotationRules.VersionAllowed("1.6.4633 rev1254"), "reference version admitted");
        Check(!AnnotationRules.VersionAllowed("1.7.1"), "1.7 rejected");
        Check(!AnnotationRules.VersionAllowed("1.60.1"), "lookalike version rejected");
        Check(!AnnotationRules.VersionAllowed(null), "unknown version rejected");

        // Dense placement invariants, not a reproduction of the placement loop.
        viewport = new LabelRect(0, 0, 500, 400);
        layout.Reset(viewport);
        List<LabelRect> retained = new List<LabelRect>();
        List<LabelRect> obstacles = new List<LabelRect>();
        for (int i = 0; i < AnnotationRules.MaximumMarkers; i++)
        {
            LabelRect marker = new LabelRect(20 + i % 12 * 38, 35 + i / 12 * 32, 8, 8);
            Check(layout.AddObstacle(marker), "marker budget accepts visible marker " + i);
            obstacles.Add(marker);
        }
        Check(!layout.AddObstacle(new LabelRect(1, 1, 8, 8)), "marker obstacle budget enforced");
        Check(layout.ObstacleCount == AnnotationRules.MaximumMarkers, "marker budget stays bounded");
        for (int i = 0; i < 240; i++)
            if (layout.TryPlace(24 + i % 12 * 38, 39 + i % 10 * 32, 60, 24, out placed))
            {
                Check(viewport.Contains(placed), "dense retained label stays in viewport");
                foreach (LabelRect prior in retained) Check(!prior.Overlaps(placed, 0), "dense pairwise label exclusion");
                foreach (LabelRect marker in obstacles) Check(!marker.Overlaps(placed, 0), "dense labels avoid every marker");
                retained.Add(placed);
            }
        Check(retained.Count > 0 && retained.Count <= LabelLayout.MaximumLabels, "dense layout yields bounded visible labels");

        BoundedCandidates<int> bounded = new BoundedCandidates<int>(AnnotationRules.MaximumMarkers,
            delegate(int a, int b) { return a.CompareTo(b); });
        for (int i = 999; i >= 0; i--) bounded.Add(i);
        Check(bounded.Count == AnnotationRules.MaximumMarkers, "candidate storage limited to actual marker budget");
        for (int i = 0; i < bounded.Count; i++) Check(bounded[i] == i, "best candidates retained in stable order " + i);
        bounded.Add(1);
        Check(bounded.Count == AnnotationRules.MaximumMarkers, "duplicate candidates do not grow storage");
        bounded.Clear();
        Check(bounded.Count == 0, "candidate helper clear empties storage");

        // Cell-visible objects outside the drawable GUI must not consume the candidate budget.
        viewport = new LabelRect(8, 8, 500, 400);
        Check(AnnotationRules.TryMarker(12, 12, viewport, out placed), "marker fits at inclusive viewport boundary");
        Check(!AnnotationRules.TryMarker(11, 12, viewport, out placed), "partly clipped marker rejected");
        Check(!AnnotationRules.TryMarker(float.NaN, 100, viewport, out placed), "NaN marker rejected");
        Check(!AnnotationRules.TryMarker(100, float.PositiveInfinity, viewport, out placed), "infinite marker rejected");
        for (int i = 0; i < AnnotationRules.MaximumMarkers; i++)
            if (AnnotationRules.TryMarker(7, 100, viewport, out placed)) bounded.Add(i);
        Check(bounded.Count == 0, "offscreen low-ID candidates do not consume retained capacity");
        if (AnnotationRules.TryMarker(100, 100, viewport, out placed)) bounded.Add(1000);
        Check(bounded.Count == 1 && bounded[0] == 1000, "drawable higher-ID candidate survives edge saturation");

        AssemblyGate gate = new AssemblyGate();
        Check(!gate.Ready, "assembly gate starts closed");
        Check(!gate.MarkInitialized(false) && !gate.Ready, "missing API keeps runtime gate closed");
        gate.ObserveReplacement();
        Check(!gate.MarkInitialized() && !gate.Ready, "replacement before initialization cannot be overwritten");
        Check(gate.ConsumeReplacement() && !gate.ConsumeReplacement(), "replacement handled once on main thread");
        gate = new AssemblyGate();
        Check(gate.MarkInitialized() && gate.Ready, "normal initial readiness");
        gate.ObserveReplacement();
        Check(!gate.Ready, "late loader notification immediately closes computed readiness");
        Check(!gate.MarkInitialized(), "repeated initialization cannot clear sticky replacement flag");
        Check(gate.ConsumeReplacement(), "late notification remains pending despite closed Ready gate");
        gate.Disable();
        Check(!gate.Ready && !gate.ConsumeReplacement(), "disabled gate stays closed without duplicate handling");
        Console.WriteLine("PASS: " + assertions + " standalone catalogue/layout/visibility/runtime-gate assertions; no game code loaded");
    }
}
