using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Debug;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[DebugWatchOnly]
[CompilerGenerated]
public class ResidentPurposeCounterSystem : GameSystemBase
{
	public enum CountPurpose
	{
		GoingHome,
		GoingToSchool,
		GoingToWork,
		Leisure,
		MovingAway,
		Shopping,
		Travel,
		None,
		Other,
		TouristLeaving,
		Mail,
		MovingIn,
		Count
	}

	[BurstCompile]
	private struct PurposeCountJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Creatures.Resident> m_ResidentType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentTypeHandle<Divert> m_DivertType;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		public NativeArray<int> m_Results;

		public void Execute()
		{
			for (int i = 0; i < m_Results.Length; i++)
			{
				m_Results[i] = 0;
			}
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Game.Creatures.Resident> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ResidentType);
				BufferAccessor<PathElement> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_PathElementType);
				NativeArray<Divert> nativeArray3 = archetypeChunk.GetNativeArray(ref m_DivertType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					if (bufferAccessor[k].Length == 0)
					{
						continue;
					}
					Entity citizen = nativeArray2[k].m_Citizen;
					Entity household = m_HouseholdMembers[citizen].m_Household;
					if (m_MovingAways.HasComponent(household))
					{
						if (m_TouristHouseholds.HasComponent(household))
						{
							m_Results[9]++;
						}
						else
						{
							m_Results[4]++;
						}
					}
					else if (m_Households.HasComponent(household) && m_TravelPurposeData.HasComponent(citizen) && !m_CurrentBuildings.HasComponent(citizen))
					{
						if ((m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0)
						{
							m_Results[11]++;
						}
						Purpose purpose = m_TravelPurposeData[citizen].m_Purpose;
						if (nativeArray3.IsCreated)
						{
							purpose = nativeArray3[k].m_Purpose;
						}
						switch (purpose)
						{
						case Purpose.GoingHome:
							m_Results[0]++;
							break;
						case Purpose.GoingToSchool:
							m_Results[1]++;
							break;
						case Purpose.GoingToWork:
							m_Results[2]++;
							break;
						case Purpose.Leisure:
							m_Results[3]++;
							break;
						case Purpose.Shopping:
							m_Results[5]++;
							break;
						case Purpose.Traveling:
							m_Results[6]++;
							break;
						case Purpose.SendMail:
							m_Results[10]++;
							break;
						default:
							m_Results[8]++;
							break;
						}
					}
					else
					{
						m_Results[7]++;
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Divert> __Game_Creatures_Divert_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Creatures_Divert_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Divert>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
		}
	}

	private EntityQuery m_CreatureQuery;

	[EnumArray(typeof(CountPurpose))]
	[DebugWatchValue(historyLength = 1024)]
	private NativeArray<int> m_Results;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Creatures.Resident>(), ComponentType.ReadOnly<Human>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<PathOwner>(), ComponentType.ReadOnly<Target>(), ComponentType.Exclude<Unspawned>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		m_Results = new NativeArray<int>(12, Allocator.Persistent);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		PurposeCountJob jobData = new PurposeCountJob
		{
			m_Chunks = m_CreatureQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DivertType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Divert_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TravelPurposeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
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
	public ResidentPurposeCounterSystem()
	{
	}
}
