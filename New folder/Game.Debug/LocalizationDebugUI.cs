using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Colossal.Localization;
using Colossal.PSI.Environment;
using Game.Input;
using Game.SceneFlow;
using Game.UI.Editor;
using Game.UI.Localization;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public class LocalizationDebugUI : IDisposable
{
	private static readonly GUIContent[] kLocalizationDebugModeStrings = new GUIContent[3]
	{
		new GUIContent("Show Translations"),
		new GUIContent("Show IDs"),
		new GUIContent("Show Fallback")
	};

	private int m_SelectedContentId;

	public LocalizationDebugUI()
	{
		InitLocalization(m_SelectedContentId);
	}

	private void InitLocalization(int contentId)
	{
	}

	public void Dispose()
	{
	}

	private void Rebuild()
	{
		DebugSystem.Rebuild(BuildLocalizationDebugUI);
	}

	[DebugTab("Localization", -5)]
	private List<DebugUI.Widget> BuildLocalizationDebugUI(World world)
	{
		LocalizationManager manager = GameManager.instance.localizationManager;
		if (manager == null)
		{
			return null;
		}
		string[] locales = manager.GetSupportedLocales();
		GUIContent[] array = new GUIContent[locales.Length];
		int[] array2 = new int[locales.Length];
		for (int i = 0; i < locales.Length; i++)
		{
			array[i] = new GUIContent(locales[i]);
			array2[i] = i;
		}
		return new List<DebugUI.Widget>
		{
			new DebugUI.EnumField
			{
				displayName = "Language",
				getter = () => Array.IndexOf(locales, manager.activeDictionary.localeID),
				setter = delegate(int value)
				{
					manager.SetActiveLocale(locales[value]);
				},
				enumNames = array,
				enumValues = array2,
				getIndex = () => Array.IndexOf(locales, manager.activeDictionary.localeID),
				setIndex = delegate
				{
				}
			},
			new DebugUI.EnumField
			{
				displayName = "Debug Mode",
				getter = () => (int)GameManager.instance.userInterface.localizationBindings.debugMode,
				setter = delegate(int value)
				{
					GameManager.instance.userInterface.localizationBindings.debugMode = (LocalizationBindings.DebugMode)value;
				},
				enumNames = kLocalizationDebugModeStrings,
				autoEnum = typeof(LocalizationBindings.DebugMode),
				getIndex = () => (int)GameManager.instance.userInterface.localizationBindings.debugMode,
				setIndex = delegate
				{
				}
			},
			new DebugUI.Button
			{
				displayName = "Print input bindings and controls",
				action = delegate
				{
					List<string> list = new List<string>();
					List<string> list2 = new List<string>();
					foreach (ProxyBinding binding in InputManager.instance.GetBindings(InputManager.PathType.Effective, InputManager.BindingOptions.OnlyRebindable))
					{
						if (!list.Contains(binding.title))
						{
							list.Add(binding.title);
						}
						foreach (string item2 in binding.ToHumanReadablePath())
						{
							string item = binding.device.ToString() + "." + item2;
							if (!list2.Contains(item))
							{
								list2.Add(item);
							}
						}
					}
					UnityEngine.Debug.Log(string.Join("\n", list.Select(delegate(string b)
					{
						string text = b.Substring(b.IndexOf("/", StringComparison.InvariantCulture) + 1).Replace("/binding", "");
						return "Options.OPTION[" + b + "]\t" + text + "\nOptions.OPTION_DESCRIPTION[" + b + "]\tTBD";
					})));
					list2.Sort();
					UnityEngine.Debug.Log(string.Join("\n", list2.Select((string p) => "Options.INPUT_CONTROL[" + p + "]\t" + p.Substring(p.IndexOf(".", StringComparison.InvariantCulture) + 1))));
				}
			},
			new DebugUI.Button
			{
				displayName = "Print asset categories",
				action = delegate
				{
					EditorAssetCategorySystem orCreateSystemManaged = world.GetOrCreateSystemManaged<EditorAssetCategorySystem>();
					StringBuilder stringBuilder = new StringBuilder();
					foreach (EditorAssetCategory category in orCreateSystemManaged.GetCategories())
					{
						stringBuilder.AppendLine(category.GetLocalizationID() + "," + category.id);
					}
					string path = Path.Combine(EnvPath.kUserDataPath, "category_locale.csv");
					using FileStream stream = (File.Exists(path) ? File.OpenWrite(path) : File.Create(path));
					using StreamWriter streamWriter = new StreamWriter(stream);
					streamWriter.Write(stringBuilder);
				}
			}
		};
	}
}
