using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Net;
using Game.Objects;
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

namespace Game.Simulation;

[CompilerGenerated]
public class PetAISystem : GameSystemBase
{
	private struct Boarding
	{
		public Entity m_Passenger;

		public Entity m_Vehicle;

		public Entity m_Leader;

		public AnimalCurrentLane m_CurrentLane;

		public CreatureVehicleFlags m_Flags;

		public float3 m_Position;

		public BoardingType m_Type;

		public static Boarding ExitVehicle(Entity passenger, Entity vehicle, Entity leader, AnimalCurrentLane newCurrentLane, float3 position)
		{
			return new Boarding
			{
				m_Passenger = passenger,
				m_Vehicle = vehicle,
				m_Leader = leader,
				m_CurrentLane = newCurrentLane,
				m_Position = position,
				m_Type = BoardingType.Exit
			};
		}

		public static Boarding TryEnterVehicle(Entity passenger, Entity vehicle, CreatureVehicleFlags flags)
		{
			return new Boarding
			{
				m_Passenger = passenger,
				m_Vehicle = vehicle,
				m_Flags = flags,
				m_Type = BoardingType.TryEnter
			};
		}

		public static Boarding FinishEnterVehicle(Entity passenger, Entity vehicle, AnimalCurrentLane oldCurrentLane)
		{
			return new Boarding
			{
				m_Passenger = passenger,
				m_Vehicle = vehicle,
				m_CurrentLane = oldCurrentLane,
				m_Type = BoardingType.FinishEnter
			};
		}

		public static Boarding CancelEnterVehicle(Entity passenger, Entity vehicle)
		{
			return new Boarding
			{
				m_Passenger = passenger,
				m_Vehicle = vehicle,
				m_Type = BoardingType.CancelEnter
			};
		}
	}

	private enum BoardingType
	{
		Exit,
		TryEnter,
		FinishEnter,
		CancelEnter
	}

	[BurstCompile]
	private struct PetTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> m_GroupCreatureType;

		public ComponentTypeHandle<Animal> m_AnimalType;

		public ComponentTypeHandle<Game.Creatures.Pet> m_PetType;

		public ComponentTypeHandle<Creature> m_CreatureType;

		public ComponentTypeHandle<AnimalCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> m_TaxiData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<PathOwner> m_PathOwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_PrefabActivityLocationElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public EntityArchetype m_ResetTripArchetype;

		public NativeQueue<Boarding>.ParallelWriter m_BoardingQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Creatures.Pet> nativeArray3 = chunk.GetNativeArray(ref m_PetType);
			NativeArray<Creature> nativeArray4 = chunk.GetNativeArray(ref m_CreatureType);
			NativeArray<Target> nativeArray5 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<GroupMember> nativeArray6 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<CurrentVehicle> nativeArray7 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<AnimalCurrentLane> nativeArray8 = chunk.GetNativeArray(ref m_CurrentLaneType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (nativeArray7.Length != 0)
			{
				if (nativeArray6.Length != 0)
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Entity entity = nativeArray[i];
						PrefabRef prefabRef = nativeArray2[i];
						Game.Creatures.Pet pet = nativeArray3[i];
						Creature creature = nativeArray4[i];
						CurrentVehicle currentVehicle = nativeArray7[i];
						Target target = nativeArray5[i];
						GroupMember groupMember = nativeArray6[i];
						CollectionUtils.TryGet(nativeArray8, i, out var value);
						TickGroupMemberInVehicle(ref random, unfilteredChunkIndex, entity, prefabRef, groupMember, currentVehicle, nativeArray8.Length != 0, ref pet, ref value, ref target);
						TickQueue(ref creature, ref value);
						nativeArray3[i] = pet;
						nativeArray4[i] = creature;
						nativeArray5[i] = target;
						CollectionUtils.TrySet(nativeArray8, i, value);
					}
					return;
				}
				BufferAccessor<GroupCreature> bufferAccessor = chunk.GetBufferAccessor(ref m_GroupCreatureType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					PrefabRef prefabRef2 = nativeArray2[j];
					Game.Creatures.Pet pet2 = nativeArray3[j];
					Creature creature2 = nativeArray4[j];
					CurrentVehicle currentVehicle2 = nativeArray7[j];
					Target target2 = nativeArray5[j];
					CollectionUtils.TryGet(nativeArray8, j, out var value2);
					CollectionUtils.TryGet(bufferAccessor, j, out var value3);
					TickInVehicle(ref random, unfilteredChunkIndex, entity2, prefabRef2, currentVehicle2, nativeArray8.Length != 0, ref pet2, ref value2, ref target2, value3);
					TickQueue(ref creature2, ref value2);
					nativeArray3[j] = pet2;
					nativeArray4[j] = creature2;
					nativeArray5[j] = target2;
					CollectionUtils.TrySet(nativeArray8, j, value2);
				}
				return;
			}
			NativeArray<Animal> nativeArray9 = chunk.GetNativeArray(ref m_AnimalType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			if (nativeArray6.Length != 0)
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity3 = nativeArray[k];
					PrefabRef prefabRef3 = nativeArray2[k];
					Animal animal = nativeArray9[k];
					Game.Creatures.Pet pet3 = nativeArray3[k];
					Creature creature3 = nativeArray4[k];
					Target target3 = nativeArray5[k];
					GroupMember groupMember2 = nativeArray6[k];
					CollectionUtils.TryGet(nativeArray8, k, out var value4);
					CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity3, value4, animal, isUnspawned, m_CommandBuffer);
					TickGroupMemberWalking(unfilteredChunkIndex, entity3, prefabRef3, groupMember2, ref pet3, ref creature3, ref value4, ref target3);
					TickQueue(ref creature3, ref value4);
					nativeArray9[k] = animal;
					nativeArray3[k] = pet3;
					nativeArray4[k] = creature3;
					nativeArray5[k] = target3;
					CollectionUtils.TrySet(nativeArray8, k, value4);
				}
			}
			else
			{
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Entity entity4 = nativeArray[l];
					PrefabRef prefabRef4 = nativeArray2[l];
					Animal animal2 = nativeArray9[l];
					Game.Creatures.Pet pet4 = nativeArray3[l];
					Creature creature4 = nativeArray4[l];
					Target target4 = nativeArray5[l];
					CollectionUtils.TryGet(nativeArray8, l, out var value5);
					CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity4, value5, animal2, isUnspawned, m_CommandBuffer);
					TickWalking(unfilteredChunkIndex, entity4, prefabRef4, ref pet4, ref creature4, ref value5, ref target4);
					TickQueue(ref creature4, ref value5);
					nativeArray9[l] = animal2;
					nativeArray3[l] = pet4;
					nativeArray4[l] = creature4;
					nativeArray5[l] = target4;
					CollectionUtils.TrySet(nativeArray8, l, value5);
				}
			}
		}

		private void TickGroupMemberInVehicle(ref Random random, int jobIndex, Entity entity, PrefabRef prefabRef, GroupMember groupMember, CurrentVehicle currentVehicle, bool hasCurrentLane, ref Game.Creatures.Pet pet, ref AnimalCurrentLane currentLane, ref Target target)
		{
			if (!m_PrefabRefData.HasComponent(currentVehicle.m_Vehicle))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return;
			}
			Entity entity2 = currentVehicle.m_Vehicle;
			if (m_ControllerData.HasComponent(currentVehicle.m_Vehicle))
			{
				Controller controller = m_ControllerData[currentVehicle.m_Vehicle];
				if (controller.m_Controller != Entity.Null)
				{
					entity2 = controller.m_Controller;
				}
			}
			if ((currentVehicle.m_Flags & CreatureVehicleFlags.Ready) == 0)
			{
				if (hasCurrentLane)
				{
					if (CreatureUtils.IsStuck(currentLane))
					{
						m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
						return;
					}
					if (!m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
					{
						CancelEnterVehicle(entity, currentVehicle.m_Vehicle, ref currentLane);
						return;
					}
					if (m_PublicTransportData.TryGetComponent(entity2, out var componentData) && (componentData.m_State & PublicTransportFlags.Boarding) == 0 && currentLane.m_Lane == currentVehicle.m_Vehicle)
					{
						currentLane.m_Flags |= CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached;
					}
					m_ResidentData.TryGetComponent(groupMember.m_Leader, out var componentData2);
					if (CreatureUtils.PathEndReached(currentLane) || componentData2.m_Timer >= 250)
					{
						FinishEnterVehicle(entity, currentVehicle.m_Vehicle, ref currentLane);
						hasCurrentLane = false;
					}
				}
				if (!hasCurrentLane)
				{
					currentVehicle.m_Flags |= CreatureVehicleFlags.Ready;
					m_CommandBuffer.SetComponent(jobIndex, entity, currentVehicle);
				}
			}
			else
			{
				if ((pet.m_Flags & PetFlags.Disembarking) == 0 && !m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
				{
					GroupLeaderDisembarking(entity, ref pet);
				}
				if ((pet.m_Flags & PetFlags.Disembarking) != PetFlags.None)
				{
					ExitVehicle(ref random, entity, entity2, prefabRef, currentVehicle, groupMember);
				}
			}
		}

		private void TickInVehicle(ref Random random, int jobIndex, Entity entity, PrefabRef prefabRef, CurrentVehicle currentVehicle, bool hasCurrentLane, ref Game.Creatures.Pet pet, ref AnimalCurrentLane currentLane, ref Target target, DynamicBuffer<GroupCreature> groupCreatures)
		{
			if (!m_PrefabRefData.HasComponent(currentVehicle.m_Vehicle))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return;
			}
			Entity entity2 = currentVehicle.m_Vehicle;
			if (m_ControllerData.HasComponent(currentVehicle.m_Vehicle))
			{
				Controller controller = m_ControllerData[currentVehicle.m_Vehicle];
				if (controller.m_Controller != Entity.Null)
				{
					entity2 = controller.m_Controller;
				}
			}
			if ((currentVehicle.m_Flags & CreatureVehicleFlags.Ready) == 0)
			{
				if (hasCurrentLane)
				{
					if (CreatureUtils.IsStuck(currentLane))
					{
						m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
						return;
					}
					if (m_PublicTransportData.TryGetComponent(entity2, out var componentData) && (componentData.m_State & PublicTransportFlags.Boarding) == 0 && currentLane.m_Lane == currentVehicle.m_Vehicle)
					{
						currentLane.m_Flags |= CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached;
					}
					if (CreatureUtils.PathEndReached(currentLane))
					{
						FinishEnterVehicle(entity, currentVehicle.m_Vehicle, ref currentLane);
						hasCurrentLane = false;
					}
				}
				if (!hasCurrentLane && HasEveryoneBoarded(groupCreatures))
				{
					currentVehicle.m_Flags |= CreatureVehicleFlags.Ready;
					m_CommandBuffer.SetComponent(jobIndex, entity, currentVehicle);
				}
				return;
			}
			if ((pet.m_Flags & PetFlags.Disembarking) == 0)
			{
				if (m_DestroyedData.HasComponent(entity2))
				{
					if (!m_MovingData.HasComponent(entity2))
					{
						pet.m_Flags |= PetFlags.Disembarking;
					}
				}
				else if (m_PersonalCarData.HasComponent(entity2))
				{
					if ((m_PersonalCarData[entity2].m_State & PersonalCarFlags.Disembarking) != 0)
					{
						CurrentVehicleDisembarking(jobIndex, entity, entity2, ref pet, ref target);
					}
				}
				else if (m_PublicTransportData.HasComponent(entity2))
				{
					if ((m_PublicTransportData[entity2].m_State & PublicTransportFlags.Boarding) != 0)
					{
						CurrentVehicleBoarding(jobIndex, entity, entity2, ref pet, ref target);
					}
				}
				else if (m_TaxiData.HasComponent(entity2))
				{
					if ((m_TaxiData[entity2].m_State & TaxiFlags.Disembarking) != 0)
					{
						CurrentVehicleDisembarking(jobIndex, entity, entity2, ref pet, ref target);
					}
				}
				else if (m_PoliceCarData.HasComponent(entity2) && (m_PoliceCarData[entity2].m_State & PoliceCarFlags.Disembarking) != 0)
				{
					CurrentVehicleDisembarking(jobIndex, entity, entity2, ref pet, ref target);
				}
			}
			if ((pet.m_Flags & PetFlags.Disembarking) != PetFlags.None)
			{
				ExitVehicle(ref random, entity, entity2, prefabRef, currentVehicle, default(GroupMember));
			}
		}

		private void TickGroupMemberWalking(int jobIndex, Entity entity, PrefabRef prefabRef, GroupMember groupMember, ref Game.Creatures.Pet pet, ref Creature creature, ref AnimalCurrentLane currentLane, ref Target target)
		{
			if ((pet.m_Flags & PetFlags.Disembarking) != PetFlags.None)
			{
				pet.m_Flags &= ~PetFlags.Disembarking;
			}
			else if (!m_PrefabRefData.HasComponent(target.m_Target) || CreatureUtils.IsStuck(currentLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return;
			}
			if (m_CurrentVehicleData.HasComponent(groupMember.m_Leader) && (currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
			{
				CurrentVehicle currentVehicle = m_CurrentVehicleData[groupMember.m_Leader];
				m_BoardingQueue.Enqueue(Boarding.TryEnterVehicle(entity, currentVehicle.m_Vehicle, (CreatureVehicleFlags)0u));
			}
		}

		private void TickWalking(int jobIndex, Entity entity, PrefabRef prefabRef, ref Game.Creatures.Pet pet, ref Creature creature, ref AnimalCurrentLane currentLane, ref Target target)
		{
			if (!CheckTarget(entity, ref currentLane, ref target))
			{
				if ((pet.m_Flags & PetFlags.Disembarking) != PetFlags.None)
				{
					pet.m_Flags &= ~PetFlags.Disembarking;
				}
				else if (!m_PrefabRefData.HasComponent(target.m_Target) || CreatureUtils.IsStuck(currentLane))
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				}
				else if (CreatureUtils.PathEndReached(currentLane))
				{
					PathEndReached(jobIndex, entity, ref pet, ref currentLane, ref target);
				}
			}
		}

		private void TickQueue(ref Creature creature, ref AnimalCurrentLane currentLane)
		{
			creature.m_QueueEntity = currentLane.m_QueueEntity;
			creature.m_QueueArea = currentLane.m_QueueArea;
		}

		private void ExitVehicle(ref Random random, Entity entity, Entity controllerVehicle, PrefabRef prefabRef, CurrentVehicle currentVehicle, GroupMember groupMember)
		{
			if (m_TransformData.HasComponent(currentVehicle.m_Vehicle))
			{
				Transform vehicleTransform = m_TransformData[currentVehicle.m_Vehicle];
				float3 position = m_TransformData[entity].m_Position;
				m_PseudoRandomSeedData.TryGetComponent(entity, out var componentData);
				BufferLookup<SubMeshGroup> subMeshGroupBuffers = default(BufferLookup<SubMeshGroup>);
				BufferLookup<CharacterElement> characterElementBuffers = default(BufferLookup<CharacterElement>);
				BufferLookup<SubMesh> subMeshBuffers = default(BufferLookup<SubMesh>);
				BufferLookup<AnimationClip> animationClipBuffers = default(BufferLookup<AnimationClip>);
				BufferLookup<AnimationMotion> animationMotionBuffers = default(BufferLookup<AnimationMotion>);
				ActivityMask activityMask;
				AnimatedPropID propID;
				float3 position2 = CreatureUtils.GetVehicleDoorPosition(ref random, ActivityType.Exit, (ActivityCondition)0u, vehicleTransform, componentData, position, isDriver: false, m_LefthandTraffic, prefabRef.m_Prefab, currentVehicle.m_Vehicle, default(DynamicBuffer<MeshGroup>), ref m_PublicTransportData, ref m_TrainData, ref m_ControllerData, ref m_PrefabRefData, ref m_PrefabCarData, ref m_PrefabActivityLocationElements, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, ref animationMotionBuffers, out activityMask, out propID).m_Position;
				AnimalCurrentLane newCurrentLane = default(AnimalCurrentLane);
				if (m_UnspawnedData.HasComponent(currentVehicle.m_Vehicle))
				{
					newCurrentLane.m_Flags |= CreatureLaneFlags.EmergeUnspawned;
				}
				if (m_PathOwnerData.TryGetComponent(controllerVehicle, out var componentData2) && VehicleUtils.PathfindFailed(componentData2))
				{
					newCurrentLane.m_Flags |= CreatureLaneFlags.Stuck | CreatureLaneFlags.EmergeUnspawned;
				}
				m_BoardingQueue.Enqueue(Boarding.ExitVehicle(entity, currentVehicle.m_Vehicle, groupMember.m_Leader, newCurrentLane, position2));
			}
			else
			{
				float3 position3 = m_TransformData[entity].m_Position;
				m_BoardingQueue.Enqueue(Boarding.ExitVehicle(entity, currentVehicle.m_Vehicle, groupMember.m_Leader, default(AnimalCurrentLane), position3));
			}
		}

		private bool HasEveryoneBoarded(DynamicBuffer<GroupCreature> group)
		{
			if (group.IsCreated)
			{
				for (int i = 0; i < group.Length; i++)
				{
					Entity creature = group[i].m_Creature;
					if (!m_CurrentVehicleData.HasComponent(creature))
					{
						return false;
					}
					if ((m_CurrentVehicleData[creature].m_Flags & CreatureVehicleFlags.Ready) == 0)
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool CheckTarget(Entity entity, ref AnimalCurrentLane currentLane, ref Target target)
		{
			if (m_VehicleData.HasComponent(target.m_Target))
			{
				Entity entity2 = target.m_Target;
				if (m_ControllerData.HasComponent(target.m_Target))
				{
					Controller controller = m_ControllerData[target.m_Target];
					if (controller.m_Controller != Entity.Null)
					{
						entity2 = controller.m_Controller;
					}
				}
				if (m_PublicTransportData.HasComponent(entity2))
				{
					if ((m_PublicTransportData[entity2].m_State & PublicTransportFlags.Boarding) != 0 && m_OwnerData.HasComponent(entity2))
					{
						Owner owner = m_OwnerData[entity2];
						if (m_BuildingData.HasComponent(owner.m_Owner))
						{
							TryEnterVehicle(entity, target.m_Target, ref currentLane);
							target.m_Target = owner.m_Owner;
							return true;
						}
					}
				}
				else if (m_PoliceCarData.HasComponent(entity2) && (m_PoliceCarData[entity2].m_State & PoliceCarFlags.AtTarget) != 0 && m_OwnerData.HasComponent(entity2))
				{
					Owner owner2 = m_OwnerData[entity2];
					if (m_BuildingData.HasComponent(owner2.m_Owner))
					{
						TryEnterVehicle(entity, target.m_Target, ref currentLane);
						target.m_Target = owner2.m_Owner;
						return true;
					}
				}
			}
			return false;
		}

		private void CurrentVehicleBoarding(int jobIndex, Entity entity, Entity controllerVehicle, ref Game.Creatures.Pet pet, ref Target target)
		{
			Game.Vehicles.PublicTransport publicTransport = m_PublicTransportData[controllerVehicle];
			if ((publicTransport.m_State & (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) == 0 || (publicTransport.m_State & PublicTransportFlags.Returning) != 0)
			{
				pet.m_Flags |= PetFlags.Disembarking;
			}
		}

		private void CurrentVehicleDisembarking(int jobIndex, Entity entity, Entity controllerVehicle, ref Game.Creatures.Pet pet, ref Target target)
		{
			pet.m_Flags |= PetFlags.Disembarking;
		}

		private void GroupLeaderDisembarking(Entity entity, ref Game.Creatures.Pet pet)
		{
			pet.m_Flags |= PetFlags.Disembarking;
		}

		private bool PathEndReached(int jobIndex, Entity entity, ref Game.Creatures.Pet pet, ref AnimalCurrentLane currentLane, ref Target target)
		{
			if (m_VehicleData.HasComponent(target.m_Target))
			{
				if (m_OwnerData.HasComponent(target.m_Target))
				{
					Owner owner = m_OwnerData[target.m_Target];
					if (m_BuildingData.HasComponent(owner.m_Owner))
					{
						target.m_Target = owner.m_Owner;
						return false;
					}
				}
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return true;
			}
			if ((pet.m_Flags & (PetFlags.Arrived | PetFlags.LeaderArrived)) == 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return true;
			}
			Entity entity2 = target.m_Target;
			if (m_PropertyRenterData.HasComponent(entity2))
			{
				entity2 = m_PropertyRenterData[entity2].m_Property;
			}
			if (m_OnFireData.HasComponent(entity2) || m_DestroyedData.HasComponent(entity2))
			{
				return false;
			}
			if ((currentLane.m_Flags & CreatureLaneFlags.Hangaround) != 0)
			{
				pet.m_Flags |= PetFlags.Hangaround;
			}
			else
			{
				pet.m_Flags &= ~PetFlags.Hangaround;
			}
			if (m_PrefabRefData.HasComponent(pet.m_HouseholdPet))
			{
				if ((pet.m_Flags & PetFlags.Hangaround) == 0)
				{
					m_CommandBuffer.RemoveComponent<CurrentTransport>(jobIndex, pet.m_HouseholdPet);
				}
				if ((pet.m_Flags & PetFlags.Arrived) == 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, pet.m_HouseholdPet, new CurrentBuilding(entity2));
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ResetTrip
					{
						m_Creature = entity,
						m_Target = target.m_Target
					});
					pet.m_Flags |= PetFlags.Arrived;
				}
			}
			if ((pet.m_Flags & PetFlags.Hangaround) != PetFlags.None)
			{
				return false;
			}
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
			return true;
		}

		private void TryEnterVehicle(Entity entity, Entity vehicle, ref AnimalCurrentLane currentLane)
		{
			m_BoardingQueue.Enqueue(Boarding.TryEnterVehicle(entity, vehicle, CreatureVehicleFlags.Leader));
		}

		private void FinishEnterVehicle(Entity entity, Entity vehicle, ref AnimalCurrentLane currentLane)
		{
			m_BoardingQueue.Enqueue(Boarding.FinishEnterVehicle(entity, vehicle, currentLane));
		}

		private void CancelEnterVehicle(Entity entity, Entity vehicle, ref AnimalCurrentLane currentLane)
		{
			m_BoardingQueue.Enqueue(Boarding.CancelEnterVehicle(entity, vehicle));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct BoardingJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLanes;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		public ComponentLookup<Creature> m_Creatures;

		public BufferLookup<Passenger> m_Passengers;

		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public ComponentTypeSet m_CurrentLaneTypes;

		public NativeQueue<Boarding> m_BoardingQueue;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Boarding item;
			while (m_BoardingQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case BoardingType.Exit:
					ExitVehicle(item.m_Passenger, item.m_Vehicle, item.m_Leader, item.m_CurrentLane, item.m_Position);
					break;
				case BoardingType.TryEnter:
					TryEnterVehicle(item.m_Passenger, item.m_Vehicle, item.m_Flags);
					break;
				case BoardingType.FinishEnter:
					FinishEnterVehicle(item.m_Passenger, item.m_Vehicle, item.m_CurrentLane);
					break;
				case BoardingType.CancelEnter:
					CancelEnterVehicle(item.m_Passenger, item.m_Vehicle);
					break;
				}
			}
		}

		private void ExitVehicle(Entity passenger, Entity vehicle, Entity leader, AnimalCurrentLane newCurrentLane, float3 position)
		{
			if (m_Passengers.HasBuffer(vehicle))
			{
				CollectionUtils.RemoveValue(m_Passengers[vehicle], new Passenger(passenger));
			}
			m_CommandBuffer.RemoveComponent<CurrentVehicle>(passenger);
			if (newCurrentLane.m_Lane == Entity.Null && m_HumanCurrentLanes.TryGetComponent(leader, out var componentData))
			{
				newCurrentLane.m_Lane = componentData.m_Lane;
				newCurrentLane.m_CurvePosition = componentData.m_CurvePosition;
				newCurrentLane.m_Flags |= (CreatureLaneFlags)((uint)componentData.m_Flags & 0xFFDFFDECu);
			}
			if (m_LaneObjects.HasBuffer(newCurrentLane.m_Lane))
			{
				NetUtils.AddLaneObject(m_LaneObjects[newCurrentLane.m_Lane], passenger, newCurrentLane.m_CurvePosition.x);
			}
			else
			{
				PrefabRef prefabRef = m_PrefabRefData[passenger];
				ObjectGeometryData geometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
				Bounds3 bounds = ObjectUtils.CalculateBounds(position, quaternion.identity, geometryData);
				m_SearchTree.Add(passenger, new QuadTreeBoundsXZ(bounds));
			}
			m_CommandBuffer.AddComponent(passenger, in m_CurrentLaneTypes);
			m_CommandBuffer.AddComponent(passenger, default(Updated));
			m_CommandBuffer.SetComponent(passenger, newCurrentLane);
			m_CommandBuffer.SetComponent(passenger, new Transform(position, quaternion.identity));
		}

		private void TryEnterVehicle(Entity passenger, Entity vehicle, CreatureVehicleFlags flags)
		{
			if (m_Passengers.HasBuffer(vehicle))
			{
				m_Passengers[vehicle].Add(new Passenger(passenger));
			}
			m_CommandBuffer.AddComponent(passenger, new CurrentVehicle(vehicle, flags));
		}

		private void CancelEnterVehicle(Entity passenger, Entity vehicle)
		{
			if (m_Passengers.HasBuffer(vehicle))
			{
				CollectionUtils.RemoveValue(m_Passengers[vehicle], new Passenger(passenger));
			}
			m_CommandBuffer.RemoveComponent<CurrentVehicle>(passenger);
		}

		private void FinishEnterVehicle(Entity passenger, Entity vehicle, AnimalCurrentLane oldCurrentLane)
		{
			Creature value = m_Creatures[passenger];
			value.m_QueueEntity = Entity.Null;
			value.m_QueueArea = default(Sphere3);
			m_Creatures[passenger] = value;
			if (m_LaneObjects.HasBuffer(oldCurrentLane.m_Lane))
			{
				NetUtils.RemoveLaneObject(m_LaneObjects[oldCurrentLane.m_Lane], passenger);
			}
			else
			{
				m_SearchTree.TryRemove(passenger);
			}
			m_CommandBuffer.RemoveComponent(passenger, in m_CurrentLaneTypes);
			m_CommandBuffer.AddComponent(passenger, default(Unspawned));
			m_CommandBuffer.AddComponent(passenger, default(Updated));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<GroupCreature> __Game_Creatures_GroupCreature_RO_BufferTypeHandle;

		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Creatures.Pet> __Game_Creatures_Pet_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		public ComponentLookup<Creature> __Game_Creatures_Creature_RW_ComponentLookup;

		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RW_BufferLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Creatures_GroupCreature_RO_BufferTypeHandle = state.GetBufferTypeHandle<GroupCreature>(isReadOnly: true);
			__Game_Creatures_Animal_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>();
			__Game_Creatures_Pet_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Pet>();
			__Game_Creatures_Creature_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PoliceCar>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentLookup = state.GetComponentLookup<PathOwner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Creatures_Creature_RW_ComponentLookup = state.GetComponentLookup<Creature>();
			__Game_Vehicles_Passenger_RW_BufferLookup = state.GetBufferLookup<Passenger>();
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_CreatureQuery;

	private EntityArchetype m_ResetTripArchetype;

	private ComponentTypeSet m_CurrentLaneTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 5;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Creatures.Pet>(), ComponentType.ReadOnly<Animal>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		m_ResetTripArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ResetTrip>());
		m_CurrentLaneTypes = new ComponentTypeSet(new ComponentType[6]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<AnimalNavigation>(),
			ComponentType.ReadWrite<AnimalCurrentLane>(),
			ComponentType.ReadWrite<Blocker>()
		});
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Boarding> boardingQueue = new NativeQueue<Boarding>(Allocator.TempJob);
		PetTickJob jobData = new PetTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupCreatureType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_GroupCreature_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Pet_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PoliceCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabActivityLocationElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_ResetTripArchetype = m_ResetTripArchetype,
			m_BoardingQueue = boardingQueue.AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle dependencies;
		BoardingJob jobData2 = new BoardingJob
		{
			m_HumanCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Creatures = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RW_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_CurrentLaneTypes = m_CurrentLaneTypes,
			m_BoardingQueue = boardingQueue,
			m_SearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: false, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_CreatureQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(job, dependencies));
		boardingQueue.Dispose(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public PetAISystem()
	{
	}
}
