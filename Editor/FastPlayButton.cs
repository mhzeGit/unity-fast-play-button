#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using FastPlayButtonTool.Toolbar;

namespace FastPlayButtonTool
{
	/// <summary>
	/// Adds a "Fast Play" button to the Unity Editor toolbar (next to the default Play button).
	/// 
	/// Behavior:
	/// - Clicking the button temporarily enables Enter Play Mode Options (disabling Domain and Scene reload)
	///   and immediately enters Play Mode.
	/// - When exiting Play Mode (by any means), the original Enter Play Mode settings are automatically restored.
	/// - The default Play button always uses whatever settings the user has configured normally.
	/// - While fast-playing, the button turns into a green "Stop" button.
	/// - While playing normally (via the default Play button), the Fast Play button is disabled.
	/// </summary>
	[InitializeOnLoad]
	public static class FastPlayButton
	{
		// SessionState keys — persist across domain reloads within the same editor session
		private const string KEY_FAST_PLAYING = "FastPlayButton_IsFastPlaying";
		private const string KEY_ORIG_ENABLED = "FastPlayButton_OrigEnabled";
		private const string KEY_ORIG_OPTIONS = "FastPlayButton_OrigOptions";

		private static GUIContent _playContent;
		private static GUIContent _stopContent;
		private static GUIStyle _buttonStyle;

		static FastPlayButton()
		{
			ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);

			// Remove before adding to prevent duplicate registration when domain reload is disabled
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.quitting -= OnEditorQuitting;
			EditorApplication.quitting += OnEditorQuitting;

			// Safety net: if the editor was restarted/reloaded while fast-playing, restore settings
			if (!EditorApplication.isPlaying && SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				RestoreOriginalSettings();
			}
		}

		/// <summary>
		/// Additional safety net: restore settings after any script reload while not playing.
		/// </summary>
		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			if (!EditorApplication.isPlaying && SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				RestoreOriginalSettings();
			}
		}

		/// <summary>
		/// Restore settings if the editor is closed during a fast play session.
		/// </summary>
		private static void OnEditorQuitting()
		{
			RestoreOriginalSettings();
		}

		/// <summary>
		/// Draws the Fast Play / Stop button on the toolbar.
		/// </summary>
		static void OnToolbarGUI()
		{
			bool isFastPlaying = SessionState.GetBool(KEY_FAST_PLAYING, false);
			bool isPlaying = EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode;

			GUILayout.BeginVertical();
			AlignVertical();

			// Tint button green while fast-playing
			Color originalBg = GUI.backgroundColor;
			if (isFastPlaying && isPlaying)
				GUI.backgroundColor = new Color(0.35f, 0.9f, 0.35f, 1f);

			// Disable when already playing normally (not via fast play)
			bool disable = isPlaying && !isFastPlaying;
			EditorGUI.BeginDisabledGroup(disable);

			GUIContent content = (isFastPlaying && isPlaying) ? GetStopContent() : GetPlayContent();

			if (GUILayout.Button(content, GetButtonStyle()))
			{
				if (isFastPlaying && isPlaying)
				{
					// Stop: exiting play mode triggers OnPlayModeStateChanged which restores settings
					EditorApplication.isPlaying = false;
				}
				else if (!isPlaying)
				{
					EnterFastPlay();
				}
			}

			EditorGUI.EndDisabledGroup();
			GUI.backgroundColor = originalBg;

			GUILayout.EndVertical();
		}

		/// <summary>
		/// Saves current settings, enables fast play, and enters Play Mode.
		/// </summary>
		private static void EnterFastPlay()
		{
			// Preserve the user's current Enter Play Mode settings
			SessionState.SetBool(KEY_ORIG_ENABLED, EditorSettings.enterPlayModeOptionsEnabled);
			SessionState.SetInt(KEY_ORIG_OPTIONS, (int)EditorSettings.enterPlayModeOptions);

			// Enable fast play: skip both Domain and Scene reload
			EditorSettings.enterPlayModeOptionsEnabled = true;
			EditorSettings.enterPlayModeOptions =
				EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

			SessionState.SetBool(KEY_FAST_PLAYING, true);

			Debug.Log("<b>[Fast Play]</b> Entering Play Mode (Domain & Scene reload disabled).");

			// Enter play mode
			EditorApplication.isPlaying = true;
		}

		/// <summary>
		/// Restores the user's original Enter Play Mode settings.
		/// Idempotent — safe to call multiple times.
		/// </summary>
		private static void RestoreOriginalSettings()
		{
			// Guard: only restore if we actually changed settings
			if (!SessionState.GetBool(KEY_FAST_PLAYING, false))
				return;

			bool origEnabled = SessionState.GetBool(KEY_ORIG_ENABLED, false);
			EnterPlayModeOptions origOptions = (EnterPlayModeOptions)SessionState.GetInt(KEY_ORIG_OPTIONS, 0);

			// Clear flag first to prevent re-entrancy from other callbacks
			SessionState.SetBool(KEY_FAST_PLAYING, false);

			EditorSettings.enterPlayModeOptionsEnabled = origEnabled;
			EditorSettings.enterPlayModeOptions = origOptions;

			Debug.Log($"<b>[Fast Play]</b> Settings restored (enterPlayModeOptionsEnabled={origEnabled}, options={origOptions}).");
		}

		/// <summary>
		/// Automatically restores settings when exiting Play Mode after a fast play session.
		/// Hooks both ExitingPlayMode (early, before any potential domain reload) and
		/// EnteredEditMode (final confirmation) for maximum reliability.
		/// </summary>
		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				// Restore as early as possible — before any domain reload can interfere
				RestoreOriginalSettings();
			}
			else if (state == PlayModeStateChange.EnteredEditMode)
			{
				// Final safety: restore again in case ExitingPlayMode was missed
				RestoreOriginalSettings();

				// Deferred verification — catches edge cases where EditorSettings
				// are overwritten after EnteredEditMode fires
				EditorApplication.delayCall += VerifySettingsRestored;
			}
		}

		/// <summary>
		/// Deferred check to ensure settings were truly restored after all editor callbacks finish.
		/// </summary>
		private static void VerifySettingsRestored()
		{
			if (SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				Debug.LogWarning("<b>[Fast Play]</b> Settings were not properly restored — forcing restoration now.");
				RestoreOriginalSettings();
			}
		}

		// ─────────────────────────────── UI Helpers ───────────────────────────────

		private static GUIContent GetPlayContent()
		{
			if (_playContent == null)
			{
				Texture icon = LoadIcon("d_PlayButton", "PlayButton", "Animation.Play");

				string tooltip = "Fast Play\nEnters Play Mode with Domain & Scene reload disabled.\nOriginal settings are restored automatically on exit.";
				_playContent = icon != null
					? new GUIContent(" Fast", icon, tooltip)
					: new GUIContent("▶ Fast", tooltip);
			}
			return _playContent;
		}

		private static GUIContent GetStopContent()
		{
			if (_stopContent == null)
			{
				// Try the active play icon first, then fall back to normal play icon
				Texture icon = LoadIcon("d_PlayButton On", "PlayButton On", "d_PlayButton", "PlayButton");

				string tooltip = "Stop Fast Play\nOriginal Enter Play Mode settings will be restored.";
				_stopContent = icon != null
					? new GUIContent(" Stop", icon, tooltip)
					: new GUIContent("■ Stop", tooltip);
			}
			return _stopContent;
		}

		private static Texture LoadIcon(params string[] names)
		{
			foreach (string name in names)
			{
				try
				{
					GUIContent content = EditorGUIUtility.IconContent(name);
					if (content?.image != null)
						return content.image;
				}
				catch
				{
					// Icon not found — try next
				}
			}
			return null;
		}

		private static GUIStyle GetButtonStyle()
		{
			if (_buttonStyle == null)
			{
				try
				{
					// "Command" matches the play/pause/step button appearance
					_buttonStyle = new GUIStyle("Command")
					{
						fontSize = 11,
						alignment = TextAnchor.MiddleCenter,
						imagePosition = ImagePosition.ImageLeft,
						fontStyle = FontStyle.Bold,
						fixedWidth = 0,
						padding = new RectOffset(4, 6, 0, 0)
					};
				}
				catch
				{
					// Fallback to standard toolbar button
					_buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
					{
						fontStyle = FontStyle.Bold,
						padding = new RectOffset(4, 6, 2, 2)
					};
				}
			}
			return _buttonStyle;
		}

		private static void AlignVertical()
		{
#if UNITY_6000_0_OR_NEWER
			// No extra spacing needed in Unity 6
#else
			GUILayout.Space(2);
#endif
		}
	}
}
#endif
