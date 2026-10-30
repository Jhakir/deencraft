using System;
using System.Reflection;
using DeenCraft.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DeenCraft.Tests.EditMode
{
    public class CloudLayerTests
    {
        private GameObject _root;
        private GameObject _player;
        private Material _material;
        private Component _layer;

        [SetUp]
        public void SetUp()
        {
            Type type = typeof(CloudLayer);
            _root = new GameObject("Cloud test");
            _root.SetActive(false);
            _player = new GameObject("Cloud follow target");
            _material = new Material(Shader.Find("DeenCraft/VertexColor"));
            _layer = _root.AddComponent(type);
            Set("_playerTransform", _player.transform);
            Set("_cloudMaterial", _material);
            Call("Start");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                var filter = _root.GetComponentInChildren<MeshFilter>(true);
                if (filter != null) Object.DestroyImmediate(filter.sharedMesh);
            }
            if (_root != null) Object.DestroyImmediate(_root);
            if (_player != null) Object.DestroyImmediate(_player);
            if (_material != null) Object.DestroyImmediate(_material);
        }

        [Test]
        public void Clouds_AreVisibleSharedGeometryAboveTerrainWithoutColliders()
        {
            MeshFilter[] filters = _root.GetComponentsInChildren<MeshFilter>(true);
            Assert.AreEqual(25, filters.Length);
            Assert.AreEqual(0, _root.GetComponentsInChildren<Collider>(true).Length);
            Mesh shared = filters[0].sharedMesh;
            Assert.Greater(shared.vertexCount, 0);
            foreach (var filter in filters)
            {
                Assert.AreSame(shared, filter.sharedMesh);
                Assert.Greater(filter.transform.position.y, GameConstants.MaxTerrainHeight);
                var renderer = filter.GetComponent<MeshRenderer>();
                Assert.AreSame(_material, renderer.sharedMaterial);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.AreSame(Texture2D.whiteTexture, properties.GetTexture("_MainTex"),
                    "Clouds must not inherit the terrain atlas's green tint.");
                Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode);
            }
        }

        [Test]
        public void Clouds_DriftAndWrapAroundPlayerWithoutAllocatingMoreObjects()
        {
            MeshFilter[] filters = _root.GetComponentsInChildren<MeshFilter>(true);
            Vector3 start = filters[0].transform.position;
            Call("Advance", 1f);
            Assert.Greater(filters[0].transform.position.x, start.x);
            _player.transform.position = new Vector3(-10000f, 200f, 10000f);
            Call("Advance", 0f);
            foreach (var filter in filters)
            {
                Vector3 delta = filter.transform.position - _player.transform.position;
                Assert.LessOrEqual(Mathf.Abs(delta.x), 200f);
                Assert.LessOrEqual(Mathf.Abs(delta.z), 200f);
                Assert.AreEqual(start.y, filter.transform.position.y);
            }
            Assert.AreEqual(filters.Length, _root.GetComponentsInChildren<MeshFilter>(true).Length);
        }

        private void Set(string name, object value) => _layer.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_layer, value);

        private void Call(string name, params object[] values) => _layer.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_layer, values);
    }
}
