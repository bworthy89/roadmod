using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class BuildingStatusInfomodePrefab : GradientInfomodeBasePrefab
{
	public BuildingStatusType m_Type;

	public Bounds1 m_Range;

	public override string infomodeTypeLocaleKey => "BuildingColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewBuildingStatusData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewBuildingStatusData
		{
			m_Type = m_Type,
			m_Range = m_Range
		});
	}
}
