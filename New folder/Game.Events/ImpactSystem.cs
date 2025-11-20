using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class ImpactSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddImpactJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Impact> m_ImpactType;

		[ReadOnly]
		public ComponentLookup<Stopped> m_StoppedData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<CarTrailer> m_CarTrailerData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<CarTrailerLane> m_CarTrailerLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> m_TaxiData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<Moving> m_MovingData;

		public ComponentLookup<Controller> m_ControllerData;

		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		public BufferLookup<TargetElement> m_TargetElements;

		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingPersonalCarAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingTaxiAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingServiceCarAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingTrailerAddTypes;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, InvolvedInAccident> nativeParallelHashMap = new NativeParallelHashMap<Entity, InvolvedInAccident>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Impact> nativeArray = m_Chunks[j].GetNativeArray(ref m_ImpactType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Impact impact = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(impact.m_Target) || (impact.m_CheckStoppedEvent && !m_MovingData.HasComponent(impact.m_Target) && m_InvolvedInAccidentData.TryGetComponent(impact.m_Target, out var componentData) && componentData.m_Event == impact.m_Event))
					{
						continue;
					}
					InvolvedInAccident involvedInAccident = new InvolvedInAccident(impact.m_Event, impact.m_Severity, m_SimulationFrame);
					if (nativeParallelHashMap.TryGetValue(impact.m_Target, out var item))
					{
						if (involvedInAccident.m_Severity > item.m_Severity)
						{
							nativeParallelHashMap[impact.m_Target] = involvedInAccident;
						}
					}
					else if (m_InvolvedInAccidentData.HasComponent(impact.m_Target))
					{
						item = m_InvolvedInAccidentData[impact.m_Target];
						if (involvedInAccident.m_Severity > item.m_Severity)
						{
							nativeParallelHashMap.TryAdd(impact.m_Target, involvedInAccident);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(impact.m_Target, involvedInAccident);
					}
					Moving moving = default(Moving);
					if (!impact.m_VelocityDelta.Equals(default(float3)) || !impact.m_AngularVelocityDelta.Equals(default(float3)))
					{
						if (m_MovingData.HasComponent(impact.m_Target))
						{
							moving = m_MovingData[impact.m_Target];
							moving.m_Velocity += impact.m_VelocityDelta;
							moving.m_AngularVelocity += impact.m_AngularVelocityDelta;
							m_MovingData[impact.m_Target] = moving;
						}
						else
						{
							moving.m_Velocity += impact.m_VelocityDelta;
							moving.m_AngularVelocity += impact.m_AngularVelocityDelta;
						}
					}
					if (m_VehicleData.HasComponent(impact.m_Target))
					{
						if (m_CarData.HasComponent(impact.m_Target))
						{
							if (m_ParkedCarData.HasComponent(impact.m_Target))
							{
								ActivateParkedCar(impact.m_Target, moving);
								m_CommandBuffer.AddComponent(impact.m_Target, default(OutOfControl));
							}
							else if (m_CarCurrentLaneData.HasComponent(impact.m_Target))
							{
								if (m_StoppedData.HasComponent(impact.m_Target))
								{
									ActivateStoppedCar(impact.m_Target, moving);
								}
								m_CommandBuffer.AddComponent(impact.m_Target, default(OutOfControl));
							}
						}
						else
						{
							if (!m_CarTrailerData.HasComponent(impact.m_Target))
							{
								continue;
							}
							if (m_ParkedCarData.HasComponent(impact.m_Target))
							{
								ActivateParkedCarTrailer(impact.m_Target, moving, default(ParkedCar));
							}
							else if (m_CarTrailerLaneData.HasComponent(impact.m_Target))
							{
								if (m_StoppedData.HasComponent(impact.m_Target))
								{
									ActivateStoppedTrailer(impact.m_Target, moving);
								}
								if (m_ControllerData.HasComponent(impact.m_Target))
								{
									DetachVehicle(impact.m_Target);
								}
								m_CommandBuffer.AddComponent(impact.m_Target, default(OutOfControl));
							}
						}
					}
					else if (m_CreatureData.HasComponent(impact.m_Target))
					{
						if (m_StoppedData.HasComponent(impact.m_Target))
						{
							ActivateStoppedCreature(impact.m_Target, moving);
						}
						m_CommandBuffer.AddComponent(impact.m_Target, default(Stumbling));
					}
				}
			}
			if (nativeParallelHashMap.Count() == 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int l = 0; l < keyArray.Length; l++)
			{
				Entity entity = keyArray[l];
				InvolvedInAccident involvedInAccident2 = nativeParallelHashMap[entity];
				if (m_InvolvedInAccidentData.HasComponent(entity))
				{
					if (m_InvolvedInAccidentData[entity].m_Event != involvedInAccident2.m_Event && m_TargetElements.HasBuffer(involvedInAccident2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[involvedInAccident2.m_Event], new TargetElement(entity));
					}
					m_InvolvedInAccidentData[entity] = involvedInAccident2;
				}
				else
				{
					if (m_TargetElements.HasBuffer(involvedInAccident2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[involvedInAccident2.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, involvedInAccident2);
				}
			}
		}

		private void DetachVehicle(Entity entity)
		{
			Controller value = m_ControllerData[entity];
			if (value.m_Controller != Entity.Null && value.m_Controller != entity)
			{
				if (m_LayoutElements.TryGetBuffer(value.m_Controller, out var bufferData))
				{
					CollectionUtils.RemoveValue(bufferData, new LayoutElement(entity));
				}
				value.m_Controller = Entity.Null;
				m_ControllerData[entity] = value;
			}
		}

		private void ActivateParkedCarTrailer(Entity entity, Moving moving, ParkedCar parkedCar)
		{
			ParkedCar parkedCar2 = m_ParkedCarData[entity];
			if (parkedCar2.m_Lane == Entity.Null)
			{
				parkedCar2.m_Lane = parkedCar.m_Lane;
				parkedCar2.m_CurvePosition = parkedCar.m_CurvePosition;
			}
			m_CommandBuffer.RemoveComponent(entity, in m_ParkedToMovingCarRemoveTypes);
			m_CommandBuffer.AddComponent(entity, in m_ParkedToMovingTrailerAddTypes);
			m_CommandBuffer.SetComponent(entity, moving);
			m_CommandBuffer.SetComponent(entity, new CarTrailerLane(parkedCar2));
			if (m_CarLaneData.HasComponent(parkedCar2.m_Lane))
			{
				m_CommandBuffer.AddComponent(parkedCar2.m_Lane, default(PathfindUpdated));
			}
		}

		private void ActivateParkedCar(Entity entity, Moving moving)
		{
			ParkedCar parkedCar = m_ParkedCarData[entity];
			Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
			m_CommandBuffer.RemoveComponent(entity, in m_ParkedToMovingCarRemoveTypes);
			if (m_PersonalCarData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(entity, in m_ParkedToMovingPersonalCarAddTypes);
			}
			else if (m_TaxiData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(entity, in m_ParkedToMovingTaxiAddTypes);
			}
			else
			{
				m_CommandBuffer.AddComponent(entity, in m_ParkedToMovingServiceCarAddTypes);
			}
			m_CommandBuffer.SetComponent(entity, moving);
			m_CommandBuffer.SetComponent(entity, new CarCurrentLane(parkedCar, flags));
			if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_GarageLaneData.HasComponent(parkedCar.m_Lane))
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(parkedCar.m_Lane);
			}
			if (!m_LayoutElements.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 1; i < bufferData.Length; i++)
			{
				Entity vehicle = bufferData[i].m_Vehicle;
				if (m_ParkedCarData.HasComponent(vehicle))
				{
					ActivateParkedCarTrailer(vehicle, default(Moving), parkedCar);
				}
			}
		}

		private void ActivateStoppedCar(Entity entity, Moving moving)
		{
			CarCurrentLane carCurrentLane = m_CarCurrentLaneData[entity];
			m_CommandBuffer.RemoveComponent<Stopped>(entity);
			m_CommandBuffer.AddComponent(entity, moving);
			m_CommandBuffer.AddBuffer<TransformFrame>(entity);
			m_CommandBuffer.AddComponent(entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(entity, default(Swaying));
			m_CommandBuffer.AddComponent(entity, default(Updated));
			if (m_CarLaneData.HasComponent(carCurrentLane.m_Lane))
			{
				m_CommandBuffer.AddComponent(carCurrentLane.m_Lane, default(PathfindUpdated));
			}
			if (m_CarLaneData.HasComponent(carCurrentLane.m_ChangeLane))
			{
				m_CommandBuffer.AddComponent(carCurrentLane.m_ChangeLane, default(PathfindUpdated));
			}
			if (!m_LayoutElements.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 1; i < bufferData.Length; i++)
			{
				Entity vehicle = bufferData[i].m_Vehicle;
				if (m_StoppedData.HasComponent(vehicle))
				{
					ActivateStoppedTrailer(vehicle, default(Moving));
				}
			}
		}

		private void ActivateStoppedTrailer(Entity entity, Moving moving)
		{
			CarTrailerLane carTrailerLane = m_CarTrailerLaneData[entity];
			m_CommandBuffer.RemoveComponent<Stopped>(entity);
			m_CommandBuffer.AddComponent(entity, moving);
			m_CommandBuffer.AddBuffer<TransformFrame>(entity);
			m_CommandBuffer.AddComponent(entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(entity, default(Swaying));
			m_CommandBuffer.AddComponent(entity, default(Updated));
			if (m_CarLaneData.HasComponent(carTrailerLane.m_Lane))
			{
				m_CommandBuffer.AddComponent(carTrailerLane.m_Lane, default(PathfindUpdated));
			}
		}

		private void ActivateStoppedCreature(Entity entity, Moving moving)
		{
			m_CommandBuffer.RemoveComponent<Stopped>(entity);
			m_CommandBuffer.AddBuffer<TransformFrame>(entity);
			m_CommandBuffer.AddComponent(entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(entity, moving);
			m_CommandBuffer.AddComponent(entity, default(Updated));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Impact> __Game_Events_Impact_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Stopped> __Game_Objects_Stopped_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailer> __Game_Vehicles_CarTrailer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<Moving> __Game_Objects_Moving_RW_ComponentLookup;

		public ComponentLookup<Controller> __Game_Vehicles_Controller_RW_ComponentLookup;

		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Impact_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Impact>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentLookup = state.GetComponentLookup<Stopped>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Vehicles_CarTrailer_RO_ComponentLookup = state.GetComponentLookup<CarTrailer>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarTrailerLane_RO_ComponentLookup = state.GetComponentLookup<CarTrailerLane>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentLookup = state.GetComponentLookup<Moving>();
			__Game_Vehicles_Controller_RW_ComponentLookup = state.GetComponentLookup<Controller>();
			__Game_Events_InvolvedInAccident_RW_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
			__Game_Vehicles_LayoutElement_RW_BufferLookup = state.GetBufferLookup<LayoutElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_ImpactQuery;

	private ComponentTypeSet m_ParkedToMovingCarRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingPersonalCarAddTypes;

	private ComponentTypeSet m_ParkedToMovingTaxiAddTypes;

	private ComponentTypeSet m_ParkedToMovingServiceCarAddTypes;

	private ComponentTypeSet m_ParkedToMovingTrailerAddTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ImpactQuery = GetEntityQuery(ComponentType.ReadOnly<Impact>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_ParkedToMovingCarRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>());
		m_ParkedToMovingPersonalCarAddTypes = new ComponentTypeSet(new ComponentType[12]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingTaxiAddTypes = new ComponentTypeSet(new ComponentType[13]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingServiceCarAddTypes = new ComponentTypeSet(new ComponentType[14]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingTrailerAddTypes = new ComponentTypeSet(new ComponentType[6]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarTrailerLane>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		RequireForUpdate(m_ImpactQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_ImpactQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new AddImpactJob
		{
			m_ImpactType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Impact_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RW_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = chunks,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_ParkedToMovingCarRemoveTypes = m_ParkedToMovingCarRemoveTypes,
			m_ParkedToMovingPersonalCarAddTypes = m_ParkedToMovingPersonalCarAddTypes,
			m_ParkedToMovingTaxiAddTypes = m_ParkedToMovingTaxiAddTypes,
			m_ParkedToMovingServiceCarAddTypes = m_ParkedToMovingServiceCarAddTypes,
			m_ParkedToMovingTrailerAddTypes = m_ParkedToMovingTrailerAddTypes,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
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
	public ImpactSystem()
	{
	}
}
