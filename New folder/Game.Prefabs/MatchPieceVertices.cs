using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class MatchPieceVertices : ComponentBase
{
	public float[] m_Offsets;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetVertexMatchData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		NetVertexMatchData componentData = new NetVertexMatchData
		{
			m_Offsets = float.NaN
		};
		if (m_Offsets != null)
		{
			if (m_Offsets.Length >= 1)
			{
				componentData.m_Offsets.x = m_Offsets[0];
			}
			if (m_Offsets.Length >= 2)
			{
				componentData.m_Offsets.y = m_Offsets[1];
			}
			if (m_Offsets.Length >= 3)
			{
				componentData.m_Offsets.z = m_Offsets[2];
			}
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
