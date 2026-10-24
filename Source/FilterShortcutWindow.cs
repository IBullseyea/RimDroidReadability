using System;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal sealed class FilterShortcutWindow : Window
    {
        internal FilterShortcutWindow()
        {
            layer = WindowLayer.GameUI; doWindowBackground = false; drawShadow = false;
            closeOnAccept = false; closeOnCancel = false; preventCameraMotion = false;
            absorbInputAroundWindow = false; draggable = false;
        }
        protected override float Margin { get { return 0; } }
        public override Vector2 InitialSize
        { get { return new Vector2(FilterShortcutGeometry.Width, FilterShortcutGeometry.Height); } }
        protected override void SetInitialSizeAndPosition() { UpdatePosition(); }
        internal void UpdatePosition()
        {
            LabelRect bounds = OverlayRenderer.FilterControlBounds;
            windowRect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        public override void WindowOnGUI()
        {
            using (new GuiState())
            {
                try
                {
                    if (!FinderController.ValidWindow || FinderController.HasForeground || !OverlayRenderer.FilterControlBounds.IsFinite) return;
                    UpdatePosition(); base.WindowOnGUI();
                }
                catch (Exception error) { ReadabilityMod.Disable(ModText.Error("ContextShortcut", error), error); }
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            using (new GuiState())
            {
                try
                {
                    if (!FinderController.ValidWindow || FinderController.HasForeground) return;
                    Text.Font = GameFont.Medium;
                    string caption = ModText.Get("NamedValue", ModText.Get("Items"), SettingsRules.FilterName(ReadabilityMod.Settings.Filter));
                    if (Text.CalcSize(caption).x > inRect.width - 12) Text.Font = GameFont.Small;
                    Text.WordWrap = true;
                    if (Widgets.ButtonText(inRect, caption))
                        ReadabilityMod.CycleItemFilter();
                    TooltipHandler.TipRegion(inRect, ModText.Get("ShortcutTip"));
                }
                catch (Exception error) { ReadabilityMod.Disable(ModText.Error("ContextShortcut", error), error); }
            }
        }
    }
}
