using System;
using System.Collections.Generic;
using Game.City;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { })]
public abstract class ParametricStatistic : StatisticsPrefab
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<StatisticParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<StatisticParameter>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<StatisticParameterData> buffer = entityManager.GetBuffer<StatisticParameterData>(entity);
		foreach (StatisticParameterData parameter in GetParameters())
		{
			buffer.Add(parameter);
		}
	}

	public abstract IEnumerable<StatisticParameterData> GetParameters();

	public abstract string GetParameterName(int parameter);
}
