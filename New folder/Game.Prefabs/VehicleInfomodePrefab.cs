using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class VehicleInfomodePrefab : ColorInfomodeBasePrefab
{
	public VehicleType m_Type;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewVehicleData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewVehicleData
		{
			m_Type = m_Type,
			m_Color = new float4(m_Color.r, m_Color.g, m_Color.b, m_Color.a)
		});
	}
}
