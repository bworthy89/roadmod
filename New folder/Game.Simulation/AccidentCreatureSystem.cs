using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AccidentCreatureSystem : GameSystemBase
{
	[BurstCompile]
	private struct AccidentCreatureJob : IJobChunk
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
		public ComponentTypeHandle<Game.Creatures.Resident> m_ResidentType;

		public ComponentTypeHandle<Human> m_HumanType;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> m_InvolvedInAccidentType;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> m_StumblingType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		public ComponentTypeHandle<Creature> m_CreatureType;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> m_HearseData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

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
		public EntityArchetype m_AddProblemArchetype;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<InvolvedInAccident> nativeArray2 = chunk.GetNativeArray(ref m_InvolvedInAccidentType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (chunk.Has(ref m_StumblingType))
			{
				NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<Moving> nativeArray4 = chunk.GetNativeArray(ref m_MovingType);
				NativeArray<Game.Creatures.Resident> nativeArray5 = chunk.GetNativeArray(ref m_ResidentType);
				NativeArray<Human> nativeArray6 = chunk.GetNativeArray(ref m_HumanType);
				NativeArray<Creature> nativeArray7 = chunk.GetNativeArray(ref m_CreatureType);
				NativeArray<CurrentVehicle> nativeArray8 = chunk.GetNativeArray(ref m_CurrentVehicleType);
				for (int i = 0; i < nativeArray7.Length; i++)
				{
					ref Creature reference = ref nativeArray7.ElementAt(i);
					reference.m_QueueEntity = Entity.Null;
					reference.m_QueueArea = default(Sphere3);
				}
				if (nativeArray8.Length != 0)
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Entity entity = nativeArray[j];
						Transform transform = nativeArray3[j];
						InvolvedInAccident involvedInAccident = nativeArray2[j];
						if (nativeArray5.Length != 0)
						{
							Game.Creatures.Resident resident = nativeArray5[j];
							CurrentVehicle currentVehicle = nativeArray8[j];
							int probability = math.select(50, 100, m_BicycleData.HasComponent(currentVehicle.m_Vehicle));
							HealthProblemFlags num = AddInjury(unfilteredChunkIndex, involvedInAccident, resident, probability, ref random);
							StopStumbling(unfilteredChunkIndex, entity);
							if ((num & HealthProblemFlags.RequireTransport) == 0)
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
						else
						{
							ClearAccident(unfilteredChunkIndex, entity);
						}
					}
					return;
				}
				if (nativeArray4.Length != 0)
				{
					for (int k = 0; k < nativeArray4.Length; k++)
					{
						Entity entity3 = nativeArray[k];
						Transform transform2 = nativeArray3[k];
						Moving moving = nativeArray4[k];
						InvolvedInAccident involvedInAccident2 = nativeArray2[k];
						if (transform2.m_Position.y < -1000f)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity3, default(Deleted));
						}
						else
						{
							if (!(math.lengthsq(moving.m_Velocity) < 0.0001f) || !(math.lengthsq(moving.m_AngularVelocity) < 0.0001f))
							{
								continue;
							}
							if (nativeArray5.Length != 0)
							{
								Game.Creatures.Resident resident2 = nativeArray5[k];
								if ((AddInjury(unfilteredChunkIndex, involvedInAccident2, resident2, 50, ref random) & HealthProblemFlags.RequireTransport) != HealthProblemFlags.None)
								{
									StopMoving(unfilteredChunkIndex, entity3);
									if (CollectionUtils.TryGet(nativeArray6, k, out var value))
									{
										value.m_Flags |= HumanFlags.Collapsed;
										nativeArray6[k] = value;
									}
								}
								else
								{
									StopStumbling(unfilteredChunkIndex, entity3);
									m_IconCommandBuffer.Add(entity3, m_PoliceConfigurationData.m_TrafficAccidentNotificationPrefab, IconPriority.MajorProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, involvedInAccident2.m_Event);
								}
								if (!m_TargetElements.HasBuffer(involvedInAccident2.m_Event))
								{
									continue;
								}
								Entity entity4 = FindAccidentSite(involvedInAccident2.m_Event);
								if (entity4 == Entity.Null)
								{
									entity4 = FindSuitableAccidentSite(transform2.m_Position);
									if (entity4 != Entity.Null)
									{
										AddAccidentSite(unfilteredChunkIndex, ref involvedInAccident2, entity4);
									}
								}
							}
							else
							{
								ClearAccident(unfilteredChunkIndex, entity3);
							}
						}
					}
					return;
				}
				NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref m_TargetType);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Entity entity5 = nativeArray[l];
					InvolvedInAccident involvedInAccident3 = nativeArray2[l];
					Target target = nativeArray9[l];
					if (IsSecured(involvedInAccident3) || m_HearseData.HasComponent(target.m_Target) || m_AmbulanceData.HasComponent(target.m_Target))
					{
						StartMoving(unfilteredChunkIndex, entity5);
						ClearAccident(unfilteredChunkIndex, entity5);
						if (CollectionUtils.TryGet(nativeArray6, l, out var value2))
						{
							value2.m_Flags &= ~HumanFlags.Collapsed;
							nativeArray6[l] = value2;
						}
					}
				}
				return;
			}
			NativeArray<Target> nativeArray10 = chunk.GetNativeArray(ref m_TargetType);
			for (int m = 0; m < nativeArray.Length; m++)
			{
				Entity entity6 = nativeArray[m];
				InvolvedInAccident involvedInAccident4 = nativeArray2[m];
				Target target2 = nativeArray10[m];
				if (IsSecured(involvedInAccident4) || m_HearseData.HasComponent(target2.m_Target) || m_AmbulanceData.HasComponent(target2.m_Target))
				{
					ClearAccident(unfilteredChunkIndex, entity6);
				}
			}
		}

		private HealthProblemFlags AddInjury(int jobIndex, InvolvedInAccident involvedInAccident, Game.Creatures.Resident resident, int probability, ref Random random)
		{
			if (m_PrefabRefData.HasComponent(resident.m_Citizen) && random.NextInt(100) < probability)
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddProblemArchetype);
				AddHealthProblem component = new AddHealthProblem
				{
					m_Event = involvedInAccident.m_Event,
					m_Target = resident.m_Citizen,
					m_Flags = HealthProblemFlags.RequireTransport
				};
				component.m_Flags |= (HealthProblemFlags)((random.NextInt(100) < 20) ? 2 : 4);
				m_CommandBuffer.SetComponent(jobIndex, e, component);
				return component.m_Flags;
			}
			return HealthProblemFlags.None;
		}

		private bool IsSecured(InvolvedInAccident involvedInAccident)
		{
			Entity entity = FindAccidentSite(involvedInAccident.m_Event);
			if (entity != Entity.Null)
			{
				AccidentSite accidentSite = m_AccidentSiteData[entity];
				if ((accidentSite.m_Flags & AccidentSiteFlags.Secured) == 0)
				{
					return m_SimulationFrame >= accidentSite.m_CreationFrame + 14400;
				}
				return true;
			}
			return true;
		}

		private void StopMoving(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Moving>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Stopped));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
		}

		private void StartMoving(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Stopped>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Moving));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
		}

		private void StopStumbling(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Stumbling>(jobIndex, entity);
		}

		private void ClearAccident(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<InvolvedInAccident>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<Stumbling>(jobIndex, entity);
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
		public ComponentTypeHandle<Game.Creatures.Resident> __Game_Creatures_Resident_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Human> __Game_Creatures_Human_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> __Game_Creatures_Stumbling_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> __Game_Vehicles_Hearse_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Resident>(isReadOnly: true);
			__Game_Creatures_Human_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Human>();
			__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InvolvedInAccident>(isReadOnly: true);
			__Game_Creatures_Stumbling_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stumbling>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_Creature_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>();
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Vehicles_Hearse_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Hearse>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Ambulance>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_CreatureQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_AddAccidentSiteArchetype;

	private EntityArchetype m_AddProblemArchetype;

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
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadWrite<InvolvedInAccident>(), ComponentType.ReadOnly<Creature>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_AddAccidentSiteArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<AddAccidentSite>());
		m_AddProblemArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<AddHealthProblem>());
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PoliceConfigurationData singleton = m_ConfigQuery.GetSingleton<PoliceConfigurationData>();
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AccidentCreatureJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Human_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InvolvedInAccidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StumblingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Stumbling_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HearseData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Hearse_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AmbulanceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_PoliceConfigurationData = singleton,
			m_AddAccidentSiteArchetype = m_AddAccidentSiteArchetype,
			m_AddProblemArchetype = m_AddProblemArchetype,
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_CreatureQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
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
	public AccidentCreatureSystem()
	{
	}
}
