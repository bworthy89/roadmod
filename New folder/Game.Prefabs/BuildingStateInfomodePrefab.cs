using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class BuildingStateInfomodePrefab : ColorInfomodeBasePrefab
{
	public BuildingStatusType m_Type;

	public override string infomodeTypeLocaleKey => "BuildingColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewBuildingStatusData>());
		if (m_Type == BuildingStatusType.LeisureProvider)
		{
			components.Add(ComponentType.ReadWrite<InfoviewNetStatusData>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewBuildingStatusData
		{
			m_Type = m_Type
		});
		if (m_Type == BuildingStatusType.LeisureProvider)
		{
			entityManager.SetComponentData(entity, new InfoviewNetStatusData
			{
				m_Type = NetStatusType.LeisureProvider
			});
		}
	}
}
