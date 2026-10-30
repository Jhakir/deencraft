using System;
using DeenCraft.Player;
using DeenCraft.World;
using UnityEngine;

namespace DeenCraft.Villager
{
    public sealed class MasjidExplorer : MonoBehaviour, IInteractable
    {
        public static event Action<MasjidExplorer, GameObject> ReadingRequested;
        public MasjidStyle Style { get; private set; }
        public MasjidTopic Topic { get; private set; }
        public string Title { get; private set; } = "Welcome to the masjid";
        public string Body => MasjidContentCatalog.Body(Style, Topic);

        public void Configure(MasjidStyle style, MasjidTopic topic, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A reading title is required.", nameof(title));
            Style = style;
            Topic = topic;
            Title = title;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null || interactor.GetComponent<PlayerController>() == null)
            {
                Debug.LogError("[MasjidExplorer] A player controller is required to read.", this);
                return;
            }
            if (!gameObject.activeInHierarchy ||
                Vector3.Distance(interactor.transform.position, transform.position) > GameConstants.PlayerReach)
            {
                Debug.LogWarning("[MasjidExplorer] Move closer to read.", this);
                return;
            }
            if (ReadingRequested == null)
            {
                Debug.LogError("[MasjidExplorer] The masjid reading panel is not available.", this);
                return;
            }
            ReadingRequested.Invoke(this, interactor);
        }
    }
}
