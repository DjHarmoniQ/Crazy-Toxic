using UnityEngine;

/// <summary>
/// Gunslinger — Ranger class.
/// <para><b>Passive — Quickdraw:</b> The first shot of each wave has zero cooldown.</para>
/// <para><b>Ultimate — Bullet Hell:</b> 360° bullet spray that lasts for 3 seconds.</para>
/// </summary>
public class CharGunslinger : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Quickdraw")]
    [Tooltip("When true, the next shot will bypass its fire-rate cooldown (first shot of a wave).")]
    [SerializeField] private bool _quickdrawReady = true;

    [Header("Ultimate — Bullet Hell")]
    [Tooltip("Duration in seconds of the Bullet Hell 360° spray.")]
    [SerializeField] private float _bulletHellDuration = 3f;

    [Tooltip("Number of bullets fired per second during Bullet Hell.")]
    [SerializeField] private float _bulletHellFireRate = 20f;

    [Tooltip("Projectile prefab used for each Bullet Hell bullet.")]
    [SerializeField] private GameObject _bulletPrefab;

    [Tooltip("Mana required to activate Bullet Hell.")]
    [SerializeField] private float _ultimateCost = 60f;

    [Tooltip("Cooldown in seconds after Bullet Hell ends.")]
    [SerializeField] private float _ultimateCooldown = 22f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _bulletHellTimer;
    private float _bulletHellFireTimer;
    private float _bulletAngle;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the Quickdraw first-shot bonus is available.
    /// <see cref="GunController"/> should zero its fire-rate cooldown when this is set
    /// and then clear it by calling <see cref="ConsumeQuickdraw"/>.
    /// </summary>
    public bool QuickdrawReady => _quickdrawReady;

    /// <summary><c>true</c> while Bullet Hell is actively spraying.</summary>
    public bool BulletHellActive => _bulletHellTimer > 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Quickdraw passive + Bullet Hell tick.
    /// Quickdraw is reset at the start of each wave (handled externally via
    /// <see cref="ResetQuickdraw"/>). Bullet Hell fires bullets each frame while active.
    /// </summary>
    public override void ApplyPassive()
    {
        // Tick and fire Bullet Hell
        if (_bulletHellTimer > 0f)
        {
            _bulletHellTimer -= Time.deltaTime;
            _bulletHellFireTimer -= Time.deltaTime;

            if (_bulletHellFireTimer <= 0f && _bulletPrefab != null)
            {
                _bulletHellFireTimer = 1f / _bulletHellFireRate;
                Vector3 dir = Quaternion.Euler(0f, 0f, _bulletAngle) * Vector3.right;
                Instantiate(_bulletPrefab, transform.position, Quaternion.LookRotation(Vector3.forward, dir));
                _bulletAngle += 360f / (_bulletHellFireRate * _bulletHellDuration);
            }
        }
    }

    /// <summary>
    /// Bullet Hell: starts the 360° spray timer.
    /// </summary>
    public override void ActivateUltimate()
    {
        _bulletHellTimer = _bulletHellDuration;
        _bulletHellFireTimer = 0f;
        _bulletAngle = 0f;
        Debug.Log($"[CharGunslinger] Bullet Hell active for {_bulletHellDuration}s!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Consumes the Quickdraw bonus after the first shot of a wave.
    /// Call from <see cref="GunController"/> after the zero-cooldown shot fires.
    /// </summary>
    public void ConsumeQuickdraw()
    {
        _quickdrawReady = false;
    }

    /// <summary>
    /// Restores the Quickdraw bonus at the start of a new wave.
    /// Call from <see cref="WaveManager"/> when a wave begins.
    /// </summary>
    public void ResetQuickdraw()
    {
        _quickdrawReady = true;
        Debug.Log("[CharGunslinger] Quickdraw ready.");
    }
}
