using System;
using System.Collections.Generic;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Activation/", new Type[] { typeof(TutorialPrefab) })]
public class TutorialFireActivation : TutorialActivation
{
	public enum FireActivationTarget
	{
		Building = 1,
		Forest
	}

	[EnumFlag]
	public FireActivationTarget m_Target = (FireActivationTarget)3;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		if ((m_Target & FireActivationTarget.Building) != 0)
		{
			components.Add(ComponentType.ReadWrite<BuildingFireActivationData>());
		}
		if ((m_Target & FireActivationTarget.Forest) != 0)
		{
			components.Add(ComponentType.ReadWrite<ForestFireActivationData>());
		}
	}
}
