using UnityEngine;
using UnityEngine.SceneManagement;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.UI
{
    /// <summary>
    /// Main menu controller. Wire buttons to these methods (New Game / Continue / Quit).
    /// New Game loads the city scene and starts a fresh economy; Continue restores a save.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public string citySceneName = "ViceBay";
        public GameObject continueButton;

        void Start()
        {
            if (continueButton) continueButton.SetActive(SaveSystem.SaveExists(0));
            if (GameManager.Instance) GameManager.Instance.SetState(GameState.MainMenu);
        }

        public void OnNewGame()
        {
            SceneManager.LoadScene(citySceneName);
            // GameManager.StartNewGame() should be called from a scene bootstrap after load
        }

        public void OnContinue()
        {
            SceneManager.LoadScene(citySceneName);
            // SaveCoordinator.Load() from scene bootstrap after load
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
