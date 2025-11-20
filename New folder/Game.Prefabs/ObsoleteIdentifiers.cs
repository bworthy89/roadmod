using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/", new Type[] { })]
public class ObsoleteIdentifiers : ComponentBase
{
	public PrefabIdentifierInfo[] m_PrefabIdentifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
