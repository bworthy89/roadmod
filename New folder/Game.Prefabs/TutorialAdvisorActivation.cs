using System;
using System.Collections.Generic;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Activation/", new Type[]
{
	typeof(TutorialPrefab),
	typeof(TutorialListPrefab)
})]
public class TutorialAdvisorActivation : TutorialActivation
{
	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AdvisorActivationData>());
	}
}
