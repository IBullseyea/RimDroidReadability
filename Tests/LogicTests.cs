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
        LabelRect placed;
        LabelLayout layout = new LabelLayout();
        LabelRect viewport = new LabelRect(8, 8, 500, 400);
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
        Check(!AnnotationRules.NativeAllowed(false, "/managed"), "desktop rejected");
        Check(!AnnotationRules.NativeAllowed(true, null), "Android without Native marker rejected");
        Check(!AnnotationRules.NativeAllowed(true, ""), "empty Native marker rejected");
        Check(AnnotationRules.NativeAllowed(true, "/managed"), "Native Android gate admitted");
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
        Console.WriteLine("PASS: " + assertions + " standalone layout/visibility/runtime-gate assertions; no game code loaded");
    }
}
