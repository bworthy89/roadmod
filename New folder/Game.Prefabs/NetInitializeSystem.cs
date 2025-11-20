using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class NetInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct FixPlaceholdersJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public BufferTypeHandle<PlaceholderObjectElement> m_PlaceholderObjectElementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<PlaceholderObjectElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PlaceholderObjectElementType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<PlaceholderObjectElement> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (m_DeletedData.HasComponent(dynamicBuffer[j].m_Object))
					{
						dynamicBuffer.RemoveAtSwapBack(j--);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeNetDefaultsJob : IJobParallelFor
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public BufferTypeHandle<NetGeometrySection> m_NetGeometrySectionType;

		public ComponentTypeHandle<NetData> m_NetType;

		public ComponentTypeHandle<NetGeometryData> m_NetGeometryType;

		public ComponentTypeHandle<PlaceableNetData> m_PlaceableNetType;

		public ComponentTypeHandle<RoadData> m_RoadType;

		public BufferTypeHandle<DefaultNetLane> m_DefaultNetLaneType;

		[ReadOnly]
		public ComponentLookup<NetPieceData> m_NetPieceData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<NetVertexMatchData> m_NetVertexMatchData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetPieceData> m_PlaceableNetPieceData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public BufferLookup<NetSubSection> m_NetSubSectionData;

		[ReadOnly]
		public BufferLookup<NetSectionPiece> m_NetSectionPieceData;

		[ReadOnly]
		public BufferLookup<NetPieceLane> m_NetPieceLanes;

		[ReadOnly]
		public BufferLookup<NetPieceObject> m_NetPieceObjects;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			NativeArray<NetGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref m_NetGeometryType);
			if (nativeArray.Length == 0)
			{
				return;
			}
			NativeArray<NetData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_NetType);
			NativeArray<PlaceableNetData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PlaceableNetType);
			NativeArray<RoadData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_RoadType);
			BufferAccessor<DefaultNetLane> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_DefaultNetLaneType);
			BufferAccessor<NetGeometrySection> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_NetGeometrySectionType);
			NativeList<NetCompositionPiece> nativeList = new NativeList<NetCompositionPiece>(32, Allocator.Temp);
			NativeList<NetCompositionLane> netLanes = new NativeList<NetCompositionLane>(32, Allocator.Temp);
			CompositionFlags flags = default(CompositionFlags);
			CompositionFlags flags2 = new CompositionFlags(CompositionFlags.General.Elevated, (CompositionFlags.Side)0u, (CompositionFlags.Side)0u);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<NetGeometrySection> geometrySections = bufferAccessor2[i];
				NetCompositionHelpers.GetCompositionPieces(nativeList, geometrySections.AsNativeArray(), flags, m_NetSubSectionData, m_NetSectionPieceData);
				NetCompositionData compositionData = default(NetCompositionData);
				NetCompositionHelpers.CalculateCompositionData(ref compositionData, nativeList.AsArray(), m_NetPieceData, m_NetVertexMatchData);
				NetCompositionHelpers.AddCompositionLanes(Entity.Null, ref compositionData, nativeList, netLanes, default(DynamicBuffer<NetCompositionCarriageway>), m_NetLaneData, m_NetPieceLanes);
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<DefaultNetLane> dynamicBuffer = bufferAccessor[i];
					dynamicBuffer.ResizeUninitialized(netLanes.Length);
					for (int j = 0; j < netLanes.Length; j++)
					{
						dynamicBuffer[j] = new DefaultNetLane(netLanes[j]);
					}
				}
				NetData netData = nativeArray2[i];
				netData.m_NodePriority += compositionData.m_Width;
				NetGeometryData value = nativeArray[i];
				value.m_DefaultWidth = compositionData.m_Width;
				value.m_DefaultHeightRange = compositionData.m_HeightRange;
				value.m_DefaultSurfaceHeight = compositionData.m_SurfaceHeight;
				UpdateFlagMasks(ref netData, geometrySections);
				if ((netData.m_RequiredLayers & (Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad)) != Layer.None)
				{
					netData.m_GeneralFlagMask |= CompositionFlags.General.TrafficLights | CompositionFlags.General.RemoveTrafficLights;
					netData.m_SideFlagMask |= CompositionFlags.Side.AddCrosswalk | CompositionFlags.Side.RemoveCrosswalk;
				}
				if ((netData.m_RequiredLayers & (Layer.Road | Layer.PublicTransportRoad)) != Layer.None)
				{
					netData.m_GeneralFlagMask |= CompositionFlags.General.AllWayStop;
					netData.m_SideFlagMask |= CompositionFlags.Side.ForbidLeftTurn | CompositionFlags.Side.ForbidRightTurn | CompositionFlags.Side.ForbidStraight;
				}
				bool num = (compositionData.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasForwardTrackLanes)) != 0;
				bool flag = (compositionData.m_State & (CompositionState.HasBackwardRoadLanes | CompositionState.HasBackwardTrackLanes)) != 0;
				if (num != flag)
				{
					value.m_Flags |= GeometryFlags.FlipTrafficHandedness;
				}
				if ((compositionData.m_State & CompositionState.Asymmetric) != 0)
				{
					value.m_Flags |= GeometryFlags.Asymmetric;
				}
				if ((compositionData.m_State & CompositionState.ExclusiveGround) != 0)
				{
					value.m_Flags |= GeometryFlags.ExclusiveGround;
				}
				if (nativeArray3.Length != 0 && (value.m_Flags & GeometryFlags.RequireElevated) == 0)
				{
					PlaceableNetComposition placeableData = default(PlaceableNetComposition);
					NetCompositionHelpers.CalculatePlaceableData(ref placeableData, nativeList.AsArray(), m_PlaceableNetPieceData);
					AddObjectCosts(ref placeableData, nativeList);
					PlaceableNetData value2 = nativeArray3[i];
					value2.m_DefaultConstructionCost = placeableData.m_ConstructionCost;
					value2.m_DefaultUpkeepCost = placeableData.m_UpkeepCost;
					nativeArray3[i] = value2;
				}
				if (nativeArray4.Length != 0)
				{
					RoadData value3 = nativeArray4[i];
					if ((compositionData.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) == CompositionState.HasForwardRoadLanes)
					{
						value3.m_Flags |= RoadFlags.DefaultIsForward;
					}
					else if ((compositionData.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) == CompositionState.HasBackwardRoadLanes)
					{
						value3.m_Flags |= RoadFlags.DefaultIsBackward;
					}
					if ((value3.m_Flags & RoadFlags.UseHighwayRules) != 0)
					{
						value.m_MinNodeOffset += value.m_DefaultWidth * 0.5f;
					}
					else if ((netData.m_RequiredLayers & (Layer.Road | Layer.PublicTransportRoad)) != Layer.None)
					{
						netData.m_SideFlagMask |= CompositionFlags.Side.ForbidSecondary;
					}
					nativeArray4[i] = value3;
				}
				nativeList.Clear();
				NetCompositionHelpers.GetCompositionPieces(nativeList, geometrySections.AsNativeArray(), flags2, m_NetSubSectionData, m_NetSectionPieceData);
				NetCompositionData compositionData2 = default(NetCompositionData);
				NetCompositionHelpers.CalculateCompositionData(ref compositionData2, nativeList.AsArray(), m_NetPieceData, m_NetVertexMatchData);
				value.m_ElevatedWidth = compositionData2.m_Width;
				value.m_ElevatedHeightRange = compositionData2.m_HeightRange;
				if (nativeArray3.Length != 0 && (value.m_Flags & GeometryFlags.RequireElevated) != 0)
				{
					PlaceableNetComposition placeableData2 = default(PlaceableNetComposition);
					NetCompositionHelpers.CalculatePlaceableData(ref placeableData2, nativeList.AsArray(), m_PlaceableNetPieceData);
					AddObjectCosts(ref placeableData2, nativeList);
					PlaceableNetData value4 = nativeArray3[i];
					value4.m_DefaultConstructionCost = placeableData2.m_ConstructionCost;
					value4.m_DefaultUpkeepCost = placeableData2.m_UpkeepCost;
					nativeArray3[i] = value4;
				}
				nativeArray2[i] = netData;
				nativeArray[i] = value;
				nativeList.Clear();
				netLanes.Clear();
			}
			nativeList.Dispose();
			netLanes.Dispose();
		}

		private void UpdateFlagMasks(ref NetData netData, DynamicBuffer<NetGeometrySection> geometrySections)
		{
			for (int i = 0; i < geometrySections.Length; i++)
			{
				NetGeometrySection netGeometrySection = geometrySections[i];
				netData.m_GeneralFlagMask |= netGeometrySection.m_CompositionAll.m_General;
				netData.m_SideFlagMask |= netGeometrySection.m_CompositionAll.m_Left | netGeometrySection.m_CompositionAll.m_Right;
				netData.m_GeneralFlagMask |= netGeometrySection.m_CompositionAny.m_General;
				netData.m_SideFlagMask |= netGeometrySection.m_CompositionAny.m_Left | netGeometrySection.m_CompositionAny.m_Right;
				netData.m_GeneralFlagMask |= netGeometrySection.m_CompositionNone.m_General;
				netData.m_SideFlagMask |= netGeometrySection.m_CompositionNone.m_Left | netGeometrySection.m_CompositionNone.m_Right;
				UpdateFlagMasks(ref netData, netGeometrySection.m_Section);
			}
		}

		private void UpdateFlagMasks(ref NetData netData, Entity section)
		{
			if (m_NetSubSectionData.TryGetBuffer(section, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					NetSubSection netSubSection = bufferData[i];
					netData.m_GeneralFlagMask |= netSubSection.m_CompositionAll.m_General;
					netData.m_SideFlagMask |= netSubSection.m_CompositionAll.m_Left | netSubSection.m_CompositionAll.m_Right;
					netData.m_GeneralFlagMask |= netSubSection.m_CompositionAny.m_General;
					netData.m_SideFlagMask |= netSubSection.m_CompositionAny.m_Left | netSubSection.m_CompositionAny.m_Right;
					netData.m_GeneralFlagMask |= netSubSection.m_CompositionNone.m_General;
					netData.m_SideFlagMask |= netSubSection.m_CompositionNone.m_Left | netSubSection.m_CompositionNone.m_Right;
					UpdateFlagMasks(ref netData, netSubSection.m_SubSection);
				}
			}
			if (!m_NetSectionPieceData.TryGetBuffer(section, out var bufferData2))
			{
				return;
			}
			for (int j = 0; j < bufferData2.Length; j++)
			{
				NetSectionPiece netSectionPiece = bufferData2[j];
				netData.m_GeneralFlagMask |= netSectionPiece.m_CompositionAll.m_General;
				netData.m_SideFlagMask |= netSectionPiece.m_CompositionAll.m_Left | netSectionPiece.m_CompositionAll.m_Right;
				netData.m_GeneralFlagMask |= netSectionPiece.m_CompositionAny.m_General;
				netData.m_SideFlagMask |= netSectionPiece.m_CompositionAny.m_Left | netSectionPiece.m_CompositionAny.m_Right;
				netData.m_GeneralFlagMask |= netSectionPiece.m_CompositionNone.m_General;
				netData.m_SideFlagMask |= netSectionPiece.m_CompositionNone.m_Left | netSectionPiece.m_CompositionNone.m_Right;
				if (m_NetPieceObjects.TryGetBuffer(netSectionPiece.m_Piece, out var bufferData3))
				{
					for (int k = 0; k < bufferData3.Length; k++)
					{
						NetPieceObject netPieceObject = bufferData3[k];
						netData.m_GeneralFlagMask |= netPieceObject.m_CompositionAll.m_General;
						netData.m_SideFlagMask |= netPieceObject.m_CompositionAll.m_Left | netPieceObject.m_CompositionAll.m_Right;
						netData.m_GeneralFlagMask |= netPieceObject.m_CompositionAny.m_General;
						netData.m_SideFlagMask |= netPieceObject.m_CompositionAny.m_Left | netPieceObject.m_CompositionAny.m_Right;
						netData.m_GeneralFlagMask |= netPieceObject.m_CompositionNone.m_General;
						netData.m_SideFlagMask |= netPieceObject.m_CompositionNone.m_Left | netPieceObject.m_CompositionNone.m_Right;
					}
				}
			}
		}

		private void AddObjectCosts(ref PlaceableNetComposition placeableCompositionData, NativeList<NetCompositionPiece> pieceBuffer)
		{
			for (int i = 0; i < pieceBuffer.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieceBuffer[i];
				if (!m_NetPieceObjects.HasBuffer(netCompositionPiece.m_Piece))
				{
					continue;
				}
				DynamicBuffer<NetPieceObject> dynamicBuffer = m_NetPieceObjects[netCompositionPiece.m_Piece];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					NetPieceObject netPieceObject = dynamicBuffer[j];
					if (m_PlaceableObjectData.HasComponent(netPieceObject.m_Prefab))
					{
						uint num = m_PlaceableObjectData[netPieceObject.m_Prefab].m_ConstructionCost;
						if (netPieceObject.m_Spacing.z > 0.1f)
						{
							num = (uint)Mathf.RoundToInt((float)num * (8f / netPieceObject.m_Spacing.z));
						}
						placeableCompositionData.m_ConstructionCost += num;
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct CollectPathfindDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<NetLaneData> m_NetLaneDataType;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLaneData> m_ConnectionLaneDataType;

		[ReadOnly]
		public ComponentLookup<PathfindCarData> m_PathfindCarData;

		[ReadOnly]
		public ComponentLookup<PathfindPedestrianData> m_PathfindPedestrianData;

		[ReadOnly]
		public ComponentLookup<PathfindTrackData> m_PathfindTrackData;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> m_PathfindTransportData;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> m_PathfindConnectionData;

		public NativeValue<PathfindHeuristicData> m_PathfindHeuristicData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetLaneData> nativeArray = chunk.GetNativeArray(ref m_NetLaneDataType);
			PathfindHeuristicData value = m_PathfindHeuristicData.value;
			if (chunk.Has(ref m_ConnectionLaneDataType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					NetLaneData netLaneData = nativeArray[i];
					if (m_PathfindConnectionData.TryGetComponent(netLaneData.m_PathfindPrefab, out var componentData))
					{
						value.m_FlyingCosts.m_Value = math.min(value.m_FlyingCosts.m_Value, componentData.m_AirwayCost.m_Value);
						value.m_OffRoadCosts.m_Value = math.min(value.m_OffRoadCosts.m_Value, componentData.m_AreaCost.m_Value);
					}
				}
			}
			else
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					NetLaneData netLaneData2 = nativeArray[j];
					if ((netLaneData2.m_Flags & LaneFlags.Road) != 0)
					{
						if (m_PathfindCarData.TryGetComponent(netLaneData2.m_PathfindPrefab, out var componentData2))
						{
							value.m_CarCosts.m_Value = math.min(value.m_CarCosts.m_Value, componentData2.m_DrivingCost.m_Value);
						}
						if (m_PathfindTransportData.TryGetComponent(netLaneData2.m_PathfindPrefab, out var componentData3))
						{
							value.m_TaxiCosts.m_Value = math.min(value.m_TaxiCosts.m_Value, componentData3.m_TravelCost.m_Value);
						}
					}
					if ((netLaneData2.m_Flags & LaneFlags.Track) != 0 && m_PathfindTrackData.TryGetComponent(netLaneData2.m_PathfindPrefab, out var componentData4))
					{
						value.m_TrackCosts.m_Value = math.min(value.m_TrackCosts.m_Value, componentData4.m_DrivingCost.m_Value);
					}
					if ((netLaneData2.m_Flags & LaneFlags.Pedestrian) != 0 && m_PathfindPedestrianData.TryGetComponent(netLaneData2.m_PathfindPrefab, out var componentData5))
					{
						value.m_PedestrianCosts.m_Value = math.min(value.m_PedestrianCosts.m_Value, componentData5.m_WalkingCost.m_Value);
					}
				}
			}
			m_PathfindHeuristicData.value = value;
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
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NetData> __Game_Prefabs_NetData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetPieceData> __Game_Prefabs_NetPieceData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetGeometryData> __Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MarkerNetData> __Game_Prefabs_MarkerNetData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<LocalConnectData> __Game_Prefabs_LocalConnectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetLaneData> __Game_Prefabs_NetLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarLaneData> __Game_Prefabs_CarLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TrackLaneData> __Game_Prefabs_TrackLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PedestrianLaneData> __Game_Prefabs_PedestrianLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<SecondaryLaneData> __Game_Prefabs_SecondaryLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetCrosswalkData> __Game_Prefabs_NetCrosswalkData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<RoadData> __Game_Prefabs_RoadData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TrackData> __Game_Prefabs_TrackData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WaterwayData> __Game_Prefabs_WaterwayData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathwayData> __Game_Prefabs_PathwayData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TaxiwayData> __Game_Prefabs_TaxiwayData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PowerLineData> __Game_Prefabs_PowerLineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PipelineData> __Game_Prefabs_PipelineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FenceData> __Game_Prefabs_FenceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainerData> __Game_Prefabs_EditorContainerData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WaterPipeConnectionData> __Game_Prefabs_WaterPipeConnectionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ResourceConnectionData> __Game_Prefabs_ResourceConnectionData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BridgeData> __Game_Prefabs_BridgeData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetTerrainData> __Game_Prefabs_NetTerrainData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UIObjectData> __Game_Prefabs_UIObjectData_RO_ComponentTypeHandle;

		public BufferTypeHandle<NetSubSection> __Game_Prefabs_NetSubSection_RW_BufferTypeHandle;

		public BufferTypeHandle<NetSectionPiece> __Game_Prefabs_NetSectionPiece_RW_BufferTypeHandle;

		public BufferTypeHandle<NetPieceLane> __Game_Prefabs_NetPieceLane_RW_BufferTypeHandle;

		public BufferTypeHandle<NetPieceArea> __Game_Prefabs_NetPieceArea_RW_BufferTypeHandle;

		public BufferTypeHandle<NetPieceObject> __Game_Prefabs_NetPieceObject_RW_BufferTypeHandle;

		public BufferTypeHandle<NetGeometrySection> __Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle;

		public BufferTypeHandle<NetGeometryEdgeState> __Game_Prefabs_NetGeometryEdgeState_RW_BufferTypeHandle;

		public BufferTypeHandle<NetGeometryNodeState> __Game_Prefabs_NetGeometryNodeState_RW_BufferTypeHandle;

		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RW_BufferTypeHandle;

		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<FixedNetElement> __Game_Prefabs_FixedNetElement_RW_BufferTypeHandle;

		public BufferTypeHandle<AuxiliaryNetLane> __Game_Prefabs_AuxiliaryNetLane_RW_BufferTypeHandle;

		public BufferTypeHandle<AuxiliaryNet> __Game_Prefabs_AuxiliaryNet_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<NetGeometrySection> __Game_Prefabs_NetGeometrySection_RO_BufferTypeHandle;

		public BufferTypeHandle<DefaultNetLane> __Game_Prefabs_DefaultNetLane_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetPieceData> __Game_Prefabs_NetPieceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetVertexMatchData> __Game_Prefabs_NetVertexMatchData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetPieceData> __Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<NetSubSection> __Game_Prefabs_NetSubSection_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetSectionPiece> __Game_Prefabs_NetSectionPiece_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetPieceLane> __Game_Prefabs_NetPieceLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetPieceObject> __Game_Prefabs_NetPieceObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLaneData> __Game_Prefabs_ConnectionLaneData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PathfindCarData> __Game_Prefabs_PathfindCarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTrackData> __Game_Prefabs_PathfindTrackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindPedestrianData> __Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> __Game_Prefabs_PathfindTransportData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindConnectionData> __Game_Prefabs_PathfindConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public BufferTypeHandle<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_NetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetData>();
			__Game_Prefabs_NetPieceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetPieceData>();
			__Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetGeometryData>();
			__Game_Prefabs_PlaceableNetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableNetData>();
			__Game_Prefabs_MarkerNetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MarkerNetData>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LocalConnectData>();
			__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneData>();
			__Game_Prefabs_NetLaneGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneGeometryData>();
			__Game_Prefabs_CarLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarLaneData>();
			__Game_Prefabs_TrackLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrackLaneData>();
			__Game_Prefabs_UtilityLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UtilityLaneData>();
			__Game_Prefabs_ParkingLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingLaneData>();
			__Game_Prefabs_PedestrianLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PedestrianLaneData>();
			__Game_Prefabs_SecondaryLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SecondaryLaneData>();
			__Game_Prefabs_NetCrosswalkData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetCrosswalkData>();
			__Game_Prefabs_RoadData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RoadData>();
			__Game_Prefabs_TrackData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrackData>();
			__Game_Prefabs_WaterwayData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterwayData>();
			__Game_Prefabs_PathwayData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathwayData>();
			__Game_Prefabs_TaxiwayData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiwayData>();
			__Game_Prefabs_PowerLineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PowerLineData>(isReadOnly: true);
			__Game_Prefabs_PipelineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PipelineData>(isReadOnly: true);
			__Game_Prefabs_FenceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FenceData>(isReadOnly: true);
			__Game_Prefabs_EditorContainerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EditorContainerData>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConnectionData>();
			__Game_Prefabs_WaterPipeConnectionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeConnectionData>();
			__Game_Prefabs_ResourceConnectionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceConnectionData>();
			__Game_Prefabs_BridgeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BridgeData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableObjectData>();
			__Game_Prefabs_NetTerrainData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetTerrainData>();
			__Game_Prefabs_UIObjectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UIObjectData>(isReadOnly: true);
			__Game_Prefabs_NetSubSection_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetSubSection>();
			__Game_Prefabs_NetSectionPiece_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetSectionPiece>();
			__Game_Prefabs_NetPieceLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetPieceLane>();
			__Game_Prefabs_NetPieceArea_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetPieceArea>();
			__Game_Prefabs_NetPieceObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetPieceObject>();
			__Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetGeometrySection>();
			__Game_Prefabs_NetGeometryEdgeState_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetGeometryEdgeState>();
			__Game_Prefabs_NetGeometryNodeState_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetGeometryNodeState>();
			__Game_Prefabs_SubObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>();
			__Game_Prefabs_SubMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>();
			__Game_Prefabs_FixedNetElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<FixedNetElement>();
			__Game_Prefabs_AuxiliaryNetLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<AuxiliaryNetLane>();
			__Game_Prefabs_AuxiliaryNet_RW_BufferTypeHandle = state.GetBufferTypeHandle<AuxiliaryNet>();
			__Game_Prefabs_NetGeometrySection_RO_BufferTypeHandle = state.GetBufferTypeHandle<NetGeometrySection>(isReadOnly: true);
			__Game_Prefabs_DefaultNetLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<DefaultNetLane>();
			__Game_Prefabs_NetPieceData_RO_ComponentLookup = state.GetComponentLookup<NetPieceData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NetVertexMatchData_RO_ComponentLookup = state.GetComponentLookup<NetVertexMatchData>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetPieceData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_NetSubSection_RO_BufferLookup = state.GetBufferLookup<NetSubSection>(isReadOnly: true);
			__Game_Prefabs_NetSectionPiece_RO_BufferLookup = state.GetBufferLookup<NetSectionPiece>(isReadOnly: true);
			__Game_Prefabs_NetPieceLane_RO_BufferLookup = state.GetBufferLookup<NetPieceLane>(isReadOnly: true);
			__Game_Prefabs_NetPieceObject_RO_BufferLookup = state.GetBufferLookup<NetPieceObject>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_ConnectionLaneData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConnectionLaneData>(isReadOnly: true);
			__Game_Prefabs_PathfindCarData_RO_ComponentLookup = state.GetComponentLookup<PathfindCarData>(isReadOnly: true);
			__Game_Prefabs_PathfindTrackData_RO_ComponentLookup = state.GetComponentLookup<PathfindTrackData>(isReadOnly: true);
			__Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup = state.GetComponentLookup<PathfindPedestrianData>(isReadOnly: true);
			__Game_Prefabs_PathfindTransportData_RO_ComponentLookup = state.GetComponentLookup<PathfindTransportData>(isReadOnly: true);
			__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup = state.GetComponentLookup<PathfindConnectionData>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PlaceholderObjectElement>();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_PlaceholderQuery;

	private NativeValue<PathfindHeuristicData> m_PathfindHeuristicData;

	private JobHandle m_PathfindHeuristicDeps;

	private Layer m_InGameLayersOnce;

	private Layer m_InGameLayersTwice;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadWrite<NetData>(),
				ComponentType.ReadWrite<NetSectionData>(),
				ComponentType.ReadWrite<NetPieceData>(),
				ComponentType.ReadWrite<NetLaneData>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[1] { ComponentType.ReadWrite<NetLaneData>() }
		});
		m_LaneQuery = GetEntityQuery(ComponentType.ReadOnly<NetLaneData>(), ComponentType.Exclude<Deleted>());
		m_PlaceholderQuery = GetEntityQuery(ComponentType.ReadOnly<NetLaneData>(), ComponentType.ReadOnly<PlaceholderObjectElement>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_PrefabQuery);
		m_PathfindHeuristicData = new NativeValue<PathfindHeuristicData>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_PathfindHeuristicData.Dispose();
		base.OnDestroy();
	}

	public PathfindHeuristicData GetHeuristicData()
	{
		m_PathfindHeuristicDeps.Complete();
		return m_PathfindHeuristicData.value;
	}

	public bool CanReplace(NetData netData, bool inGame)
	{
		if (!inGame)
		{
			return true;
		}
		return (netData.m_RequiredLayers & m_InGameLayersOnce & ~m_InGameLayersTwice) == 0;
	}

	private void AddSections(PrefabBase prefab, NetSectionInfo[] source, DynamicBuffer<NetGeometrySection> target, NetSectionFlags flags)
	{
		int2 @int = new int2(int.MaxValue, int.MinValue);
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i].m_Median)
			{
				int y = i << 1;
				@int.x = math.min(@int.x, y);
				@int.y = math.max(@int.y, y);
			}
		}
		if (@int.Equals(new int2(int.MaxValue, int.MinValue)))
		{
			@int = source.Length - 1;
			flags |= NetSectionFlags.AlignCenter;
		}
		for (int j = 0; j < source.Length; j++)
		{
			NetSectionInfo netSectionInfo = source[j];
			NetGeometrySection elem = new NetGeometrySection
			{
				m_Section = m_PrefabSystem.GetEntity(netSectionInfo.m_Section),
				m_Offset = netSectionInfo.m_Offset,
				m_Flags = flags
			};
			NetCompositionHelpers.GetRequirementFlags(netSectionInfo.m_RequireAll, out elem.m_CompositionAll, out var sectionFlags);
			NetCompositionHelpers.GetRequirementFlags(netSectionInfo.m_RequireAny, out elem.m_CompositionAny, out var sectionFlags2);
			NetCompositionHelpers.GetRequirementFlags(netSectionInfo.m_RequireNone, out elem.m_CompositionNone, out var sectionFlags3);
			NetSectionFlags netSectionFlags = sectionFlags | sectionFlags2 | sectionFlags3;
			if (netSectionFlags != 0)
			{
				COSystemBase.baseLog.ErrorFormat(prefab, "Net section ({0}: {1}) cannot require section flags: {2}", prefab.name, netSectionInfo.m_Section.name, netSectionFlags);
			}
			if (netSectionInfo.m_Invert)
			{
				elem.m_Flags |= NetSectionFlags.Invert;
			}
			if (netSectionInfo.m_Flip)
			{
				elem.m_Flags |= NetSectionFlags.FlipLanes | NetSectionFlags.FlipMesh;
			}
			if (netSectionInfo.m_HalfLength)
			{
				elem.m_Flags |= NetSectionFlags.HalfLength;
			}
			NetPieceLayerMask netPieceLayerMask = NetPieceLayerMask.Surface | NetPieceLayerMask.Bottom | NetPieceLayerMask.Top | NetPieceLayerMask.Side;
			if ((netSectionInfo.m_HiddenLayers & netPieceLayerMask) == netPieceLayerMask)
			{
				elem.m_Flags |= NetSectionFlags.Hidden;
			}
			if ((netSectionInfo.m_HiddenLayers & NetPieceLayerMask.Surface) != 0)
			{
				elem.m_Flags |= NetSectionFlags.HiddenSurface;
			}
			if ((netSectionInfo.m_HiddenLayers & NetPieceLayerMask.Bottom) != 0)
			{
				elem.m_Flags |= NetSectionFlags.HiddenBottom;
			}
			if ((netSectionInfo.m_HiddenLayers & NetPieceLayerMask.Top) != 0)
			{
				elem.m_Flags |= NetSectionFlags.HiddenTop;
			}
			if ((netSectionInfo.m_HiddenLayers & NetPieceLayerMask.Side) != 0)
			{
				elem.m_Flags |= NetSectionFlags.HiddenSide;
			}
			int num = j << 1;
			if (num >= @int.x && num <= @int.y)
			{
				elem.m_Flags |= NetSectionFlags.Median;
			}
			else if (num > @int.y)
			{
				elem.m_Flags |= NetSectionFlags.Right;
			}
			else
			{
				elem.m_Flags |= NetSectionFlags.Left;
			}
			target.Add(elem);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		bool flag = false;
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetPieceData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetPieceData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetGeometryData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PlaceableNetData> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<MarkerNetData> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MarkerNetData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<LocalConnectData> typeHandle8 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetLaneData> typeHandle9 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetLaneGeometryData> typeHandle10 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<CarLaneData> typeHandle11 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<TrackLaneData> typeHandle12 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<UtilityLaneData> typeHandle13 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<ParkingLaneData> typeHandle14 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PedestrianLaneData> typeHandle15 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PedestrianLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<SecondaryLaneData> typeHandle16 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SecondaryLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetCrosswalkData> typeHandle17 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCrosswalkData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<RoadData> typeHandle18 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_RoadData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<TrackData> typeHandle19 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TrackData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<WaterwayData> typeHandle20 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterwayData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PathwayData> typeHandle21 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PathwayData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<TaxiwayData> typeHandle22 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TaxiwayData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PowerLineData> typeHandle23 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PowerLineData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PipelineData> typeHandle24 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PipelineData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<FenceData> typeHandle25 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_FenceData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<EditorContainerData> typeHandle26 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EditorContainerData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<ElectricityConnectionData> typeHandle27 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<WaterPipeConnectionData> typeHandle28 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPipeConnectionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<ResourceConnectionData> typeHandle29 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResourceConnectionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BridgeData> typeHandle30 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BridgeData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<SpawnableObjectData> typeHandle31 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetTerrainData> typeHandle32 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetTerrainData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<UIObjectData> typeHandle33 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UIObjectData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetSubSection> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetSubSection_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetSectionPiece> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetSectionPiece_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetPieceLane> bufferTypeHandle3 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetPieceLane_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetPieceArea> bufferTypeHandle4 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetPieceArea_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetPieceObject> bufferTypeHandle5 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetPieceObject_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetGeometrySection> bufferTypeHandle6 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetGeometryEdgeState> bufferTypeHandle7 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometryEdgeState_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<NetGeometryNodeState> bufferTypeHandle8 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometryNodeState_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubObject> bufferTypeHandle9 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<SubMesh> bufferTypeHandle10 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<FixedNetElement> bufferTypeHandle11 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_FixedNetElement_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<AuxiliaryNetLane> bufferTypeHandle12 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AuxiliaryNetLane_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<AuxiliaryNet> bufferTypeHandle13 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RW_BufferTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			FixedNetElement value53 = default(FixedNetElement);
			for (int i = 0; i < chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = chunks[i];
				if (archetypeChunk.Has(ref typeHandle))
				{
					flag = archetypeChunk.Has(ref typeHandle31);
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
				bool flag2 = archetypeChunk.Has(ref typeHandle7);
				bool flag3 = archetypeChunk.Has(ref typeHandle30);
				bool flag4 = archetypeChunk.Has(ref typeHandle33);
				NativeArray<NetData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
				NativeArray<NetGeometryData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle5);
				NativeArray<PlaceableNetData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle6);
				NativeArray<PathwayData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle21);
				if (nativeArray4.Length != 0)
				{
					BufferAccessor<NetGeometrySection> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle6);
					for (int j = 0; j < nativeArray4.Length; j++)
					{
						_ = nativeArray[j];
						NetGeometryPrefab prefab = m_PrefabSystem.GetPrefab<NetGeometryPrefab>(nativeArray2[j]);
						NetGeometryData value = nativeArray4[j];
						DynamicBuffer<NetGeometrySection> target = bufferAccessor[j];
						value.m_EdgeLengthRange.max = 200f;
						value.m_ElevatedLength = 80f;
						value.m_MaxSlopeSteepness = math.select(prefab.m_MaxSlopeSteepness, 0f, prefab.m_MaxSlopeSteepness < 0.001f);
						value.m_ElevationLimit = 4f;
						if (prefab.m_AggregateType != null)
						{
							value.m_AggregateType = m_PrefabSystem.GetEntity(prefab.m_AggregateType);
						}
						if (prefab.m_StyleType != null)
						{
							value.m_StyleType = m_PrefabSystem.GetEntity(prefab.m_StyleType);
						}
						if (flag2)
						{
							value.m_Flags |= GeometryFlags.Marker;
						}
						AddSections(prefab, prefab.m_Sections, target, (NetSectionFlags)0);
						UndergroundNetSections component = prefab.GetComponent<UndergroundNetSections>();
						if (component != null)
						{
							AddSections(prefab, component.m_Sections, target, NetSectionFlags.Underground);
						}
						OverheadNetSections component2 = prefab.GetComponent<OverheadNetSections>();
						if (component2 != null)
						{
							AddSections(prefab, component2.m_Sections, target, NetSectionFlags.Overhead);
						}
						switch (prefab.m_InvertMode)
						{
						case CompositionInvertMode.InvertLefthandTraffic:
							value.m_Flags |= GeometryFlags.InvertCompositionHandedness;
							break;
						case CompositionInvertMode.FlipLefthandTraffic:
							value.m_Flags |= GeometryFlags.FlipCompositionHandedness;
							break;
						case CompositionInvertMode.InvertRighthandTraffic:
							value.m_Flags |= GeometryFlags.IsLefthanded | GeometryFlags.InvertCompositionHandedness;
							break;
						case CompositionInvertMode.FlipRighthandTraffic:
							value.m_Flags |= GeometryFlags.IsLefthanded | GeometryFlags.FlipCompositionHandedness;
							break;
						}
						nativeArray4[j] = value;
					}
					BufferAccessor<NetGeometryEdgeState> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle7);
					BufferAccessor<NetGeometryNodeState> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle8);
					for (int k = 0; k < nativeArray4.Length; k++)
					{
						NetGeometryPrefab prefab2 = m_PrefabSystem.GetPrefab<NetGeometryPrefab>(nativeArray2[k]);
						DynamicBuffer<NetGeometryEdgeState> dynamicBuffer = bufferAccessor2[k];
						DynamicBuffer<NetGeometryNodeState> dynamicBuffer2 = bufferAccessor3[k];
						if (prefab2.m_EdgeStates != null)
						{
							for (int l = 0; l < prefab2.m_EdgeStates.Length; l++)
							{
								NetEdgeStateInfo obj = prefab2.m_EdgeStates[l];
								NetGeometryEdgeState elem = default(NetGeometryEdgeState);
								NetCompositionHelpers.GetRequirementFlags(obj.m_RequireAll, out elem.m_CompositionAll, out var sectionFlags);
								NetCompositionHelpers.GetRequirementFlags(obj.m_RequireAny, out elem.m_CompositionAny, out var sectionFlags2);
								NetCompositionHelpers.GetRequirementFlags(obj.m_RequireNone, out elem.m_CompositionNone, out var sectionFlags3);
								NetCompositionHelpers.GetRequirementFlags(obj.m_SetState, out elem.m_State, out var sectionFlags4);
								NetSectionFlags netSectionFlags = sectionFlags | sectionFlags2 | sectionFlags3 | sectionFlags4;
								if (netSectionFlags != 0)
								{
									COSystemBase.baseLog.ErrorFormat(prefab2, "Net edge state ({0}) cannot require/set section flags: {1}", prefab2.name, netSectionFlags);
								}
								dynamicBuffer.Add(elem);
							}
						}
						if (prefab2.m_NodeStates == null)
						{
							continue;
						}
						for (int m = 0; m < prefab2.m_NodeStates.Length; m++)
						{
							NetNodeStateInfo netNodeStateInfo = prefab2.m_NodeStates[m];
							NetGeometryNodeState elem2 = default(NetGeometryNodeState);
							NetCompositionHelpers.GetRequirementFlags(netNodeStateInfo.m_RequireAll, out elem2.m_CompositionAll, out var sectionFlags5);
							NetCompositionHelpers.GetRequirementFlags(netNodeStateInfo.m_RequireAny, out elem2.m_CompositionAny, out var sectionFlags6);
							NetCompositionHelpers.GetRequirementFlags(netNodeStateInfo.m_RequireNone, out elem2.m_CompositionNone, out var sectionFlags7);
							NetCompositionHelpers.GetRequirementFlags(netNodeStateInfo.m_SetState, out elem2.m_State, out var sectionFlags8);
							NetSectionFlags netSectionFlags2 = sectionFlags5 | sectionFlags6 | sectionFlags7 | sectionFlags8;
							if (netSectionFlags2 != 0)
							{
								COSystemBase.baseLog.ErrorFormat(prefab2, "Net node state ({0}) cannot require/set section flags: {1}", prefab2.name, netSectionFlags2);
							}
							elem2.m_MatchType = netNodeStateInfo.m_MatchType;
							dynamicBuffer2.Add(elem2);
						}
					}
				}
				for (int n = 0; n < nativeArray5.Length; n++)
				{
					NetPrefab prefab3 = m_PrefabSystem.GetPrefab<NetPrefab>(nativeArray2[n]);
					PlaceableNetData value2 = nativeArray5[n];
					value2.m_SnapDistance = 8f;
					value2.m_MinWaterElevation = 5f;
					PlaceableNet component3 = prefab3.GetComponent<PlaceableNet>();
					if (component3 != null)
					{
						value2.m_ElevationRange = component3.m_ElevationRange;
						value2.m_XPReward = component3.m_XPReward;
						if (component3.m_UndergroundPrefab != null)
						{
							value2.m_UndergroundPrefab = m_PrefabSystem.GetEntity(component3.m_UndergroundPrefab);
						}
						if (component3.m_AllowParallelMode)
						{
							value2.m_PlacementFlags |= PlacementFlags.AllowParallel;
						}
					}
					NetUpgrade component4 = prefab3.GetComponent<NetUpgrade>();
					if (component4 != null)
					{
						NetCompositionHelpers.GetRequirementFlags(component4.m_SetState, out value2.m_SetUpgradeFlags, out var sectionFlags9);
						NetCompositionHelpers.GetRequirementFlags(component4.m_UnsetState, out value2.m_UnsetUpgradeFlags, out var sectionFlags10);
						value2.m_PlacementFlags |= PlacementFlags.IsUpgrade;
						if (!component4.m_Standalone)
						{
							value2.m_PlacementFlags |= PlacementFlags.UpgradeOnly;
						}
						if (component4.m_Underground)
						{
							value2.m_PlacementFlags |= PlacementFlags.UndergroundUpgrade;
						}
						if (((value2.m_SetUpgradeFlags | value2.m_UnsetUpgradeFlags) & CompositionFlags.nodeMask) != default(CompositionFlags))
						{
							value2.m_PlacementFlags |= PlacementFlags.NodeUpgrade;
						}
						NetSectionFlags netSectionFlags3 = sectionFlags9 | sectionFlags10;
						if (netSectionFlags3 != 0)
						{
							COSystemBase.baseLog.ErrorFormat(prefab3, "PlaceableNet ({0}) cannot upgrade section flags: {1}", prefab3.name, netSectionFlags3);
						}
						if ((value2.m_SetUpgradeFlags & CompositionFlags.directionalMask) != default(CompositionFlags) && nativeArray4.Length != 0)
						{
							NetGeometryData value3 = nativeArray4[n];
							value3.m_Flags |= GeometryFlags.Directional;
							nativeArray4[n] = value3;
						}
					}
					nativeArray5[n] = value2;
				}
				BufferAccessor<SubObject> bufferAccessor4 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle9);
				for (int num = 0; num < bufferAccessor4.Length; num++)
				{
					NetSubObjects component5 = m_PrefabSystem.GetPrefab<NetPrefab>(nativeArray2[num]).GetComponent<NetSubObjects>();
					bool flag5 = false;
					bool flag6 = false;
					NetGeometryData value4 = default(NetGeometryData);
					if (nativeArray4.Length != 0)
					{
						value4 = nativeArray4[num];
					}
					DynamicBuffer<SubObject> dynamicBuffer3 = bufferAccessor4[num];
					for (int num2 = 0; num2 < component5.m_SubObjects.Length; num2++)
					{
						NetSubObjectInfo netSubObjectInfo = component5.m_SubObjects[num2];
						ObjectPrefab prefab4 = netSubObjectInfo.m_Object;
						SubObject elem3 = new SubObject
						{
							m_Prefab = m_PrefabSystem.GetEntity(prefab4),
							m_Position = netSubObjectInfo.m_Position,
							m_Rotation = netSubObjectInfo.m_Rotation,
							m_Probability = 100
						};
						switch (netSubObjectInfo.m_Placement)
						{
						case NetObjectPlacement.EdgeEndsOrNode:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.AllowCombine;
							break;
						case NetObjectPlacement.EdgeMiddle:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.MiddlePlacement;
							if (base.EntityManager.HasComponent<PillarData>(elem3.m_Prefab))
							{
								value4.m_Flags |= GeometryFlags.MiddlePillars;
							}
							if (netSubObjectInfo.m_Spacing != 0f)
							{
								elem3.m_Flags |= SubObjectFlags.EvenSpacing;
								elem3.m_Position.z = netSubObjectInfo.m_Spacing;
							}
							break;
						case NetObjectPlacement.EdgeEnds:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement;
							break;
						case NetObjectPlacement.CourseStart:
							elem3.m_Flags |= SubObjectFlags.CoursePlacement | SubObjectFlags.StartPlacement;
							if (!flag5)
							{
								elem3.m_Flags |= SubObjectFlags.MakeOwner;
								value4.m_Flags |= GeometryFlags.SubOwner;
								flag5 = true;
							}
							break;
						case NetObjectPlacement.CourseEnd:
							elem3.m_Flags |= SubObjectFlags.CoursePlacement | SubObjectFlags.EndPlacement;
							if (!flag5)
							{
								elem3.m_Flags |= SubObjectFlags.MakeOwner;
								value4.m_Flags |= GeometryFlags.SubOwner;
								flag5 = true;
							}
							break;
						case NetObjectPlacement.NodeBeforeFixedSegment:
							elem3.m_Flags |= SubObjectFlags.StartPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.NodeBetweenFixedSegment:
							elem3.m_Flags |= SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.NodeAfterFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EndPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeMiddleFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.MiddlePlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							if (base.EntityManager.HasComponent<PillarData>(elem3.m_Prefab))
							{
								value4.m_Flags |= GeometryFlags.MiddlePillars;
							}
							if (netSubObjectInfo.m_Spacing != 0f)
							{
								elem3.m_Flags |= SubObjectFlags.EvenSpacing;
								elem3.m_Position.z = netSubObjectInfo.m_Spacing;
							}
							break;
						case NetObjectPlacement.EdgeEndsFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeStartFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.StartPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeEndFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.EndPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeEndsOrNodeFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.AllowCombine | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeStartOrNodeFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.AllowCombine | SubObjectFlags.StartPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.EdgeEndOrNodeFixedSegment:
							elem3.m_Flags |= SubObjectFlags.EdgePlacement | SubObjectFlags.AllowCombine | SubObjectFlags.EndPlacement | SubObjectFlags.FixedPlacement;
							elem3.m_ParentIndex = netSubObjectInfo.m_FixedIndex;
							break;
						case NetObjectPlacement.WaterwayCrossingNode:
							elem3.m_Flags |= SubObjectFlags.WaterwayCrossing;
							value4.m_IntersectLayers |= Layer.Waterway;
							flag6 = true;
							break;
						case NetObjectPlacement.NotWaterwayCrossingNode:
							elem3.m_Flags |= SubObjectFlags.NotWaterwayCrossing;
							value4.m_IntersectLayers |= Layer.Waterway;
							flag6 = true;
							break;
						case NetObjectPlacement.NotWaterwayCrossingEdgeMiddle:
							elem3.m_Flags |= SubObjectFlags.NotWaterwayCrossing | SubObjectFlags.EdgePlacement | SubObjectFlags.MiddlePlacement;
							value4.m_IntersectLayers |= Layer.Waterway;
							if (base.EntityManager.HasComponent<PillarData>(elem3.m_Prefab))
							{
								value4.m_Flags |= GeometryFlags.MiddlePillars;
							}
							if (netSubObjectInfo.m_Spacing != 0f)
							{
								elem3.m_Flags |= SubObjectFlags.EvenSpacing;
								elem3.m_Position.z = netSubObjectInfo.m_Spacing;
							}
							flag6 = true;
							break;
						case NetObjectPlacement.NotWaterwayCrossingEdgeEndsOrNode:
							elem3.m_Flags |= SubObjectFlags.NotWaterwayCrossing | SubObjectFlags.EdgePlacement | SubObjectFlags.AllowCombine;
							value4.m_IntersectLayers |= Layer.Waterway;
							flag6 = true;
							break;
						}
						if (netSubObjectInfo.m_AnchorTop)
						{
							elem3.m_Flags |= SubObjectFlags.AnchorTop;
						}
						if (netSubObjectInfo.m_AnchorCenter)
						{
							elem3.m_Flags |= SubObjectFlags.AnchorCenter;
						}
						if (netSubObjectInfo.m_RequireElevated)
						{
							elem3.m_Flags |= SubObjectFlags.RequireElevated;
						}
						if (netSubObjectInfo.m_RequireOutsideConnection)
						{
							elem3.m_Flags |= SubObjectFlags.RequireOutsideConnection;
						}
						if (netSubObjectInfo.m_RequireDeadEnd)
						{
							elem3.m_Flags |= SubObjectFlags.RequireDeadEnd;
						}
						if (netSubObjectInfo.m_RequireOrphan)
						{
							elem3.m_Flags |= SubObjectFlags.RequireOrphan;
						}
						if (CollectionUtils.TryGet(nativeArray6, num, out var value5) && base.EntityManager.HasComponent<LeisureProviderData>(elem3.m_Prefab))
						{
							value5.m_LeisureProvider = true;
							nativeArray6[num] = value5;
						}
						dynamicBuffer3.Add(elem3);
					}
					if (flag6)
					{
						nativeArray3.ElementAt(num).m_NodePriority += 500f;
					}
					if (nativeArray4.Length != 0)
					{
						nativeArray4[num] = value4;
					}
				}
				BufferAccessor<AuxiliaryNet> bufferAccessor5 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle13);
				for (int num3 = 0; num3 < bufferAccessor5.Length; num3++)
				{
					AuxiliaryNets component6 = m_PrefabSystem.GetPrefab<NetPrefab>(nativeArray2[num3]).GetComponent<AuxiliaryNets>();
					DynamicBuffer<AuxiliaryNet> dynamicBuffer4 = bufferAccessor5[num3];
					dynamicBuffer4.ResizeUninitialized(component6.m_AuxiliaryNets.Length);
					if (CollectionUtils.TryGet(nativeArray5, num3, out var value6))
					{
						if (component6.m_LinkEndOffsets)
						{
							value6.m_PlacementFlags |= PlacementFlags.LinkAuxOffsets;
						}
						nativeArray5[num3] = value6;
					}
					for (int num4 = 0; num4 < component6.m_AuxiliaryNets.Length; num4++)
					{
						AuxiliaryNetInfo auxiliaryNetInfo = component6.m_AuxiliaryNets[num4];
						dynamicBuffer4[num4] = new AuxiliaryNet
						{
							m_Prefab = m_PrefabSystem.GetEntity(auxiliaryNetInfo.m_Prefab),
							m_Position = auxiliaryNetInfo.m_Position,
							m_InvertMode = auxiliaryNetInfo.m_InvertWhen
						};
					}
				}
				BufferAccessor<NetSectionPiece> bufferAccessor6 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
				if (bufferAccessor6.Length != 0)
				{
					BufferAccessor<NetSubSection> bufferAccessor7 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
					for (int num5 = 0; num5 < bufferAccessor6.Length; num5++)
					{
						NetSectionPrefab prefab5 = m_PrefabSystem.GetPrefab<NetSectionPrefab>(nativeArray2[num5]);
						DynamicBuffer<NetSubSection> dynamicBuffer5 = bufferAccessor7[num5];
						DynamicBuffer<NetSectionPiece> dynamicBuffer6 = bufferAccessor6[num5];
						if (prefab5.m_SubSections != null)
						{
							for (int num6 = 0; num6 < prefab5.m_SubSections.Length; num6++)
							{
								NetSubSectionInfo netSubSectionInfo = prefab5.m_SubSections[num6];
								NetSubSection elem4 = new NetSubSection
								{
									m_SubSection = m_PrefabSystem.GetEntity(netSubSectionInfo.m_Section)
								};
								NetCompositionHelpers.GetRequirementFlags(netSubSectionInfo.m_RequireAll, out elem4.m_CompositionAll, out elem4.m_SectionAll);
								NetCompositionHelpers.GetRequirementFlags(netSubSectionInfo.m_RequireAny, out elem4.m_CompositionAny, out elem4.m_SectionAny);
								NetCompositionHelpers.GetRequirementFlags(netSubSectionInfo.m_RequireNone, out elem4.m_CompositionNone, out elem4.m_SectionNone);
								dynamicBuffer5.Add(elem4);
							}
						}
						if (prefab5.m_Pieces == null)
						{
							continue;
						}
						for (int num7 = 0; num7 < prefab5.m_Pieces.Length; num7++)
						{
							NetPieceInfo netPieceInfo = prefab5.m_Pieces[num7];
							NetSectionPiece elem5 = new NetSectionPiece
							{
								m_Piece = m_PrefabSystem.GetEntity(netPieceInfo.m_Piece)
							};
							NetCompositionHelpers.GetRequirementFlags(netPieceInfo.m_RequireAll, out elem5.m_CompositionAll, out elem5.m_SectionAll);
							NetCompositionHelpers.GetRequirementFlags(netPieceInfo.m_RequireAny, out elem5.m_CompositionAny, out elem5.m_SectionAny);
							NetCompositionHelpers.GetRequirementFlags(netPieceInfo.m_RequireNone, out elem5.m_CompositionNone, out elem5.m_SectionNone);
							switch (netPieceInfo.m_Piece.m_Layer)
							{
							case NetPieceLayer.Surface:
								elem5.m_Flags |= NetPieceFlags.Surface;
								break;
							case NetPieceLayer.Bottom:
								elem5.m_Flags |= NetPieceFlags.Bottom;
								break;
							case NetPieceLayer.Top:
								elem5.m_Flags |= NetPieceFlags.Top;
								break;
							case NetPieceLayer.Side:
								elem5.m_Flags |= NetPieceFlags.Side;
								break;
							}
							if (netPieceInfo.m_Piece.meshCount != 0)
							{
								elem5.m_Flags |= NetPieceFlags.HasMesh;
							}
							NetDividerPiece component7 = netPieceInfo.m_Piece.GetComponent<NetDividerPiece>();
							if (component7 != null)
							{
								if (component7.m_PreserveShape)
								{
									elem5.m_Flags |= NetPieceFlags.PreserveShape | NetPieceFlags.DisableTiling;
								}
								if (component7.m_BlockTraffic)
								{
									elem5.m_Flags |= NetPieceFlags.BlockTraffic;
								}
								if (component7.m_BlockCrosswalk)
								{
									elem5.m_Flags |= NetPieceFlags.BlockCrosswalk;
								}
							}
							NetPieceTiling component8 = netPieceInfo.m_Piece.GetComponent<NetPieceTiling>();
							if (component8 != null && component8.m_DisableTextureTiling)
							{
								elem5.m_Flags |= NetPieceFlags.DisableTiling;
							}
							MovePieceVertices component9 = netPieceInfo.m_Piece.GetComponent<MovePieceVertices>();
							if (component9 != null)
							{
								if (component9.m_LowerBottomToTerrain)
								{
									elem5.m_Flags |= NetPieceFlags.LowerBottomToTerrain;
								}
								if (component9.m_RaiseTopToTerrain)
								{
									elem5.m_Flags |= NetPieceFlags.RaiseTopToTerrain;
								}
								if (component9.m_SmoothTopNormal)
								{
									elem5.m_Flags |= NetPieceFlags.SmoothTopNormal;
								}
							}
							AsymmetricPieceMesh component10 = netPieceInfo.m_Piece.GetComponent<AsymmetricPieceMesh>();
							if (component10 != null)
							{
								if (component10.m_Sideways)
								{
									elem5.m_Flags |= NetPieceFlags.AsymmetricMeshX;
								}
								if (component10.m_Lengthwise)
								{
									elem5.m_Flags |= NetPieceFlags.AsymmetricMeshZ;
								}
							}
							elem5.m_Offset = netPieceInfo.m_Offset;
							dynamicBuffer6.Add(elem5);
						}
					}
				}
				NativeArray<NetPieceData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle4);
				if (nativeArray7.Length != 0)
				{
					BufferAccessor<NetPieceLane> bufferAccessor8 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle3);
					BufferAccessor<NetPieceArea> bufferAccessor9 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle4);
					BufferAccessor<NetPieceObject> bufferAccessor10 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle5);
					NativeArray<NetCrosswalkData> nativeArray8 = archetypeChunk.GetNativeArray(ref typeHandle17);
					NativeArray<NetTerrainData> nativeArray9 = archetypeChunk.GetNativeArray(ref typeHandle32);
					for (int num8 = 0; num8 < nativeArray7.Length; num8++)
					{
						NetPiecePrefab prefab6 = m_PrefabSystem.GetPrefab<NetPiecePrefab>(nativeArray2[num8]);
						NetPieceData value7 = nativeArray7[num8];
						value7.m_HeightRange = prefab6.m_HeightRange;
						value7.m_SurfaceHeights = prefab6.m_SurfaceHeights;
						value7.m_Width = prefab6.m_Width;
						value7.m_Length = prefab6.m_Length;
						value7.m_WidthOffset = prefab6.m_WidthOffset;
						value7.m_NodeOffset = prefab6.m_NodeOffset;
						value7.m_SideConnectionOffset = prefab6.m_SideConnectionOffset;
						if (bufferAccessor8.Length != 0)
						{
							NetPieceLanes component11 = prefab6.GetComponent<NetPieceLanes>();
							if (component11.m_Lanes != null)
							{
								DynamicBuffer<NetPieceLane> dynamicBuffer7 = bufferAccessor8[num8];
								for (int num9 = 0; num9 < component11.m_Lanes.Length; num9++)
								{
									NetLaneInfo netLaneInfo = component11.m_Lanes[num9];
									NetPieceLane elem6 = new NetPieceLane
									{
										m_Lane = m_PrefabSystem.GetEntity(netLaneInfo.m_Lane),
										m_Position = netLaneInfo.m_Position
									};
									if (netLaneInfo.m_FindAnchor)
									{
										elem6.m_ExtraFlags |= LaneFlags.FindAnchor;
									}
									dynamicBuffer7.Add(elem6);
								}
								if (dynamicBuffer7.Length > 1)
								{
									dynamicBuffer7.AsNativeArray().Sort();
								}
							}
						}
						if (bufferAccessor9.Length != 0)
						{
							DynamicBuffer<NetPieceArea> dynamicBuffer8 = bufferAccessor9[num8];
							BuildableNetPiece component12 = prefab6.GetComponent<BuildableNetPiece>();
							if (component12 != null)
							{
								dynamicBuffer8.Add(new NetPieceArea
								{
									m_Flags = (component12.m_AllowOnBridge ? NetAreaFlags.Buildable : (NetAreaFlags.Buildable | NetAreaFlags.NoBridge)),
									m_Position = component12.m_Position,
									m_Width = component12.m_Width,
									m_SnapPosition = component12.m_SnapPosition,
									m_SnapWidth = component12.m_SnapWidth
								});
							}
							if (dynamicBuffer8.Length > 1)
							{
								dynamicBuffer8.AsNativeArray().Sort();
							}
						}
						if (bufferAccessor10.Length != 0)
						{
							DynamicBuffer<NetPieceObject> dynamicBuffer9 = bufferAccessor10[num8];
							NetPieceObjects component13 = prefab6.GetComponent<NetPieceObjects>();
							if (component13 != null)
							{
								dynamicBuffer9.ResizeUninitialized(component13.m_PieceObjects.Length);
								for (int num10 = 0; num10 < component13.m_PieceObjects.Length; num10++)
								{
									NetPieceObjectInfo netPieceObjectInfo = component13.m_PieceObjects[num10];
									NetPieceObject value8 = new NetPieceObject
									{
										m_Prefab = m_PrefabSystem.GetEntity(netPieceObjectInfo.m_Object),
										m_Position = netPieceObjectInfo.m_Position,
										m_Offset = netPieceObjectInfo.m_Offset,
										m_Spacing = netPieceObjectInfo.m_Spacing,
										m_UseCurveRotation = netPieceObjectInfo.m_UseCurveRotation,
										m_MinLength = netPieceObjectInfo.m_MinLength,
										m_Probability = math.select(netPieceObjectInfo.m_Probability, 100, netPieceObjectInfo.m_Probability == 0),
										m_CurveOffsetRange = netPieceObjectInfo.m_CurveOffsetRange,
										m_Rotation = netPieceObjectInfo.m_Rotation
									};
									NetCompositionHelpers.GetRequirementFlags(netPieceObjectInfo.m_RequireAll, out value8.m_CompositionAll, out value8.m_SectionAll);
									NetCompositionHelpers.GetRequirementFlags(netPieceObjectInfo.m_RequireAny, out value8.m_CompositionAny, out value8.m_SectionAny);
									NetCompositionHelpers.GetRequirementFlags(netPieceObjectInfo.m_RequireNone, out value8.m_CompositionNone, out value8.m_SectionNone);
									if (netPieceObjectInfo.m_FlipWhenInverted)
									{
										value8.m_Flags |= SubObjectFlags.FlipInverted;
									}
									if (netPieceObjectInfo.m_EvenSpacing)
									{
										value8.m_Flags |= SubObjectFlags.EvenSpacing;
									}
									if (netPieceObjectInfo.m_SpacingOverride)
									{
										value8.m_Flags |= SubObjectFlags.SpacingOverride;
									}
									dynamicBuffer9[num10] = value8;
								}
							}
						}
						if (nativeArray8.Length != 0)
						{
							NetPieceCrosswalk component14 = prefab6.GetComponent<NetPieceCrosswalk>();
							nativeArray8[num8] = new NetCrosswalkData
							{
								m_Lane = m_PrefabSystem.GetEntity(component14.m_Lane),
								m_Start = component14.m_Start,
								m_End = component14.m_End
							};
						}
						if (nativeArray9.Length != 0)
						{
							NetTerrainPiece component15 = prefab6.GetComponent<NetTerrainPiece>();
							nativeArray9[num8] = new NetTerrainData
							{
								m_WidthOffset = component15.m_WidthOffset,
								m_ClipHeightOffset = component15.m_ClipHeightOffset,
								m_MinHeightOffset = component15.m_MinHeightOffset,
								m_MaxHeightOffset = component15.m_MaxHeightOffset
							};
						}
						nativeArray7[num8] = value7;
					}
				}
				NativeArray<NetLaneData> nativeArray10 = archetypeChunk.GetNativeArray(ref typeHandle9);
				if (nativeArray10.Length != 0)
				{
					NativeArray<ParkingLaneData> nativeArray11 = archetypeChunk.GetNativeArray(ref typeHandle14);
					NativeArray<CarLaneData> nativeArray12 = archetypeChunk.GetNativeArray(ref typeHandle11);
					NativeArray<TrackLaneData> nativeArray13 = archetypeChunk.GetNativeArray(ref typeHandle12);
					NativeArray<PedestrianLaneData> nativeArray14 = archetypeChunk.GetNativeArray(ref typeHandle15);
					NativeArray<UtilityLaneData> nativeArray15 = archetypeChunk.GetNativeArray(ref typeHandle13);
					NativeArray<SecondaryLaneData> nativeArray16 = archetypeChunk.GetNativeArray(ref typeHandle16);
					BufferAccessor<AuxiliaryNetLane> bufferAccessor11 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle12);
					for (int num11 = 0; num11 < nativeArray10.Length; num11++)
					{
						NetLanePrefab prefab7 = m_PrefabSystem.GetPrefab<NetLanePrefab>(nativeArray2[num11]);
						NetLaneData value9 = nativeArray10[num11];
						if (prefab7.m_PathfindPrefab != null)
						{
							value9.m_PathfindPrefab = m_PrefabSystem.GetEntity(prefab7.m_PathfindPrefab);
							if (prefab7.m_PathfindPrefab.m_TrackTrafficFlow)
							{
								value9.m_Flags |= LaneFlags.TrackFlow;
							}
						}
						if (nativeArray12.Length != 0)
						{
							CarLane component16 = prefab7.GetComponent<CarLane>();
							value9.m_Flags |= LaneFlags.Road;
							value9.m_Width = component16.m_Width;
							if (component16.m_StartingLane)
							{
								value9.m_Flags |= LaneFlags.DisconnectedStart;
							}
							if (component16.m_EndingLane)
							{
								value9.m_Flags |= LaneFlags.DisconnectedEnd;
							}
							if (component16.m_Twoway)
							{
								value9.m_Flags |= LaneFlags.Twoway;
							}
							if (component16.m_BusLane)
							{
								value9.m_Flags |= LaneFlags.PublicOnly;
							}
							if (component16.m_RoadType == RoadTypes.Watercraft)
							{
								value9.m_Flags |= LaneFlags.OnWater;
							}
							if (component16.m_RoadType == RoadTypes.Bicycle)
							{
								value9.m_Flags |= LaneFlags.BicyclesOnly;
							}
							CarLaneData value10 = nativeArray12[num11];
							if (component16.m_NotTrackLane != null)
							{
								value10.m_NotTrackLanePrefab = m_PrefabSystem.GetEntity(component16.m_NotTrackLane);
							}
							if (component16.m_NotBusLane != null)
							{
								value10.m_NotBusLanePrefab = m_PrefabSystem.GetEntity(component16.m_NotBusLane);
							}
							value10.m_RoadTypes = component16.m_RoadType;
							value10.m_MaxSize = component16.m_MaxSize;
							nativeArray12[num11] = value10;
						}
						if (nativeArray13.Length != 0)
						{
							TrackLane component17 = prefab7.GetComponent<TrackLane>();
							value9.m_Flags |= LaneFlags.Track;
							value9.m_Width = component17.m_Width;
							if (component17.m_Twoway)
							{
								value9.m_Flags |= LaneFlags.Twoway;
							}
							TrackLaneData value11 = nativeArray13[num11];
							if (component17.m_FallbackLane != null)
							{
								value11.m_FallbackPrefab = m_PrefabSystem.GetEntity(component17.m_FallbackLane);
							}
							if (component17.m_EndObject != null)
							{
								value11.m_EndObjectPrefab = m_PrefabSystem.GetEntity(component17.m_EndObject);
							}
							value11.m_TrackTypes = component17.m_TrackType;
							value11.m_MaxCurviness = math.radians(component17.m_MaxCurviness);
							nativeArray13[num11] = value11;
						}
						if (nativeArray15.Length != 0)
						{
							UtilityLane component18 = prefab7.GetComponent<UtilityLane>();
							value9.m_Flags |= LaneFlags.Utility;
							value9.m_Width = component18.m_Width;
							if (component18.m_Underground)
							{
								value9.m_Flags |= LaneFlags.Underground;
							}
							UtilityLaneData value12 = nativeArray15[num11];
							if (component18.m_LocalConnectionLane != null)
							{
								value12.m_LocalConnectionPrefab = m_PrefabSystem.GetEntity(component18.m_LocalConnectionLane);
							}
							if (component18.m_LocalConnectionLane2 != null)
							{
								value12.m_LocalConnectionPrefab2 = m_PrefabSystem.GetEntity(component18.m_LocalConnectionLane2);
							}
							if (component18.m_NodeObject != null)
							{
								value12.m_NodeObjectPrefab = m_PrefabSystem.GetEntity(component18.m_NodeObject);
							}
							value12.m_VisualCapacity = component18.m_VisualCapacity;
							value12.m_Hanging = component18.m_Hanging;
							value12.m_UtilityTypes = component18.m_UtilityType;
							nativeArray15[num11] = value12;
						}
						if (nativeArray11.Length != 0)
						{
							ParkingLane component19 = prefab7.GetComponent<ParkingLane>();
							value9.m_Flags |= LaneFlags.Parking;
							ParkingLaneData value13 = nativeArray11[num11];
							value13.m_RoadTypes = component19.m_RoadType;
							value13.m_SlotSize = math.select(component19.m_SlotSize, 0f, component19.m_SlotSize < 0.001f);
							value13.m_SlotAngle = math.radians(math.clamp(component19.m_SlotAngle, 0f, 90f));
							value13.m_MaxCarLength = math.select(0f, value13.m_SlotSize.y + 0.4f, value13.m_SlotSize.y != 0f);
							float2 y = new float2(math.cos(value13.m_SlotAngle), math.sin(value13.m_SlotAngle));
							if (y.y < 0.001f)
							{
								value13.m_SlotInterval = value13.m_SlotSize.y;
							}
							else if (y.x < 0.001f)
							{
								value13.m_SlotInterval = value13.m_SlotSize.x;
								value9.m_Flags |= LaneFlags.Twoway;
							}
							else
							{
								float2 @float = value13.m_SlotSize / y.yx;
								@float = math.select(@float, 0f, @float < 0.001f);
								if (@float.x < @float.y)
								{
									value13.m_SlotInterval = @float.x;
								}
								else
								{
									value13.m_SlotInterval = @float.y;
									value13.m_MaxCarLength = math.max(0f, value13.m_SlotSize.y - 1f);
								}
							}
							value9.m_Width = math.dot(value13.m_SlotSize, y);
							value9.m_Width = math.select(value9.m_Width, value13.m_SlotSize.y, value13.m_SlotSize.y != 0f && value13.m_SlotSize.y < value9.m_Width);
							if (value13.m_SlotSize.x == 0f)
							{
								value9.m_Flags |= LaneFlags.Virtual;
							}
							if (component19.m_SpecialVehicles)
							{
								value9.m_Flags |= LaneFlags.PublicOnly;
							}
							nativeArray11[num11] = value13;
						}
						if (nativeArray14.Length != 0)
						{
							PedestrianLane component20 = prefab7.GetComponent<PedestrianLane>();
							value9.m_Flags |= LaneFlags.Pedestrian | LaneFlags.Twoway;
							value9.m_Width = component20.m_Width;
							if (component20.m_OnWater)
							{
								value9.m_Flags |= LaneFlags.OnWater;
							}
							PedestrianLaneData value14 = nativeArray14[num11];
							if (component20.m_NotWalkLane != null)
							{
								value14.m_NotWalkLanePrefab = m_PrefabSystem.GetEntity(component20.m_NotWalkLane);
							}
							if (component20.m_Activities != null && component20.m_Activities.Length != 0)
							{
								value14.m_ActivityMask = default(ActivityMask);
								for (int num12 = 0; num12 < component20.m_Activities.Length; num12++)
								{
									value14.m_ActivityMask.m_Mask |= new ActivityMask(component20.m_Activities[num12]).m_Mask;
								}
							}
							else
							{
								value14.m_ActivityMask = new ActivityMask(ActivityType.Standing);
							}
							nativeArray14[num11] = value14;
						}
						if (nativeArray16.Length != 0)
						{
							Entity entity = nativeArray[num11];
							SecondaryLane component21 = prefab7.GetComponent<SecondaryLane>();
							value9.m_Flags |= LaneFlags.Secondary;
							bool flag7 = component21.m_LeftLanes != null && component21.m_LeftLanes.Length != 0;
							bool flag8 = component21.m_RightLanes != null && component21.m_RightLanes.Length != 0;
							bool flag9 = component21.m_CrossingLanes != null && component21.m_CrossingLanes.Length != 0;
							SecondaryLaneData value15 = nativeArray16[num11];
							if (component21.m_SkipSafePedestrianOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipSafePedestrianOverlap;
							}
							if (component21.m_SkipSafeCarOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipSafeCarOverlap;
							}
							if (component21.m_SkipUnsafeCarOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipUnsafeCarOverlap;
							}
							if (component21.m_SkipSideCarOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipSideCarOverlap;
							}
							if (component21.m_SkipTrackOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipTrackOverlap;
							}
							if (component21.m_SkipMergeOverlap)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.SkipMergeOverlap;
							}
							if (component21.m_FitToParkingSpaces)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.FitToParkingSpaces;
							}
							if (component21.m_EvenSpacing)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.EvenSpacing;
							}
							if (component21.m_InvertOverlapCuts)
							{
								value15.m_Flags |= SecondaryLaneDataFlags.InvertOverlapCuts;
							}
							value15.m_PositionOffset = component21.m_PositionOffset;
							value15.m_LengthOffset = component21.m_LengthOffset;
							value15.m_CutMargin = component21.m_CutMargin;
							value15.m_CutOffset = component21.m_CutOffset;
							value15.m_CutOverlap = component21.m_CutOverlap;
							value15.m_Spacing = component21.m_Spacing;
							SecondaryNetLaneFlags secondaryNetLaneFlags = (SecondaryNetLaneFlags)0;
							if (component21.m_CanFlipSides)
							{
								secondaryNetLaneFlags |= SecondaryNetLaneFlags.CanFlipSides;
							}
							if (component21.m_DuplicateSides)
							{
								secondaryNetLaneFlags |= SecondaryNetLaneFlags.DuplicateSides;
							}
							if (component21.m_RequireParallel)
							{
								secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireParallel;
							}
							if (component21.m_RequireOpposite)
							{
								secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireOpposite;
							}
							if (flag7)
							{
								SecondaryNetLaneFlags secondaryNetLaneFlags2 = secondaryNetLaneFlags | SecondaryNetLaneFlags.Left;
								if (!flag8)
								{
									secondaryNetLaneFlags2 |= SecondaryNetLaneFlags.OneSided;
								}
								for (int num13 = 0; num13 < component21.m_LeftLanes.Length; num13++)
								{
									SecondaryLaneInfo secondaryLaneInfo = component21.m_LeftLanes[num13];
									SecondaryNetLaneFlags flags = secondaryNetLaneFlags2 | secondaryLaneInfo.GetFlags();
									Entity entity2 = m_PrefabSystem.GetEntity(secondaryLaneInfo.m_Lane);
									base.EntityManager.GetBuffer<SecondaryNetLane>(entity2).Add(new SecondaryNetLane
									{
										m_Lane = entity,
										m_Flags = flags
									});
								}
							}
							if (flag8)
							{
								SecondaryNetLaneFlags secondaryNetLaneFlags3 = secondaryNetLaneFlags | SecondaryNetLaneFlags.Right;
								if (!flag7)
								{
									secondaryNetLaneFlags3 |= SecondaryNetLaneFlags.OneSided;
								}
								for (int num14 = 0; num14 < component21.m_RightLanes.Length; num14++)
								{
									SecondaryLaneInfo secondaryLaneInfo2 = component21.m_RightLanes[num14];
									SecondaryNetLaneFlags secondaryNetLaneFlags4 = secondaryNetLaneFlags3 | secondaryLaneInfo2.GetFlags();
									Entity entity3 = m_PrefabSystem.GetEntity(secondaryLaneInfo2.m_Lane);
									DynamicBuffer<SecondaryNetLane> buffer = base.EntityManager.GetBuffer<SecondaryNetLane>(entity3);
									int num15 = 0;
									while (true)
									{
										if (num15 < buffer.Length)
										{
											SecondaryNetLane value16 = buffer[num15];
											if (value16.m_Lane == entity && ((value16.m_Flags ^ secondaryNetLaneFlags4) & ~(SecondaryNetLaneFlags.Left | SecondaryNetLaneFlags.Right)) == 0)
											{
												value16.m_Flags |= secondaryNetLaneFlags4;
												buffer[num15] = value16;
												break;
											}
											num15++;
											continue;
										}
										buffer.Add(new SecondaryNetLane
										{
											m_Lane = entity,
											m_Flags = secondaryNetLaneFlags4
										});
										break;
									}
								}
							}
							if (flag9)
							{
								SecondaryNetLaneFlags secondaryNetLaneFlags5 = SecondaryNetLaneFlags.Crossing;
								for (int num16 = 0; num16 < component21.m_CrossingLanes.Length; num16++)
								{
									SecondaryLaneInfo2 secondaryLaneInfo3 = component21.m_CrossingLanes[num16];
									SecondaryNetLaneFlags flags2 = secondaryNetLaneFlags5 | secondaryLaneInfo3.GetFlags();
									Entity entity4 = m_PrefabSystem.GetEntity(secondaryLaneInfo3.m_Lane);
									base.EntityManager.GetBuffer<SecondaryNetLane>(entity4).Add(new SecondaryNetLane
									{
										m_Lane = entity,
										m_Flags = flags2
									});
								}
							}
							nativeArray16[num11] = value15;
						}
						if (bufferAccessor11.Length != 0)
						{
							DynamicBuffer<AuxiliaryNetLane> dynamicBuffer10 = bufferAccessor11[num11];
							AuxiliaryLanes component22 = prefab7.GetComponent<AuxiliaryLanes>();
							if (component22 != null)
							{
								dynamicBuffer10.ResizeUninitialized(component22.m_AuxiliaryLanes.Length);
								for (int num17 = 0; num17 < component22.m_AuxiliaryLanes.Length; num17++)
								{
									AuxiliaryLaneInfo auxiliaryLaneInfo = component22.m_AuxiliaryLanes[num17];
									AuxiliaryNetLane value17 = new AuxiliaryNetLane
									{
										m_Prefab = m_PrefabSystem.GetEntity(auxiliaryLaneInfo.m_Lane),
										m_Position = auxiliaryLaneInfo.m_Position,
										m_Spacing = auxiliaryLaneInfo.m_Spacing
									};
									if (auxiliaryLaneInfo.m_EvenSpacing)
									{
										value17.m_Flags |= LaneFlags.EvenSpacing;
									}
									if (auxiliaryLaneInfo.m_FindAnchor)
									{
										value17.m_Flags |= LaneFlags.FindAnchor;
									}
									NetCompositionHelpers.GetRequirementFlags(auxiliaryLaneInfo.m_RequireAll, out value17.m_CompositionAll, out var sectionFlags11);
									NetCompositionHelpers.GetRequirementFlags(auxiliaryLaneInfo.m_RequireAny, out value17.m_CompositionAny, out var sectionFlags12);
									NetCompositionHelpers.GetRequirementFlags(auxiliaryLaneInfo.m_RequireNone, out value17.m_CompositionNone, out var sectionFlags13);
									NetSectionFlags netSectionFlags4 = sectionFlags11 | sectionFlags12 | sectionFlags13;
									if (netSectionFlags4 != 0)
									{
										COSystemBase.baseLog.ErrorFormat(prefab7, "Auxiliary net lane ({0}: {1}) cannot require section flags: {2}", prefab7.name, auxiliaryLaneInfo.m_Lane.name, netSectionFlags4);
									}
									dynamicBuffer10[num17] = value17;
									value9.m_Flags |= LaneFlags.HasAuxiliary;
								}
							}
						}
						nativeArray10[num11] = value9;
					}
					NativeArray<NetLaneGeometryData> nativeArray17 = archetypeChunk.GetNativeArray(ref typeHandle10);
					BufferAccessor<SubMesh> bufferAccessor12 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle10);
					for (int num18 = 0; num18 < nativeArray17.Length; num18++)
					{
						NetLaneGeometryPrefab prefab8 = m_PrefabSystem.GetPrefab<NetLaneGeometryPrefab>(nativeArray2[num18]);
						NetLaneData value18 = nativeArray10[num18];
						NetLaneGeometryData value19 = nativeArray17[num18];
						DynamicBuffer<SubMesh> dynamicBuffer11 = bufferAccessor12[num18];
						value19.m_MinLod = 255;
						value19.m_GameLayers = (MeshLayer)0;
						value19.m_EditorLayers = (MeshLayer)0;
						if (prefab8.m_Meshes != null)
						{
							for (int num19 = 0; num19 < prefab8.m_Meshes.Length; num19++)
							{
								NetLaneMeshInfo obj2 = prefab8.m_Meshes[num19];
								RenderPrefab mesh = obj2.m_Mesh;
								Entity entity5 = m_PrefabSystem.GetEntity(mesh);
								MeshData componentData = base.EntityManager.GetComponentData<MeshData>(entity5);
								float3 y2 = MathUtils.Size(mesh.bounds);
								componentData.m_MinLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(y2.xy), componentData.m_LodBias);
								componentData.m_ShadowLod = (byte)RenderingUtils.CalculateLodLimit(RenderingUtils.GetShadowRenderingSize(y2.xy), componentData.m_ShadowBias);
								value19.m_Size = math.max(value19.m_Size, y2);
								value19.m_MinLod = math.min(value19.m_MinLod, componentData.m_MinLod);
								SubMeshFlags subMeshFlags = (SubMeshFlags)0u;
								if (obj2.m_RequireSafe)
								{
									subMeshFlags |= SubMeshFlags.RequireSafe;
								}
								if (obj2.m_RequireLevelCrossing)
								{
									subMeshFlags |= SubMeshFlags.RequireLevelCrossing;
								}
								if (obj2.m_RequireEditor)
								{
									subMeshFlags |= SubMeshFlags.RequireEditor;
								}
								if (obj2.m_RequireTrackCrossing)
								{
									subMeshFlags |= SubMeshFlags.RequireTrack;
								}
								if (obj2.m_RequireClear)
								{
									subMeshFlags |= SubMeshFlags.RequireClear;
								}
								if (obj2.m_RequireLeftHandTraffic)
								{
									subMeshFlags |= SubMeshFlags.RequireLeftHandTraffic;
								}
								if (obj2.m_RequireRightHandTraffic)
								{
									subMeshFlags |= SubMeshFlags.RequireRightHandTraffic;
								}
								dynamicBuffer11.Add(new SubMesh(entity5, subMeshFlags, (ushort)num19));
								MeshLayer meshLayer = ((componentData.m_DefaultLayers == (MeshLayer)0) ? MeshLayer.Default : componentData.m_DefaultLayers);
								if (!obj2.m_RequireEditor)
								{
									value19.m_GameLayers |= meshLayer;
								}
								value19.m_EditorLayers |= meshLayer;
								base.EntityManager.SetComponentData(entity5, componentData);
								if (mesh.Has<ColorProperties>())
								{
									value18.m_Flags |= LaneFlags.PseudoRandom;
								}
							}
						}
						nativeArray10[num18] = value18;
						nativeArray17[num18] = value19;
					}
					NativeArray<SpawnableObjectData> nativeArray18 = archetypeChunk.GetNativeArray(ref typeHandle31);
					if (nativeArray18.Length != 0)
					{
						for (int num20 = 0; num20 < nativeArray18.Length; num20++)
						{
							Entity obj3 = nativeArray[num20];
							SpawnableObjectData value20 = nativeArray18[num20];
							SpawnableLane component23 = m_PrefabSystem.GetPrefab<NetLanePrefab>(nativeArray2[num20]).GetComponent<SpawnableLane>();
							for (int num21 = 0; num21 < component23.m_Placeholders.Length; num21++)
							{
								NetLanePrefab prefab9 = component23.m_Placeholders[num21];
								Entity entity6 = m_PrefabSystem.GetEntity(prefab9);
								base.EntityManager.GetBuffer<PlaceholderObjectElement>(entity6).Add(new PlaceholderObjectElement(obj3));
							}
							if (component23.m_RandomizationGroup != null)
							{
								value20.m_RandomizationGroup = m_PrefabSystem.GetEntity(component23.m_RandomizationGroup);
							}
							value20.m_Probability = component23.m_Probability;
							nativeArray18[num20] = value20;
						}
					}
				}
				NativeArray<RoadData> nativeArray19 = archetypeChunk.GetNativeArray(ref typeHandle18);
				if (nativeArray19.Length != 0)
				{
					for (int num22 = 0; num22 < nativeArray19.Length; num22++)
					{
						RoadPrefab prefab10 = m_PrefabSystem.GetPrefab<RoadPrefab>(nativeArray2[num22]);
						NetData value21 = nativeArray3[num22];
						NetGeometryData value22 = nativeArray4[num22];
						RoadData value23 = nativeArray19[num22];
						switch (prefab10.m_RoadType)
						{
						case RoadType.Normal:
							value21.m_RequiredLayers |= Layer.Road;
							break;
						case RoadType.PublicTransport:
							value21.m_RequiredLayers |= Layer.PublicTransportRoad;
							break;
						}
						value21.m_ConnectLayers |= Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.Fence | Layer.PublicTransportRoad;
						value21.m_ConnectLayers |= value22.m_IntersectLayers & Layer.Waterway;
						value21.m_LocalConnectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value21.m_NodePriority += 2000f;
						value22.m_MergeLayers |= Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad;
						value22.m_IntersectLayers |= Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.PublicTransportRoad;
						value22.m_Flags |= GeometryFlags.SupportRoundabout | GeometryFlags.BlockZone | GeometryFlags.Directional | GeometryFlags.FlattenTerrain | GeometryFlags.ClipTerrain;
						value23.m_SpeedLimit = prefab10.m_SpeedLimit / 3.6f;
						if (prefab10.m_ZoneBlock != null)
						{
							value22.m_Flags |= GeometryFlags.SnapCellSize;
							value23.m_ZoneBlockPrefab = m_PrefabSystem.GetEntity(prefab10.m_ZoneBlock);
							value23.m_Flags |= RoadFlags.EnableZoning;
						}
						if (prefab10.m_TrafficLights)
						{
							value23.m_Flags |= RoadFlags.PreferTrafficLights;
						}
						if (prefab10.m_HighwayRules)
						{
							value23.m_Flags |= RoadFlags.UseHighwayRules;
							value22.m_MinNodeOffset = math.max(value22.m_MinNodeOffset, 2f);
							value22.m_Flags |= GeometryFlags.SmoothElevation;
						}
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value24 = nativeArray5[num22];
							value24.m_PlacementFlags |= PlacementFlags.OnGround;
							nativeArray5[num22] = value24;
						}
						nativeArray3[num22] = value21;
						nativeArray4[num22] = value22;
						nativeArray19[num22] = value23;
					}
				}
				NativeArray<TrackData> nativeArray20 = archetypeChunk.GetNativeArray(ref typeHandle19);
				if (nativeArray20.Length != 0)
				{
					for (int num23 = 0; num23 < nativeArray20.Length; num23++)
					{
						TrackPrefab prefab11 = m_PrefabSystem.GetPrefab<TrackPrefab>(nativeArray2[num23]);
						NetData value25 = nativeArray3[num23];
						NetGeometryData value26 = nativeArray4[num23];
						TrackData value27 = nativeArray20[num23];
						Layer layer;
						Layer layer2;
						float num24;
						float y3;
						switch (prefab11.m_TrackType)
						{
						case TrackTypes.Train:
							layer = Layer.TrainTrack;
							layer2 = Layer.TrainTrack | Layer.Pathway;
							num24 = 200f;
							y3 = 10f;
							value26.m_Flags |= GeometryFlags.SmoothElevation;
							break;
						case TrackTypes.Tram:
							layer = Layer.TramTrack;
							layer2 = Layer.TramTrack;
							num24 = 0f;
							y3 = 8f;
							value26.m_Flags |= GeometryFlags.SupportRoundabout;
							break;
						case TrackTypes.Subway:
							layer = Layer.SubwayTrack;
							layer2 = Layer.SubwayTrack;
							num24 = 200f;
							y3 = 9f;
							value26.m_Flags |= GeometryFlags.SmoothElevation;
							break;
						default:
							layer = Layer.None;
							layer2 = Layer.None;
							num24 = 0f;
							y3 = 0f;
							break;
						}
						value25.m_RequiredLayers |= layer;
						value25.m_ConnectLayers |= layer2;
						value25.m_ConnectLayers |= value26.m_IntersectLayers & Layer.Waterway;
						value25.m_LocalConnectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value26.m_MergeLayers |= layer;
						value26.m_IntersectLayers |= layer2;
						value26.m_EdgeLengthRange.max = math.max(value26.m_EdgeLengthRange.max, num24 * 1.5f);
						value26.m_MinNodeOffset = math.max(value26.m_MinNodeOffset, y3);
						value26.m_Flags |= GeometryFlags.BlockZone | GeometryFlags.Directional | GeometryFlags.FlattenTerrain | GeometryFlags.ClipTerrain;
						value27.m_TrackType = prefab11.m_TrackType;
						value27.m_SpeedLimit = prefab11.m_SpeedLimit / 3.6f;
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value28 = nativeArray5[num23];
							value28.m_PlacementFlags |= PlacementFlags.OnGround;
							nativeArray5[num23] = value28;
						}
						nativeArray3[num23] = value25;
						nativeArray4[num23] = value26;
						nativeArray20[num23] = value27;
					}
				}
				NativeArray<WaterwayData> nativeArray21 = archetypeChunk.GetNativeArray(ref typeHandle20);
				if (nativeArray21.Length != 0)
				{
					for (int num25 = 0; num25 < nativeArray21.Length; num25++)
					{
						WaterwayPrefab prefab12 = m_PrefabSystem.GetPrefab<WaterwayPrefab>(nativeArray2[num25]);
						NetData value29 = nativeArray3[num25];
						NetGeometryData value30 = nativeArray4[num25];
						WaterwayData value31 = nativeArray21[num25];
						value29.m_RequiredLayers |= Layer.Waterway;
						value29.m_ConnectLayers |= Layer.Waterway;
						value29.m_LocalConnectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value30.m_MergeLayers |= Layer.Waterway;
						value30.m_IntersectLayers |= Layer.Waterway;
						value30.m_EdgeLengthRange.max = 1000f;
						value30.m_ElevatedLength = 1000f;
						value30.m_Flags |= GeometryFlags.BlockZone | GeometryFlags.Directional | GeometryFlags.FlattenTerrain | GeometryFlags.OnWater;
						value31.m_SpeedLimit = prefab12.m_SpeedLimit / 3.6f;
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value32 = nativeArray5[num25];
							value32.m_PlacementFlags |= PlacementFlags.Floating;
							value32.m_SnapDistance = 16f;
							nativeArray5[num25] = value32;
						}
						nativeArray3[num25] = value29;
						nativeArray4[num25] = value30;
						nativeArray21[num25] = value31;
					}
				}
				if (nativeArray6.Length != 0)
				{
					NativeArray<LocalConnectData> nativeArray22 = archetypeChunk.GetNativeArray(ref typeHandle8);
					for (int num26 = 0; num26 < nativeArray6.Length; num26++)
					{
						PathwayPrefab prefab13 = m_PrefabSystem.GetPrefab<PathwayPrefab>(nativeArray2[num26]);
						NetData value33 = nativeArray3[num26];
						NetGeometryData value34 = nativeArray4[num26];
						LocalConnectData value35 = nativeArray22[num26];
						PathwayData value36 = nativeArray6[num26];
						Layer layer3 = (flag2 ? Layer.MarkerPathway : Layer.Pathway);
						value33.m_RequiredLayers |= layer3;
						value33.m_ConnectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value33.m_ConnectLayers |= value34.m_IntersectLayers & Layer.Waterway;
						value33.m_LocalConnectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value34.m_MergeLayers |= layer3;
						value34.m_IntersectLayers |= Layer.Pathway | Layer.MarkerPathway;
						value34.m_ElevationLimit = 2f;
						value34.m_Flags |= GeometryFlags.Directional;
						if (flag2)
						{
							value34.m_ElevatedLength = value34.m_EdgeLengthRange.max;
							value34.m_Flags |= GeometryFlags.LoweredIsTunnel | GeometryFlags.RaisedIsElevated;
						}
						else
						{
							value34.m_ElevatedLength = 40f;
							value34.m_Flags |= GeometryFlags.BlockZone | GeometryFlags.FlattenTerrain | GeometryFlags.ClipTerrain;
						}
						value35.m_Flags |= LocalConnectFlags.KeepOpen | LocalConnectFlags.RequireDeadend | LocalConnectFlags.ChooseBest | LocalConnectFlags.ChooseSides;
						value35.m_Layers |= Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.Waterway | Layer.TramTrack | Layer.SubwayTrack | Layer.MarkerPathway | Layer.PublicTransportRoad;
						value35.m_HeightRange = new Bounds1(-8f, 8f);
						value35.m_SearchDistance = 4f;
						value36.m_SpeedLimit = prefab13.m_SpeedLimit / 3.6f;
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value37 = nativeArray5[num26];
							value37.m_PlacementFlags |= PlacementFlags.OnGround;
							value37.m_SnapDistance = (flag2 ? 2f : 4f);
							value37.m_MinWaterElevation = 2.5f;
							nativeArray5[num26] = value37;
						}
						nativeArray3[num26] = value33;
						nativeArray4[num26] = value34;
						nativeArray22[num26] = value35;
						nativeArray6[num26] = value36;
					}
				}
				NativeArray<TaxiwayData> nativeArray23 = archetypeChunk.GetNativeArray(ref typeHandle22);
				if (nativeArray23.Length != 0)
				{
					for (int num27 = 0; num27 < nativeArray23.Length; num27++)
					{
						TaxiwayPrefab prefab14 = m_PrefabSystem.GetPrefab<TaxiwayPrefab>(nativeArray2[num27]);
						NetData value38 = nativeArray3[num27];
						NetGeometryData value39 = nativeArray4[num27];
						TaxiwayData value40 = nativeArray23[num27];
						Layer layer4 = (flag2 ? Layer.MarkerTaxiway : Layer.Taxiway);
						value38.m_RequiredLayers |= layer4;
						value38.m_ConnectLayers |= Layer.Pathway | Layer.Taxiway | Layer.MarkerPathway | Layer.MarkerTaxiway;
						value39.m_MergeLayers |= layer4;
						value39.m_IntersectLayers |= Layer.Pathway | Layer.Taxiway | Layer.MarkerPathway | Layer.MarkerTaxiway;
						value39.m_EdgeLengthRange.max = 1000f;
						value39.m_ElevatedLength = 1000f;
						value39.m_Flags |= GeometryFlags.Directional;
						if (!flag2)
						{
							value39.m_Flags |= GeometryFlags.BlockZone | GeometryFlags.FlattenTerrain | GeometryFlags.ClipTerrain;
						}
						value40.m_SpeedLimit = prefab14.m_SpeedLimit / 3.6f;
						if (prefab14.m_Airspace)
						{
							if (prefab14.m_Runway)
							{
								value40.m_Flags |= TaxiwayFlags.Runway;
							}
							else if (!prefab14.m_Taxiway)
							{
								value39.m_Flags |= GeometryFlags.RaisedIsElevated | GeometryFlags.BlockZone | GeometryFlags.FlattenTerrain;
							}
							value40.m_Flags |= TaxiwayFlags.Airspace;
						}
						else if (prefab14.m_Runway)
						{
							value40.m_Flags |= TaxiwayFlags.Runway;
						}
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value41 = nativeArray5[num27];
							value41.m_PlacementFlags |= PlacementFlags.OnGround;
							value41.m_SnapDistance = (flag2 ? 4f : 8f);
							nativeArray5[num27] = value41;
						}
						nativeArray3[num27] = value38;
						nativeArray4[num27] = value39;
						nativeArray23[num27] = value40;
					}
				}
				bool flag10 = archetypeChunk.Has(ref typeHandle23);
				if (flag10)
				{
					for (int num28 = 0; num28 < nativeArray.Length; num28++)
					{
						PowerLinePrefab prefab15 = m_PrefabSystem.GetPrefab<PowerLinePrefab>(nativeArray2[num28]);
						NetGeometryData value42 = nativeArray4[num28];
						bool flag11 = false;
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value43 = nativeArray5[num28];
							value43.m_PlacementFlags |= PlacementFlags.OnGround;
							flag11 = value43.m_ElevationRange.max < 0f;
							nativeArray5[num28] = value43;
						}
						value42.m_EdgeLengthRange.max = prefab15.m_MaxPylonDistance;
						value42.m_ElevatedLength = prefab15.m_MaxPylonDistance;
						value42.m_Hanging = prefab15.m_Hanging;
						value42.m_Flags |= GeometryFlags.StrictNodes | GeometryFlags.LoweredIsTunnel | GeometryFlags.RaisedIsElevated;
						if (!flag2)
						{
							value42.m_Flags |= GeometryFlags.FlattenTerrain;
						}
						if (flag11)
						{
							value42.m_IntersectLayers |= Layer.PowerlineLow | Layer.PowerlineHigh;
							value42.m_MergeLayers |= Layer.PowerlineLow | Layer.PowerlineHigh;
						}
						else
						{
							value42.m_Flags |= GeometryFlags.StraightEdges | GeometryFlags.NoEdgeConnection | GeometryFlags.SnapToNetAreas | GeometryFlags.BlockZone | GeometryFlags.StandingNodes;
						}
						nativeArray4[num28] = value42;
					}
				}
				NativeArray<WaterPipeConnectionData> nativeArray24 = archetypeChunk.GetNativeArray(ref typeHandle28);
				NativeArray<ResourceConnectionData> nativeArray25 = archetypeChunk.GetNativeArray(ref typeHandle29);
				bool flag12 = archetypeChunk.Has(ref typeHandle24);
				if (flag12)
				{
					for (int num29 = 0; num29 < nativeArray.Length; num29++)
					{
						m_PrefabSystem.GetPrefab<PipelinePrefab>(nativeArray2[num29]);
						NetGeometryData value44 = nativeArray4[num29];
						value44.m_ElevatedLength = value44.m_EdgeLengthRange.max;
						value44.m_Flags |= GeometryFlags.StrictNodes | GeometryFlags.LoweredIsTunnel | GeometryFlags.RaisedIsElevated;
						if (nativeArray24.Length != 0)
						{
							value44.m_IntersectLayers |= Layer.WaterPipe | Layer.SewagePipe | Layer.StormwaterPipe;
							value44.m_MergeLayers |= Layer.WaterPipe | Layer.SewagePipe | Layer.StormwaterPipe;
						}
						if (nativeArray25.Length != 0)
						{
							value44.m_IntersectLayers |= Layer.ResourceLine;
							value44.m_MergeLayers |= Layer.ResourceLine;
						}
						if (!flag2)
						{
							value44.m_Flags |= GeometryFlags.FlattenTerrain;
						}
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value45 = nativeArray5[num29];
							value45.m_PlacementFlags |= PlacementFlags.OnGround;
							nativeArray5[num29] = value45;
						}
						nativeArray4[num29] = value44;
					}
				}
				if (archetypeChunk.Has(ref typeHandle25))
				{
					for (int num30 = 0; num30 < nativeArray.Length; num30++)
					{
						m_PrefabSystem.GetPrefab<FencePrefab>(nativeArray2[num30]);
						NetData value46 = nativeArray3[num30];
						NetGeometryData value47 = nativeArray4[num30];
						value46.m_RequiredLayers |= Layer.Fence;
						value46.m_ConnectLayers |= Layer.Fence;
						value47.m_ElevatedLength = value47.m_EdgeLengthRange.max;
						value47.m_Flags |= GeometryFlags.StrictNodes | GeometryFlags.BlockZone | GeometryFlags.FlattenTerrain;
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value48 = nativeArray5[num30];
							value48.m_PlacementFlags |= PlacementFlags.OnGround;
							value48.m_SnapDistance = 4f;
							nativeArray5[num30] = value48;
						}
						nativeArray3[num30] = value46;
						nativeArray4[num30] = value47;
					}
				}
				if (archetypeChunk.Has(ref typeHandle26))
				{
					for (int num31 = 0; num31 < nativeArray3.Length; num31++)
					{
						NetData value49 = nativeArray3[num31];
						value49.m_RequiredLayers |= Layer.LaneEditor;
						value49.m_ConnectLayers |= Layer.LaneEditor;
						nativeArray3[num31] = value49;
					}
				}
				if (flag3)
				{
					BufferAccessor<FixedNetElement> bufferAccessor13 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle11);
					for (int num32 = 0; num32 < nativeArray4.Length; num32++)
					{
						NetGeometryPrefab prefab16 = m_PrefabSystem.GetPrefab<NetGeometryPrefab>(nativeArray2[num32]);
						Bridge component24 = prefab16.GetComponent<Bridge>();
						NetData value50 = nativeArray3[num32];
						NetGeometryData value51 = nativeArray4[num32];
						value50.m_NodePriority += 1000f;
						if (component24.m_SegmentLength > 0.1f)
						{
							if (!component24.m_AllowMinimalLength)
							{
								value51.m_EdgeLengthRange.min = component24.m_SegmentLength * 0.6f;
							}
							value51.m_EdgeLengthRange.max = component24.m_SegmentLength * 1.4f;
						}
						value51.m_ElevatedLength = value51.m_EdgeLengthRange.max;
						value51.m_Hanging = component24.m_Hanging;
						value51.m_Flags |= GeometryFlags.StraightEdges | GeometryFlags.StraightEnds | GeometryFlags.RequireElevated | GeometryFlags.SymmetricalEdges | GeometryFlags.SmoothSlopes;
						if (component24.m_CanCurve)
						{
							value51.m_Flags &= ~GeometryFlags.StraightEdges;
						}
						switch (component24.m_BuildStyle)
						{
						case BridgeBuildStyle.Raised:
							value51.m_Flags |= GeometryFlags.ElevatedIsRaised;
							break;
						case BridgeBuildStyle.Quay:
							value51.m_Flags |= GeometryFlags.ElevatedIsRaised;
							value51.m_Flags &= ~GeometryFlags.RequireElevated;
							break;
						}
						if (nativeArray5.Length != 0)
						{
							PlaceableNetData value52 = nativeArray5[num32];
							switch (component24.m_WaterFlow)
							{
							case BridgeWaterFlow.Left:
								value52.m_PlacementFlags |= PlacementFlags.FlowLeft;
								break;
							case BridgeWaterFlow.Right:
								value52.m_PlacementFlags |= PlacementFlags.FlowRight;
								break;
							}
							if (component24.m_BuildStyle == BridgeBuildStyle.Quay)
							{
								value52.m_PlacementFlags |= PlacementFlags.ShoreLine;
							}
							value52.m_MinWaterElevation = component24.m_ElevationOnWater;
							nativeArray5[num32] = value52;
						}
						if (bufferAccessor13.Length != 0)
						{
							DynamicBuffer<FixedNetElement> dynamicBuffer12 = bufferAccessor13[num32];
							dynamicBuffer12.ResizeUninitialized(component24.m_FixedSegments.Length);
							int num33 = 0;
							bool flag13 = false;
							for (int num34 = 0; num34 < dynamicBuffer12.Length; num34++)
							{
								FixedNetSegmentInfo fixedNetSegmentInfo = component24.m_FixedSegments[num34];
								flag13 |= fixedNetSegmentInfo.m_Length <= 0.1f;
							}
							for (int num35 = 0; num35 < dynamicBuffer12.Length; num35++)
							{
								FixedNetSegmentInfo fixedNetSegmentInfo2 = component24.m_FixedSegments[num35];
								value53.m_Flags = (FixedNetFlags)0u;
								if (fixedNetSegmentInfo2.m_Length > 0.1f)
								{
									if (flag13)
									{
										value53.m_LengthRange.min = fixedNetSegmentInfo2.m_Length;
										value53.m_LengthRange.max = fixedNetSegmentInfo2.m_Length;
									}
									else
									{
										value53.m_LengthRange.min = fixedNetSegmentInfo2.m_Length * 0.6f;
										value53.m_LengthRange.max = fixedNetSegmentInfo2.m_Length * 1.4f;
									}
								}
								else
								{
									value53.m_LengthRange = value51.m_EdgeLengthRange;
								}
								if (fixedNetSegmentInfo2.m_CanCurve)
								{
									value51.m_Flags &= ~GeometryFlags.StraightEdges;
									num33++;
								}
								else
								{
									value53.m_Flags |= FixedNetFlags.Straight;
								}
								value53.m_CountRange = fixedNetSegmentInfo2.m_CountRange;
								NetCompositionHelpers.GetRequirementFlags(fixedNetSegmentInfo2.m_SetState, out value53.m_SetState, out var sectionFlags14);
								NetCompositionHelpers.GetRequirementFlags(fixedNetSegmentInfo2.m_UnsetState, out value53.m_UnsetState, out var sectionFlags15);
								if ((sectionFlags14 | sectionFlags15) != 0)
								{
									COSystemBase.baseLog.ErrorFormat(prefab16, "Net segment state ({0}) cannot (un)set section flags: {1}", prefab16.name, sectionFlags14 | sectionFlags15);
								}
								dynamicBuffer12[num35] = value53;
							}
							if (num33 >= 2)
							{
								value51.m_Flags |= GeometryFlags.NoCurveSplit;
							}
						}
						nativeArray3[num32] = value50;
						nativeArray4[num32] = value51;
					}
				}
				NativeArray<ElectricityConnectionData> nativeArray26 = archetypeChunk.GetNativeArray(ref typeHandle27);
				if (nativeArray26.Length != 0)
				{
					NativeArray<LocalConnectData> nativeArray27 = archetypeChunk.GetNativeArray(ref typeHandle8);
					for (int num36 = 0; num36 < nativeArray26.Length; num36++)
					{
						NetPrefab prefab17 = m_PrefabSystem.GetPrefab<NetPrefab>(nativeArray2[num36]);
						ElectricityConnection component25 = prefab17.GetComponent<ElectricityConnection>();
						NetData value54 = nativeArray3[num36];
						ElectricityConnectionData value55 = nativeArray26[num36];
						Layer layer5;
						Layer layer6;
						float snapDistance;
						switch (component25.m_Voltage)
						{
						case ElectricityConnection.Voltage.Low:
							layer5 = Layer.PowerlineLow;
							layer6 = Layer.Road | Layer.PowerlineLow;
							snapDistance = 4f;
							break;
						case ElectricityConnection.Voltage.High:
							layer5 = Layer.PowerlineHigh;
							layer6 = Layer.PowerlineHigh;
							snapDistance = 8f;
							break;
						default:
							layer5 = Layer.None;
							layer6 = Layer.None;
							snapDistance = 8f;
							break;
						}
						if (flag10)
						{
							value54.m_RequiredLayers |= layer5;
							value54.m_ConnectLayers |= layer5;
							LocalConnectData value56 = nativeArray27[num36];
							value56.m_Flags |= LocalConnectFlags.ExplicitNodes | LocalConnectFlags.ChooseBest;
							value56.m_Layers |= layer6;
							value56.m_HeightRange = new Bounds1(-1000f, 1000f);
							value56.m_SearchDistance = 0f;
							if (flag2)
							{
								value56.m_Flags |= LocalConnectFlags.KeepOpen;
								value56.m_SearchDistance = 4f;
							}
							nativeArray27[num36] = value56;
							if (nativeArray5.Length != 0)
							{
								PlaceableNetData value57 = nativeArray5[num36];
								value57.m_SnapDistance = snapDistance;
								nativeArray5[num36] = value57;
							}
						}
						value54.m_LocalConnectLayers |= layer5;
						value55.m_Direction = component25.m_Direction;
						value55.m_Capacity = component25.m_Capacity;
						value55.m_Voltage = component25.m_Voltage;
						NetCompositionHelpers.GetRequirementFlags(component25.m_RequireAll, out value55.m_CompositionAll, out var sectionFlags16);
						NetCompositionHelpers.GetRequirementFlags(component25.m_RequireAny, out value55.m_CompositionAny, out var sectionFlags17);
						NetCompositionHelpers.GetRequirementFlags(component25.m_RequireNone, out value55.m_CompositionNone, out var sectionFlags18);
						NetSectionFlags netSectionFlags5 = sectionFlags16 | sectionFlags17 | sectionFlags18;
						if (netSectionFlags5 != 0)
						{
							COSystemBase.baseLog.ErrorFormat(prefab17, "Electricity connection ({0}) cannot require section flags: {1}", prefab17.name, netSectionFlags5);
						}
						nativeArray3[num36] = value54;
						nativeArray26[num36] = value55;
					}
				}
				if (nativeArray24.Length != 0)
				{
					NativeArray<LocalConnectData> nativeArray28 = archetypeChunk.GetNativeArray(ref typeHandle8);
					for (int num37 = 0; num37 < nativeArray24.Length; num37++)
					{
						WaterPipeConnection component26 = m_PrefabSystem.GetPrefab<NetPrefab>(nativeArray2[num37]).GetComponent<WaterPipeConnection>();
						NetData value58 = nativeArray3[num37];
						WaterPipeConnectionData value59 = nativeArray24[num37];
						Layer layer7 = Layer.None;
						if (component26.m_FreshCapacity != 0)
						{
							layer7 |= Layer.WaterPipe;
						}
						if (component26.m_SewageCapacity != 0)
						{
							layer7 |= Layer.SewagePipe;
						}
						if (component26.m_StormCapacity != 0)
						{
							layer7 |= Layer.StormwaterPipe;
						}
						if (flag12)
						{
							value58.m_RequiredLayers |= layer7;
							value58.m_ConnectLayers |= layer7;
							LocalConnectData value60 = nativeArray28[num37];
							value60.m_Flags |= LocalConnectFlags.ExplicitNodes | LocalConnectFlags.ChooseBest;
							value60.m_Layers |= Layer.Road | layer7;
							value60.m_HeightRange = new Bounds1(-1000f, 1000f);
							value60.m_SearchDistance = 0f;
							if (flag2)
							{
								value60.m_Flags |= LocalConnectFlags.KeepOpen;
								value60.m_SearchDistance = 4f;
							}
							nativeArray28[num37] = value60;
							if (nativeArray5.Length != 0)
							{
								PlaceableNetData value61 = nativeArray5[num37];
								value61.m_SnapDistance = 4f;
								nativeArray5[num37] = value61;
							}
						}
						value58.m_LocalConnectLayers |= layer7;
						value59.m_FreshCapacity = component26.m_FreshCapacity;
						value59.m_SewageCapacity = component26.m_SewageCapacity;
						value59.m_StormCapacity = component26.m_StormCapacity;
						nativeArray3[num37] = value58;
						nativeArray24[num37] = value59;
					}
				}
				if (nativeArray25.Length != 0)
				{
					NativeArray<LocalConnectData> nativeArray29 = archetypeChunk.GetNativeArray(ref typeHandle8);
					for (int num38 = 0; num38 < nativeArray.Length; num38++)
					{
						NetData value62 = nativeArray3[num38];
						if (flag12)
						{
							value62.m_RequiredLayers |= Layer.ResourceLine;
							value62.m_ConnectLayers |= Layer.ResourceLine;
							LocalConnectData value63 = nativeArray29[num38];
							value63.m_Flags |= LocalConnectFlags.ExplicitNodes | LocalConnectFlags.ChooseBest;
							value63.m_Layers |= Layer.Pathway | Layer.ResourceLine;
							value63.m_HeightRange = new Bounds1(-1000f, 1000f);
							value63.m_SearchDistance = 0f;
							if (flag2)
							{
								value63.m_Flags |= LocalConnectFlags.KeepOpen;
								value63.m_SearchDistance = 4f;
							}
							nativeArray29[num38] = value63;
							if (nativeArray5.Length != 0)
							{
								PlaceableNetData value64 = nativeArray5[num38];
								value64.m_SnapDistance = 4f;
								nativeArray5[num38] = value64;
							}
						}
						value62.m_LocalConnectLayers |= Layer.ResourceLine;
						nativeArray3[num38] = value62;
					}
				}
				if (flag4)
				{
					for (int num39 = 0; num39 < nativeArray3.Length; num39++)
					{
						NetData netData = nativeArray3[num39];
						m_InGameLayersTwice |= m_InGameLayersOnce & netData.m_RequiredLayers;
						m_InGameLayersOnce |= netData.m_RequiredLayers;
					}
				}
			}
		}
		catch
		{
			chunks.Dispose();
			throw;
		}
		m_PathfindHeuristicData.value = new PathfindHeuristicData
		{
			m_CarCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f),
			m_TrackCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f),
			m_PedestrianCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f),
			m_FlyingCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f),
			m_TaxiCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f),
			m_OffRoadCosts = new PathfindCosts(1000000f, 1000000f, 1000000f, 1000000f)
		};
		InitializeNetDefaultsJob jobData = new InitializeNetDefaultsJob
		{
			m_Chunks = chunks,
			m_NetGeometrySectionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometrySection_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableNetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_RoadData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DefaultNetLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_DefaultNetLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetPieceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetPieceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetVertexMatchData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetVertexMatchData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableNetPieceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetSubSectionData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetSubSection_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSectionPieceData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetSectionPiece_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetPieceLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetPieceLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetPieceObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetPieceObject_RO_BufferLookup, ref base.CheckedStateRef)
		};
		CollectPathfindDataJob jobData2 = new CollectPathfindDataJob
		{
			m_NetLaneDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectionLaneDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ConnectionLaneData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathfindCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindCarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindTrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTrackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindPedestrianData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindPedestrianData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTransportData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindHeuristicData = m_PathfindHeuristicData
		};
		JobHandle job = IJobParallelForExtensions.Schedule(jobData, chunks.Length, 1, base.Dependency);
		JobHandle jobHandle = JobHandle.CombineDependencies(job, m_PathfindHeuristicDeps = JobChunkExtensions.Schedule(jobData2, m_LaneQuery, base.Dependency));
		if (flag)
		{
			JobHandle job2 = JobChunkExtensions.ScheduleParallel(new FixPlaceholdersJob
			{
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderObjectElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RW_BufferTypeHandle, ref base.CheckedStateRef)
			}, m_PlaceholderQuery, base.Dependency);
			jobHandle = JobHandle.CombineDependencies(jobHandle, job2);
		}
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
	public NetInitializeSystem()
	{
	}
}
