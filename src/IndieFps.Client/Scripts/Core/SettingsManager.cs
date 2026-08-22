using Godot;
using IndieFps.Client.Storage;
using System.Collections.Generic;

namespace IndieFps.Client.Core;

public partial class SettingsManager : Node
{
    private LocalDb _localDb = null!;
    private readonly Dictionary<string, Variant> _settingsCache = new();
    
    // Graphics
    public int ResolutionWidth { get; private set; } = 1920;
    public int ResolutionHeight { get; private set; } = 1080;
    public bool Fullscreen { get; private set; } = true;
    public int VSyncMode { get; private set; } = 1; // 0=Off, 1=On, 2=Adaptive
    public int TextureQuality { get; private set; } = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
    public int ShadowQuality { get; private set; } = 2;
    public int AntiAliasing { get; private set; } = 2; // 0=Off, 1=FXAA, 2=TAA, 3=MSAA2x, 4=MSAA4x
    public float RenderScale { get; private set; } = 1.0f;
    public bool MotionBlur { get; private set; } = true;
    public bool Bloom { get; private set; } = true;
    
    // Input
    public float MouseSensitivity { get; private set; } = 1.0f;
    public float ControllerSensitivity { get; private set; } = 1.0f;
    public bool InvertY { get; private set; } = false;
    public bool ToggleSprint { get; private set; } = false;
    public bool ToggleCrouch { get; private set; } = false;
    public bool ToggleAim { get; private set; } = true;
    
    // Audio
    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 0.8f;
    public float SfxVolume { get; private set; } = 1.0f;
    public float VoiceVolume { get; private set; } = 1.0f;
    
    // Gameplay
    public bool ShowFps { get; private set; } = false;
    public bool ShowDamageNumbers { get; private set; } = true;
    public bool AutoPickupAmmo { get; private set; } = true;
    public string Language { get; private set; } = "en";
    
    public event Action OnSettingsChanged;
    
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
        RenderingServer.SetVSyncMode((RenderingServer.VSyncMode)VSyncMode);
        
        // Render scale
        RenderingServer.SetRenderScaling3dMode(RenderingServer.RenderScaling3dMode.Bilinear);
        // Note: Render scale would need viewport setup
        
        // Audio
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(MasterVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), Mathf.LinearToDb(MusicVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), Mathf.LinearToDb(SfxVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), Mathf.LinearToDb(VoiceVolume));
        
        OnSettingsChanged?.Invoke();
    }
    
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