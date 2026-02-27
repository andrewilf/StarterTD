using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;

namespace StarterTD.UI;

/// <summary>
/// Rendering methods for UIPanel. Partial class split from UIPanel.cs.
/// </summary>
public partial class UIPanel
{
    public void Draw(
        SpriteBatch spriteBatch,
        IReadOnlyDictionary<TowerType, float> cooldowns,
        int lives,
        int wave,
        int totalWaves,
        bool waveInProgress,
        float timeSlowBankFraction,
        Tower? selectedTower = null,
        IEnemy? selectedEnemy = null
    )
    {
        // Panel background
        TextureManager.DrawRect(spriteBatch, new Rectangle(_x, 0, _width, _height), Color.Black);
        // Separator line
        TextureManager.DrawRect(spriteBatch, new Rectangle(_x, 0, 2, _height), Color.Gray);

        if (_font != null)
        {
            float championCooldown = cooldowns.GetValueOrDefault(TowerType.ChampionGun);

            DrawConsolidatedTowerButton(
                spriteBatch,
                _gunTowerButton,
                TowerType.Gun,
                TowerType.ChampionGun,
                cooldowns.GetValueOrDefault(TowerType.Gun),
                championCooldown
            );
            DrawAbilityButton(spriteBatch, _gunAbilityButton, TowerType.ChampionGun);

            DrawConsolidatedTowerButton(
                spriteBatch,
                _cannonTowerButton,
                TowerType.Cannon,
                TowerType.ChampionCannon,
                cooldowns.GetValueOrDefault(TowerType.Cannon),
                championCooldown
            );
            DrawAbilityButton(spriteBatch, _cannonAbilityButton, TowerType.ChampionCannon);

            DrawConsolidatedTowerButton(
                spriteBatch,
                _wallTowerButton,
                TowerType.Walling,
                TowerType.ChampionWalling,
                cooldowns.GetValueOrDefault(TowerType.Walling),
                championCooldown
            );
            DrawAbilityButton(spriteBatch, _wallAbilityButton, TowerType.ChampionWalling);

            string cdText =
                championCooldown > 0f ? $"Champ CD: {championCooldown:F1}s" : "Champ: Ready!";
            Color cdColor = championCooldown > 0f ? Color.OrangeRed : Color.LimeGreen;
            spriteBatch.DrawString(_font, cdText, new Vector2(_x + 10, 10), cdColor);
            spriteBatch.DrawString(
                _font,
                $"Lives: {lives}",
                new Vector2(_x + 10, 35),
                Color.LimeGreen
            );
            spriteBatch.DrawString(
                _font,
                $"Wave: {wave}/{totalWaves}",
                new Vector2(_x + 10, 60),
                Color.White
            );

            // Debug section
            spriteBatch.DrawString(
                _font,
                "Debug Tools:",
                new Vector2(_x + 10, _placeHighGroundButton.Top - 30),
                Color.Orange
            );
            DrawDebugButton(
                spriteBatch,
                _placeHighGroundButton,
                "Place High Ground",
                UISelectionMode.PlaceHighGround
            );
            DrawDebugButton(
                spriteBatch,
                _spawnEnemyButton,
                "Spawn Enemy",
                UISelectionMode.SpawnEnemy
            );

            // Time-slow toggle button
            Color timeSlowBg = IsTimeSlowed ? new Color(0, 80, 120) : new Color(20, 60, 80);
            Color timeSlowOutline = IsTimeSlowed ? Color.DeepSkyBlue : Color.SteelBlue;
            string timeSlowText = IsTimeSlowed ? ">> 0.5x Speed <<" : "Time Slow";
            TextureManager.DrawRect(spriteBatch, _timeSlowButton, timeSlowBg);
            TextureManager.DrawRectOutline(spriteBatch, _timeSlowButton, timeSlowOutline, 2);
            Vector2 tsSize = _font.MeasureString(timeSlowText);
            spriteBatch.DrawString(
                _font,
                timeSlowText,
                new Vector2(
                    _timeSlowButton.X + (_timeSlowButton.Width - tsSize.X) / 2,
                    _timeSlowButton.Y + (_timeSlowButton.Height - tsSize.Y) / 2
                ),
                IsTimeSlowed ? Color.DeepSkyBlue : Color.LightSteelBlue
            );

            DrawTimeSlowBar(spriteBatch, timeSlowBankFraction);

            // Start Wave button
            Color waveBtnColor = waveInProgress ? Color.Gray : Color.Green;
            TextureManager.DrawRect(spriteBatch, _startWaveButton, waveBtnColor);
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
            string waveText = waveInProgress ? "Wave Active..." : "Start Wave";
            Vector2 textSize = _font.MeasureString(waveText);
            spriteBatch.DrawString(
                _font,
                waveText,
                new Vector2(
                    _startWaveButton.X + (_startWaveButton.Width - textSize.X) / 2,
                    _startWaveButton.Y + (_startWaveButton.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
        else
        {
            // Fallback: no font loaded — draw colored blocks as indicators
            bool gunChampAlive = _championManager?.IsChampionAlive(TowerType.ChampionGun) ?? false;
            bool cannonChampAlive =
                _championManager?.IsChampionAlive(TowerType.ChampionCannon) ?? false;

            DrawButtonNoFont(
                spriteBatch,
                _gunTowerButton,
                gunChampAlive ? TowerType.Gun : TowerType.ChampionGun
            );
            DrawButtonNoFont(
                spriteBatch,
                _cannonTowerButton,
                cannonChampAlive ? TowerType.Cannon : TowerType.ChampionCannon
            );
            bool wallChampAlive =
                _championManager?.IsChampionAlive(TowerType.ChampionWalling) ?? false;
            DrawButtonNoFont(
                spriteBatch,
                _wallTowerButton,
                wallChampAlive ? TowerType.Walling : TowerType.ChampionWalling
            );

            TextureManager.DrawRect(
                spriteBatch,
                _timeSlowButton,
                IsTimeSlowed ? new Color(0, 80, 120) : new Color(20, 60, 80)
            );
            TextureManager.DrawRectOutline(spriteBatch, _timeSlowButton, Color.SteelBlue, 2);
            DrawTimeSlowBar(spriteBatch, timeSlowBankFraction);

            TextureManager.DrawRect(
                spriteBatch,
                _startWaveButton,
                waveInProgress ? Color.Gray : Color.Green
            );
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
        }

        if (selectedTower != null)
            DrawTowerInfoPanel(spriteBatch, selectedTower);
        else if (selectedEnemy != null)
            DrawEnemyInfoPanel(spriteBatch, selectedEnemy);
    }

    private void DrawTimeSlowBar(SpriteBatch spriteBatch, float fraction)
    {
        // Dark background track
        TextureManager.DrawRect(spriteBatch, _timeSlowBarBg, new Color(15, 15, 25));

        // Colour signals state: active = DeepSkyBlue, blocked = OrangeRed, recharging = SteelBlue
        Color fillColor;
        if (IsTimeSlowed)
            fillColor = Color.DeepSkyBlue;
        else if (!CanActivateTimeSlow)
            fillColor = Color.OrangeRed;
        else
            fillColor = Color.SteelBlue;

        int fillWidth = (int)(_timeSlowBarBg.Width * fraction);
        if (fillWidth > 0)
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(_timeSlowBarBg.X, _timeSlowBarBg.Y, fillWidth, _timeSlowBarBg.Height),
                fillColor
            );
        }
    }

    private void DrawButtonNoFont(SpriteBatch spriteBatch, Rectangle rect, TowerType type)
    {
        bool isSelected = SelectedTowerType == type;
        Color bgColor = isSelected ? Color.SlateGray : Color.DarkSlateGray;
        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(type);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );
    }

    /// <summary>
    /// Draws a single button that acts as the champion button when the champion is dead,
    /// and switches to the generic button once the champion is alive on the field.
    /// </summary>
    private void DrawConsolidatedTowerButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        TowerType genericType,
        TowerType championType,
        float genericCooldown,
        float championCooldown
    )
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        bool isSelected = SelectedTowerType == genericType || SelectedTowerType == championType;

        Color bgColor;
        Color textColor = Color.White;
        string mainLabel;
        string? subLabel = null;
        TowerType indicatorType;

        if (!championAlive)
        {
            indicatorType = championType;
            mainLabel = $"{genericType} Champion";

            float globalCooldown = _championManager?.GlobalCooldownRemaining ?? 0f;
            float respawnCooldown = _championManager?.GetRespawnCooldown(championType) ?? 0f;

            if (championCooldown > 0f)
            {
                bgColor = Color.DarkGray;
                subLabel = $"Locked: {championCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else if (globalCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                subLabel = $"Global: {globalCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else if (respawnCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                subLabel = $"Respawn: {respawnCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else
            {
                bgColor = Color.DarkSlateGray;
                subLabel = "Place Champion";
            }
        }
        else
        {
            indicatorType = genericType;
            mainLabel = $"{genericType}";
            if (genericCooldown > 0f)
            {
                bgColor = Color.DarkGray;
                textColor = Color.DarkGray;
                subLabel = $"Locked: {genericCooldown:F1}s";
            }
            else
            {
                bgColor = Color.DarkSlateGray;
            }
        }

        if (isSelected)
            bgColor = Color.SlateGray;

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(indicatorType);
        float activeCooldown = championAlive ? genericCooldown : championCooldown;
        Color swatchColor = activeCooldown > 0f ? Color.DarkGray : stats.Color;
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            swatchColor
        );

        spriteBatch.DrawString(_font, mainLabel, new Vector2(rect.X + 50, rect.Y + 10), textColor);

        if (subLabel != null)
        {
            Color subColor;
            if (subLabel == "Place Champion")
                subColor = Color.LightGreen;
            else if (subLabel.StartsWith("Locked"))
                subColor = Color.OrangeRed;
            else
                subColor = Color.Yellow; // global/respawn cooldown text
            spriteBatch.DrawString(
                _font,
                subLabel,
                new Vector2(rect.X + 50, rect.Y + 28),
                subColor
            );
        }
    }

    /// <summary>
    /// Draws a semi-transparent info panel showing stats for the selected tower.
    /// Positioned above the Start Wave button.
    /// </summary>
    private void DrawTowerInfoPanel(SpriteBatch spriteBatch, Tower tower)
    {
        if (_font == null)
            return;

        const int padding = 8;
        const int lineHeight = 20;
        int panelWidth = _width - 12;
        int numLines = 6; // name + separator + HP + block + damage + fire rate
        int panelHeight = padding * 2 + numLines * lineHeight;
        int panelX = _x + 6;
        int panelY = _startWaveButton.Y - panelHeight - 10;

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Black * 0.75f
        );
        TextureManager.DrawRectOutline(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Gray,
            1
        );

        int textX = panelX + padding;
        int y = panelY + padding;

        // Tower name with color indicator
        TextureManager.DrawRect(spriteBatch, new Rectangle(textX, y + 2, 12, 12), tower.TowerColor);
        spriteBatch.DrawString(_font, tower.Name, new Vector2(textX + 18, y), Color.White);
        y += lineHeight;

        // Thin separator line
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(textX, y + 2, panelWidth - padding * 2, 1),
            Color.Gray
        );
        y += lineHeight - 4;

        string hpText = $"HP: {tower.CurrentHealth} / {tower.MaxHealth}";
        spriteBatch.DrawString(_font, hpText, new Vector2(textX, y), Color.LimeGreen);
        y += lineHeight;

        int remaining = Math.Max(0, tower.BlockCapacity - tower.CurrentEngagedCount);
        string blockText = $"Block: {remaining} / {tower.BlockCapacity}";
        spriteBatch.DrawString(_font, blockText, new Vector2(textX, y), Color.CornflowerBlue);
        y += lineHeight;

        string dmgText = tower.IsAOE ? $"Damage: {tower.Damage} (AOE)" : $"Damage: {tower.Damage}";
        spriteBatch.DrawString(_font, dmgText, new Vector2(textX, y), Color.White);
        y += lineHeight;

        // Show as attacks per second for readability
        float aps = 1f / tower.FireRate;
        string fireText = $"Fire Rate: {aps:F1}/s";
        spriteBatch.DrawString(_font, fireText, new Vector2(textX, y), Color.White);
    }

    private void DrawEnemyInfoPanel(SpriteBatch spriteBatch, IEnemy enemy)
    {
        if (_font == null)
            return;

        const int padding = 8;
        const int lineHeight = 20;
        int panelWidth = _width - 12;
        int numLines = 6; // name + separator + health + speed + bounty + attack damage
        int panelHeight = padding * 2 + numLines * lineHeight;
        int panelX = _x + 6;
        int panelY = _startWaveButton.Y - panelHeight - 10;

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Black * 0.75f
        );
        TextureManager.DrawRectOutline(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Gray,
            1
        );

        int textX = panelX + padding;
        int y = panelY + padding;

        // Enemy name with color indicator
        TextureManager.DrawRect(spriteBatch, new Rectangle(textX, y + 2, 12, 12), Color.OrangeRed);
        spriteBatch.DrawString(_font, enemy.Name, new Vector2(textX + 18, y), Color.White);
        y += lineHeight;

        // Thin separator line
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(textX, y + 2, panelWidth - padding * 2, 1),
            Color.Gray
        );
        y += lineHeight - 4;

        string healthText = $"Health: {enemy.Health:F1} / {enemy.MaxHealth:F1}";
        spriteBatch.DrawString(_font, healthText, new Vector2(textX, y), Color.LimeGreen);
        y += lineHeight;

        string speedText = $"Speed: {enemy.Speed:F1}";
        spriteBatch.DrawString(_font, speedText, new Vector2(textX, y), Color.CornflowerBlue);
        y += lineHeight;

        string bountyText = $"Bounty: ${enemy.Bounty}";
        spriteBatch.DrawString(_font, bountyText, new Vector2(textX, y), Color.Gold);
        y += lineHeight;

        string dmgText = $"Attack Dmg: {enemy.AttackDamage}";
        spriteBatch.DrawString(_font, dmgText, new Vector2(textX, y), Color.White);
    }

    private void DrawAbilityButton(SpriteBatch spriteBatch, Rectangle rect, TowerType championType)
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        float cooldown = _championManager?.GetAbilityCooldownRemaining(championType) ?? 0f;

        Color bgColor;
        string label;
        Color textColor;
        Color outlineColor;

        if (!championAlive)
        {
            bgColor = new Color(40, 40, 40);
            label = "ABILITY (no champion)";
            textColor = Color.DarkGray;
            outlineColor = Color.DarkGray;
        }
        else if (cooldown > 0f)
        {
            bgColor = new Color(50, 50, 30);
            label = $"ABILITY CD: {cooldown:F1}s";
            textColor = Color.Yellow;
            outlineColor = Color.DarkGoldenrod;
        }
        else
        {
            bgColor = new Color(30, 80, 20);
            label = "USE ABILITY!";
            textColor = Color.LimeGreen;
            outlineColor = Color.LimeGreen;
        }

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(spriteBatch, rect, outlineColor, 1);

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2f,
                    rect.Y + (rect.Height - textSize.Y) / 2f
                ),
                textColor
            );
        }
    }

    private void DrawDebugButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        UISelectionMode mode
    )
    {
        bool isSelected = SelectionMode == mode;
        Color bgColor = isSelected ? Color.DarkOrange : new Color(60, 40, 20);

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2,
                    rect.Y + (rect.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
    }
}
