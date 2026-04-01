using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages player-2 spawning and the downed/revive mechanic for Co-Op mode.
///
/// Attach alongside <see cref="ModeCoOp"/> on the GameModeManager prefab.
/// Wire <see cref="player2Prefab"/> and <see cref="player2SpawnPoint"/> in the Inspector.
/// </summary>
public class CoOpPlayerManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Player 2 Spawn")]
    [Tooltip("Prefab used to instantiate Player 2 at the start of a Co-Op run.")]
    [SerializeField] private GameObject _player2Prefab;

    [Tooltip("World-space position where Player 2 spawns at the start of the run.")]
    [SerializeField] private Transform _player2SpawnPoint;

    [Header("Input")]
    [Tooltip("When true, Player 2 uses a gamepad instead of the keyboard arrow-keys scheme.")]
    [SerializeField] private bool _useController;

    [Header("Revival")]
    [Tooltip("Seconds the living player must stand near a downed player to revive them.")]
    [SerializeField] private float _reviveDuration = 3f;

    [Tooltip("World-space radius within which a living player can initiate a revival.")]
    [SerializeField] private float _reviveRadius = 1.5f;

    [Header("Revival UI")]
    [Tooltip("World-space canvas shown above a downed player. Must contain a Slider named ReviveSlider.")]
    [SerializeField] private GameObject _reviveProgressBarPrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime References
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The Player 2 GameObject spawned at run-start.</summary>
    public GameObject Player2Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Downed state per player index (0 = P1, 1 = P2).</summary>
    private readonly bool[] _downed = new bool[2];

    /// <summary>Active revive progress bars keyed by player index.</summary>
    private readonly GameObject[] _reviveBars = new GameObject[2];

    /// <summary>Accumulated revive timer per player index.</summary>
    private readonly float[] _reviveTimers = new float[2];

    /// <summary>Cached player transforms (index 0 = P1, index 1 = P2).</summary>
    private readonly Transform[] _playerTransforms = new Transform[2];

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns Player 2 at <see cref="_player2SpawnPoint"/> and caches both player transforms.
    /// Called by <see cref="ModeCoOp.OnModeStart"/>.
    /// </summary>
    public void SpawnPlayer2()
    {
        if (_player2Prefab == null)
        {
            Debug.LogWarning("[CoOpPlayerManager] Player 2 prefab not assigned.");
            return;
        }

        Vector3 spawnPos = _player2SpawnPoint != null
            ? _player2SpawnPoint.position
            : Vector3.zero;

        Player2Instance = Instantiate(_player2Prefab, spawnPos, Quaternion.identity);
        Debug.Log("[CoOpPlayerManager] Player 2 spawned.");

        // Cache both player transforms.
        GameObject p1 = GameObject.FindGameObjectWithTag("Player");
        if (p1 != null) _playerTransforms[0] = p1.transform;
        _playerTransforms[1] = Player2Instance.transform;
    }

    /// <summary>
    /// Returns <c>true</c> if the player at <paramref name="playerIndex"/> is currently downed.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public bool IsPlayerDowned(int playerIndex) => _downed[playerIndex];

    /// <summary>
    /// Marks a player as downed and shows the revival progress bar above them.
    /// Also notifies <see cref="ModeCoOp"/> of the downed event.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public void DownPlayer(int playerIndex)
    {
        if (_downed[playerIndex]) return;

        _downed[playerIndex] = true;
        _reviveTimers[playerIndex] = 0f;
        ShowReviveBar(playerIndex);

        // Notify the mode.
        if (GameModeManager.Instance?.CurrentMode is ModeCoOp coOp)
            coOp.NotifyPlayerDowned(playerIndex);

        Debug.Log($"[CoOpPlayerManager] Player {playerIndex + 1} downed.");
    }

    /// <summary>
    /// Fully revives the player at <paramref name="playerIndex"/>, hides the progress bar,
    /// and notifies <see cref="ModeCoOp"/>.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public void RevivePlayer(int playerIndex)
    {
        if (!_downed[playerIndex]) return;

        _downed[playerIndex] = false;
        _reviveTimers[playerIndex] = 0f;
        HideReviveBar(playerIndex);

        // Notify the mode.
        if (GameModeManager.Instance?.CurrentMode is ModeCoOp coOp)
            coOp.NotifyPlayerRevived(playerIndex);

        Debug.Log($"[CoOpPlayerManager] Player {playerIndex + 1} revived!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        for (int i = 0; i < 2; i++)
        {
            if (!_downed[i]) continue;

            int other = 1 - i;
            if (_playerTransforms[other] == null || _playerTransforms[i] == null) continue;

            float dist = Vector2.Distance(
                _playerTransforms[other].position,
                _playerTransforms[i].position);

            if (dist <= _reviveRadius)
            {
                _reviveTimers[i] += Time.deltaTime;
                UpdateReviveBar(i, _reviveTimers[i] / _reviveDuration);

                if (_reviveTimers[i] >= _reviveDuration)
                    RevivePlayer(i);
            }
            else
            {
                // Reset timer if the living player walks away.
                if (_reviveTimers[i] > 0f)
                {
                    _reviveTimers[i] = 0f;
                    UpdateReviveBar(i, 0f);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Spawns and positions the revival progress bar above <paramref name="playerIndex"/>.</summary>
    private void ShowReviveBar(int playerIndex)
    {
        if (_reviveProgressBarPrefab == null || _playerTransforms[playerIndex] == null) return;

        _reviveBars[playerIndex] = Instantiate(
            _reviveProgressBarPrefab,
            _playerTransforms[playerIndex].position + Vector3.up * 1.5f,
            Quaternion.identity);

        // Parent to the player so the bar follows them.
        _reviveBars[playerIndex].transform.SetParent(_playerTransforms[playerIndex], worldPositionStays: true);
        UpdateReviveBar(playerIndex, 0f);
    }

    /// <summary>Destroys the revival progress bar for <paramref name="playerIndex"/>.</summary>
    private void HideReviveBar(int playerIndex)
    {
        if (_reviveBars[playerIndex] != null)
        {
            Destroy(_reviveBars[playerIndex]);
            _reviveBars[playerIndex] = null;
        }
    }

    /// <summary>Sets the fill amount of the revival progress bar.</summary>
    private void UpdateReviveBar(int playerIndex, float progress)
    {
        if (_reviveBars[playerIndex] == null) return;

        Slider slider = _reviveBars[playerIndex].GetComponentInChildren<Slider>();
        if (slider != null)
            slider.value = Mathf.Clamp01(progress);
    }
}
