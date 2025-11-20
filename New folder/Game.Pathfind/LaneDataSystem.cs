using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class LaneDataSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateLaneDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LaneObject> m_LaneObjectType;

		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		public ComponentTypeHandle<Game.Net.PedestrianLane> m_PedestrianLaneType;

		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Gate> m_GateData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidenteData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public Entity m_City;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Lane> nativeArray = chunk.GetNativeArray(ref m_LaneType);
			NativeArray<Game.Net.CarLane> nativeArray2 = chunk.GetNativeArray(ref m_CarLaneType);
			NativeArray<Game.Net.PedestrianLane> nativeArray3 = chunk.GetNativeArray(ref m_PedestrianLaneType);
			NativeArray<Game.Net.TrackLane> nativeArray4 = chunk.GetNativeArray(ref m_TrackLaneType);
			NativeArray<Game.Net.ConnectionLane> nativeArray5 = chunk.GetNativeArray(ref m_ConnectionLaneType);
			NativeArray<EdgeLane> nativeArray6 = chunk.GetNativeArray(ref m_EdgeLaneType);
			NativeArray<Owner> nativeArray7 = chunk.GetNativeArray(ref m_OwnerType);
			bool isEdgeLane = nativeArray6.Length != 0;
			bool allowExit;
			if (nativeArray2.Length != 0)
			{
				NativeArray<MasterLane> nativeArray8 = chunk.GetNativeArray(ref m_MasterLaneType);
				NativeArray<SlaveLane> nativeArray9 = chunk.GetNativeArray(ref m_SlaveLaneType);
				NativeArray<PrefabRef> nativeArray10 = chunk.GetNativeArray(ref m_PrefabRefType);
				Game.City.City city = default(Game.City.City);
				if (m_City != Entity.Null)
				{
					city = m_CityData[m_City];
				}
				if (nativeArray8.Length != 0)
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Game.Net.CarLane carLane = nativeArray2[i];
						carLane.m_AccessRestriction = Entity.Null;
						carLane.m_Flags &= ~(Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.AllowEnter);
						carLane.m_SpeedLimit = carLane.m_DefaultSpeedLimit;
						carLane.m_BlockageStart = byte.MaxValue;
						carLane.m_BlockageEnd = 0;
						carLane.m_CautionStart = byte.MaxValue;
						carLane.m_CautionEnd = 0;
						if (nativeArray7.Length != 0)
						{
							MasterLane masterLane = nativeArray8[i];
							Owner owner = nativeArray7[i];
							PrefabRef prefabRef = nativeArray10[i];
							DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
							CollectionUtils.TryGet(nativeArray6, i, out var value);
							Bounds1 bounds = new Bounds1(1f, 0f);
							ushort minIndex = masterLane.m_MinIndex;
							int num = math.min(masterLane.m_MaxIndex, dynamicBuffer.Length - 1);
							bool flag = true;
							bool isSideConnection = (carLane.m_Flags & Game.Net.CarLaneFlags.SideConnection) != 0;
							bool isRoundabout = (carLane.m_Flags & Game.Net.CarLaneFlags.Roundabout) != 0;
							for (int j = minIndex; j <= num; j++)
							{
								Entity subLane = dynamicBuffer[j].m_SubLane;
								if (!m_LaneObjects.HasBuffer(subLane))
								{
									continue;
								}
								DynamicBuffer<LaneObject> laneObjects = m_LaneObjects[subLane];
								if (flag)
								{
									bounds = CheckBlockage(laneObjects, out allowExit, out var isSecured);
									flag = false;
									if (isSecured)
									{
										carLane.m_Flags |= Game.Net.CarLaneFlags.IsSecured;
									}
								}
								else
								{
									bounds &= CheckBlockage(laneObjects, out allowExit, out var isSecured2);
									if (isSecured2)
									{
										carLane.m_Flags |= Game.Net.CarLaneFlags.IsSecured;
									}
								}
							}
							CarLaneData carLaneData = m_PrefabCarLaneData[prefabRef.m_Prefab];
							Game.Prefabs.BuildingFlags flag2 = (((carLaneData.m_RoadTypes & (RoadTypes.Car | RoadTypes.Helicopter | RoadTypes.Airplane | RoadTypes.Bicycle)) != RoadTypes.None) ? Game.Prefabs.BuildingFlags.RestrictedCar : ((Game.Prefabs.BuildingFlags)0u));
							carLane.m_AccessRestriction = GetAccessRestriction(owner, flag2, isEdgeLane, isSideConnection, noRestriction: false, isRoundabout, nativeArray[i], out var allowEnter, out allowExit);
							if (allowEnter)
							{
								carLane.m_Flags |= Game.Net.CarLaneFlags.AllowEnter;
							}
							AddOptionData(ref carLane, city);
							AddOptionData(ref carLane, owner, value, masterLane, carLaneData);
							AddBlockageData(ref carLane, bounds, addCaution: false);
						}
						nativeArray2[i] = carLane;
					}
				}
				else
				{
					BufferAccessor<LaneObject> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneObjectType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Game.Net.CarLane carLane2 = nativeArray2[k];
						carLane2.m_AccessRestriction = Entity.Null;
						carLane2.m_Flags &= ~(Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.AllowEnter);
						carLane2.m_SpeedLimit = carLane2.m_DefaultSpeedLimit;
						carLane2.m_BlockageStart = byte.MaxValue;
						carLane2.m_BlockageEnd = 0;
						carLane2.m_CautionStart = byte.MaxValue;
						carLane2.m_CautionEnd = 0;
						AddOptionData(ref carLane2, city);
						if (nativeArray7.Length != 0)
						{
							Owner owner2 = nativeArray7[k];
							PrefabRef prefabRef2 = nativeArray10[k];
							CollectionUtils.TryGet(nativeArray6, k, out var value2);
							CarLaneData carLaneData2 = m_PrefabCarLaneData[prefabRef2.m_Prefab];
							Game.Prefabs.BuildingFlags flag3 = (((carLaneData2.m_RoadTypes & (RoadTypes.Car | RoadTypes.Helicopter | RoadTypes.Airplane | RoadTypes.Bicycle)) != RoadTypes.None) ? Game.Prefabs.BuildingFlags.RestrictedCar : ((Game.Prefabs.BuildingFlags)0u));
							bool isSideConnection2 = (carLane2.m_Flags & Game.Net.CarLaneFlags.SideConnection) != 0;
							bool isRoundabout2 = (carLane2.m_Flags & Game.Net.CarLaneFlags.Roundabout) != 0;
							carLane2.m_AccessRestriction = GetAccessRestriction(owner2, flag3, isEdgeLane, isSideConnection2, noRestriction: false, isRoundabout2, nativeArray[k], out var allowEnter2, out allowExit);
							if (allowEnter2)
							{
								carLane2.m_Flags |= Game.Net.CarLaneFlags.AllowEnter;
							}
							AddOptionData(ref carLane2, owner2, value2, default(MasterLane), carLaneData2);
						}
						if (bufferAccessor.Length != 0)
						{
							DynamicBuffer<LaneObject> laneObjects2 = bufferAccessor[k];
							bool isEmergency;
							bool isSecured3;
							Bounds1 bounds2 = CheckBlockage(laneObjects2, out isEmergency, out isSecured3);
							bool flag4 = isEmergency;
							if (bounds2.min <= bounds2.max && !flag4)
							{
								flag4 = nativeArray9.Length == 0 || (nativeArray9[k].m_Flags & (SlaveLaneFlags.StartingLane | SlaveLaneFlags.EndingLane)) != (SlaveLaneFlags.StartingLane | SlaveLaneFlags.EndingLane);
							}
							AddBlockageData(ref carLane2, bounds2, flag4);
							if (isSecured3)
							{
								carLane2.m_Flags |= Game.Net.CarLaneFlags.IsSecured;
							}
						}
						nativeArray2[k] = carLane2;
					}
				}
			}
			if (nativeArray3.Length != 0)
			{
				for (int l = 0; l < nativeArray3.Length; l++)
				{
					Game.Net.PedestrianLane value3 = nativeArray3[l];
					value3.m_AccessRestriction = Entity.Null;
					value3.m_Flags &= ~(PedestrianLaneFlags.AllowEnter | PedestrianLaneFlags.ForbidTransitTraffic | PedestrianLaneFlags.AllowExit);
					if (nativeArray7.Length != 0)
					{
						Owner owner3 = nativeArray7[l];
						if (m_BorderDistrictData.HasComponent(owner3.m_Owner))
						{
							BorderDistrict borderDistrict = m_BorderDistrictData[owner3.m_Owner];
							PedestrianLaneFlags pedestrianLaneFlags = (PedestrianLaneFlags)0;
							PedestrianLaneFlags pedestrianLaneFlags2 = (PedestrianLaneFlags)0;
							if (m_DistrictData.HasComponent(borderDistrict.m_Left))
							{
								if (AreaUtils.CheckOption(m_DistrictData[borderDistrict.m_Left], DistrictOption.ForbidTransitTraffic))
								{
									pedestrianLaneFlags |= PedestrianLaneFlags.ForbidTransitTraffic;
								}
								else
								{
									pedestrianLaneFlags2 |= PedestrianLaneFlags.ForbidTransitTraffic;
								}
							}
							if (m_DistrictData.HasComponent(borderDistrict.m_Right))
							{
								if (AreaUtils.CheckOption(m_DistrictData[borderDistrict.m_Right], DistrictOption.ForbidTransitTraffic))
								{
									pedestrianLaneFlags |= PedestrianLaneFlags.ForbidTransitTraffic;
								}
								else
								{
									pedestrianLaneFlags2 |= PedestrianLaneFlags.ForbidTransitTraffic;
								}
							}
							value3.m_Flags |= pedestrianLaneFlags & ~pedestrianLaneFlags2;
						}
						Game.Prefabs.BuildingFlags flag5 = (((value3.m_Flags & PedestrianLaneFlags.OnWater) == 0) ? Game.Prefabs.BuildingFlags.RestrictedPedestrian : ((Game.Prefabs.BuildingFlags)0u));
						bool isSideConnection3 = (value3.m_Flags & PedestrianLaneFlags.SideConnection) != 0;
						value3.m_AccessRestriction = GetAccessRestriction(owner3, flag5, isEdgeLane, isSideConnection3, noRestriction: false, isRoundabout: false, nativeArray[l], out var allowEnter3, out var allowExit2);
						if (allowEnter3)
						{
							value3.m_Flags |= PedestrianLaneFlags.AllowEnter;
						}
						if (allowExit2)
						{
							value3.m_Flags |= PedestrianLaneFlags.AllowExit;
						}
					}
					nativeArray3[l] = value3;
				}
			}
			bool allowExit3;
			if (nativeArray4.Length != 0)
			{
				for (int m = 0; m < nativeArray4.Length; m++)
				{
					Game.Net.TrackLane value4 = nativeArray4[m];
					value4.m_AccessRestriction = Entity.Null;
					if (nativeArray7.Length != 0)
					{
						Owner owner4 = nativeArray7[m];
						Game.Prefabs.BuildingFlags flag6 = (((value4.m_Flags & TrackLaneFlags.Station) != 0) ? Game.Prefabs.BuildingFlags.RestrictedTrack : ((Game.Prefabs.BuildingFlags)0u));
						value4.m_AccessRestriction = GetAccessRestriction(owner4, flag6, isEdgeLane, isSideConnection: false, noRestriction: false, isRoundabout: false, nativeArray[m], out allowExit, out allowExit3);
					}
					nativeArray4[m] = value4;
				}
			}
			if (nativeArray5.Length == 0)
			{
				return;
			}
			for (int n = 0; n < nativeArray5.Length; n++)
			{
				Game.Net.ConnectionLane value5 = nativeArray5[n];
				value5.m_AccessRestriction = Entity.Null;
				value5.m_Flags &= ~(ConnectionLaneFlags.AllowEnter | ConnectionLaneFlags.AllowExit);
				if (nativeArray7.Length != 0)
				{
					Owner owner5 = nativeArray7[n];
					bool noRestriction = (value5.m_Flags & ConnectionLaneFlags.NoRestriction) != 0;
					if ((value5.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						value5.m_AccessRestriction = GetAccessRestriction(owner5, Game.Prefabs.BuildingFlags.RestrictedPedestrian, isEdgeLane, isSideConnection: false, noRestriction, isRoundabout: false, nativeArray[n], out var allowEnter4, out var allowExit4);
						if (allowEnter4)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowEnter;
						}
						if (allowExit4)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowExit;
						}
					}
					else if ((value5.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						Game.Prefabs.BuildingFlags flag7 = (((value5.m_RoadTypes & (RoadTypes.Car | RoadTypes.Helicopter | RoadTypes.Airplane | RoadTypes.Bicycle)) != RoadTypes.None) ? Game.Prefabs.BuildingFlags.RestrictedCar : ((Game.Prefabs.BuildingFlags)0u));
						value5.m_AccessRestriction = GetAccessRestriction(owner5, flag7, isEdgeLane, isSideConnection: false, noRestriction, isRoundabout: false, nativeArray[n], out var allowEnter5, out allowExit3);
						if (allowEnter5)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowEnter;
						}
					}
					else if ((value5.m_Flags & ConnectionLaneFlags.Parking) != 0)
					{
						value5.m_AccessRestriction = GetAccessRestriction(owner5, Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar, isEdgeLane, isSideConnection: false, noRestriction, isRoundabout: false, nativeArray[n], out var allowEnter6, out var allowExit5);
						if (allowEnter6)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowEnter;
						}
						if (allowExit5)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowExit;
						}
					}
					else if ((value5.m_Flags & ConnectionLaneFlags.AllowCargo) != 0)
					{
						Game.Prefabs.BuildingFlags flag8 = Game.Prefabs.BuildingFlags.RestrictedCar;
						value5.m_AccessRestriction = GetAccessRestriction(owner5, flag8, isEdgeLane, isSideConnection: false, noRestriction, isRoundabout: false, nativeArray[n], out var allowEnter7, out allowExit3);
						if (allowEnter7)
						{
							value5.m_Flags |= ConnectionLaneFlags.AllowEnter;
						}
					}
					else if ((value5.m_Flags & ConnectionLaneFlags.Track) != 0)
					{
						value5.m_AccessRestriction = GetAccessRestriction(owner5, Game.Prefabs.BuildingFlags.RestrictedTrack, isEdgeLane, isSideConnection: false, noRestriction, isRoundabout: false, nativeArray[n], out allowExit3, out allowExit);
					}
				}
				nativeArray5[n] = value5;
			}
		}

		private bool IsSecured(InvolvedInAccident involvedInAccident)
		{
			Entity entity = FindAccidentSite(involvedInAccident.m_Event);
			if (entity != Entity.Null)
			{
				return (m_AccidentSiteData[entity].m_Flags & AccidentSiteFlags.Secured) != 0;
			}
			return true;
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

		private Entity GetAccessRestriction(Owner owner, Game.Prefabs.BuildingFlags flag, bool isEdgeLane, bool isSideConnection, bool noRestriction, bool isRoundabout, Lane lane, out bool allowEnter, out bool allowExit)
		{
			allowEnter = false;
			allowExit = false;
			isSideConnection |= !isEdgeLane && m_ConnectedNodes.HasBuffer(owner.m_Owner);
			if (isSideConnection)
			{
				DynamicBuffer<ConnectedEdge> bufferData3;
				if (m_ConnectedNodes.TryGetBuffer(owner.m_Owner, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						ConnectedNode connectedNode = bufferData[i];
						DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[connectedNode.m_Node];
						ConnectedEdge connectedEdge;
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							connectedEdge = dynamicBuffer[j];
							if (connectedEdge.m_Edge == owner.m_Owner || !m_SubLanes.TryGetBuffer(connectedEdge.m_Edge, out var bufferData2))
							{
								continue;
							}
							int num = 0;
							while (num < bufferData2.Length)
							{
								Game.Net.SubLane subLane = bufferData2[num];
								Lane lane2 = m_LaneData[subLane.m_SubLane];
								if (!lane2.m_StartNode.Equals(lane.m_StartNode) && !lane2.m_EndNode.Equals(lane.m_StartNode) && !lane2.m_StartNode.Equals(lane.m_EndNode) && !lane2.m_EndNode.Equals(lane.m_EndNode))
								{
									num++;
									continue;
								}
								goto IL_0128;
							}
						}
						continue;
						IL_0128:
						owner.m_Owner = connectedEdge.m_Edge;
						break;
					}
				}
				else if (m_ConnectedEdges.TryGetBuffer(owner.m_Owner, out bufferData3))
				{
					for (int k = 0; k < bufferData3.Length; k++)
					{
						ConnectedEdge connectedEdge2 = bufferData3[k];
						Game.Net.Edge edge = m_EdgeData[connectedEdge2.m_Edge];
						if (edge.m_Start != owner.m_Owner && edge.m_End != owner.m_Owner)
						{
							continue;
						}
						bufferData = m_ConnectedNodes[connectedEdge2.m_Edge];
						ConnectedEdge connectedEdge3;
						for (int l = 0; l < bufferData.Length; l++)
						{
							ConnectedNode connectedNode2 = bufferData[l];
							DynamicBuffer<ConnectedEdge> dynamicBuffer2 = m_ConnectedEdges[connectedNode2.m_Node];
							for (int m = 0; m < dynamicBuffer2.Length; m++)
							{
								connectedEdge3 = dynamicBuffer2[m];
								if (connectedEdge3.m_Edge == connectedEdge2.m_Edge || !m_SubLanes.TryGetBuffer(connectedEdge3.m_Edge, out var bufferData4))
								{
									continue;
								}
								int num2 = 0;
								while (num2 < bufferData4.Length)
								{
									Game.Net.SubLane subLane2 = bufferData4[num2];
									Lane lane3 = m_LaneData[subLane2.m_SubLane];
									if (!lane3.m_StartNode.Equals(lane.m_StartNode) && !lane3.m_EndNode.Equals(lane.m_StartNode) && !lane3.m_StartNode.Equals(lane.m_EndNode) && !lane3.m_EndNode.Equals(lane.m_EndNode))
									{
										num2++;
										continue;
									}
									goto IL_02e0;
								}
							}
						}
						continue;
						IL_02e0:
						owner.m_Owner = connectedEdge3.m_Edge;
						break;
					}
				}
			}
			if (m_PrefabRefData.TryGetComponent(owner.m_Owner, out var componentData) && m_PrefabGeometryData.TryGetComponent(componentData.m_Prefab, out var componentData2) && (componentData2.m_Flags & Game.Net.GeometryFlags.SubOwner) != 0)
			{
				return Entity.Null;
			}
			Game.Prefabs.BuildingFlags restrictionMask;
			Entity topLevelOwner = GetTopLevelOwner(owner.m_Owner, out restrictionMask);
			if (m_BuildingData.HasComponent(topLevelOwner))
			{
				PrefabRef prefabRef = m_PrefabRefData[topLevelOwner];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
				buildingData.m_Flags &= restrictionMask;
				if (noRestriction)
				{
					buildingData.m_Flags &= ~flag;
				}
				if (m_RoadData.HasComponent(owner.m_Owner))
				{
					buildingData.m_Flags &= ~(Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar);
				}
				if (m_GateData.HasComponent(owner.m_Owner))
				{
					return Entity.Null;
				}
				bool flag2 = (buildingData.m_Flags & flag) != 0;
				bool flag3 = (flag & Game.Prefabs.BuildingFlags.RestrictedCar) != 0;
				bool flag4 = (flag & Game.Prefabs.BuildingFlags.RestrictedPedestrian) != 0;
				if (flag2 || flag3 || flag4)
				{
					if (!isEdgeLane && !isSideConnection && m_ConnectedEdges.HasBuffer(owner.m_Owner))
					{
						DynamicBuffer<ConnectedEdge> dynamicBuffer3 = m_ConnectedEdges[owner.m_Owner];
						bool2 x = false;
						bool2 x2 = default(bool2);
						for (int n = 0; n < dynamicBuffer3.Length; n++)
						{
							ConnectedEdge connectedEdge4 = dynamicBuffer3[n];
							Game.Net.Edge edge2 = m_EdgeData[connectedEdge4.m_Edge];
							if (edge2.m_Start != owner.m_Owner && edge2.m_End != owner.m_Owner)
							{
								continue;
							}
							if (m_GateData.HasComponent(connectedEdge4.m_Edge) && m_SubLanes.TryGetBuffer(connectedEdge4.m_Edge, out var bufferData5))
							{
								for (int num3 = 0; num3 < bufferData5.Length; num3++)
								{
									Game.Net.SubLane subLane3 = bufferData5[num3];
									Lane lane4 = m_LaneData[subLane3.m_SubLane];
									x2.x = lane4.m_StartNode.Equals(lane.m_StartNode) || lane4.m_EndNode.Equals(lane.m_StartNode);
									x2.y = lane4.m_StartNode.Equals(lane.m_EndNode) || lane4.m_EndNode.Equals(lane.m_EndNode);
									if (math.any(x2))
									{
										return Entity.Null;
									}
								}
							}
							Game.Prefabs.BuildingFlags restrictionMask2;
							bool flag5 = topLevelOwner == GetTopLevelOwner(connectedEdge4.m_Edge, out restrictionMask2);
							if (isRoundabout)
							{
								if (flag5)
								{
									if (m_SubLanes.TryGetBuffer(connectedEdge4.m_Edge, out var bufferData6))
									{
										for (int num4 = 0; num4 < bufferData6.Length; num4++)
										{
											Game.Net.SubLane subLane4 = bufferData6[num4];
											Lane lane5 = m_LaneData[subLane4.m_SubLane];
											x.x |= lane5.m_StartNode.Equals(lane.m_StartNode) || lane5.m_EndNode.Equals(lane.m_StartNode) || lane5.m_StartNode.Equals(lane.m_EndNode) || lane5.m_EndNode.Equals(lane.m_EndNode);
										}
									}
								}
								else
								{
									x.y = true;
								}
							}
							else
							{
								if (flag5)
								{
									continue;
								}
								if (!flag3)
								{
									return Entity.Null;
								}
								if (!m_SubLanes.TryGetBuffer(connectedEdge4.m_Edge, out var bufferData7))
								{
									continue;
								}
								for (int num5 = 0; num5 < bufferData7.Length; num5++)
								{
									Game.Net.SubLane subLane5 = bufferData7[num5];
									Lane lane6 = m_LaneData[subLane5.m_SubLane];
									x.x |= lane6.m_StartNode.Equals(lane.m_StartNode) || lane6.m_EndNode.Equals(lane.m_StartNode);
									x.y |= lane6.m_StartNode.Equals(lane.m_EndNode) || lane6.m_EndNode.Equals(lane.m_EndNode);
									if (math.all(x))
									{
										return Entity.Null;
									}
								}
							}
						}
						if (isRoundabout && !x.x && x.y)
						{
							return Entity.Null;
						}
					}
					allowEnter = !flag2;
					if (flag3 && flag4)
					{
						allowExit = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RestrictedParking) == 0;
					}
					else if (flag4)
					{
						allowExit = allowEnter && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RestrictedCar) != 0;
					}
					else
					{
						allowExit = false;
					}
					return topLevelOwner;
				}
			}
			return Entity.Null;
		}

		private Entity GetTopLevelOwner(Entity entity, out Game.Prefabs.BuildingFlags restrictionMask)
		{
			Entity entity2 = entity;
			restrictionMask = Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar | Game.Prefabs.BuildingFlags.RestrictedParking | Game.Prefabs.BuildingFlags.RestrictedTrack;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity2, out componentData))
			{
				if (m_BuildingData.HasComponent(entity2))
				{
					PrefabRef prefabRef = m_PrefabRefData[entity2];
					restrictionMask &= m_PrefabBuildingData[prefabRef.m_Prefab].m_Flags;
				}
				entity2 = componentData.m_Owner;
			}
			if (m_AttachmentData.TryGetComponent(entity2, out var componentData2) && componentData2.m_Attached != Entity.Null)
			{
				entity2 = componentData2.m_Attached;
			}
			return entity2;
		}

		private void AddOptionData(ref Game.Net.CarLane carLane, Game.City.City city)
		{
			if ((carLane.m_Flags & Game.Net.CarLaneFlags.Highway) != 0 && CityUtils.CheckOption(city, CityOption.UnlimitedHighwaySpeed))
			{
				carLane.m_SpeedLimit = 111.111115f;
			}
		}

		private void AddOptionData(ref Game.Net.CarLane carLane, Owner owner, EdgeLane edgeLane, MasterLane masterLane, CarLaneData carLaneData)
		{
			bool flag = (carLaneData.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None && (carLaneData.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None && (masterLane.m_Flags & MasterLaneFlags.HasBikeOnlyLane) == 0 && edgeLane.m_EdgeDelta.x != edgeLane.m_EdgeDelta.y;
			bool flag2 = false;
			if (m_BorderDistrictData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				float2 x = carLane.m_SpeedLimit;
				if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None && (carLane.m_Flags & Game.Net.CarLaneFlags.Highway) == 0)
				{
					if (m_DistrictModifiers.TryGetBuffer(componentData.m_Left, out var bufferData))
					{
						AreaUtils.ApplyModifier(ref x.x, bufferData, DistrictModifierType.StreetSpeedLimit);
					}
					if (m_DistrictModifiers.TryGetBuffer(componentData.m_Right, out var bufferData2))
					{
						AreaUtils.ApplyModifier(ref x.y, bufferData2, DistrictModifierType.StreetSpeedLimit);
					}
				}
				if (flag)
				{
					flag2 = AreaUtils.CheckOption(componentData, DistrictOption.ForbidBicycles, ref m_DistrictData);
				}
				if (math.cmax(x) >= carLane.m_SpeedLimit)
				{
					carLane.m_SpeedLimit = math.max(carLane.m_SpeedLimit, math.cmin(x));
				}
				else
				{
					carLane.m_SpeedLimit = math.min(carLane.m_SpeedLimit, math.cmax(x));
				}
			}
			if (flag)
			{
				if (m_CompositionData.TryGetComponent(owner.m_Owner, out var componentData2) && m_NetCompositionData.TryGetComponent(componentData2.m_Edge, out var componentData3))
				{
					CompositionFlags.Side side = ((edgeLane.m_EdgeDelta.y < edgeLane.m_EdgeDelta.x != ((componentData3.m_Flags.m_General & CompositionFlags.General.Invert) != 0)) ? componentData3.m_Flags.m_Left : componentData3.m_Flags.m_Right);
					flag2 = flag2 || (side & CompositionFlags.Side.ForbidSecondary) != 0;
				}
				if (flag2)
				{
					carLane.m_Flags |= Game.Net.CarLaneFlags.ForbidBicycles;
				}
			}
		}

		private Bounds1 CheckBlockage(DynamicBuffer<LaneObject> laneObjects, out bool isEmergency, out bool isSecured)
		{
			Bounds1 result = new Bounds1(1f, 0f);
			isEmergency = false;
			isSecured = false;
			for (int i = 0; i < laneObjects.Length; i++)
			{
				LaneObject laneObject = laneObjects[i];
				if (!m_MovingData.HasComponent(laneObject.m_LaneObject))
				{
					result |= MathUtils.Bounds(laneObject.m_CurvePosition.x, laneObject.m_CurvePosition.y);
					Car componentData2;
					if (m_InvolvedInAccidenteData.TryGetComponent(laneObject.m_LaneObject, out var componentData))
					{
						isSecured |= IsSecured(componentData);
						isEmergency = true;
					}
					else if (m_CarData.TryGetComponent(laneObject.m_LaneObject, out componentData2))
					{
						isEmergency |= (componentData2.m_Flags & CarFlags.Emergency) != 0;
					}
				}
			}
			return result;
		}

		private void AddBlockageData(ref Game.Net.CarLane carLane, Bounds1 bounds, bool addCaution)
		{
			if (bounds.min <= bounds.max)
			{
				carLane.m_BlockageStart = (byte)math.max(0, Mathf.FloorToInt(bounds.min * 255f));
				carLane.m_BlockageEnd = (byte)math.min(255, Mathf.CeilToInt(bounds.max * 255f));
				if (addCaution)
				{
					carLane.m_CautionStart = carLane.m_BlockageStart;
					carLane.m_CautionEnd = carLane.m_BlockageEnd;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLaneData2Job : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				Game.Net.CarLane value = m_CarLaneData[entity];
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
				bool flag = false;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity subLane = dynamicBuffer[j].m_SubLane;
					if (!(subLane != entity) || !m_CarLaneData.HasComponent(subLane))
					{
						continue;
					}
					Game.Net.CarLane carLane = m_CarLaneData[subLane];
					if (carLane.m_CarriagewayGroup == value.m_CarriagewayGroup && carLane.m_CautionEnd >= carLane.m_CautionStart)
					{
						if (((value.m_Flags ^ carLane.m_Flags) & Game.Net.CarLaneFlags.Invert) != 0)
						{
							value.m_CautionStart = (byte)math.min(value.m_CautionStart, 255 - carLane.m_CautionEnd);
							value.m_CautionEnd = (byte)math.max(value.m_CautionEnd, 255 - carLane.m_CautionStart);
						}
						else
						{
							value.m_CautionStart = (byte)math.min((int)value.m_CautionStart, (int)carLane.m_CautionStart);
							value.m_CautionEnd = (byte)math.max((int)value.m_CautionEnd, (int)carLane.m_CautionEnd);
						}
						value.m_Flags |= carLane.m_Flags & Game.Net.CarLaneFlags.IsSecured;
						flag = true;
					}
				}
				if (flag)
				{
					m_CarLaneData[entity] = value;
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
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> __Game_Net_MasterLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LaneObject> __Game_Net_LaneObject_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Gate> __Game_Net_Gate_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.City.City> __Game_City_City_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<LaneObject>(isReadOnly: true);
			__Game_Net_CarLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>();
			__Game_Net_PedestrianLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.PedestrianLane>();
			__Game_Net_TrackLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>();
			__Game_Net_ConnectionLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ConnectionLane>();
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_Gate_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Gate>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_City_City_RO_ComponentLookup = state.GetComponentLookup<Game.City.City>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_CarLane_RW_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>();
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_LaneQuery2;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.TrackLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<GarageLane>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PathfindUpdated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.TrackLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<GarageLane>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LaneQuery2 = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.CarLane>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PathfindUpdated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.CarLane>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_LaneQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateLaneDataJob jobData = new UpdateLaneDataJob
		{
			m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Gate_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_City_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidenteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency);
		if (!m_LaneQuery.IsEmptyIgnoreFilter)
		{
			UpdateLaneData2Job jobData2 = new UpdateLaneData2Job
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, m_LaneQuery2, base.Dependency);
		}
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
	public LaneDataSystem()
	{
	}
}
