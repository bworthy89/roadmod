#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Notifications;
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
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AccidentSiteSystem : GameSystemBase
{
	[BurstCompile]
	private struct AccidentSiteJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		public ComponentTypeHandle<AccidentSite> m_AccidentSiteType;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<Criminal> m_CriminalData;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> m_PrefabTrafficAccidentData;

		[ReadOnly]
		public ComponentLookup<CrimeData> m_PrefabCrimeData;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EntityArchetype m_PoliceRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_EventImpactArchetype;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<AccidentSite> nativeArray2 = chunk.GetNativeArray(ref m_AccidentSiteType);
			bool flag = chunk.Has(ref m_BuildingType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				AccidentSite accidentSite = nativeArray2[i];
				Random random = m_RandomSeed.GetRandom(entity.Index);
				Entity entity2 = Entity.Null;
				int num = 0;
				float num2 = 0f;
				if (m_SimulationFrame - accidentSite.m_CreationFrame >= 3600)
				{
					accidentSite.m_Flags &= ~AccidentSiteFlags.StageAccident;
				}
				accidentSite.m_Flags &= ~AccidentSiteFlags.MovingVehicles;
				if (m_TargetElements.HasBuffer(accidentSite.m_Event))
				{
					DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity entity3 = dynamicBuffer[j].m_Entity;
						if (m_InvolvedInAccidentData.TryGetComponent(entity3, out var componentData))
						{
							if (componentData.m_Event == accidentSite.m_Event)
							{
								num++;
								bool flag2 = m_MovingData.HasComponent(entity3);
								if (flag2 && (accidentSite.m_Flags & AccidentSiteFlags.MovingVehicles) == 0 && m_VehicleData.HasComponent(entity3))
								{
									accidentSite.m_Flags |= AccidentSiteFlags.MovingVehicles;
								}
								if (componentData.m_Severity > num2)
								{
									entity2 = (flag2 ? Entity.Null : entity3);
									num2 = componentData.m_Severity;
									accidentSite.m_Flags &= ~AccidentSiteFlags.StageAccident;
								}
							}
						}
						else
						{
							if (!m_CriminalData.HasComponent(entity3))
							{
								continue;
							}
							Criminal criminal = m_CriminalData[entity3];
							if (criminal.m_Event == accidentSite.m_Event && (criminal.m_Flags & CriminalFlags.Arrested) == 0)
							{
								num++;
								if ((criminal.m_Flags & CriminalFlags.Monitored) != 0)
								{
									accidentSite.m_Flags |= AccidentSiteFlags.CrimeMonitored;
								}
							}
						}
					}
					if (num == 0 && (accidentSite.m_Flags & AccidentSiteFlags.StageAccident) != 0)
					{
						PrefabRef prefabRef = m_PrefabRefData[accidentSite.m_Event];
						if (m_PrefabTrafficAccidentData.HasComponent(prefabRef.m_Prefab))
						{
							TrafficAccidentData trafficAccidentData = m_PrefabTrafficAccidentData[prefabRef.m_Prefab];
							Entity entity4 = TryFindSubject(entity, ref random, trafficAccidentData);
							if (entity4 != Entity.Null)
							{
								AddImpact(unfilteredChunkIndex, accidentSite.m_Event, ref random, entity4, trafficAccidentData);
							}
						}
					}
				}
				if ((accidentSite.m_Flags & (AccidentSiteFlags.CrimeScene | AccidentSiteFlags.CrimeDetected)) == AccidentSiteFlags.CrimeScene)
				{
					PrefabRef prefabRef2 = m_PrefabRefData[accidentSite.m_Event];
					if (m_PrefabCrimeData.HasComponent(prefabRef2.m_Prefab))
					{
						CrimeData crimeData = m_PrefabCrimeData[prefabRef2.m_Prefab];
						float num3 = (float)(m_SimulationFrame - accidentSite.m_CreationFrame) / 60f;
						Bounds1 alarmDelay = crimeData.m_AlarmDelay;
						CityUtils.ApplyModifier(ref alarmDelay.min, m_CityModifiers[m_City], CityModifierType.CrimeResponseTime);
						CityUtils.ApplyModifier(ref alarmDelay.max, m_CityModifiers[m_City], CityModifierType.CrimeResponseTime);
						if ((accidentSite.m_Flags & AccidentSiteFlags.CrimeMonitored) != 0 || num3 >= alarmDelay.max)
						{
							accidentSite.m_Flags |= AccidentSiteFlags.CrimeDetected;
						}
						else if (num3 >= alarmDelay.min)
						{
							float num4 = 1.0666667f / (alarmDelay.max - alarmDelay.min);
							if (random.NextFloat(1f) <= num4)
							{
								accidentSite.m_Flags |= AccidentSiteFlags.CrimeDetected;
							}
						}
					}
					if ((accidentSite.m_Flags & AccidentSiteFlags.CrimeDetected) != 0)
					{
						m_IconCommandBuffer.Add(entity, m_PoliceConfigurationData.m_CrimeSceneNotificationPrefab, IconPriority.MajorProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, accidentSite.m_Event);
					}
				}
				else if ((accidentSite.m_Flags & (AccidentSiteFlags.CrimeScene | AccidentSiteFlags.CrimeFinished)) == AccidentSiteFlags.CrimeScene)
				{
					PrefabRef prefabRef3 = m_PrefabRefData[accidentSite.m_Event];
					if (m_PrefabCrimeData.HasComponent(prefabRef3.m_Prefab))
					{
						CrimeData crimeData2 = m_PrefabCrimeData[prefabRef3.m_Prefab];
						float num5 = (float)(m_SimulationFrame - accidentSite.m_CreationFrame) / 60f;
						if (num5 >= crimeData2.m_CrimeDuration.max)
						{
							accidentSite.m_Flags |= AccidentSiteFlags.CrimeFinished;
						}
						else if (num5 >= crimeData2.m_CrimeDuration.min)
						{
							float num6 = 1.0666667f / (crimeData2.m_CrimeDuration.max - crimeData2.m_CrimeDuration.min);
							if (random.NextFloat(1f) <= num6)
							{
								accidentSite.m_Flags |= AccidentSiteFlags.CrimeFinished;
							}
						}
					}
				}
				accidentSite.m_Flags &= ~AccidentSiteFlags.RequirePolice;
				if (num2 > 0f || (accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) == AccidentSiteFlags.CrimeScene)
				{
					if (num2 > 0f || (accidentSite.m_Flags & AccidentSiteFlags.CrimeDetected) != 0)
					{
						if (flag)
						{
							entity2 = entity;
						}
						if (entity2 != Entity.Null)
						{
							accidentSite.m_Flags |= AccidentSiteFlags.RequirePolice;
							RequestPoliceIfNeeded(unfilteredChunkIndex, entity, ref accidentSite, entity2, num2);
						}
					}
				}
				else if (num == 0 && ((accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) != (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene) || m_SimulationFrame >= accidentSite.m_SecuredFrame + 1024))
				{
					m_CommandBuffer.RemoveComponent<AccidentSite>(unfilteredChunkIndex, entity);
					if ((accidentSite.m_Flags & AccidentSiteFlags.CrimeScene) != 0)
					{
						m_IconCommandBuffer.Remove(entity, m_PoliceConfigurationData.m_CrimeSceneNotificationPrefab);
					}
				}
				nativeArray2[i] = accidentSite;
			}
		}

		private Entity TryFindSubject(Entity entity, ref Random random, TrafficAccidentData trafficAccidentData)
		{
			Entity result = Entity.Null;
			int num = 0;
			if (m_SubLanes.HasBuffer(entity))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (!m_LaneObjects.HasBuffer(subLane))
					{
						continue;
					}
					DynamicBuffer<LaneObject> dynamicBuffer2 = m_LaneObjects[subLane];
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Entity laneObject = dynamicBuffer2[j].m_LaneObject;
						if (trafficAccidentData.m_SubjectType == EventTargetType.MovingCar && m_CarData.HasComponent(laneObject) && m_MovingData.HasComponent(laneObject) && !m_BicycleData.HasComponent(laneObject) && !m_InvolvedInAccidentData.HasComponent(laneObject))
						{
							num++;
							if (random.NextInt(num) == num - 1)
							{
								result = laneObject;
							}
						}
					}
				}
			}
			return result;
		}

		private void RequestPoliceIfNeeded(int jobIndex, Entity entity, ref AccidentSite accidentSite, Entity target, float severity)
		{
			if (!m_PoliceEmergencyRequestData.HasComponent(accidentSite.m_PoliceRequest))
			{
				PolicePurpose purpose = (((accidentSite.m_Flags & AccidentSiteFlags.CrimeMonitored) == 0) ? PolicePurpose.Emergency : PolicePurpose.Intelligence);
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PoliceEmergencyRequest(entity, target, severity, purpose));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
			}
		}

		private void AddImpact(int jobIndex, Entity eventEntity, ref Random random, Entity target, TrafficAccidentData trafficAccidentData)
		{
			Impact component = new Impact
			{
				m_Event = eventEntity,
				m_Target = target
			};
			if (trafficAccidentData.m_AccidentType == TrafficAccidentType.LoseControl && m_MovingData.HasComponent(target))
			{
				Moving moving = m_MovingData[target];
				component.m_Severity = 5f;
				if (random.NextBool())
				{
					component.m_AngularVelocityDelta.y = -2f;
					component.m_VelocityDelta.xz = component.m_Severity * MathUtils.Left(math.normalizesafe(moving.m_Velocity.xz));
				}
				else
				{
					component.m_AngularVelocityDelta.y = 2f;
					component.m_VelocityDelta.xz = component.m_Severity * MathUtils.Right(math.normalizesafe(moving.m_Velocity.xz));
				}
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EventImpactArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
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
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		public ComponentTypeHandle<AccidentSite> __Game_Events_AccidentSite_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Criminal> __Game_Citizens_Criminal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> __Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> __Game_Prefabs_TrafficAccidentData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeData> __Game_Prefabs_CrimeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Events_AccidentSite_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AccidentSite>();
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentLookup = state.GetComponentLookup<Criminal>(isReadOnly: true);
			__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup = state.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup = state.GetComponentLookup<TrafficAccidentData>(isReadOnly: true);
			__Game_Prefabs_CrimeData_RO_ComponentLookup = state.GetComponentLookup<CrimeData>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private EntityQuery m_AccidentQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_PoliceRequestArchetype;

	private EntityArchetype m_EventImpactArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_AccidentQuery = GetEntityQuery(ComponentType.ReadWrite<AccidentSite>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_PoliceRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PoliceEmergencyRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_EventImpactArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Impact>());
		RequireForUpdate(m_AccidentQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AccidentSiteJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AccidentSiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AccidentSite_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CriminalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceEmergencyRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrafficAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCrimeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CrimeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_PoliceRequestArchetype = m_PoliceRequestArchetype,
			m_EventImpactArchetype = m_EventImpactArchetype,
			m_PoliceConfigurationData = m_ConfigQuery.GetSingleton<PoliceConfigurationData>(),
			m_City = m_CitySystem.City,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_AccidentQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
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
	public AccidentSiteSystem()
	{
	}
}
