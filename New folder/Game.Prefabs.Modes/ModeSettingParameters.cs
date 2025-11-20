using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/", new Type[] { })]
public class ModeSettingParameters : PrefabBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ModeSettingData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ModeSettingData
		{
			m_Enable = false
		});
	}
}
