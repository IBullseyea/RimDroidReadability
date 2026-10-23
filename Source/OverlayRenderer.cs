using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal static class OverlayRenderer
    {
        private struct Candidate
        {
            internal Thing Thing;
            internal bool Colonist;
            internal Candidate(Thing thing, bool colonist) { Thing = thing; Colonist = colonist; }
        }

        private struct Marker
        {
            internal Candidate Candidate;
            internal Vector2 Anchor;
            internal Color Color;
        }

        private static readonly BoundedCandidates<Candidate> candidates = new BoundedCandidates<Candidate>(
            AnnotationRules.MaximumMarkers, delegate(Candidate a, Candidate b) {
                return AnnotationRules.Compare(a.Colonist, a.Thing.thingIDNumber, b.Colonist, b.Thing.thingIDNumber);
            });
        private static readonly Marker[] markers = new Marker[AnnotationRules.MaximumMarkers];
        private static readonly LabelLayout layout = new LabelLayout();
        private static Map cachedMap;
        private static float nextRefresh;
        private static GUIStyle labelStyle;
        private static int styleSize;
        private static readonly Color ColonistColor = new Color(0.4f, 0.9f, 1f);
        private static readonly Color MedicineColor = new Color(0.5f, 1f, 0.55f);
        private static readonly Color WoodColor = new Color(1f, 0.8f, 0.35f);

        internal static void Invalidate()
        { candidates.Clear(); Array.Clear(markers, 0, markers.Length); cachedMap = null; nextRefresh = 0; }

        public static void Postfix()
        {
            ReadabilityMod.HandleAssemblyReplacement(); // Before Ready: a replacement may have made Ready false.
            if (!ReadabilityMod.Ready || ReadabilityMod.Settings == null || !ReadabilityMod.Settings.Enabled
                || Event.current == null || Event.current.type != EventType.Repaint) return;
            Color oldColor = GUI.color;
            try { Render(); }
            catch (Exception error)
            { ReadabilityMod.Disable("Çizim hatası: " + error.GetType().Name + ": " + error.Message); }
            finally { GUI.color = oldColor; }
        }

        private static void Render()
        {
            if (Current.ProgramState != ProgramState.Playing || !WorldRendererUtility.DrawingMap)
            { Invalidate(); return; }
            Map map = Find.CurrentMap;
            if (map == null || Find.CameraDriver == null) { Invalidate(); return; }
            LabelRect bounds = new LabelRect(8, 8, UI.screenWidth - 16, UI.screenHeight - 16);
            if (!bounds.IsFinite) return;
            CellRect view = Find.CameraDriver.CurrentViewRect;
            if (map != cachedMap || Time.realtimeSinceStartup >= nextRefresh) Refresh(map, view, bounds);
            ReadabilitySettings settings = ReadabilityMod.Settings;
            if (labelStyle == null || styleSize != settings.FontSize)
            {
                styleSize = settings.FontSize;
                labelStyle = new GUIStyle(Text.CurFontStyle) {
                    fontSize = styleSize, alignment = TextAnchor.MiddleCenter, wordWrap = false,
                    clipping = TextClipping.Clip, richText = false
                };
                labelStyle.normal.textColor = Color.white;
            }
            layout.Reset(bounds);
            int markerCount = 0;
            for (int i = 0; i < candidates.Count && markerCount < AnnotationRules.MaximumMarkers; i++)
            {
                Candidate candidate = candidates[i];
                Thing thing = candidate.Thing;
                if (candidate.Colonist && !settings.Colonists) continue;
                Vector2 anchor;
                LabelRect marker;
                if (!TryMarker(map, view, bounds, thing, out anchor, out marker)) continue;
                Color color = candidate.Colonist ? ColonistColor : settings.Filter == ItemFilter.Medicine ? MedicineColor : WoodColor;
                markers[markerCount++] = new Marker { Candidate = candidate, Anchor = anchor, Color = color };
                layout.AddObstacle(marker);
                GUI.color = color;
                GUI.DrawTexture(new Rect(marker.X, marker.Y, marker.Width, marker.Height), Texture2D.whiteTexture);
            }
            // All marker obstacles are known before any label is placed. No text work after the label cap.
            for (int i = 0; i < markerCount && layout.Count < LabelLayout.MaximumLabels; i++)
            {
                Marker marker = markers[i];
                Candidate candidate = marker.Candidate;
                Thing thing = candidate.Thing;
                string text = candidate.Colonist ? ((Pawn)thing).LabelShort :
                    (settings.Filter == ItemFilter.Medicine ? "İlaç ×" : "Odun ×") + thing.stackCount;
                if (text.Length > 48) text = text.Substring(0, 47) + "…";
                Vector2 measured = labelStyle.CalcSize(new GUIContent(text));
                LabelRect placed;
                if (!layout.TryPlace(marker.Anchor.x, marker.Anchor.y, measured.x + 12, measured.y + 6, out placed)) continue;
                Rect rect = new Rect(placed.X, placed.Y, placed.Width, placed.Height);
                GUI.color = new Color(0.04f, 0.04f, 0.04f, 0.9f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = marker.Color;
                GUI.Label(rect, text, labelStyle);
            }
        }

        private static bool Visible(Map map, CellRect view, Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != map) return false;
            IntVec3 cell = thing.Position;
            return AnnotationRules.Visible(thing.Destroyed, thing.Spawned, thing.Map == map,
                view.Contains(cell), map.fogGrid.IsFogged(cell));
        }

        private static bool TryMarker(Map map, CellRect view, LabelRect bounds, Thing thing,
            out Vector2 anchor, out LabelRect marker)
        {
            anchor = default(Vector2);
            marker = default(LabelRect);
            if (!Visible(map, view, thing)) return false;
            anchor = GenMapUI.LabelDrawPosFor(thing, 0f);
            return AnnotationRules.TryMarker(anchor.x, anchor.y, bounds, out marker);
        }

        private static void Refresh(Map map, CellRect view, LabelRect bounds)
        {
            candidates.Clear();
            cachedMap = map;
            nextRefresh = Time.realtimeSinceStartup + 0.25f;
            ReadabilitySettings settings = ReadabilityMod.Settings;
            Vector2 anchor;
            LabelRect marker;
            if (settings.Colonists)
            {
                List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                    if (TryMarker(map, view, bounds, pawns[i], out anchor, out marker))
                        candidates.Add(new Candidate(pawns[i], true));
            }
            List<Thing> items = settings.Filter == ItemFilter.Medicine ?
                map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine) : settings.Filter == ItemFilter.Wood ?
                map.listerThings.ThingsOfDef(ThingDefOf.WoodLog) : null;
            if (items != null)
                for (int i = 0; i < items.Count; i++)
                    if (TryMarker(map, view, bounds, items[i], out anchor, out marker))
                        candidates.Add(new Candidate(items[i], false));
        }
    }
}
