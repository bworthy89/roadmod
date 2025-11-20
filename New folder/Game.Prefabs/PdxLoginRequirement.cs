using System;
using System.Collections.Generic;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Content/", new Type[] { typeof(ContentPrefab) })]
public class PdxLoginRequirement : ContentRequirementBase
{
	public override string GetDebugString()
	{
		return "Paradox Account Login";
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override bool CheckRequirement()
	{
		return PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk")?.hasEverLoggedIn ?? false;
	}
}
