using System.Collections.Generic;
using BlackoutClause.Client.Storage;
using Godot;

namespace BlackoutClause.Client.Core;

/// <summary>
/// Manages game settings including graphics, input, audio, and gameplay options.
/// Persists settings to local SQLite database.
/// </summary>
public partial class SettingsManager : Node
{
    private LocalDb _localDb = null!;
    private readonly Dictionary<string, Variant> _settingsCache = new();

    // Graphics
    /// <summary>Gets or sets the screen resolution width.</summary>
    public int ResolutionWidth { get; private set; } = 1920;

    /// <summary>Gets or sets the screen resolution height.</summary>
    public int ResolutionHeight { get; private set; } = 1080;

    /// <summary>Gets or sets whether the game runs in fullscreen mode.</summary>
    public bool Fullscreen { get; private set; } = true;

    /// <summary>Gets or sets the VSync mode (0=Off, 1=On, 2=Adaptive).</summary>
    public int VSyncMode { get; private set; } = 1;

    /// <summary>Gets or sets the texture quality (0=Low, 1=Medium, 2=High, 3=Ultra).</summary>
    public int TextureQuality { get; private set; } = 2;

    /// <summary>Gets or sets the shadow quality (0=Low, 1=Medium, 2=High, 3=Ultra).</summary>
    public int ShadowQuality { get; private set; } = 2;

    /// <summary>Gets or sets the anti-aliasing mode (0=Off, 1=FXAA, 2=TAA, 3=MSAA2x, 4=MSAA4x).</summary>
    public int AntiAliasing { get; private set; } = 2;

    /// <summary>Gets or sets the render scale multiplier.</summary>
    public float RenderScale { get; private set; } = 1.0f;

    /// <summary>Gets or sets whether motion blur is enabled.</summary>
    public bool MotionBlur { get; private set; } = true;

    /// <summary>Gets or sets whether bloom is enabled.</summary>
    public bool Bloom { get; private set; } = true;

    // Input
    /// <summary>Gets or sets the mouse sensitivity multiplier.</summary>
    public float MouseSensitivity { get; private set; } = 1.0f;

    /// <summary>Gets or sets the controller sensitivity multiplier.</summary>
    public float ControllerSensitivity { get; private set; } = 1.0f;

    /// <summary>Gets or sets whether Y-axis is inverted.</summary>
    public bool InvertY { get; private set; } = false;

    /// <summary>Gets or sets whether sprint is toggled.</summary>
    public bool ToggleSprint { get; private set; } = false;

    /// <summary>Gets or sets whether crouch is toggled.</summary>
    public bool ToggleCrouch { get; private set; } = false;

    /// <summary>Gets or sets whether aim is toggled.</summary>
    public bool ToggleAim { get; private set; } = true;

    // Audio
    /// <summary>Gets or sets the master volume (0.0-1.0).</summary>
    public float MasterVolume { get; private set; } = 1.0f;

    /// <summary>Gets or sets the music volume (0.0-1.0).</summary>
    public float MusicVolume { get; private set; } = 0.8f;

    /// <summary>Gets or sets the SFX volume (0.0-1.0).</summary>
    public float SfxVolume { get; private set; } = 1.0f;

    /// <summary>Gets or sets the voice chat volume (0.0-1.0).</summary>
    public float VoiceVolume { get; private set; } = 1.0f;

    // Gameplay
    /// <summary>Gets or sets whether FPS counter is shown.</summary>
    public bool ShowFps { get; private set; } = false;

    /// <summary>Gets or sets whether damage numbers are shown.</summary>
    public bool ShowDamageNumbers { get; private set; } = true;

    /// <summary>Gets or sets whether ammo is auto-picked up.</summary>
    public bool AutoPickupAmmo { get; private set; } = true;

    /// <summary>Gets or sets the UI language code.</summary>
    public string Language { get; private set; } = "en";

    /// <summary>
    /// Fired when any setting changes.
    /// </summary>
    public event Action? OnSettingsChanged;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _localDb = GetNode<LocalDb>("/root/LocalDb");
        LoadSettings();
        ApplySettings();
    }

    private async void LoadSettings()
    {
        _settingsCache.Clear();

        var keys = new[]
        {
            "graphics.resolution_width", "graphics.resolution_height", "graphics.fullscreen", "graphics.vsync",
            "graphics.texture_quality", "graphics.shadow_quality", "graphics.anti_aliasing", "graphics.render_scale",
            "graphics.motion_blur", "graphics.bloom",
            "input.mouse_sensitivity", "input.controller_sensitivity", "input.invert_y",
            "input.toggle_sprint", "input.toggle_crouch", "input.toggle_aim",
            "audio.master_volume", "audio.music_volume", "audio.sfx_volume", "audio.voice_volume",
            "gameplay.show_fps", "gameplay.show_damage_numbers", "gameplay.auto_pickup_ammo", "gameplay.language"
        };

        foreach (var key in keys)
        {
            var value = await _localDb.GetSettingAsync(key);
            if (value != null)
            {
                _settingsCache[key] = value;
            }
        }

        // Apply to properties
        ResolutionWidth = GetInt("graphics.resolution_width", 1920);
        ResolutionHeight = GetInt("graphics.resolution_height", 1080);
        Fullscreen = GetBool("graphics.fullscreen", true);
        VSyncMode = GetInt("graphics.vsync", 1);
        TextureQuality = GetInt("graphics.texture_quality", 2);
        ShadowQuality = GetInt("graphics.shadow_quality", 2);
        AntiAliasing = GetInt("graphics.anti_aliasing", 2);
        RenderScale = GetFloat("graphics.render_scale", 1.0f);
        MotionBlur = GetBool("graphics.motion_blur", true);
        Bloom = GetBool("graphics.bloom", true);

        MouseSensitivity = GetFloat("input.mouse_sensitivity", 1.0f);
        ControllerSensitivity = GetFloat("input.controller_sensitivity", 1.0f);
        InvertY = GetBool("input.invert_y", false);
        ToggleSprint = GetBool("input.toggle_sprint", false);
        ToggleCrouch = GetBool("input.toggle_crouch", false);
        ToggleAim = GetBool("input.toggle_aim", true);

        MasterVolume = GetFloat("audio.master_volume", 1.0f);
        MusicVolume = GetFloat("audio.music_volume", 0.8f);
        SfxVolume = GetFloat("audio.sfx_volume", 1.0f);
        VoiceVolume = GetFloat("audio.voice_volume", 1.0f);

        ShowFps = GetBool("gameplay.show_fps", false);
        ShowDamageNumbers = GetBool("gameplay.show_damage_numbers", true);
        AutoPickupAmmo = GetBool("gameplay.auto_pickup_ammo", true);
        Language = GetString("gameplay.language", "en");
    }

    private void ApplySettings()
    {
        // Window
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(new Vector2I(ResolutionWidth, ResolutionHeight));

        // VSync
        DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)VSyncMode);

        // Render scale
        // Note: Render scale would need viewport setup

        // Audio
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(MasterVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb(MusicVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb(SfxVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), Mathf.LinearToDb(VoiceVolume));

        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Sets all graphics settings at once.
    /// </summary>
    /// <param name="width">Screen width.</param>
    /// <param name="height">Screen height.</param>
    /// <param name="fullscreen">Fullscreen mode.</param>
    /// <param name="vsync">VSync mode.</param>
    /// <param name="textureQuality">Texture quality level.</param>
    /// <param name="shadowQuality">Shadow quality level.</param>
    /// <param name="antiAliasing">Anti-aliasing mode.</param>
    /// <param name="renderScale">Render scale multiplier.</param>
    /// <param name="motionBlur">Enable motion blur.</param>
    /// <param name="bloom">Enable bloom.</param>
    public async Task SetGraphicsSettingsAsync(int width, int height, bool fullscreen, int vsync, int textureQuality,
        int shadowQuality, int antiAliasing, float renderScale, bool motionBlur, bool bloom)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        Fullscreen = fullscreen;
        VSyncMode = vsync;
        TextureQuality = textureQuality;
        ShadowQuality = shadowQuality;
        AntiAliasing = antiAliasing;
        RenderScale = renderScale;
        MotionBlur = motionBlur;
        Bloom = bloom;

        await SaveSettingAsync("graphics.resolution_width", width);
        await SaveSettingAsync("graphics.resolution_height", height);
        await SaveSettingAsync("graphics.fullscreen", fullscreen);
        await SaveSettingAsync("graphics.vsync", vsync);
        await SaveSettingAsync("graphics.texture_quality", textureQuality);
        await SaveSettingAsync("graphics.shadow_quality", shadowQuality);
        await SaveSettingAsync("graphics.anti_aliasing", antiAliasing);
        await SaveSettingAsync("graphics.render_scale", renderScale);
        await SaveSettingAsync("graphics.motion_blur", motionBlur);
        await SaveSettingAsync("graphics.bloom", bloom);

        ApplySettings();
    }

    /// <summary>
    /// Sets all input settings at once.
    /// </summary>
    /// <param name="mouseSens">Mouse sensitivity.</param>
    /// <param name="controllerSens">Controller sensitivity.</param>
    /// <param name="invertY">Invert Y axis.</param>
    /// <param name="toggleSprint">Toggle sprint mode.</param>
    /// <param name="toggleCrouch">Toggle crouch mode.</param>
    /// <param name="toggleAim">Toggle aim mode.</param>
    public async Task SetInputSettingsAsync(float mouseSens, float controllerSens, bool invertY, bool toggleSprint, bool toggleCrouch, bool toggleAim)
    {
        MouseSensitivity = mouseSens;
        ControllerSensitivity = controllerSens;
        InvertY = invertY;
        ToggleSprint = toggleSprint;
        ToggleCrouch = toggleCrouch;
        ToggleAim = toggleAim;

        await SaveSettingAsync("input.mouse_sensitivity", mouseSens);
        await SaveSettingAsync("input.controller_sensitivity", controllerSens);
        await SaveSettingAsync("input.invert_y", invertY);
        await SaveSettingAsync("input.toggle_sprint", toggleSprint);
        await SaveSettingAsync("input.toggle_crouch", toggleCrouch);
        await SaveSettingAsync("input.toggle_aim", toggleAim);

        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Sets all audio settings at once.
    /// </summary>
    /// <param name="master">Master volume.</param>
    /// <param name="music">Music volume.</param>
    /// <param name="sfx">SFX volume.</param>
    /// <param name="voice">Voice volume.</param>
    public async Task SetAudioSettingsAsync(float master, float music, float sfx, float voice)
    {
        MasterVolume = master;
        MusicVolume = music;
        SfxVolume = sfx;
        VoiceVolume = voice;

        await SaveSettingAsync("audio.master_volume", master);
        await SaveSettingAsync("audio.music_volume", music);
        await SaveSettingAsync("audio.sfx_volume", sfx);
        await SaveSettingAsync("audio.voice_volume", voice);

        ApplySettings();
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Sets all gameplay settings at once.
    /// </summary>
    /// <param name="showFps">Show FPS counter.</param>
    /// <param name="showDamageNumbers">Show damage numbers.</param>
    /// <param name="autoPickup">Auto-pickup ammo.</param>
    /// <param name="language">UI language code.</param>
    public async Task SetGameplaySettingsAsync(bool showFps, bool showDamageNumbers, bool autoPickup, string language)
    {
        ShowFps = showFps;
        ShowDamageNumbers = showDamageNumbers;
        AutoPickupAmmo = autoPickup;
        Language = language;

        await SaveSettingAsync("gameplay.show_fps", showFps);
        await SaveSettingAsync("gameplay.show_damage_numbers", showDamageNumbers);
        await SaveSettingAsync("gameplay.auto_pickup_ammo", autoPickup);
        await SaveSettingAsync("gameplay.language", language);

        OnSettingsChanged?.Invoke();
    }

    private async Task SaveSettingAsync(string key, Variant value)
    {
        _settingsCache[key] = value;
        await _localDb.SetSettingAsync(key, value.ToString());
    }

    private int GetInt(string key, int defaultValue)
    {
        return _settingsCache.TryGetValue(key, out var value) ? value.AsInt32() : defaultValue;
    }

    private float GetFloat(string key, float defaultValue)
    {
        return _settingsCache.TryGetValue(key, out var value) ? value.AsSingle() : defaultValue;
    }

    private bool GetBool(string key, bool defaultValue)
    {
        return _settingsCache.TryGetValue(key, out var value) ? value.AsBool() : defaultValue;
    }

    private string GetString(string key, string defaultValue)
    {
        return _settingsCache.TryGetValue(key, out var value) ? value.AsString() : defaultValue;
    }
}
