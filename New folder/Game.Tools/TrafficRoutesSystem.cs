using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class TrafficRoutesSystem : GameSystemBase
{
	private struct LivePathEntityData
	{
		public Entity m_Entity;

		public int m_SegmentCount;

		public bool m_HasNewSegments;
	}

	[BurstCompile]
	private struct FillTargetMapJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<AggregateElement> m_AggregateElements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRoutes;

		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public int m_SelectedIndex;

		public NativeHashSet<Entity> m_TargetMap;

		public void Execute()
		{
			m_TargetMap.Add(m_SelectedEntity);
			AddSubLanes(m_SelectedEntity);
			AddSubNets(m_SelectedEntity);
			AddSubAreas(m_SelectedEntity);
			AddSubObjects(m_SelectedEntity);
			if (m_SpawnLocations.TryGetBuffer(m_SelectedEntity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					SpawnLocationElement spawnLocationElement = bufferData[i];
					m_TargetMap.Add(spawnLocationElement.m_SpawnLocation);
				}
				if (m_AttachedData.TryGetComponent(m_SelectedEntity, out var componentData))
				{
					AddSubLanes(componentData.m_Parent);
					AddSubNets(componentData.m_Parent);
					AddSubAreas(componentData.m_Parent);
					AddSubObjects(componentData.m_Parent);
				}
			}
			if (m_Renters.TryGetBuffer(m_SelectedEntity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Renter renter = bufferData2[j];
					m_TargetMap.Add(renter.m_Renter);
				}
			}
			if (m_AggregateElements.TryGetBuffer(m_SelectedEntity, out var bufferData3))
			{
				if (m_SelectedIndex >= 0 && m_SelectedIndex < bufferData3.Length)
				{
					AddSubLanes(bufferData3[m_SelectedIndex].m_Edge);
				}
				else
				{
					for (int k = 0; k < bufferData3.Length; k++)
					{
						AddSubLanes(bufferData3[k].m_Edge);
					}
				}
			}
			if (m_ConnectedRoutes.TryGetBuffer(m_SelectedEntity, out var bufferData4))
			{
				for (int l = 0; l < bufferData4.Length; l++)
				{
					ConnectedRoute connectedRoute = bufferData4[l];
					m_TargetMap.Add(connectedRoute.m_Waypoint);
				}
			}
			if (m_OutsideConnectionData.HasComponent(m_SelectedEntity) && m_OwnerData.TryGetComponent(m_SelectedEntity, out var componentData2))
			{
				AddSubLanes(componentData2.m_Owner);
			}
		}

		private void AddSubObjects(Entity entity)
		{
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Game.Objects.SubObject subObject = bufferData[i];
					AddSubLanes(subObject.m_SubObject);
					AddSubNets(subObject.m_SubObject);
					AddSubAreas(subObject.m_SubObject);
					AddSubObjects(subObject.m_SubObject);
				}
			}
		}

		private void AddSubNets(Entity entity)
		{
			if (m_SubNets.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					AddSubLanes(bufferData[i].m_SubNet);
				}
			}
		}

		private void AddSubAreas(Entity entity)
		{
			if (m_SubAreas.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Game.Areas.SubArea subArea = bufferData[i];
					AddSubLanes(subArea.m_Area);
					AddSubAreas(subArea.m_Area);
				}
			}
		}

		private void AddSubLanes(Entity entity)
		{
			if (!m_SubLanes.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				if (subLane.m_PathMethods != 0)
				{
					m_TargetMap.Add(subLane.m_SubLane);
				}
			}
		}
	}

	[BurstCompile]
	private struct FindPathSourcesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> m_AircraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> m_TrainCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

		[ReadOnly]
		public BufferTypeHandle<WatercraftNavigationLane> m_WatercraftNavigationLaneType;

		[ReadOnly]
		public BufferTypeHandle<AircraftNavigationLane> m_AircraftNavigationLaneType;

		[ReadOnly]
		public BufferTypeHandle<TrainNavigationLane> m_TrainNavigationLaneType;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public NativeHashSet<Entity> m_TargetMap;

		public NativeQueue<Entity>.ParallelWriter m_PathSourceQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Target> nativeArray2 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray3 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<CurrentVehicle> nativeArray4 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			bool flag = nativeArray4.Length != 0 && !chunk.Has(ref m_TransformFrames);
			NativeArray<HumanCurrentLane> nativeArray5 = default(NativeArray<HumanCurrentLane>);
			NativeArray<CarCurrentLane> buffer = default(NativeArray<CarCurrentLane>);
			NativeArray<WatercraftCurrentLane> buffer2 = default(NativeArray<WatercraftCurrentLane>);
			NativeArray<AircraftCurrentLane> buffer3 = default(NativeArray<AircraftCurrentLane>);
			NativeArray<TrainCurrentLane> buffer4 = default(NativeArray<TrainCurrentLane>);
			NativeArray<Controller> buffer5 = default(NativeArray<Controller>);
			BufferAccessor<CarNavigationLane> bufferAccessor2 = default(BufferAccessor<CarNavigationLane>);
			BufferAccessor<WatercraftNavigationLane> bufferAccessor3 = default(BufferAccessor<WatercraftNavigationLane>);
			BufferAccessor<AircraftNavigationLane> bufferAccessor4 = default(BufferAccessor<AircraftNavigationLane>);
			BufferAccessor<TrainNavigationLane> bufferAccessor5 = default(BufferAccessor<TrainNavigationLane>);
			nativeArray5 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			if (nativeArray5.Length == 0 && nativeArray4.Length == 0)
			{
				buffer = chunk.GetNativeArray(ref m_CarCurrentLaneType);
				buffer5 = chunk.GetNativeArray(ref m_ControllerType);
				if (buffer.Length != 0)
				{
					bufferAccessor2 = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
				}
				else
				{
					buffer2 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
					if (buffer2.Length != 0)
					{
						bufferAccessor3 = chunk.GetBufferAccessor(ref m_WatercraftNavigationLaneType);
					}
					else
					{
						buffer3 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
						if (buffer3.Length != 0)
						{
							bufferAccessor4 = chunk.GetBufferAccessor(ref m_AircraftNavigationLaneType);
						}
						else
						{
							buffer4 = chunk.GetNativeArray(ref m_TrainCurrentLaneType);
							if (buffer4.Length != 0)
							{
								bufferAccessor5 = chunk.GetBufferAccessor(ref m_TrainNavigationLaneType);
							}
						}
					}
				}
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (CollectionUtils.TryGet(nativeArray3, i, out var value) && CollectionUtils.TryGet(bufferAccessor, i, out var value2))
				{
					int num = value.m_ElementIndex;
					while (num < value2.Length)
					{
						PathElement pathElement = value2[num];
						if ((pathElement.m_Flags & PathElementFlags.Action) != 0 || !m_TargetMap.Contains(pathElement.m_Target))
						{
							num++;
							continue;
						}
						goto IL_040f;
					}
				}
				if (!CollectionUtils.TryGet(nativeArray2, i, out var value3) || !m_TargetMap.Contains(value3.m_Target))
				{
					CarCurrentLane value5;
					WatercraftCurrentLane value7;
					AircraftCurrentLane value9;
					if (CollectionUtils.TryGet(nativeArray5, i, out var value4))
					{
						if (!m_TargetMap.Contains(value4.m_Lane))
						{
							continue;
						}
					}
					else if (CollectionUtils.TryGet(buffer, i, out value5))
					{
						if (!m_TargetMap.Contains(value5.m_Lane))
						{
							if (!CollectionUtils.TryGet(bufferAccessor2, i, out var value6))
							{
								continue;
							}
							int num2 = 0;
							while (num2 < value6.Length)
							{
								CarNavigationLane carNavigationLane = value6[num2];
								if (!m_TargetMap.Contains(carNavigationLane.m_Lane))
								{
									num2++;
									continue;
								}
								goto IL_040f;
							}
							continue;
						}
					}
					else if (CollectionUtils.TryGet(buffer2, i, out value7))
					{
						if (!m_TargetMap.Contains(value7.m_Lane))
						{
							if (!CollectionUtils.TryGet(bufferAccessor3, i, out var value8))
							{
								continue;
							}
							int num3 = 0;
							while (num3 < value8.Length)
							{
								WatercraftNavigationLane watercraftNavigationLane = value8[num3];
								if (!m_TargetMap.Contains(watercraftNavigationLane.m_Lane))
								{
									num3++;
									continue;
								}
								goto IL_040f;
							}
							continue;
						}
					}
					else if (CollectionUtils.TryGet(buffer3, i, out value9))
					{
						if (!m_TargetMap.Contains(value9.m_Lane))
						{
							if (!CollectionUtils.TryGet(bufferAccessor4, i, out var value10))
							{
								continue;
							}
							int num4 = 0;
							while (num4 < value10.Length)
							{
								AircraftNavigationLane aircraftNavigationLane = value10[num4];
								if (!m_TargetMap.Contains(aircraftNavigationLane.m_Lane))
								{
									num4++;
									continue;
								}
								goto IL_040f;
							}
							continue;
						}
					}
					else
					{
						if (!CollectionUtils.TryGet(buffer4, i, out var value11))
						{
							continue;
						}
						if (!m_TargetMap.Contains(value11.m_Front.m_Lane) && !m_TargetMap.Contains(value11.m_Rear.m_Lane))
						{
							if (!CollectionUtils.TryGet(bufferAccessor5, i, out var value12))
							{
								continue;
							}
							int num5 = 0;
							while (num5 < value12.Length)
							{
								TrainNavigationLane trainNavigationLane = value12[num5];
								if (!m_TargetMap.Contains(trainNavigationLane.m_Lane))
								{
									num5++;
									continue;
								}
								goto IL_040f;
							}
							continue;
						}
					}
				}
				goto IL_040f;
				IL_040f:
				if (flag)
				{
					CurrentVehicle currentVehicle = nativeArray4[i];
					if (!m_PublicTransportData.HasComponent(currentVehicle.m_Vehicle))
					{
						continue;
					}
				}
				if (CollectionUtils.TryGet(buffer5, i, out var value13) && value13.m_Controller != Entity.Null)
				{
					m_PathSourceQueue.Enqueue(value13.m_Controller);
				}
				else
				{
					m_PathSourceQueue.Enqueue(nativeArray[i]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLivePathsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_LivePathChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<PathSource> m_PathSourceData;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<Watercraft> m_WatercraftData;

		[ReadOnly]
		public ComponentLookup<Aircraft> m_AircraftData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public BufferLookup<Passenger> m_Passengers;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public int m_UpdateFrameIndex;

		[ReadOnly]
		public int m_SourceCountLimit;

		[ReadOnly]
		public RouteConfigurationData m_RouteConfigurationData;

		[NativeDisableContainerSafetyRestriction]
		public NativeQueue<Entity> m_PathSourceQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeHashMap<Entity, LivePathEntityData> livePathEntities = new NativeHashMap<Entity, LivePathEntityData>(10, Allocator.Temp);
			NativeHashMap<Entity, bool> pathSourceFound = new NativeHashMap<Entity, bool>(100, Allocator.Temp);
			for (int i = 0; i < m_LivePathChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_LivePathChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<RouteSegment> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RouteSegmentType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					DynamicBuffer<RouteSegment> dynamicBuffer = bufferAccessor[j];
					livePathEntities[nativeArray2[j].m_Prefab] = new LivePathEntityData
					{
						m_Entity = nativeArray[j],
						m_SegmentCount = dynamicBuffer.Length
					};
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity segment = dynamicBuffer[k].m_Segment;
						pathSourceFound[m_PathSourceData[segment].m_Entity] = false;
					}
				}
			}
			Entity entity = m_SelectedEntity;
			if (m_CurrentTransportData.TryGetComponent(entity, out var componentData))
			{
				entity = componentData.m_CurrentTransport;
			}
			if (m_ControllerData.TryGetComponent(entity, out var componentData2) && componentData2.m_Controller != Entity.Null)
			{
				entity = componentData2.m_Controller;
			}
			AddLivePath(entity, livePathEntities, pathSourceFound);
			if (m_CurrentVehicleData.TryGetComponent(entity, out var componentData3))
			{
				if (m_ControllerData.TryGetComponent(componentData3.m_Vehicle, out componentData2) && componentData2.m_Controller != Entity.Null)
				{
					AddLivePath(componentData2.m_Controller, livePathEntities, pathSourceFound);
				}
				else
				{
					AddLivePath(componentData3.m_Vehicle, livePathEntities, pathSourceFound);
				}
			}
			DynamicBuffer<Passenger> bufferData3;
			if (m_LayoutElements.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
			{
				for (int l = 0; l < bufferData.Length; l++)
				{
					if (m_Passengers.TryGetBuffer(bufferData[l].m_Vehicle, out var bufferData2))
					{
						for (int m = 0; m < bufferData2.Length; m++)
						{
							AddLivePath(bufferData2[m].m_Passenger, livePathEntities, pathSourceFound);
						}
					}
				}
			}
			else if (m_Passengers.TryGetBuffer(entity, out bufferData3))
			{
				for (int n = 0; n < bufferData3.Length; n++)
				{
					AddLivePath(bufferData3[n].m_Passenger, livePathEntities, pathSourceFound);
				}
			}
			if (m_HouseholdCitizens.TryGetBuffer(entity, out var bufferData4))
			{
				for (int num = 0; num < bufferData4.Length; num++)
				{
					entity = bufferData4[num].m_Citizen;
					if (m_CurrentTransportData.TryGetComponent(entity, out componentData))
					{
						entity = componentData.m_CurrentTransport;
					}
					AddLivePath(entity, livePathEntities, pathSourceFound);
					if (m_CurrentVehicleData.TryGetComponent(entity, out componentData3))
					{
						if (m_ControllerData.TryGetComponent(componentData3.m_Vehicle, out componentData2) && componentData2.m_Controller != Entity.Null)
						{
							AddLivePath(componentData2.m_Controller, livePathEntities, pathSourceFound);
						}
						else
						{
							AddLivePath(componentData3.m_Vehicle, livePathEntities, pathSourceFound);
						}
					}
				}
			}
			if (m_PathSourceQueue.IsCreated)
			{
				Entity item;
				while (m_PathSourceQueue.TryDequeue(out item))
				{
					AddLivePath(item, livePathEntities, pathSourceFound);
				}
			}
			for (int num2 = 0; num2 < m_LivePathChunks.Length; num2++)
			{
				ArchetypeChunk archetypeChunk2 = m_LivePathChunks[num2];
				NativeArray<Entity> nativeArray3 = archetypeChunk2.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk2.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<RouteSegment> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_RouteSegmentType);
				for (int num3 = 0; num3 < bufferAccessor2.Length; num3++)
				{
					Entity e = nativeArray3[num3];
					DynamicBuffer<RouteSegment> dynamicBuffer2 = bufferAccessor2[num3];
					int num4 = 0;
					for (int num5 = 0; num5 < dynamicBuffer2.Length; num5++)
					{
						RouteSegment value = dynamicBuffer2[num5];
						PathSource pathSource = m_PathSourceData[value.m_Segment];
						if (!pathSourceFound[pathSource.m_Entity] && GetUpdateFrameIndex(pathSource.m_Entity) == m_UpdateFrameIndex)
						{
							m_CommandBuffer.AddComponent<Deleted>(value.m_Segment);
						}
						else
						{
							dynamicBuffer2[num4++] = value;
						}
					}
					bool flag = livePathEntities[nativeArray4[num3].m_Prefab].m_HasNewSegments;
					if (num4 < dynamicBuffer2.Length)
					{
						dynamicBuffer2.RemoveRange(num4, dynamicBuffer2.Length - num4);
						if (num4 == 0 && !flag)
						{
							m_CommandBuffer.AddComponent<Deleted>(e);
						}
					}
					if (flag)
					{
						m_CommandBuffer.AddComponent<Updated>(e);
					}
				}
			}
			livePathEntities.Dispose();
			pathSourceFound.Dispose();
		}

		private int GetUpdateFrameIndex(Entity sourceEntity)
		{
			if (m_UpdateFrameIndex == -1)
			{
				return m_UpdateFrameIndex;
			}
			if (!m_EntityLookup.Exists(sourceEntity))
			{
				return m_UpdateFrameIndex;
			}
			EntityStorageInfo entityStorageInfo = m_EntityLookup[sourceEntity];
			if (!entityStorageInfo.Chunk.Has(m_UpdateFrameType))
			{
				return m_UpdateFrameIndex;
			}
			return (int)entityStorageInfo.Chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
		}

		private void AddLivePath(Entity sourceEntity, NativeHashMap<Entity, LivePathEntityData> livePathEntities, NativeHashMap<Entity, bool> pathSourceFound)
		{
			if (!m_PathElements.HasBuffer(sourceEntity))
			{
				return;
			}
			if (pathSourceFound.TryGetValue(sourceEntity, out var item))
			{
				if (!item)
				{
					pathSourceFound[sourceEntity] = true;
				}
				return;
			}
			Entity entity = (m_HumanData.HasComponent(sourceEntity) ? m_RouteConfigurationData.m_HumanPathVisualization : (m_WatercraftData.HasComponent(sourceEntity) ? m_RouteConfigurationData.m_WatercraftPathVisualization : (m_AircraftData.HasComponent(sourceEntity) ? m_RouteConfigurationData.m_AircraftPathVisualization : (m_TrainData.HasComponent(sourceEntity) ? m_RouteConfigurationData.m_TrainPathVisualization : ((!m_BicycleData.HasComponent(sourceEntity)) ? m_RouteConfigurationData.m_CarPathVisualization : m_RouteConfigurationData.m_BicyclePathVisualization)))));
			RouteData routeData = m_PrefabRouteData[entity];
			if (!livePathEntities.TryGetValue(entity, out var item2))
			{
				item2.m_Entity = m_CommandBuffer.CreateEntity(routeData.m_RouteArchetype);
				item2.m_SegmentCount = 1;
				item2.m_HasNewSegments = true;
				m_CommandBuffer.SetComponent(item2.m_Entity, new PrefabRef(entity));
				m_CommandBuffer.SetComponent(item2.m_Entity, new Game.Routes.Color(routeData.m_Color));
				livePathEntities[entity] = item2;
			}
			else
			{
				if (item2.m_SegmentCount++ >= m_SourceCountLimit)
				{
					return;
				}
				item2.m_HasNewSegments = true;
				livePathEntities[entity] = item2;
			}
			Entity entity2 = m_CommandBuffer.CreateEntity(routeData.m_SegmentArchetype);
			m_CommandBuffer.SetComponent(entity2, new PrefabRef(entity));
			m_CommandBuffer.SetComponent(entity2, new Owner(item2.m_Entity));
			m_CommandBuffer.SetComponent(entity2, new PathSource
			{
				m_Entity = sourceEntity
			});
			m_CommandBuffer.AppendToBuffer(item2.m_Entity, new RouteSegment(entity2));
			pathSourceFound.Add(sourceEntity, item: true);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AggregateElement> __Game_Net_AggregateElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public BufferTypeHandle<RouteSegment> __Game_Routes_RouteSegment_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<PathSource> __Game_Routes_PathSource_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Net_AggregateElement_RO_BufferLookup = state.GetBufferLookup<AggregateElement>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>(isReadOnly: true);
			__Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<WatercraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<TrainNavigationLane>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Routes_RouteSegment_RW_BufferTypeHandle = state.GetBufferTypeHandle<RouteSegment>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Routes_PathSource_RO_ComponentLookup = state.GetComponentLookup<PathSource>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentLookup = state.GetComponentLookup<Watercraft>(isReadOnly: true);
			__Game_Vehicles_Aircraft_RO_ComponentLookup = state.GetComponentLookup<Aircraft>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_LivePathQuery;

	private EntityQuery m_PathSourceQuery;

	private EntityQuery m_RouteConfigQuery;

	private int m_UpdateFrameIndex;

	private TypeHandle __TypeHandle;

	public bool routesVisible { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_LivePathQuery = GetEntityQuery(ComponentType.ReadOnly<LivePath>(), ComponentType.ReadOnly<Route>(), ComponentType.Exclude<Deleted>());
		m_PathSourceQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<UpdateFrame>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<PathOwner>(),
				ComponentType.ReadOnly<TrainCurrentLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RouteConfigQuery = GetEntityQuery(ComponentType.ReadOnly<RouteConfigurationData>());
		m_UpdateFrameIndex = -1;
		routesVisible = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Entity entity = (routesVisible ? m_ToolSystem.selected : Entity.Null);
		if (entity == Entity.Null && m_LivePathQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> livePathChunks = m_LivePathQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		NativeQueue<Entity> pathSourceQueue = default(NativeQueue<Entity>);
		JobHandle jobHandle = base.Dependency;
		if (base.EntityManager.HasComponent<Building>(entity) || base.EntityManager.HasComponent<Aggregate>(entity) || base.EntityManager.HasComponent<Game.Net.Node>(entity) || base.EntityManager.HasComponent<Game.Net.Edge>(entity) || base.EntityManager.HasComponent<Game.Routes.TransportStop>(entity) || base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(entity))
		{
			NativeHashSet<Entity> targetMap = new NativeHashSet<Entity>(100, Allocator.TempJob);
			pathSourceQueue = new NativeQueue<Entity>(Allocator.TempJob);
			if (++m_UpdateFrameIndex == 16)
			{
				m_UpdateFrameIndex = 0;
			}
			m_PathSourceQuery.ResetFilter();
			m_PathSourceQuery.AddSharedComponentFilter(new UpdateFrame((uint)m_UpdateFrameIndex));
			FillTargetMapJob jobData = new FillTargetMapJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_AggregateElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_AggregateElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
				m_SelectedEntity = entity,
				m_SelectedIndex = m_ToolSystem.selectedIndex,
				m_TargetMap = targetMap
			};
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new FindPathSourcesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrainCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TransformFrames = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_WatercraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TrainNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetMap = targetMap,
				m_PathSourceQueue = pathSourceQueue.AsParallelWriter()
			}, dependsOn: IJobExtensions.Schedule(jobData, jobHandle), query: m_PathSourceQuery);
			targetMap.Dispose(jobHandle2);
			jobHandle = jobHandle2;
		}
		else
		{
			m_UpdateFrameIndex = -1;
		}
		JobHandle jobHandle3 = IJobExtensions.Schedule(new UpdateLivePathsJob
		{
			m_LivePathChunks = livePathChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteSegmentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteSegment_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathSource_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_SelectedEntity = entity,
			m_UpdateFrameIndex = m_UpdateFrameIndex,
			m_SourceCountLimit = 200,
			m_RouteConfigurationData = m_RouteConfigQuery.GetSingleton<RouteConfigurationData>(),
			m_PathSourceQueue = pathSourceQueue,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(outJobHandle, jobHandle));
		livePathChunks.Dispose(jobHandle3);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
		if (pathSourceQueue.IsCreated)
		{
			pathSourceQueue.Dispose(jobHandle3);
		}
		base.Dependency = jobHandle3;
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
	public TrafficRoutesSystem()
	{
	}
}
