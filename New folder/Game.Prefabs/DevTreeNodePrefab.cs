using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[RequireComponent(typeof(ManualUnlockable))]
public class DevTreeNodePrefab : PrefabBase
{
	public ServicePrefab m_Service;

	public DevTreeNodePrefab[] m_Requirements;

	public int m_Cost;

	public int m_HorizontalPosition;

	public float m_VerticalPosition;

	public string m_IconPath;

	public PrefabBase m_IconPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Service != null)
		{
			prefabs.Add(m_Service);
		}
		if (m_Requirements == null)
		{
			return;
		}
		DevTreeNodePrefab[] requirements = m_Requirements;
		foreach (DevTreeNodePrefab devTreeNodePrefab in requirements)
		{
			if (devTreeNodePrefab != null)
			{
				prefabs.Add(devTreeNodePrefab);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<DevTreeNodeData>());
		if (HasRequirements())
		{
			components.Add(ComponentType.ReadWrite<DevTreeNodeRequirement>());
		}
		if (m_Cost == 0)
		{
			components.Add(ComponentType.ReadWrite<DevTreeNodeAutoUnlock>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		Entity entity2 = existingSystemManaged.GetEntity(m_Service);
		entityManager.SetComponentData(entity, new DevTreeNodeData
		{
			m_Cost = m_Cost,
			m_Service = entity2
		});
		if (!entityManager.HasComponent<DevTreeNodeRequirement>(entity))
		{
			return;
		}
		DynamicBuffer<DevTreeNodeRequirement> buffer = entityManager.GetBuffer<DevTreeNodeRequirement>(entity);
		DevTreeNodePrefab[] requirements = m_Requirements;
		foreach (DevTreeNodePrefab devTreeNodePrefab in requirements)
		{
			if (devTreeNodePrefab != null)
			{
				Entity entity3 = existingSystemManaged.GetEntity(devTreeNodePrefab);
				buffer.Add(new DevTreeNodeRequirement
				{
					m_Node = entity3
				});
			}
		}
	}

	private bool HasRequirements()
	{
		if (m_Requirements != null)
		{
			DevTreeNodePrefab[] requirements = m_Requirements;
			for (int i = 0; i < requirements.Length; i++)
			{
				if (requirements[i] != null)
				{
					return true;
				}
			}
		}
		return false;
	}
}
