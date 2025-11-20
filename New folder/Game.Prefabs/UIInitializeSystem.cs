using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class UIInitializeSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UIObjectData> __Game_Prefabs_UIObjectData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UIAssetCategoryData> __Game_Prefabs_UIAssetCategoryData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_UIObjectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UIObjectData>(isReadOnly: true);
			__Game_Prefabs_UIAssetCategoryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UIAssetCategoryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_PrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_PolicyQuery;

	private TypeHandle __TypeHandle;

	public IEnumerable<PolicyPrefab> policies
	{
		get
		{
			if (!m_PolicyQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<PrefabData> prefabs = m_PolicyQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
				int i = 0;
				while (i < prefabs.Length)
				{
					yield return m_PrefabSystem.GetPrefab<PolicyPrefab>(prefabs[i]);
					int num = i + 1;
					i = num;
				}
				prefabs.Dispose();
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<Deleted>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<UIObjectData>(),
				ComponentType.ReadOnly<UIAssetCategoryData>()
			}
		});
		m_PolicyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<PolicyData>()
			}
		});
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<UIObjectData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UIObjectData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<UIAssetCategoryData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UIAssetCategoryData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<UIObjectData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<UIAssetCategoryData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					Entity entity = nativeArray2[j];
					UIObjectData uIObjectData = nativeArray3[j];
					if (base.EntityManager.TryGetBuffer(uIObjectData.m_Group, isReadOnly: false, out DynamicBuffer<UIGroupElement> buffer))
					{
						RemoveFrom(entity, buffer);
					}
					if (base.EntityManager.TryGetBuffer(uIObjectData.m_Group, isReadOnly: false, out DynamicBuffer<UnlockRequirement> buffer2))
					{
						RemoveFrom(entity, buffer2);
					}
				}
				for (int k = 0; k < nativeArray4.Length; k++)
				{
					Entity entity2 = nativeArray2[k];
					UIAssetCategoryData uIAssetCategoryData = nativeArray4[k];
					if (base.EntityManager.TryGetBuffer(uIAssetCategoryData.m_Menu, isReadOnly: false, out DynamicBuffer<UIGroupElement> buffer3))
					{
						RemoveFrom(entity2, buffer3);
					}
					if (base.EntityManager.TryGetBuffer(uIAssetCategoryData.m_Menu, isReadOnly: false, out DynamicBuffer<UnlockRequirement> buffer4))
					{
						RemoveFrom(entity2, buffer4);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose(base.Dependency);
		}
	}

	private void RemoveFrom(Entity entity, DynamicBuffer<UIGroupElement> uiGroupElements)
	{
		for (int i = 0; i < uiGroupElements.Length; i++)
		{
			if (uiGroupElements[i].m_Prefab == entity)
			{
				uiGroupElements.RemoveAtSwapBack(i);
				break;
			}
		}
	}

	private void RemoveFrom(Entity entity, DynamicBuffer<UnlockRequirement> unlockRequirements)
	{
		for (int i = 0; i < unlockRequirements.Length; i++)
		{
			if (unlockRequirements[i].m_Prefab == entity)
			{
				unlockRequirements.RemoveAtSwapBack(i);
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public UIInitializeSystem()
	{
	}
}
