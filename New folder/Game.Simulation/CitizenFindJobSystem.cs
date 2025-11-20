using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
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
public class CitizenFindJobSystem : GameSystemBase
{
	[BurstCompile]
	private struct CitizenFindJobJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Worker> m_WorkerType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<HasJobSeeker> m_HasJobSeekers;

		[ReadOnly]
		public Workplaces m_AvailableWorkspacesByLevel;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public bool m_IsUnemployedFindJob;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity household = m_HouseholdMembers[nativeArray[i]].m_Household;
				Citizen value = nativeArray2[i];
				CitizenAge age = value.GetAge();
				if (age == CitizenAge.Child || age == CitizenAge.Elderly)
				{
					value.m_UnemploymentTimeCounter = 0f;
					nativeArray2[i] = value;
					continue;
				}
				if (m_HasJobSeekers[nativeArray[i]].m_LastJobSeekFrameIndex + random.NextInt(kJobSeekCoolDownMin, kJobSeekCoolDownMax) > m_SimulationFrame)
				{
					value.m_UnemploymentTimeCounter += 1f / (float)kUpdatesPerDay;
					nativeArray2[i] = value;
					continue;
				}
				if (m_MovingAways.HasComponent(household))
				{
					value.m_UnemploymentTimeCounter += 1f / (float)kUpdatesPerDay;
					nativeArray2[i] = value;
					continue;
				}
				int educationLevel = value.GetEducationLevel();
				if (m_IsUnemployedFindJob)
				{
					value.m_UnemploymentTimeCounter += 1f / (float)kUpdatesPerDay;
					nativeArray2[i] = value;
					int num = 0;
					for (int j = 0; j <= educationLevel; j++)
					{
						if (m_AvailableWorkspacesByLevel[j] > 0)
						{
							num += m_AvailableWorkspacesByLevel[j];
						}
					}
					if (num <= 0 || num < random.NextInt(100))
					{
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, nativeArray[i], new HasJobSeeker
						{
							m_Seeker = Entity.Null,
							m_LastJobSeekFrameIndex = m_SimulationFrame
						});
						continue;
					}
				}
				else
				{
					value.m_UnemploymentTimeCounter = 0f;
					nativeArray2[i] = value;
					NativeArray<Worker> nativeArray4 = chunk.GetNativeArray(ref m_WorkerType);
					int num2 = ((!m_OutsideConnections.HasComponent(nativeArray4[i].m_Workplace)) ? nativeArray4[i].m_Level : 0);
					if (num2 >= educationLevel)
					{
						continue;
					}
					int num3 = 0;
					for (int k = num2; k <= educationLevel; k++)
					{
						if (m_AvailableWorkspacesByLevel[k] > 0)
						{
							num3 += m_AvailableWorkspacesByLevel[k];
						}
					}
					if (num3 <= 100 || num3 < random.NextInt(500))
					{
						m_CommandBuffer.SetComponent(unfilteredChunkIndex, nativeArray[i], new HasJobSeeker
						{
							m_Seeker = Entity.Null,
							m_LastJobSeekFrameIndex = m_SimulationFrame
						});
						continue;
					}
				}
				Entity entity = Entity.Null;
				if (!m_TouristHouseholds.HasComponent(household) && m_PropertyRenters.HasComponent(household))
				{
					entity = m_PropertyRenters[household].m_Property;
				}
				else if (m_HomelessHouseholds.HasComponent(household))
				{
					entity = m_HomelessHouseholds[household].m_TempHome;
				}
				else if (chunk.Has(ref m_CurrentBuildingType) && (value.m_State & CitizenFlags.Commuter) != CitizenFlags.None)
				{
					entity = nativeArray3[i].m_CurrentBuilding;
				}
				if (entity != Entity.Null)
				{
					Entity entity2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, new Owner
					{
						m_Owner = nativeArray[i]
					});
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, new JobSeeker
					{
						m_Level = (byte)value.GetEducationLevel(),
						m_Outside = (byte)(((value.m_State & CitizenFlags.Commuter) != CitizenFlags.None) ? 1u : 0u)
					});
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, new CurrentBuilding
					{
						m_CurrentBuilding = entity
					});
					m_CommandBuffer.SetComponentEnabled<HasJobSeeker>(unfilteredChunkIndex, nativeArray[i], value: true);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, nativeArray[i], new HasJobSeeker
					{
						m_Seeker = entity2,
						m_LastJobSeekFrameIndex = m_SimulationFrame
					});
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

		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Worker> __Game_Citizens_Worker_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HasJobSeeker> __Game_Agents_HasJobSeeker_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Worker>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Agents_HasJobSeeker_RO_ComponentLookup = state.GetComponentLookup<HasJobSeeker>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 256;

	public static readonly int kJobSeekCoolDownMax = 10000;

	public static readonly int kJobSeekCoolDownMin = 5000;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_UnemployedQuery;

	private EntityQuery m_EmployedQuery;

	private EntityQuery m_CitizenParametersQuery;

	private SimulationSystem m_SimulationSystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UnemployedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Citizen>(),
				ComponentType.ReadOnly<HouseholdMember>()
			},
			None = new ComponentType[7]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Worker>(),
				ComponentType.ReadOnly<Game.Citizens.Student>(),
				ComponentType.ReadOnly<HasJobSeeker>(),
				ComponentType.ReadOnly<HasSchoolSeeker>(),
				ComponentType.ReadOnly<HealthProblem>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_EmployedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Citizen>(),
				ComponentType.ReadOnly<HouseholdMember>(),
				ComponentType.ReadOnly<Worker>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Game.Citizens.Student>(),
				ComponentType.ReadOnly<HasJobSeeker>(),
				ComponentType.ReadOnly<HasSchoolSeeker>(),
				ComponentType.ReadOnly<HealthProblem>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_CitizenParametersQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenParametersData>());
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		RequireForUpdate(m_CitizenParametersQuery);
		RequireForUpdate(m_UnemployedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		CitizenFindJobJob jobData = new CitizenFindJobJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HasJobSeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_HasJobSeeker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IsUnemployedFindJob = true,
			m_UpdateFrameIndex = updateFrame,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_RandomSeed = RandomSeed.Next(),
			m_AvailableWorkspacesByLevel = m_CountWorkplacesSystem.GetUnemployedWorkspaceByLevel(),
			m_SimulationFrame = m_SimulationSystem.frameIndex
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UnemployedQuery, base.Dependency);
		if (!m_EmployedQuery.IsEmpty && RandomSeed.Next().GetRandom((int)m_SimulationSystem.frameIndex).NextFloat(1f) > m_CitizenParametersQuery.GetSingleton<CitizenParametersData>().m_SwitchJobRate)
		{
			CitizenFindJobJob jobData2 = new CitizenFindJobJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WorkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HasJobSeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_HasJobSeeker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IsUnemployedFindJob = false,
				m_UpdateFrameIndex = updateFrame,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_RandomSeed = RandomSeed.Next(),
				m_AvailableWorkspacesByLevel = m_CountWorkplacesSystem.GetFreeWorkplaces(),
				m_SimulationFrame = m_SimulationSystem.frameIndex
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_EmployedQuery, base.Dependency);
		}
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
	public CitizenFindJobSystem()
	{
	}
}
