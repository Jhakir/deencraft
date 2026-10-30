using System;
using System.Text.RegularExpressions;
using DeenCraft.Player;
using DeenCraft.Villager;
using UnityEngine;
using UnityEngine.UI;

namespace DeenCraft.UI
{
    public sealed class VillagerTradePanel : MonoBehaviour
    {
        private const float PanelWidth = 720f;
        private const float PanelHeight = 760f;
        private const float RowHeight = 43f;
        private const float ReachTolerance = 1f;
        [SerializeField] private Font _font;
        private GameObject _panel;
        private Text _title;
        private Text _status;
        private Button[] _buttons;
        private Text[] _rows;
        private VillagerAI _villager;
        private Inventory _inventory;
        private PlayerController _player;
        private bool _previousInputBlocked;
        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void OnEnable() => VillagerAI.TradeRequested += Open;
        private void OnDisable()
        {
            VillagerAI.TradeRequested -= Open;
            Close();
        }

        public void Open(VillagerAI villager, GameObject player)
        {
            if (villager == null || player == null || villager.Profile == null)
                throw new ArgumentException("Trading needs a villager profile and player.");
            var holder = player.GetComponent<InventoryHolder>();
            var controller = player.GetComponent<PlayerController>();
            if (holder == null || controller == null)
            {
                Debug.LogError("[VillagerTradePanel] Player inventory and controller are required.", this);
                return;
            }
            if (Vector3.Distance(player.transform.position, villager.transform.position) > GameConstants.PlayerReach + ReachTolerance)
            {
                Debug.LogWarning("[VillagerTradePanel] Move closer to the villager to trade.", this);
                return;
            }
            if (_font == null)
            {
                Debug.LogError("[VillagerTradePanel] The trade menu font is not assigned.", this);
                return;
            }
            if (controller.InputBlocked && (!IsOpen || _player != controller))
            {
                Debug.LogWarning("[VillagerTradePanel] Close the current menu before trading.", this);
                return;
            }
            Close();
            _villager = villager;
            _inventory = holder.Inventory;
            _player = controller;
            _previousInputBlocked = _player.InputBlocked;
            _player.InputBlocked = true;
            if (_panel == null) Build();
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _title.text = "Trade with " + villager.Profile.Name;
            _status.text = "Collect wood, wheat or wool to earn gold. Escape closes this menu.";
            RefreshRows();
        }

        public bool TryTrade(int index)
        {
            if (!IsOpen || _villager == null || index < 0 || index >= _villager.Profile.Trades.Length)
            {
                Debug.LogWarning("[VillagerTradePanel] No active offer at that index.", this);
                return false;
            }
            TradeOffer offer = _villager.Profile.Trades[index];
            bool success = _villager.ExecuteTrade(index, _inventory);
            _status.text = success
                ? $"Received {offer.OfferedCount} {ItemName(offer.OfferedItem)}."
                : _inventory.CountItem(offer.RequestedItem) < offer.RequestedCount
                    ? $"You need {offer.RequestedCount} {ItemName(offer.RequestedItem)} for this trade."
                    : "Your inventory is full. Make room before trading.";
            RefreshRows();
            return success;
        }

        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_player != null) _player.InputBlocked = _previousInputBlocked;
            _player = null;
            _villager = null;
            _inventory = null;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_villager == null || !_villager.gameObject.activeInHierarchy || _player == null
                || Vector3.Distance(_player.transform.position, _villager.transform.position) > GameConstants.PlayerReach + ReachTolerance
                || Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private void RefreshRows()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool visible = i < _villager.Profile.Trades.Length;
                _buttons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                TradeOffer offer = _villager.Profile.Trades[i];
                _rows[i].text = $"{offer.RequestedCount} {ItemName(offer.RequestedItem)}  ->  "
                    + $"{offer.OfferedCount} {ItemName(offer.OfferedItem)}"
                    + $"    (You have {_inventory.CountItem(offer.RequestedItem)})";
                _buttons[i].interactable = TradeSession.CanExecute(offer, _inventory);
            }
        }

        private static string ItemName(ItemId item) => Regex.Replace(item.ToString(), "(\\B[A-Z])", " $1");

        private void Build()
        {
            _panel = new GameObject("Villager Trade Menu", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0.08f, 0.14f, 0.12f, 0.98f);
            _title = Label("Title", _panel.transform, new Vector2(0f, 330f), new Vector2(660f, 50f), 30);
            _status = Label("Trade status", _panel.transform, new Vector2(0f, -300f), new Vector2(660f, 65f), 20);
            _buttons = new Button[GameConstants.MaxTradesPerVillager];
            _rows = new Text[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i;
                _buttons[i] = MakeButton("Offer " + (i + 1), new Vector2(0f, 270f - i * RowHeight),
                    new Vector2(660f, RowHeight - 4f), out _rows[i]);
                _buttons[i].onClick.AddListener(() => TryTrade(index));
            }
            var close = MakeButton("Close", new Vector2(0f, -350f), new Vector2(240f, 40f), out Text closeText);
            closeText.text = "Close";
            close.onClick.AddListener(Close);
        }

        private Button MakeButton(string name, Vector2 position, Vector2 size, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.23f, 0.43f, 0.32f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            label = Label("Label", go.transform, Vector2.zero, size, 20);
            return button;
        }

        private Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
