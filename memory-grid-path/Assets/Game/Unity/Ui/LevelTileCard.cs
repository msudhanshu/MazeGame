using Game.Core.State;
using Nixin.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Prefab-authored level card. Bind clones this object and only fills caption, lock,
    /// stars, and thumbnail — layout and chrome stay on the template.
    /// </summary>
    public sealed class LevelTileCard : MonoBehaviour
    {
        public const string DefaultTilePhotoResource = "Photo/tilethumnail";

        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] Outline _border;
        [SerializeField] Mask _mask;
        [SerializeField] RawImage _thumb;
        [SerializeField] GameObject _lock;
        [SerializeField] Image _lockScrim;
        [SerializeField] Transform _stars;
        [SerializeField] Image _starPanel;
        [SerializeField] Image[] _starIcons;
        [SerializeField] TextMeshProUGUI _number;
        [SerializeField] Sprite _starFill;
        [SerializeField] Sprite _starEmpty;
        [SerializeField] Texture _defaultThumb;
        [SerializeField] Color _clearedFill = MemoryPathPalette.ClearedTile;
        [SerializeField] Color _lockedFill = MemoryPathPalette.LockedTile;
        [SerializeField] Color _clearedBorder = MemoryPathPalette.ClearedBorder;
        [SerializeField] Color _lockedBorder = MemoryPathPalette.LockedBorder;
        [SerializeField] Color _currentBorder = MemoryPathPalette.HomeModeSelectedRing;
        [SerializeField] Color _clearedNumber = MemoryPathPalette.Ink;
        [SerializeField] Color _lockedNumber = MemoryPathPalette.HomeMuted;
        [SerializeField] Color _starPanelFill = MemoryPathPalette.ScoreChip;
        [SerializeField] Color _lockScrimColor = new Color(0.12f, 0.14f, 0.18f, 0.55f);
        [SerializeField] float _currentOutline = 2.5f;

        public Button Button => _button;
        public TextMeshProUGUI NumberLabel => _number;
        public GameObject LockIcon => _lock;
        public Transform Stars => _stars;
        public RawImage Thumb => _thumb;

        public void EnsureSlots()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_background == null)
                _background = GetComponent<Image>();
            if (_border == null)
                _border = GetComponent<Outline>();
            if (_mask == null)
                _mask = GetComponent<Mask>();
            if (_thumb == null)
                _thumb = Find<RawImage>("Art/Thumb") ?? Find<RawImage>("Thumb");

            if (_lock == null)
            {
                var overlay = Find("Art/LockOverlay") ?? Find("LockOverlay");
                if (overlay != null)
                    _lock = overlay.gameObject;
                else
                {
                    var lockSlot = Find("Overlay/Lock") ?? Find("Lock");
                    if (lockSlot != null)
                        _lock = lockSlot.gameObject;
                }
            }

            if (_lockScrim == null)
            {
                var scrim = Find("Art/LockOverlay/Scrim") ?? Find("LockOverlay/Scrim") ?? Find("Scrim");
                if (scrim != null)
                    _lockScrim = scrim.GetComponent<Image>();
            }

            if (_stars == null)
                _stars = Find("Stars") ?? Find("Overlay/Stars");
            if (_starPanel == null && _stars != null)
                _starPanel = _stars.GetComponent<Image>();
            if ((_starIcons == null || _starIcons.Length == 0) && _stars != null)
            {
                _starIcons = new Image[3];
                for (var i = 0; i < 3; i++)
                {
                    var star = _stars.Find("Star" + i);
                    if (star != null)
                        _starIcons[i] = star.GetComponent<Image>();
                }
            }

            if (_number == null)
            {
                var number = Find("Caption") ?? Find("Overlay/Number") ?? Find("Number");
                if (number != null)
                    _number = number.GetComponent<TextMeshProUGUI>();
            }

            if (_starFill == null)
                _starFill = UiDraw.ResourceSprite(NixinIcons.StarFill);
            if (_starEmpty == null)
                _starEmpty = UiDraw.ResourceSprite(NixinIcons.StarEmpty);
            if (_defaultThumb == null)
                _defaultThumb = Resources.Load<Texture2D>(DefaultTilePhotoResource);
        }

        public void Apply(JourneyLevelTileInfo info)
        {
            EnsureSlots();
            if (info == null)
                return;

            var locked = info.Lane == LevelLane.Locked;
            var current = info.Lane == LevelLane.Current;
            if (_background != null)
                _background.color = locked ? _lockedFill : _clearedFill;
            if (_border != null)
            {
                _border.effectColor = current ? _currentBorder : (locked ? _lockedBorder : _clearedBorder);
                var thickness = current ? _currentOutline : 1f;
                _border.effectDistance = new Vector2(thickness, -thickness);
                _border.useGraphicAlpha = !current;
            }

            if (_mask != null)
                _mask.enabled = !current;

            if (_button != null)
                _button.interactable = !locked;

            var texture = info.Thumbnail != null ? info.Thumbnail : _defaultThumb;
            var hasThumb = texture != null;
            if (_thumb != null)
            {
                _thumb.texture = texture;
                _thumb.color = hasThumb ? Color.white : info.Swatch;
                _thumb.gameObject.SetActive(true);
            }

            if (_lock != null)
                _lock.SetActive(locked);
            if (_lockScrim != null)
            {
                _lockScrim.color = _lockScrimColor;
                _lockScrim.gameObject.SetActive(locked);
            }

            var filled = Mathf.Clamp(info.Stars, 0, 3);
            if (_starPanel != null)
                _starPanel.color = filled > 0 ? _starPanelFill : Color.clear;
            if (_starIcons != null)
            {
                for (var i = 0; i < _starIcons.Length; i++)
                {
                    var icon = _starIcons[i];
                    if (icon == null)
                        continue;
                    icon.gameObject.SetActive(filled > 0);
                    if (filled <= 0)
                        continue;
                    var sprite = i < filled ? _starFill : _starEmpty;
                    if (sprite != null)
                        icon.sprite = sprite;
                }
            }

            if (_number != null)
            {
                _number.text = "Level " + info.Number;
                _number.color = locked ? _lockedNumber : _clearedNumber;
            }
        }

        public void SetClick(System.Action onClick)
        {
            if (_button == null)
                return;
            _button.onClick.RemoveAllListeners();
            if (onClick != null)
                _button.onClick.AddListener(() => onClick());
        }

        Transform Find(string path) => transform.Find(path);

        T Find<T>(string path) where T : Component
        {
            var slot = Find(path);
            return slot != null ? slot.GetComponent<T>() : null;
        }
    }
}
