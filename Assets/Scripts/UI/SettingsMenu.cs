using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the in-game settings menu.  Reads and writes slider / toggle / dropdown
/// values in real-time and persists them to <see cref="PlayerPrefs"/>.
///
/// Volume changes are forwarded live to <see cref="SFXManager"/> and
/// <see cref="MusicSystem"/>.
///
/// Attach to: The Settings panel GameObject inside your UI Canvas.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  PlayerPrefs Keys
    // ─────────────────────────────────────────────────────────────────────────

    private const string KeyMasterVolume = "Settings_MasterVolume";
    private const string KeySFXVolume    = "Settings_SFXVolume";
    private const string KeyMusicVolume  = "Settings_MusicVolume";
    private const string KeyFullscreen   = "Settings_Fullscreen";
    private const string KeyResolution   = "Settings_ResolutionIndex";

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Volume Sliders")]
    [Tooltip("Slider that controls the master volume (0–1).")]
    [SerializeField] private Slider masterVolumeSlider;

    [Tooltip("Slider that controls the SFX volume (0–1).")]
    [SerializeField] private Slider sfxVolumeSlider;

    [Tooltip("Slider that controls the music volume (0–1).")]
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Display Settings")]
    [Tooltip("Toggle that switches between fullscreen and windowed mode.")]
    [SerializeField] private Toggle fullscreenToggle;

    [Tooltip("Dropdown populated with the available screen resolutions.")]
    [SerializeField] private Dropdown resolutionDropdown;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Resolution[] _resolutions;
    private int _currentResolutionIndex;
    private bool _initialising; // guard to prevent callbacks firing during Awake init

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _initialising = true;

        LoadSettings();
        PopulateResolutionDropdown();

        _initialising = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI Callbacks (wired up in Inspector or via AddListener)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the master volume <see cref="Slider"/> OnValueChanged event.
    /// Applies the value immediately and saves it to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">New slider value [0, 1].</param>
    public void OnMasterVolumeChanged(float value)
    {
        if (_initialising) return;
        PlayerPrefs.SetFloat(KeyMasterVolume, value);
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetMasterVolume(value);
    }

    /// <summary>
    /// Called by the SFX volume <see cref="Slider"/> OnValueChanged event.
    /// Applies the value immediately and saves it to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">New slider value [0, 1].</param>
    public void OnSFXVolumeChanged(float value)
    {
        if (_initialising) return;
        PlayerPrefs.SetFloat(KeySFXVolume, value);
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetSFXVolume(value);
    }

    /// <summary>
    /// Called by the music volume <see cref="Slider"/> OnValueChanged event.
    /// Applies the value immediately and saves it to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="value">New slider value [0, 1].</param>
    public void OnMusicVolumeChanged(float value)
    {
        if (_initialising) return;
        PlayerPrefs.SetFloat(KeyMusicVolume, value);
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetMusicVolume(value);
        if (MusicSystem.Instance != null)
            MusicSystem.Instance.SetMusicVolume(value);
    }

    /// <summary>
    /// Called by the fullscreen <see cref="Toggle"/> OnValueChanged event.
    /// Applies the mode immediately and saves it to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="isFullscreen"><c>true</c> for fullscreen, <c>false</c> for windowed.</param>
    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        if (_initialising) return;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(KeyFullscreen, isFullscreen ? 1 : 0);
    }

    /// <summary>
    /// Called by the resolution <see cref="Dropdown"/> OnValueChanged event.
    /// Applies the selected resolution immediately and saves the index to <see cref="PlayerPrefs"/>.
    /// </summary>
    /// <param name="index">Index into the <see cref="_resolutions"/> array.</param>
    public void OnResolutionChanged(int index)
    {
        if (_initialising) return;
        if (_resolutions == null || index < 0 || index >= _resolutions.Length) return;

        _currentResolutionIndex = index;
        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(KeyResolution, index);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads persisted settings from <see cref="PlayerPrefs"/> and applies them to
    /// the UI controls and the audio systems.
    /// </summary>
    private void LoadSettings()
    {
        // Volume sliders
        float master = PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
        float sfx    = PlayerPrefs.GetFloat(KeySFXVolume,    1f);
        float music  = PlayerPrefs.GetFloat(KeyMusicVolume,  0.7f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfx;
        if (musicVolumeSlider  != null) musicVolumeSlider.value  = music;

        // Apply to audio systems immediately
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.SetMasterVolume(master);
            SFXManager.Instance.SetSFXVolume(sfx);
            SFXManager.Instance.SetMusicVolume(music);
        }
        if (MusicSystem.Instance != null)
            MusicSystem.Instance.SetMusicVolume(music);

        // Fullscreen toggle
        bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
    }

    /// <summary>
    /// Fills the resolution <see cref="Dropdown"/> with all available resolutions
    /// and selects the last-saved (or current) one.
    /// </summary>
    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        _resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int savedIndex = PlayerPrefs.GetInt(KeyResolution, -1);
        var options = new List<Dropdown.OptionData>();

        for (int i = 0; i < _resolutions.Length; i++)
        {
            Resolution res = _resolutions[i];
            options.Add(new Dropdown.OptionData($"{res.width} × {res.height} @ {res.refreshRate}Hz"));

            // Match against current screen resolution if no saved preference
            if (savedIndex < 0 &&
                res.width  == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
            {
                savedIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        _currentResolutionIndex = Mathf.Clamp(savedIndex, 0, _resolutions.Length - 1);
        resolutionDropdown.value          = _currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }
}
