using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Net;
using Game.Notifications;
using Game.Objects;
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
public class AccidentVehicleSystem : GameSystemBase
{
	[BurstCompile]
	private struct AccidentVehicleJob : IJobChunk
	{
		private struct EdgeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float3 m_Position;

			public float m_MaxDistance;

			public Entity m_Result;

			public ComponentLookup<AccidentSite> m_AccidentSiteData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<Road> m_RoadData;

			public ComponentLookup<Curve> m_CurveData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(m_Bounds, bounds.m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity edge)
			{
				if (MathUtils.Intersect(m_Bounds, bounds.m_Bounds) && m_RoadData.HasComponent(edge) && !m_AccidentSiteData.HasComponent(edge) && m_CurveData.HasComponent(edge))
				{
					Curve curve = m_CurveData[edge];
					EdgeGeometry edgeGeometry = m_EdgeGeometryData[edge];
					float num = math.distance(edgeGeometry.m_Start.m_Left.d, edgeGeometry.m_Start.m_Right.d);
					float t;
					float num2 = MathUtils.Distance(curve.m_Bezier, m_Position, out t) - num * 0.5f;
					if (num2 < m_MaxDistance)
					{
						m_MaxDistance = num2;
						m_Result = edge;
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Moving> m_MovingType;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> m_OnFireType;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> m_InvolvedInAccidentType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> m_BicycleType;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> m_BlockedLaneType;

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireData> m_PrefabFireData;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public EntityArchetype m_AddAccidentSiteArchetype;

		[ReadOnly]
		public EntityArchetype m_EventIgniteArchetype;

		[ReadOnly]
		public EntityArchetype m_AddImpactArchetype;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			NativeArray<Destroyed> nativeArray5 = chunk.GetNativeArray(ref m_DestroyedType);
			NativeArray<InvolvedInAccident> nativeArray6 = chunk.GetNativeArray(ref m_InvolvedInAccidentType);
			NativeArray<Controller> nativeArray7 = chunk.GetNativeArray(ref m_ControllerType);
			BufferAccessor<BlockedLane> bufferAccessor = chunk.GetBufferAccessor(ref m_BlockedLaneType);
			BufferAccessor<LayoutElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LayoutElementType);
			BufferAccessor<Passenger> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PassengerType);
			bool flag = chunk.Has(ref m_OnFireType);
			bool test = chunk.Has(ref m_BicycleType);
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray3.Length; i++)
				{
					Entity entity = nativeArray[i];
					Transform transform = nativeArray2[i];
					Moving moving = nativeArray3[i];
					InvolvedInAccident involvedInAccident = nativeArray6[i];
					if (!CollectionUtils.TryGet(bufferAccessor, i, out var value))
					{
						ClearAccident(unfilteredChunkIndex, entity);
						continue;
					}
					CollectionUtils.TryGet(bufferAccessor2, i, out var value2);
					float num = m_SimulationFrame - involvedInAccident.m_InvolvedFrame;
					num = 0.01f + num * num * 3E-09f;
					num *= num;
					if (transform.m_Position.y < -1000f)
					{
						VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity, value2);
					}
					else
					{
						if (!(math.lengthsq(moving.m_Velocity) < num) || !(math.lengthsq(moving.m_AngularVelocity) < num))
						{
							continue;
						}
						StopVehicle(unfilteredChunkIndex, entity, value);
						Random random = m_RandomSeed.GetRandom(entity.Index);
						if (!flag && nativeArray4.Length != 0 && m_PrefabRefData.HasComponent(involvedInAccident.m_Event))
						{
							Damaged damaged = nativeArray4[i];
							PrefabRef prefabRef = m_PrefabRefData[involvedInAccident.m_Event];
							if (m_PrefabFireData.HasComponent(prefabRef.m_Prefab))
							{
								FireData fireData = m_PrefabFireData[prefabRef.m_Prefab];
								float num2 = damaged.m_Damage.x * fireData.m_StartProbability;
								if (num2 > 0.01f && random.NextFloat(100f) < num2)
								{
									IgniteFire(unfilteredChunkIndex, entity, involvedInAccident.m_Event, fireData);
								}
							}
						}
						if (nativeArray5.Length != 0)
						{
							m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
							m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
							m_IconCommandBuffer.Add(entity, m_PoliceConfigurationData.m_TrafficAccidentNotificationPrefab, IconPriority.FatalProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, involvedInAccident.m_Event);
							if (bufferAccessor3.Length != 0)
							{
								AddInjuries(unfilteredChunkIndex, involvedInAccident, bufferAccessor3[i], ref random);
							}
						}
						else
						{
							m_IconCommandBuffer.Add(entity, m_PoliceConfigurationData.m_TrafficAccidentNotificationPrefab, IconPriority.MajorProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, involvedInAccident.m_Event);
						}
						if (!m_TargetElements.HasBuffer(involvedInAccident.m_Event))
						{
							continue;
						}
						Entity entity2 = FindAccidentSite(involvedInAccident.m_Event);
						if (entity2 == Entity.Null)
						{
							entity2 = FindSuitableAccidentSite(transform.m_Position);
							if (entity2 != Entity.Null)
							{
								AddAccidentSite(unfilteredChunkIndex, ref involvedInAccident, entity2);
							}
						}
					}
				}
				return;
			}
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity3 = nativeArray[j];
				InvolvedInAccident involvedInAccident2 = nativeArray6[j];
				if (!CollectionUtils.TryGet(bufferAccessor, j, out var value3))
				{
					ClearAccident(unfilteredChunkIndex, entity3);
					continue;
				}
				CollectionUtils.TryGet(bufferAccessor2, j, out var value4);
				if (!IsSecured(involvedInAccident2))
				{
					continue;
				}
				if (!flag)
				{
					if (nativeArray5.Length != 0)
					{
						Destroyed destroyed = nativeArray5[j];
						uint num3 = math.select(14400u, 300u, test);
						if (destroyed.m_Cleared >= 1f || m_SimulationFrame >= involvedInAccident2.m_InvolvedFrame + num3)
						{
							VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity3, value4);
							continue;
						}
					}
					else
					{
						if (nativeArray4.Length == 0)
						{
							if (nativeArray7.Length == 0 || m_PrefabRefData.HasComponent(nativeArray7[j].m_Controller))
							{
								StartVehicle(unfilteredChunkIndex, entity3, value3);
								ClearAccident(unfilteredChunkIndex, entity3);
							}
							else
							{
								VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity3, value4);
							}
							continue;
						}
						if (m_SimulationFrame >= involvedInAccident2.m_InvolvedFrame + 14400)
						{
							VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity3, value4);
							continue;
						}
					}
				}
				for (int k = 0; k < value3.Length; k++)
				{
					Entity lane = value3[k].m_Lane;
					if (m_CarLaneData.HasComponent(lane) && (m_CarLaneData[lane].m_Flags & Game.Net.CarLaneFlags.IsSecured) == 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, lane, default(PathfindUpdated));
					}
				}
			}
		}

		private void AddInjuries(int jobIndex, InvolvedInAccident involvedInAccident, DynamicBuffer<Passenger> passengers, ref Random random)
		{
			float num = involvedInAccident.m_Severity;
			for (int i = 0; i < passengers.Length; i++)
			{
				Entity passenger = passengers[i].m_Passenger;
				if (m_ResidentData.HasComponent(passenger))
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddImpactArchetype);
					Impact component = new Impact
					{
						m_Event = involvedInAccident.m_Event,
						m_Target = passenger,
						m_Severity = random.NextFloat(num)
					};
					num *= random.NextFloat(0.8f, 0.9f);
					m_CommandBuffer.SetComponent(jobIndex, e, component);
				}
			}
		}

		private bool IsSecured(InvolvedInAccident involvedInAccident)
		{
			Entity entity = FindAccidentSite(involvedInAccident.m_Event);
			if (entity != Entity.Null)
			{
				AccidentSite accidentSite = m_AccidentSiteData[entity];
				if ((accidentSite.m_Flags & AccidentSiteFlags.MovingVehicles) == 0)
				{
					if ((accidentSite.m_Flags & AccidentSiteFlags.Secured) == 0)
					{
						return m_SimulationFrame >= accidentSite.m_CreationFrame + 14400;
					}
					return true;
				}
				return false;
			}
			return true;
		}

		private void IgniteFire(int jobIndex, Entity entity, Entity _event, FireData fireData)
		{
			Ignite component = new Ignite
			{
				m_Target = entity,
				m_Event = _event,
				m_Intensity = fireData.m_StartIntensity,
				m_RequestFrame = m_SimulationFrame
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EventIgniteArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
		}

		private void StopVehicle(int jobIndex, Entity entity, DynamicBuffer<BlockedLane> blockedLanes)
		{
			m_CommandBuffer.RemoveComponent<Moving>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<InterpolatedTransform>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<Swaying>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Stopped));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			for (int i = 0; i < blockedLanes.Length; i++)
			{
				Entity lane = blockedLanes[i].m_Lane;
				if (m_CarLaneData.HasComponent(lane))
				{
					m_CommandBuffer.AddComponent(jobIndex, lane, default(PathfindUpdated));
				}
			}
		}

		private void StartVehicle(int jobIndex, Entity entity, DynamicBuffer<BlockedLane> blockedLanes)
		{
			m_CommandBuffer.RemoveComponent<Stopped>(jobIndex, entity);
			m_CommandBuffer.AddBuffer<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Moving));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Swaying));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			for (int i = 0; i < blockedLanes.Length; i++)
			{
				Entity lane = blockedLanes[i].m_Lane;
				if (m_CarLaneData.HasComponent(lane))
				{
					m_CommandBuffer.AddComponent(jobIndex, lane, default(PathfindUpdated));
				}
			}
		}

		private void ClearAccident(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<InvolvedInAccident>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<OutOfControl>(jobIndex, entity);
			m_IconCommandBuffer.Remove(entity, m_PoliceConfigurationData.m_TrafficAccidentNotificationPrefab);
		}

		private Entity FindAccidentSite(Entity _event)
		{
			if (m_TargetElements.HasBuffer(_event))
			{
				DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[_event];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity entity = dynamicBuffer[i].m_Entity;
					if (m_AccidentSiteData.HasComponent(entity))
					{
						return entity;
					}
				}
			}
			return Entity.Null;
		}

		private Entity FindSuitableAccidentSite(float3 position)
		{
			float num = 30f;
			EdgeIterator iterator = new EdgeIterator
			{
				m_Bounds = new Bounds3(position - num, position + num),
				m_Position = position,
				m_MaxDistance = num,
				m_AccidentSiteData = m_AccidentSiteData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_RoadData = m_RoadData,
				m_CurveData = m_CurveData
			};
			m_NetSearchTree.Iterate(ref iterator);
			return iterator.m_Result;
		}

		private void AddAccidentSite(int jobIndex, ref InvolvedInAccident involvedInAccident, Entity target)
		{
			AddAccidentSite component = new AddAccidentSite
			{
				m_Event = involvedInAccident.m_Event,
				m_Target = target,
				m_Flags = AccidentSiteFlags.TrafficAccident
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddAccidentSiteArchetype);
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
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> __Game_Events_OnFire_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> __Game_Objects_BlockedLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireData> __Game_Prefabs_FireData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OnFire>(isReadOnly: true);
			__Game_Objects_BlockedLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<BlockedLane>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InvolvedInAccident>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Bicycle>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_FireData_RO_ComponentLookup = state.GetComponentLookup<FireData>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_VehicleQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_AddAccidentSiteArchetype;

	private EntityArchetype m_EventIgniteArchetype;

	private EntityArchetype m_AddImpactArchetype;

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
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<InvolvedInAccident>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_AddAccidentSiteArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<AddAccidentSite>());
		m_EventIgniteArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Ignite>());
		m_AddImpactArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Impact>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PoliceConfigurationData singleton = m_ConfigQuery.GetSingleton<PoliceConfigurationData>();
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AccidentVehicleJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OnFireType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockedLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_BlockedLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InvolvedInAccidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_PoliceConfigurationData = singleton,
			m_AddAccidentSiteArchetype = m_AddAccidentSiteArchetype,
			m_EventIgniteArchetype = m_EventIgniteArchetype,
			m_AddImpactArchetype = m_AddImpactArchetype,
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
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
	public AccidentVehicleSystem()
	{
	}
}
