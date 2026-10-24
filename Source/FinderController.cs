using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimDroid.Readability
{
    internal static class FinderController
    {
        private static FinderButtonWindow button;
        private static FinderWindow finder;
        private static readonly SessionScope scope = new SessionScope();
        internal static bool CanDraw
        { get { return ReadabilityMod.Ready && ReadabilityMod.Settings != null && ReadabilityMod.Settings.Enabled && TargetCatalog.InMap; } }
        internal static bool ValidWindow { get { return CanDraw && scope.Matches(Find.CurrentMap, Current.Game); } }
        internal static bool HasForeground
        {
            get
            {
                if (Find.WindowStack == null) return true;
                foreach (Window window in Find.WindowStack.Windows)
                    if (window != button && window.layer >= WindowLayer.Dialog) return true;
                return false;
            }
        }

        public static void Postfix()
        {
            ReadabilityMod.HandleAssemblyReplacement();
            try
            {
                if (!CanDraw) { Reset(); return; }
                if (!scope.Matches(Find.CurrentMap, Current.Game))
                { Reset(); scope.Bind(Find.CurrentMap, Current.Game); }
                if (finder != null && !finder.IsOpen) finder = null;
                if (HasForeground)
                { Remove(button); button = null; return; }
                if (button == null || !button.IsOpen)
                { button = new FinderButtonWindow(); Find.WindowStack.Add(button); }
                button.UpdatePosition();
            }
            catch (Exception error) { ReadabilityMod.Disable("Harita arayüzü hatası: " + error.GetType().Name + ": " + error.Message); }
        }

        internal static void Open()
        {
            if (!ValidWindow || HasForeground) return;
            if (finder == null || !finder.IsOpen) { finder = new FinderWindow(); Find.WindowStack.Add(finder); }
        }

        internal static void Reset()
        {
            FinderButtonWindow oldButton = button; FinderWindow oldFinder = finder;
            button = null; finder = null; scope.Clear();
            try { Remove(oldButton); }
            finally
            {
                try { Remove(oldFinder); }
                finally { TargetCatalog.Search = ""; OverlayRenderer.Invalidate(); }
            }
        }

        private static void Remove(Window window)
        { if (window != null && Find.WindowStack != null) Find.WindowStack.TryRemove(window, false); }

        internal static void Go(int id)
        {
            if (!ValidWindow) return;
            Thing target = TargetCatalog.Resolve(id);
            if (target == null)
            {
                TargetCatalog.Clear();
                Messages.Message("Hedef artık bu haritada uygun değil. Liste yenilendi.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            float size = Find.CameraDriver.RootSize;
            // Capture before invoking the game API so rejected/stale targets never move the camera.
            Vector3 oldPosition = (Vector3)ApiContract.CameraPosition.GetValue(Find.CameraDriver, null);
            List<object> oldSelection = new List<object>(Find.Selector.SelectedObjectsListForReading);
            try
            {
                CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(target), CameraJumper.MovementMode.Cut);
                Find.CameraDriver.SetRootSize(size);
                if (!Find.Selector.IsSelected(target)) throw new InvalidOperationException("Hedef seçilemedi");
                Remove(finder); finder = null;
            }
            catch
            {
                Find.CameraDriver.JumpToCurrentMapLoc(oldPosition); Find.CameraDriver.SetRootSize(size);
                Find.Selector.ClearSelection();
                foreach (object selected in oldSelection) Find.Selector.Select(selected, false, false);
                throw;
            }
        }
    }
}
