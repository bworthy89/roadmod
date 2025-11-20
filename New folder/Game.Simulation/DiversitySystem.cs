using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DiversitySystem : GameSystemBase, IPostDeserialize
{
	private EntityQuery m_AtmosphereQuery;

	private EntityQuery m_AtmospherePrefabQuery;

	private EntityQuery m_BiomeQuery;

	private EntityQuery m_BiomePrefabQuery;

	private EntityQuery m_EditorContainerQuery;

	public void ApplyAtmospherePreset(Entity atmospherePrefab)
	{
		AtmosphereData singleton = m_AtmosphereQuery.GetSingleton<AtmosphereData>();
		singleton.m_AtmospherePrefab = atmospherePrefab;
		m_AtmosphereQuery.SetSingleton(singleton);
	}

	public void ApplyBiomePreset(Entity biomePrefab)
	{
		BiomeData singleton = m_BiomeQuery.GetSingleton<BiomeData>();
		singleton.m_BiomePrefab = biomePrefab;
		m_BiomeQuery.SetSingleton(singleton);
	}

	public void PostDeserialize(Context context)
	{
		NativeArray<Entity> nativeArray = m_AtmospherePrefabQuery.ToEntityArray(Allocator.Temp);
		if (nativeArray.Length == 0)
		{
			COSystemBase.baseLog.InfoFormat("WARNING: PostDeserialize({0}): no Atmosphere prefabs found", context);
			return;
		}
		NativeArray<Entity> nativeArray2 = m_BiomePrefabQuery.ToEntityArray(Allocator.Temp);
		if (nativeArray2.Length == 0)
		{
			COSystemBase.baseLog.InfoFormat("WARNING: PostDeserialize({0}): no Biome prefabs found", context);
			return;
		}
		if (m_AtmosphereQuery.IsEmptyIgnoreFilter)
		{
			Entity prefab = nativeArray[0];
			Entity entity = base.EntityManager.CreateEntity();
			base.EntityManager.AddComponentData(entity, new AtmosphereData(prefab));
		}
		if (m_BiomeQuery.IsEmptyIgnoreFilter)
		{
			Entity prefab2 = nativeArray2[0];
			Entity entity2 = base.EntityManager.CreateEntity();
			base.EntityManager.AddComponentData(entity2, new BiomeData(prefab2));
		}
		if (m_EditorContainerQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Entity> nativeArray3 = m_EditorContainerQuery.ToEntityArray(Allocator.TempJob);
		foreach (Entity item in nativeArray3)
		{
			if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(item, out var component) && component.m_Position.Equals(float3.zero))
			{
				Entity entity3 = item;
				UnityEngine.Debug.Log("There is invalid EditorContainer in the map:" + entity3.ToString());
				if (base.EntityManager.TryGetComponent<Owner>(item, out var component2) && base.EntityManager.TryGetComponent<Game.Objects.Transform>(component2.m_Owner, out var component3))
				{
					base.EntityManager.SetComponentData(item, component3);
				}
			}
		}
		nativeArray3.Dispose();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AtmosphereQuery = GetEntityQuery(ComponentType.ReadOnly<AtmosphereData>());
		m_AtmospherePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<AtmospherePrefabData>());
		m_BiomeQuery = GetEntityQuery(ComponentType.ReadOnly<BiomeData>());
		m_BiomePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<BiomePrefabData>());
		m_EditorContainerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Tools.EditorContainer>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<Owner>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	[Preserve]
	public DiversitySystem()
	{
	}
}
