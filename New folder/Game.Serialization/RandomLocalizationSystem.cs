using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class RandomLocalizationSystem : GameSystemBase
{
	[BurstCompile]
	private struct EnsureLocalizationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<RandomLocalizationIndex> m_RandomLocalizationType;

		[ReadOnly]
		public BufferLookup<LocalizationCount> m_LocalizationCounts;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<RandomLocalizationIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_RandomLocalizationType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<RandomLocalizationIndex> indices = bufferAccessor[i];
				Entity entity2 = nativeArray2[i].m_Prefab;
				if (m_SpawnableBuildingDatas.HasComponent(entity2))
				{
					entity2 = m_SpawnableBuildingDatas[entity2].m_ZonePrefab;
				}
				if (m_LocalizationCounts.TryGetBuffer(entity2, out var bufferData))
				{
					Random random = m_RandomSeed.GetRandom(1 + entity.Index);
					RandomLocalizationIndex.EnsureValidRandomIndices(indices, bufferData, ref random);
				}
				else
				{
					indices.Clear();
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<RandomLocalizationIndex> __Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<LocalizationCount> __Game_Prefabs_LocalizationCount_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle = state.GetBufferTypeHandle<RandomLocalizationIndex>();
			__Game_Prefabs_LocalizationCount_RO_BufferLookup = state.GetBufferLookup<LocalizationCount>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<RandomLocalizationIndex>(), ComponentType.ReadOnly<PrefabRef>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EnsureLocalizationJob jobData = new EnsureLocalizationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RandomLocalizationType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Common_RandomLocalizationIndex_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_LocalizationCounts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LocalizationCount_RO_BufferLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public RandomLocalizationSystem()
	{
	}
}
