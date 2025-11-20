using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class ObjectStatusInfomodePrefab : GradientInfomodeBasePrefab
{
	public ObjectStatusType m_Type;

	public Bounds1 m_Range;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewObjectStatusData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewObjectStatusData
		{
			m_Type = m_Type,
			m_Range = m_Range
		});
	}
}
