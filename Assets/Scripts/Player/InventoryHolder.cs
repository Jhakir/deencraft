// Assets/Scripts/Player/InventoryHolder.cs
using UnityEngine;

namespace DeenCraft.Player
{
    /// <summary>
    /// Holds the player's Inventory instance on the scene GameObject.
    /// Other MonoBehaviours (CraftingSystem, VitalitySystem) fetch this via GetComponent.
    /// </summary>
    public class InventoryHolder : MonoBehaviour
    {
        public Inventory Inventory { get; } = new Inventory();
    }
}
