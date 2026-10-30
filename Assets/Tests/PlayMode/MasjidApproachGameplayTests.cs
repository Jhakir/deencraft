using System.Collections;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeenCraft.Tests.PlayMode
{
    public class MasjidApproachGameplayTests
    {
        [UnityTest]
        public IEnumerator DiscoverySign_StaysAwayFromInitialThirdPersonCamera()
        {
            var root = new GameObject("Approach sign test");
            try
            {
                var worldObject = new GameObject("World");
                worldObject.transform.SetParent(root.transform);
                worldObject.SetActive(false);
                ChunkManager world = worldObject.AddComponent<ChunkManager>();
                Set(world, "_poolSize", 1);
                Set(world, "_viewDistance", 0);
                world.enabled = false;
                worldObject.SetActive(true);
                var pathObject = new GameObject("Path");
                pathObject.transform.SetParent(root.transform);
                var path = pathObject.AddComponent<MasjidApproachPath>();
                Set(path, "_chunkManager", world);
                yield return null;
                TextMesh label = path.GetComponentInChildren<TextMesh>(true);
                Assert.That(label, Is.Not.Null);
                Vector3 initialCamera = new Vector3(0f, world.DistrictLayout.ApproachStart.y + 1.5f, -5f);
                Assert.That(Vector3.Distance(label.transform.position, initialCamera), Is.GreaterThan(4f),
                    "The initial camera must not sit inside the billboard, covering the whole game.");
                Assert.That(label.transform.localScale.x, Is.LessThanOrEqualTo(0.05f));
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
