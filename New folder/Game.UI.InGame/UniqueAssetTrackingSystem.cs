using System;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class UniqueAssetTrackingSystem : GameSystemBase, IUniqueAssetTrackingSystem
{
	private EntityQuery m_LoadedUniqueAssetQuery;

	private EntityQuery m_DeletedUniqueAssetQuery;

	private EntityQuery m_PlacedUniqueAssetQuery;

	private bool m_Loaded;

	public NativeParallelHashSet<Entity> placedUniqueAssets { get; private set; }

	public Action<Entity, bool> EventUniqueAssetStatusChanged { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadedUniqueAssetQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>());
		m_DeletedUniqueAssetQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_PlacedUniqueAssetQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		placedUniqueAssets = new NativeParallelHashSet<Entity>(32, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		placedUniqueAssets.Dispose();
		base.OnDestroy();
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (GetLoaded() && !m_LoadedUniqueAssetQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<PrefabRef> nativeArray = m_LoadedUniqueAssetQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				placedUniqueAssets.Add(nativeArray[i].m_Prefab);
				EventUniqueAssetStatusChanged?.Invoke(nativeArray[i].m_Prefab, arg2: true);
			}
			nativeArray.Dispose();
		}
		if (!m_PlacedUniqueAssetQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<PrefabRef> nativeArray2 = m_PlacedUniqueAssetQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				placedUniqueAssets.Add(nativeArray2[j].m_Prefab);
				EventUniqueAssetStatusChanged?.Invoke(nativeArray2[j].m_Prefab, arg2: true);
			}
			nativeArray2.Dispose();
		}
		if (!m_DeletedUniqueAssetQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<PrefabRef> nativeArray3 = m_DeletedUniqueAssetQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				placedUniqueAssets.Remove(nativeArray3[k].m_Prefab);
				EventUniqueAssetStatusChanged?.Invoke(nativeArray3[k].m_Prefab, arg2: false);
			}
			nativeArray3.Dispose();
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		placedUniqueAssets.Clear();
		m_Loaded = true;
	}

	public bool IsUniqueAsset(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<PlaceableObjectData>(entity, out var component))
		{
			return (component.m_Flags & PlacementFlags.Unique) != 0;
		}
		return false;
	}

	public bool IsPlacedUniqueAsset(Entity entity)
	{
		if (IsUniqueAsset(entity))
		{
			return placedUniqueAssets.Contains(entity);
		}
		return false;
	}

	[Preserve]
	public UniqueAssetTrackingSystem()
	{
	}
}
