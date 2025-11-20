using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs.Climate;

[ComponentMenu("Themes/", new Type[] { typeof(WeatherPrefab) })]
public class SeasonFilter : ComponentBase
{
	public SeasonPrefab[] m_Seasons;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Seasons.Length; i++)
		{
			prefabs.Add(m_Seasons[i]);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ObjectRequirementElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<ObjectRequirementElement> buffer = entityManager.GetBuffer<ObjectRequirementElement>(entity);
		int length = buffer.Length;
		for (int i = 0; i < m_Seasons.Length; i++)
		{
			SeasonPrefab seasonPrefab = m_Seasons[i];
			Entity entity2 = existingSystemManaged.GetEntity(seasonPrefab);
			buffer.Add(new ObjectRequirementElement(entity2, length));
		}
	}
}
