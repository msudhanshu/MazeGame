using System;
using Game.Unity.Fue;
using UnityEngine;

namespace Game.Unity.Ui
{
    /// <summary>Figma popup copy, color, and button chrome for Memory Path dialogs.</summary>
    public static class MemoryPathPopups
    {
        public static MemoryPathPopupPayload Exit(int level, Action stay, Action leave)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Exit,
                Panel = MemoryPathPalette.PopupCream,
                Kicker = "Exit Confirmation",
                KickerColor = MemoryPathPalette.KickerExit,
                Title = "Leaving so soon?",
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 18,
                Message = "Your current path progress on Level " + Mathf.Max(1, level) + " will be lost.",
                CloseOnBackdrop = true,
                Buttons = new[]
                {
                    Stay("Stay", stay),
                    new MemoryPathButton
                    {
                        Label = "Leave",
                        Fill = MemoryPathPalette.Leave,
                        OnClick = leave
                    }
                }
            };
        }

        public static MemoryPathPopupPayload Complete(int score, bool canAdvance, Action next, Action replay, Action home = null)
        {
            var buttons = canAdvance
                ? new[]
                {
                    Primary(
                        "Next Level",
                        MemoryPathPalette.Next,
                        new Color(0.063f, 0.725f, 0.506f, 0.4f),
                        next),
                    Ghost("Replay", MemoryPathPalette.ReplayBorder, MemoryPathPalette.Body, replay),
                    QuietMenu("Main Menu", home)
                }
                : new[]
                {
                    Primary(
                        "Replay",
                        MemoryPathPalette.Next,
                        new Color(0.063f, 0.725f, 0.506f, 0.4f),
                        replay),
                    QuietMenu("Main Menu", home)
                };

            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Complete,
                Panel = MemoryPathPalette.PopupComplete,
                Kicker = "Level Completed!",
                KickerColor = MemoryPathPalette.KickerWin,
                Title = "Splendid Memory!",
                TitleColor = MemoryPathPalette.TitleGold,
                TitleWeight = UiWeight.Black,
                TitleSize = 24,
                Stars = 3,
                ScoreLine = score + " pts",
                CloseOnBackdrop = false,
                StackButtons = true,
                Buttons = buttons
            };
        }

        public static MemoryPathPopupPayload GameOver(Action home, Action retry)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Fail,
                Panel = MemoryPathPalette.PopupFail,
                Kicker = "Game Over",
                KickerColor = MemoryPathPalette.KickerFail,
                Title = "You Failed!",
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 26,
                Message = "Try again on a\n NEW PATH.\nMemorize this time.",
                Glyph = MemoryPathGlyph.Heart,
                CloseOnBackdrop = false,
                StackButtons = true,
                MessageSize = 22,
                PanelWidth = MemoryPathPopup.S(420),
                Buttons = new[]
                {
                    Primary(
                        "Try Again",
                        MemoryPathPalette.Leave,
                        new Color(1f, 0.42f, 0.42f, 0.33f),
                        retry),
                    QuietMenu("Main Menu", home)
                }
            };
        }

        public static MemoryPathPopupPayload Pause(Action resume, Action restart, Action quit)
        {
            return Pause(resume, restart, null, quit);
        }

        public static MemoryPathPopupPayload Pause(Action resume, Action restart, Action selectLevel, Action quit)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Pause,
                Panel = MemoryPathPalette.PopupPause,
                Kicker = "Game Paused",
                KickerColor = MemoryPathPalette.KickerPause,
                Title = "Take a breather!",
                TitleColor = MemoryPathPalette.PauseTitle,
                TitleWeight = UiWeight.SemiBold,
                TitleSize = 15,
                Glyph = MemoryPathGlyph.Pause,
                CloseOnBackdrop = true,
                StackButtons = true,
                Buttons = new[]
                {
                    Primary("Resume Path", MemoryPathPalette.Resume, new Color(0.388f, 0.4f, 0.945f, 0.4f), resume),
                    new MemoryPathButton
                    {
                        Label = "Restart",
                        Fill = MemoryPathPalette.Restart,
                        Border = MemoryPathPalette.RestartBorder,
                        LabelColor = MemoryPathPalette.ScoreCaption,
                        Height = MemoryPathPopup.S(42),
                        OnClick = restart
                    },
                    new MemoryPathButton
                    {
                        Label = "Select Level",
                        Fill = MemoryPathPalette.Stay,
                        Border = MemoryPathPalette.StayBorder,
                        LabelColor = MemoryPathPalette.ButtonInk,
                        Height = MemoryPathPopup.S(42),
                        OnClick = selectLevel
                    },
                    Ghost("Quit to Menu", MemoryPathPalette.QuitBorder, MemoryPathPalette.HudMuted, quit)
                }
            };
        }

        public static MemoryPathPopupPayload Skip(int rewardPoints, Action play, Action skip)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Exit,
                Panel = MemoryPathPalette.PopupCream,
                Kicker = "Skip",
                KickerColor = MemoryPathPalette.KickerPause,
                Title = "Skip this stretch?",
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 18,
                Message = "Play it, or skip for +" + rewardPoints + " points.",
                CloseOnBackdrop = false,
                Buttons = new[]
                {
                    Stay("Play", play),
                    new MemoryPathButton
                    {
                        Label = "Skip",
                        Fill = MemoryPathPalette.Resume,
                        OnClick = skip
                    }
                }
            };
        }

        public static MemoryPathPopupPayload RetryFromMemory()
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Fail,
                Panel = MemoryPathPalette.PopupFail,
                Kicker = "Walk Over",
                KickerColor = MemoryPathPalette.KickerFail,
                Title = "Try again from memory",
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 28,
                KickerSize = 16,
                MessageSize = 22,
                PanelWidth = MemoryPathPopup.S(430),
                Message = "With your memory, try to walk the SAME PATH again.",
                CloseOnBackdrop = true,
                ShowCloseButton = true,
                Buttons = Array.Empty<MemoryPathButton>()
            };
        }

        public static MemoryPathPopupPayload TutorialSkipped(Action onOk)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Exit,
                Panel = MemoryPathPalette.PopupCream,
                Kicker = "Tutorial",
                KickerColor = MemoryPathPalette.KickerPause,
                Title = TutorialCopy.SkipTitle,
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 16,
                Message = TutorialCopy.SkipBody,
                CloseOnBackdrop = true,
                ShowCloseButton = true,
                PanelWidth = MemoryPathPopup.S(360),
                MessageSize = 15,
                Buttons = new[]
                {
                    Primary(TutorialCopy.SkipOk, MemoryPathPalette.PlayButton, Color.clear, onOk)
                }
            };
        }

        public static MemoryPathPopupPayload Notice(string title, string body, string actionLabel, Action onAction)
        {
            return new MemoryPathPopupPayload
            {
                Chrome = MemoryPathPopupChrome.Exit,
                Panel = MemoryPathPalette.PopupCream,
                Kicker = "Hang On",
                KickerColor = MemoryPathPalette.KickerExit,
                Title = title,
                TitleColor = MemoryPathPalette.PopupInk,
                TitleWeight = UiWeight.ExtraBold,
                TitleSize = 18,
                Message = body,
                CloseOnBackdrop = false,
                Buttons = new[]
                {
                    Primary(actionLabel, MemoryPathPalette.PlayButton, Color.clear, onAction)
                }
            };
        }

        static MemoryPathButton Stay(string label, Action onClick)
        {
            return new MemoryPathButton
            {
                Label = label,
                Fill = MemoryPathPalette.Stay,
                Border = MemoryPathPalette.StayBorder,
                LabelColor = MemoryPathPalette.ButtonInk,
                OnClick = onClick
            };
        }

        static MemoryPathButton Primary(string label, Color fill, Color shadow, Action onClick)
        {
            return new MemoryPathButton
            {
                Label = label,
                Fill = fill,
                Weight = UiWeight.ExtraBold,
                Height = MemoryPathPopup.S(48),
                Shadow = shadow,
                OnClick = onClick
            };
        }

        static MemoryPathButton Ghost(string label, Color border, Color text, Action onClick)
        {
            return new MemoryPathButton
            {
                Label = label,
                Fill = Color.white,
                Border = border,
                LabelColor = text,
                Height = MemoryPathPopup.S(42),
                OnClick = onClick
            };
        }

        static MemoryPathButton QuietMenu(string label, Action onClick)
        {
            return new MemoryPathButton
            {
                Label = label,
                Fill = Color.clear,
                LabelColor = MemoryPathPalette.HudMuted,
                Weight = UiWeight.SemiBold,
                Height = MemoryPathPopup.S(32),
                OnClick = onClick
            };
        }
    }
}
