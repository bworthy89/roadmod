using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class EarlyGameOutsideConnectionTriggerSystem : GameSystemBase
{
	[BurstCompile]
	private struct TriggerJob : IJob
	{
		[ReadOnly]
		[DeallocateOnJobCompletion]
		public NativeArray<Building> m_Buildings;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_AvailabilityDatas;

		public NativeQueue<TriggerAction> m_ActionBuffer;

		public void Execute()
		{
			for (int i = 0; i < m_Buildings.Length; i++)
			{
				Building building = m_Buildings[i];
				if (building.m_RoadEdge != Entity.Null && m_AvailabilityDatas.HasBuffer(building.m_RoadEdge) && NetUtils.GetAvailability(m_AvailabilityDatas[building.m_RoadEdge], AvailableResource.OutsideConnection, building.m_CurvePosition) <= 0f)
				{
					m_ActionBuffer.Enqueue(new TriggerAction(TriggerType.NoOutsideConnection, Entity.Null, 0f));
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private static readonly float kDelaySeconds = 10f;

	private EntityQuery m_BuildingQuery;

	private TriggerSystem m_TriggerSystem;

	private SimulationSystem m_SimulationSystem;

	private ResourceAvailabilitySystem m_ResourceAvailabilitySystem;

	private bool m_Started;

	private double m_StartTime;

	private bool m_Triggered;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<BuildingCondition>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceAvailabilitySystem = base.World.GetOrCreateSystemManaged<ResourceAvailabilitySystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_BuildingQuery.IsEmptyIgnoreFilter && !m_Triggered)
		{
			if (!m_Started)
			{
				m_StartTime = m_SimulationSystem.frameIndex;
				m_Started = true;
			}
			if (m_ResourceAvailabilitySystem.appliedResource == AvailableResource.OutsideConnection && (double)m_SimulationSystem.frameIndex - m_StartTime > (double)(kDelaySeconds * 60f))
			{
				TriggerJob jobData = new TriggerJob
				{
					m_Buildings = m_BuildingQuery.ToComponentDataArray<Building>(Allocator.TempJob),
					m_AvailabilityDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
					m_ActionBuffer = m_TriggerSystem.CreateActionBuffer()
				};
				base.Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
				m_TriggerSystem.AddActionBufferWriter(base.Dependency);
				m_Triggered = true;
			}
		}
		if (m_BuildingQuery.IsEmptyIgnoreFilter && m_Started && !m_Triggered)
		{
			m_Started = false;
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (serializationContext.purpose == Purpose.NewGame)
		{
			m_Started = false;
			m_StartTime = 0.0;
			m_Triggered = false;
		}
		else
		{
			m_Triggered = true;
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
	public EarlyGameOutsideConnectionTriggerSystem()
	{
	}
}
