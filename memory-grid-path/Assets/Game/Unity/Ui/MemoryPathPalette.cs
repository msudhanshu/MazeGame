using UnityEngine;

namespace Game.Unity.Ui
{
    /// <summary>Figma Memory Path tokens — dark HUD vs pastel menus vs cream popups.</summary>
    public static class MemoryPathPalette
    {
        public static readonly Color PlayBackground = Hex(0x0B0D14);
        public static readonly Color HudPanel = new Color(13f / 255f, 17f / 255f, 31f / 255f, 0.92f);
        public static readonly Color HudBorder = Hex(0x1E293B);
        public static readonly Color HudPill = Hex(0x2B2F3A);
        public static readonly Color HudPause = new Color(30f / 255f, 41f / 255f, 59f / 255f, 0.8f);
        public static readonly Color HudPauseGlow = new Color(244f / 255f, 114f / 255f, 182f / 255f, 0.25f);
        public static readonly Color HudHealthWell = new Color(11f / 255f, 15f / 255f, 25f / 255f, 0.6f);
        public static readonly Color HudSteps = new Color(30f / 255f, 41f / 255f, 59f / 255f, 0.5f);
        public static readonly Color HudStepsBorder = new Color(244f / 255f, 114f / 255f, 182f / 255f, 0.2f);
        public static readonly Color HudText = Hex(0xF3F4F6);
        public static readonly Color HudMuted = Hex(0x9CA3AF);
        public static readonly Color HudLevel = Hex(0xFBBF24);
        public static readonly Color HudScore = Hex(0x22D3EE);
        public static readonly Color Teal = Hex(0x2DD4BF);
        public static readonly Color HealthFill = Hex(0x33F24D);
        public static readonly Color HealthGlow = new Color(102f / 255f, 1f, 128f / 255f, 0.3f);
        public static readonly Color HealthEmpty = Hex(0x1F2E26);
        public static readonly Color HealthDivider = Hex(0x0F141A);

        public static readonly Color Ink = Hex(0x111827);
        public static readonly Color PopupInk = Hex(0x1A1D2A);
        public static readonly Color Body = Hex(0x6B7280);
        public static readonly Color ButtonInk = Hex(0x374151);
        public static readonly Color Card = new Color(1f, 1f, 1f, 0.88f);
        public static readonly Color CardBorder = Hex(0xE5E7EB);
        public static readonly Color PlayButton = Hex(0x14B8A6);
        public static readonly Color LevelsButton = Hex(0xF59E0B);
        public static readonly Color SettingsButton = Hex(0xF472B6);
        public static readonly Color Mascot = Hex(0xFB923C);
        public static readonly Color ScoreOrange = Hex(0xFB923C);
        public static readonly Color ScoreChip = Hex(0xFFF9E6);
        public static readonly Color ScoreChipBorder = Hex(0xFDE68A);
        public static readonly Color ScoreCaption = Hex(0x92400E);
        public static readonly Color ScoreValue = Hex(0xD97706);
        public static readonly Color ClearedTile = Hex(0xCCFBF1);
        public static readonly Color ClearedBorder = Hex(0x99F6E4);
        public static readonly Color LockedTile = Hex(0xE5E7EB);
        public static readonly Color LockedBorder = Hex(0xD1D5DB);

        public static readonly Color PopupCream = Hex(0xFFFBF0);
        public static readonly Color PopupComplete = Hex(0xFEFFF8);
        public static readonly Color PopupFail = Hex(0xFFF5F5);
        public static readonly Color PopupPause = Hex(0xF0F9FF);
        public static readonly Color Dimmer = new Color(13f / 255f, 14f / 255f, 21f / 255f, 0.82f);
        public static readonly Color Leave = Hex(0xFF6B6B);
        public static readonly Color Stay = Hex(0xF0F4FF);
        public static readonly Color StayBorder = Hex(0xCBD5E1);
        public static readonly Color Next = Hex(0x10B981);
        public static readonly Color Resume = Hex(0x6366F1);
        public static readonly Color Restart = Hex(0xFFF3CD);
        public static readonly Color RestartBorder = Hex(0xFDE68A);
        public static readonly Color ReplayBorder = Hex(0xD1D5DB);
        public static readonly Color HomeFill = Hex(0xF3F4F6);
        public static readonly Color HomeBorder = Hex(0xE5E7EB);
        public static readonly Color QuitBorder = Hex(0xE5E7EB);
        public static readonly Color GlyphFail = Hex(0xFFE4E1);
        public static readonly Color GlyphFailBorder = Hex(0xFECACA);
        public static readonly Color GlyphPause = Hex(0xEEF2FF);
        public static readonly Color GlyphPauseBorder = Hex(0xC7D2FE);
        public static readonly Color PauseTitle = Hex(0x374151);
        public static readonly Color ConfettiRed = Hex(0xFF6B6B);
        public static readonly Color ConfettiYellow = Hex(0xFFD93D);
        public static readonly Color ConfettiGreen = Hex(0x6BCB77);
        public static readonly Color ConfettiBlue = Hex(0x4D96FF);
        public static readonly Color ConfettiOrange = Hex(0xFB923C);
        public static readonly Color ConfettiGold = Hex(0xFBBF24);
        public static readonly Color ConfettiPurple = Hex(0xA78BFA);
        public static readonly Color ConfettiPink = Hex(0xF472B6);
        public static readonly Color ConfettiMint = Hex(0x34D399);
        public static readonly Color ConfettiSky = Hex(0x60A5FA);
        public static readonly Color ConfettiIndigo = Hex(0x818CF8);
        public static readonly Color KickerExit = Hex(0xE05C5C);
        public static readonly Color KickerWin = Hex(0x059669);
        public static readonly Color KickerFail = Hex(0xEF4444);
        public static readonly Color KickerPause = Hex(0x6366F1);
        public static readonly Color TitleGold = Hex(0xF59E0B);

        public static readonly Color MenuFrom = Hex(0xFCCFCF);
        public static readonly Color MenuMid = Hex(0xFAF0A0);
        public static readonly Color MenuTo = Hex(0xB8EDE8);

        public static readonly Color HomeInk = Hex(0x1E293B);
        public static readonly Color HomeMuted = Hex(0x64748B);
        public static readonly Color HomeFrost = new Color(1f, 1f, 1f, 0.7f);
        public static readonly Color HomeFrostBorder = new Color(1f, 1f, 1f, 0.6f);
        public static readonly Color HomeModePanel = new Color(0.82f, 0.86f, 0.90f, 0.88f);
        public static readonly Color HomePlayFrom = Hex(0x2DD4BF);
        public static readonly Color HomePlayTo = Hex(0x0D9488);
        public static readonly Color HomePlayRing = new Color(1f, 1f, 1f, 0.65f);
        public static readonly Color HomePlayShadow = new Color(13f / 255f, 148f / 255f, 136f / 255f, 0.33f);
        public static readonly Color HomeStar = Hex(0xF59E0B);
        public static readonly Color HomeScore = Hex(0x0D9488);
        public static readonly Color HomeModeSelected = Hex(0x14B8A6);
        public static readonly Color HomeGraph = Hex(0x6366F1);
        public static readonly Color HomeLockedCard = new Color(226f / 255f, 232f / 255f, 240f / 255f, 0.5f);
        public static readonly Color HomeLockedHeader = new Color(148f / 255f, 163f / 255f, 184f / 255f, 0.25f);
        public static readonly Color HomeDivider = new Color(148f / 255f, 163f / 255f, 184f / 255f, 0.35f);
        public static readonly Color SelectTo = Hex(0xFCE7F3);
        public static readonly Color SelectMid = Hex(0xFFEDD5);
        public static readonly Color SelectFrom = Hex(0xDBEAFE);
        public static readonly Color SettingsFrom = Hex(0xE0F2FE);
        public static readonly Color LogoWell = Hex(0x11131C);
        public static readonly Color DetailTo = Hex(0xFFD1D1);
        public static readonly Color DetailMid = Hex(0xFFE4D6);
        public static readonly Color DetailFrom = Hex(0xF3E8FF);
        public static readonly Color BannerFrom = Hex(0xFDBA74);
        public static readonly Color BannerVia = Hex(0xFB923C);
        public static readonly Color BannerTo = Hex(0xF59E0B);
        public static readonly Color BannerKicker = Hex(0xFDE68A);
        public static readonly Color EmptyStar = Hex(0xD1D5DB);
        public static readonly Color NavFrost = new Color(1f, 1f, 1f, 0.8f);

        public static readonly Color Ocean = Hex(0x14B8A6);
        public static readonly Color Sunset = Hex(0xF97316);
        public static readonly Color Forest = Hex(0x65A30D);
        public static readonly Color Candy = Hex(0xF472B6);

        static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }
    }
}
