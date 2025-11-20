using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/", new Type[] { })]
public class ToBeRemoved : ComponentBase
{
	public PrefabBase m_ReplaceWith;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ComponentBase.baseLog.WarnFormat(base.prefab, "Loading prefab that is set to be removed ({0})", base.prefab.name);
	}
}
