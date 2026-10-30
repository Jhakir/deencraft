using DeenCraft.Player;
using DeenCraft.Villager;
using DeenCraft.World;
using UnityEngine;
using UnityEngine.UI;

namespace DeenCraft.UI
{
    public sealed class MasjidInformationPanel : MonoBehaviour
    {
        private const float NoticeSeconds = 4f;
        [SerializeField] private Font _font;
        private GameObject _panel;
        private Text _title;
        private Text _body;
        private Text _notice;
        private float _noticeUntil;
        private ScrollRect _scroll;
        private PlayerController _player;
        private MasjidExplorer _target;
        private ChunkManager _world;
        private int _revision;
        private bool _previousInputBlocked;
        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void OnEnable()
        {
            MasjidExplorer.ReadingRequested += Open;
            BlockInteractor.BlockEditDenied += Notify;
        }

        private void OnDisable()
        {
            MasjidExplorer.ReadingRequested -= Open;
            BlockInteractor.BlockEditDenied -= Notify;
            Close();
        }

        public void Open(MasjidExplorer target, GameObject player)
        {
            if (target == null || player == null || !target.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[MasjidInformationPanel] The reading target is no longer available.", this);
                return;
            }
            var controller = player.GetComponent<PlayerController>();
            if (controller == null || _font == null)
            {
                Debug.LogError("[MasjidInformationPanel] The player controller and reading font must be assigned.", this);
                return;
            }
            if (controller.InputBlocked && (!IsOpen || _player != controller))
            {
                Debug.LogWarning("[MasjidInformationPanel] Close the current menu before reading.", this);
                return;
            }
            if (Vector3.Distance(player.transform.position, target.transform.position) > GameConstants.PlayerReach)
            {
                Debug.LogWarning("[MasjidInformationPanel] Move closer to read.", this);
                return;
            }
            Close();
            if (_panel == null) Build();
            _target = target;
            _player = controller;
            _previousInputBlocked = controller.InputBlocked;
            _world = ChunkManager.Instance;
            _revision = _world != null ? _world.WorldRevision : 0;
            _title.text = target.Title;
            _body.text = target.Body;
            _panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            _body.rectTransform.sizeDelta = new Vector2(0f, _body.preferredHeight + 20f);
            _scroll.verticalNormalizedPosition = 1f;
            controller.InputBlocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_player != null) _player.InputBlocked = _previousInputBlocked;
            _player = null;
            _target = null;
            _world = null;
        }

        private void Update()
        {
            if (_notice != null && Time.unscaledTime >= _noticeUntil) _notice.gameObject.SetActive(false);
            if (!IsOpen) return;
            if (_target == null || !_target.gameObject.activeInHierarchy || _player == null ||
                (_world != null && _world.WorldRevision != _revision) ||
                Vector3.Distance(_player.transform.position, _target.transform.position) > GameConstants.PlayerReach ||
                Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void Notify(GameObject player, string message)
        {
            if (_font == null)
            {
                Debug.LogError("[MasjidInformationPanel] Cannot display protection feedback without a font.", this);
                return;
            }
            if (_notice == null)
            {
                _notice = Label("Landmark notice", transform, new Vector2(0f, -300f), new Vector2(900f, 90f), 24);
                _notice.alignment = TextAnchor.MiddleCenter;
                _notice.color = new Color(1f, 0.9f, 0.55f);
            }
            _notice.text = message;
            _notice.gameObject.SetActive(true);
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
        }

        private void Build()
        {
            _panel = new GameObject("Masjid reading panel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            RectTransform rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(780f, 680f);
            _panel.GetComponent<Image>().color = new Color(0.07f, 0.16f, 0.14f, 0.98f);
            _title = Label("Title", _panel.transform, new Vector2(0f, 270f), new Vector2(700f, 90f), 30);
            _title.alignment = TextAnchor.MiddleCenter;
            var scrollObject = new GameObject("Reading scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(_panel.transform, false);
            var scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(690f, 420f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            viewport.GetComponent<Image>().color = Color.clear;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            _body = Label("Reading text", viewport.transform, Vector2.zero, Vector2.zero, 25);
            _body.alignment = TextAnchor.UpperLeft;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform content = _body.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            _scroll = scrollObject.GetComponent<ScrollRect>();
            _scroll.viewport = viewportRect;
            _scroll.content = content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            var buttonObject = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_panel.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -280f);
            buttonRect.sizeDelta = new Vector2(260f, 50f);
            buttonObject.GetComponent<Image>().color = new Color(0.23f, 0.43f, 0.32f);
            buttonObject.GetComponent<Button>().onClick.AddListener(Close);
            var label = Label("Close label", buttonObject.transform, Vector2.zero, new Vector2(260f, 50f), 24);
            label.text = "Close (Escape)";
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            _panel.SetActive(false);
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
            return text;
        }
    }
}
