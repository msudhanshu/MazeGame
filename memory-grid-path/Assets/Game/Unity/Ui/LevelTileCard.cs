using Game.Core.State;
using Nixin.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Prefab-authored level card. Bind clones this object and only fills number, lock,
    /// stars, and thumbnail — layout and chrome stay on the template.
    /// </summary>
    public sealed class LevelTileCard : MonoBehaviour
    {
        public const string DefaultTilePhotoResource = "Photo/tilethumnail";

        [SerializeField] Button _button;
        [SerializeField] Image _background;
        [SerializeField] Outline _border;
        [SerializeField] RawImage _thumb;
        [SerializeField] GameObject _scrim;
        [SerializeField] GameObject _lock;
        [SerializeField] Transform _stars;
        [SerializeField] Image[] _starIcons;
        [SerializeField] TextMeshProUGUI _number;
        [SerializeField] Sprite _starFill;
        [SerializeField] Sprite _starEmpty;
        [SerializeField] Texture _defaultThumb;
        [SerializeField] Color _clearedFill = MemoryPathPalette.ClearedTile;
        [SerializeField] Color _lockedFill = MemoryPathPalette.LockedTile;
        [SerializeField] Color _clearedBorder = MemoryPathPalette.ClearedBorder;
        [SerializeField] Color _lockedBorder = MemoryPathPalette.LockedBorder;
        [SerializeField] Color _clearedNumber = MemoryPathPalette.Ink;
        [SerializeField] Color _lockedNumber = MemoryPathPalette.ButtonInk;

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
            if (_thumb == null)
                _thumb = Find<RawImage>("Thumb");
            if (_scrim == null)
            {
                var scrim = Find("Scrim");
                if (scrim != null)
                    _scrim = scrim.gameObject;
            }

            if (_lock == null)
            {
                var lockSlot = Find("Overlay/Lock") ?? Find("Lock");
                if (lockSlot != null)
                    _lock = lockSlot.gameObject;
            }

            if (_stars == null)
                _stars = Find("Overlay/Stars") ?? Find("Stars");
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
                var number = Find("Overlay/Number") ?? Find("Number");
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
            var cleared = info.Lane == LevelLane.Cleared;
            if (_background != null)
                _background.color = cleared ? _clearedFill : _lockedFill;
            if (_border != null)
                _border.effectColor = cleared ? _clearedBorder : _lockedBorder;

            if (_button != null)
                _button.interactable = !locked;

            var texture = info.Thumbnail != null ? info.Thumbnail : _defaultThumb;
            var hasThumb = texture != null;
            if (_thumb != null)
            {
                _thumb.texture = texture;
                _thumb.color = hasThumb ? Color.white : info.Swatch;
                _thumb.gameObject.SetActive(hasThumb);
            }

            if (_scrim != null)
                _scrim.SetActive(hasThumb);
            if (_lock != null)
                _lock.SetActive(locked);
            if (_stars != null)
                _stars.gameObject.SetActive(!locked);

            if (!locked && _starIcons != null)
            {
                var filled = Mathf.Clamp(info.Stars, 0, 3);
                for (var i = 0; i < _starIcons.Length; i++)
                {
                    var icon = _starIcons[i];
                    if (icon == null)
                        continue;
                    var sprite = i < filled ? _starFill : _starEmpty;
                    if (sprite != null)
                        icon.sprite = sprite;
                }
            }

            if (_number != null)
            {
                _number.text = info.Number.ToString();
                _number.color = cleared ? _clearedNumber : _lockedNumber;
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
