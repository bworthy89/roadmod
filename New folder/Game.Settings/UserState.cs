using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using Game.Assets;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Settings;

[FileLocation("UserState")]
public class UserState : Setting
{
	public Dictionary<string, bool> shownTutorials { get; set; }

	public SaveGameMetadata lastSaveGameMetadata { get; set; }

	public string lastCloudTarget { get; set; }

	public bool leftHandTraffic { get; set; }

	public bool naturalDisasters { get; set; }

	public bool unlockAll { get; set; }

	public bool unlimitedMoney { get; set; }

	public bool unlockMapTiles { get; set; }

	[SettingsUIHidden]
	public List<string> seenWhatsNew { get; set; }

	public void ResetTutorials()
	{
		shownTutorials.Clear();
		ApplyAndSave();
		World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TutorialSystem>().OnResetTutorials();
	}

	public UserState()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		shownTutorials = new Dictionary<string, bool>();
		lastSaveGameMetadata = null;
		lastCloudTarget = GetDefaultCloudTarget();
		leftHandTraffic = false;
		naturalDisasters = true;
		unlockAll = false;
		unlimitedMoney = false;
		unlockMapTiles = false;
		seenWhatsNew = new List<string>();
	}

	public string GetDefaultCloudTarget()
	{
		return "Local";
	}
}
