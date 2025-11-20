using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Creatures;

[CompilerGenerated]
public class ReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateCreatureReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Resident> m_ResidentType;

		[ReadOnly]
		public ComponentTypeHandle<Pet> m_PetType;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		public ComponentLookup<CurrentTransport> m_CurrentTransports;

		public BufferLookup<OwnedCreature> m_OwnedCreatures;

		public BufferLookup<LaneObject> m_LaneObjects;

		public BufferLookup<Passenger> m_Passengers;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
				for (int i = 0; i < nativeArray4.Length; i++)
				{
					Entity creature = nativeArray[i];
					Owner owner = nativeArray4[i];
					if (m_OwnedCreatures.HasBuffer(owner.m_Owner))
					{
						m_OwnedCreatures[owner.m_Owner].Add(new OwnedCreature(creature));
					}
				}
				NativeArray<Resident> nativeArray5 = chunk.GetNativeArray(ref m_ResidentType);
				for (int j = 0; j < nativeArray5.Length; j++)
				{
					Entity currentTransport = nativeArray[j];
					Resident resident = nativeArray5[j];
					if (m_CurrentTransports.HasComponent(resident.m_Citizen))
					{
						CurrentTransport value = m_CurrentTransports[resident.m_Citizen];
						value.m_CurrentTransport = currentTransport;
						m_CurrentTransports[resident.m_Citizen] = value;
					}
				}
				NativeArray<Pet> nativeArray6 = chunk.GetNativeArray(ref m_PetType);
				for (int k = 0; k < nativeArray6.Length; k++)
				{
					Entity currentTransport2 = nativeArray[k];
					Pet pet = nativeArray6[k];
					if (m_CurrentTransports.HasComponent(pet.m_HouseholdPet))
					{
						CurrentTransport value2 = m_CurrentTransports[pet.m_HouseholdPet];
						value2.m_CurrentTransport = currentTransport2;
						m_CurrentTransports[pet.m_HouseholdPet] = value2;
					}
				}
				NativeArray<CurrentVehicle> nativeArray7 = chunk.GetNativeArray(ref m_CurrentVehicleType);
				for (int l = 0; l < nativeArray7.Length; l++)
				{
					Entity passenger = nativeArray[l];
					CurrentVehicle currentVehicle = nativeArray7[l];
					if (m_Passengers.HasBuffer(currentVehicle.m_Vehicle))
					{
						m_Passengers[currentVehicle.m_Vehicle].Add(new Passenger(passenger));
					}
				}
				NativeArray<HumanCurrentLane> nativeArray8 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
				for (int m = 0; m < nativeArray8.Length; m++)
				{
					Entity entity = nativeArray[m];
					HumanCurrentLane humanCurrentLane = nativeArray8[m];
					if (m_LaneObjects.HasBuffer(humanCurrentLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[humanCurrentLane.m_Lane], entity, humanCurrentLane.m_CurvePosition.xx);
						continue;
					}
					Transform transform = nativeArray2[m];
					PrefabRef prefabRef = nativeArray3[m];
					ObjectGeometryData geometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
					Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
					m_SearchTree.Add(entity, new QuadTreeBoundsXZ(bounds));
				}
				NativeArray<AnimalCurrentLane> nativeArray9 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
				for (int n = 0; n < nativeArray9.Length; n++)
				{
					Entity entity2 = nativeArray[n];
					AnimalCurrentLane animalCurrentLane = nativeArray9[n];
					if (m_LaneObjects.HasBuffer(animalCurrentLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[animalCurrentLane.m_Lane], entity2, animalCurrentLane.m_CurvePosition.xx);
						continue;
					}
					Transform transform2 = nativeArray2[n];
					PrefabRef prefabRef2 = nativeArray3[n];
					ObjectGeometryData geometryData2 = m_ObjectGeometryData[prefabRef2.m_Prefab];
					Bounds3 bounds2 = ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, geometryData2);
					m_SearchTree.Add(entity2, new QuadTreeBoundsXZ(bounds2));
				}
				return;
			}
			NativeArray<Owner> nativeArray10 = chunk.GetNativeArray(ref m_OwnerType);
			if (nativeArray10.Length != 0)
			{
				chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num = 0; num < nativeArray10.Length; num++)
				{
					Entity creature2 = nativeArray[num];
					Owner owner2 = nativeArray10[num];
					if (m_OwnedCreatures.HasBuffer(owner2.m_Owner))
					{
						CollectionUtils.RemoveValue(m_OwnedCreatures[owner2.m_Owner], new OwnedCreature(creature2));
					}
				}
			}
			NativeArray<Resident> nativeArray11 = chunk.GetNativeArray(ref m_ResidentType);
			for (int num2 = 0; num2 < nativeArray11.Length; num2++)
			{
				Entity entity3 = nativeArray[num2];
				Resident resident2 = nativeArray11[num2];
				if (m_CurrentTransports.HasComponent(resident2.m_Citizen) && m_CurrentTransports[resident2.m_Citizen].m_CurrentTransport == entity3)
				{
					m_CommandBuffer.RemoveComponent<CurrentTransport>(resident2.m_Citizen);
				}
			}
			NativeArray<Pet> nativeArray12 = chunk.GetNativeArray(ref m_PetType);
			for (int num3 = 0; num3 < nativeArray12.Length; num3++)
			{
				Entity entity4 = nativeArray[num3];
				Pet pet2 = nativeArray12[num3];
				if (m_CurrentTransports.HasComponent(pet2.m_HouseholdPet) && m_CurrentTransports[pet2.m_HouseholdPet].m_CurrentTransport == entity4)
				{
					m_CommandBuffer.RemoveComponent<CurrentTransport>(pet2.m_HouseholdPet);
				}
			}
			NativeArray<CurrentVehicle> nativeArray13 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			if (nativeArray13.Length != 0)
			{
				for (int num4 = 0; num4 < nativeArray13.Length; num4++)
				{
					Entity passenger2 = nativeArray[num4];
					CurrentVehicle currentVehicle2 = nativeArray13[num4];
					if (m_Passengers.HasBuffer(currentVehicle2.m_Vehicle))
					{
						CollectionUtils.RemoveValue(m_Passengers[currentVehicle2.m_Vehicle], new Passenger(passenger2));
					}
				}
			}
			NativeArray<HumanCurrentLane> nativeArray14 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			for (int num5 = 0; num5 < nativeArray14.Length; num5++)
			{
				Entity entity5 = nativeArray[num5];
				HumanCurrentLane humanCurrentLane2 = nativeArray14[num5];
				if (m_LaneObjects.HasBuffer(humanCurrentLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[humanCurrentLane2.m_Lane], entity5);
				}
				else
				{
					m_SearchTree.TryRemove(entity5);
				}
			}
			NativeArray<AnimalCurrentLane> nativeArray15 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			for (int num6 = 0; num6 < nativeArray15.Length; num6++)
			{
				Entity entity6 = nativeArray[num6];
				AnimalCurrentLane animalCurrentLane2 = nativeArray15[num6];
				if (m_LaneObjects.HasBuffer(animalCurrentLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[animalCurrentLane2.m_Lane], entity6);
				}
				else
				{
					m_SearchTree.TryRemove(entity6);
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
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Pet> __Game_Creatures_Pet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RW_ComponentLookup;

		public BufferLookup<OwnedCreature> __Game_Creatures_OwnedCreature_RW_BufferLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Resident>(isReadOnly: true);
			__Game_Creatures_Pet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Pet>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RW_ComponentLookup = state.GetComponentLookup<CurrentTransport>();
			__Game_Creatures_OwnedCreature_RW_BufferLookup = state.GetBufferLookup<OwnedCreature>();
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
			__Game_Vehicles_Passenger_RW_BufferLookup = state.GetBufferLookup<Passenger>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private Game.Objects.SearchSystem m_SearchSystem;

	private EntityQuery m_CreatureQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_CreatureQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Creature>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new UpdateCreatureReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Pet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedCreatures = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Creatures_OwnedCreature_RW_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RW_BufferLookup, ref base.CheckedStateRef),
			m_SearchTree = m_SearchSystem.GetMovingSearchTree(readOnly: false, out dependencies),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, m_CreatureQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_SearchSystem.AddMovingSearchTreeWriter(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public ReferencesSystem()
	{
	}
}
