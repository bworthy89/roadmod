using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class EventTickSystem : GameSystemBase
{
	[BurstCompile]
	private struct EventTickJob : IJobChunk
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		public BufferTypeHandle<TargetElement> m_TargetElementType;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<FacingWeather> m_FacingWeatherData;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<SpectatorSite> m_SpectatorSiteData;

		[ReadOnly]
		public ComponentLookup<Criminal> m_CriminalData;

		[ReadOnly]
		public ComponentLookup<Flooded> m_FloodedData;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> m_Attendings;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Duration> nativeArray2 = chunk.GetNativeArray(ref m_DurationType);
			BufferAccessor<TargetElement> bufferAccessor = chunk.GetBufferAccessor(ref m_TargetElementType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<TargetElement> dynamicBuffer = bufferAccessor[i];
				int num = 0;
				while (num < dynamicBuffer.Length)
				{
					TargetElement targetElement = dynamicBuffer[num++];
					if ((!m_OnFireData.HasComponent(targetElement.m_Entity) || !(m_OnFireData[targetElement.m_Entity].m_Event == entity)) && (!m_AccidentSiteData.HasComponent(targetElement.m_Entity) || !(m_AccidentSiteData[targetElement.m_Entity].m_Event == entity)) && (!m_InvolvedInAccidentData.HasComponent(targetElement.m_Entity) || !(m_InvolvedInAccidentData[targetElement.m_Entity].m_Event == entity)) && (!m_FacingWeatherData.HasComponent(targetElement.m_Entity) || !(m_FacingWeatherData[targetElement.m_Entity].m_Event == entity)) && (!m_HealthProblemData.HasComponent(targetElement.m_Entity) || !(m_HealthProblemData[targetElement.m_Entity].m_Event == entity)) && (!m_DestroyedData.HasComponent(targetElement.m_Entity) || !(m_DestroyedData[targetElement.m_Entity].m_Event == entity)) && (!m_SpectatorSiteData.HasComponent(targetElement.m_Entity) || !(m_SpectatorSiteData[targetElement.m_Entity].m_Event == entity)) && (!m_CriminalData.HasComponent(targetElement.m_Entity) || !(m_CriminalData[targetElement.m_Entity].m_Event == entity)) && (!m_FloodedData.HasComponent(targetElement.m_Entity) || !(m_FloodedData[targetElement.m_Entity].m_Event == entity)) && (!m_Attendings.HasComponent(targetElement.m_Entity) || !(m_Attendings[targetElement.m_Entity].m_Meeting == entity)))
					{
						dynamicBuffer[--num] = dynamicBuffer[dynamicBuffer.Length - 1];
						dynamicBuffer.RemoveAt(dynamicBuffer.Length - 1);
					}
				}
				if (dynamicBuffer.Length == 0 && (nativeArray2.Length == 0 || nativeArray2[i].m_EndFrame <= m_SimulationFrame))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
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
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		public BufferTypeHandle<TargetElement> __Game_Events_TargetElement_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FacingWeather> __Game_Events_FacingWeather_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpectatorSite> __Game_Events_SpectatorSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Criminal> __Game_Citizens_Criminal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Flooded> __Game_Events_Flooded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Events_TargetElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<TargetElement>();
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Events_FacingWeather_RO_ComponentLookup = state.GetComponentLookup<FacingWeather>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Events_SpectatorSite_RO_ComponentLookup = state.GetComponentLookup<SpectatorSite>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentLookup = state.GetComponentLookup<Criminal>(isReadOnly: true);
			__Game_Events_Flooded_RO_ComponentLookup = state.GetComponentLookup<Flooded>(isReadOnly: true);
			__Game_Citizens_AttendingMeeting_RO_ComponentLookup = state.GetComponentLookup<AttendingMeeting>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_EventQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Events.Event>(), ComponentType.ReadWrite<TargetElement>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_EventQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EventTickJob jobData = new EventTickJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FacingWeatherData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_FacingWeather_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpectatorSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_SpectatorSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CriminalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FloodedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Flooded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attendings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EventQuery, base.Dependency);
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
	public EventTickSystem()
	{
	}
}
