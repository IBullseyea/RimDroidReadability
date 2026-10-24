using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal sealed class GuiState : IDisposable
    {
        private readonly Color color = GUI.color, background = GUI.backgroundColor, content = GUI.contentColor;
        private readonly bool enabled = GUI.enabled, wordWrap = Text.WordWrap;
        private readonly GameFont font = Text.Font;
        private readonly TextAnchor anchor = Text.Anchor;
        public void Dispose()
        { GUI.color = color; GUI.backgroundColor = background; GUI.contentColor = content; GUI.enabled = enabled;
            Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wordWrap; }
    }

    internal sealed class FinderButtonWindow : Window
    {
        internal FinderButtonWindow()
        {
            layer = WindowLayer.GameUI; doWindowBackground = false; drawShadow = false;
            closeOnAccept = false; closeOnCancel = false; preventCameraMotion = false;
            absorbInputAroundWindow = false; draggable = false;
        }
        protected override float Margin { get { return 0; } }
        public override Vector2 InitialSize { get { LabelRect b = OverlayRenderer.ControlBounds; return new Vector2(b.Width, b.Height); } }
        protected override void SetInitialSizeAndPosition() { UpdatePosition(); }
        internal void UpdatePosition()
        { LabelRect b = OverlayRenderer.ControlBounds; windowRect = new Rect(b.X, b.Y, b.Width, b.Height); }
        public override void WindowOnGUI()
        {
            using (new GuiState())
            {
                try
                {
                    if (!FinderController.ValidWindow || FinderController.HasForeground) return;
                    UpdatePosition(); base.WindowOnGUI();
                }
                catch (Exception error) { ReadabilityMod.Disable("Bul kontrolü pencere hatası: " + error.Message, error); }
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
                    string count = OverlayRenderer.OmittedGroups > 0 ? " (+" + OverlayRenderer.OmittedGroups + ")" : "";
                    if (Widgets.ButtonText(inRect, "Bul" + count)) FinderController.Open();
                    TooltipHandler.TipRegion(inRect, "Kolonist ve eşya bulucu. + sayı, etiketi gösterilemeyen görsel grup sayısıdır; yığın sayısı değildir.");
                }
                catch (Exception error) { ReadabilityMod.Disable("Bul kontrolü hatası: " + error.Message, error); }
            }
        }
    }

    internal sealed class FinderWindow : Window
    {
        private readonly FinderState state = new FinderState();
        private Vector2 scroll;
        private int screenWidth, screenHeight;
        internal FinderWindow()
        {
            layer = WindowLayer.Dialog; absorbInputAroundWindow = true; preventCameraMotion = true;
            closeOnAccept = false; closeOnCancel = true; doCloseX = false; doCloseButton = false;
            draggable = false; resizeable = false;
        }
        public override Vector2 InitialSize
        { get { LabelRect b = FinderGeometry.Window(UI.screenWidth, UI.screenHeight); return new Vector2(b.Width, b.Height); } }

        public override void WindowOnGUI()
        {
            using (new GuiState())
            {
                try
                {
                    if (!FinderController.ValidWindow) { Close(false); return; }
                    if (screenWidth != UI.screenWidth || screenHeight != UI.screenHeight)
                    {
                        screenWidth = UI.screenWidth; screenHeight = UI.screenHeight;
                        Vector2 size = InitialSize;
                        windowRect = new Rect((screenWidth - size.x) / 2, (screenHeight - size.y) / 2, size.x, size.y);
                        scroll = Vector2.zero;
                    }
                    base.WindowOnGUI();
                }
                catch (Exception error) { ReadabilityMod.Disable("Bulucu pencere hatası: " + error.GetType().Name + ": " + error.Message, error); }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (new GuiState())
            {
                try { Draw(inRect); }
                catch (Exception error) { ReadabilityMod.Disable("Bulucu hatası: " + error.GetType().Name + ": " + error.Message, error); }
            }
        }

        private void Draw(Rect inRect)
        {
            if (!FinderController.ValidWindow) { Close(false); return; }
            Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
            if (!FinderGeometry.FitsActions(inRect.width))
            { Widgets.Label(inRect, "Arayüz alanı yetersiz. UI ölçeğini küçültün veya ekranı yatay kullanın."); return; }
            // Whole content scrolls, including controls: small screens never shrink action targets.
            int pageSize = FinderGeometry.Rows(inRect.height - (state.TypeKey != null ? 56 : 0), state.Items);
            float width = Math.Max(320, inRect.width - 20);
            float header = state.Items ? 256 + (state.TypeKey != null ? 56 : 0) : 144;
            List<TargetData> snapshot = TargetCatalog.Snapshot(false);
            List<FinderRow> rows = FinderRows.Build(snapshot, state);
            List<FinderRow> pageRows = PageRules.Slice(rows, ref state.Page, pageSize);
            float rowsHeight = 0;
            foreach (FinderRow row in pageRows) rowsHeight += RowHeight(row, width);
            float height = Math.Max(inRect.height, header + Math.Max(64, rowsHeight) + 64);
            bool scrolling = false;
            try
            {
                Widgets.BeginScrollView(inRect, ref scroll, new Rect(0, 0, width, height)); scrolling = true;
                float y = 0;
                Widgets.Label(new Rect(0, y, width - 104, 48), "Hedef bulucu");
                if (Widgets.ButtonText(new Rect(width - 96, y, 96, 48), "Kapat")) { Close(); return; }
                y += 56;
                float half = (width - 8) / 2;
                if (Widgets.ButtonText(new Rect(0, y, half, 48), state.Items ? "Kolonistler" : "• Kolonistler")) { state.SetTab(false); scroll = Vector2.zero; return; }
                if (Widgets.ButtonText(new Rect(half + 8, y, half, 48), state.Items ? "• Eşyalar" : "Eşyalar")) { state.SetTab(true); scroll = Vector2.zero; return; }
                y += 56;
                if (state.Items)
                {
                    if (Widgets.ButtonText(new Rect(0, y, width, 48), "Kategori: " + SettingsRules.FilterName(ReadabilityMod.Settings.Filter)))
                    {
                        ReadabilityMod.Settings.Filter = SettingsRules.NextFilter(ReadabilityMod.Settings.Filter);
                        ReadabilityMod.Settings.Write(); TargetCatalog.Clear(); state.ResetNavigation(); return;
                    }
                    y += 56;
                    string search = Widgets.TextField(new Rect(0, y, width - 104, 48), TargetCatalog.Search);
                    if (search != TargetCatalog.Search) { TargetCatalog.Search = search; TargetCatalog.Clear(); state.ResetNavigation(); return; }
                    if (Widgets.ButtonText(new Rect(width - 96, y, 96, 48), "Temizle"))
                    { TargetCatalog.Search = ""; TargetCatalog.Clear(); state.ResetNavigation(); GUI.FocusControl(null); return; }
                    y += 56;
                }
                if (state.Items && state.TypeKey != null)
                {
                    if (Widgets.ButtonText(new Rect(0, y, 96, 48), "Geri")) { state.ResetNavigation(); return; }
                    Widgets.Label(new Rect(104, y, width - 104, 48), rows.Count == 0 ? "Yığın kalmadı. Geri ile türlere dönün." : "Türün tüm uygun yığınları");
                    y += 56;
                }
                if (rows.Count == 0)
                    Widgets.Label(new Rect(0, y, width, 56), state.Items ? "Uygun eşya bulunamadı. Kategoriyi veya aramayı değiştirin." : "Bu haritada uygun serbest kolonist yok.");
                foreach (FinderRow row in pageRows)
                {
                    float rowHeight = RowHeight(row, width);
                    Rect name = new Rect(0, y, width - 104, rowHeight - 8);
                    Widgets.Label(name, row.Name + "\n" + row.Detail);
                    TooltipHandler.TipRegion(name, row.Name + "\n" + row.Detail);
                    if (Widgets.ButtonText(new Rect(width - 96, y, 96, 48), row.Group ? "Aç" : "Git"))
                    {
                        if (row.Group) { state.OpenGroup(row.TypeKey); scroll = Vector2.zero; }
                        else FinderController.Go(row.Id);
                        return;
                    }
                    y += rowHeight;
                }
                y = Math.Max(y + 8, header + rowsHeight);
                int pages = PageRules.Count(rows.Count, pageSize);
                GUI.enabled = state.Page > 0;
                if (Widgets.ButtonText(new Rect(0, y, 96, 48), "Önceki")) { state.Page--; scroll = Vector2.zero; }
                GUI.enabled = true;
                Widgets.Label(new Rect(104, y, width - 208, 48), (state.Page + 1) + " / " + pages + "\n" + rows.Count + " sonuç");
                GUI.enabled = state.Page + 1 < pages;
                if (Widgets.ButtonText(new Rect(width - 96, y, 96, 48), "Sonraki")) { state.Page++; scroll = Vector2.zero; }
            }
            finally { if (scrolling) Widgets.EndScrollView(); }
        }

        private static float RowHeight(FinderRow row, float width)
        { return Math.Max(FinderGeometry.RowHeight, Text.CalcHeight(row.Name + "\n" + row.Detail, width - 104) + 8); }
    }
}
