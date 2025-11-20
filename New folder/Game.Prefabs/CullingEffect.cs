using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class CullingEffect : ComponentBase
{
	public enum AudioCullingGroup : byte
	{
		None,
		Fire,
		CarEngine,
		PublicTrans,
		Count
	}

	[Tooltip("The audio culling group")]
	public AudioCullingGroup m_AudioCullGroup;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if (m_AudioCullGroup != AudioCullingGroup.None)
		{
			components.Add(ComponentType.ReadWrite<CullingGroupData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_AudioCullGroup != AudioCullingGroup.None)
		{
			CullingGroupData componentData = new CullingGroupData
			{
				m_GroupIndex = (int)m_AudioCullGroup
			};
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
