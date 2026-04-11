public enum GameMode
{
    Solo,
    Partner,
    AI
}

public static class GameModeState
{
    public static GameMode SelectedMode { get; set; } = GameMode.Solo;
}
