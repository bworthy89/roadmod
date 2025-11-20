using System;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Content/", new Type[] { typeof(ContentPrefab) })]
public class DlcRequirement : ContentRequirementBase
{
	public DlcId m_Dlc;

	public bool m_BaseGameRequiresDatabase;

	public override string GetDebugString()
	{
		return PlatformManager.instance.GetDlcName(m_Dlc).Nicify() + " DLC";
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override bool CheckRequirement()
	{
		if (!m_BaseGameRequiresDatabase || (AssetDatabase.global.TryGetDatabase(base.prefab.name, out var database) && database.dlcId == m_Dlc))
		{
			return PlatformManager.instance.IsDlcOwned(m_Dlc);
		}
		return false;
	}
}
