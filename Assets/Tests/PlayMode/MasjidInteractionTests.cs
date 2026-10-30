using System;
using System.Collections;
using System.Reflection;
using DeenCraft.Player;
using DeenCraft.UI;
using DeenCraft.Villager;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeenCraft.Tests.PlayMode
{
    public class MasjidInteractionTests
    {
        [UnityTest]
        public IEnumerator Reading_OpensFromExplorerAndRestoresGameplayWithoutStackingTrade()
        {
            Type explorerType = typeof(VillageSpawner).Assembly.GetType("DeenCraft.Villager.MasjidExplorer");
            Type panelType = typeof(VillagerTradePanel).Assembly.GetType("DeenCraft.UI.MasjidInformationPanel");
            Assert.That(explorerType, Is.Not.Null, "Teachers and boards need an interaction.");
            Assert.That(panelType, Is.Not.Null, "An exploration reading panel is required.");
            var root = new GameObject("Masjid UI test");
            try
            {
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                player.SetActive(false);
                var body = new GameObject("PlayerBody");
                body.transform.SetParent(player.transform);
                player.AddComponent<CharacterController>();
                PlayerController controller = player.AddComponent<PlayerController>();
                controller.enabled = false;
                var camera = new GameObject("Test camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform);
                typeof(PlayerController).GetField("_cameraTarget", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, camera.transform);
                player.AddComponent<InventoryHolder>();
                player.SetActive(true);
                var ui = new GameObject("UI", typeof(Canvas));
                ui.transform.SetParent(root.transform);
                Component panel = ui.AddComponent(panelType);
                panelType.GetField("_font", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(panel, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var trade = ui.AddComponent<VillagerTradePanel>();
                typeof(VillagerTradePanel).GetField("_font", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(trade, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var teacher = new GameObject("Teacher");
                teacher.transform.SetParent(root.transform);
                var explorer = (IInteractable)teacher.AddComponent(explorerType);
                yield return null;
                explorer.Interact(player);
                Assert.That((bool)panelType.GetProperty("IsOpen").GetValue(panel), Is.True);
                Assert.That(controller.InputBlocked, Is.True);
                Assert.That(ui.GetComponentInChildren<ScrollRect>(), Is.Not.Null);
                var villager = teacher.AddComponent<VillagerAI>();
                villager.SetProfile(VillagerProfile.CreateDefault());
                villager.enabled = false;
                trade.Open(villager, player);
                Assert.That(trade.IsOpen, Is.False, "Trading cannot steal the reading panel's input ownership.");
                Assert.That(ui.GetComponentsInChildren<Text>(), Has.None.Matches<Text>(t => t.text.Contains("Join")));
                panelType.GetMethod("Close").Invoke(panel, null);
                Assert.That(controller.InputBlocked, Is.False);
                Assert.That(controller.CanProcessGameplayInput, Is.False, "Closing frame must not mine blocks.");
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }
    }
}
