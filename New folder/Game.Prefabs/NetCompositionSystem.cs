using System;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class NetCompositionSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeCompositionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<NetCompositionData> m_NetCompositionType;

		public ComponentTypeHandle<PlaceableNetComposition> m_PlaceableNetCompositionType;

		public ComponentTypeHandle<RoadComposition> m_RoadCompositionType;

		public ComponentTypeHandle<TrackComposition> m_TrackCompositionType;

		public ComponentTypeHandle<WaterwayComposition> m_WaterwayCompositionType;

		public ComponentTypeHandle<PathwayComposition> m_PathwayCompositionType;

		public ComponentTypeHandle<TaxiwayComposition> m_TaxiwayCompositionType;

		public ComponentTypeHandle<TerrainComposition> m_TerrainCompositionType;

		public BufferTypeHandle<NetCompositionPiece> m_NetCompositionPieceType;

		public BufferTypeHandle<NetCompositionLane> m_NetCompositionLaneType;

		public BufferTypeHandle<NetCompositionObject> m_NetCompositionObjectType;

		public BufferTypeHandle<NetCompositionArea> m_NetCompositionAreaType;

		public BufferTypeHandle<NetCompositionCrosswalk> m_NetCompositionCrosswalkType;

		public BufferTypeHandle<NetCompositionCarriageway> m_NetCompositionCarriagewayType;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetPieceData> m_PlaceableNetPieceData;

		[ReadOnly]
		public ComponentLookup<NetPieceData> m_NetPieceData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<NetCrosswalkData> m_NetCrosswalkData;

		[ReadOnly]
		public ComponentLookup<NetVertexMatchData> m_NetVertexMatchData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadData;

		[ReadOnly]
		public ComponentLookup<TrackData> m_TrackData;

		[ReadOnly]
		public ComponentLookup<WaterwayData> m_WaterwayData;

		[ReadOnly]
		public ComponentLookup<PathwayData> m_PathwayData;

		[ReadOnly]
		public ComponentLookup<TaxiwayData> m_TaxiwayData;

		[ReadOnly]
		public ComponentLookup<StreetLightData> m_StreetLightData;

		[ReadOnly]
		public ComponentLookup<LaneDirectionData> m_LaneDirectionData;

		[ReadOnly]
		public ComponentLookup<TrafficSignData> m_TrafficSignData;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> m_UtilityObjectData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<NetTerrainData> m_TerrainData;

		[ReadOnly]
		public ComponentLookup<BridgeData> m_BridgeData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public BufferLookup<NetPieceLane> m_NetPieceLanes;

		[ReadOnly]
		public BufferLookup<NetPieceObject> m_NetPieceObjects;

		[ReadOnly]
		public BufferLookup<NetPieceArea> m_NetPieceAreas;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetCompositionData> nativeArray = chunk.GetNativeArray(ref m_NetCompositionType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PlaceableNetComposition> nativeArray3 = chunk.GetNativeArray(ref m_PlaceableNetCompositionType);
			NativeArray<RoadComposition> nativeArray4 = chunk.GetNativeArray(ref m_RoadCompositionType);
			NativeArray<TrackComposition> nativeArray5 = chunk.GetNativeArray(ref m_TrackCompositionType);
			NativeArray<WaterwayComposition> nativeArray6 = chunk.GetNativeArray(ref m_WaterwayCompositionType);
			NativeArray<PathwayComposition> nativeArray7 = chunk.GetNativeArray(ref m_PathwayCompositionType);
			NativeArray<TaxiwayComposition> nativeArray8 = chunk.GetNativeArray(ref m_TaxiwayCompositionType);
			NativeArray<TerrainComposition> nativeArray9 = chunk.GetNativeArray(ref m_TerrainCompositionType);
			BufferAccessor<NetCompositionPiece> bufferAccessor = chunk.GetBufferAccessor(ref m_NetCompositionPieceType);
			BufferAccessor<NetCompositionLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_NetCompositionLaneType);
			BufferAccessor<NetCompositionObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_NetCompositionObjectType);
			BufferAccessor<NetCompositionArea> bufferAccessor4 = chunk.GetBufferAccessor(ref m_NetCompositionAreaType);
			BufferAccessor<NetCompositionCrosswalk> bufferAccessor5 = chunk.GetBufferAccessor(ref m_NetCompositionCrosswalkType);
			BufferAccessor<NetCompositionCarriageway> bufferAccessor6 = chunk.GetBufferAccessor(ref m_NetCompositionCarriagewayType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NetCompositionData compositionData = nativeArray[i];
				DynamicBuffer<NetCompositionPiece> dynamicBuffer = bufferAccessor[i];
				PrefabRef prefabRef = nativeArray2[i];
				NetGeometryData netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
				NetCompositionHelpers.CalculateCompositionData(ref compositionData, dynamicBuffer.AsNativeArray(), m_NetPieceData, m_NetVertexMatchData);
				if (!m_PrefabData.IsComponentEnabled(prefabRef.m_Prefab))
				{
					if ((compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0)
					{
						compositionData.m_Width = netGeometryData.m_ElevatedWidth;
						compositionData.m_HeightRange = netGeometryData.m_ElevatedHeightRange;
					}
					else
					{
						compositionData.m_Width = netGeometryData.m_DefaultWidth;
						compositionData.m_HeightRange = netGeometryData.m_DefaultHeightRange;
					}
					compositionData.m_SurfaceHeight = netGeometryData.m_DefaultSurfaceHeight;
				}
				NetCompositionHelpers.CalculateMinLod(ref compositionData, dynamicBuffer.AsNativeArray(), m_MeshData);
				if ((netGeometryData.m_Flags & GeometryFlags.Marker) != 0)
				{
					compositionData.m_State |= CompositionState.Marker | CompositionState.NoSubCollisions;
				}
				if ((netGeometryData.m_Flags & GeometryFlags.BlockZone) != 0)
				{
					compositionData.m_State |= CompositionState.BlockZone;
				}
				nativeArray[i] = compositionData;
			}
			if (bufferAccessor2.Length != 0)
			{
				NativeList<NetCompositionLane> netLanes = new NativeList<NetCompositionLane>(32, Allocator.Temp);
				for (int j = 0; j < bufferAccessor2.Length; j++)
				{
					NetCompositionData compositionData2 = nativeArray[j];
					DynamicBuffer<NetCompositionPiece> pieces = bufferAccessor[j];
					DynamicBuffer<NetCompositionLane> dynamicBuffer2 = bufferAccessor2[j];
					PrefabRef prefabRef2 = nativeArray2[j];
					DynamicBuffer<NetCompositionCarriageway> carriageways = default(DynamicBuffer<NetCompositionCarriageway>);
					if (bufferAccessor6.Length != 0)
					{
						carriageways = bufferAccessor6[j];
					}
					NetCompositionHelpers.AddCompositionLanes(prefabRef2.m_Prefab, ref compositionData2, pieces, netLanes, carriageways, m_NetLaneData, m_NetPieceLanes);
					dynamicBuffer2.CopyFrom(netLanes.AsArray());
					netLanes.Clear();
					nativeArray[j] = compositionData2;
				}
			}
			for (int k = 0; k < bufferAccessor3.Length; k++)
			{
				NetCompositionData compositionData3 = nativeArray[k];
				DynamicBuffer<NetCompositionPiece> pieces2 = bufferAccessor[k];
				DynamicBuffer<NetCompositionObject> objects = bufferAccessor3[k];
				AddCompositionObjects(nativeArray2[k].m_Prefab, compositionData3, pieces2, objects, m_StreetLightData, m_LaneDirectionData, m_TrafficSignData, m_UtilityObjectData, m_NetPieceObjects);
			}
			for (int l = 0; l < bufferAccessor4.Length; l++)
			{
				NetCompositionData compositionData4 = nativeArray[l];
				DynamicBuffer<NetCompositionPiece> pieces3 = bufferAccessor[l];
				DynamicBuffer<NetCompositionArea> netAreas = bufferAccessor4[l];
				PrefabRef prefabRef3 = nativeArray2[l];
				bool isBridge = (compositionData4.m_Flags.m_General & CompositionFlags.General.Elevated) != 0 && m_BridgeData.HasComponent(prefabRef3.m_Prefab);
				AddCompositionAreas(prefabRef3.m_Prefab, compositionData4, pieces3, netAreas, isBridge);
			}
			for (int m = 0; m < bufferAccessor5.Length; m++)
			{
				NetCompositionData compositionData5 = nativeArray[m];
				DynamicBuffer<NetCompositionPiece> pieces4 = bufferAccessor[m];
				DynamicBuffer<NetCompositionCrosswalk> crosswalks = bufferAccessor5[m];
				AddCompositionCrosswalks(nativeArray2[m].m_Prefab, compositionData5, pieces4, crosswalks);
			}
			for (int n = 0; n < nativeArray3.Length; n++)
			{
				PlaceableNetComposition placeableData = nativeArray3[n];
				NetCompositionHelpers.CalculatePlaceableData(ref placeableData, bufferAccessor[n].AsNativeArray(), m_PlaceableNetPieceData);
				nativeArray3[n] = placeableData;
			}
			for (int num = 0; num < nativeArray4.Length; num++)
			{
				PrefabRef prefabRef4 = nativeArray2[num];
				NetCompositionData netCompositionData = nativeArray[num];
				RoadComposition value = nativeArray4[num];
				RoadData roadData = m_RoadData[prefabRef4.m_Prefab];
				value.m_ZoneBlockPrefab = roadData.m_ZoneBlockPrefab;
				value.m_Flags = roadData.m_Flags;
				value.m_SpeedLimit = roadData.m_SpeedLimit;
				value.m_Priority = roadData.m_SpeedLimit;
				if ((netCompositionData.m_State & CompositionState.SeparatedCarriageways) != 0)
				{
					value.m_Flags |= RoadFlags.SeparatedCarriageways;
				}
				if ((netCompositionData.m_Flags.m_General & CompositionFlags.General.Gravel) != 0)
				{
					value.m_Priority -= 1.25f;
				}
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<NetCompositionLane> dynamicBuffer3 = bufferAccessor2[num];
					int num2 = 0;
					int num3 = 0;
					for (int num4 = 0; num4 < dynamicBuffer3.Length; num4++)
					{
						NetCompositionLane netCompositionLane = dynamicBuffer3[num4];
						if ((netCompositionLane.m_Flags & (LaneFlags.Master | LaneFlags.Road)) == LaneFlags.Road)
						{
							if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0)
							{
								num3++;
							}
							else
							{
								num2++;
							}
						}
					}
					if ((roadData.m_Flags & RoadFlags.UseHighwayRules) != 0)
					{
						value.m_Priority += math.max(num2, num3);
					}
					else
					{
						value.m_Priority += (float)math.max(num2, num3) * 0.5f + (float)(num2 + num3) * 0.25f;
					}
				}
				if (bufferAccessor3.Length != 0)
				{
					DynamicBuffer<NetCompositionObject> dynamicBuffer4 = bufferAccessor3[num];
					for (int num5 = 0; num5 < dynamicBuffer4.Length; num5++)
					{
						NetCompositionObject netCompositionObject = dynamicBuffer4[num5];
						if (m_StreetLightData.HasComponent(netCompositionObject.m_Prefab))
						{
							value.m_Flags |= RoadFlags.HasStreetLights;
							break;
						}
					}
				}
				nativeArray4[num] = value;
			}
			for (int num6 = 0; num6 < nativeArray5.Length; num6++)
			{
				PrefabRef prefabRef5 = nativeArray2[num6];
				TrackComposition value2 = nativeArray5[num6];
				TrackData trackData = m_TrackData[prefabRef5.m_Prefab];
				value2.m_TrackType = trackData.m_TrackType;
				value2.m_SpeedLimit = trackData.m_SpeedLimit;
				nativeArray5[num6] = value2;
			}
			for (int num7 = 0; num7 < nativeArray6.Length; num7++)
			{
				PrefabRef prefabRef6 = nativeArray2[num7];
				WaterwayComposition value3 = nativeArray6[num7];
				value3.m_SpeedLimit = m_WaterwayData[prefabRef6.m_Prefab].m_SpeedLimit;
				nativeArray6[num7] = value3;
			}
			for (int num8 = 0; num8 < nativeArray7.Length; num8++)
			{
				PrefabRef prefabRef7 = nativeArray2[num8];
				PathwayComposition value4 = nativeArray7[num8];
				value4.m_SpeedLimit = m_PathwayData[prefabRef7.m_Prefab].m_SpeedLimit;
				nativeArray7[num8] = value4;
			}
			for (int num9 = 0; num9 < nativeArray8.Length; num9++)
			{
				PrefabRef prefabRef8 = nativeArray2[num9];
				TaxiwayComposition value5 = nativeArray8[num9];
				NetCompositionData value6 = nativeArray[num9];
				TaxiwayData taxiwayData = m_TaxiwayData[prefabRef8.m_Prefab];
				value5.m_SpeedLimit = taxiwayData.m_SpeedLimit;
				value5.m_Flags = taxiwayData.m_Flags;
				if ((taxiwayData.m_Flags & TaxiwayFlags.Airspace) != 0)
				{
					value6.m_State &= ~CompositionState.NoSubCollisions;
					value6.m_State |= CompositionState.Airspace;
				}
				nativeArray[num9] = value6;
				nativeArray8[num9] = value5;
			}
			for (int num10 = 0; num10 < nativeArray9.Length; num10++)
			{
				TerrainComposition value7 = nativeArray9[num10];
				NetCompositionData netCompositionData2 = nativeArray[num10];
				DynamicBuffer<NetCompositionPiece> dynamicBuffer5 = bufferAccessor[num10];
				value7.m_ClipHeightOffset = new float2(float.MaxValue, float.MinValue);
				value7.m_MinHeightOffset = float.MaxValue;
				value7.m_MaxHeightOffset = float.MinValue;
				for (int num11 = 0; num11 < dynamicBuffer5.Length; num11++)
				{
					NetCompositionPiece netCompositionPiece = dynamicBuffer5[num11];
					if (m_TerrainData.TryGetComponent(netCompositionPiece.m_Piece, out var componentData))
					{
						float2 @float = netCompositionData2.m_Width * 0.5f + new float2(netCompositionPiece.m_Offset.x, 0f - netCompositionPiece.m_Offset.x) - netCompositionPiece.m_Size.x * 0.5f;
						if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0)
						{
							componentData.m_WidthOffset = componentData.m_WidthOffset.yx;
							componentData.m_MinHeightOffset.xz = componentData.m_MinHeightOffset.zx;
							componentData.m_MaxHeightOffset.xz = componentData.m_MaxHeightOffset.zx;
						}
						if (componentData.m_WidthOffset.x != 0f)
						{
							value7.m_WidthOffset.x = math.max(value7.m_WidthOffset.x, @float.x + componentData.m_WidthOffset.x);
						}
						if (componentData.m_WidthOffset.y != 0f)
						{
							value7.m_WidthOffset.y = math.max(value7.m_WidthOffset.y, @float.y + componentData.m_WidthOffset.y);
						}
						value7.m_ClipHeightOffset.x = math.min(value7.m_ClipHeightOffset.x, componentData.m_ClipHeightOffset.x);
						value7.m_ClipHeightOffset.y = math.max(value7.m_ClipHeightOffset.y, componentData.m_ClipHeightOffset.y);
						value7.m_MinHeightOffset = math.min(value7.m_MinHeightOffset, componentData.m_MinHeightOffset);
						value7.m_MaxHeightOffset = math.max(value7.m_MaxHeightOffset, componentData.m_MaxHeightOffset);
					}
				}
				value7.m_ClipHeightOffset = math.select(value7.m_ClipHeightOffset, 0f, value7.m_ClipHeightOffset == new float2(float.MaxValue, float.MinValue));
				value7.m_MinHeightOffset = math.select(value7.m_MinHeightOffset, 0f, value7.m_MinHeightOffset == float.MaxValue);
				value7.m_MaxHeightOffset = math.select(value7.m_MaxHeightOffset, 0f, value7.m_MaxHeightOffset == float.MinValue);
				nativeArray9[num10] = value7;
			}
		}

		private void AddCompositionAreas(Entity entity, NetCompositionData compositionData, DynamicBuffer<NetCompositionPiece> pieces, DynamicBuffer<NetCompositionArea> netAreas, bool isBridge)
		{
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				if (!m_NetPieceAreas.HasBuffer(netCompositionPiece.m_Piece))
				{
					continue;
				}
				DynamicBuffer<NetPieceArea> dynamicBuffer = m_NetPieceAreas[netCompositionPiece.m_Piece];
				bool flag = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0;
				bool flag2 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Median) != 0;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					NetPieceArea netPieceArea = dynamicBuffer[math.select(j, dynamicBuffer.Length - j - 1, flag)];
					if (!isBridge || (netPieceArea.m_Flags & NetAreaFlags.NoBridge) == 0)
					{
						if (flag)
						{
							netPieceArea.m_Position.x = 0f - netPieceArea.m_Position.x;
							netPieceArea.m_SnapPosition.x = 0f - netPieceArea.m_SnapPosition.x;
							netPieceArea.m_Flags |= NetAreaFlags.Invert;
						}
						if (flag2)
						{
							netPieceArea.m_Flags |= NetAreaFlags.Median;
						}
						netAreas.Add(new NetCompositionArea
						{
							m_Flags = netPieceArea.m_Flags,
							m_Position = netCompositionPiece.m_Offset + netPieceArea.m_Position,
							m_SnapPosition = netCompositionPiece.m_Offset + netPieceArea.m_SnapPosition,
							m_Width = netPieceArea.m_Width,
							m_SnapWidth = netPieceArea.m_SnapWidth
						});
					}
				}
			}
		}

		public static void AddCompositionObjects(Entity entity, NetCompositionData compositionData, DynamicBuffer<NetCompositionPiece> pieces, DynamicBuffer<NetCompositionObject> objects, ComponentLookup<StreetLightData> streetLightDatas, ComponentLookup<LaneDirectionData> laneDirectionDatas, ComponentLookup<TrafficSignData> trafficSignDatas, ComponentLookup<UtilityObjectData> utilityObjectDatas, BufferLookup<NetPieceObject> netPieceObjects)
		{
			bool flag = (compositionData.m_Flags.m_General & CompositionFlags.General.Edge) != 0;
			bool flag2 = (compositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0;
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				if (!netPieceObjects.HasBuffer(netCompositionPiece.m_Piece))
				{
					continue;
				}
				DynamicBuffer<NetPieceObject> dynamicBuffer = netPieceObjects[netCompositionPiece.m_Piece];
				bool flag3 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0;
				bool flag4 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.FlipLanes) != 0;
				bool flag5 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Median) != 0;
				bool flag6 = (netCompositionPiece.m_PieceFlags & NetPieceFlags.PreserveShape) != 0;
				bool flag7 = flag && flag4;
				CompositionFlags compositionFlags;
				NetSectionFlags sectionFlags;
				if (flag3)
				{
					compositionFlags = NetCompositionHelpers.InvertCompositionFlags(compositionData.m_Flags);
					sectionFlags = NetCompositionHelpers.InvertSectionFlags(netCompositionPiece.m_SectionFlags);
				}
				else
				{
					compositionFlags = compositionData.m_Flags;
					sectionFlags = netCompositionPiece.m_SectionFlags;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					NetPieceObject netPieceObject = dynamicBuffer[math.select(j, dynamicBuffer.Length - j - 1, flag3)];
					if (!NetCompositionHelpers.TestObjectFlags(netPieceObject, compositionFlags, sectionFlags))
					{
						continue;
					}
					NetCompositionObject elem = default(NetCompositionObject);
					bool flag8 = false;
					if (flag3)
					{
						flag8 ^= (netPieceObject.m_Flags & SubObjectFlags.FlipInverted) != 0;
						netPieceObject.m_Position.x = 0f - netPieceObject.m_Position.x;
					}
					if (flag7)
					{
						flag8 ^= laneDirectionDatas.HasComponent(netPieceObject.m_Prefab) || trafficSignDatas.HasComponent(netPieceObject.m_Prefab);
					}
					if (flag8)
					{
						netPieceObject.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), netPieceObject.m_Rotation);
						netPieceObject.m_CurveOffsetRange = 1f - netPieceObject.m_CurveOffsetRange;
						if ((compositionData.m_Flags.m_General & CompositionFlags.General.Edge) != 0)
						{
							netPieceObject.m_Position.z = 0f - netPieceObject.m_Position.z;
						}
					}
					float3 @float = netCompositionPiece.m_Offset + netPieceObject.m_Position;
					elem.m_Prefab = netPieceObject.m_Prefab;
					elem.m_Position = @float.xz;
					elem.m_Offset = math.rotate(netPieceObject.m_Rotation, netPieceObject.m_Offset);
					elem.m_Offset.y += @float.y;
					elem.m_Rotation = netPieceObject.m_Rotation;
					elem.m_Flags = netPieceObject.m_Flags;
					elem.m_SpacingIgnore = netPieceObject.m_CompositionNone.m_General;
					elem.m_UseCurveRotation = netPieceObject.m_UseCurveRotation;
					elem.m_Probability = netPieceObject.m_Probability;
					elem.m_CurveOffsetRange = netPieceObject.m_CurveOffsetRange;
					elem.m_Spacing = netPieceObject.m_Spacing.z;
					elem.m_MinLength = netPieceObject.m_MinLength;
					if (netPieceObject.m_Spacing.z > 0.1f)
					{
						elem.m_Flags |= SubObjectFlags.AllowCombine;
					}
					if (flag5)
					{
						elem.m_Flags |= SubObjectFlags.OnMedian;
					}
					if (flag6)
					{
						elem.m_Flags |= SubObjectFlags.PreserveShape;
					}
					int k;
					NetCompositionObject netCompositionObject;
					bool flag11;
					if (netPieceObject.m_Spacing.x > 0.1f)
					{
						StreetLightData streetLightData = default(StreetLightData);
						bool flag9 = false;
						bool flag10 = utilityObjectDatas.HasComponent(netPieceObject.m_Prefab);
						if (streetLightDatas.HasComponent(netPieceObject.m_Prefab))
						{
							streetLightData = streetLightDatas[netPieceObject.m_Prefab];
							flag9 = true;
						}
						if (!flag9 || (compositionData.m_Flags.m_General & CompositionFlags.General.Intersection) == 0)
						{
							for (k = 0; k < objects.Length; k++)
							{
								netCompositionObject = objects[k];
								float num = math.abs(elem.m_Position.x - netCompositionObject.m_Position.x);
								flag11 = ((elem.m_Flags ^ netCompositionObject.m_Flags) & SubObjectFlags.SpacingOverride) != 0;
								if (num >= netPieceObject.m_Spacing.x && !flag11)
								{
									continue;
								}
								if (netCompositionObject.m_Prefab != elem.m_Prefab)
								{
									if (!flag9 || !streetLightDatas.HasComponent(netCompositionObject.m_Prefab))
									{
										if (!flag9 && !flag10 && elem.m_Spacing > 0.1f && netCompositionObject.m_Spacing > 0.1f && ((elem.m_Flags ^ netCompositionObject.m_Flags) & SubObjectFlags.EvenSpacing) == 0)
										{
											elem.m_AvoidSpacing = netCompositionObject.m_Spacing;
										}
										continue;
									}
									StreetLightData streetLightData2 = streetLightDatas[netCompositionObject.m_Prefab];
									if (streetLightData.m_Layer != streetLightData2.m_Layer)
									{
										continue;
									}
								}
								goto IL_0455;
							}
						}
					}
					goto IL_04cc;
					IL_04cc:
					objects.Add(elem);
					continue;
					IL_0455:
					if (flag11)
					{
						if ((elem.m_Flags & SubObjectFlags.SpacingOverride) == 0)
						{
							continue;
						}
						objects.RemoveAt(k);
					}
					else
					{
						float num2 = math.abs(elem.m_Position.x) - math.abs(netCompositionObject.m_Position.x);
						if (num2 >= 4f || (flag2 && num2 > -4f))
						{
							continue;
						}
						objects.RemoveAt(k);
					}
					goto IL_04cc;
				}
			}
		}

		private void AddCompositionCrosswalks(Entity entity, NetCompositionData compositionData, DynamicBuffer<NetCompositionPiece> pieces, DynamicBuffer<NetCompositionCrosswalk> crosswalks)
		{
			NetCrosswalkData netCrosswalkData = default(NetCrosswalkData);
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.Surface) == 0)
				{
					continue;
				}
				if (m_NetCrosswalkData.HasComponent(netCompositionPiece.m_Piece))
				{
					NetCrosswalkData netCrosswalkData2 = m_NetCrosswalkData[netCompositionPiece.m_Piece];
					if (flag3)
					{
						if (!flag)
						{
							NetLaneData netLaneData = m_NetLaneData[netCrosswalkData.m_Lane];
							NetCompositionCrosswalk elem = new NetCompositionCrosswalk
							{
								m_Lane = netCrosswalkData.m_Lane,
								m_Start = netCrosswalkData.m_Start,
								m_End = netCrosswalkData.m_End,
								m_Flags = netLaneData.m_Flags
							};
							if (flag4)
							{
								elem.m_Flags |= LaneFlags.CrossRoad;
							}
							crosswalks.Add(elem);
						}
						netCrosswalkData = default(NetCrosswalkData);
						flag = flag2;
						flag2 = false;
						flag3 = false;
						flag4 = false;
					}
					if (!flag4 && m_NetPieceLanes.HasBuffer(netCompositionPiece.m_Piece))
					{
						DynamicBuffer<NetPieceLane> dynamicBuffer = m_NetPieceLanes[netCompositionPiece.m_Piece];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							NetPieceLane netPieceLane = dynamicBuffer[j];
							if (netPieceLane.m_Position.x >= netCrosswalkData2.m_Start.x && netPieceLane.m_Position.x <= netCrosswalkData2.m_End.x && (m_NetLaneData[netPieceLane.m_Lane].m_Flags & LaneFlags.Road) != 0)
							{
								flag4 = true;
								break;
							}
						}
					}
					if ((netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0)
					{
						float x = netCrosswalkData2.m_Start.x;
						netCrosswalkData2.m_Start.x = 0f - netCrosswalkData2.m_End.x;
						netCrosswalkData2.m_End.x = 0f - x;
					}
					if (netCrosswalkData.m_Lane == Entity.Null)
					{
						netCrosswalkData.m_Lane = netCrosswalkData2.m_Lane;
						netCrosswalkData.m_Start = netCompositionPiece.m_Offset + netCrosswalkData2.m_Start;
						netCrosswalkData.m_End = netCompositionPiece.m_Offset + netCrosswalkData2.m_End;
					}
					else
					{
						netCrosswalkData.m_End = netCompositionPiece.m_Offset + netCrosswalkData2.m_End;
					}
					continue;
				}
				if (netCompositionPiece.m_Size.x > 0f)
				{
					flag2 = false;
					if (netCrosswalkData.m_Lane != Entity.Null)
					{
						flag3 = true;
					}
				}
				if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.BlockCrosswalk) != 0)
				{
					flag = true;
					flag2 = true;
				}
			}
			if (netCrosswalkData.m_Lane != Entity.Null && !flag)
			{
				NetLaneData netLaneData2 = m_NetLaneData[netCrosswalkData.m_Lane];
				NetCompositionCrosswalk elem2 = new NetCompositionCrosswalk
				{
					m_Lane = netCrosswalkData.m_Lane,
					m_Start = netCrosswalkData.m_Start,
					m_End = netCrosswalkData.m_End,
					m_Flags = netLaneData2.m_Flags
				};
				if (flag4)
				{
					elem2.m_Flags |= LaneFlags.CrossRoad;
				}
				crosswalks.Add(elem2);
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NetCompositionData> __Game_Prefabs_NetCompositionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceableNetComposition> __Game_Prefabs_PlaceableNetComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<RoadComposition> __Game_Prefabs_RoadComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TrackComposition> __Game_Prefabs_TrackComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WaterwayComposition> __Game_Prefabs_WaterwayComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathwayComposition> __Game_Prefabs_PathwayComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TaxiwayComposition> __Game_Prefabs_TaxiwayComposition_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TerrainComposition> __Game_Prefabs_TerrainComposition_RW_ComponentTypeHandle;

		public BufferTypeHandle<NetCompositionPiece> __Game_Prefabs_NetCompositionPiece_RW_BufferTypeHandle;

		public BufferTypeHandle<NetCompositionLane> __Game_Prefabs_NetCompositionLane_RW_BufferTypeHandle;

		public BufferTypeHandle<NetCompositionObject> __Game_Prefabs_NetCompositionObject_RW_BufferTypeHandle;

		public BufferTypeHandle<NetCompositionArea> __Game_Prefabs_NetCompositionArea_RW_BufferTypeHandle;

		public BufferTypeHandle<NetCompositionCrosswalk> __Game_Prefabs_NetCompositionCrosswalk_RW_BufferTypeHandle;

		public BufferTypeHandle<NetCompositionCarriageway> __Game_Prefabs_NetCompositionCarriageway_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetPieceData> __Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetPieceData> __Game_Prefabs_NetPieceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCrosswalkData> __Game_Prefabs_NetCrosswalkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetVertexMatchData> __Game_Prefabs_NetVertexMatchData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackData> __Game_Prefabs_TrackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterwayData> __Game_Prefabs_WaterwayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathwayData> __Game_Prefabs_PathwayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiwayData> __Game_Prefabs_TaxiwayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StreetLightData> __Game_Prefabs_StreetLightData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneDirectionData> __Game_Prefabs_LaneDirectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficSignData> __Game_Prefabs_TrafficSignData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> __Game_Prefabs_UtilityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetTerrainData> __Game_Prefabs_NetTerrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BridgeData> __Game_Prefabs_BridgeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<NetPieceLane> __Game_Prefabs_NetPieceLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetPieceObject> __Game_Prefabs_NetPieceObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetPieceArea> __Game_Prefabs_NetPieceArea_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetCompositionData>();
			__Game_Prefabs_PlaceableNetComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableNetComposition>();
			__Game_Prefabs_RoadComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RoadComposition>();
			__Game_Prefabs_TrackComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrackComposition>();
			__Game_Prefabs_WaterwayComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterwayComposition>();
			__Game_Prefabs_PathwayComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathwayComposition>();
			__Game_Prefabs_TaxiwayComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiwayComposition>();
			__Game_Prefabs_TerrainComposition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TerrainComposition>();
			__Game_Prefabs_NetCompositionPiece_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionPiece>();
			__Game_Prefabs_NetCompositionLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionLane>();
			__Game_Prefabs_NetCompositionObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionObject>();
			__Game_Prefabs_NetCompositionArea_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionArea>();
			__Game_Prefabs_NetCompositionCrosswalk_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionCrosswalk>();
			__Game_Prefabs_NetCompositionCarriageway_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetCompositionCarriageway>();
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetPieceData>(isReadOnly: true);
			__Game_Prefabs_NetPieceData_RO_ComponentLookup = state.GetComponentLookup<NetPieceData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NetCrosswalkData_RO_ComponentLookup = state.GetComponentLookup<NetCrosswalkData>(isReadOnly: true);
			__Game_Prefabs_NetVertexMatchData_RO_ComponentLookup = state.GetComponentLookup<NetVertexMatchData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Prefabs_TrackData_RO_ComponentLookup = state.GetComponentLookup<TrackData>(isReadOnly: true);
			__Game_Prefabs_WaterwayData_RO_ComponentLookup = state.GetComponentLookup<WaterwayData>(isReadOnly: true);
			__Game_Prefabs_PathwayData_RO_ComponentLookup = state.GetComponentLookup<PathwayData>(isReadOnly: true);
			__Game_Prefabs_TaxiwayData_RO_ComponentLookup = state.GetComponentLookup<TaxiwayData>(isReadOnly: true);
			__Game_Prefabs_StreetLightData_RO_ComponentLookup = state.GetComponentLookup<StreetLightData>(isReadOnly: true);
			__Game_Prefabs_LaneDirectionData_RO_ComponentLookup = state.GetComponentLookup<LaneDirectionData>(isReadOnly: true);
			__Game_Prefabs_TrafficSignData_RO_ComponentLookup = state.GetComponentLookup<TrafficSignData>(isReadOnly: true);
			__Game_Prefabs_UtilityObjectData_RO_ComponentLookup = state.GetComponentLookup<UtilityObjectData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_NetTerrainData_RO_ComponentLookup = state.GetComponentLookup<NetTerrainData>(isReadOnly: true);
			__Game_Prefabs_BridgeData_RO_ComponentLookup = state.GetComponentLookup<BridgeData>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_NetPieceLane_RO_BufferLookup = state.GetBufferLookup<NetPieceLane>(isReadOnly: true);
			__Game_Prefabs_NetPieceObject_RO_BufferLookup = state.GetBufferLookup<NetPieceObject>(isReadOnly: true);
			__Game_Prefabs_NetPieceArea_RO_BufferLookup = state.GetBufferLookup<NetPieceArea>(isReadOnly: true);
		}
	}

	private EntityQuery m_CompositionQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CompositionQuery = GetEntityQuery(ComponentType.ReadWrite<NetCompositionData>(), ComponentType.ReadWrite<NetCompositionPiece>(), ComponentType.ReadOnly<NetCompositionMeshRef>(), ComponentType.ReadOnly<Created>());
		RequireForUpdate(m_CompositionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitializeCompositionJob jobData = new InitializeCompositionJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableNetCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableNetComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_RoadComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrackCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TrackComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterwayCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterwayComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathwayCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PathwayComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TaxiwayCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TaxiwayComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TerrainCompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TerrainComposition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionPieceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionPiece_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionObject_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionArea_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionCrosswalkType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionCrosswalk_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetCompositionCarriagewayType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionCarriageway_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableNetPieceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetPieceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetPieceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetPieceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCrosswalkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCrosswalkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetVertexMatchData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetVertexMatchData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterwayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterwayData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathwayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathwayData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiwayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TaxiwayData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StreetLightData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneDirectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LaneDirectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficSignData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficSignData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UtilityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetTerrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BridgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BridgeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetPieceLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetPieceLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetPieceObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetPieceObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetPieceAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetPieceArea_RO_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompositionQuery, base.Dependency);
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
	public NetCompositionSystem()
	{
	}
}
