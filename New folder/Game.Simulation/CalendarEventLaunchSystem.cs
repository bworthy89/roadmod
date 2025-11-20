using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CalendarEventLaunchSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckEventLaunchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CalendarEventData> m_CalendarEventType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_EventType;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public CalendarEventMonths m_Month;

		public CalendarEventTimes m_Time;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CalendarEventData> nativeArray2 = chunk.GetNativeArray(ref m_CalendarEventType);
			NativeArray<EventData> nativeArray3 = chunk.GetNativeArray(ref m_EventType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray[i];
				CalendarEventData calendarEventData = nativeArray2[i];
				EventData eventData = nativeArray3[i];
				if ((m_Month & calendarEventData.m_AllowedMonths) != 0 && (m_Time & calendarEventData.m_AllowedTimes) != 0 && (float)random.NextInt(100) < calendarEventData.m_OccurenceProbability.min)
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, eventData.m_Archetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new PrefabRef(prefab));
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
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CalendarEventData> __Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CalendarEventData>(isReadOnly: true);
		}
	}

	private const int UPDATES_PER_DAY = 4;

	private EndFrameBarrier m_EndFrameBarrier;

	private TimeSystem m_TimeSystem;

	private EntityQuery m_CalendarEventQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 65536;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CalendarEventQuery = GetEntityQuery(ComponentType.ReadOnly<CalendarEventData>());
		GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		RequireForUpdate(m_CalendarEventQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CalendarEventMonths month = (CalendarEventMonths)(1 << Mathf.FloorToInt(m_TimeSystem.normalizedDate * 12f));
		CalendarEventTimes time = (CalendarEventTimes)(1 << Mathf.FloorToInt(m_TimeSystem.normalizedTime * 4f));
		CheckEventLaunchJob jobData = new CheckEventLaunchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_EventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CalendarEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CalendarEventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Month = month,
			m_Time = time,
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CalendarEventQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CalendarEventLaunchSystem()
	{
	}
}
