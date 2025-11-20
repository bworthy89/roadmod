using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
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
public class WorkWatercraftAISystem : GameSystemBase
{
	private struct WorkAction
	{
		public VehicleWorkType m_WorkType;

		public Entity m_Target;

		public Entity m_Owner;

		public float m_WorkAmount;
	}

	[BurstCompile]
	private struct WorkWatercraftTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentRoute> m_CurrentRouteType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Watercraft> m_WatercraftType;

		public ComponentTypeHandle<WatercraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public ComponentTypeHandle<Game.Vehicles.WorkVehicle> m_WorkVehicleType;

		public BufferTypeHandle<WatercraftNavigationLane> m_NavigationLaneType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Plant> m_PlantData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> m_ExtractorFacilityData;

		[ReadOnly]
		public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> m_PrefabWorkVehicleData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public ComponentLookup<WorkStopData> m_PrefabWorkStopData;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> m_PrefabNavigationAreaData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Route> m_RouteData;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_WaypointData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<BoardingVehicle> m_BoardingVehicleData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<VehicleModel> m_VehicleModels;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<WorkAction>.ParallelWriter m_WorkQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PathInformation> nativeArray3 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CurrentRoute> nativeArray4 = chunk.GetNativeArray(ref m_CurrentRouteType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<WatercraftCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Watercraft> nativeArray7 = chunk.GetNativeArray(ref m_WatercraftType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Game.Vehicles.WorkVehicle> nativeArray10 = chunk.GetNativeArray(ref m_WorkVehicleType);
			BufferAccessor<WatercraftNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_NavigationLaneType);
			BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PathInformation pathInformation = nativeArray3[i];
				PrefabRef prefabRef = nativeArray5[i];
				Watercraft watercraft = nativeArray7[i];
				WatercraftCurrentLane currentLane = nativeArray6[i];
				PathOwner pathOwner = nativeArray9[i];
				Target target = nativeArray8[i];
				Game.Vehicles.WorkVehicle workVehicle = nativeArray10[i];
				DynamicBuffer<WatercraftNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<PathElement> path = bufferAccessor2[i];
				CollectionUtils.TryGet(nativeArray4, i, out var value);
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, pathInformation, value, prefabRef, navigationLanes, path, ref workVehicle, ref watercraft, ref currentLane, ref pathOwner, ref target);
				nativeArray7[i] = watercraft;
				nativeArray6[i] = currentLane;
				nativeArray9[i] = pathOwner;
				nativeArray8[i] = target;
				nativeArray10[i] = workVehicle;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PathInformation pathInformation, CurrentRoute currentRoute, PrefabRef prefabRef, DynamicBuffer<WatercraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> path, ref Game.Vehicles.WorkVehicle workVehicle, ref Watercraft watercraft, ref WatercraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner) && !ResetPath(jobIndex, vehicleEntity, pathInformation, path, ref workVehicle, ref watercraft, ref currentLane, ref target, ref pathOwner))
			{
				ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref watercraft, ref pathOwner, ref target);
				FindPathIfNeeded(vehicleEntity, owner, currentRoute, prefabRef, Entity.Null, ref workVehicle, ref watercraft, ref currentLane, ref pathOwner, ref target);
				return;
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref watercraft, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if (IsWorkStop(currentRoute, ref target, out var workLocation))
				{
					bool abandonRoute = false;
					if ((workLocation && PerformWork(jobIndex, vehicleEntity, owner, currentRoute, prefabRef, ref workVehicle, ref target, ref pathOwner)) || (!workLocation && ShouldStartWork(currentRoute, prefabRef, ref workVehicle, out abandonRoute)))
					{
						SetNextWaypointTarget(owner, currentRoute, ref workVehicle, ref pathOwner, ref target);
					}
					else if (abandonRoute)
					{
						ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref watercraft, ref pathOwner, ref target);
					}
				}
				else
				{
					if ((workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
						return;
					}
					if (PerformWork(jobIndex, vehicleEntity, owner, currentRoute, prefabRef, ref workVehicle, ref target, ref pathOwner) && !TrySetWaypointTarget(owner, currentRoute, ref workVehicle, ref pathOwner, ref target))
					{
						ReturnToDepot(jobIndex, vehicleEntity, owner, ref workVehicle, ref watercraft, ref pathOwner, ref target);
					}
				}
			}
			Entity skipWaypoint = Entity.Null;
			watercraft.m_Flags |= WatercraftFlags.Working;
			if ((workVehicle.m_State & WorkVehicleFlags.Arriving) == 0)
			{
				CheckNavigationLanes(owner, currentRoute, navigationLanes, ref workVehicle, ref currentLane, ref pathOwner, ref target, out skipWaypoint);
			}
			FindPathIfNeeded(vehicleEntity, owner, currentRoute, prefabRef, skipWaypoint, ref workVehicle, ref watercraft, ref currentLane, ref pathOwner, ref target);
		}

		private bool IsWorkStop(CurrentRoute currentRoute, ref Target target, out bool workLocation)
		{
			workLocation = false;
			if (!m_EntityLookup.Exists(currentRoute.m_Route))
			{
				return false;
			}
			if (m_ConnectedData.TryGetComponent(target.m_Target, out var componentData) && m_PrefabRefData.TryGetComponent(componentData.m_Connected, out var componentData2) && m_PrefabWorkStopData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
			{
				workLocation = componentData3.m_WorkLocation;
				return true;
			}
			return false;
		}

		private void CheckNavigationLanes(Owner owner, CurrentRoute currentRoute, DynamicBuffer<WatercraftNavigationLane> navigationLanes, ref Game.Vehicles.WorkVehicle workVehicle, ref WatercraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target, out Entity skipWaypoint)
		{
			skipWaypoint = Entity.Null;
			if (navigationLanes.Length == 0 || navigationLanes.Length == 8)
			{
				return;
			}
			WatercraftNavigationLane value = navigationLanes[navigationLanes.Length - 1];
			if ((value.m_Flags & WatercraftLaneFlags.EndOfPath) == 0)
			{
				return;
			}
			if (m_WaypointData.HasComponent(target.m_Target) && m_RouteWaypoints.HasBuffer(currentRoute.m_Route) && (!m_ConnectedData.TryGetComponent(target.m_Target, out var componentData) || !m_BoardingVehicleData.HasComponent(componentData.m_Connected)))
			{
				if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete)) == 0)
				{
					skipWaypoint = target.m_Target;
					SetNextWaypointTarget(owner, currentRoute, ref workVehicle, ref pathOwner, ref target);
					if ((value.m_Flags & WatercraftLaneFlags.GroupTarget) != 0)
					{
						navigationLanes.RemoveAt(navigationLanes.Length - 1);
						return;
					}
					value.m_Flags &= ~WatercraftLaneFlags.EndOfPath;
					navigationLanes[navigationLanes.Length - 1] = value;
				}
				return;
			}
			workVehicle.m_State |= WorkVehicleFlags.Arriving;
			if (!m_RouteLaneData.TryGetComponent(target.m_Target, out var componentData2))
			{
				return;
			}
			if (componentData2.m_StartLane != componentData2.m_EndLane)
			{
				value.m_CurvePosition.y = 1f;
				WatercraftNavigationLane elem = new WatercraftNavigationLane
				{
					m_Lane = value.m_Lane
				};
				if (FindNextLane(ref elem.m_Lane))
				{
					value.m_Flags &= ~WatercraftLaneFlags.EndOfPath;
					navigationLanes[navigationLanes.Length - 1] = value;
					elem.m_Flags |= WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.FixedLane;
					elem.m_CurvePosition = new float2(0f, componentData2.m_EndCurvePos);
					navigationLanes.Add(elem);
				}
				else
				{
					navigationLanes[navigationLanes.Length - 1] = value;
				}
			}
			else
			{
				value.m_CurvePosition.y = componentData2.m_EndCurvePos;
				navigationLanes[navigationLanes.Length - 1] = value;
			}
		}

		private bool FindNextLane(ref Entity lane)
		{
			if (!m_OwnerData.HasComponent(lane) || !m_LaneData.HasComponent(lane))
			{
				return false;
			}
			Owner owner = m_OwnerData[lane];
			Lane lane2 = m_LaneData[lane];
			if (!m_SubLanes.HasBuffer(owner.m_Owner))
			{
				return false;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				Lane lane3 = m_LaneData[subLane];
				if (lane2.m_EndNode.Equals(lane3.m_StartNode))
				{
					lane = subLane;
					return true;
				}
			}
			return false;
		}

		private void FindPathIfNeeded(Entity vehicleEntity, Owner owner, CurrentRoute currentRoute, PrefabRef prefabRef, Entity skipWaypoint, ref Game.Vehicles.WorkVehicle workVehicle, ref Watercraft watercraft, ref WatercraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (!VehicleUtils.RequireNewPath(pathOwner))
			{
				return;
			}
			WatercraftData watercraftData = m_PrefabWatercraftData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = watercraftData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (VehicleUtils.GetPathMethods(watercraftData) | PathMethod.Offroad),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (VehicleUtils.GetPathMethods(watercraftData) | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Watercraft
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (VehicleUtils.GetPathMethods(watercraftData) | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Watercraft,
				m_Entity = target.m_Target
			};
			if (skipWaypoint != Entity.Null)
			{
				origin.m_Entity = skipWaypoint;
				pathOwner.m_State |= PathFlags.Append;
			}
			else
			{
				pathOwner.m_State &= ~PathFlags.Append;
			}
			WorkVehicleData workVehicleData = m_PrefabWorkVehicleData[prefabRef.m_Prefab];
			if (m_EntityLookup.Exists(currentRoute.m_Route))
			{
				if ((workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
				{
					workVehicle.m_State &= ~WorkVehicleFlags.RouteSource;
				}
				else
				{
					if ((workVehicle.m_State & WorkVehicleFlags.RouteSource) != 0)
					{
						parameters.m_PathfindFlags |= PathfindFlags.Stable | PathfindFlags.IgnoreFlow;
					}
					if ((workVehicle.m_State & WorkVehicleFlags.WorkLocation) != 0)
					{
						workVehicle.m_State &= ~WorkVehicleFlags.RouteSource;
						destination.m_Type = SetupTargetType.AreaLocation;
						destination.m_Entity = owner.m_Owner;
						destination.m_Value = (int)workVehicleData.m_WorkType;
						target.m_Target = owner.m_Owner;
					}
				}
			}
			else
			{
				if ((workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.ExtractorVehicle)) == WorkVehicleFlags.ExtractorVehicle)
				{
					if (workVehicleData.m_MapFeature == MapFeature.Forest)
					{
						destination.m_Type = SetupTargetType.WoodResource;
					}
					else
					{
						destination.m_Type = SetupTargetType.AreaLocation;
					}
					destination.m_Entity = owner.m_Owner;
					destination.m_Value = (int)workVehicleData.m_WorkType;
					target.m_Target = owner.m_Owner;
				}
				else if ((workVehicle.m_State & (WorkVehicleFlags.Returning | WorkVehicleFlags.StorageVehicle)) == WorkVehicleFlags.StorageVehicle)
				{
					destination.m_Type = SetupTargetType.AreaLocation;
					destination.m_Entity = owner.m_Owner;
					destination.m_Value = (int)workVehicleData.m_WorkType;
					target.m_Target = owner.m_Owner;
				}
				workVehicle.m_State &= ~WorkVehicleFlags.RouteSource;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private bool TrySetWaypointTarget(Owner owner, CurrentRoute currentRoute, ref Game.Vehicles.WorkVehicle workVehicle, ref PathOwner pathOwner, ref Target target)
		{
			workVehicle.m_State &= ~WorkVehicleFlags.WorkLocation;
			if (m_RouteWaypoints.TryGetBuffer(currentRoute.m_Route, out var bufferData) && m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					RouteWaypoint routeWaypoint = bufferData[i];
					if (m_ConnectedData.TryGetComponent(routeWaypoint.m_Waypoint, out var componentData2) && m_OwnerData.TryGetComponent(componentData2.m_Connected, out var componentData3) && componentData3.m_Owner == componentData.m_Owner)
					{
						target.m_Target = routeWaypoint.m_Waypoint;
						SetNextWaypointTarget(owner, currentRoute, ref workVehicle, ref pathOwner, ref target);
						return true;
					}
				}
			}
			return false;
		}

		private void SetNextWaypointTarget(Owner owner, CurrentRoute currentRoute, ref Game.Vehicles.WorkVehicle workVehicle, ref PathOwner pathOwner, ref Target target)
		{
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[currentRoute.m_Route];
			int num = m_WaypointData[target.m_Target].m_Index + 1;
			num = math.select(num, 0, num >= dynamicBuffer.Length);
			Entity waypoint = dynamicBuffer[num].m_Waypoint;
			if (m_ConnectedData.TryGetComponent(waypoint, out var componentData) && m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData2) && m_OwnerData.TryGetComponent(componentData.m_Connected, out var componentData3) && m_PrefabRefData.TryGetComponent(componentData.m_Connected, out var componentData4) && m_PrefabWorkStopData.TryGetComponent(componentData4.m_Prefab, out var componentData5) && HasNavigation(owner.m_Owner, RoadTypes.Watercraft) && componentData3.m_Owner == componentData2.m_Owner && componentData5.m_WorkLocation)
			{
				VehicleUtils.SetTarget(ref pathOwner, ref target, owner.m_Owner);
				workVehicle.m_State |= WorkVehicleFlags.RouteSource | WorkVehicleFlags.WorkLocation;
			}
			else
			{
				VehicleUtils.SetTarget(ref pathOwner, ref target, dynamicBuffer[num].m_Waypoint);
				workVehicle.m_State |= WorkVehicleFlags.RouteSource;
			}
		}

		private bool HasNavigation(Entity entity, RoadTypes roadTypes)
		{
			if (HasNavigationSelf(entity, roadTypes))
			{
				return true;
			}
			if (m_SubAreas.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (HasNavigationSelf(bufferData[i].m_Area, roadTypes))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool HasNavigationSelf(Entity entity, RoadTypes roadTypes)
		{
			if (m_PrefabRefData.TryGetComponent(entity, out var componentData) && m_PrefabNavigationAreaData.TryGetComponent(componentData.m_Prefab, out var componentData2))
			{
				return (componentData2.m_RoadTypes & roadTypes) != 0;
			}
			return false;
		}

		private bool ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<PathElement> path, ref Game.Vehicles.WorkVehicle workVehicle, ref Watercraft watercraft, ref WatercraftCurrentLane currentLane, ref Target target, ref PathOwner pathOwner)
		{
			workVehicle.m_State &= ~WorkVehicleFlags.Arriving;
			if ((pathOwner.m_State & PathFlags.Append) == 0)
			{
				PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			}
			if ((workVehicle.m_State & WorkVehicleFlags.Returning) != 0)
			{
				watercraft.m_Flags &= ~WatercraftFlags.StayOnWaterway;
			}
			else
			{
				watercraft.m_Flags |= WatercraftFlags.StayOnWaterway;
				target.m_Target = pathInformation.m_Destination;
			}
			return true;
		}

		private void ReturnToDepot(int jobIndex, Entity vehicleEntity, Owner ownerData, ref Game.Vehicles.WorkVehicle workVehicle, ref Watercraft watercraft, ref PathOwner pathOwner, ref Target target)
		{
			workVehicle.m_State &= ~WorkVehicleFlags.WorkLocation;
			workVehicle.m_State |= WorkVehicleFlags.Returning;
			Entity newTarget = ownerData.m_Owner;
			if (m_OwnerData.HasComponent(ownerData.m_Owner))
			{
				Owner owner = m_OwnerData[ownerData.m_Owner];
				newTarget = ((!m_AttachmentData.HasComponent(owner.m_Owner)) ? owner.m_Owner : m_AttachmentData[owner.m_Owner].m_Attached);
			}
			VehicleUtils.SetTarget(ref pathOwner, ref target, newTarget);
		}

		private bool ShouldStartWork(CurrentRoute currentRoute, PrefabRef prefabRef, ref Game.Vehicles.WorkVehicle workVehicle, out bool abandonRoute)
		{
			abandonRoute = false;
			if (m_RouteData.TryGetComponent(currentRoute.m_Route, out var componentData) && RouteUtils.CheckOption(componentData, RouteOption.Inactive))
			{
				return false;
			}
			if (!CheckVehicleModel(currentRoute, prefabRef))
			{
				abandonRoute = true;
				return false;
			}
			float num = math.min(workVehicle.m_WorkAmount, workVehicle.m_DoneAmount);
			workVehicle.m_WorkAmount -= num;
			workVehicle.m_DoneAmount -= num;
			return workVehicle.m_DoneAmount < workVehicle.m_WorkAmount;
		}

		private bool CheckVehicleModel(CurrentRoute currentRoute, PrefabRef prefabRef)
		{
			if (m_VehicleModels.TryGetBuffer(currentRoute.m_Route, out var bufferData))
			{
				return RouteUtils.CheckVehicleModel(bufferData, prefabRef);
			}
			return true;
		}

		private bool PerformWork(int jobIndex, Entity vehicleEntity, Owner owner, CurrentRoute currentRoute, PrefabRef prefabRef, ref Game.Vehicles.WorkVehicle workVehicle, ref Target target, ref PathOwner pathOwner)
		{
			WorkVehicleData workVehicleData = m_PrefabWorkVehicleData[prefabRef.m_Prefab];
			float num = workVehicleData.m_MaxWorkAmount;
			Route componentData;
			bool flag = (m_RouteData.TryGetComponent(currentRoute.m_Route, out componentData) && RouteUtils.CheckOption(componentData, RouteOption.Inactive)) || !CheckVehicleModel(currentRoute, prefabRef);
			if (!flag)
			{
				if ((workVehicle.m_State & WorkVehicleFlags.ExtractorVehicle) != 0)
				{
					switch (workVehicleData.m_WorkType)
					{
					case VehicleWorkType.Harvest:
						num = 1000f;
						if (m_TreeData.HasComponent(target.m_Target))
						{
							Tree tree = m_TreeData[target.m_Target];
							Plant plant = m_PlantData[target.m_Target];
							PrefabRef prefabRef2 = m_PrefabRefData[target.m_Target];
							m_DamagedData.TryGetComponent(target.m_Target, out var componentData2);
							if (m_PrefabTreeData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
							{
								num = ObjectUtils.CalculateWoodAmount(tree, plant, componentData2, componentData3);
							}
							m_CommandBuffer.AddComponent(jobIndex, target.m_Target, default(BatchesUpdated));
						}
						else if (m_ExtractorFacilityData.HasComponent(target.m_Target))
						{
							num = workVehicleData.m_MaxWorkAmount * 0.5f;
						}
						m_WorkQueue.Enqueue(new WorkAction
						{
							m_WorkType = workVehicleData.m_WorkType,
							m_Target = target.m_Target,
							m_Owner = owner.m_Owner,
							m_WorkAmount = num
						});
						break;
					case VehicleWorkType.Collect:
						if (m_TreeData.HasComponent(target.m_Target))
						{
							m_WorkQueue.Enqueue(new WorkAction
							{
								m_WorkType = workVehicleData.m_WorkType,
								m_Target = target.m_Target
							});
						}
						num = workVehicleData.m_MaxWorkAmount * 0.25f;
						break;
					}
				}
				else if ((workVehicle.m_State & WorkVehicleFlags.StorageVehicle) != 0)
				{
					num = workVehicleData.m_MaxWorkAmount * 0.25f;
				}
			}
			if ((workVehicle.m_State & WorkVehicleFlags.WorkLocation) != 0)
			{
				VehicleUtils.SetTarget(ref pathOwner, ref target, Entity.Null);
			}
			if (flag)
			{
				return true;
			}
			QuantityUpdated(jobIndex, vehicleEntity);
			workVehicle.m_DoneAmount += num;
			return workVehicle.m_DoneAmount > workVehicle.m_WorkAmount - 1f;
		}

		private void QuantityUpdated(int jobIndex, Entity vehicleEntity)
		{
			if (m_SubObjects.HasBuffer(vehicleEntity))
			{
				DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[vehicleEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subObject = dynamicBuffer[i].m_SubObject;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct WorkWatercraftWorkJob : IJob
	{
		public ComponentLookup<Tree> m_TreeData;

		public ComponentLookup<Extractor> m_ExtractorData;

		public NativeQueue<WorkAction> m_WorkQueue;

		public void Execute()
		{
			int count = m_WorkQueue.Count;
			for (int i = 0; i < count; i++)
			{
				WorkAction workAction = m_WorkQueue.Dequeue();
				switch (workAction.m_WorkType)
				{
				case VehicleWorkType.Harvest:
				{
					float num = 0f;
					if (m_TreeData.HasComponent(workAction.m_Target))
					{
						Tree value2 = m_TreeData[workAction.m_Target];
						if ((value2.m_State & TreeState.Stump) == 0)
						{
							value2.m_State &= ~(TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Collected);
							value2.m_State |= TreeState.Stump;
							value2.m_Growth = 0;
							m_TreeData[workAction.m_Target] = value2;
							num = workAction.m_WorkAmount;
						}
					}
					if (m_ExtractorData.HasComponent(workAction.m_Owner))
					{
						Extractor value3 = m_ExtractorData[workAction.m_Owner];
						value3.m_ExtractedAmount -= num;
						value3.m_HarvestedAmount += workAction.m_WorkAmount;
						m_ExtractorData[workAction.m_Owner] = value3;
					}
					break;
				}
				case VehicleWorkType.Collect:
					if (m_TreeData.HasComponent(workAction.m_Target))
					{
						Tree value = m_TreeData[workAction.m_Target];
						if ((value.m_State & TreeState.Collected) == 0)
						{
							value.m_State |= TreeState.Collected;
							m_TreeData[workAction.m_Target] = value;
						}
					}
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Watercraft> __Game_Vehicles_Watercraft_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.WorkVehicle> __Game_Vehicles_WorkVehicle_RW_ComponentTypeHandle;

		public BufferTypeHandle<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ExtractorFacility> __Game_Buildings_ExtractorFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftData> __Game_Prefabs_WatercraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> __Game_Prefabs_WorkVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkStopData> __Game_Prefabs_WorkStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> __Game_Prefabs_NavigationAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Route> __Game_Routes_Route_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BoardingVehicle> __Game_Routes_BoardingVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<VehicleModel> __Game_Routes_VehicleModel_RO_BufferLookup;

		public ComponentLookup<Tree> __Game_Objects_Tree_RW_ComponentLookup;

		public ComponentLookup<Extractor> __Game_Areas_Extractor_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentRoute>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Watercraft>();
			__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_WorkVehicle_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.WorkVehicle>();
			__Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<WatercraftNavigationLane>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Buildings_ExtractorFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ExtractorFacility>(isReadOnly: true);
			__Game_Prefabs_WatercraftData_RO_ComponentLookup = state.GetComponentLookup<WatercraftData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WorkVehicleData_RO_ComponentLookup = state.GetComponentLookup<WorkVehicleData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Prefabs_WorkStopData_RO_ComponentLookup = state.GetComponentLookup<WorkStopData>(isReadOnly: true);
			__Game_Prefabs_NavigationAreaData_RO_ComponentLookup = state.GetComponentLookup<NavigationAreaData>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Routes_Route_RO_ComponentLookup = state.GetComponentLookup<Route>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_BoardingVehicle_RO_ComponentLookup = state.GetComponentLookup<BoardingVehicle>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_VehicleModel_RO_BufferLookup = state.GetBufferLookup<VehicleModel>(isReadOnly: true);
			__Game_Objects_Tree_RW_ComponentLookup = state.GetComponentLookup<Tree>();
			__Game_Areas_Extractor_RW_ComponentLookup = state.GetComponentLookup<Extractor>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 8;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Vehicles.WorkVehicle>(), ComponentType.ReadWrite<WatercraftCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<WorkAction> workQueue = new NativeQueue<WorkAction>(Allocator.TempJob);
		WorkWatercraftTickJob jobData = new WorkWatercraftTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentRouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Watercraft_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WorkVehicle_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ExtractorFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WatercraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNavigationAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NavigationAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Route_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BoardingVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_VehicleModels = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_VehicleModel_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_WorkQueue = workQueue.AsParallelWriter()
		};
		WorkWatercraftWorkJob jobData2 = new WorkWatercraftWorkJob
		{
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WorkQueue = workQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		workQueue.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle2;
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
	public WorkWatercraftAISystem()
	{
	}
}
