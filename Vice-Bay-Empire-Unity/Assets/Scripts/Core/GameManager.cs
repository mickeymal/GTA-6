using UnityEngine;

namespace ViceBayEmpire.Core
{
    public enum GameState { MainMenu, Playing, Paused, Cutscene, Dead, Endgame }

    /// <summary>
    /// Root singleton. Owns global game state, spawns/holds references to the
    /// persistent managers, and drives the pause/story-complete flow.
    /// Place one on a bootstrap GameObject in the first scene.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Persistent Managers (auto-found if left empty)")]
        public EconomyManager economy;
        public WorldClock clock;

        [Header("Story")]
        public bool storyComplete;                 // unlocks full sandbox / endgame
        public int mainMissionIndex;

        public GameState State { get; private set; } = GameState.MainMenu;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (economy == null) economy = GetComponentInChildren<EconomyManager>();
            if (clock == null) clock = GetComponentInChildren<WorldClock>();
        }

        void OnEnable() => GameEvents.PlayerDied += HandlePlayerDied;
        void OnDisable() => GameEvents.PlayerDied -= HandlePlayerDied;

        public void SetState(GameState state)
        {
            State = state;
            Time.timeScale = (state == GameState.Paused || state == GameState.MainMenu) ? 0f : 1f;
        }

        public void StartNewGame()
        {
            storyComplete = false;
            mainMissionIndex = 0;
            economy.ResetTo(2500, 0);
            SetState(GameState.Playing);
            GameEvents.RaiseNotify("Welcome to Vice Bay.", NotifyType.Info);
        }

        public void CompleteStory()
        {
            storyComplete = true;
            SetState(GameState.Endgame);
            GameEvents.RaiseNotify("The city is yours. Build your empire.", NotifyType.Success);
        }

        void HandlePlayerDied()
        {
            SetState(GameState.Dead);
            // respawn handled by PlayerHealth after a delay; drop some cash as a penalty
            economy.SpendUpTo((long)(economy.Cash * 0.1f));
        }

        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.Endgame) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(storyComplete ? GameState.Endgame : GameState.Playing);
        }
    }
}
