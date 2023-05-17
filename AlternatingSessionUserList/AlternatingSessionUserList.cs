using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BaseX;
using CodeX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Data;
using FrooxEngine.LogiX.ProgramFlow;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;

namespace AlternatingSessionUserList
{
    public class AlternatingSessionUserList : NeosMod
    {
        public static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<color> FirstRowColor = new("FirstRowColor", "Background color of the first row in the Session user lists.", () => new color(0, .85f));

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<color> SecondRowColor = new("SecondRowColor", "Background color of the second row in the Session user lists.", () => new color(1, .15f));

        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosAlternatingSessionUserList";
        public override string Name => "AlternatingSessionUserList";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Save(true);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SessionControlDialog))]
        private static class SessionControlDialogPatches
        {
            private static IEnumerable<Slot> GetUserRows(SessionControlDialog.Tab tab, Slot uiRoot)
            {
                return tab switch
                {
                    SessionControlDialog.Tab.Users => uiRoot.GetComponentsInChildren<SessionUserController>().Select(controller => controller.Slot),
                    SessionControlDialog.Tab.Permissions => uiRoot.GetComponentsInChildren<SessionPermissionController>().Select(controller => controller.Slot),
                    _ => Array.Empty<Slot>(),
                };
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnCommonUpdate")]
            private static void OnCommonUpdatePostfix(Sync<SessionControlDialog.Tab> ___ActiveTab, SyncRef<Slot> ____uiContentRoot)
            {
                var first = true;

                foreach (var userRow in GetUserRows(___ActiveTab, ____uiContentRoot))
                {
                    if (userRow.GetComponent<LayoutElement>() is LayoutElement layout && layout.MinHeight == SessionPermissionController.HEIGHT)
                    {
                        var childOffset = new float2(0, 4);
                        layout.MinHeight.Value = SessionPermissionController.HEIGHT + 8;

                        var children = userRow.Children.Select(child => child.GetComponent<RectTransform>()).Where(rect => rect != null).ToArray();
                        children[0].OffsetMin.Value += childOffset.yx;
                        children[^1].OffsetMax.Value -= childOffset.yx;

                        foreach (var childRect in children)
                        {
                            childRect.OffsetMax.Value -= childOffset;
                            childRect.OffsetMin.Value += childOffset;
                        }
                    }

                    var image = userRow.AttachComponent<Image>();
                    image.Tint.Value = Config.GetValue(first ? FirstRowColor : SecondRowColor);

                    first = !first;
                }
            }
        }
    }
}