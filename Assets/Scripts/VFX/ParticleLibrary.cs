using System.Collections;
using UnityEngine;

/// <summary>
/// Extends <see cref="HitEffectManager"/> with a library of gameplay-specific
/// pooled particle effects: level-up bursts, card pickups, enemy deaths, healing,
/// mana pickups, ultimates, and combo milestones.
///
/// Singleton – access via <see cref="Instance"/>.
/// Attach to: the same "Effects" GameObject that hosts <see cref="HitEffectManager"/>,
/// or a dedicated child GameObject.
/// </summary>
public class ParticleLibrary : HitEffectManager
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The scene-global <see cref="ParticleLibrary"/> instance.</summary>
    public static new ParticleLibrary Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector – Prefabs
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Level-Up Effect")]
    [Tooltip("Golden burst particle prefab played on level-up.")]
    [SerializeField] private ParticleSystem levelUpBurstPrefab;
    [Tooltip("Star particle prefab played alongside the level-up burst.")]
    [SerializeField] private ParticleSystem levelUpStarPrefab;

    [Header("Card Pickup Effect")]
    [Tooltip("Sparkle trail particle prefab tinted to the card's rarity colour.")]
    [SerializeField] private ParticleSystem cardPickupPrefab;

    [Header("Enemy Death Effect")]
    [Tooltip("Explosion burst particle prefab tinted to the enemy's colour.")]
    [SerializeField] private ParticleSystem deathBurstPrefab;

    [Header("Boss Death Effect")]
    [Tooltip("Large shockwave particle prefab for boss-death sequence.")]
    [SerializeField] private ParticleSystem bossDeathPrefab;

    [Header("Heal Effect")]
    [Tooltip("Green crosses rising particle prefab played on heal.")]
    [SerializeField] private ParticleSystem healPrefab;

    [Header("Mana Pickup Effect")]
    [Tooltip("Blue orb burst particle prefab played on mana pickup.")]
    [SerializeField] private ParticleSystem manaPickupPrefab;

    [Header("Ultimate Effect")]
    [Tooltip("Shockwave ring particle prefab tinted to the character's colour.")]
    [SerializeField] private ParticleSystem ultimatePrefab;

    [Header("Combo Milestone Effect")]
    [Tooltip("Text/burst particle prefab played at combo milestones (x10 / x20 / x50).")]
    [SerializeField] private ParticleSystem comboMilestonePrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector – Pool Sizes
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Pool Sizes")]
    [Tooltip("Pool size for the level-up burst effect.")]
    [SerializeField] private int levelUpPoolSize = 4;
    [Tooltip("Pool size for the card-pickup sparkle effect.")]
    [SerializeField] private int cardPickupPoolSize = 8;
    [Tooltip("Pool size for the enemy-death explosion effect.")]
    [SerializeField] private int deathPoolSize = 16;
    [Tooltip("Pool size for the boss-death sequence effect.")]
    [SerializeField] private int bossDeathPoolSize = 2;
    [Tooltip("Pool size for the heal crosses effect.")]
    [SerializeField] private int healPoolSize = 8;
    [Tooltip("Pool size for the mana orb burst effect.")]
    [SerializeField] private int manaPoolSize = 8;
    [Tooltip("Pool size for the ultimate shockwave effect.")]
    [SerializeField] private int ultimatePoolSize = 4;
    [Tooltip("Pool size for the combo milestone text particle.")]
    [SerializeField] private int comboPoolSize = 6;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Pools
    // ─────────────────────────────────────────────────────────────────────────

    private ParticleSystem[] _levelUpBurstPool;
    private ParticleSystem[] _levelUpStarPool;
    private ParticleSystem[] _cardPickupPool;
    private ParticleSystem[] _deathPool;
    private ParticleSystem[] _bossDeathPool;
    private ParticleSystem[] _healPool;
    private ParticleSystem[] _manaPool;
    private ParticleSystem[] _ultimatePool;
    private ParticleSystem[] _comboPool;

    private int _levelUpBurstIdx;
    private int _levelUpStarIdx;
    private int _cardPickupIdx;
    private int _deathIdx;
    private int _bossDeathIdx;
    private int _healIdx;
    private int _manaIdx;
    private int _ultimateIdx;
    private int _comboIdx;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        base.Awake(); // initialise HitEffectManager pools

        _levelUpBurstPool = BuildLibraryPool(levelUpBurstPrefab, levelUpPoolSize);
        _levelUpStarPool  = BuildLibraryPool(levelUpStarPrefab,  levelUpPoolSize);
        _cardPickupPool   = BuildLibraryPool(cardPickupPrefab,   cardPickupPoolSize);
        _deathPool        = BuildLibraryPool(deathBurstPrefab,   deathPoolSize);
        _bossDeathPool    = BuildLibraryPool(bossDeathPrefab,    bossDeathPoolSize);
        _healPool         = BuildLibraryPool(healPrefab,         healPoolSize);
        _manaPool         = BuildLibraryPool(manaPickupPrefab,   manaPoolSize);
        _ultimatePool     = BuildLibraryPool(ultimatePrefab,     ultimatePoolSize);
        _comboPool        = BuildLibraryPool(comboMilestonePrefab, comboPoolSize);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays a golden burst and star particle shower at <paramref name="position"/>
    /// to celebrate a level-up event.
    /// </summary>
    /// <param name="position">World-space position to spawn the effect.</param>
    public void PlayLevelUpEffect(Vector2 position)
    {
        PlayLibraryEffect(_levelUpBurstPool, ref _levelUpBurstIdx, position, new Color(1f, 0.84f, 0f), 1.5f);
        PlayLibraryEffect(_levelUpStarPool,  ref _levelUpStarIdx,  position, Color.yellow, 1f);
    }

    /// <summary>
    /// Plays a coloured sparkle trail to indicate a card has been picked up.
    /// </summary>
    /// <param name="position">World-space position to spawn the effect.</param>
    /// <param name="rarityColor">Colour that matches the card's rarity tier.</param>
    public void PlayCardPickupEffect(Vector2 position, Color rarityColor)
    {
        PlayLibraryEffect(_cardPickupPool, ref _cardPickupIdx, position, rarityColor, 1f);
    }

    /// <summary>
    /// Plays a coloured explosion burst when a regular enemy dies.
    /// </summary>
    /// <param name="position">World-space position of the dead enemy.</param>
    /// <param name="enemyColor">The enemy's sprite colour used to tint the burst.</param>
    public void PlayDeathEffect(Vector2 position, Color enemyColor)
    {
        PlayLibraryEffect(_deathPool, ref _deathIdx, position, enemyColor, 1f);
    }

    /// <summary>
    /// Plays a massive screen-filling explosion sequence when a boss is defeated.
    /// Spawns several offset bursts over a short coroutine to fill the screen.
    /// </summary>
    /// <param name="position">World-space position of the boss.</param>
    public void PlayBossDeathEffect(Vector2 position)
    {
        StartCoroutine(BossDeathSequence(position));
    }

    /// <summary>
    /// Plays green crosses rising from the heal position.
    /// </summary>
    /// <param name="position">World-space position to spawn the effect.</param>
    public void PlayHealEffect(Vector2 position)
    {
        PlayLibraryEffect(_healPool, ref _healIdx, position, new Color(0.2f, 1f, 0.4f), 1f);
    }

    /// <summary>
    /// Plays a blue orb burst at the mana pickup location.
    /// </summary>
    /// <param name="position">World-space position to spawn the effect.</param>
    public void PlayManaPickupEffect(Vector2 position)
    {
        PlayLibraryEffect(_manaPool, ref _manaIdx, position, new Color(0.2f, 0.5f, 1f), 1f);
    }

    /// <summary>
    /// Plays a coloured shockwave ring when the player's ultimate ability fires.
    /// </summary>
    /// <param name="position">World-space position (player centre).</param>
    /// <param name="characterColor">The selected character's primary colour.</param>
    public void PlayUltimateEffect(Vector2 position, Color characterColor)
    {
        PlayLibraryEffect(_ultimatePool, ref _ultimateIdx, position, characterColor, 2f);
    }

    /// <summary>
    /// Plays a combo milestone text particle scaled to the combo count.
    /// Triggers at x10 / x20 / x50 thresholds.
    /// </summary>
    /// <param name="combo">Current combo count; determines the scale-up factor.</param>
    public void PlayComboMilestoneEffect(int combo)
    {
        Vector2 screenCentre = Camera.main != null
            ? Camera.main.transform.position
            : Vector2.zero;

        float scale = combo >= 50 ? 2.5f : combo >= 20 ? 1.8f : 1.3f;
        Color color = combo >= 50 ? Color.red : combo >= 20 ? new Color(1f, 0.5f, 0f) : Color.yellow;

        PlayLibraryEffect(_comboPool, ref _comboIdx, screenCentre, color, scale);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Allocates and pre-warms a round-robin pool specifically for
    /// <see cref="ParticleLibrary"/> effects (separate from the base-class helper
    /// which is private).
    /// </summary>
    private ParticleSystem[] BuildLibraryPool(ParticleSystem prefab, int size)
    {
        if (prefab == null)
            return new ParticleSystem[0];

        var pool = new ParticleSystem[size];
        for (int i = 0; i < size; i++)
        {
            var ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            pool[i] = ps;
        }
        return pool;
    }

    /// <summary>
    /// Retrieves the next pooled instance in round-robin order, repositions,
    /// tints, scales, and plays it.
    /// </summary>
    private void PlayLibraryEffect(ParticleSystem[] pool, ref int index,
                                   Vector2 position, Color color, float scale)
    {
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning("[ParticleLibrary] Effect pool is empty or prefab not assigned.");
            return;
        }

        ParticleSystem ps = pool[index];
        index = (index + 1) % pool.Length;
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(true);
        ps.transform.position = position;
        ps.transform.localScale = Vector3.one * scale;

        var main = ps.main;
        main.startColor = color;

        ps.Play();
    }

    /// <summary>
    /// Coroutine that spawns several offset bursts over 0.6 s to produce a
    /// screen-filling explosion sequence for boss deaths.
    /// </summary>
    private IEnumerator BossDeathSequence(Vector2 origin)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            float scale = Random.Range(1.5f, 3.5f);
            PlayLibraryEffect(_bossDeathPool, ref _bossDeathIdx, origin + offset,
                              new Color(1f, 0.4f, 0f), scale);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
