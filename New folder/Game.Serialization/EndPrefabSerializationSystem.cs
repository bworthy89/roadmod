using System.Runtime.CompilerServices;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class EndPrefabSerializationSystem : GameSystemBase
{
	[BurstCompile]
	private struct EndPrefabSerializationJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<LoadedIndex> m_LoadedIndexType;

		public ComponentTypeHandle<PrefabData> m_PrefabDataType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabData> nativeArray = chunk.GetNativeArray(ref m_PrefabDataType);
			BufferAccessor<LoadedIndex> bufferAccessor = chunk.GetBufferAccessor(ref m_LoadedIndexType);
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_PrefabDataType);
			PrefabData value = default(PrefabData);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (enabledMask[i])
				{
					value.m_Index = bufferAccessor[i][0].m_Index;
					nativeArray[i] = value;
				}
				else
				{
					value = nativeArray[i];
				}
				enabledMask[i] = value.m_Index >= 0;
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
		public BufferTypeHandle<LoadedIndex> __Game_Prefabs_LoadedIndex_RO_BufferTypeHandle;

		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_LoadedIndex_RO_BufferTypeHandle = state.GetBufferTypeHandle<LoadedIndex>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>();
		}
	}

	private SaveGameSystem m_SaveGameSystem;

	private EntityQuery m_LoadedPrefabsQuery;

	private EntityQuery m_ContentPrefabQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SaveGameSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_LoadedPrefabsQuery = GetEntityQuery(ComponentType.ReadOnly<LoadedIndex>());
		m_ContentPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ContentData>(), ComponentType.ReadOnly<PrefabData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_SaveGameSystem.referencedContent.IsCreated)
		{
			m_SaveGameSystem.referencedContent.Dispose();
		}
		m_SaveGameSystem.referencedContent = m_ContentPrefabQuery.ToEntityArray(Allocator.Persistent);
		JobChunkExtensions.ScheduleParallel(new EndPrefabSerializationJob
		{
			m_LoadedIndexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_LoadedIndex_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_LoadedPrefabsQuery, base.Dependency).Complete();
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
	public EndPrefabSerializationSystem()
	{
	}
}
