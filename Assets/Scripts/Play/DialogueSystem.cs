using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Branching dialogue with text + placeholder voice. Two modes:
    ///  • Say(): a one-off barked line (teller during a robbery, NPC reaction), shown
    ///    briefly with a synthesized voice blip.
    ///  • StartTree(): a full branching conversation rendered with OnGUI; number keys
    ///    or clicks choose options. Choices apply effects (reputation, flags, money,
    ///    cash, and callbacks) so story/Dark-Web deals can branch.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        // ---- barks ----
        string barkSpeaker, barkText;
        float barkUntil;

        // ---- trees ----
        public class Choice
        {
            public string label;
            public string nextNode;
            public Action onPick;
        }
        public class Node
        {
            public string speaker;
            public string text;
            public float voicePitch = 1f;
            public List<Choice> choices = new();
        }

        Dictionary<string, Node> tree;
        Node current;
        Action onTreeEnd;

        public bool TreeActive => current != null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // ---------------------------------------------------------------- barks
        public void Say(string speaker, string text, float pitch = 1f)
        {
            barkSpeaker = speaker; barkText = text; barkUntil = Time.unscaledTime + 3f;
            Audio.AudioManager.Instance?.SpeakPlaceholder(text, pitch);
            Debug.Log($"[VOICE:{speaker}] \"{text}\"");   // placeholder for a real voice line
        }

        // ---------------------------------------------------------------- trees
        public void StartTree(Dictionary<string, Node> nodes, string startNode, Action onEnd = null)
        {
            tree = nodes;
            current = nodes.TryGetValue(startNode, out var n) ? n : null;
            onTreeEnd = onEnd;
            if (current != null)
            {
                PlayRefs.UIBlocking = true;
                GameManager.Instance.SetState(GameState.Cutscene);
                Time.timeScale = 0f;
                SpeakCurrent();
            }
        }

        void SpeakCurrent()
        {
            if (current == null) return;
            Audio.AudioManager.Instance?.SpeakPlaceholder(current.text, current.voicePitch);
            Debug.Log($"[VOICE:{current.speaker}] \"{current.text}\"");
        }

        public void Pick(int index)
        {
            if (current == null || index < 0 || index >= current.choices.Count) return;
            var choice = current.choices[index];
            choice.onPick?.Invoke();
            if (!string.IsNullOrEmpty(choice.nextNode) && tree.TryGetValue(choice.nextNode, out var next))
            { current = next; SpeakCurrent(); }
            else EndTree();
        }

        void EndTree()
        {
            current = null;
            PlayRefs.UIBlocking = false;
            Time.timeScale = 1f;
            GameManager.Instance.SetState(GameManager.Instance.storyComplete ? GameState.Endgame : GameState.Playing);
            var cb = onTreeEnd; onTreeEnd = null; cb?.Invoke();
        }

        // ---------------------------------------------------------------- rendering
        void OnGUI()
        {
            // barks (small bottom-center caption)
            if (Time.unscaledTime < barkUntil && !TreeActive)
            {
                var style = new GUIStyle(GUI.skin.box) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleLeft };
                var rect = new Rect(Screen.width / 2f - 260, Screen.height - 150, 520, 54);
                GUI.Box(rect, $" {barkSpeaker}: \"{barkText}\"", style);
            }

            if (!TreeActive) return;

            // full conversation panel
            float w = 720, h = 90 + current.choices.Count * 34;
            var panel = new Rect(Screen.width / 2f - w / 2f, Screen.height - h - 40, w, h);
            GUI.Box(panel, GUIContent.none);
            var name = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            name.normal.textColor = new Color(1f, 0.4f, 0.7f);
            GUI.Label(new Rect(panel.x + 14, panel.y + 8, w - 28, 24), current.speaker, name);
            var body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            body.normal.textColor = Color.white;
            GUI.Label(new Rect(panel.x + 14, panel.y + 30, w - 28, 44), current.text, body);

            for (int i = 0; i < current.choices.Count; i++)
            {
                var r = new Rect(panel.x + 20, panel.y + 78 + i * 32, w - 40, 28);
                if (GUI.Button(r, $"{i + 1}. {current.choices[i].label}")) Pick(i);
            }
        }

        void Update()
        {
            if (!TreeActive) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Pick(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Pick(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Pick(2);
        }
    }
}
