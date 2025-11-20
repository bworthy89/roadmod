using System.Runtime.CompilerServices;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Citizens;

[CompilerGenerated]
public class MeetingInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeMeetingJob : IJobChunk
	{
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<CoordinatedMeetingAttendee> m_AttendeeType;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> m_Attendings;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<CoordinatedMeetingAttendee> bufferAccessor = chunk.GetBufferAccessor(ref m_AttendeeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity meeting = nativeArray[i];
				DynamicBuffer<CoordinatedMeetingAttendee> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity attendee = dynamicBuffer[j].m_Attendee;
					if (!m_Attendings.HasComponent(attendee))
					{
						m_CommandBuffer.AddComponent(attendee, new AttendingMeeting
						{
							m_Meeting = meeting
						});
					}
					else
					{
						dynamicBuffer.RemoveAt(j);
						j--;
					}
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

		public BufferTypeHandle<CoordinatedMeetingAttendee> __Game_Citizens_CoordinatedMeetingAttendee_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CoordinatedMeetingAttendee_RW_BufferTypeHandle = state.GetBufferTypeHandle<CoordinatedMeetingAttendee>();
			__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier5;

	private EntityQuery m_MeetingQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier5 = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_MeetingQuery = GetEntityQuery(ComponentType.ReadOnly<CoordinatedMeeting>(), ComponentType.ReadWrite<CoordinatedMeetingAttendee>(), ComponentType.ReadOnly<Created>());
		RequireForUpdate(m_MeetingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitializeMeetingJob jobData = new InitializeMeetingJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AttendeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_CoordinatedMeetingAttendee_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Attendings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier5.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_MeetingQuery, base.Dependency);
		m_ModificationBarrier5.AddJobHandleForProducer(base.Dependency);
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
	public MeetingInitializeSystem()
	{
	}
}
