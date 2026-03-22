#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using FastPlayButtonTool.Toolbar;

namespace FastPlayButtonTool
{
	/// <summary>
	/// Adds a "Fast Play" button to the toolbar that enters Play Mode with Domain and Scene reload disabled.
	/// Original settings are restored on exit.
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

			// Safety: if editor restarted while fast-playing, restore settings
			if (!EditorApplication.isPlaying && SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				RestoreOriginalSettings();
			}
		}

		/// <summary>Restore settings after script reload while not playing.</summary>
		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			if (!EditorApplication.isPlaying && SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				RestoreOriginalSettings();
			}
		}

		private static void OnEditorQuitting()
		{
			RestoreOriginalSettings();
		}

		static void OnToolbarGUI()
		{
			bool isFastPlaying = SessionState.GetBool(KEY_FAST_PLAYING, false);
			bool isPlaying = EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode;

			GUILayout.BeginVertical();
			AlignVertical();

			Color originalBg = GUI.backgroundColor;
			if (isFastPlaying && isPlaying)
				GUI.backgroundColor = new Color(0.35f, 0.9f, 0.35f, 1f);

			bool disable = isPlaying && !isFastPlaying;
			EditorGUI.BeginDisabledGroup(disable);

			GUIContent content = (isFastPlaying && isPlaying) ? GetStopContent() : GetPlayContent();

			if (GUILayout.Button(content, GetButtonStyle()))
			{
				if (isFastPlaying && isPlaying)
				{
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

		private static void EnterFastPlay()
		{
			SessionState.SetBool(KEY_ORIG_ENABLED, EditorSettings.enterPlayModeOptionsEnabled);
			SessionState.SetInt(KEY_ORIG_OPTIONS, (int)EditorSettings.enterPlayModeOptions);

			// Skip both Domain and Scene reload
			EditorSettings.enterPlayModeOptionsEnabled = true;
			EditorSettings.enterPlayModeOptions =
				EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

			SessionState.SetBool(KEY_FAST_PLAYING, true);
			Debug.Log("<b>[Fast Play]</b> Entering Play Mode (Domain & Scene reload disabled).");
			EditorApplication.isPlaying = true;
		}

		/// <summary>Restores original Enter Play Mode settings. Idempotent.</summary>
		private static void RestoreOriginalSettings()
		{
			if (!SessionState.GetBool(KEY_FAST_PLAYING, false))
				return;

			bool origEnabled = SessionState.GetBool(KEY_ORIG_ENABLED, false);
			EnterPlayModeOptions origOptions = (EnterPlayModeOptions)SessionState.GetInt(KEY_ORIG_OPTIONS, 0);

			// Clear flag first to prevent re-entrancy
			SessionState.SetBool(KEY_FAST_PLAYING, false);

			EditorSettings.enterPlayModeOptionsEnabled = origEnabled;
			EditorSettings.enterPlayModeOptions = origOptions;

			Debug.Log($"<b>[Fast Play]</b> Settings restored (enterPlayModeOptionsEnabled={origEnabled}, options={origOptions}).");
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				RestoreOriginalSettings();
			}
			else if (state == PlayModeStateChange.EnteredEditMode)
			{
				RestoreOriginalSettings();
				EditorApplication.delayCall += VerifySettingsRestored;
			}
		}

		/// <summary>Deferred safety check to ensure settings were truly restored.</summary>
		private static void VerifySettingsRestored()
		{
			if (SessionState.GetBool(KEY_FAST_PLAYING, false))
			{
				Debug.LogWarning("<b>[Fast Play]</b> Settings were not properly restored — forcing restoration now.");
				RestoreOriginalSettings();
			}
		}

		// UI Helpers

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
				// Try the active play icon first, then fall back
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
				catch { }
			}
			return null;
		}

		private static GUIStyle GetButtonStyle()
		{
			if (_buttonStyle == null)
			{
				try
				{
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
#else
			GUILayout.Space(2);
#endif
		}
	}
}
#endif
