using NUnit.Framework;
using UnityEngine;
using DeenCraft.Player;
using DeenCraft.Auth.Models;

namespace DeenCraft.Tests.EditMode.PlayerTests
{
    public class CharacterDataBridgeTests
    {
        [Test]
        public void ToAppearance_DefaultData_ProducesMediumSkinTone()
        {
            var data = new CharacterData(); // skinToneIndex=2 by default
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(SkinTone.Medium, appearance.SkinTone);
        }

        [Test]
        public void ToAppearance_HijabType_MapsToHijabHeadwear()
        {
            var data = new CharacterData { headCoveringType = 1 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.Hijab, appearance.HeadwearType);
        }

        [Test]
        public void ToAppearance_KufiType_MapsToKufi()
        {
            var data = new CharacterData { headCoveringType = 2 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.Kufi, appearance.HeadwearType);
        }

        [Test]
        public void ToAppearance_UnknownHeadType_MapsToNone()
        {
            var data = new CharacterData { headCoveringType = 99 };
            var appearance = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(HeadwearType.None, appearance.HeadwearType);
        }

        [Test]
        public void ToData_DefaultAppearance_ProducesValidCharacterData()
        {
            var appearance = new CharacterAppearance();
            var data = CharacterDataBridge.ToData(appearance);
            Assert.IsTrue(data.IsValid());
        }

        [Test]
        public void ToData_DarkSkinTone_SetsIndex4()
        {
            var appearance = new CharacterAppearance { SkinTone = SkinTone.Dark };
            var data = CharacterDataBridge.ToData(appearance);
            Assert.AreEqual(4, data.skinToneIndex);
        }

        [Test]
        public void RoundTrip_AppearanceToDataAndBack_PreservesSkinTone()
        {
            var original  = new CharacterAppearance { SkinTone = SkinTone.MediumDark };
            var data      = CharacterDataBridge.ToData(original);
            var restored  = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(original.SkinTone, restored.SkinTone);
        }

        [Test]
        public void RoundTrip_PreservesHeadwearType()
        {
            var original = new CharacterAppearance { HeadwearType = HeadwearType.Hijab };
            var data     = CharacterDataBridge.ToData(original);
            var restored = CharacterDataBridge.ToAppearance(data);
            Assert.AreEqual(original.HeadwearType, restored.HeadwearType);
        }
    }
}
