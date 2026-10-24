using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal static class OverlayRenderer
    {
        private static readonly LabelLayout layout = new LabelLayout();
        private static GUIStyle labelStyle;
        private static int styleSize;
        internal static int OmittedGroups { get; private set; }
        internal static LabelRect ControlBounds
        { get { return FinderGeometry.Control(UI.screenWidth, UI.screenHeight); } }

        internal static void Invalidate()
        {
            TargetCatalog.Clear(); layout.Reset(default(LabelRect)); OmittedGroups = 0;
        }

        public static void Postfix()
        {
            ReadabilityMod.HandleAssemblyReplacement();
            if (!ReadabilityMod.Ready || ReadabilityMod.Settings == null || !ReadabilityMod.Settings.Enabled
                || Event.current == null || Event.current.type != EventType.Repaint) return;
            Color oldColor = GUI.color;
            try { Render(); }
            catch (Exception error) { ReadabilityMod.Disable("Çizim hatası: " + error.GetType().Name + ": " + error.Message, error); }
            finally { GUI.color = oldColor; }
        }

        private static void Render()
        {
            if (!TargetCatalog.InMap) { Invalidate(); return; }
            ReadabilitySettings settings = ReadabilityMod.Settings;
            LabelRect bounds = new LabelRect(8, 8, UI.screenWidth - 16, UI.screenHeight - 16);
            if (!bounds.IsFinite) return;
            if (labelStyle == null || styleSize != settings.FontSize)
            {
                styleSize = settings.FontSize;
                labelStyle = new GUIStyle(Text.CurFontStyle) {
                    fontSize = styleSize, alignment = TextAnchor.MiddleCenter, wordWrap = false,
                    clipping = TextClipping.Clip, richText = false
                };
                labelStyle.normal.textColor = Color.white;
            }
            List<TargetData> targets = TargetCatalog.Snapshot(true);
            targets.RemoveAll(delegate(TargetData target) {
                LabelRect marker;
                return (target.Colonist && !settings.Colonists) || !target.OnScreen
                    || !AnnotationRules.TryMarker(target.X, target.Y, bounds, settings.MarkerSize, out marker);
            });
            List<TargetGroup> groups = GroupRules.Visual(targets, settings.GroupItems);
            List<PlannedMarker> markers = OverlayBudget.Select(groups, bounds, ControlBounds, settings.MarkerSize);
            layout.Reset(bounds); layout.SetControl(ControlBounds);
            foreach (PlannedMarker marker in markers)
            {
                layout.AddObstacle(marker.Rect);
                DrawMarker(marker.Rect, marker.Group.Colonist);
            }
            foreach (PlannedMarker marker in markers)
            {
                if (layout.Count == LabelLayout.MaximumLabels) break;
                string text = marker.Group.Summary;
                if (text.Length > 64) text = text.Substring(0, 63) + "…";
                Vector2 measured = labelStyle.CalcSize(new GUIContent(text));
                LabelRect placed;
                if (!layout.TryPlace(marker.Group.X, marker.Group.Y, measured.x + 12, measured.y + 6,
                    out placed, settings.MarkerSize)) continue;
                Rect rect = new Rect(placed.X, placed.Y, placed.Width, placed.Height);
                GUI.color = new Color(0.025f, 0.025f, 0.025f, 0.95f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(rect, text, labelStyle);
            }
            // Each group without a full text label counts once, including marker-only groups.
            OmittedGroups = OverlayBudget.Omitted(groups.Count, layout.Count);
        }

        private static void DrawMarker(LabelRect marker, bool colonist)
        {
            Rect rect = new Rect(marker.X, marker.Y, marker.Width, marker.Height);
            GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = colonist ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.85f, 0.4f);
            Rect inner = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
            if (!colonist) GUI.DrawTexture(inner, Texture2D.whiteTexture);
            else
            {
                // Cross versus solid square, also labelled K/E: color is not the sole distinction.
                float thickness = Math.Max(2, inner.width / 3);
                GUI.DrawTexture(new Rect(inner.center.x - thickness / 2, inner.y, thickness, inner.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(inner.x, inner.center.y - thickness / 2, inner.width, thickness), Texture2D.whiteTexture);
            }
        }
    }
}
