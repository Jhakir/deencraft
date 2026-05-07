using NUnit.Framework;
using UnityEngine;
using DeenCraft.UI;

namespace DeenCraft.Tests.EditMode.UI
{
    public class SettingsModelTests
    {
        private const string KeyMusicVol     = "dc_music_vol";
        private const string KeySfxVol       = "dc_sfx_vol";
        private const string KeyGraphicsQual = "dc_graphics_quality";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(KeyMusicVol);
            PlayerPrefs.DeleteKey(KeySfxVol);
            PlayerPrefs.DeleteKey(KeyGraphicsQual);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(KeyMusicVol);
            PlayerPrefs.DeleteKey(KeySfxVol);
            PlayerPrefs.DeleteKey(KeyGraphicsQual);
        }

        [Test]
        public void Defaults_AreReasonable()
        {
            var model = new SettingsModel();
            Assert.AreEqual(0.8f, model.MusicVolume,  0.001f);
            Assert.AreEqual(1.0f, model.SfxVolume,    0.001f);
            Assert.AreEqual(1,    model.GraphicsQuality);
        }

        [Test]
        public void Save_ThenLoad_RestoresMusicVolume()
        {
            var model = new SettingsModel();
            model.MusicVolume = 0.4f;
            model.Save();

            var loaded = new SettingsModel();
            loaded.Load();
            Assert.AreEqual(0.4f, loaded.MusicVolume, 0.001f);
        }

        [Test]
        public void Save_ThenLoad_RestoresSfxVolume()
        {
            var model = new SettingsModel();
            model.SfxVolume = 0.6f;
            model.Save();

            var loaded = new SettingsModel();
            loaded.Load();
            Assert.AreEqual(0.6f, loaded.SfxVolume, 0.001f);
        }

        [Test]
        public void Save_ThenLoad_RestoresGraphicsQuality()
        {
            var model = new SettingsModel();
            model.GraphicsQuality = 2;
            model.Save();

            var loaded = new SettingsModel();
            loaded.Load();
            Assert.AreEqual(2, loaded.GraphicsQuality);
        }

        [Test]
        public void MusicVolume_ClampedToZero_WhenNegative()
        {
            var model = new SettingsModel();
            model.MusicVolume = -5f;
            Assert.AreEqual(0f, model.MusicVolume, 0.001f);
        }

        [Test]
        public void MusicVolume_ClampedToOne_WhenAboveMax()
        {
            var model = new SettingsModel();
            model.MusicVolume = 99f;
            Assert.AreEqual(1f, model.MusicVolume, 0.001f);
        }
    }
}
