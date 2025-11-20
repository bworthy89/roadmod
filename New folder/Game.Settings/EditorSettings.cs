using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Settings;

[FileLocation("Editor")]
public class EditorSettings : Setting
{
	public int prefabPickerColumnCount { get; set; }

	public string[] prefabPickerFavorites { get; set; }

	public string[] prefabPickerSearchHistory { get; set; }

	public string[] prefabPickerSearchFavorites { get; set; }

	public int assetPickerColumnCount { get; set; }

	public string[] assetPickerFavorites { get; set; }

	public string[] directoryPickerFavorites { get; set; }

	public int inspectorWidth { get; set; }

	public int hierarchyWidth { get; set; }

	public bool useParallelImport { get; set; }

	public bool lowQualityTextureCompression { get; set; }

	public string lastSelectedProjectRootDirectory { get; set; }

	public string lastSelectedImportDirectory { get; set; }

	public bool showTutorials { get; set; }

	public Dictionary<string, bool> shownTutorials { get; set; }

	[SettingsUIButton]
	[SettingsUIConfirmation(null, null)]
	public bool resetTutorials
	{
		set
		{
			ResetTutorials();
		}
	}

	public void ResetTutorials()
	{
		shownTutorials.Clear();
		ApplyAndSave();
		World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EditorTutorialSystem>().OnResetTutorials();
	}

	public EditorSettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		prefabPickerColumnCount = 1;
		prefabPickerFavorites = new string[0];
		prefabPickerSearchHistory = new string[0];
		prefabPickerSearchFavorites = new string[0];
		assetPickerColumnCount = 4;
		assetPickerFavorites = new string[0];
		directoryPickerFavorites = new string[0];
		inspectorWidth = 450;
		hierarchyWidth = 350;
		lastSelectedProjectRootDirectory = null;
		lastSelectedImportDirectory = null;
		useParallelImport = true;
		lowQualityTextureCompression = false;
		showTutorials = false;
		shownTutorials = new Dictionary<string, bool>();
	}
}
