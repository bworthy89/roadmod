using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public class LimitSettingPrefab : PrefabBase
{
	public int m_MaxChirpsLimit = 100;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LimitSettingData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new LimitSettingData
		{
			m_MaxChirpsLimit = m_MaxChirpsLimit
		});
	}
}
