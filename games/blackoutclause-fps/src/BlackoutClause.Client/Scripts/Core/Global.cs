using System;
using Godot;

namespace BlackoutClause.Client.Core;

/// <summary>
/// Global game state singleton for managing high-level game flow.
/// </summary>
public partial class Global : Node
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static Global Instance { get; private set; } = null!;

    /// <summary>
    /// Game state enumeration.
    /// </summary>
    public enum GameState
    {
        /// <summary>Main menu state.</summary>
        MainMenu,
        /// <summary>Loading screen state.</summary>
        Loading,
        /// <summary>Active gameplay state.</summary>
        Playing,
        /// <summary>Paused state.</summary>
        Paused,
        /// <summary>Settings menu state.</summary>
        Settings
    }

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>
    /// Fired when the game state changes.
    /// Event arg: newState (GameState).
    /// </summary>
    public event Action<GameState>? OnStateChanged;

    /// <inheritdoc/>
    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Changes the current game state.
    /// </summary>
    /// <param name="newState">The new game state.</param>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        var oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);

        GD.Print($"Game state changed: {oldState} -> {newState}");
    }

    /// <summary>
    /// Quits the game application.
    /// </summary>
    public void QuitGame()
    {
        GetTree().Quit();
    }
}
