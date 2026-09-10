using Game.Core.Domain;
using Game.Core.Rules;
using Nixin.Memory.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Unity
{
    public sealed class InvestigationPlay : MonoBehaviour
    {
        Investigation _investigation;
        Text _title;
        Text _status;
        Transform _buttonRow;
        Font _font;

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            }

            BuildUi();
            StartCase();
        }

        void Update()
        {
            if (_investigation == null || _investigation.Phase != InvestigationPhase.Questioning)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var options = _investigation.CurrentOptions();
            if (keyboard.digit1Key.wasPressedThisFrame && options.Count > 0)
                Answer(options[0].Token);
            else if (keyboard.digit2Key.wasPressedThisFrame && options.Count > 1)
                Answer(options[1].Token);
            else if (keyboard.digit3Key.wasPressedThisFrame && options.Count > 2)
                Answer(options[2].Token);
        }

        void StartCase()
        {
            _investigation = new Investigation(FirstCase.ManorMurder());
            Refresh();
        }

        void BeginQuestioning()
        {
            _investigation.BeginQuestioning();
            Refresh();
        }

        void Answer(TokenId choice)
        {
            _investigation.Answer(choice);
            Refresh();
        }

        void Refresh()
        {
            _title.text = _investigation.Title;
            switch (_investigation.Phase)
            {
                case InvestigationPhase.Briefing:
                    _status.text = "Clues:\n- " + string.Join("\n- ", _investigation.Clues) +
                        "\n\nStudy them, then start questioning.";
                    break;
                case InvestigationPhase.Questioning:
                    _status.text = "Attempt " + _investigation.Attempt + "  ·  " + _investigation.CurrentQuestion +
                        "\nClick an answer, or press 1 / 2 / 3.";
                    break;
                case InvestigationPhase.Closed:
                    _status.text = "Case closed. You remembered the clues.";
                    break;
            }

            RebuildButtons();
        }

        void RebuildButtons()
        {
            for (var i = _buttonRow.childCount - 1; i >= 0; i--)
                Destroy(_buttonRow.GetChild(i).gameObject);

            if (_investigation.Phase == InvestigationPhase.Briefing)
            {
                AddButton("Begin questioning", new Color(0.28f, 0.32f, 0.5f), BeginQuestioning);
                return;
            }

            if (_investigation.Phase == InvestigationPhase.Closed)
            {
                AddButton("New case", new Color(0.25f, 0.45f, 0.32f), DetectiveBoot.ReloadPlay);
                return;
            }

            var options = _investigation.CurrentOptions();
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var label = (i + 1) + "  " + option.Token.Value;
                if (option.RememberedAsWrong)
                    label += "  (ruled out)";
                var color = option.RememberedAsWrong
                    ? new Color(0.4f, 0.16f, 0.18f)
                    : new Color(0.35f, 0.38f, 0.52f);
                var token = option.Token;
                AddButton(label, color, () => Answer(token));
            }
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _title = MakeText(canvasGo.transform, "Title", 48, FontStyle.Bold, new Vector2(0, -70), new Vector2(1600, 80));
            _status = MakeText(canvasGo.transform, "Status", 26, FontStyle.Normal, new Vector2(0, -280), new Vector2(1500, 360));

            var row = new GameObject("Buttons");
            row.transform.SetParent(canvasGo.transform, false);
            var rowRect = row.GetComponent<RectTransform>();
            if (rowRect == null)
                rowRect = row.AddComponent<RectTransform>();
            row.AddComponent<HorizontalLayoutGroup>();
            rowRect.anchorMin = new Vector2(0.5f, 0f);
            rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0, 80);
            rowRect.sizeDelta = new Vector2(1600, 120);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            _buttonRow = row.transform;
        }

        Text MakeText(Transform parent, string name, int size, FontStyle style, Vector2 anchored, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = sizeDelta;
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        void AddButton(string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(_buttonRow, false);
            go.GetComponent<LayoutElement>().preferredHeight = 96;
            go.GetComponent<Image>().color = color;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var child = new GameObject("Label", typeof(Text));
            child.transform.SetParent(go.transform, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = child.GetComponent<Text>();
            text.font = _font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(go);
        }
    }
}
