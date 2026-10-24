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
                catch (Exception error) { ReadabilityMod.Disable("Eşya kısayolu pencere hatası: " + error.Message, error); }
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
                    if (Widgets.ButtonText(inRect, "Eşya: " + SettingsRules.FilterName(ReadabilityMod.Settings.Filter)))
                        ReadabilityMod.CycleItemFilter();
                    TooltipHandler.TipRegion(inRect, "Sonraki eşya filtresine geç. Kapalı → Tümü → İlaç → Odun → Silah → Yiyecek → Kaynak → Giysi. Bulucudaki arama da uygulanır.");
                }
                catch (Exception error) { ReadabilityMod.Disable("Eşya kısayolu hatası: " + error.Message, error); }
            }
        }
    }
}
