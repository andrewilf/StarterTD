using System;
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
        int lives,
        string spawnStatusText,
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
            spriteBatch.DrawString(
                _font,
                $"Lives: {lives}",
                new Vector2(_x + 10, 10),
                Color.LimeGreen
            );
            spriteBatch.DrawString(_font, spawnStatusText, new Vector2(_x + 10, 60), Color.White);
            spriteBatch.DrawString(
                _font,
                "Debug Tools:",
                new Vector2(_x + 10, _placeHighGroundButton.Top - 30),
                Color.Orange
            );
        }
        else
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(_x + 10, 60, _width - 20, 24),
                Color.Black
            );
        }

        DrawTimeSlowBar(spriteBatch, timeSlowBankFraction);

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

    /// <summary>
    /// Draws a semi-transparent info panel showing stats for the selected tower.
    /// Positioned above the bottom time-slow controls.
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
        int panelY = _timeSlowButton.Y - panelHeight - 10;

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
        float aps = 1f / tower.EffectiveFireInterval;
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
        int numLines = 5; // name + separator + health + speed + attack damage
        int panelHeight = padding * 2 + numLines * lineHeight;
        int panelX = _x + 6;
        int panelY = _timeSlowButton.Y - panelHeight - 10;

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

        string damageText = $"Attack Dmg: {enemy.AttackDamage}";
        spriteBatch.DrawString(_font, damageText, new Vector2(textX, y), Color.White);
    }
}
