using Godot;
using System;

namespace IndieFps.Client.Core;

public partial class Global : Node
{
    public static Global Instance { get; private set; } = null!;
    
    public enum GameState { MainMenu, Loading, Playing, Paused, Settings }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    
    public event Action<GameState> OnStateChanged;
    
    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }
    
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        var oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        
        GD.Print($"Game state changed: {oldState} -> {newState}");
    }
    
    public void QuitGame()
    {
        GetTree().Quit();
    }
}