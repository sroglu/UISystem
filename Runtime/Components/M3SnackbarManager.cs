using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace mehmetsrl.UISystem.Components
{
    /// <summary>
    /// MonoBehaviour singleton that manages a queue of M3Snackbar instances.
    /// Shows one at a time; queues subsequent requests.
    ///
    /// Usage:
    ///   M3SnackbarManager.Instance.Show(root, "Message", "Undo", OnUndo);
    /// </summary>
    public class M3SnackbarManager : MonoBehaviour
    {
        public static M3SnackbarManager Instance { get; private set; }

        private readonly Queue<SnackbarRequest> _queue = new();
        private M3Snackbar _current;
        private VisualElement _root;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Register the root VisualElement to show snackbars in.</summary>
        public void SetRoot(VisualElement root) => _root = root;

        /// <summary>Enqueue a snackbar.</summary>
        public void Show(string text, string actionText = null, Action onAction = null, int durationMs = 4000)
        {
            _queue.Enqueue(new SnackbarRequest(text, actionText, onAction, durationMs));
            TryShowNext();
        }

        private void TryShowNext()
        {
            if (_current != null || _queue.Count == 0 || _root == null) return;

            var req = _queue.Dequeue();
            _current = new M3Snackbar
            {
                Text       = req.Text,
                ActionText = req.ActionText,
                DurationMs = req.DurationMs,
            };

            if (req.OnAction != null)
                _current.OnAction += req.OnAction;

            _current.OnDismissed += () =>
            {
                _current = null;
                TryShowNext();
            };

            _current.Show(_root);
        }

        private readonly struct SnackbarRequest
        {
            public readonly string Text;
            public readonly string ActionText;
            public readonly Action OnAction;
            public readonly int    DurationMs;

            public SnackbarRequest(string text, string actionText, Action onAction, int durationMs)
            {
                Text       = text;
                ActionText = actionText;
                OnAction   = onAction;
                DurationMs = durationMs;
            }
        }
    }
}
