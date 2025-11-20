using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class ObjectEmergeSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindObjectsInBuildingJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_Building;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> m_TripSourceType;

		public NativeQueue<Entity>.ParallelWriter m_EmergeQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentBuilding> nativeArray2 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					if (nativeArray2[i].m_CurrentBuilding == m_Building)
					{
						m_EmergeQueue.Enqueue(nativeArray[i]);
					}
				}
				return;
			}
			NativeArray<TripSource> nativeArray3 = chunk.GetNativeArray(ref m_TripSourceType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				if (nativeArray3[j].m_Source == m_Building)
				{
					m_EmergeQueue.Enqueue(nativeArray[j]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindObjectsInVehiclesJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		public NativeQueue<Entity>.ParallelWriter m_EmergeQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Passenger> bufferAccessor = chunk.GetBufferAccessor(ref m_PassengerType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<Passenger> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Passenger passenger = dynamicBuffer[j];
					m_EmergeQueue.Enqueue(passenger.m_Passenger);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct EmergeObjectsJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> m_CreatureDataType;

		[ReadOnly]
		public ComponentTypeHandle<PetData> m_PetDataType;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> m_ResidentDataType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<TripSource> m_TripSourceData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> m_HouseholdPetData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<Animal> m_AnimalData;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLane;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<HouseholdPetData> m_PrefabHouseholdPetData;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_TripSourceRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_CurrentVehicleRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_CurrentVehicleHumanAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_CurrentVehicleAnimalAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_HumanSpawnTypes;

		[ReadOnly]
		public ComponentTypeSet m_AnimalSpawnTypes;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CreaturePrefabChunks;

		public NativeQueue<Entity> m_EmergeQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int count = m_EmergeQueue.Count;
			if (count == 0)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				Entity entity = m_EmergeQueue.Dequeue();
				if (m_TripSourceData.HasComponent(entity))
				{
					if (!m_UpdatedData.HasComponent(entity))
					{
						m_CommandBuffer.RemoveComponent(entity, in m_TripSourceRemoveTypes);
						m_CommandBuffer.AddComponent(entity, default(BatchesUpdated));
					}
				}
				else if (m_CurrentVehicleData.HasComponent(entity))
				{
					ExitVehicle(entity, m_CurrentVehicleData[entity].m_Vehicle);
				}
				else if (m_CurrentBuildingData.HasComponent(entity))
				{
					ExitBuilding(entity, m_CurrentBuildingData[entity].m_CurrentBuilding);
				}
			}
		}

		private void ExitVehicle(Entity creature, Entity vehicle)
		{
			Transform component = m_TransformData[creature];
			if (m_TransformData.HasComponent(vehicle))
			{
				component = m_TransformData[vehicle];
			}
			m_CommandBuffer.RemoveComponent(creature, in m_CurrentVehicleRemoveTypes);
			bool num = m_HumanData.HasComponent(creature);
			bool flag = m_AnimalData.HasComponent(creature);
			CreatureLaneFlags creatureLaneFlags = CreatureLaneFlags.Obsolete;
			if (m_UnspawnedData.HasComponent(vehicle))
			{
				creatureLaneFlags |= CreatureLaneFlags.EmergeUnspawned;
				m_CommandBuffer.AddComponent(creature, default(Unspawned));
			}
			if (num && !m_HumanCurrentLaneData.HasComponent(creature))
			{
				m_CommandBuffer.AddComponent(creature, in m_CurrentVehicleHumanAddTypes);
				m_CommandBuffer.SetComponent(creature, component);
				m_CommandBuffer.SetComponent(creature, new HumanCurrentLane(creatureLaneFlags));
			}
			else if (flag && !m_AnimalCurrentLane.HasComponent(creature))
			{
				m_CommandBuffer.AddComponent(creature, in m_CurrentVehicleAnimalAddTypes);
				m_CommandBuffer.SetComponent(creature, component);
				m_CommandBuffer.SetComponent(creature, new AnimalCurrentLane(creatureLaneFlags));
			}
			if (m_ResidentData.HasComponent(creature))
			{
				Game.Creatures.Resident component2 = m_ResidentData[creature];
				component2.m_Flags &= ~ResidentFlags.InVehicle;
				component2.m_Timer = 0;
				m_CommandBuffer.SetComponent(creature, component2);
			}
		}

		private void ExitBuilding(Entity entity, Entity building)
		{
			m_CommandBuffer.RemoveComponent<CurrentBuilding>(entity);
			if (m_CurrentTransportData.HasComponent(entity))
			{
				CurrentTransport currentTransport = m_CurrentTransportData[entity];
				if (m_TripSourceData.HasComponent(currentTransport.m_CurrentTransport) && !m_UpdatedData.HasComponent(currentTransport.m_CurrentTransport))
				{
					m_CommandBuffer.RemoveComponent(currentTransport.m_CurrentTransport, in m_TripSourceRemoveTypes);
				}
			}
			else if (m_CitizenData.HasComponent(entity))
			{
				bool isDead = false;
				if (m_HealthProblemData.TryGetComponent(entity, out var componentData))
				{
					isDead = (componentData.m_Flags & HealthProblemFlags.Dead) != 0;
				}
				Entity householdHomeBuilding = BuildingUtils.GetHouseholdHomeBuilding(m_HouseholdMembers[entity].m_Household, ref m_PropertyRenters, ref m_HomelessHouseholds);
				SpawnResident(entity, building, householdHomeBuilding, isDead);
			}
			else if (m_HouseholdPetData.HasComponent(entity))
			{
				SpawnPet(entity, building);
			}
		}

		private void SpawnResident(Entity citizenEntity, Entity building, Entity homeEntity, bool isDead)
		{
			if (!isDead)
			{
				CreatureData creatureData;
				PseudoRandomSeed randomSeed;
				Entity entity = SelectResidentPrefab(m_CitizenData[citizenEntity], m_CreaturePrefabChunks, m_EntityType, ref m_CreatureDataType, ref m_ResidentDataType, out creatureData, out randomSeed);
				ObjectData objectData = m_PrefabObjectData[entity];
				PrefabRef component = new PrefabRef
				{
					m_Prefab = entity
				};
				Transform component2 = ((!m_TransformData.HasComponent(building)) ? new Transform(default(float3), quaternion.identity) : m_TransformData[building]);
				Game.Creatures.Resident component3 = new Game.Creatures.Resident
				{
					m_Citizen = citizenEntity
				};
				PathOwner component4 = new PathOwner(PathFlags.Obsolete);
				TripSource component5 = new TripSource(building);
				HumanCurrentLane component6 = new HumanCurrentLane(CreatureLaneFlags.Obsolete);
				Divert component7 = new Divert
				{
					m_Purpose = Purpose.Safety
				};
				Entity entity2 = m_CommandBuffer.CreateEntity(objectData.m_Archetype);
				m_CommandBuffer.AddComponent(entity2, in m_HumanSpawnTypes);
				m_CommandBuffer.SetComponent(entity2, component2);
				m_CommandBuffer.SetComponent(entity2, component);
				m_CommandBuffer.SetComponent(entity2, component3);
				m_CommandBuffer.SetComponent(entity2, component4);
				m_CommandBuffer.SetComponent(entity2, randomSeed);
				m_CommandBuffer.SetComponent(entity2, component6);
				m_CommandBuffer.SetComponent(entity2, component5);
				m_CommandBuffer.SetComponent(entity2, component7);
				m_CommandBuffer.AddComponent(citizenEntity, new CurrentTransport(entity2));
			}
			Purpose purpose = Purpose.GoingHome;
			if (homeEntity == Entity.Null || m_Deleteds.HasComponent(homeEntity))
			{
				purpose = Purpose.Leisure;
			}
			m_CommandBuffer.AddComponent(citizenEntity, new TravelPurpose
			{
				m_Purpose = purpose
			});
		}

		private void SpawnPet(Entity householdPet, Entity building)
		{
			PrefabRef prefabRef = m_PrefabRefData[householdPet];
			HouseholdPetData householdPetData = m_PrefabHouseholdPetData[prefabRef.m_Prefab];
			Random random = m_RandomSeed.GetRandom(householdPet.Index);
			PseudoRandomSeed randomSeed;
			Entity entity = SelectAnimalPrefab(ref random, householdPetData.m_Type, m_CreaturePrefabChunks, m_EntityType, m_PetDataType, out randomSeed);
			ObjectData objectData = m_PrefabObjectData[entity];
			Entity entity2 = m_CommandBuffer.CreateEntity(objectData.m_Archetype);
			m_CommandBuffer.AddComponent(entity2, in m_AnimalSpawnTypes);
			m_CommandBuffer.SetComponent(entity2, new PrefabRef(entity));
			m_CommandBuffer.SetComponent(entity2, m_TransformData[building]);
			m_CommandBuffer.SetComponent(entity2, new Game.Creatures.Pet(householdPet));
			m_CommandBuffer.SetComponent(entity2, randomSeed);
			m_CommandBuffer.SetComponent(entity2, new TripSource(building));
			m_CommandBuffer.SetComponent(entity2, new AnimalCurrentLane(CreatureLaneFlags.Obsolete));
			m_CommandBuffer.AddComponent(householdPet, new CurrentTransport(entity2));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PetData> __Game_Prefabs_PetData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TripSource> __Game_Objects_TripSource_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPet> __Game_Citizens_HouseholdPet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Animal> __Game_Creatures_Animal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPetData> __Game_Prefabs_HouseholdPetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(isReadOnly: true);
			__Game_Prefabs_PetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PetData>(isReadOnly: true);
			__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentLookup = state.GetComponentLookup<TripSource>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdPet_RO_ComponentLookup = state.GetComponentLookup<HouseholdPet>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Creatures_Animal_RO_ComponentLookup = state.GetComponentLookup<Animal>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_HouseholdPetData_RO_ComponentLookup = state.GetComponentLookup<HouseholdPetData>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
		}
	}

	private ModificationBarrier4B m_ModificationBarrier;

	private EntityQuery m_DeletedBuildingQuery;

	private EntityQuery m_DeletedVehicleQuery;

	private EntityQuery m_EmergeObjectQuery;

	private EntityQuery m_CreaturePrefabQuery;

	private ComponentTypeSet m_TripSourceRemoveTypes;

	private ComponentTypeSet m_CurrentVehicleRemoveTypes;

	private ComponentTypeSet m_CurrentVehicleHumanAddTypes;

	private ComponentTypeSet m_CurrentVehicleAnimalAddTypes;

	private ComponentTypeSet m_HumanSpawnTypes;

	private ComponentTypeSet m_AnimalSpawnTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_DeletedBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>());
		m_DeletedVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Passenger>(), ComponentType.Exclude<Temp>());
		m_EmergeObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<CurrentBuilding>(),
				ComponentType.ReadOnly<TripSource>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_CreaturePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureData>(), ComponentType.ReadOnly<PrefabData>());
		m_TripSourceRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>());
		m_CurrentVehicleRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<CurrentVehicle>(), ComponentType.ReadWrite<Relative>(), ComponentType.ReadWrite<Unspawned>());
		m_CurrentVehicleHumanAddTypes = new ComponentTypeSet(new ComponentType[7]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<HumanNavigation>(),
			ComponentType.ReadWrite<HumanCurrentLane>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_CurrentVehicleAnimalAddTypes = new ComponentTypeSet(new ComponentType[7]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<AnimalNavigation>(),
			ComponentType.ReadWrite<AnimalCurrentLane>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_HumanSpawnTypes = new ComponentTypeSet(ComponentType.ReadWrite<HumanCurrentLane>(), ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>(), ComponentType.ReadWrite<Divert>());
		m_AnimalSpawnTypes = new ComponentTypeSet(ComponentType.ReadWrite<AnimalCurrentLane>(), ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_DeletedBuildingQuery.IsEmptyIgnoreFilter;
		bool flag2 = !m_DeletedVehicleQuery.IsEmptyIgnoreFilter;
		if (!flag && !flag2)
		{
			return;
		}
		NativeQueue<Entity> emergeQueue = new NativeQueue<Entity>(Allocator.TempJob);
		NativeQueue<Entity>.ParallelWriter emergeQueue2 = emergeQueue.AsParallelWriter();
		if (flag)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_DeletedBuildingQuery.ToArchetypeChunkArray(Allocator.TempJob);
			try
			{
				EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					NativeArray<Entity> nativeArray2 = nativeArray[i].GetNativeArray(entityTypeHandle);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						FindObjectsInBuildingJob jobData = new FindObjectsInBuildingJob
						{
							m_Building = nativeArray2[j],
							m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
							m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
							m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentTypeHandle, ref base.CheckedStateRef),
							m_EmergeQueue = emergeQueue2
						};
						base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EmergeObjectQuery, base.Dependency);
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		if (flag2)
		{
			FindObjectsInVehiclesJob jobData2 = new FindObjectsInVehiclesJob
			{
				m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_EmergeQueue = emergeQueue2
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_DeletedVehicleQuery, base.Dependency);
		}
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> creaturePrefabChunks = m_CreaturePrefabQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new EmergeObjectsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PetDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PetData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TripSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Animal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHouseholdPetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HouseholdPetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TripSourceRemoveTypes = m_TripSourceRemoveTypes,
			m_CurrentVehicleRemoveTypes = m_CurrentVehicleRemoveTypes,
			m_CurrentVehicleHumanAddTypes = m_CurrentVehicleHumanAddTypes,
			m_CurrentVehicleAnimalAddTypes = m_CurrentVehicleAnimalAddTypes,
			m_HumanSpawnTypes = m_HumanSpawnTypes,
			m_AnimalSpawnTypes = m_AnimalSpawnTypes,
			m_CreaturePrefabChunks = creaturePrefabChunks,
			m_EmergeQueue = emergeQueue,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		emergeQueue.Dispose(jobHandle);
		creaturePrefabChunks.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	public static Entity SelectResidentPrefab(Citizen citizenData, NativeList<ArchetypeChunk> chunks, EntityTypeHandle entityType, ref ComponentTypeHandle<CreatureData> creatureType, ref ComponentTypeHandle<ResidentData> residentType, out CreatureData creatureData, out PseudoRandomSeed randomSeed)
	{
		Random random = citizenData.GetPseudoRandom(CitizenPseudoRandom.SpawnResident);
		GenderMask genderMask = (((citizenData.m_State & CitizenFlags.Male) == 0) ? GenderMask.Female : GenderMask.Male);
		Game.Prefabs.AgeMask ageMask = citizenData.GetAge() switch
		{
			CitizenAge.Child => Game.Prefabs.AgeMask.Child, 
			CitizenAge.Teen => Game.Prefabs.AgeMask.Teen, 
			CitizenAge.Adult => Game.Prefabs.AgeMask.Adult, 
			CitizenAge.Elderly => Game.Prefabs.AgeMask.Elderly, 
			_ => (Game.Prefabs.AgeMask)0, 
		};
		Entity result = Entity.Null;
		int totalProbability = 0;
		creatureData = default(CreatureData);
		randomSeed = new PseudoRandomSeed(ref random);
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityType);
			NativeArray<CreatureData> nativeArray2 = archetypeChunk.GetNativeArray(ref creatureType);
			NativeArray<ResidentData> nativeArray3 = archetypeChunk.GetNativeArray(ref residentType);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				CreatureData creatureData2 = nativeArray2[j];
				ResidentData residentData = nativeArray3[j];
				if ((creatureData2.m_Gender & genderMask) == genderMask && (residentData.m_Age & ageMask) == ageMask)
				{
					int probability = 100;
					if (SelectItem(ref random, probability, ref totalProbability))
					{
						result = nativeArray[j];
						creatureData = creatureData2;
					}
				}
			}
		}
		return result;
	}

	public static Entity SelectAnimalPrefab(ref Random random, PetType petType, NativeList<ArchetypeChunk> chunks, EntityTypeHandle entityType, ComponentTypeHandle<PetData> petDataType, out PseudoRandomSeed randomSeed)
	{
		int totalProbability = 0;
		Entity result = Entity.Null;
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityType);
			NativeArray<PetData> nativeArray2 = archetypeChunk.GetNativeArray(ref petDataType);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				if (nativeArray2[j].m_Type == petType && SelectItem(ref random, 100, ref totalProbability))
				{
					result = nativeArray[j];
				}
			}
		}
		randomSeed = new PseudoRandomSeed(ref random);
		return result;
	}

	private static bool SelectItem(ref Random random, int probability, ref int totalProbability)
	{
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
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
	public ObjectEmergeSystem()
	{
	}
}
