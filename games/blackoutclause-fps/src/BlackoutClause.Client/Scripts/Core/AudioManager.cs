using System.Collections.Generic;
using Godot;

namespace BlackoutClause.Client.Core;

/// <summary>
/// Manages audio playback including music, 3D sound effects, and 2D UI sounds.
/// </summary>
public partial class AudioManager : Node
{
    private AudioStreamPlayer3D? _musicPlayer;
    private readonly Dictionary<string, AudioStream> _sfxCache = new();
    private readonly Dictionary<string, AudioStreamPlayer3D> _active3DSounds = new();
    private SettingsManager _settings = null!;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _settings = GetNode<SettingsManager>("/root/SettingsManager");
        _settings.OnSettingsChanged += OnSettingsChanged;

        // Create music player
        _musicPlayer = new AudioStreamPlayer3D();
        _musicPlayer.Bus = "Music";
        _musicPlayer.StreamPaused = true;
        AddChild(_musicPlayer);

        // Preload common SFX
        PreloadSfx("res://Audio/SFX/footstep_concrete.ogg", "footstep_concrete");
        PreloadSfx("res://Audio/SFX/footstep_metal.ogg", "footstep_metal");
        PreloadSfx("res://Audio/SFX/footstep_dirt.ogg", "footstep_dirt");
        PreloadSfx("res://Audio/SFX/weapon_reload.ogg", "reload");
        PreloadSfx("res://Audio/SFX/weapon_switch.ogg", "weapon_switch");
        PreloadSfx("res://Audio/SFX/hit_marker.ogg", "hit_marker");
        PreloadSfx("res://Audio/SFX/jump.ogg", "jump");
        PreloadSfx("res://Audio/SFX/land.ogg", "land");
    }

    private void OnSettingsChanged()
    {
        // Volume changes handled by SettingsManager directly on AudioServer buses
    }

    private void PreloadSfx(string path, string key)
    {
        if (ResourceLoader.Exists(path))
        {
            _sfxCache[key] = ResourceLoader.Load<AudioStream>(path);
        }
    }

    /// <summary>
    /// Plays background music with optional fade-in.
    /// </summary>
    /// <param name="resourcePath">Path to audio stream resource.</param>
    /// <param name="loop">Whether to loop the music.</param>
    /// <param name="fadeIn">Fade-in duration in seconds.</param>
    public void PlayMusic(string resourcePath, bool loop = true, float fadeIn = 1.0f)
    {
        if (_musicPlayer == null) return;

        var stream = ResourceLoader.Load<AudioStream>(resourcePath);
        if (stream == null) return;

        _musicPlayer.Stream = stream;
        _musicPlayer.StreamPaused = false;
        _musicPlayer.VolumeDb = 0; // Will be controlled by bus volume

        if (fadeIn > 0)
        {
            var tween = CreateTween();
            tween.TweenProperty(_musicPlayer, "volume_db", -80, 0).SetTrans(Tween.TransitionType.Linear);
        }
    }

    /// <summary>
    /// Stops background music with optional fade-out.
    /// </summary>
    /// <param name="fadeOut">Fade-out duration in seconds.</param>
    public void StopMusic(float fadeOut = 1.0f)
    {
        if (_musicPlayer == null) return;

        if (fadeOut > 0)
        {
            var tween = CreateTween();
            tween.TweenProperty(_musicPlayer, "volume_db", 0, -80).SetTrans(Tween.TransitionType.Linear);
            tween.TweenCallback(Callable.From(() => _musicPlayer!.StreamPaused = true));
        }
        else
        {
            _musicPlayer.StreamPaused = true;
        }
    }

    /// <summary>
    /// Plays a 3D sound effect at a specific position.
    /// </summary>
    /// <param name="key">Preloaded SFX key.</param>
    /// <param name="position">World position for 3D audio.</param>
    /// <param name="volumeDb">Volume adjustment in decibels.</param>
    /// <param name="pitchScale">Pitch multiplier.</param>
    /// <param name="randomPitch">Whether to apply slight random pitch variation.</param>
    public void PlaySfx(string key, Vector3 position, float volumeDb = 0, float pitchScale = 1.0f, bool randomPitch = false)
    {
        if (!_sfxCache.TryGetValue(key, out var stream))
        {
            GD.PushWarning($"SFX not found: {key}");
            return;
        }

        var player = new AudioStreamPlayer3D
        {
            Stream = stream,
            Bus = "SFX",
            VolumeDb = volumeDb,
            PitchScale = randomPitch ? pitchScale * (float)GD.RandRange(0.95, 1.05) : pitchScale,
            AttenuationFilterCutoffHz = 5000,
            Position = position
        };

        AddChild(player);
        player.Play();
        _active3DSounds[key + player.GetInstanceId()] = player;

        // Cleanup when finished
        player.Finished += () =>
        {
            _active3DSounds.Remove(key + player.GetInstanceId());
            player.QueueFree();
        };
    }

    /// <summary>
    /// Plays a 2D sound effect (UI sounds, no spatial positioning).
    /// </summary>
    /// <param name="key">Preloaded SFX key.</param>
    /// <param name="volumeDb">Volume adjustment in decibels.</param>
    /// <param name="pitchScale">Pitch multiplier.</param>
    public void PlaySfx2D(string key, float volumeDb = 0, float pitchScale = 1.0f)
    {
        if (!_sfxCache.TryGetValue(key, out var stream))
        {
            GD.PushWarning($"SFX not found: {key}");
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX",
            VolumeDb = volumeDb,
            PitchScale = pitchScale
        };

        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }

    /// <summary>
    /// Plays footstep sound based on surface type.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <param name="surfaceType">Surface type (concrete, metal, dirt).</param>
    public void PlayFootstep(Vector3 position, string surfaceType = "concrete")
    {
        var key = $"footstep_{surfaceType}";
        if (!_sfxCache.ContainsKey(key)) key = "footstep_concrete";

        PlaySfx(key, position, -6, 1.0f, true);
    }

    /// <summary>
    /// Plays weapon-related sound.
    /// </summary>
    /// <param name="weaponName">Weapon identifier.</param>
    /// <param name="action">Action type (fire, reload, switch).</param>
    /// <param name="position">World position.</param>
    public void PlayWeaponSound(string weaponName, string action, Vector3 position)
    {
        var key = $"{weaponName}_{action}";
        PlaySfx(key, position, 0, 1.0f);
    }

    /// <summary>
    /// Plays hit marker sound (2D UI sound).
    /// </summary>
    public void PlayHitMarker()
    {
        PlaySfx2D("hit_marker", -3);
    }

    /// <summary>
    /// Sets audio listener position (kept for compatibility).
    /// In Godot 4, the listener follows the AudioListener3D attached to the camera.
    /// </summary>
    /// <param name="position">Listener position.</param>
    /// <param name="rotation">Listener rotation basis.</param>
    public void SetListenerPosition(Vector3 position, Basis rotation)
    {
        // In Godot 4, audio listener is handled by AudioListener3D node
        // attached to the camera. This method is kept for compatibility.
        // The actual listener follows the camera automatically.
    }
}
