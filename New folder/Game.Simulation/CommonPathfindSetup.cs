using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CommonPathfindSetup
{
	[BurstCompile]
	private struct SetupCurrentLocationJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public ComponentLookup<PathOwner> m_PathOwnerData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			float num = targetSeeker.m_SetupQueueTarget.m_Value2;
			if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0 && m_VehicleData.HasComponent(entity))
			{
				num = math.max(10f, num);
			}
			EdgeFlags edgeFlags = EdgeFlags.DefaultMask;
			if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.SecondaryPath) != SetupTargetFlags.None)
			{
				edgeFlags |= EdgeFlags.Secondary;
			}
			PathElement pathElement = default(PathElement);
			bool flag = false;
			bool flag2 = false;
			if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.PathEnd) != SetupTargetFlags.None)
			{
				flag = true;
				if (m_PathOwnerData.TryGetComponent(entity, out var componentData) && m_PathElements.TryGetBuffer(entity, out var bufferData) && componentData.m_ElementIndex < bufferData.Length)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag2 = true;
				}
			}
			if (flag2)
			{
				targetSeeker.m_Buffer.Enqueue(new PathTarget(entity, pathElement.m_Target, pathElement.m_TargetDelta.y, 0f));
			}
			else
			{
				if ((targetSeeker.FindTargets(entity, entity, 0f, edgeFlags, allowAccessRestriction: true, flag) != 0 && flag) || ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) == 0 && num <= 0f))
				{
					return;
				}
				Entity entity2 = entity;
				if (targetSeeker.m_CurrentTransport.HasComponent(entity2))
				{
					entity2 = targetSeeker.m_CurrentTransport[entity2].m_CurrentTransport;
				}
				else if (targetSeeker.m_CurrentBuilding.HasComponent(entity2))
				{
					entity2 = targetSeeker.m_CurrentBuilding[entity2].m_CurrentBuilding;
				}
				if (!targetSeeker.m_Transform.HasComponent(entity2))
				{
					return;
				}
				float3 position = targetSeeker.m_Transform[entity2].m_Position;
				if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Road) != 0 && num > 0f && targetSeeker.m_Owner.TryGetComponent(entity2, out var componentData2) && targetSeeker.m_PrefabRef.TryGetComponent(entity2, out var componentData3) && targetSeeker.m_Building.HasComponent(entity2) && targetSeeker.m_Building.HasComponent(componentData2.m_Owner) && (!m_PlaceableObjectData.TryGetComponent(componentData3.m_Prefab, out var componentData4) || (componentData4.m_Flags & Game.Objects.PlacementFlags.Floating) == 0))
				{
					targetSeeker.FindTargets(entity, componentData2.m_Owner, 100f, edgeFlags, allowAccessRestriction: true, flag);
				}
				if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) != 0)
				{
					if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						Entity lane = Entity.Null;
						float curvePos = 0f;
						float distance = float.MaxValue;
						targetSeeker.m_AirwayData.helicopterMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane, ref curvePos, ref distance);
						if (lane != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(entity, lane, curvePos, 0f));
						}
					}
					if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Airplane) != RoadTypes.None)
					{
						Entity lane2 = Entity.Null;
						float curvePos2 = 0f;
						float distance2 = float.MaxValue;
						targetSeeker.m_AirwayData.airplaneMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane2, ref curvePos2, ref distance2);
						if (lane2 != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(entity, lane2, curvePos2, 0f));
						}
					}
				}
				if (num > 0f)
				{
					TargetIterator iterator = new TargetIterator
					{
						m_Entity = entity,
						m_Bounds = new Bounds3(position - num, position + num),
						m_Position = position,
						m_MaxDistance = num,
						m_TargetSeeker = targetSeeker,
						m_Flags = edgeFlags,
						m_CompositionData = m_CompositionData,
						m_NetCompositionData = m_NetCompositionData
					};
					m_NetSearchTree.Iterate(ref iterator);
					Entity entity3 = entity2;
					Owner componentData5;
					while (targetSeeker.m_Owner.TryGetComponent(entity3, out componentData5) && !targetSeeker.m_AreaNode.HasBuffer(entity3))
					{
						entity3 = componentData5.m_Owner;
					}
					if (targetSeeker.m_AreaNode.HasBuffer(entity3))
					{
						Random random = targetSeeker.m_RandomSeed.GetRandom(entity3.Index);
						m_SubAreas.TryGetBuffer(entity3, out var bufferData2);
						targetSeeker.AddAreaTargets(ref random, entity, entity3, entity2, bufferData2, 0f, addDistanceCost: true, EdgeFlags.DefaultMask);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct SetupAccidentLocationJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			if (!m_AccidentSiteData.HasComponent(entity))
			{
				return;
			}
			AccidentSite accidentSite = m_AccidentSiteData[entity];
			if (!m_TargetElements.HasBuffer(accidentSite.m_Event))
			{
				return;
			}
			DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
			EdgeFlags edgeFlags = EdgeFlags.DefaultMask;
			if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.SecondaryPath) != SetupTargetFlags.None)
			{
				edgeFlags |= EdgeFlags.Secondary;
			}
			bool allowAccessRestriction = true;
			CheckTarget(entity, accidentSite, edgeFlags, ref targetSeeker, ref allowAccessRestriction);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity entity2 = dynamicBuffer[i].m_Entity;
				if (entity2 != entity)
				{
					CheckTarget(entity2, accidentSite, edgeFlags, ref targetSeeker, ref allowAccessRestriction);
				}
			}
		}

		private void CheckTarget(Entity target, AccidentSite accidentSite, EdgeFlags edgeFlags, ref PathfindTargetSeeker<PathfindSetupBuffer> targetSeeker, ref bool allowAccessRestriction)
		{
			if ((accidentSite.m_Flags & AccidentSiteFlags.TrafficAccident) != 0 && !m_CreatureData.HasComponent(target) && !m_VehicleData.HasComponent(target))
			{
				return;
			}
			int num = targetSeeker.FindTargets(target, target, 0f, edgeFlags, allowAccessRestriction, navigationEnd: false);
			allowAccessRestriction &= num == 0;
			Entity entity = target;
			if (targetSeeker.m_CurrentTransport.HasComponent(entity))
			{
				entity = targetSeeker.m_CurrentTransport[entity].m_CurrentTransport;
			}
			else if (targetSeeker.m_CurrentBuilding.HasComponent(entity))
			{
				entity = targetSeeker.m_CurrentBuilding[entity].m_CurrentBuilding;
			}
			if (!targetSeeker.m_Transform.HasComponent(entity))
			{
				return;
			}
			float3 position = targetSeeker.m_Transform[entity].m_Position;
			if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) != 0)
			{
				if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
				{
					Entity lane = Entity.Null;
					float curvePos = 0f;
					float distance = float.MaxValue;
					targetSeeker.m_AirwayData.helicopterMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane, ref curvePos, ref distance);
					if (lane != Entity.Null)
					{
						targetSeeker.m_Buffer.Enqueue(new PathTarget(target, lane, curvePos, 0f));
					}
				}
				if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Airplane) != RoadTypes.None)
				{
					Entity lane2 = Entity.Null;
					float curvePos2 = 0f;
					float distance2 = float.MaxValue;
					targetSeeker.m_AirwayData.airplaneMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane2, ref curvePos2, ref distance2);
					if (lane2 != Entity.Null)
					{
						targetSeeker.m_Buffer.Enqueue(new PathTarget(target, lane2, curvePos2, 0f));
					}
				}
			}
			float value = targetSeeker.m_SetupQueueTarget.m_Value2;
			TargetIterator iterator = new TargetIterator
			{
				m_Entity = target,
				m_Bounds = new Bounds3(position - value, position + value),
				m_Position = position,
				m_MaxDistance = value,
				m_TargetSeeker = targetSeeker,
				m_Flags = edgeFlags,
				m_CompositionData = m_CompositionData,
				m_NetCompositionData = m_NetCompositionData
			};
			m_NetSearchTree.Iterate(ref iterator);
			Entity entity2 = entity;
			Owner componentData;
			while (targetSeeker.m_Owner.TryGetComponent(entity2, out componentData) && !targetSeeker.m_AreaNode.HasBuffer(entity2))
			{
				entity2 = componentData.m_Owner;
			}
			if (targetSeeker.m_AreaNode.HasBuffer(entity2))
			{
				Random random = targetSeeker.m_RandomSeed.GetRandom(entity2.Index);
				m_SubAreas.TryGetBuffer(entity2, out var bufferData);
				targetSeeker.AddAreaTargets(ref random, target, entity2, entity, bufferData, 0f, addDistanceCost: true, EdgeFlags.DefaultMask);
			}
		}
	}

	[BurstCompile]
	private struct SetupSafetyJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			Entity entity2 = entity;
			if (targetSeeker.m_CurrentTransport.HasComponent(entity2))
			{
				entity2 = targetSeeker.m_CurrentTransport[entity2].m_CurrentTransport;
			}
			else if (targetSeeker.m_CurrentBuilding.HasComponent(entity2))
			{
				entity2 = targetSeeker.m_CurrentBuilding[entity2].m_CurrentBuilding;
			}
			if (!targetSeeker.m_Transform.HasComponent(entity2))
			{
				return;
			}
			float3 position = targetSeeker.m_Transform[entity2].m_Position;
			if (targetSeeker.m_Building.HasComponent(entity2))
			{
				Building building = targetSeeker.m_Building[entity2];
				if (targetSeeker.m_SubLane.HasBuffer(building.m_RoadEdge))
				{
					Random random = targetSeeker.m_RandomSeed.GetRandom(building.m_RoadEdge.Index);
					targetSeeker.AddEdgeTargets(ref random, entity, 0f, EdgeFlags.DefaultMask, building.m_RoadEdge, position, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: true);
				}
			}
			float num = 100f;
			TargetIterator iterator = new TargetIterator
			{
				m_Entity = entity2,
				m_Bounds = new Bounds3(position - num, position + num),
				m_Position = position,
				m_MaxDistance = num,
				m_TargetSeeker = targetSeeker,
				m_Flags = EdgeFlags.DefaultMask,
				m_CompositionData = m_CompositionData,
				m_NetCompositionData = m_NetCompositionData
			};
			m_NetSearchTree.Iterate(ref iterator);
		}
	}

	public struct TargetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_Entity;

		public Bounds3 m_Bounds;

		public float3 m_Position;

		public float m_MaxDistance;

		public PathfindTargetSeeker<PathfindSetupBuffer> m_TargetSeeker;

		public EdgeFlags m_Flags;

		public ComponentLookup<Composition> m_CompositionData;

		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_CompositionData.HasComponent(edgeEntity))
			{
				return;
			}
			Composition composition = m_CompositionData[edgeEntity];
			NetCompositionData netCompositionData = m_NetCompositionData[composition.m_Edge];
			bool flag = false;
			if ((m_TargetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Road) != 0)
			{
				flag |= (netCompositionData.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) != 0;
			}
			if ((m_TargetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0)
			{
				flag |= (netCompositionData.m_State & CompositionState.HasPedestrianLanes) != 0;
			}
			if (flag)
			{
				float t;
				float num = MathUtils.Distance(m_TargetSeeker.m_Curve[edgeEntity].m_Bezier, m_Position, out t) - netCompositionData.m_Width * 0.5f;
				if (num < m_MaxDistance)
				{
					Random random = m_TargetSeeker.m_RandomSeed.GetRandom(edgeEntity.Index);
					m_TargetSeeker.AddEdgeTargets(ref random, m_Entity, num, m_Flags, edgeEntity, m_Position, m_MaxDistance, allowLaneGroupSwitch: true, allowAccessRestriction: false);
				}
			}
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private ComponentLookup<PathOwner> m_PathOwnerData;

	private ComponentLookup<Vehicle> m_VehicleData;

	private ComponentLookup<Composition> m_CompositionData;

	private ComponentLookup<NetCompositionData> m_NetCompositionData;

	private ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

	private ComponentLookup<Creature> m_CreatureData;

	private ComponentLookup<AccidentSite> m_AccidentSiteData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<Game.Areas.SubArea> m_SubAreas;

	private BufferLookup<TargetElement> m_TargetElements;

	public CommonPathfindSetup(PathfindSetupSystem system)
	{
		m_NetSearchSystem = system.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_PathOwnerData = system.GetComponentLookup<PathOwner>(isReadOnly: true);
		m_VehicleData = system.GetComponentLookup<Vehicle>(isReadOnly: true);
		m_CompositionData = system.GetComponentLookup<Composition>(isReadOnly: true);
		m_NetCompositionData = system.GetComponentLookup<NetCompositionData>(isReadOnly: true);
		m_PlaceableObjectData = system.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
		m_CreatureData = system.GetComponentLookup<Creature>(isReadOnly: true);
		m_AccidentSiteData = system.GetComponentLookup<AccidentSite>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_SubAreas = system.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
		m_TargetElements = system.GetBufferLookup<TargetElement>(isReadOnly: true);
	}

	public JobHandle SetupCurrentLocation(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_PathOwnerData.Update(system);
		m_VehicleData.Update(system);
		m_CompositionData.Update(system);
		m_NetCompositionData.Update(system);
		m_PlaceableObjectData.Update(system);
		m_PathElements.Update(system);
		m_SubAreas.Update(system);
		JobHandle dependencies;
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(new SetupCurrentLocationJob
		{
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_PathOwnerData = m_PathOwnerData,
			m_VehicleData = m_VehicleData,
			m_CompositionData = m_CompositionData,
			m_NetCompositionData = m_NetCompositionData,
			m_PlaceableObjectData = m_PlaceableObjectData,
			m_PathElements = m_PathElements,
			m_SubAreas = m_SubAreas,
			m_SetupData = setupData
		}, setupData.Length, 1, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}

	public JobHandle SetupAccidentLocation(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_CreatureData.Update(system);
		m_VehicleData.Update(system);
		m_AccidentSiteData.Update(system);
		m_CompositionData.Update(system);
		m_NetCompositionData.Update(system);
		m_TargetElements.Update(system);
		m_SubAreas.Update(system);
		JobHandle dependencies;
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(new SetupAccidentLocationJob
		{
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CreatureData = m_CreatureData,
			m_VehicleData = m_VehicleData,
			m_AccidentSiteData = m_AccidentSiteData,
			m_CompositionData = m_CompositionData,
			m_NetCompositionData = m_NetCompositionData,
			m_TargetElements = m_TargetElements,
			m_SubAreas = m_SubAreas,
			m_SetupData = setupData
		}, setupData.Length, 1, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}

	public JobHandle SetupSafety(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_CompositionData.Update(system);
		m_NetCompositionData.Update(system);
		JobHandle dependencies;
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(new SetupSafetyJob
		{
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CompositionData = m_CompositionData,
			m_NetCompositionData = m_NetCompositionData,
			m_SetupData = setupData
		}, setupData.Length, 1, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}
}
