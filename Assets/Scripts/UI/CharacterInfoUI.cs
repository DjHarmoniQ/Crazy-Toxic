using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen character roster panel accessible from the Main Menu.
///
/// Behaviour:
/// <list type="bullet">
///   <item>Populates a grid with all 14 <see cref="CharacterData"/> entries.</item>
///   <item>Locked characters are shown as silhouettes with an unlock hint text.</item>
///   <item>Clicking an unlocked character opens a stat panel with animated bars,
///         passive / ultimate names, and a lore blurb.</item>
/// </list>
///
/// Attach to: The CharacterInfo full-screen panel in the main-menu Canvas.
/// </summary>
public class CharacterInfoUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Nested Types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Represents a single character slot in the grid.</summary>
    [System.Serializable]
    private class CharacterSlot
    {
        public Button      button;
        public Image       portrait;
        public TextMeshProUGUI nameLabelText;
        public GameObject  lockedOverlay;
        public TextMeshProUGUI unlockHintText;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Data")]
    [Tooltip("All 14 character data assets in roster order.")]
    [SerializeField] private CharacterData[] allCharacters;

    [Header("Grid")]
    [Tooltip("Parent transform of the character slot buttons.")]
    [SerializeField] private Transform gridContainer;

    [Tooltip("Prefab instantiated for each character slot in the grid.")]
    [SerializeField] private GameObject characterSlotPrefab;

    [Header("Stat Panel")]
    [Tooltip("Root panel that slides in when a character is selected.")]
    [SerializeField] private GameObject statPanel;

    [Tooltip("Large portrait shown in the stat panel.")]
    [SerializeField] private Image statPortrait;

    [Tooltip("Character name in the stat panel.")]
    [SerializeField] private TextMeshProUGUI statNameText;

    [Tooltip("Passive ability name and description.")]
    [SerializeField] private TextMeshProUGUI passiveText;

    [Tooltip("Ultimate ability name and description.")]
    [SerializeField] private TextMeshProUGUI ultimateText;

    [Tooltip("Lore / flavour text blurb.")]
    [SerializeField] private TextMeshProUGUI loreText;

    [Header("Stat Bars")]
    [Tooltip("Slider representing the character's HP stat (normalised 0–1).")]
    [SerializeField] private Slider hpBar;

    [Tooltip("Slider representing the character's Speed stat (normalised 0–1).")]
    [SerializeField] private Slider speedBar;

    [Tooltip("Slider representing the character's Damage stat (normalised 0–1).")]
    [SerializeField] private Slider damageBar;

    [Tooltip("Slider representing the character's Armour stat (normalised 0–1).")]
    [SerializeField] private Slider armorBar;

    [Header("Navigation")]
    [Tooltip("Button that closes this panel and returns to the main menu.")]
    [SerializeField] private Button closeButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    // Normalisation maxima — designer-tunable via private constants
    private const float MaxHP     = 300f;
    private const float MaxSpeed  = 10f;
    private const float MaxDamage = 50f;
    private const float MaxArmor  = 20f;

    private const float StatBarAnimDuration = 0.5f;

    private readonly List<CharacterSlot> _slots = new List<CharacterSlot>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeButton?.onClick.AddListener(() =>
        {
            statPanel?.SetActive(false);
            gameObject.SetActive(false);
        });
    }

    private void OnEnable()
    {
        statPanel?.SetActive(false);
        BuildGrid();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Grid Building
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Instantiates or refreshes all character slots in the grid.</summary>
    private void BuildGrid()
    {
        if (allCharacters == null || gridContainer == null || characterSlotPrefab == null) return;

        // Clear old slots
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);
        _slots.Clear();

        for (int i = 0; i < allCharacters.Length; i++)
        {
            CharacterData data = allCharacters[i];
            if (data == null) continue;

            GameObject go   = Instantiate(characterSlotPrefab, gridContainer);
            var slot        = new CharacterSlot
            {
                button         = go.GetComponentInChildren<Button>(),
                portrait       = go.transform.Find("Portrait")?.GetComponent<Image>(),
                nameLabelText  = go.transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>(),
                lockedOverlay  = go.transform.Find("LockedOverlay")?.gameObject,
                unlockHintText = go.transform.Find("LockedOverlay/UnlockHint")?.GetComponent<TextMeshProUGUI>()
            };

            bool unlocked = UnlockSystem.Instance == null || UnlockSystem.Instance.IsCharacterUnlocked(data.characterName);

            // Portrait: full colour if unlocked, silhouette-grey if locked
            if (slot.portrait != null)
            {
                slot.portrait.sprite = data.portrait;
                slot.portrait.color  = unlocked ? Color.white : Color.black;
            }
            if (slot.nameLabelText != null)
                slot.nameLabelText.text = unlocked ? data.characterName : "???";
            if (slot.lockedOverlay != null)
                slot.lockedOverlay.SetActive(!unlocked);
            if (slot.unlockHintText != null)
            {
                UnlockSystem.CharacterConditions.TryGetValue(data.characterName, out var cond);
                slot.unlockHintText.text = !string.IsNullOrEmpty(cond.description) ? cond.description : "???";
            }

            // Wire up click only for unlocked characters
            if (slot.button != null)
            {
                CharacterData capturedData = data;
                if (unlocked)
                    slot.button.onClick.AddListener(() => ShowStatPanel(capturedData));
                else
                    slot.button.interactable = false;
            }

            _slots.Add(slot);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Stat Panel
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates and opens the stat panel for <paramref name="data"/>.
    /// Stat bars animate from 0 to their target value.
    /// </summary>
    private void ShowStatPanel(CharacterData data)
    {
        if (statPanel == null) return;
        statPanel.SetActive(true);

        if (statPortrait != null) statPortrait.sprite = data.portrait;
        if (statNameText != null) statNameText.text   = data.characterName;
        if (passiveText  != null) passiveText.text    = $"Passive: {data.passiveName}";
        if (ultimateText != null) ultimateText.text   = $"Ultimate: {data.ultimateName}";
        if (loreText     != null) loreText.text       = data.loreBlurb;

        // Animate stat bars
        AnimateBar(hpBar,     data.maxHealth / MaxHP);
        AnimateBar(speedBar,  data.moveSpeed / MaxSpeed);
        AnimateBar(damageBar, data.damage    / MaxDamage);
        AnimateBar(armorBar,  data.armor     / MaxArmor);
    }

    /// <summary>Animates a slider from 0 to <paramref name="target"/> using a coroutine.</summary>
    private void AnimateBar(Slider bar, float target)
    {
        if (bar == null) return;
        bar.value = 0f;
        StartCoroutine(AnimateBarCoroutine(bar, Mathf.Clamp01(target)));
    }

    private System.Collections.IEnumerator AnimateBarCoroutine(Slider bar, float target)
    {
        float elapsed = 0f;
        while (elapsed < StatBarAnimDuration)
        {
            elapsed += Time.deltaTime;
            bar.value = Mathf.Lerp(0f, target, elapsed / StatBarAnimDuration);
            yield return null;
        }
        bar.value = target;
    }
}
