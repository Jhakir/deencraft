// Assets/Scripts/IInteractable.cs
// Placed in DeenCraft.Core (Assets/Scripts/ root — no sub-folder asmdef).
// Any MonoBehaviour the player can interact with by pressing E should implement this.
using UnityEngine;

namespace DeenCraft
{
    /// <summary>
    /// Implemented by any in-world object that responds to player interaction (E key / raycast).
    /// Kept in Core so Player, Animals, and Villager assemblies can all reference it
    /// without creating circular dependencies.
    /// </summary>
    public interface IInteractable
    {
        /// <param name="interactor">The GameObject of the interacting player.</param>
        void Interact(GameObject interactor);
    }
}
