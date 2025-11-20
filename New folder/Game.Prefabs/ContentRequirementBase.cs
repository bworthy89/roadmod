using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public abstract class ContentRequirementBase : ComponentBase
{
	[TextArea]
	public string m_Notes;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public abstract bool CheckRequirement();
}
