using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class WeatherOverride : ComponentBase
{
	public WeatherPrefab[] m_Placeholders;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Placeholders.Length; i++)
		{
			prefabs.Add(m_Placeholders[i]);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpawnableObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Placeholders.Length; i++)
		{
			WeatherPrefab weatherPrefab = m_Placeholders[i];
			Entity entity2 = existingSystemManaged.GetEntity(weatherPrefab);
			entityManager.GetBuffer<PlaceholderObjectElement>(entity2).Add(new PlaceholderObjectElement(entity));
		}
	}
}
