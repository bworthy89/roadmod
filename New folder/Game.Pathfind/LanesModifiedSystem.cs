using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Pathfind;

[CompilerGenerated]
public class LanesModifiedSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddPathEdgeJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Density> m_DensityData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PathfindPedestrianData> m_PedestrianPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindCarData> m_CarPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindTrackData> m_TrackPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> m_TransportPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> m_ConnectionPathfindData;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<GarageLane> m_GarageLaneType;

		[ReadOnly]
		public ComponentTypeHandle<LaneConnection> m_LaneConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[WriteOnly]
		public NativeArray<CreateActionData> m_Actions;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Lane> nativeArray2 = archetypeChunk.GetNativeArray(ref m_LaneType);
				NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CurveType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Game.Net.CarLane> nativeArray5 = archetypeChunk.GetNativeArray(ref m_CarLaneType);
				NativeArray<Game.Net.TrackLane> nativeArray6 = archetypeChunk.GetNativeArray(ref m_TrackLaneType);
				if (nativeArray5.Length != 0 && !archetypeChunk.Has(ref m_SlaveLaneType))
				{
					NativeArray<Owner> nativeArray7 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					NativeArray<LaneConnection> nativeArray8 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					NativeArray<MasterLane> nativeArray9 = archetypeChunk.GetNativeArray(ref m_MasterLaneType);
					if (nativeArray6.Length != 0)
					{
						for (int j = 0; j < nativeArray5.Length; j++)
						{
							Lane lane = nativeArray2[j];
							Curve curve = nativeArray3[j];
							Game.Net.CarLane carLane = nativeArray5[j];
							Game.Net.TrackLane trackLaneData = nativeArray6[j];
							PrefabRef prefabRef = nativeArray4[j];
							NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
							CarLaneData carLaneData = m_CarLaneData[prefabRef.m_Prefab];
							PathfindCarData carPathfindData = m_CarPathfindData[netLaneData.m_PathfindPrefab];
							PathfindTransportData transportPathfindData = m_TransportPathfindData[netLaneData.m_PathfindPrefab];
							RuleFlags ruleFlags = (RuleFlags)0;
							float num2 = 0.01f;
							if (CollectionUtils.TryGet(nativeArray7, j, out var value))
							{
								ruleFlags = GetRuleFlags(carLane, value, carLaneData);
								if (m_DensityData.TryGetComponent(value.m_Owner, out var componentData))
								{
									num2 = math.max(num2, componentData.m_Density);
								}
							}
							if (CollectionUtils.TryGet(nativeArray8, j, out var value2))
							{
								CheckLaneConnections(ref lane, value2);
							}
							CollectionUtils.TryGet(nativeArray9, j, out var value3);
							CreateActionData value4 = new CreateActionData
							{
								m_Owner = nativeArray[j],
								m_StartNode = lane.m_StartNode,
								m_MiddleNode = lane.m_MiddleNode,
								m_EndNode = lane.m_EndNode,
								m_Specification = PathUtils.GetCarDriveSpecification(curve, carLane, value3, trackLaneData, carLaneData, carPathfindData, ruleFlags, num2),
								m_Location = PathUtils.GetLocationSpecification(curve)
							};
							if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
							{
								value4.m_SecondaryStartNode = value4.m_StartNode;
								value4.m_SecondaryEndNode = value4.m_EndNode;
								value4.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve, carLane, carPathfindData, transportPathfindData, ruleFlags, num2);
							}
							m_Actions[num++] = value4;
						}
						continue;
					}
					for (int k = 0; k < nativeArray5.Length; k++)
					{
						Lane lane2 = nativeArray2[k];
						Curve curve2 = nativeArray3[k];
						Game.Net.CarLane carLane2 = nativeArray5[k];
						PrefabRef prefabRef2 = nativeArray4[k];
						NetLaneData netLaneData2 = m_NetLaneData[prefabRef2.m_Prefab];
						CarLaneData carLaneData2 = m_CarLaneData[prefabRef2.m_Prefab];
						PathfindCarData carPathfindData2 = m_CarPathfindData[netLaneData2.m_PathfindPrefab];
						PathfindTransportData transportPathfindData2 = m_TransportPathfindData[netLaneData2.m_PathfindPrefab];
						RuleFlags ruleFlags2 = (RuleFlags)0;
						float num3 = 0.01f;
						if (CollectionUtils.TryGet(nativeArray7, k, out var value5))
						{
							ruleFlags2 = GetRuleFlags(carLane2, value5, carLaneData2);
							if (m_DensityData.TryGetComponent(value5.m_Owner, out var componentData2))
							{
								num3 = math.max(num3, componentData2.m_Density);
							}
						}
						if (CollectionUtils.TryGet(nativeArray8, k, out var value6))
						{
							CheckLaneConnections(ref lane2, value6);
						}
						CollectionUtils.TryGet(nativeArray9, k, out var value7);
						CreateActionData value8 = new CreateActionData
						{
							m_Owner = nativeArray[k],
							m_StartNode = lane2.m_StartNode,
							m_MiddleNode = lane2.m_MiddleNode,
							m_EndNode = lane2.m_EndNode,
							m_Specification = PathUtils.GetCarDriveSpecification(curve2, carLane2, value7, carLaneData2, carPathfindData2, ruleFlags2, num3),
							m_Location = PathUtils.GetLocationSpecification(curve2)
						};
						if ((carLaneData2.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
						{
							value8.m_SecondaryStartNode = value8.m_StartNode;
							value8.m_SecondaryEndNode = value8.m_EndNode;
							value8.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve2, carLane2, carPathfindData2, transportPathfindData2, ruleFlags2, num3);
						}
						else if ((carLaneData2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None && (carLane2.m_Flags & (CarLaneFlags.SecondaryStart | CarLaneFlags.SecondaryEnd)) != 0)
						{
							value8.m_SecondaryStartNode = value8.m_StartNode;
							value8.m_SecondaryEndNode = value8.m_EndNode;
							value8.m_SecondarySpecification = value8.m_Specification;
							value8.m_Specification.m_Methods = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
						}
						m_Actions[num++] = value8;
					}
					continue;
				}
				if (nativeArray6.Length != 0)
				{
					for (int l = 0; l < nativeArray6.Length; l++)
					{
						Lane lane3 = nativeArray2[l];
						Curve curveData = nativeArray3[l];
						Game.Net.TrackLane trackLaneData2 = nativeArray6[l];
						PrefabRef prefabRef3 = nativeArray4[l];
						NetLaneData netLaneData3 = m_NetLaneData[prefabRef3.m_Prefab];
						PathfindTrackData trackPathfindData = m_TrackPathfindData[netLaneData3.m_PathfindPrefab];
						CreateActionData value9 = new CreateActionData
						{
							m_Owner = nativeArray[l],
							m_StartNode = lane3.m_StartNode,
							m_MiddleNode = lane3.m_MiddleNode,
							m_EndNode = lane3.m_EndNode,
							m_Specification = PathUtils.GetTrackDriveSpecification(curveData, trackLaneData2, trackPathfindData),
							m_Location = PathUtils.GetLocationSpecification(curveData)
						};
						m_Actions[num++] = value9;
					}
					continue;
				}
				NativeArray<Game.Net.ParkingLane> nativeArray10 = archetypeChunk.GetNativeArray(ref m_ParkingLaneType);
				if (nativeArray10.Length != 0)
				{
					NativeArray<LaneConnection> nativeArray11 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					for (int m = 0; m < nativeArray10.Length; m++)
					{
						Lane lane4 = nativeArray2[m];
						Curve curveData2 = nativeArray3[m];
						Game.Net.ParkingLane parkingLane = nativeArray10[m];
						PrefabRef prefabRef4 = nativeArray4[m];
						NetLaneData netLaneData4 = m_NetLaneData[prefabRef4.m_Prefab];
						ParkingLaneData parkingLaneData = m_ParkingLaneData[prefabRef4.m_Prefab];
						PathfindCarData carPathfindData3 = m_CarPathfindData[netLaneData4.m_PathfindPrefab];
						PathfindTransportData transportPathfindData3 = m_TransportPathfindData[netLaneData4.m_PathfindPrefab];
						if (CollectionUtils.TryGet(nativeArray11, m, out var value10))
						{
							CheckLaneConnections(ref lane4, value10);
						}
						CreateActionData value11 = new CreateActionData
						{
							m_Owner = nativeArray[m],
							m_StartNode = lane4.m_StartNode,
							m_MiddleNode = lane4.m_MiddleNode,
							m_EndNode = lane4.m_EndNode
						};
						if ((parkingLane.m_Flags & ParkingLaneFlags.SecondaryStart) == 0)
						{
							value11.m_Specification = PathUtils.GetParkingSpaceSpecification(parkingLane, parkingLaneData, carPathfindData3);
						}
						if ((parkingLane.m_Flags & (ParkingLaneFlags.AdditionalStart | ParkingLaneFlags.SecondaryStart)) != 0)
						{
							value11.m_SecondaryStartNode = (((parkingLane.m_Flags & ParkingLaneFlags.AdditionalStart) != 0) ? parkingLane.m_AdditionalStartNode : value11.m_StartNode);
							value11.m_SecondaryEndNode = value11.m_EndNode;
							value11.m_SecondarySpecification = PathUtils.GetParkingSpaceSpecification(parkingLane, parkingLaneData, carPathfindData3);
						}
						else if ((parkingLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
						{
							value11.m_SecondaryStartNode = value11.m_StartNode;
							value11.m_SecondaryEndNode = value11.m_EndNode;
							value11.m_SecondarySpecification = PathUtils.GetTaxiAccessSpecification(parkingLane, carPathfindData3, transportPathfindData3);
						}
						value11.m_Location = PathUtils.GetLocationSpecification(curveData2, parkingLane);
						m_Actions[num++] = value11;
					}
					continue;
				}
				NativeArray<Game.Net.PedestrianLane> nativeArray12 = archetypeChunk.GetNativeArray(ref m_PedestrianLaneType);
				if (nativeArray12.Length != 0)
				{
					NativeArray<LaneConnection> nativeArray13 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					for (int n = 0; n < nativeArray12.Length; n++)
					{
						Lane lane5 = nativeArray2[n];
						Curve curveData3 = nativeArray3[n];
						Game.Net.PedestrianLane pedestrianLaneData = nativeArray12[n];
						PrefabRef prefabRef5 = nativeArray4[n];
						NetLaneData netLaneData5 = m_NetLaneData[prefabRef5.m_Prefab];
						PathfindPedestrianData pedestrianPathfindData = m_PedestrianPathfindData[netLaneData5.m_PathfindPrefab];
						if (CollectionUtils.TryGet(nativeArray13, n, out var value12))
						{
							CheckLaneConnections(ref lane5, value12);
						}
						CreateActionData value13 = new CreateActionData
						{
							m_Owner = nativeArray[n],
							m_StartNode = lane5.m_StartNode,
							m_MiddleNode = lane5.m_MiddleNode,
							m_EndNode = lane5.m_EndNode,
							m_Specification = PathUtils.GetSpecification(curveData3, pedestrianLaneData, pedestrianPathfindData),
							m_Location = PathUtils.GetLocationSpecification(curveData3)
						};
						if ((pedestrianLaneData.m_Flags & PedestrianLaneFlags.AllowBicycle) != 0)
						{
							value13.m_SecondaryStartNode = value13.m_StartNode;
							value13.m_SecondaryEndNode = value13.m_EndNode;
							value13.m_SecondarySpecification = PathUtils.GetBicycleWalkSpecification(curveData3, pedestrianLaneData, pedestrianPathfindData);
						}
						m_Actions[num++] = value13;
					}
					continue;
				}
				NativeArray<Game.Net.ConnectionLane> nativeArray14 = archetypeChunk.GetNativeArray(ref m_ConnectionLaneType);
				if (nativeArray14.Length == 0)
				{
					continue;
				}
				NativeArray<GarageLane> nativeArray15 = archetypeChunk.GetNativeArray(ref m_GarageLaneType);
				NativeArray<Game.Net.OutsideConnection> nativeArray16 = archetypeChunk.GetNativeArray(ref m_OutsideConnectionType);
				for (int num4 = 0; num4 < nativeArray14.Length; num4++)
				{
					Lane lane6 = nativeArray2[num4];
					Curve curveData4 = nativeArray3[num4];
					Game.Net.ConnectionLane connectionLaneData = nativeArray14[num4];
					PrefabRef prefabRef6 = nativeArray4[num4];
					NetLaneData netLaneData6 = m_NetLaneData[prefabRef6.m_Prefab];
					PathfindConnectionData connectionPathfindData = m_ConnectionPathfindData[netLaneData6.m_PathfindPrefab];
					if (!CollectionUtils.TryGet(nativeArray15, num4, out var value14))
					{
						value14.m_VehicleCapacity = ushort.MaxValue;
					}
					CollectionUtils.TryGet(nativeArray16, num4, out var value15);
					CreateActionData value16 = new CreateActionData
					{
						m_Owner = nativeArray[num4],
						m_StartNode = lane6.m_StartNode,
						m_MiddleNode = lane6.m_MiddleNode,
						m_EndNode = lane6.m_EndNode
					};
					if (lane6.m_StartNode.Equals(lane6.m_EndNode) && (connectionLaneData.m_Flags & ConnectionLaneFlags.SecondaryStart) != 0)
					{
						value16.m_SecondaryStartNode = value16.m_StartNode;
						value16.m_SecondaryEndNode = value16.m_EndNode;
						value16.m_SecondarySpecification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						value16.m_SecondarySpecification.m_Flags |= EdgeFlags.SecondaryStart;
					}
					else
					{
						value16.m_Specification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						if ((connectionLaneData.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0)
						{
							value16.m_SecondaryStartNode = value16.m_StartNode;
							value16.m_SecondaryEndNode = value16.m_EndNode;
							value16.m_SecondarySpecification = PathUtils.GetSecondarySpecification(curveData4, connectionLaneData, value15, connectionPathfindData);
						}
					}
					value16.m_Location = PathUtils.GetLocationSpecification(curveData4);
					m_Actions[num++] = value16;
				}
			}
		}

		private void CheckLaneConnections(ref Lane lane, LaneConnection laneConnection)
		{
			if (m_LaneData.TryGetComponent(laneConnection.m_StartLane, out var componentData))
			{
				lane.m_StartNode = new PathNode(componentData.m_MiddleNode, laneConnection.m_StartPosition);
			}
			if (m_LaneData.TryGetComponent(laneConnection.m_EndLane, out var componentData2))
			{
				lane.m_EndNode = new PathNode(componentData2.m_MiddleNode, laneConnection.m_EndPosition);
			}
		}

		private RuleFlags GetRuleFlags(Game.Net.CarLane carLane, Owner owner, CarLaneData carLaneData)
		{
			RuleFlags addFlags = (RuleFlags)0;
			RuleFlags forbidFlags = (RuleFlags)0;
			if (m_BorderDistrictData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				if (m_DistrictData.TryGetComponent(componentData.m_Left, out var componentData2))
				{
					GetRuleFlags(carLane, componentData2, carLaneData, ref addFlags, ref forbidFlags);
				}
				if (m_DistrictData.TryGetComponent(componentData.m_Right, out var componentData3))
				{
					GetRuleFlags(carLane, componentData3, carLaneData, ref addFlags, ref forbidFlags);
				}
			}
			return (RuleFlags)((uint)addFlags & (uint)(byte)(~(int)forbidFlags));
		}

		private void GetRuleFlags(Game.Net.CarLane carLane, District district, CarLaneData carLaneData, ref RuleFlags addFlags, ref RuleFlags forbidFlags)
		{
			if ((carLaneData.m_RoadTypes & RoadTypes.Airplane) == 0)
			{
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidCombustionEngines))
				{
					addFlags |= RuleFlags.ForbidCombustionEngines;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidCombustionEngines;
				}
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidTransitTraffic))
				{
					addFlags |= RuleFlags.ForbidTransitTraffic;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidTransitTraffic;
				}
			}
			if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None && (carLane.m_Flags & CarLaneFlags.Highway) == 0)
			{
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidHeavyTraffic))
				{
					addFlags |= RuleFlags.ForbidHeavyTraffic;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidHeavyTraffic;
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdatePathEdgeJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Density> m_DensityData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PathfindPedestrianData> m_PedestrianPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindCarData> m_CarPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindTrackData> m_TrackPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> m_TransportPathfindData;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> m_ConnectionPathfindData;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<GarageLane> m_GarageLaneType;

		[ReadOnly]
		public ComponentTypeHandle<LaneConnection> m_LaneConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[WriteOnly]
		public NativeArray<UpdateActionData> m_Actions;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Lane> nativeArray2 = archetypeChunk.GetNativeArray(ref m_LaneType);
				NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CurveType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Game.Net.CarLane> nativeArray5 = archetypeChunk.GetNativeArray(ref m_CarLaneType);
				NativeArray<Game.Net.TrackLane> nativeArray6 = archetypeChunk.GetNativeArray(ref m_TrackLaneType);
				if (nativeArray5.Length != 0 && !archetypeChunk.Has(ref m_SlaveLaneType))
				{
					NativeArray<Owner> nativeArray7 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					NativeArray<LaneConnection> nativeArray8 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					NativeArray<MasterLane> nativeArray9 = archetypeChunk.GetNativeArray(ref m_MasterLaneType);
					if (nativeArray6.Length != 0)
					{
						for (int j = 0; j < nativeArray5.Length; j++)
						{
							Lane lane = nativeArray2[j];
							Curve curve = nativeArray3[j];
							Game.Net.CarLane carLane = nativeArray5[j];
							Game.Net.TrackLane trackLaneData = nativeArray6[j];
							PrefabRef prefabRef = nativeArray4[j];
							NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
							CarLaneData carLaneData = m_CarLaneData[prefabRef.m_Prefab];
							PathfindCarData carPathfindData = m_CarPathfindData[netLaneData.m_PathfindPrefab];
							PathfindTransportData transportPathfindData = m_TransportPathfindData[netLaneData.m_PathfindPrefab];
							RuleFlags ruleFlags = (RuleFlags)0;
							float num2 = 0.01f;
							if (CollectionUtils.TryGet(nativeArray7, j, out var value))
							{
								ruleFlags = GetRuleFlags(carLane, value, carLaneData);
								if (m_DensityData.TryGetComponent(value.m_Owner, out var componentData))
								{
									num2 = math.max(num2, componentData.m_Density);
								}
							}
							if (CollectionUtils.TryGet(nativeArray8, j, out var value2))
							{
								CheckLaneConnections(ref lane, value2);
							}
							CollectionUtils.TryGet(nativeArray9, j, out var value3);
							UpdateActionData value4 = new UpdateActionData
							{
								m_Owner = nativeArray[j],
								m_StartNode = lane.m_StartNode,
								m_MiddleNode = lane.m_MiddleNode,
								m_EndNode = lane.m_EndNode,
								m_Specification = PathUtils.GetCarDriveSpecification(curve, carLane, value3, trackLaneData, carLaneData, carPathfindData, ruleFlags, num2),
								m_Location = PathUtils.GetLocationSpecification(curve)
							};
							if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
							{
								value4.m_SecondaryStartNode = value4.m_StartNode;
								value4.m_SecondaryEndNode = value4.m_EndNode;
								value4.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve, carLane, carPathfindData, transportPathfindData, ruleFlags, num2);
							}
							m_Actions[num++] = value4;
						}
						continue;
					}
					for (int k = 0; k < nativeArray5.Length; k++)
					{
						Lane lane2 = nativeArray2[k];
						Curve curve2 = nativeArray3[k];
						Game.Net.CarLane carLane2 = nativeArray5[k];
						PrefabRef prefabRef2 = nativeArray4[k];
						NetLaneData netLaneData2 = m_NetLaneData[prefabRef2.m_Prefab];
						CarLaneData carLaneData2 = m_CarLaneData[prefabRef2.m_Prefab];
						PathfindCarData carPathfindData2 = m_CarPathfindData[netLaneData2.m_PathfindPrefab];
						PathfindTransportData transportPathfindData2 = m_TransportPathfindData[netLaneData2.m_PathfindPrefab];
						RuleFlags ruleFlags2 = (RuleFlags)0;
						float num3 = 0.01f;
						if (CollectionUtils.TryGet(nativeArray7, k, out var value5))
						{
							ruleFlags2 = GetRuleFlags(carLane2, value5, carLaneData2);
							if (m_DensityData.TryGetComponent(value5.m_Owner, out var componentData2))
							{
								num3 = math.max(num3, componentData2.m_Density);
							}
						}
						if (CollectionUtils.TryGet(nativeArray8, k, out var value6))
						{
							CheckLaneConnections(ref lane2, value6);
						}
						CollectionUtils.TryGet(nativeArray9, k, out var value7);
						UpdateActionData value8 = new UpdateActionData
						{
							m_Owner = nativeArray[k],
							m_StartNode = lane2.m_StartNode,
							m_MiddleNode = lane2.m_MiddleNode,
							m_EndNode = lane2.m_EndNode,
							m_Specification = PathUtils.GetCarDriveSpecification(curve2, carLane2, value7, carLaneData2, carPathfindData2, ruleFlags2, num3),
							m_Location = PathUtils.GetLocationSpecification(curve2)
						};
						if ((carLaneData2.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
						{
							value8.m_SecondaryStartNode = value8.m_StartNode;
							value8.m_SecondaryEndNode = value8.m_EndNode;
							value8.m_SecondarySpecification = PathUtils.GetTaxiDriveSpecification(curve2, carLane2, carPathfindData2, transportPathfindData2, ruleFlags2, num3);
						}
						else if ((carLaneData2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None && (carLane2.m_Flags & (CarLaneFlags.SecondaryStart | CarLaneFlags.SecondaryEnd)) != 0)
						{
							value8.m_SecondaryStartNode = value8.m_StartNode;
							value8.m_SecondaryEndNode = value8.m_EndNode;
							value8.m_SecondarySpecification = value8.m_Specification;
							value8.m_Specification.m_Methods = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
						}
						m_Actions[num++] = value8;
					}
					continue;
				}
				if (nativeArray6.Length != 0)
				{
					for (int l = 0; l < nativeArray6.Length; l++)
					{
						Lane lane3 = nativeArray2[l];
						Curve curveData = nativeArray3[l];
						Game.Net.TrackLane trackLaneData2 = nativeArray6[l];
						PrefabRef prefabRef3 = nativeArray4[l];
						NetLaneData netLaneData3 = m_NetLaneData[prefabRef3.m_Prefab];
						PathfindTrackData trackPathfindData = m_TrackPathfindData[netLaneData3.m_PathfindPrefab];
						UpdateActionData value9 = new UpdateActionData
						{
							m_Owner = nativeArray[l],
							m_StartNode = lane3.m_StartNode,
							m_MiddleNode = lane3.m_MiddleNode,
							m_EndNode = lane3.m_EndNode,
							m_Specification = PathUtils.GetTrackDriveSpecification(curveData, trackLaneData2, trackPathfindData),
							m_Location = PathUtils.GetLocationSpecification(curveData)
						};
						m_Actions[num++] = value9;
					}
					continue;
				}
				NativeArray<Game.Net.ParkingLane> nativeArray10 = archetypeChunk.GetNativeArray(ref m_ParkingLaneType);
				if (nativeArray10.Length != 0)
				{
					NativeArray<LaneConnection> nativeArray11 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					for (int m = 0; m < nativeArray10.Length; m++)
					{
						Lane lane4 = nativeArray2[m];
						Curve curveData2 = nativeArray3[m];
						Game.Net.ParkingLane parkingLane = nativeArray10[m];
						PrefabRef prefabRef4 = nativeArray4[m];
						NetLaneData netLaneData4 = m_NetLaneData[prefabRef4.m_Prefab];
						ParkingLaneData parkingLaneData = m_ParkingLaneData[prefabRef4.m_Prefab];
						PathfindCarData carPathfindData3 = m_CarPathfindData[netLaneData4.m_PathfindPrefab];
						PathfindTransportData transportPathfindData3 = m_TransportPathfindData[netLaneData4.m_PathfindPrefab];
						if (CollectionUtils.TryGet(nativeArray11, m, out var value10))
						{
							CheckLaneConnections(ref lane4, value10);
						}
						UpdateActionData value11 = new UpdateActionData
						{
							m_Owner = nativeArray[m],
							m_StartNode = lane4.m_StartNode,
							m_MiddleNode = lane4.m_MiddleNode,
							m_EndNode = lane4.m_EndNode
						};
						if ((parkingLane.m_Flags & ParkingLaneFlags.SecondaryStart) == 0)
						{
							value11.m_Specification = PathUtils.GetParkingSpaceSpecification(parkingLane, parkingLaneData, carPathfindData3);
						}
						if ((parkingLane.m_Flags & (ParkingLaneFlags.AdditionalStart | ParkingLaneFlags.SecondaryStart)) != 0)
						{
							value11.m_SecondaryStartNode = (((parkingLane.m_Flags & ParkingLaneFlags.AdditionalStart) != 0) ? parkingLane.m_AdditionalStartNode : value11.m_StartNode);
							value11.m_SecondaryEndNode = value11.m_EndNode;
							value11.m_SecondarySpecification = PathUtils.GetParkingSpaceSpecification(parkingLane, parkingLaneData, carPathfindData3);
						}
						else if ((parkingLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
						{
							value11.m_SecondaryStartNode = value11.m_StartNode;
							value11.m_SecondaryEndNode = value11.m_EndNode;
							value11.m_SecondarySpecification = PathUtils.GetTaxiAccessSpecification(parkingLane, carPathfindData3, transportPathfindData3);
						}
						value11.m_Location = PathUtils.GetLocationSpecification(curveData2, parkingLane);
						m_Actions[num++] = value11;
					}
					continue;
				}
				NativeArray<Game.Net.PedestrianLane> nativeArray12 = archetypeChunk.GetNativeArray(ref m_PedestrianLaneType);
				if (nativeArray12.Length != 0)
				{
					NativeArray<LaneConnection> nativeArray13 = archetypeChunk.GetNativeArray(ref m_LaneConnectionType);
					for (int n = 0; n < nativeArray12.Length; n++)
					{
						Lane lane5 = nativeArray2[n];
						Curve curveData3 = nativeArray3[n];
						Game.Net.PedestrianLane pedestrianLaneData = nativeArray12[n];
						PrefabRef prefabRef5 = nativeArray4[n];
						NetLaneData netLaneData5 = m_NetLaneData[prefabRef5.m_Prefab];
						PathfindPedestrianData pedestrianPathfindData = m_PedestrianPathfindData[netLaneData5.m_PathfindPrefab];
						if (CollectionUtils.TryGet(nativeArray13, n, out var value12))
						{
							CheckLaneConnections(ref lane5, value12);
						}
						UpdateActionData value13 = new UpdateActionData
						{
							m_Owner = nativeArray[n],
							m_StartNode = lane5.m_StartNode,
							m_MiddleNode = lane5.m_MiddleNode,
							m_EndNode = lane5.m_EndNode,
							m_Specification = PathUtils.GetSpecification(curveData3, pedestrianLaneData, pedestrianPathfindData),
							m_Location = PathUtils.GetLocationSpecification(curveData3)
						};
						if ((pedestrianLaneData.m_Flags & PedestrianLaneFlags.AllowBicycle) != 0)
						{
							value13.m_SecondaryStartNode = value13.m_StartNode;
							value13.m_SecondaryEndNode = value13.m_EndNode;
							value13.m_SecondarySpecification = PathUtils.GetBicycleWalkSpecification(curveData3, pedestrianLaneData, pedestrianPathfindData);
						}
						m_Actions[num++] = value13;
					}
					continue;
				}
				NativeArray<Game.Net.ConnectionLane> nativeArray14 = archetypeChunk.GetNativeArray(ref m_ConnectionLaneType);
				if (nativeArray14.Length == 0)
				{
					continue;
				}
				NativeArray<GarageLane> nativeArray15 = archetypeChunk.GetNativeArray(ref m_GarageLaneType);
				NativeArray<Game.Net.OutsideConnection> nativeArray16 = archetypeChunk.GetNativeArray(ref m_OutsideConnectionType);
				for (int num4 = 0; num4 < nativeArray14.Length; num4++)
				{
					Lane lane6 = nativeArray2[num4];
					Curve curveData4 = nativeArray3[num4];
					Game.Net.ConnectionLane connectionLaneData = nativeArray14[num4];
					PrefabRef prefabRef6 = nativeArray4[num4];
					NetLaneData netLaneData6 = m_NetLaneData[prefabRef6.m_Prefab];
					PathfindConnectionData connectionPathfindData = m_ConnectionPathfindData[netLaneData6.m_PathfindPrefab];
					if (!CollectionUtils.TryGet(nativeArray15, num4, out var value14))
					{
						value14.m_VehicleCapacity = ushort.MaxValue;
					}
					CollectionUtils.TryGet(nativeArray16, num4, out var value15);
					UpdateActionData value16 = new UpdateActionData
					{
						m_Owner = nativeArray[num4],
						m_StartNode = lane6.m_StartNode,
						m_MiddleNode = lane6.m_MiddleNode,
						m_EndNode = lane6.m_EndNode
					};
					if (lane6.m_StartNode.Equals(lane6.m_EndNode) && (connectionLaneData.m_Flags & ConnectionLaneFlags.SecondaryStart) != 0)
					{
						value16.m_SecondaryStartNode = value16.m_StartNode;
						value16.m_SecondaryEndNode = value16.m_EndNode;
						value16.m_SecondarySpecification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						value16.m_SecondarySpecification.m_Flags |= EdgeFlags.SecondaryStart;
					}
					else
					{
						value16.m_Specification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						if ((connectionLaneData.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0)
						{
							value16.m_SecondaryStartNode = value16.m_StartNode;
							value16.m_SecondaryEndNode = value16.m_EndNode;
							value16.m_SecondarySpecification = PathUtils.GetSecondarySpecification(curveData4, connectionLaneData, value15, connectionPathfindData);
						}
					}
					value16.m_Location = PathUtils.GetLocationSpecification(curveData4);
					m_Actions[num++] = value16;
				}
			}
		}

		private void CheckLaneConnections(ref Lane lane, LaneConnection laneConnection)
		{
			if (m_LaneData.TryGetComponent(laneConnection.m_StartLane, out var componentData))
			{
				lane.m_StartNode = new PathNode(componentData.m_MiddleNode, laneConnection.m_StartPosition);
			}
			if (m_LaneData.TryGetComponent(laneConnection.m_EndLane, out var componentData2))
			{
				lane.m_EndNode = new PathNode(componentData2.m_MiddleNode, laneConnection.m_EndPosition);
			}
		}

		private RuleFlags GetRuleFlags(Game.Net.CarLane carLane, Owner owner, CarLaneData carLaneData)
		{
			RuleFlags addFlags = (RuleFlags)0;
			RuleFlags forbidFlags = (RuleFlags)0;
			if (m_BorderDistrictData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				if (m_DistrictData.TryGetComponent(componentData.m_Left, out var componentData2))
				{
					GetRuleFlags(carLane, componentData2, carLaneData, ref addFlags, ref forbidFlags);
				}
				if (m_DistrictData.TryGetComponent(componentData.m_Right, out var componentData3))
				{
					GetRuleFlags(carLane, componentData3, carLaneData, ref addFlags, ref forbidFlags);
				}
			}
			return (RuleFlags)((uint)addFlags & (uint)(byte)(~(int)forbidFlags));
		}

		private void GetRuleFlags(Game.Net.CarLane carLane, District district, CarLaneData carLaneData, ref RuleFlags addFlags, ref RuleFlags forbidFlags)
		{
			if ((carLaneData.m_RoadTypes & RoadTypes.Airplane) == 0)
			{
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidCombustionEngines))
				{
					addFlags |= RuleFlags.ForbidCombustionEngines;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidCombustionEngines;
				}
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidTransitTraffic))
				{
					addFlags |= RuleFlags.ForbidTransitTraffic;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidTransitTraffic;
				}
			}
			if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None && (carLane.m_Flags & CarLaneFlags.Highway) == 0)
			{
				if (AreaUtils.CheckOption(district, DistrictOption.ForbidHeavyTraffic))
				{
					addFlags |= RuleFlags.ForbidHeavyTraffic;
				}
				else
				{
					forbidFlags |= RuleFlags.ForbidHeavyTraffic;
				}
			}
		}
	}

	[BurstCompile]
	private struct RemovePathEdgeJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[WriteOnly]
		public NativeArray<DeleteActionData> m_Actions;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				NativeArray<Entity> nativeArray = m_Chunks[i].GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					DeleteActionData value = new DeleteActionData
					{
						m_Owner = nativeArray[j]
					};
					m_Actions[num++] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Density> __Game_Net_Density_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindPedestrianData> __Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindCarData> __Game_Prefabs_PathfindCarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTrackData> __Game_Prefabs_PathfindTrackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> __Game_Prefabs_PathfindTransportData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> __Game_Prefabs_PathfindConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> __Game_Net_MasterLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GarageLane> __Game_Net_GarageLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LaneConnection> __Game_Net_LaneConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_Density_RO_ComponentLookup = state.GetComponentLookup<Density>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup = state.GetComponentLookup<PathfindPedestrianData>(isReadOnly: true);
			__Game_Prefabs_PathfindCarData_RO_ComponentLookup = state.GetComponentLookup<PathfindCarData>(isReadOnly: true);
			__Game_Prefabs_PathfindTrackData_RO_ComponentLookup = state.GetComponentLookup<PathfindTrackData>(isReadOnly: true);
			__Game_Prefabs_PathfindTransportData_RO_ComponentLookup = state.GetComponentLookup<PathfindTransportData>(isReadOnly: true);
			__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup = state.GetComponentLookup<PathfindConnectionData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GarageLane>(isReadOnly: true);
			__Game_Net_LaneConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LaneConnection>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private PathfindQueueSystem m_PathfindQueueSystem;

	private EntityQuery m_CreatedLanesQuery;

	private EntityQuery m_UpdatedLanesQuery;

	private EntityQuery m_DeletedLanesQuery;

	private EntityQuery m_AllLanesQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_CreatedLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SlaveLane>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.TrackLane>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SlaveLane>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.TrackLane>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
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
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SlaveLane>()
			}
		});
		m_DeletedLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SlaveLane>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.TrackLane>() },
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllLanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Lane>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.CarLane>(),
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<SlaveLane>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Lane>() },
			Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.TrackLane>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery;
		int num;
		if (GetLoaded())
		{
			entityQuery = m_AllLanesQuery;
			num = 0;
		}
		else
		{
			entityQuery = m_CreatedLanesQuery;
			num = m_UpdatedLanesQuery.CalculateEntityCount();
		}
		int num2 = entityQuery.CalculateEntityCount();
		int num3 = m_DeletedLanesQuery.CalculateEntityCount();
		if (num2 != 0 || num != 0 || num3 != 0)
		{
			JobHandle jobHandle = base.Dependency;
			if (num2 != 0)
			{
				CreateAction action = new CreateAction(num2, Allocator.Persistent);
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				JobHandle jobHandle2 = IJobExtensions.Schedule(new AddPathEdgeJob
				{
					m_Chunks = chunks,
					m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DensityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Density_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
					m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
					m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PedestrianPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CarPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindCarData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TrackPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTrackData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransportPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTransportData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ConnectionPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_GarageLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_LaneConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_Actions = action.m_CreateData
				}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
				chunks.Dispose(jobHandle2);
				m_PathfindQueueSystem.Enqueue(action, jobHandle2);
			}
			if (num != 0)
			{
				UpdateAction action2 = new UpdateAction(num, Allocator.Persistent);
				JobHandle outJobHandle2;
				NativeList<ArchetypeChunk> chunks2 = m_UpdatedLanesQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
				JobHandle jobHandle3 = IJobExtensions.Schedule(new UpdatePathEdgeJob
				{
					m_Chunks = chunks2,
					m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DensityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Density_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
					m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
					m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PedestrianPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CarPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindCarData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TrackPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTrackData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TransportPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTransportData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ConnectionPathfindData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_GarageLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_LaneConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_Actions = action2.m_UpdateData
				}, JobHandle.CombineDependencies(base.Dependency, outJobHandle2));
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
				chunks2.Dispose(jobHandle3);
				m_PathfindQueueSystem.Enqueue(action2, jobHandle3);
			}
			if (num3 != 0)
			{
				DeleteAction action3 = new DeleteAction(num3, Allocator.Persistent);
				JobHandle outJobHandle3;
				NativeList<ArchetypeChunk> chunks3 = m_DeletedLanesQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle3);
				JobHandle jobHandle4 = IJobExtensions.Schedule(new RemovePathEdgeJob
				{
					m_Chunks = chunks3,
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_Actions = action3.m_DeleteData
				}, JobHandle.CombineDependencies(base.Dependency, outJobHandle3));
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
				chunks3.Dispose(jobHandle4);
				m_PathfindQueueSystem.Enqueue(action3, jobHandle4);
			}
			base.Dependency = jobHandle;
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
	public LanesModifiedSystem()
	{
	}
}
