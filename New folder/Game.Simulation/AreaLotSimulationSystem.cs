using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AreaLotSimulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct ManageVehiclesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<WoodResource> m_WoodResourceType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		public ComponentTypeHandle<Extractor> m_ExtractorType;

		public ComponentTypeHandle<Storage> m_StorageType;

		public ComponentTypeHandle<Game.Buildings.CargoTransportStation> m_CargoTransportStationType;

		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<WorkRoute> m_WorkRouteData;

		[ReadOnly]
		public ComponentLookup<Route> m_RouteData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_PrefabExtractorAreaData;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> m_PrefabStorageAreaData;

		[ReadOnly]
		public ComponentLookup<LotData> m_PrefabLotData;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> m_PrefabWorkVehicleData;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> m_PrefabNavigationAreaData;

		[ReadOnly]
		public ComponentLookup<CargoTransportStationData> m_PrefabCargoTransportStationData;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_PrefabStorageCompanyData;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_VehicleLayouts;

		[ReadOnly]
		public BufferLookup<SubRoute> m_SubRoutes;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicles;

		[ReadOnly]
		public BufferLookup<VehicleModel> m_VehicleModels;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.WorkVehicle> m_WorkVehicleData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public WorkVehicleSelectData m_WorkVehicleSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Extractor> nativeArray2 = chunk.GetNativeArray(ref m_ExtractorType);
			NativeArray<Storage> nativeArray3 = chunk.GetNativeArray(ref m_StorageType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<Game.Buildings.CargoTransportStation> nativeArray5 = chunk.GetNativeArray(ref m_CargoTransportStationType);
			NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (nativeArray4.Length != 0)
			{
				BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
				for (int i = 0; i < nativeArray4.Length; i++)
				{
					Entity entity = nativeArray[i];
					PathInformation pathInformation = nativeArray4[i];
					DynamicBuffer<PathElement> path = bufferAccessor[i];
					PrefabRef prefabRef = nativeArray6[i];
					m_PrefabLotData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
					if (nativeArray2.Length != 0)
					{
						Extractor value = nativeArray2[i];
						ExtractorAreaData extractorAreaData = m_PrefabExtractorAreaData[prefabRef.m_Prefab];
						switch (value.m_WorkType)
						{
						case VehicleWorkType.Harvest:
							TrySpawnVehicle(unfilteredChunkIndex, ref random, entity, Entity.Null, pathInformation, path, value.m_WorkType, extractorAreaData.m_MapFeature, Resource.NoResource, WorkVehicleFlags.ExtractorVehicle, componentData.m_OnWater, ref value.m_WorkAmount);
							break;
						case VehicleWorkType.Collect:
							TrySpawnVehicle(unfilteredChunkIndex, ref random, entity, Entity.Null, pathInformation, path, value.m_WorkType, extractorAreaData.m_MapFeature, Resource.NoResource, WorkVehicleFlags.ExtractorVehicle, componentData.m_OnWater, ref value.m_HarvestedAmount);
							break;
						}
						nativeArray2[i] = value;
					}
					if (nativeArray3.Length != 0)
					{
						Storage value2 = nativeArray3[i];
						StorageAreaData storageAreaData = m_PrefabStorageAreaData[prefabRef.m_Prefab];
						TrySpawnVehicle(unfilteredChunkIndex, ref random, entity, Entity.Null, pathInformation, path, VehicleWorkType.Collect, MapFeature.None, storageAreaData.m_Resources, WorkVehicleFlags.StorageVehicle, componentData.m_OnWater, ref value2.m_WorkAmount);
						nativeArray3[i] = value2;
					}
					if (nativeArray5.Length != 0)
					{
						Game.Buildings.CargoTransportStation value3 = nativeArray5[i];
						CargoTransportStationData cargoTransportStationData = m_PrefabCargoTransportStationData[prefabRef.m_Prefab];
						StorageCompanyData storageCompanyData = m_PrefabStorageCompanyData[prefabRef.m_Prefab];
						float lotWorkAmount = value3.m_WorkAmount * cargoTransportStationData.m_WorkMultiplier;
						TrySpawnVehicle(unfilteredChunkIndex, ref random, entity, Entity.Null, pathInformation, path, VehicleWorkType.Move, MapFeature.None, storageCompanyData.m_StoredResources, WorkVehicleFlags.CargoMoveVehicle, componentData.m_OnWater, ref lotWorkAmount);
						value3.m_WorkAmount = lotWorkAmount / cargoTransportStationData.m_WorkMultiplier;
						nativeArray5[i] = value3;
					}
					m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
					m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
				}
				return;
			}
			NativeArray<Owner> nativeArray7 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<WoodResource> bufferAccessor2 = chunk.GetBufferAccessor(ref m_WoodResourceType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			for (int j = 0; j < bufferAccessor3.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				PrefabRef prefabRef2 = nativeArray6[j];
				DynamicBuffer<OwnedVehicle> dynamicBuffer = bufferAccessor3[j];
				m_PrefabLotData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2);
				CollectionUtils.TryGet(nativeArray7, j, out var value4);
				CollectionUtils.TryGet(nativeArray2, j, out var value5);
				CollectionUtils.TryGet(nativeArray3, j, out var value6);
				CollectionUtils.TryGet(nativeArray5, j, out var value7);
				CollectionUtils.TryGet(bufferAccessor2, j, out var value8);
				float pendingWorkAmount = 0f;
				float pendingWorkAmount2 = 0f;
				float pendingWorkAmount3 = 0f;
				float pendingWorkAmount4 = 0f;
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity vehicle = dynamicBuffer[k].m_Vehicle;
					if (m_WorkVehicleData.TryGetComponent(vehicle, out var componentData3))
					{
						if (m_VehicleLayouts.TryGetBuffer(vehicle, out var bufferData) && bufferData.Length != 0)
						{
							for (int l = 0; l < bufferData.Length; l++)
							{
								Entity vehicle2 = bufferData[l].m_Vehicle;
								PrefabRef prefabRef3 = m_PrefabRefData[vehicle2];
								WorkVehicleData workVehicleData = m_PrefabWorkVehicleData[prefabRef3.m_Prefab];
								if ((componentData3.m_State & WorkVehicleFlags.ExtractorVehicle) != 0)
								{
									switch (workVehicleData.m_WorkType)
									{
									case VehicleWorkType.Harvest:
										CheckVehicle(vehicle2, workVehicleData, ref value5.m_WorkAmount, ref pendingWorkAmount);
										break;
									case VehicleWorkType.Collect:
										CheckVehicle(vehicle2, workVehicleData, ref value5.m_HarvestedAmount, ref pendingWorkAmount2);
										break;
									}
								}
								else if ((componentData3.m_State & WorkVehicleFlags.StorageVehicle) != 0)
								{
									CheckVehicle(vehicle2, workVehicleData, ref value6.m_WorkAmount, ref pendingWorkAmount3);
								}
								else if ((componentData3.m_State & WorkVehicleFlags.CargoMoveVehicle) != 0)
								{
									CargoTransportStationData cargoTransportStationData2 = m_PrefabCargoTransportStationData[prefabRef2.m_Prefab];
									float lotWorkAmount2 = value7.m_WorkAmount * cargoTransportStationData2.m_WorkMultiplier;
									CheckVehicle(vehicle2, workVehicleData, ref lotWorkAmount2, ref pendingWorkAmount4);
									if (lotWorkAmount2 != value7.m_WorkAmount * cargoTransportStationData2.m_WorkMultiplier)
									{
										value7.m_WorkAmount = lotWorkAmount2 / cargoTransportStationData2.m_WorkMultiplier;
									}
								}
							}
							continue;
						}
						PrefabRef prefabRef4 = m_PrefabRefData[vehicle];
						WorkVehicleData workVehicleData2 = m_PrefabWorkVehicleData[prefabRef4.m_Prefab];
						if ((componentData3.m_State & WorkVehicleFlags.ExtractorVehicle) != 0)
						{
							switch (workVehicleData2.m_WorkType)
							{
							case VehicleWorkType.Harvest:
								CheckVehicle(vehicle, workVehicleData2, ref value5.m_WorkAmount, ref pendingWorkAmount);
								break;
							case VehicleWorkType.Collect:
								CheckVehicle(vehicle, workVehicleData2, ref value5.m_HarvestedAmount, ref pendingWorkAmount2);
								break;
							}
						}
						else if ((componentData3.m_State & WorkVehicleFlags.StorageVehicle) != 0)
						{
							CheckVehicle(vehicle, workVehicleData2, ref value6.m_WorkAmount, ref pendingWorkAmount3);
						}
						else if ((componentData3.m_State & WorkVehicleFlags.CargoMoveVehicle) != 0)
						{
							CargoTransportStationData cargoTransportStationData3 = m_PrefabCargoTransportStationData[prefabRef2.m_Prefab];
							float lotWorkAmount3 = value7.m_WorkAmount * cargoTransportStationData3.m_WorkMultiplier;
							CheckVehicle(vehicle, workVehicleData2, ref lotWorkAmount3, ref pendingWorkAmount4);
							if (lotWorkAmount3 != value7.m_WorkAmount * cargoTransportStationData3.m_WorkMultiplier)
							{
								value7.m_WorkAmount = lotWorkAmount3 / cargoTransportStationData3.m_WorkMultiplier;
							}
						}
					}
					else if (!m_PrefabRefData.HasComponent(vehicle))
					{
						dynamicBuffer.RemoveAtSwapBack(k--);
					}
				}
				if (nativeArray2.Length != 0)
				{
					ExtractorAreaData extractorAreaData2 = m_PrefabExtractorAreaData[prefabRef2.m_Prefab];
					if (extractorAreaData2.m_MapFeature == MapFeature.Forest)
					{
						value5.m_ExtractedAmount = value5.m_WorkAmount + pendingWorkAmount;
					}
					if (value5.m_WorkAmount >= 1000f)
					{
						FindTarget(unfilteredChunkIndex, ref random, entity2, value4, extractorAreaData2.m_MapFeature, VehicleWorkType.Harvest, Resource.NoResource, WorkVehicleFlags.ExtractorVehicle, value8, componentData2.m_OnWater, ref value5.m_WorkAmount);
						value5.m_WorkType = VehicleWorkType.Harvest;
					}
					else if (value5.m_HarvestedAmount >= 1000f)
					{
						FindTarget(unfilteredChunkIndex, ref random, entity2, value4, extractorAreaData2.m_MapFeature, VehicleWorkType.Collect, Resource.NoResource, WorkVehicleFlags.ExtractorVehicle, value8, componentData2.m_OnWater, ref value5.m_HarvestedAmount);
						value5.m_WorkType = VehicleWorkType.Collect;
					}
					nativeArray2[j] = value5;
				}
				if (nativeArray3.Length != 0)
				{
					StorageAreaData storageAreaData2 = m_PrefabStorageAreaData[prefabRef2.m_Prefab];
					if (value6.m_WorkAmount >= 1000f)
					{
						FindTarget(unfilteredChunkIndex, ref random, entity2, value4, MapFeature.None, VehicleWorkType.Collect, storageAreaData2.m_Resources, WorkVehicleFlags.StorageVehicle, value8, componentData2.m_OnWater, ref value6.m_WorkAmount);
					}
					nativeArray3[j] = value6;
				}
				if (nativeArray5.Length == 0)
				{
					continue;
				}
				CargoTransportStationData cargoTransportStationData4 = m_PrefabCargoTransportStationData[prefabRef2.m_Prefab];
				StorageCompanyData storageCompanyData2 = m_PrefabStorageCompanyData[prefabRef2.m_Prefab];
				if (cargoTransportStationData4.m_WorkMultiplier > 0f)
				{
					float extractorWorkAmount = value7.m_WorkAmount * cargoTransportStationData4.m_WorkMultiplier;
					if (extractorWorkAmount >= 1000f)
					{
						FindTarget(unfilteredChunkIndex, ref random, entity2, new Owner(entity2), MapFeature.None, VehicleWorkType.Move, storageCompanyData2.m_StoredResources, WorkVehicleFlags.CargoMoveVehicle, value8, onWater: false, ref extractorWorkAmount);
						value7.m_WorkAmount = extractorWorkAmount / cargoTransportStationData4.m_WorkMultiplier;
					}
				}
				else
				{
					value7.m_WorkAmount = 0f;
				}
				nativeArray5[j] = value7;
			}
		}

		private void CheckVehicle(Entity vehicle, WorkVehicleData workVehicleData, ref float lotWorkAmount, ref float pendingWorkAmount)
		{
			Game.Vehicles.WorkVehicle value = m_WorkVehicleData[vehicle];
			if (lotWorkAmount >= 1f)
			{
				float num = (((value.m_State & WorkVehicleFlags.Returning) != 0) ? math.min(lotWorkAmount, value.m_DoneAmount - value.m_WorkAmount) : math.min(lotWorkAmount, workVehicleData.m_MaxWorkAmount - value.m_WorkAmount));
				if (num > 0f)
				{
					value.m_WorkAmount += num;
					lotWorkAmount -= num;
					m_WorkVehicleData[vehicle] = value;
				}
			}
			pendingWorkAmount += value.m_WorkAmount - value.m_DoneAmount;
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

		private void FindTarget(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Owner owner, MapFeature mapFeature, VehicleWorkType workType, Resource resource, WorkVehicleFlags flags, DynamicBuffer<WoodResource> woodResources, bool onWater, ref float extractorWorkAmount)
		{
			if (FindRoute(owner.m_Owner, out var route, out var targetWaypoint) && FindStartWaypoint(owner.m_Owner, route, out var firstWaypoint, out var nextWaypoint))
			{
				PathInformation pathInformation = new PathInformation
				{
					m_Origin = firstWaypoint,
					m_Destination = nextWaypoint
				};
				RoadTypes roadTypes = ((!onWater) ? RoadTypes.Car : RoadTypes.Watercraft);
				flags |= WorkVehicleFlags.RouteSource;
				if (targetWaypoint == nextWaypoint && HasNavigation(entity, roadTypes))
				{
					pathInformation.m_Destination = entity;
					flags |= WorkVehicleFlags.WorkLocation;
				}
				TrySpawnVehicle(jobIndex, ref random, entity, route, pathInformation, default(DynamicBuffer<PathElement>), workType, mapFeature, resource, flags, onWater, ref extractorWorkAmount);
				return;
			}
			Entity entity2 = Entity.Null;
			if (m_AttachmentData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				entity2 = componentData.m_Attached;
			}
			else if (m_TransformData.HasComponent(owner.m_Owner))
			{
				entity2 = owner.m_Owner;
			}
			if (entity2 != Entity.Null)
			{
				if (mapFeature == MapFeature.Forest)
				{
					if (woodResources.IsCreated && woodResources.Length != 0)
					{
						FindTarget(jobIndex, entity, entity2, SetupTargetType.WoodResource, workType, onWater);
					}
					else
					{
						extractorWorkAmount = 0f;
					}
				}
				else
				{
					FindTarget(jobIndex, entity, entity2, SetupTargetType.AreaLocation, workType, onWater);
				}
			}
			else
			{
				extractorWorkAmount = 0f;
			}
		}

		private bool FindRoute(Entity areaOwner, out Entity route, out Entity targetWaypoint)
		{
			Entity entity = areaOwner;
			if (m_OwnerData.TryGetComponent(areaOwner, out var componentData))
			{
				entity = componentData.m_Owner;
			}
			int num = int.MaxValue;
			route = Entity.Null;
			targetWaypoint = Entity.Null;
			if (m_SubRoutes.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					SubRoute subRoute = bufferData[i];
					if (!m_WorkRouteData.HasComponent(subRoute.m_Route) || !m_RouteData.TryGetComponent(subRoute.m_Route, out var componentData2) || !m_RouteWaypoints.TryGetBuffer(subRoute.m_Route, out var bufferData2) || RouteUtils.CheckOption(componentData2, RouteOption.Inactive))
					{
						continue;
					}
					for (int j = 0; j < bufferData2.Length; j++)
					{
						RouteWaypoint routeWaypoint = bufferData2[j];
						if (m_ConnectedData.TryGetComponent(routeWaypoint.m_Waypoint, out var componentData3) && m_OwnerData.TryGetComponent(componentData3.m_Connected, out componentData) && componentData.m_Owner == areaOwner)
						{
							int num2 = 0;
							if (m_RouteVehicles.TryGetBuffer(subRoute.m_Route, out var bufferData3))
							{
								num2 = bufferData3.Length;
							}
							if (num2 < num)
							{
								num = num2;
								route = subRoute.m_Route;
								targetWaypoint = routeWaypoint.m_Waypoint;
							}
							break;
						}
					}
				}
			}
			return route != Entity.Null;
		}

		private bool FindStartWaypoint(Entity areaOwner, Entity route, out Entity firstWaypoint, out Entity nextWaypoint)
		{
			if (m_RouteWaypoints.TryGetBuffer(route, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					RouteWaypoint routeWaypoint = bufferData[i];
					if (m_ConnectedData.TryGetComponent(routeWaypoint.m_Waypoint, out var componentData) && m_OwnerData.TryGetComponent(componentData.m_Connected, out var componentData2) && componentData2.m_Owner != areaOwner)
					{
						int num = i + 1;
						num = math.select(num, 0, num >= bufferData.Length);
						firstWaypoint = routeWaypoint.m_Waypoint;
						nextWaypoint = bufferData[num].m_Waypoint;
						return true;
					}
				}
			}
			firstWaypoint = Entity.Null;
			nextWaypoint = Entity.Null;
			return false;
		}

		private void FindTarget(int jobIndex, Entity owner, Entity source, SetupTargetType targetType, VehicleWorkType workType, bool onWater)
		{
			RoadTypes roadTypes = ((!onWater) ? RoadTypes.Car : RoadTypes.Watercraft);
			PathMethod pathMethod = PathMethod.Road | PathMethod.Offroad | PathMethod.MediumRoad;
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = pathMethod
			};
			if (workType == VehicleWorkType.Move)
			{
				parameters.m_Methods &= ~(PathMethod.Road | PathMethod.MediumRoad);
				pathMethod |= PathMethod.CargoLoading;
			}
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = pathMethod,
				m_RoadTypes = roadTypes,
				m_Entity = source
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = targetType,
				m_Methods = pathMethod,
				m_RoadTypes = roadTypes,
				m_Entity = owner,
				m_Value = (int)workType
			};
			m_PathfindQueue.Enqueue(new SetupQueueItem(owner, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, owner, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, owner);
		}

		private void TrySpawnVehicle(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, Entity route, PathInformation pathInformation, DynamicBuffer<PathElement> path, VehicleWorkType workType, MapFeature mapFeature, Resource resource, WorkVehicleFlags flags, bool onWater, ref float lotWorkAmount)
		{
			if (pathInformation.m_Destination != Entity.Null)
			{
				float workAmount = lotWorkAmount;
				if (SpawnVehicle(jobIndex, ref random, owner, route, pathInformation, path, workType, mapFeature, resource, flags, onWater, ref workAmount))
				{
					lotWorkAmount -= workAmount;
				}
				else
				{
					lotWorkAmount = 0f;
				}
			}
			else
			{
				lotWorkAmount = 0f;
			}
		}

		private bool SpawnVehicle(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, Entity route, PathInformation pathInformation, DynamicBuffer<PathElement> path, VehicleWorkType workType, MapFeature mapFeature, Resource resource, WorkVehicleFlags flags, bool onWater, ref float workAmount)
		{
			Entity entity = pathInformation.m_Origin;
			DynamicBuffer<VehicleModel> bufferData = default(DynamicBuffer<VehicleModel>);
			if (route != Entity.Null)
			{
				if (m_ConnectedData.TryGetComponent(entity, out var componentData))
				{
					entity = componentData.m_Connected;
				}
				m_VehicleModels.TryGetBuffer(route, out bufferData);
			}
			if (!m_TransformData.TryGetComponent(entity, out var componentData2))
			{
				return false;
			}
			RoadTypes roadTypes = ((!onWater) ? RoadTypes.Car : RoadTypes.Watercraft);
			SizeClass sizeClass = SizeClass.Undefined;
			Entity entity2 = m_WorkVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, bufferData, roadTypes, sizeClass, workType, mapFeature, resource, ref workAmount, componentData2, pathInformation.m_Origin, flags);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(pathInformation.m_Destination));
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(owner));
				if (route != Entity.Null)
				{
					m_CommandBuffer.AddComponent(jobIndex, entity2, new CurrentRoute(route));
				}
				else if (path.IsCreated && path.Length != 0)
				{
					DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
					PathUtils.CopyPath(path, default(PathOwner), 0, targetElements);
					m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
					m_CommandBuffer.SetComponent(jobIndex, entity2, pathInformation);
				}
				return true;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ExtractResourcesJob : IJob
	{
		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Extractor> m_ExtractorData;

			public BufferLookup<MapFeatureElement> m_MapFeatureElements;

			public NativeParallelHashSet<Entity> m_UpdateSet;

			public NativeList<Entity> m_UpdateList;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && (m_ExtractorData.HasComponent(item.m_Area) || m_MapFeatureElements.HasBuffer(item.m_Area)) && m_UpdateSet.Add(item.m_Area))
				{
					m_UpdateList.Add(in item.m_Area);
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_ExtractorAreaData;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public ComponentLookup<Extractor> m_ExtractorData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ExtractorParameterData m_ExtractorParameters;

		public CellMapData<NaturalResourceCell> m_NaturalResourceData;

		public NativeList<Entity> m_UpdateList;

		public void Execute()
		{
			NativeParallelHashSet<Entity> updateSet = new NativeParallelHashSet<Entity>(16, Allocator.Temp);
			AreaIterator iterator = new AreaIterator
			{
				m_ExtractorData = m_ExtractorData,
				m_MapFeatureElements = m_MapFeatureElements,
				m_UpdateSet = updateSet,
				m_UpdateList = m_UpdateList
			};
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<Game.Areas.Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_NodeType);
				BufferAccessor<Triangle> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_TriangleType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity value = nativeArray[j];
					PrefabRef prefabRef = nativeArray2[j];
					ExtractorAreaData extractorAreaData = m_ExtractorAreaData[prefabRef.m_Prefab];
					if (extractorAreaData.m_MapFeature == MapFeature.Forest)
					{
						if (updateSet.Add(value))
						{
							m_UpdateList.Add(in value);
						}
						continue;
					}
					Extractor extractor = m_ExtractorData[value];
					if (extractor.m_ExtractedAmount >= math.max(1f, (extractorAreaData.m_MapFeature == MapFeature.Ore || extractorAreaData.m_MapFeature == MapFeature.Oil) ? 1f : (extractor.m_ResourceAmount * 0.001f)))
					{
						switch (extractorAreaData.m_MapFeature)
						{
						case MapFeature.FertileLand:
						case MapFeature.Oil:
						case MapFeature.Ore:
						case MapFeature.Fish:
						{
							DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
							ExtractNaturalResources(ref random, ref iterator, bufferAccessor[j], bufferAccessor2[j], cityModifiers, ref extractor, extractorAreaData.m_MapFeature);
							break;
						}
						}
						m_ExtractorData[value] = extractor;
					}
				}
			}
			updateSet.Dispose();
		}

		private int GetUnlimitedUsage(float originalConcentration, float currentConcentration, float mu, ref Unity.Mathematics.Random random, int extractedAmount)
		{
			float num = math.log(originalConcentration) - math.log(currentConcentration);
			return MathUtils.RoundToIntRandom(ref random, mu * originalConcentration * math.exp(0f - num) * (float)extractedAmount * 10000f);
		}

		private void ExtractNaturalResources(ref Unity.Mathematics.Random random, ref AreaIterator iterator, DynamicBuffer<Game.Areas.Node> nodes, DynamicBuffer<Triangle> triangles, DynamicBuffer<CityModifier> cityModifiers, ref Extractor extractor, MapFeature mapFeature)
		{
			float4 xyxy = (1f / m_NaturalResourceData.m_CellSize).xyxy;
			float4 xyxy2 = ((float2)m_NaturalResourceData.m_TextureSize * 0.5f).xyxy;
			float num = 1f / (m_NaturalResourceData.m_CellSize.x * m_NaturalResourceData.m_CellSize.y);
			int num2 = Mathf.FloorToInt(extractor.m_ExtractedAmount);
			Bounds2 bounds4 = default(Bounds2);
			do
			{
				int index = -1;
				int num3 = 0;
				float num4 = 0f;
				bool flag = mapFeature == MapFeature.Ore || mapFeature == MapFeature.Oil;
				Bounds2 bounds = default(Bounds2);
				for (int i = 0; i < triangles.Length; i++)
				{
					Triangle2 triangle = AreaUtils.GetTriangle2(nodes, triangles[i]);
					Bounds2 bounds2 = MathUtils.Bounds(triangle);
					int4 valueToClamp = (int4)math.floor(new float4(bounds2.min, bounds2.max) * xyxy + xyxy2);
					valueToClamp = math.clamp(valueToClamp, 0, m_NaturalResourceData.m_TextureSize.xyxy - 1);
					float num5 = 0f;
					float num6 = 0f;
					Bounds2 bounds3 = default(Bounds2);
					int num7 = 0;
					float num8 = 0f;
					for (int j = valueToClamp.y; j <= valueToClamp.w; j++)
					{
						bounds4.min.y = ((float)j - xyxy2.y) * m_NaturalResourceData.m_CellSize.y;
						bounds4.max.y = bounds4.min.y + m_NaturalResourceData.m_CellSize.y;
						for (int k = valueToClamp.x; k <= valueToClamp.z; k++)
						{
							int num9 = k + m_NaturalResourceData.m_TextureSize.x * j;
							NaturalResourceCell naturalResourceCell = m_NaturalResourceData.m_Buffer[num9];
							float valueToClamp2;
							switch (mapFeature)
							{
							case MapFeature.FertileLand:
								valueToClamp2 = (int)naturalResourceCell.m_Fertility.m_Base;
								valueToClamp2 -= (float)(int)naturalResourceCell.m_Fertility.m_Used;
								break;
							case MapFeature.Ore:
								valueToClamp2 = (int)naturalResourceCell.m_Ore.m_Base;
								CityUtils.ApplyModifier(ref valueToClamp2, cityModifiers, CityModifierType.OreResourceAmount);
								valueToClamp2 -= (float)(int)naturalResourceCell.m_Ore.m_Used;
								break;
							case MapFeature.Oil:
								valueToClamp2 = (int)naturalResourceCell.m_Oil.m_Base;
								CityUtils.ApplyModifier(ref valueToClamp2, cityModifiers, CityModifierType.OilResourceAmount);
								valueToClamp2 -= (float)(int)naturalResourceCell.m_Oil.m_Used;
								break;
							case MapFeature.Fish:
								valueToClamp2 = (int)naturalResourceCell.m_Fish.m_Base;
								valueToClamp2 -= (float)(int)naturalResourceCell.m_Fish.m_Used;
								break;
							default:
								valueToClamp2 = 0f;
								break;
							}
							valueToClamp2 = math.clamp(valueToClamp2, 0f, 65535f);
							if (valueToClamp2 == 0f)
							{
								continue;
							}
							bounds4.min.x = ((float)k - xyxy2.x) * m_NaturalResourceData.m_CellSize.x;
							bounds4.max.x = bounds4.min.x + m_NaturalResourceData.m_CellSize.x;
							if (MathUtils.Intersect(bounds4, triangle, out var area))
							{
								num5 += area * random.NextFloat(0.99f, 1.01f) * math.min(valueToClamp2 * 0.0001f, 1f);
								num6 += area;
								if (valueToClamp2 * area * num > num8)
								{
									num8 = valueToClamp2 * area * num;
									num7 = num9;
									bounds3 = bounds4;
								}
							}
						}
					}
					num5 = ((num6 > 0.01f) ? (num5 / num6) : 0f);
					if (num5 > num4)
					{
						index = num7;
						num3 = (flag ? num2 : math.min(Mathf.RoundToInt(num8), num2));
						num4 = num5;
						bounds = bounds3;
					}
				}
				if (num3 > 0)
				{
					NaturalResourceCell value = m_NaturalResourceData.m_Buffer[index];
					switch (mapFeature)
					{
					case MapFeature.FertileLand:
						value.m_Fertility.m_Used = (ushort)math.min(65535, value.m_Fertility.m_Used + num3);
						break;
					case MapFeature.Ore:
					{
						float originalConcentration2 = (float)(int)value.m_Ore.m_Base * 0.0001f;
						float currentConcentration2 = (float)(value.m_Ore.m_Base - value.m_Ore.m_Used) * 0.0001f;
						int unlimitedUsage2 = GetUnlimitedUsage(originalConcentration2, currentConcentration2, 1f / m_ExtractorParameters.m_OreConsumption, ref random, num3);
						value.m_Ore.m_Used = (ushort)math.min(65535, value.m_Ore.m_Used + unlimitedUsage2);
						break;
					}
					case MapFeature.Oil:
					{
						float originalConcentration = (float)(int)value.m_Oil.m_Base * 0.0001f;
						float currentConcentration = (float)(value.m_Oil.m_Base - value.m_Oil.m_Used) * 0.0001f;
						int unlimitedUsage = GetUnlimitedUsage(originalConcentration, currentConcentration, 1f / m_ExtractorParameters.m_OilConsumption, ref random, num3);
						value.m_Oil.m_Used = (ushort)math.min(65535, value.m_Oil.m_Used + unlimitedUsage);
						break;
					}
					case MapFeature.Fish:
						value.m_Fish.m_Used = (ushort)math.min(65535, value.m_Fish.m_Used + num3);
						break;
					}
					m_NaturalResourceData.m_Buffer[index] = value;
					extractor.m_ExtractedAmount -= num3;
					iterator.m_Bounds = bounds;
					m_AreaTree.Iterate(ref iterator);
					num2 = Mathf.FloorToInt(extractor.m_ExtractedAmount);
					continue;
				}
				break;
			}
			while (num2 > 0);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<WoodResource> __Game_Areas_WoodResource_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Extractor> __Game_Areas_Extractor_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Storage> __Game_Areas_Storage_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.CargoTransportStation> __Game_Buildings_CargoTransportStation_RW_ComponentTypeHandle;

		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkRoute> __Game_Routes_WorkRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Route> __Game_Routes_Route_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LotData> __Game_Prefabs_LotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> __Game_Prefabs_WorkVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> __Game_Prefabs_NavigationAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubRoute> __Game_Routes_SubRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<VehicleModel> __Game_Routes_VehicleModel_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.WorkVehicle> __Game_Vehicles_WorkVehicle_RW_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		public ComponentLookup<Extractor> __Game_Areas_Extractor_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public BufferLookup<WoodResource> __Game_Areas_WoodResource_RW_BufferLookup;

		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Areas_WoodResource_RO_BufferTypeHandle = state.GetBufferTypeHandle<WoodResource>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Areas_Extractor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Extractor>();
			__Game_Areas_Storage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Storage>();
			__Game_Buildings_CargoTransportStation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.CargoTransportStation>();
			__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_WorkRoute_RO_ComponentLookup = state.GetComponentLookup<WorkRoute>(isReadOnly: true);
			__Game_Routes_Route_RO_ComponentLookup = state.GetComponentLookup<Route>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			__Game_Prefabs_LotData_RO_ComponentLookup = state.GetComponentLookup<LotData>(isReadOnly: true);
			__Game_Prefabs_WorkVehicleData_RO_ComponentLookup = state.GetComponentLookup<WorkVehicleData>(isReadOnly: true);
			__Game_Prefabs_NavigationAreaData_RO_ComponentLookup = state.GetComponentLookup<NavigationAreaData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportStationData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportStationData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Routes_SubRoute_RO_BufferLookup = state.GetBufferLookup<SubRoute>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
			__Game_Routes_VehicleModel_RO_BufferLookup = state.GetBufferLookup<VehicleModel>(isReadOnly: true);
			__Game_Vehicles_WorkVehicle_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.WorkVehicle>();
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Areas_MapFeatureElement_RO_BufferLookup = state.GetBufferLookup<MapFeatureElement>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Areas_Extractor_RW_ComponentLookup = state.GetComponentLookup<Extractor>();
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Areas_WoodResource_RW_BufferLookup = state.GetBufferLookup<WoodResource>();
			__Game_Areas_MapFeatureElement_RW_BufferLookup = state.GetBufferLookup<MapFeatureElement>();
		}
	}

	private const uint UPDATE_INTERVAL = 512u;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private CitySystem m_CitySystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_Watersystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_AreaQuery;

	private EntityQuery m_ExtractorQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EntityQuery m_ExtractorParameterQuery;

	private WorkVehicleSelectData m_WorkVehicleSelectData;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_220632541_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_Watersystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_WorkVehicleSelectData = new WorkVehicleSelectData(this);
		m_AreaQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadWrite<Extractor>(),
				ComponentType.ReadWrite<Storage>(),
				ComponentType.ReadWrite<Game.Buildings.CargoTransportStation>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ExtractorQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Areas.Lot>(),
				ComponentType.ReadWrite<Extractor>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_VehiclePrefabQuery = GetEntityQuery(WorkVehicleSelectData.GetEntityQueryDesc());
		m_ExtractorParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ExtractorParameterData>());
		RequireForUpdate(m_AreaQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_WorkVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new ManageVehiclesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WoodResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_WoodResource_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ExtractorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StorageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Storage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CargoTransportStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CargoTransportStation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WorkRoute_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Route_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStorageAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNavigationAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NavigationAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCargoTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStorageCompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_VehicleLayouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_SubRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_VehicleModels = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_VehicleModel_RO_BufferLookup, ref base.CheckedStateRef),
			m_WorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WorkVehicle_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_WorkVehicleSelectData = m_WorkVehicleSelectData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 512).AsParallelWriter()
		}, m_AreaQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		m_WorkVehicleSelectData.PostUpdate(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle2);
		base.Dependency = jobHandle2;
		if (!m_ExtractorQuery.IsEmptyIgnoreFilter)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			JobHandle dependencies;
			CellMapData<NaturalResourceCell> data = m_NaturalResourceSystem.GetData(readOnly: false, out dependencies);
			JobHandle outJobHandle;
			JobHandle dependencies2;
			ExtractResourcesJob jobData = new ExtractResourcesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Chunks = m_ExtractorQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies2),
				m_City = m_CitySystem.City,
				m_RandomSeed = RandomSeed.Next(),
				m_ExtractorParameters = m_ExtractorParameterQuery.GetSingleton<ExtractorParameterData>(),
				m_NaturalResourceData = data,
				m_UpdateList = nativeList
			};
			JobHandle dependencies3;
			JobHandle dependencies4;
			JobHandle deps;
			AreaResourceSystem.UpdateAreaResourcesJob jobData2 = new AreaResourceSystem.UpdateAreaResourcesJob
			{
				m_City = m_CitySystem.City,
				m_FullUpdate = false,
				m_UpdateList = nativeList.AsDeferredJobArray(),
				m_ObjectTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
				m_NaturalResourceData = data,
				m_GroundWaterResourceData = m_GroundWaterSystem.GetData(readOnly: true, out dependencies4),
				m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
				m_WoodResources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_WoodResource_RW_BufferLookup, ref base.CheckedStateRef),
				m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RW_BufferLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_Watersystem.GetSurfaceData(out deps),
				m_BuildableLandMaxSlope = __query_220632541_0.GetSingleton<AreasConfigurationData>().m_BuildableLandMaxSlope
			};
			JobHandle jobHandle3 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(outJobHandle, dependencies2, dependencies)));
			JobHandle jobHandle4 = jobData2.Schedule(nativeList, 1, JobUtils.CombineDependencies(jobHandle3, dependencies3, deps, dependencies4));
			nativeList.Dispose(jobHandle4);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle3);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle4);
			m_NaturalResourceSystem.AddWriter(jobHandle4);
			m_TerrainSystem.AddCPUHeightReader(jobHandle4);
			m_Watersystem.AddSurfaceReader(jobHandle4);
			m_GroundWaterSystem.AddReader(jobHandle4);
			base.Dependency = jobHandle4;
		}
	}

	public static int GetUnlimitedTotalAmount(int used, int originalAmount, float mu)
	{
		return Mathf.RoundToInt(math.log(originalAmount / 10000) - math.log((originalAmount - used) / 10000) / mu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<AreasConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_220632541_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AreaLotSimulationSystem()
	{
	}
}
