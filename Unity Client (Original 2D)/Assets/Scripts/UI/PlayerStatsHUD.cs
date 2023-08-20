using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the client player's statistics in the top-left corner of the screen.
/// </summary>
public sealed class PlayerStatsHUD : MonoBehaviour
{
    private Transform _xpBar;
    private Transform _healthBar;
    private Transform _staminaBar;
     
    private Transform _xpText;
    private Transform _healthText;
    private Transform _staminaText;

    private const float XPBarWidth = 8.9F; // width of the XP bar in screen space
    private const float CombatBarWidth = 11F; // width of the HP and stamina bars in screen space

    public void Start()
    {
        _xpBar = gameObject.transform.Find("XP Bar");
        _healthBar = gameObject.transform.Find("Health Bar");
        _staminaBar = gameObject.transform.Find("Stamina Bar");

        _xpText = gameObject.transform.Find("XP Text").transform.Find("Text");
        _healthText = gameObject.transform.Find("Health Text").transform.Find("Text");
        _staminaText = gameObject.transform.Find("Stamina Text").transform.Find("Text");
    }

    /// <summary>
    /// Updates the client player's experience level.
    /// </summary>
    public void UpdateLevel(int level)
    {
        gameObject.transform.Find("Player Level").transform.Find("Text").GetComponent<Text>().text = level.ToString();
    }
    
    /// <summary>
    /// Updates the client player's experience points.
    /// </summary>
    public void UpdateXP(int currentXP, int maxXP)
    {
        _xpBar.localScale = new Vector3(XPBarWidth * ((float)currentXP) / ((float)maxXP), 1F, 1F);
        _xpText.GetComponent<Text>().text = currentXP.ToString() + "/" + maxXP.ToString();
    }

    /// <summary>
    /// Updates the client player's health information.
    /// Does NOT validate <paramref name="currentHealth"/> and <paramref name="maxHealth"/>.
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        _healthBar.localScale = new Vector3(CombatBarWidth * ((float)currentHealth) / ((float)maxHealth), 1F, 1F);
        _healthText.GetComponent<Text>().text = currentHealth.ToString() + "/" + maxHealth.ToString();
    }

    /// <summary>
    /// Updates the client player's stamina information.
    /// Does NOT validate <paramref name="currentStamina"/> and <paramref name="maxStamina"/>.
    /// </summary>
    public void UpdateStamina(int currentStamina, int maxStamina)
    {
        _staminaBar.localScale = new Vector3(CombatBarWidth * ((float)currentStamina) / ((float)maxStamina), 1F, 1F);
        _staminaText.GetComponent<Text>().text = currentStamina.ToString() + "/" + maxStamina.ToString();
    }
}
