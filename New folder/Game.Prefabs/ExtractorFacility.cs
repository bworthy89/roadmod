using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class ExtractorFacility : ComponentBase
{
	public Bounds1 m_RotationRange;

	public Bounds1 m_HeightOffset;

	public bool m_RouteNeeded;

	public bool m_NetNeeded;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ExtractorFacilityData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.ExtractorFacility>());
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		ExtractorFacilityData componentData = default(ExtractorFacilityData);
		componentData.m_RotationRange.min = math.radians(m_RotationRange.min);
		componentData.m_RotationRange.max = math.radians(m_RotationRange.max);
		componentData.m_HeightOffset = m_HeightOffset;
		componentData.m_Requirements = ExtractorRequirementFlags.None;
		if (m_RouteNeeded)
		{
			componentData.m_Requirements |= ExtractorRequirementFlags.RouteConnect;
		}
		if (m_NetNeeded)
		{
			componentData.m_Requirements |= ExtractorRequirementFlags.NetConnect;
		}
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(14));
	}
}
