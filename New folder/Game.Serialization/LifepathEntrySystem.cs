using System.Runtime.CompilerServices;
using Game.Common;
using Game.Triggers;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class LifepathEntrySystem : GameSystemBase
{
	public struct FixLifepathChirpReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Chirp> m_ChirpType;

		[ReadOnly]
		public BufferLookup<LifePathEntry> m_EntryDatas;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityTypeHandle);
			NativeArray<Chirp> nativeArray2 = chunk.GetNativeArray(ref m_ChirpType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity sender = nativeArray2[i].m_Sender;
				if (m_EntryDatas.TryGetBuffer(sender, out var bufferData))
				{
					if (!Contains(bufferData, entity))
					{
						m_CommandBuffer.AppendToBuffer(unfilteredChunkIndex, sender, new LifePathEntry(entity));
					}
				}
				else
				{
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity);
				}
			}
		}

		private bool Contains(DynamicBuffer<LifePathEntry> entries, Entity chirp)
		{
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].m_Entity == chirp)
				{
					return true;
				}
			}
			return false;
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
		public ComponentTypeHandle<Chirp> __Game_Triggers_Chirp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<LifePathEntry> __Game_Triggers_LifePathEntry_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Triggers_Chirp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Chirp>(isReadOnly: true);
			__Game_Triggers_LifePathEntry_RO_BufferLookup = state.GetBufferLookup<LifePathEntry>(isReadOnly: true);
		}
	}

	private EntityQuery m_ChirpQuery;

	private DeserializationBarrier m_DeserializationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ChirpQuery = GetEntityQuery(ComponentType.ReadOnly<Chirp>(), ComponentType.ReadOnly<LifePathEvent>());
		m_DeserializationBarrier = base.World.GetOrCreateSystemManaged<DeserializationBarrier>();
		RequireForUpdate(m_ChirpQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FixLifepathChirpReferencesJob jobData = new FixLifepathChirpReferencesJob
		{
			m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ChirpType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Triggers_Chirp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntryDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Triggers_LifePathEntry_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_DeserializationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ChirpQuery, base.Dependency);
		m_DeserializationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public LifepathEntrySystem()
	{
	}
}
