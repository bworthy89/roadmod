using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class NetObject : ComponentBase
{
	public NetPieceRequirements[] m_SetCompositionState;

	public RoadTypes m_RequireRoad;

	public RoadTypes m_RoadPassThrough;

	public TrackTypes m_TrackPassThrough;

	public float m_NodeOffset;

	public bool m_Attached = true;

	public bool m_RequirePedestrian;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetObjectData>());
		components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.NetObject>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		NetObjectData componentData = default(NetObjectData);
		NetCompositionHelpers.GetRequirementFlags(m_SetCompositionState, out componentData.m_CompositionFlags, out var sectionFlags);
		if (sectionFlags != 0)
		{
			ComponentBase.baseLog.ErrorFormat(base.prefab, "NetObject ({0}) cannot set section flags: {1}", base.prefab.name, sectionFlags);
		}
		componentData.m_RequireRoad = m_RequireRoad;
		componentData.m_RoadPassThrough = m_RoadPassThrough;
		componentData.m_TrackPassThrough = m_TrackPassThrough;
		if (m_RequireRoad == RoadTypes.Car && m_SetCompositionState != null)
		{
			for (int i = 0; i < m_SetCompositionState.Length; i++)
			{
				if (m_SetCompositionState[i] == NetPieceRequirements.ShipStop)
				{
					componentData.m_RequireRoad |= RoadTypes.Watercraft;
				}
			}
		}
		entityManager.SetComponentData(entity, componentData);
		PlaceableObjectData componentData2 = entityManager.GetComponentData<PlaceableObjectData>(entity);
		componentData2.m_Flags |= Game.Objects.PlacementFlags.NetObject;
		componentData2.m_PlacementOffset.z = m_NodeOffset;
		bool flag = (componentData.m_CompositionFlags & CompositionFlags.nodeMask) != default(CompositionFlags);
		bool num = (componentData.m_CompositionFlags & ~CompositionFlags.nodeMask) != default(CompositionFlags);
		if (flag)
		{
			componentData2.m_Flags |= Game.Objects.PlacementFlags.RoadNode;
			componentData2.m_SubReplacementType = SubReplacementType.None;
		}
		if (num || !flag)
		{
			componentData2.m_Flags |= Game.Objects.PlacementFlags.RoadEdge;
			componentData2.m_SubReplacementType = SubReplacementType.None;
		}
		if ((m_RequireRoad & RoadTypes.Watercraft) != RoadTypes.None)
		{
			componentData2.m_Flags |= Game.Objects.PlacementFlags.Waterway;
		}
		if (m_Attached)
		{
			componentData2.m_Flags |= Game.Objects.PlacementFlags.Attached;
		}
		if (m_RequirePedestrian)
		{
			componentData2.m_Flags |= Game.Objects.PlacementFlags.RequirePedestrian;
		}
		entityManager.SetComponentData(entity, componentData2);
	}
}
