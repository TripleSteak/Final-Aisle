using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Stores and loads in initial player statistics.
/// </summary>
public sealed class PlayerStats : MonoBehaviour
{
    // Level & Experience
    private int CurrentMainLevel { get; set; }
    private int CurrentMainExp { get; set; }

    // Health
    private int CurrentHealth { get; set; }
    private int MaxHealth { get; set; }

    // Stamina
    private int CurrentStamina { get; set; }
    private int MaxStamina { get; set; }
    
    private PlayerStatsHUD _playerStatsHUD;

    [SerializeField]
    private GameObject statBarsObject;

    public void Start()
    {
        _playerStatsHUD = statBarsObject.GetComponent<PlayerStatsHUD>();

        // TODO: Load player character stats from the server instead of using sample hard-coded values
        CurrentMainLevel = 20;
        CurrentMainExp = 37;
        CurrentHealth = 100;
        MaxHealth = 120;
        CurrentStamina = 50;
        MaxStamina = 120;

        // TODO: This should be done with a Unity Coroutine instead
        var updateStatsThread = new Thread(() => {
            Thread.Sleep(1000);
            
            UnityThread.ExecuteInUpdate(() =>
            {
                _playerStatsHUD.UpdateLevel(CurrentMainLevel);
                _playerStatsHUD.UpdateXP(CurrentMainExp, 100);
                _playerStatsHUD.UpdateHealth(CurrentHealth, MaxHealth);
                _playerStatsHUD.UpdateStamina(CurrentStamina, MaxStamina);
            });
        });
        updateStatsThread.Start();
    }
}
