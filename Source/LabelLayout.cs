using System;
using System.Collections.Generic;

namespace RimDroid.Readability
{
    // Deliberately independent of Unity/game assemblies so geometry can be tested offline.
    internal struct LabelRect
    {
        internal readonly float X, Y, Width, Height;
        internal LabelRect(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
        internal float Right { get { return X + Width; } }
        internal float Bottom { get { return Y + Height; } }
        internal bool IsFinite
        {
            get { return Finite(X) && Finite(Y) && Finite(Width) && Finite(Height)
                && Width > 0 && Height > 0 && Finite(Right) && Finite(Bottom); }
        }
        internal static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        internal bool Contains(LabelRect other)
        { return other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom; }
        internal bool Overlaps(LabelRect other, float gap)
        { return X < other.Right + gap && Right + gap > other.X && Y < other.Bottom + gap && Bottom + gap > other.Y; }
    }

    internal sealed class LabelLayout
    {
        internal const int MaximumLabels = 40;
        private readonly List<LabelRect> occupied = new List<LabelRect>(MaximumLabels);
        private readonly List<LabelRect> obstacles = new List<LabelRect>(AnnotationRules.MaximumMarkers);
        private LabelRect viewport;
        internal int Count { get { return occupied.Count; } }
        internal int ObstacleCount { get { return obstacles.Count; } }
        internal void Reset(LabelRect bounds) { viewport = bounds; occupied.Clear(); obstacles.Clear(); }
        internal bool AddObstacle(LabelRect marker)
        {
            if (obstacles.Count >= AnnotationRules.MaximumMarkers || !viewport.IsFinite || !marker.IsFinite || !viewport.Contains(marker))
                return false;
            obstacles.Add(marker);
            return true;
        }
        internal bool TryPlace(float x, float y, float width, float height, out LabelRect placed)
        {
            placed = default(LabelRect);
            if (!viewport.IsFinite || !LabelRect.Finite(x) || !LabelRect.Finite(y)
                || !LabelRect.Finite(width) || !LabelRect.Finite(height) || width <= 0 || height <= 0
                || occupied.Count >= MaximumLabels) return false;
            // Fixed ordering and short offsets keep annotations attached to their anchor.
            for (int i = 0; i < 4; i++)
            {
                float px = i == 1 ? x + 10 : i == 2 ? x - width - 10 : x - width / 2;
                float py = i == 0 ? y - height - 10 : i == 3 ? y + 10 : y - height / 2;
                LabelRect candidate = new LabelRect(px, py, width, height);
                if (!candidate.IsFinite || !viewport.Contains(candidate)) continue;
                bool collision = false;
                for (int j = 0; j < occupied.Count; j++)
                    if (candidate.Overlaps(occupied[j], 3)) { collision = true; break; }
                for (int j = 0; !collision && j < obstacles.Count; j++)
                    if (candidate.Overlaps(obstacles[j], 2)) collision = true;
                if (collision) continue;
                occupied.Add(candidate);
                placed = candidate;
                return true;
            }
            return false;
        }
    }

    internal sealed class BoundedCandidates<T>
    {
        private readonly int maximum;
        private readonly Comparer<T> comparer;
        private readonly List<T> values;
        internal BoundedCandidates(int maximum, Comparison<T> comparison)
        {
            this.maximum = maximum;
            comparer = Comparer<T>.Create(comparison);
            values = new List<T>(maximum);
        }
        internal int Count { get { return values.Count; } }
        internal T this[int index] { get { return values[index]; } }
        internal void Clear() { values.Clear(); }
        internal void Add(T value)
        {
            int index = values.BinarySearch(value, comparer);
            if (index >= 0) return;
            index = ~index;
            if (values.Count == maximum)
            {
                if (index >= maximum) return;
                values.RemoveAt(maximum - 1); // Remove first so even List capacity never grows beyond the budget.
            }
            values.Insert(index, value);
        }
    }

    internal sealed class AssemblyGate
    {
        private volatile bool replaced;
        private bool initialized;
        private bool replacementHandled;
        internal bool Ready { get { return initialized && !replaced; } }
        internal bool ReplacementObserved { get { return replaced; } }
        internal void ObserveReplacement() { replaced = true; }
        internal bool MarkInitialized() { initialized = true; return Ready; }
        internal void Disable() { initialized = false; }
        // Called only on the Unity/main thread. The loader callback only sets the sticky volatile flag.
        internal bool ConsumeReplacement()
        {
            if (!replaced || replacementHandled) return false;
            replacementHandled = true;
            return true;
        }
    }

    internal static class AnnotationRules
    {
        internal const int MaximumMarkers = 120;
        internal static bool NativeAllowed(bool android, string marker)
        { return android && !string.IsNullOrEmpty(marker); }
        internal static bool VersionAllowed(string version)
        { return version != null && version.StartsWith("1.6.", StringComparison.Ordinal); }
        internal static bool Visible(bool destroyed, bool spawned, bool sameMap, bool inView, bool fogged)
        { return !destroyed && spawned && sameMap && inView && !fogged; }
        internal static bool TryMarker(float x, float y, LabelRect bounds, out LabelRect marker)
        {
            marker = default(LabelRect);
            if (!bounds.IsFinite || !LabelRect.Finite(x) || !LabelRect.Finite(y)) return false;
            marker = new LabelRect(x - 4, y - 4, 8, 8);
            return marker.IsFinite && bounds.Contains(marker);
        }
        internal static int Compare(bool leftColonist, int leftId, bool rightColonist, int rightId)
        {
            if (leftColonist != rightColonist) return leftColonist ? -1 : 1;
            return leftId.CompareTo(rightId);
        }
    }
}
