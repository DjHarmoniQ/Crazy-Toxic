using UnityEngine;

/// <summary>
/// Displays a skull icon in the HUD corner during <see cref="ModeHardcore"/>.
/// Auto-shows/hides based on the active game mode.
/// </summary>
public class HardcoreBadgeUI : MonoBehaviour
{
    [Header("Badge")]
    [Tooltip("Root GameObject of the skull badge overlay. Shown only in Hardcore mode.")]
    [SerializeField] private GameObject _badgePanel;

    private void Start()
    {
        bool isHardcore = GameModeManager.Instance?.CurrentMode is ModeHardcore;
        if (_badgePanel != null)
            _badgePanel.SetActive(isHardcore);
    }
}
