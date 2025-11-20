using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Pathfind;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetEdgeDensitySystem : GameSystemBase
{
	[BurstCompile]
	private struct CalculateDensityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedBuilding> m_BuildingType;

		[ReadOnly]
		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		public ComponentTypeHandle<Density> m_DensityType;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_Workplaces;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public NativeQueue<DensityActionData>.ParallelWriter m_DensityActions;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Density> nativeArray2 = chunk.GetNativeArray(ref m_DensityType);
			NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
			BufferAccessor<ConnectedBuilding> bufferAccessor = chunk.GetBufferAccessor(ref m_BuildingType);
			BufferAccessor<SubLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubLaneType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Density value = nativeArray2[i];
				float density = value.m_Density;
				value.m_Density = 0f;
				DynamicBuffer<ConnectedBuilding> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity building = dynamicBuffer[j].m_Building;
					if (m_Renters.HasBuffer(building))
					{
						DynamicBuffer<Renter> dynamicBuffer2 = m_Renters[building];
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							Entity renter = dynamicBuffer2[k].m_Renter;
							if (m_HouseholdCitizens.HasBuffer(renter))
							{
								value.m_Density += m_HouseholdCitizens[renter].Length;
							}
							else if (m_Workplaces.HasComponent(renter))
							{
								value.m_Density += m_Workplaces[renter].m_MaxWorkers;
							}
						}
					}
					if (m_Workplaces.HasComponent(building))
					{
						value.m_Density += m_Workplaces[building].m_MaxWorkers;
					}
				}
				value.m_Density /= nativeArray3[i].m_Length;
				nativeArray2[i] = value;
				if (value.m_Density == density)
				{
					continue;
				}
				float density2 = math.sqrt(math.max(0.01f, value.m_Density));
				DynamicBuffer<SubLane> dynamicBuffer3 = bufferAccessor2[i];
				for (int l = 0; l < dynamicBuffer3.Length; l++)
				{
					Entity subLane = dynamicBuffer3[l].m_SubLane;
					if (m_CarLaneData.HasComponent(subLane))
					{
						m_DensityActions.Enqueue(new DensityActionData
						{
							m_Owner = subLane,
							m_Density = density2
						});
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

		[ReadOnly]
		public BufferTypeHandle<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Density> __Game_Net_Density_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedBuilding>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Density_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Density>();
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
		}
	}

	private EntityQuery m_EdgeQuery;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 1024;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<ConnectedBuilding>(), ComponentType.ReadWrite<Density>(), ComponentType.ReadOnly<Curve>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_EdgeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		DensityAction action = new DensityAction(Allocator.Persistent);
		CalculateDensityJob jobData = new CalculateDensityJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DensityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Density_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_Workplaces = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_DensityActions = action.m_DensityData.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EdgeQuery, base.Dependency);
		m_PathfindQueueSystem.Enqueue(action, base.Dependency);
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
	public NetEdgeDensitySystem()
	{
	}
}
