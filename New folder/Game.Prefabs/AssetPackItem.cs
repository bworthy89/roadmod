using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Asset Packs/", new Type[]
{
	typeof(ZonePrefab),
	typeof(ObjectPrefab),
	typeof(NetPrefab),
	typeof(AreaPrefab),
	typeof(RoutePrefab),
	typeof(NetLanePrefab)
})]
public class AssetPackItem : ComponentBase
{
	[NotNull]
	public AssetPackPrefab[] m_Packs;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Packs == null)
		{
			return;
		}
		for (int i = 0; i < m_Packs.Length; i++)
		{
			if (m_Packs[i] != null)
			{
				prefabs.Add(m_Packs[i]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AssetPackElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<AssetPackElement> buffer = entityManager.GetBuffer<AssetPackElement>(entity);
		buffer.Clear();
		if (m_Packs == null || m_Packs.Length == 0)
		{
			return;
		}
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		AssetPackElement elem = default(AssetPackElement);
		for (int i = 0; i < m_Packs.Length; i++)
		{
			AssetPackPrefab assetPackPrefab = m_Packs[i];
			if (!(assetPackPrefab == null))
			{
				elem.m_Pack = existingSystemManaged.GetEntity(assetPackPrefab);
				buffer.Add(elem);
			}
		}
	}
}
