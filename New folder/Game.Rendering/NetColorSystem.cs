using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class NetColorSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateEdgeColorsJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> m_InfoviewCoverageType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewAvailabilityData> m_InfoviewAvailabilityType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

		[ReadOnly]
		public ComponentTypeHandle<TrainTrack> m_TrainTrackType;

		[ReadOnly]
		public ComponentTypeHandle<TramTrack> m_TramTrackType;

		[ReadOnly]
		public ComponentTypeHandle<Waterway> m_WaterwayType;

		[ReadOnly]
		public ComponentTypeHandle<SubwayTrack> m_SubwayTrackType;

		[ReadOnly]
		public ComponentTypeHandle<NetCondition> m_NetConditionType;

		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

		[ReadOnly]
		public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		[ReadOnly]
		public ComponentLookup<SecondaryFlow> m_SecondaryFlowData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_Edges;

		[ReadOnly]
		public ComponentLookup<Node> m_Nodes;

		[ReadOnly]
		public ComponentLookup<Temp> m_Temps;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public ComponentLookup<PathwayData> m_PrefabPathwayData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverageData;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_ResourceAvailabilityData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<ProcessEstimate> m_ProcessEstimates;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		public ComponentTypeHandle<EdgeColor> m_ColorType;

		[ReadOnly]
		public Entity m_ZonePrefab;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public NativeArray<GroundPollution> m_GroundPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<int> m_IndustrialDemands;

		[ReadOnly]
		public NativeArray<int> m_StorageDemands;

		[ReadOnly]
		public NativeList<IndustrialProcessData> m_Processes;

		[ReadOnly]
		public ZonePreferenceData m_ZonePreferences;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<EdgeColor> nativeArray = chunk.GetNativeArray(ref m_ColorType);
			InfoviewAvailabilityData availabilityData;
			InfomodeActive activeData2;
			InfoviewNetStatusData statusData;
			InfomodeActive activeData3;
			int index;
			if (chunk.Has(ref m_ServiceCoverageType) && GetServiceCoverageData(chunk, out var coverageData, out var activeData))
			{
				NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
				BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceCoverageType);
				EdgeColor value2 = default(EdgeColor);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
					if (CollectionUtils.TryGet(nativeArray2, i, out var value) && m_ServiceCoverageData.TryGetBuffer(value.m_Original, out var bufferData))
					{
						dynamicBuffer = bufferData;
					}
					if (dynamicBuffer.Length == 0)
					{
						nativeArray[i] = default(EdgeColor);
						continue;
					}
					Game.Net.ServiceCoverage serviceCoverage = dynamicBuffer[(int)coverageData.m_Service];
					value2.m_Index = (byte)activeData.m_Index;
					value2.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(coverageData, serviceCoverage.m_Coverage.x) * 255f), 0, 255);
					value2.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(coverageData, serviceCoverage.m_Coverage.y) * 255f), 0, 255);
					nativeArray[i] = value2;
				}
			}
			else if (chunk.Has(ref m_ResourceAvailabilityType) && GetResourceAvailabilityData(chunk, out availabilityData, out activeData2))
			{
				ZonePreferenceData preferences = m_ZonePreferences;
				NativeArray<Game.Net.Edge> nativeArray3 = chunk.GetNativeArray(ref m_EdgeType);
				NativeArray<Temp> nativeArray4 = chunk.GetNativeArray(ref m_TempType);
				BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceAvailabilityType);
				EdgeColor value4 = default(EdgeColor);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Game.Net.Edge edge = nativeArray3[j];
					DynamicBuffer<ResourceAvailability> availabilityBuffer = bufferAccessor2[j];
					float num;
					float num2;
					if (CollectionUtils.TryGet(nativeArray4, j, out var value3))
					{
						if (!m_Edges.TryGetComponent(value3.m_Original, out var componentData))
						{
							num = ((!m_Temps.TryGetComponent(edge.m_Start, out var componentData2) || !m_LandValues.TryGetComponent(componentData2.m_Original, out var componentData3)) ? m_LandValues[edge.m_Start].m_LandValue : componentData3.m_LandValue);
							num2 = ((!m_Temps.TryGetComponent(edge.m_End, out var componentData4) || !m_LandValues.TryGetComponent(componentData4.m_Original, out var componentData5)) ? m_LandValues[edge.m_End].m_LandValue : componentData5.m_LandValue);
						}
						else
						{
							edge = componentData;
							num = m_LandValues[componentData.m_Start].m_LandValue;
							num2 = m_LandValues[componentData.m_End].m_LandValue;
							if (m_ResourceAvailabilityData.TryGetBuffer(value3.m_Original, out var bufferData2))
							{
								availabilityBuffer = bufferData2;
							}
						}
					}
					else
					{
						num = m_LandValues[edge.m_Start].m_LandValue;
						num2 = m_LandValues[edge.m_End].m_LandValue;
					}
					if (availabilityBuffer.Length == 0)
					{
						nativeArray[j] = default(EdgeColor);
						continue;
					}
					float3 position = m_Nodes[edge.m_Start].m_Position;
					float3 position2 = m_Nodes[edge.m_End].m_Position;
					GroundPollution pollution = GroundPollutionSystem.GetPollution(position, m_GroundPollutionMap);
					GroundPollution pollution2 = GroundPollutionSystem.GetPollution(position2, m_GroundPollutionMap);
					NoisePollution pollution3 = NoisePollutionSystem.GetPollution(position, m_NoisePollutionMap);
					NoisePollution pollution4 = NoisePollutionSystem.GetPollution(position2, m_NoisePollutionMap);
					AirPollution pollution5 = AirPollutionSystem.GetPollution(position, m_AirPollutionMap);
					AirPollution pollution6 = AirPollutionSystem.GetPollution(position2, m_AirPollutionMap);
					m_ProcessEstimates.TryGetBuffer(m_ZonePrefab, out var bufferData3);
					if (m_ZonePropertiesDatas.TryGetComponent(m_ZonePrefab, out var componentData6))
					{
						float num3 = ((availabilityData.m_AreaType != AreaType.Residential) ? componentData6.m_SpaceMultiplier : (componentData6.m_ScaleResidentials ? componentData6.m_ResidentialProperties : (componentData6.m_ResidentialProperties / 8f)));
						num /= num3;
						num2 /= num3;
					}
					value4.m_Index = (byte)activeData2.m_Index;
					value4.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(availabilityData, availabilityBuffer, 0f, ref preferences, m_IndustrialDemands, m_StorageDemands, new float3(pollution.m_Pollution, pollution3.m_Pollution, pollution5.m_Pollution), num, bufferData3, m_Processes, m_ResourcePrefabs, m_ResourceDatas) * 255f), 0, 255);
					value4.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(availabilityData, availabilityBuffer, 1f, ref preferences, m_IndustrialDemands, m_StorageDemands, new float3(pollution2.m_Pollution, pollution4.m_Pollution, pollution6.m_Pollution), num2, bufferData3, m_Processes, m_ResourcePrefabs, m_ResourceDatas) * 255f), 0, 255);
					nativeArray[j] = value4;
				}
			}
			else if (GetNetStatusType(chunk, out statusData, out activeData3))
			{
				GetNetStatusColors(nativeArray, chunk, statusData, activeData3);
			}
			else if (GetNetGeometryColor(chunk, out index))
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					nativeArray[k] = new EdgeColor((byte)index, 0, 0);
				}
			}
			else
			{
				for (int l = 0; l < nativeArray.Length; l++)
				{
					nativeArray[l] = new EdgeColor(0, byte.MaxValue, byte.MaxValue);
				}
			}
		}

		private bool GetServiceCoverageData(ArchetypeChunk chunk, out InfoviewCoverageData coverageData, out InfomodeActive activeData)
		{
			coverageData = default(InfoviewCoverageData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewCoverageData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewCoverageType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						coverageData = nativeArray[j];
						coverageData.m_Service = CoverageService.Count;
						activeData = infomodeActive;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool GetResourceAvailabilityData(ArchetypeChunk chunk, out InfoviewAvailabilityData availabilityData, out InfomodeActive activeData)
		{
			availabilityData = default(InfoviewAvailabilityData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewAvailabilityData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewAvailabilityType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						availabilityData = nativeArray[j];
						activeData = infomodeActive;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool GetNetStatusType(ArchetypeChunk chunk, out InfoviewNetStatusData statusData, out InfomodeActive activeData)
		{
			statusData = default(InfoviewNetStatusData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewNetStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewNetStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						InfoviewNetStatusData infoviewNetStatusData = nativeArray[j];
						if (HasNetStatus(nativeArray[j], chunk))
						{
							statusData = infoviewNetStatusData;
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasNetStatus(InfoviewNetStatusData infoviewNetStatusData, ArchetypeChunk chunk)
		{
			return infoviewNetStatusData.m_Type switch
			{
				NetStatusType.Wear => chunk.Has(ref m_NetConditionType), 
				NetStatusType.TrafficFlow => chunk.Has(ref m_RoadType), 
				NetStatusType.NoisePollutionSource => chunk.Has(ref m_PollutionType), 
				NetStatusType.AirPollutionSource => chunk.Has(ref m_PollutionType), 
				NetStatusType.TrafficVolume => chunk.Has(ref m_RoadType), 
				NetStatusType.LeisureProvider => !chunk.Has(ref m_ServiceCoverageType), 
				NetStatusType.BicycleLanes => chunk.Has(ref m_SubLaneType), 
				NetStatusType.BicycleTrafficVolume => chunk.Has(ref m_SubLaneType), 
				_ => false, 
			};
		}

		private void GetNetStatusColors(NativeArray<EdgeColor> results, ArchetypeChunk chunk, InfoviewNetStatusData statusData, InfomodeActive activeData)
		{
			switch (statusData.m_Type)
			{
			case NetStatusType.Wear:
			{
				NativeArray<NetCondition> nativeArray9 = chunk.GetNativeArray(ref m_NetConditionType);
				EdgeColor value9 = default(EdgeColor);
				for (int num4 = 0; num4 < nativeArray9.Length; num4++)
				{
					NetCondition netCondition = nativeArray9[num4];
					value9.m_Index = (byte)activeData.m_Index;
					value9.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, netCondition.m_Wear.x / 10f) * 255f), 0, 255);
					value9.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, netCondition.m_Wear.y / 10f) * 255f), 0, 255);
					results[num4] = value9;
				}
				break;
			}
			case NetStatusType.TrafficFlow:
			{
				NativeArray<Road> nativeArray4 = chunk.GetNativeArray(ref m_RoadType);
				EdgeColor value5 = default(EdgeColor);
				for (int n = 0; n < nativeArray4.Length; n++)
				{
					Road road = nativeArray4[n];
					float4 trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(road.m_TrafficFlowDuration0, road.m_TrafficFlowDistance0);
					float4 trafficFlowSpeed2 = NetUtils.GetTrafficFlowSpeed(road.m_TrafficFlowDuration1, road.m_TrafficFlowDistance1);
					value5.m_Index = (byte)activeData.m_Index;
					value5.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed) * 0.125f + math.cmin(trafficFlowSpeed) * 0.5f) * 255f), 0, 255);
					value5.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed2) * 0.125f + math.cmin(trafficFlowSpeed2) * 0.5f) * 255f), 0, 255);
					results[n] = value5;
				}
				break;
			}
			case NetStatusType.NoisePollutionSource:
			{
				NativeArray<Game.Net.Pollution> nativeArray6 = chunk.GetNativeArray(ref m_PollutionType);
				NativeArray<EdgeGeometry> nativeArray7 = chunk.GetNativeArray(ref m_EdgeGeometryType);
				EdgeColor value7 = default(EdgeColor);
				for (int num2 = 0; num2 < nativeArray6.Length; num2++)
				{
					float status2 = nativeArray6[num2].m_Accumulation.x / math.max(0.1f, nativeArray7[num2].m_Start.middleLength + nativeArray7[num2].m_End.middleLength);
					value7.m_Index = (byte)activeData.m_Index;
					value7.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status2) * 255f), 0, 255);
					value7.m_Value1 = value7.m_Value0;
					results[num2] = value7;
				}
				break;
			}
			case NetStatusType.AirPollutionSource:
			{
				NativeArray<Game.Net.Pollution> nativeArray2 = chunk.GetNativeArray(ref m_PollutionType);
				NativeArray<EdgeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_EdgeGeometryType);
				EdgeColor value3 = default(EdgeColor);
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					float status = nativeArray2[k].m_Accumulation.y / math.max(0.1f, nativeArray3[k].m_Start.middleLength + nativeArray3[k].m_End.middleLength);
					value3.m_Index = (byte)activeData.m_Index;
					value3.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status) * 255f), 0, 255);
					value3.m_Value1 = value3.m_Value0;
					results[k] = value3;
				}
				break;
			}
			case NetStatusType.TrafficVolume:
			{
				NativeArray<Road> nativeArray8 = chunk.GetNativeArray(ref m_RoadType);
				EdgeColor value8 = default(EdgeColor);
				for (int num3 = 0; num3 < nativeArray8.Length; num3++)
				{
					Road road2 = nativeArray8[num3];
					float4 x3 = math.sqrt(road2.m_TrafficFlowDistance0 * 5.3333335f);
					float4 x4 = math.sqrt(road2.m_TrafficFlowDistance1 * 5.3333335f);
					value8.m_Index = (byte)activeData.m_Index;
					value8.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x3) * 0.25f) * 255f), 0, 255);
					value8.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x4) * 0.25f) * 255f), 0, 255);
					results[num3] = value8;
				}
				break;
			}
			case NetStatusType.LeisureProvider:
			{
				NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num = 0; num < nativeArray5.Length; num++)
				{
					EdgeColor value6 = new EdgeColor
					{
						m_Value0 = byte.MaxValue,
						m_Value1 = byte.MaxValue
					};
					if (m_PrefabPathwayData.TryGetComponent(nativeArray5[num].m_Prefab, out var componentData4) && componentData4.m_LeisureProvider)
					{
						value6.m_Index = (byte)activeData.m_Index;
					}
					results[num] = value6;
				}
				break;
			}
			case NetStatusType.BicycleLanes:
			{
				BufferAccessor<Game.Net.SubLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubLaneType);
				for (int l = 0; l < bufferAccessor2.Length; l++)
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = bufferAccessor2[l];
					EdgeColor value4 = new EdgeColor
					{
						m_Value0 = byte.MaxValue,
						m_Value1 = byte.MaxValue
					};
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						Game.Net.SubLane subLane2 = dynamicBuffer2[m];
						if ((subLane2.m_PathMethods & PathMethod.Bicycle) == 0)
						{
							continue;
						}
						value4.m_Index = (byte)activeData.m_Index;
						if ((subLane2.m_PathMethods & ~PathMethod.Bicycle) != 0)
						{
							if (m_CarLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData3) && (componentData3.m_Flags & CarLaneFlags.ForbidBicycles) != 0)
							{
								value4.m_Value0 = 127;
								value4.m_Value1 = 127;
								break;
							}
							value4.m_Value0 = 0;
							value4.m_Value1 = 0;
						}
					}
					results[l] = value4;
				}
				break;
			}
			case NetStatusType.BicycleTrafficVolume:
			{
				NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
				BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = bufferAccessor[i];
					float4 @float = 0f;
					float4 float2 = 0f;
					EdgeColor value = new EdgeColor
					{
						m_Value0 = byte.MaxValue,
						m_Value1 = byte.MaxValue
					};
					if (CollectionUtils.TryGet(nativeArray, i, out var value2) && m_SubLanes.TryGetBuffer(value2.m_Original, out var bufferData))
					{
						dynamicBuffer = bufferData;
					}
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Game.Net.SubLane subLane = dynamicBuffer[j];
						if ((subLane.m_PathMethods & PathMethod.Bicycle) != 0 && m_SecondaryFlowData.TryGetComponent(subLane.m_SubLane, out var componentData))
						{
							value.m_Index = (byte)activeData.m_Index;
							EdgeLane componentData2;
							float2 float3 = ((!m_EdgeLaneData.TryGetComponent(subLane.m_SubLane, out componentData2)) ? ((float2)1f) : math.select(0f, 1f, new bool2(math.any(componentData2.m_EdgeDelta == 0f), math.any(componentData2.m_EdgeDelta == 1f))));
							@float += componentData.m_Distance * float3.x;
							float2 += componentData.m_Distance * float3.y;
						}
					}
					if (value.m_Index == activeData.m_Index)
					{
						float4 x = math.sqrt(@float * 5.3333335f);
						float4 x2 = math.sqrt(float2 * 5.3333335f);
						value.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x) * 0.25f) * 255f), 0, 255);
						value.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x2) * 0.25f) * 255f), 0, 255);
						results[i] = value;
					}
					results[i] = value;
				}
				break;
			}
			case NetStatusType.LowVoltageFlow:
			case NetStatusType.HighVoltageFlow:
			case NetStatusType.PipeWaterFlow:
			case NetStatusType.PipeSewageFlow:
			case NetStatusType.OilFlow:
				break;
			}
		}

		private bool GetNetGeometryColor(ArchetypeChunk chunk, out int index)
		{
			index = 0;
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewNetGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewNetGeometryType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num && HasNetGeometryColor(nativeArray[j], chunk))
					{
						index = infomodeActive.m_Index;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasNetGeometryColor(InfoviewNetGeometryData infoviewNetGeometryData, ArchetypeChunk chunk)
		{
			return infoviewNetGeometryData.m_Type switch
			{
				NetType.TrainTrack => chunk.Has(ref m_TrainTrackType), 
				NetType.TramTrack => chunk.Has(ref m_TramTrackType), 
				NetType.Waterway => chunk.Has(ref m_WaterwayType), 
				NetType.SubwayTrack => chunk.Has(ref m_SubwayTrackType), 
				NetType.Road => chunk.Has(ref m_RoadType), 
				_ => false, 
			};
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateNodeColorsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<EdgeColor> m_ColorData;

		[ReadOnly]
		public ComponentLookup<SecondaryFlow> m_SecondaryFlowData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Roundabout> m_RoundaboutData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

		public ComponentTypeHandle<NodeColor> m_ColorType;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

		[ReadOnly]
		public ComponentTypeHandle<TrainTrack> m_TrainTrackType;

		[ReadOnly]
		public ComponentTypeHandle<TramTrack> m_TramTrackType;

		[ReadOnly]
		public ComponentTypeHandle<Waterway> m_WaterwayType;

		[ReadOnly]
		public ComponentTypeHandle<SubwayTrack> m_SubwayTrackType;

		[ReadOnly]
		public ComponentTypeHandle<NetCondition> m_NetConditionType;

		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

		[ReadOnly]
		public ComponentTypeHandle<Roundabout> m_RoundaboutType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<NodeColor> nativeArray2 = chunk.GetNativeArray(ref m_ColorType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			bool flag = false;
			bool forbidEdgeColor = false;
			int index;
			if (GetNetStatusType(chunk, out var statusData, out var activeData))
			{
				GetNetStatusColors(nativeArray2, chunk, statusData, activeData, out forbidEdgeColor);
				flag = true;
			}
			else if (GetNetGeometryColor(chunk, out index))
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					nativeArray2[i] = new NodeColor((byte)index, 0);
				}
				flag = true;
			}
			int3 @int = default(int3);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity = nativeArray[j];
				DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[j];
				if (CollectionUtils.TryGet(nativeArray3, j, out var value) && m_ConnectedEdges.TryGetBuffer(value.m_Original, out var bufferData))
				{
					entity = value.m_Original;
					dynamicBuffer = bufferData;
				}
				int3 falseValue = default(int3);
				int3 trueValue = default(int3);
				bool flag2 = flag;
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity edge = dynamicBuffer[k].m_Edge;
					if (!m_ColorData.HasComponent(edge))
					{
						continue;
					}
					Game.Net.Edge edge2 = m_EdgeData[edge];
					bool2 x = new bool2(edge2.m_Start == entity, edge2.m_End == entity);
					if (!math.any(x))
					{
						continue;
					}
					if (flag2)
					{
						EndNodeGeometry componentData2;
						if (x.x)
						{
							if (m_StartNodeGeometryData.TryGetComponent(edge, out var componentData))
							{
								flag2 = math.any(componentData.m_Geometry.m_Left.m_Length > 0.05f) | math.any(componentData.m_Geometry.m_Right.m_Length > 0.05f);
							}
						}
						else if (m_EndNodeGeometryData.TryGetComponent(edge, out componentData2))
						{
							flag2 = math.any(componentData2.m_Geometry.m_Left.m_Length > 0.05f) | math.any(componentData2.m_Geometry.m_Right.m_Length > 0.05f);
						}
					}
					EdgeColor edgeColor = m_ColorData[edge];
					if (edgeColor.m_Index != 0)
					{
						@int.x = edgeColor.m_Index;
						@int.y = (x.x ? edgeColor.m_Value0 : edgeColor.m_Value1);
						@int.z = 1;
						if ((@int.x == falseValue.x) | (falseValue.z == 0))
						{
							falseValue.x = @int.x;
							falseValue.yz += @int.yz;
						}
						else if ((@int.x == trueValue.x) | (trueValue.z == 0))
						{
							trueValue.x = @int.x;
							trueValue.yz += @int.yz;
						}
					}
				}
				if (!flag2)
				{
					falseValue = math.select(falseValue, trueValue, (trueValue.z > falseValue.z) | ((trueValue.z == falseValue.z) & (trueValue.x < falseValue.x)));
					if (falseValue.z > 0 && !forbidEdgeColor)
					{
						falseValue.y /= falseValue.z;
						nativeArray2[j] = new NodeColor((byte)falseValue.x, (byte)falseValue.y);
					}
					else
					{
						nativeArray2[j] = new NodeColor(0, byte.MaxValue);
					}
				}
			}
		}

		private bool GetNetStatusType(ArchetypeChunk chunk, out InfoviewNetStatusData statusData, out InfomodeActive activeData)
		{
			statusData = default(InfoviewNetStatusData);
			activeData = default(InfomodeActive);
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewNetStatusData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewNetStatusType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num)
					{
						InfoviewNetStatusData infoviewNetStatusData = nativeArray[j];
						if (HasNetStatus(nativeArray[j], chunk))
						{
							statusData = infoviewNetStatusData;
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasNetStatus(InfoviewNetStatusData infoviewNetStatusData, ArchetypeChunk chunk)
		{
			return infoviewNetStatusData.m_Type switch
			{
				NetStatusType.Wear => chunk.Has(ref m_NetConditionType), 
				NetStatusType.TrafficFlow => chunk.Has(ref m_RoadType), 
				NetStatusType.NoisePollutionSource => chunk.Has(ref m_PollutionType), 
				NetStatusType.AirPollutionSource => chunk.Has(ref m_PollutionType), 
				NetStatusType.TrafficVolume => chunk.Has(ref m_RoadType), 
				NetStatusType.BicycleLanes => chunk.Has(ref m_SubLaneType), 
				NetStatusType.BicycleTrafficVolume => chunk.Has(ref m_SubLaneType), 
				_ => false, 
			};
		}

		private float GetRelativeLength(Entity entity, DynamicBuffer<ConnectedEdge> edges)
		{
			float num = 0f;
			for (int i = 0; i < edges.Length; i++)
			{
				Entity edge = edges[i].m_Edge;
				Game.Net.Edge edge2 = m_EdgeData[edge];
				bool2 x = new bool2(edge2.m_Start == entity, edge2.m_End == entity);
				if (!math.any(x))
				{
					continue;
				}
				EdgeNodeGeometry edgeNodeGeometry = default(EdgeNodeGeometry);
				EndNodeGeometry componentData2;
				if (x.x)
				{
					if (m_StartNodeGeometryData.TryGetComponent(edge, out var componentData))
					{
						edgeNodeGeometry = componentData.m_Geometry;
					}
				}
				else if (m_EndNodeGeometryData.TryGetComponent(edge, out componentData2))
				{
					edgeNodeGeometry = componentData2.m_Geometry;
				}
				num = ((!(edgeNodeGeometry.m_MiddleRadius > 0f)) ? (num + (edgeNodeGeometry.m_Left.middleLength + edgeNodeGeometry.m_Right.middleLength) * 0.5f) : (num + (edgeNodeGeometry.m_Left.middleLength + edgeNodeGeometry.m_Right.middleLength)));
			}
			return num;
		}

		private void GetNetStatusColors(NativeArray<NodeColor> results, ArchetypeChunk chunk, InfoviewNetStatusData statusData, InfomodeActive activeData, out bool forbidEdgeColor)
		{
			forbidEdgeColor = false;
			switch (statusData.m_Type)
			{
			case NetStatusType.Wear:
			{
				NativeArray<NetCondition> nativeArray7 = chunk.GetNativeArray(ref m_NetConditionType);
				NodeColor value7 = default(NodeColor);
				for (int num3 = 0; num3 < nativeArray7.Length; num3++)
				{
					value7.m_Index = (byte)activeData.m_Index;
					value7.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.cmax(nativeArray7[num3].m_Wear) / 10f) * 255f), 0, 255);
					results[num3] = value7;
				}
				break;
			}
			case NetStatusType.TrafficFlow:
			{
				NativeArray<Road> nativeArray2 = chunk.GetNativeArray(ref m_RoadType);
				NodeColor value3 = default(NodeColor);
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					float4 trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(nativeArray2[k]);
					value3.m_Index = (byte)activeData.m_Index;
					value3.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed) * 0.125f + math.cmin(trafficFlowSpeed) * 0.5f) * 255f), 0, 255);
					results[k] = value3;
				}
				break;
			}
			case NetStatusType.NoisePollutionSource:
			{
				NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Game.Net.Pollution> nativeArray5 = chunk.GetNativeArray(ref m_PollutionType);
				BufferAccessor<ConnectedEdge> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				NodeColor value5 = default(NodeColor);
				for (int num = 0; num < nativeArray5.Length; num++)
				{
					float status = nativeArray5[num].m_Accumulation.x / math.max(0.1f, GetRelativeLength(nativeArray4[num], bufferAccessor4[num]));
					value5.m_Index = (byte)activeData.m_Index;
					value5.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status) * 255f), 0, 255);
					results[num] = value5;
				}
				break;
			}
			case NetStatusType.AirPollutionSource:
			{
				NativeArray<Entity> nativeArray8 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Game.Net.Pollution> nativeArray9 = chunk.GetNativeArray(ref m_PollutionType);
				BufferAccessor<ConnectedEdge> bufferAccessor5 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				NodeColor value8 = default(NodeColor);
				for (int num4 = 0; num4 < nativeArray9.Length; num4++)
				{
					float status2 = nativeArray9[num4].m_Accumulation.y / math.max(0.1f, GetRelativeLength(nativeArray8[num4], bufferAccessor5[num4]));
					value8.m_Index = (byte)activeData.m_Index;
					value8.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status2) * 255f), 0, 255);
					results[num4] = value8;
				}
				break;
			}
			case NetStatusType.TrafficVolume:
			{
				NativeArray<Road> nativeArray6 = chunk.GetNativeArray(ref m_RoadType);
				NodeColor value6 = default(NodeColor);
				for (int num2 = 0; num2 < nativeArray6.Length; num2++)
				{
					Road road = nativeArray6[num2];
					float4 x2 = math.sqrt((road.m_TrafficFlowDistance0 + road.m_TrafficFlowDistance1) * 2.6666667f);
					value6.m_Index = (byte)activeData.m_Index;
					value6.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x2) * 0.25f) * 255f), 0, 255);
					results[num2] = value6;
				}
				break;
			}
			case NetStatusType.BicycleLanes:
			{
				NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
				BufferAccessor<Game.Net.SubLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubLaneType);
				BufferAccessor<ConnectedEdge> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
				forbidEdgeColor = true;
				for (int l = 0; l < bufferAccessor2.Length; l++)
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = bufferAccessor2[l];
					NodeColor value4 = new NodeColor
					{
						m_Value = byte.MaxValue
					};
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						Game.Net.SubLane subLane2 = dynamicBuffer2[m];
						if ((subLane2.m_PathMethods & PathMethod.Bicycle) != 0)
						{
							value4.m_Index = (byte)activeData.m_Index;
							if ((subLane2.m_PathMethods & ~PathMethod.Bicycle) != 0)
							{
								value4.m_Value = 0;
								break;
							}
						}
					}
					if (value4.m_Index == activeData.m_Index)
					{
						Entity entity = nativeArray3[l];
						DynamicBuffer<ConnectedEdge> dynamicBuffer3 = bufferAccessor3[l];
						for (int n = 0; n < dynamicBuffer3.Length; n++)
						{
							Entity edge = dynamicBuffer3[n].m_Edge;
							if (!m_ColorData.TryGetComponent(edge, out var componentData2))
							{
								continue;
							}
							Game.Net.Edge edge2 = m_EdgeData[edge];
							if (math.any(new bool2(edge2.m_Start == entity, edge2.m_End == entity)) && componentData2.m_Index == value4.m_Index)
							{
								if (componentData2.m_Value0 == 0)
								{
									value4.m_Value = 0;
									break;
								}
								if (componentData2.m_Value0 == 127)
								{
									value4.m_Value = 127;
								}
							}
						}
					}
					results[l] = value4;
				}
				break;
			}
			case NetStatusType.BicycleTrafficVolume:
			{
				NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
				BufferAccessor<Game.Net.SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
				bool flag = chunk.Has(ref m_RoundaboutType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = bufferAccessor[i];
					float4 @float = 0f;
					NodeColor value = new NodeColor
					{
						m_Value = byte.MaxValue
					};
					bool flag2 = flag;
					if (CollectionUtils.TryGet(nativeArray, i, out var value2) && m_SubLanes.TryGetBuffer(value2.m_Original, out var bufferData))
					{
						dynamicBuffer = bufferData;
						flag2 = m_RoundaboutData.HasComponent(value2.m_Original);
					}
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Game.Net.SubLane subLane = dynamicBuffer[j];
						if ((subLane.m_PathMethods & PathMethod.Bicycle) != 0 && m_SecondaryFlowData.TryGetComponent(subLane.m_SubLane, out var componentData))
						{
							value.m_Index = (byte)activeData.m_Index;
							@float += componentData.m_Distance;
						}
					}
					if (value.m_Index == activeData.m_Index)
					{
						if (flag2)
						{
							@float *= 1f / 3f;
						}
						float4 x = math.sqrt(@float * 5.3333335f);
						value.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x) * 0.25f) * 255f), 0, 255);
					}
					results[i] = value;
				}
				break;
			}
			case NetStatusType.LowVoltageFlow:
			case NetStatusType.HighVoltageFlow:
			case NetStatusType.PipeWaterFlow:
			case NetStatusType.PipeSewageFlow:
			case NetStatusType.OilFlow:
			case NetStatusType.LeisureProvider:
				break;
			}
		}

		private bool GetNetGeometryColor(ArchetypeChunk chunk, out int index)
		{
			index = 0;
			int num = int.MaxValue;
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<InfoviewNetGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref m_InfoviewNetGeometryType);
				if (nativeArray.Length == 0)
				{
					continue;
				}
				NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					InfomodeActive infomodeActive = nativeArray2[j];
					int priority = infomodeActive.m_Priority;
					if (priority < num && HasNetGeometryColor(nativeArray[j], chunk))
					{
						index = infomodeActive.m_Index;
						num = priority;
					}
				}
			}
			return num != int.MaxValue;
		}

		private bool HasNetGeometryColor(InfoviewNetGeometryData infoviewNetGeometryData, ArchetypeChunk chunk)
		{
			return infoviewNetGeometryData.m_Type switch
			{
				NetType.TrainTrack => chunk.Has(ref m_TrainTrackType), 
				NetType.TramTrack => chunk.Has(ref m_TramTrackType), 
				NetType.Waterway => chunk.Has(ref m_WaterwayType), 
				NetType.SubwayTrack => chunk.Has(ref m_SubwayTrackType), 
				NetType.Road => chunk.Has(ref m_RoadType), 
				_ => false, 
			};
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateEdgeColors2Job : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<NodeColor> m_ColorData;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> m_StartNodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> m_EndNodeGeometryType;

		public ComponentTypeHandle<EdgeColor> m_ColorType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Net.Edge> nativeArray = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<StartNodeGeometry> nativeArray2 = chunk.GetNativeArray(ref m_StartNodeGeometryType);
			NativeArray<EndNodeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_EndNodeGeometryType);
			NativeArray<EdgeColor> nativeArray4 = chunk.GetNativeArray(ref m_ColorType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Net.Edge edge = nativeArray[i];
				EdgeColor value = nativeArray4[i];
				bool2 @bool = false;
				if (nativeArray2.Length != 0)
				{
					StartNodeGeometry startNodeGeometry = nativeArray2[i];
					if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
					{
						@bool.x = true;
					}
				}
				if (nativeArray3.Length != 0)
				{
					EndNodeGeometry endNodeGeometry = nativeArray3[i];
					if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
					{
						@bool.y = true;
					}
				}
				if (!@bool.x && m_ColorData.TryGetComponent(edge.m_Start, out var componentData) && componentData.m_Index == value.m_Index)
				{
					value.m_Value0 = componentData.m_Value;
				}
				if (!@bool.y && m_ColorData.TryGetComponent(edge.m_End, out var componentData2) && componentData2.m_Index == value.m_Index)
				{
					value.m_Value1 = componentData2.m_Value;
				}
				nativeArray4[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct LaneColorJob : IJobChunk
	{
		private interface IFlowImplementation
		{
			Entity sinkNode { get; }

			bool subObjects { get; }

			bool connectedBuildings { get; }

			int multiplier { get; }

			bool TryGetFlowNode(Entity entity, out Entity flowNode);

			bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning);

			void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning);
		}

		private struct ElectricityFlow : IFlowImplementation
		{
			[ReadOnly]
			public ComponentLookup<ElectricityNodeConnection> m_NodeConnectionData;

			[ReadOnly]
			public ComponentLookup<ElectricityFlowEdge> m_FlowEdgeData;

			[ReadOnly]
			public ComponentLookup<ElectricityBuildingConnection> m_BuildingConnectionData;

			[ReadOnly]
			public ComponentLookup<ElectricityConsumer> m_ConsumerData;

			[ReadOnly]
			public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

			public Entity sinkNode { get; set; }

			public bool subObjects => false;

			public bool connectedBuildings => true;

			public int multiplier => 1;

			public bool TryGetFlowNode(Entity entity, out Entity flowNode)
			{
				if (m_NodeConnectionData.TryGetComponent(entity, out var componentData))
				{
					flowNode = componentData.m_ElectricityNode;
					return true;
				}
				flowNode = default(Entity);
				return false;
			}

			public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
			{
				if (ElectricityGraphUtils.TryGetFlowEdge(startNode, endNode, ref m_ConnectedFlowEdges, ref m_FlowEdgeData, out ElectricityFlowEdge edge))
				{
					flow = edge.m_Flow;
					capacity = edge.m_Capacity;
					warning = math.select(0f, 0.75f, (edge.m_Flags & ElectricityFlowEdgeFlags.BeyondBottleneck) != 0);
					warning = math.select(warning, 1f, (edge.m_Flags & ElectricityFlowEdgeFlags.Bottleneck) != 0);
					return true;
				}
				flow = (capacity = 0);
				warning = 0f;
				return false;
			}

			public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
			{
				if (m_ConsumerData.TryGetComponent(building, out var componentData) && !m_BuildingConnectionData.HasComponent(building))
				{
					wantedConsumption = componentData.m_WantedConsumption;
					fulfilledConsumption = componentData.m_FulfilledConsumption;
					warning = math.select(0f, 0.75f, (componentData.m_Flags & ElectricityConsumerFlags.BottleneckWarning) != 0);
				}
				else
				{
					wantedConsumption = (fulfilledConsumption = 0);
					warning = 0f;
				}
			}
		}

		private struct WaterFlow : IFlowImplementation
		{
			[ReadOnly]
			public ComponentLookup<WaterPipeNodeConnection> m_NodeConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterPipeEdge> m_FlowEdgeData;

			[ReadOnly]
			public ComponentLookup<WaterPipeBuildingConnection> m_BuildingConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterConsumer> m_ConsumerData;

			[ReadOnly]
			public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

			public float m_MaxToleratedPollution;

			public Entity sinkNode { get; set; }

			public bool subObjects => false;

			public bool connectedBuildings => true;

			public int multiplier => 1;

			public bool TryGetFlowNode(Entity entity, out Entity flowNode)
			{
				if (m_NodeConnectionData.TryGetComponent(entity, out var componentData))
				{
					flowNode = componentData.m_WaterPipeNode;
					return true;
				}
				flowNode = default(Entity);
				return false;
			}

			public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
			{
				if (WaterPipeGraphUtils.TryGetFlowEdge(startNode, endNode, ref m_ConnectedFlowEdges, ref m_FlowEdgeData, out WaterPipeEdge edge))
				{
					flow = edge.m_FreshFlow;
					capacity = 10000;
					warning = math.saturate(edge.m_FreshPollution / m_MaxToleratedPollution);
					return true;
				}
				flow = (capacity = 0);
				warning = 0f;
				return false;
			}

			public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
			{
				if (m_ConsumerData.TryGetComponent(building, out var componentData) && !m_BuildingConnectionData.HasComponent(building))
				{
					wantedConsumption = componentData.m_WantedConsumption;
					fulfilledConsumption = componentData.m_FulfilledFresh;
					warning = math.select(0f, 1f, componentData.m_Pollution > 0f);
				}
				else
				{
					wantedConsumption = (fulfilledConsumption = 0);
					warning = 0f;
				}
			}
		}

		private struct SewageFlow : IFlowImplementation
		{
			[ReadOnly]
			public ComponentLookup<WaterPipeNodeConnection> m_NodeConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterPipeEdge> m_FlowEdgeData;

			[ReadOnly]
			public ComponentLookup<WaterPipeBuildingConnection> m_BuildingConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterConsumer> m_ConsumerData;

			[ReadOnly]
			public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

			public Entity sinkNode { get; set; }

			public bool subObjects => false;

			public bool connectedBuildings => true;

			public int multiplier => -1;

			public bool TryGetFlowNode(Entity entity, out Entity flowNode)
			{
				if (m_NodeConnectionData.TryGetComponent(entity, out var componentData))
				{
					flowNode = componentData.m_WaterPipeNode;
					return true;
				}
				flowNode = default(Entity);
				return false;
			}

			public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
			{
				if (WaterPipeGraphUtils.TryGetFlowEdge(startNode, endNode, ref m_ConnectedFlowEdges, ref m_FlowEdgeData, out WaterPipeEdge edge))
				{
					flow = edge.m_SewageFlow;
					capacity = 10000;
					warning = 0f;
					return true;
				}
				flow = (capacity = 0);
				warning = 0f;
				return false;
			}

			public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
			{
				if (m_ConsumerData.TryGetComponent(building, out var componentData) && !m_BuildingConnectionData.HasComponent(building))
				{
					wantedConsumption = componentData.m_WantedConsumption;
					fulfilledConsumption = componentData.m_FulfilledSewage;
				}
				else
				{
					wantedConsumption = (fulfilledConsumption = 0);
				}
				warning = 0f;
			}
		}

		private struct ResourceFlow : IFlowImplementation
		{
			[ReadOnly]
			public ComponentLookup<Game.Net.Edge> m_EdgeData;

			[ReadOnly]
			public ComponentLookup<Game.Net.ResourceConnection> m_ResourceConnectionData;

			[ReadOnly]
			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			public Entity sinkNode { get; set; }

			public bool subObjects => true;

			public bool connectedBuildings => false;

			public int multiplier => -1;

			public bool TryGetFlowNode(Entity entity, out Entity flowNode)
			{
				if (m_ResourceConnectionData.HasComponent(entity))
				{
					flowNode = entity;
					return true;
				}
				flowNode = default(Entity);
				return false;
			}

			public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
			{
				int num = 0;
				if (m_EdgeData.TryGetComponent(startNode, out var componentData))
				{
					CommonUtils.Swap(ref startNode, ref endNode);
					num = -1;
				}
				else if (m_EdgeData.TryGetComponent(endNode, out componentData))
				{
					num = 1;
				}
				if (num != 0)
				{
					flow = 0;
					if (startNode == componentData.m_Start)
					{
						if (m_ResourceConnectionData.TryGetComponent(endNode, out var componentData2))
						{
							flow = componentData2.m_Flow.x;
						}
					}
					else if (startNode == componentData.m_End)
					{
						if (m_ResourceConnectionData.TryGetComponent(endNode, out var componentData3))
						{
							flow = -componentData3.m_Flow.y;
						}
					}
					else
					{
						bool flag = false;
						if (m_ConnectedEdges.TryGetBuffer(startNode, out var bufferData))
						{
							for (int i = 0; i < bufferData.Length; i++)
							{
								ConnectedEdge connectedEdge = bufferData[i];
								if (connectedEdge.m_Edge == endNode)
								{
									continue;
								}
								componentData = m_EdgeData[connectedEdge.m_Edge];
								if (componentData.m_Start == startNode)
								{
									if (m_ResourceConnectionData.TryGetComponent(connectedEdge.m_Edge, out var componentData4))
									{
										flow = -componentData4.m_Flow.x;
										flag = true;
									}
									break;
								}
								if (componentData.m_End == startNode)
								{
									if (m_ResourceConnectionData.TryGetComponent(connectedEdge.m_Edge, out var componentData5))
									{
										flow = componentData5.m_Flow.y;
										flag = true;
									}
									break;
								}
							}
						}
						if (!flag && m_ResourceConnectionData.TryGetComponent(startNode, out var componentData6))
						{
							flow = -componentData6.m_Flow.x;
						}
					}
					flow *= num;
					capacity = 100;
					warning = 0f;
					return true;
				}
				flow = (capacity = 0);
				warning = 0f;
				return false;
			}

			public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
			{
				wantedConsumption = (fulfilledConsumption = 0);
				warning = 0f;
			}
		}

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<NodeLane> m_NodeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> m_UtilityLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.SecondaryLane> m_SecondaryLaneType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeMapping> m_EdgeMappingType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<LaneColor> m_ColorType;

		public BufferTypeHandle<SubFlow> m_SubFlowType;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> m_ResourceConnectionData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> m_ObjectColorData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<NodeColor> m_NodeColorData;

		[ReadOnly]
		public ComponentLookup<EdgeColor> m_EdgeColorData;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnectionData;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_ElectricityFlowEdgeData;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> m_ElectricityBuildingConnectionData;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> m_WaterPipeNodeConnectionData;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> m_WaterPipeEdgeData;

		[ReadOnly]
		public ComponentLookup<WaterPipeBuildingConnection> m_WaterPipeBuildingConnectionData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerData;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

		public Entity m_ElectricitySinkNode;

		public Entity m_WaterSinkNode;

		public WaterPipeParameterData m_WaterPipeParameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<LaneColor> nativeArray = chunk.GetNativeArray(ref m_ColorType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<EdgeLane> nativeArray4 = chunk.GetNativeArray(ref m_EdgeLaneType);
			NativeArray<NodeLane> nativeArray5 = chunk.GetNativeArray(ref m_NodeLaneType);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			float num9 = 0f;
			float num10 = 0f;
			float num11 = 0f;
			float num12 = 0f;
			float num13 = 0f;
			bool flag = chunk.Has(ref m_CarLaneType);
			bool flag2 = chunk.Has(ref m_TrackLaneType);
			bool flag3 = chunk.Has(ref m_UtilityLaneType);
			bool flag4 = chunk.Has(ref m_SecondaryLaneType);
			bool flag5 = chunk.Has(ref m_TempType);
			NativeArray<EdgeMapping> nativeArray6 = default(NativeArray<EdgeMapping>);
			NativeArray<PrefabRef> nativeArray7 = default(NativeArray<PrefabRef>);
			BufferAccessor<SubFlow> bufferAccessor = default(BufferAccessor<SubFlow>);
			if (flag)
			{
				nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				int num14 = int.MaxValue;
				for (int i = 0; i < m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
					NativeArray<InfoviewNetStatusData> nativeArray8 = archetypeChunk.GetNativeArray(ref m_InfoviewNetStatusType);
					if (nativeArray8.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray9 = archetypeChunk.GetNativeArray(ref m_InfomodeActiveType);
					for (int j = 0; j < nativeArray8.Length; j++)
					{
						InfoviewNetStatusData infoviewNetStatusData = nativeArray8[j];
						InfomodeActive infomodeActive = nativeArray9[j];
						int priority = infomodeActive.m_Priority;
						if (infoviewNetStatusData.m_Type == NetStatusType.BicycleLanes && priority < num14)
						{
							num = infomodeActive.m_Index;
							num14 = priority;
						}
					}
				}
			}
			if (flag2)
			{
				nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				int num15 = int.MaxValue;
				int num16 = int.MaxValue;
				for (int k = 0; k < m_InfomodeChunks.Length; k++)
				{
					ArchetypeChunk archetypeChunk2 = m_InfomodeChunks[k];
					NativeArray<InfoviewNetGeometryData> nativeArray10 = archetypeChunk2.GetNativeArray(ref m_InfoviewNetGeometryType);
					if (nativeArray10.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray11 = archetypeChunk2.GetNativeArray(ref m_InfomodeActiveType);
					for (int l = 0; l < nativeArray10.Length; l++)
					{
						InfoviewNetGeometryData infoviewNetGeometryData = nativeArray10[l];
						InfomodeActive infomodeActive2 = nativeArray11[l];
						int priority2 = infomodeActive2.m_Priority;
						switch (infoviewNetGeometryData.m_Type)
						{
						case NetType.TrainTrack:
							if (priority2 < num15)
							{
								num2 = infomodeActive2.m_Index;
								num15 = priority2;
							}
							break;
						case NetType.TramTrack:
							if (priority2 < num16)
							{
								num3 = infomodeActive2.m_Index;
								num16 = priority2;
							}
							break;
						}
					}
				}
			}
			if (flag3)
			{
				nativeArray6 = chunk.GetNativeArray(ref m_EdgeMappingType);
				nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				bufferAccessor = chunk.GetBufferAccessor(ref m_SubFlowType);
				int num17 = int.MaxValue;
				int num18 = int.MaxValue;
				int num19 = int.MaxValue;
				int num20 = int.MaxValue;
				int num21 = int.MaxValue;
				for (int m = 0; m < m_InfomodeChunks.Length; m++)
				{
					ArchetypeChunk archetypeChunk3 = m_InfomodeChunks[m];
					NativeArray<InfoviewNetStatusData> nativeArray12 = archetypeChunk3.GetNativeArray(ref m_InfoviewNetStatusType);
					if (nativeArray12.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray13 = archetypeChunk3.GetNativeArray(ref m_InfomodeActiveType);
					for (int n = 0; n < nativeArray12.Length; n++)
					{
						InfoviewNetStatusData infoviewNetStatusData2 = nativeArray12[n];
						InfomodeActive infomodeActive3 = nativeArray13[n];
						int priority3 = infomodeActive3.m_Priority;
						switch (infoviewNetStatusData2.m_Type)
						{
						case NetStatusType.LowVoltageFlow:
							if (priority3 < num17)
							{
								num4 = infomodeActive3.m_Index;
								num9 = infoviewNetStatusData2.m_Tiling;
								num17 = priority3;
							}
							break;
						case NetStatusType.HighVoltageFlow:
							if (priority3 < num18)
							{
								num5 = infomodeActive3.m_Index;
								num10 = infoviewNetStatusData2.m_Tiling;
								num18 = priority3;
							}
							break;
						case NetStatusType.PipeWaterFlow:
							if (priority3 < num19)
							{
								num6 = infomodeActive3.m_Index;
								num11 = infoviewNetStatusData2.m_Tiling;
								num19 = priority3;
							}
							break;
						case NetStatusType.PipeSewageFlow:
							if (priority3 < num20)
							{
								num7 = infomodeActive3.m_Index;
								num12 = infoviewNetStatusData2.m_Tiling;
								num20 = priority3;
							}
							break;
						case NetStatusType.OilFlow:
							if (priority3 < num21)
							{
								num8 = infomodeActive3.m_Index;
								num13 = infoviewNetStatusData2.m_Tiling;
								num21 = priority3;
							}
							break;
						}
					}
				}
			}
			bool flag6 = flag && num != 0;
			bool flag7 = flag2 && (num2 != 0 || num3 != 0);
			bool flag8 = flag3 && bufferAccessor.Length != 0 && (num4 != 0 || num5 != 0 || num6 != 0 || num7 != 0 || num8 != 0);
			for (int num22 = 0; num22 < nativeArray.Length; num22++)
			{
				if (flag6)
				{
					PrefabRef prefabRef = nativeArray7[num22];
					if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && componentData.m_RoadTypes == RoadTypes.Bicycle && num != 0)
					{
						nativeArray[num22] = new LaneColor((byte)num, byte.MaxValue, byte.MaxValue);
						continue;
					}
				}
				if (flag7)
				{
					PrefabRef prefabRef2 = nativeArray7[num22];
					if (m_PrefabTrackLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						if ((componentData2.m_TrackTypes & TrackTypes.Train) != 0 && num2 != 0)
						{
							nativeArray[num22] = new LaneColor((byte)num2, 0, 0);
							continue;
						}
						if ((componentData2.m_TrackTypes & TrackTypes.Tram) != 0 && num3 != 0)
						{
							nativeArray[num22] = new LaneColor((byte)num3, 0, 0);
							continue;
						}
					}
				}
				if (flag8)
				{
					PrefabRef prefabRef3 = nativeArray7[num22];
					if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef3.m_Prefab, out var componentData3))
					{
						int num23 = 0;
						float num24 = 0f;
						if ((componentData3.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None && num4 != 0)
						{
							num23 = num4;
							num24 = num9;
						}
						else if ((componentData3.m_UtilityTypes & UtilityTypes.HighVoltageLine) != UtilityTypes.None && num5 != 0)
						{
							num23 = num5;
							num24 = num10;
						}
						else if ((componentData3.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None && num6 != 0)
						{
							num23 = num6;
							num24 = num11;
						}
						else if ((componentData3.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None && num7 != 0)
						{
							num23 = num7;
							num24 = num12;
						}
						else if ((componentData3.m_UtilityTypes & UtilityTypes.Resource) != UtilityTypes.None && num8 != 0)
						{
							num23 = num8;
							num24 = num13;
						}
						if (num23 != 0)
						{
							Curve curve = nativeArray3[num22];
							EdgeMapping edgeMapping = nativeArray6[num22];
							DynamicBuffer<SubFlow> dynamicBuffer = bufferAccessor[num22];
							Owner owner = default(Owner);
							if (nativeArray2.Length != 0)
							{
								owner = nativeArray2[num22];
							}
							if (dynamicBuffer.Length != 16)
							{
								dynamicBuffer.ResizeUninitialized(16);
							}
							NativeArray<SubFlow> nativeArray14 = dynamicBuffer.AsNativeArray();
							float warning = 0f;
							if (edgeMapping.m_Parent1 != Entity.Null)
							{
								if (m_EdgeData.HasComponent(edgeMapping.m_Parent1))
								{
									if (flag5)
									{
										if (edgeMapping.m_Parent2 != Entity.Null)
										{
											MathUtils.Divide(curve.m_Bezier, out var output, out var output2, 0.5f);
											GetOriginalEdge(output, ref edgeMapping.m_Parent1, ref edgeMapping.m_CurveDelta1);
											GetOriginalEdge(output2, ref edgeMapping.m_Parent2, ref edgeMapping.m_CurveDelta2);
										}
										else
										{
											GetOriginalEdge(curve.m_Bezier, ref edgeMapping.m_Parent1, ref edgeMapping.m_CurveDelta1);
										}
									}
									if (num23 == num4 || num23 == num5)
									{
										FillEdgeFlow(GetElectricityFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num6)
									{
										FillEdgeFlow(GetWaterFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num7)
									{
										FillEdgeFlow(GetSewageFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num8)
									{
										FillEdgeFlow(GetResourceFlow(), nativeArray14, edgeMapping, out warning);
									}
									else
									{
										nativeArray14.Fill(default(SubFlow));
									}
								}
								else
								{
									if (flag5)
									{
										GetOriginalNode(ref edgeMapping.m_Parent1);
										GetOriginalEdge(curve.m_Bezier, ref edgeMapping.m_Parent2, ref edgeMapping.m_CurveDelta2);
									}
									if (num23 == num4 || num23 == num5)
									{
										FillNodeFlow(GetElectricityFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num6)
									{
										FillNodeFlow(GetWaterFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num7)
									{
										FillNodeFlow(GetSewageFlow(), nativeArray14, edgeMapping, out warning);
									}
									else if (num23 == num8)
									{
										FillNodeFlow(GetResourceFlow(), nativeArray14, edgeMapping, out warning);
									}
									else
									{
										nativeArray14.Fill(default(SubFlow));
									}
								}
							}
							else if (flag4)
							{
								if (num23 == num4 || num23 == num5)
								{
									FillBuildingFlow(GetElectricityFlow(), nativeArray14, owner.m_Owner, out warning);
								}
								else if (num23 == num6)
								{
									FillBuildingFlow(GetWaterFlow(), nativeArray14, owner.m_Owner, out warning);
								}
								else if (num23 == num7)
								{
									FillBuildingFlow(GetSewageFlow(), nativeArray14, owner.m_Owner, out warning);
								}
								else
								{
									nativeArray14.Fill(default(SubFlow));
								}
							}
							else
							{
								num23 = 0;
							}
							if (num23 != 0)
							{
								int2 @int = new int2(dynamicBuffer[0].m_Value, dynamicBuffer[15].m_Value);
								bool flag9 = (((@int.x ^ @int.y) & 0x80) != 0) & math.all(@int != 0);
								int num25 = math.clamp(Mathf.RoundToInt(curve.m_Length * num24), 1, 255);
								int num26 = math.clamp(Mathf.RoundToInt(warning * 255f), 0, 255);
								num25 = math.select(num25, 2, num25 == 1 && flag9);
								nativeArray[num22] = new LaneColor((byte)num23, (byte)num25, (byte)num26);
								continue;
							}
						}
					}
				}
				if (nativeArray2.Length != 0)
				{
					Owner owner2 = nativeArray2[num22];
					if (nativeArray4.Length != 0)
					{
						if (m_EdgeColorData.TryGetComponent(owner2.m_Owner, out var componentData4))
						{
							nativeArray[num22] = new LaneColor(componentData4.m_Index, componentData4.m_Value0, componentData4.m_Value1);
							continue;
						}
					}
					else if (nativeArray5.Length != 0)
					{
						if (m_NodeColorData.TryGetComponent(owner2.m_Owner, out var componentData5))
						{
							nativeArray[num22] = new LaneColor(componentData5.m_Index, componentData5.m_Value, componentData5.m_Value);
							continue;
						}
					}
					else
					{
						PrefabRef prefabRef4 = nativeArray7[num22];
						if ((m_PrefabNetLaneData[prefabRef4.m_Prefab].m_Flags & LaneFlags.Underground) == 0)
						{
							Game.Objects.Color componentData6;
							while (!m_ObjectColorData.TryGetComponent(owner2.m_Owner, out componentData6))
							{
								if (m_OwnerData.TryGetComponent(owner2.m_Owner, out var componentData7))
								{
									owner2 = componentData7;
									continue;
								}
								goto IL_0ac6;
							}
							if (componentData6.m_SubColor)
							{
								nativeArray[num22] = new LaneColor(componentData6.m_Index, componentData6.m_Value, componentData6.m_Value);
								continue;
							}
						}
					}
				}
				goto IL_0ac6;
				IL_0ac6:
				nativeArray[num22] = default(LaneColor);
			}
		}

		private void GetOriginalEdge(Bezier4x3 laneCurve, ref Entity parent, ref float2 curveMapping)
		{
			if (!m_TempData.TryGetComponent(parent, out var componentData))
			{
				return;
			}
			Game.Net.Edge componentData2;
			Temp componentData3;
			Temp componentData4;
			if (componentData.m_Original != Entity.Null)
			{
				parent = componentData.m_Original;
			}
			else if (m_EdgeData.TryGetComponent(parent, out componentData2) && m_TempData.TryGetComponent(componentData2.m_Start, out componentData3) && m_TempData.TryGetComponent(componentData2.m_End, out componentData4) && componentData3.m_Original != Entity.Null && componentData4.m_Original != Entity.Null)
			{
				Curve componentData6;
				if (m_CurveData.TryGetComponent(componentData3.m_Original, out var componentData5))
				{
					parent = componentData3.m_Original;
					MathUtils.Distance(componentData5.m_Bezier.xz, laneCurve.a.xz, out curveMapping.x);
					MathUtils.Distance(componentData5.m_Bezier.xz, laneCurve.d.xz, out curveMapping.y);
				}
				else if (m_CurveData.TryGetComponent(componentData4.m_Original, out componentData6))
				{
					parent = componentData4.m_Original;
					MathUtils.Distance(componentData6.m_Bezier.xz, laneCurve.a.xz, out curveMapping.x);
					MathUtils.Distance(componentData6.m_Bezier.xz, laneCurve.d.xz, out curveMapping.y);
				}
			}
		}

		private void GetOriginalNode(ref Entity parent)
		{
			if (m_TempData.TryGetComponent(parent, out var componentData))
			{
				parent = componentData.m_Original;
			}
		}

		private void FillEdgeFlow<T>(T impl, NativeArray<SubFlow> flowArray, EdgeMapping edgeMapping, out float warning) where T : struct, IFlowImplementation
		{
			if (edgeMapping.m_Parent2 != Entity.Null)
			{
				FillEdgeFlow(impl, flowArray.GetSubArray(0, 8), edgeMapping.m_Parent1, edgeMapping.m_CurveDelta1, out warning);
				FillEdgeFlow(impl, flowArray.GetSubArray(8, 8), edgeMapping.m_Parent2, edgeMapping.m_CurveDelta2, out warning);
			}
			else
			{
				FillEdgeFlow(impl, flowArray, edgeMapping.m_Parent1, edgeMapping.m_CurveDelta1, out warning);
			}
		}

		private unsafe void FillEdgeFlow<T>(T impl, NativeArray<SubFlow> flows, Entity edge, float2 curveMapping, out float warning) where T : struct, IFlowImplementation
		{
			if (m_EdgeData.TryGetComponent(edge, out var componentData) && impl.TryGetFlowNode(edge, out var flowNode) && impl.TryGetFlowNode(componentData.m_Start, out var flowNode2) && impl.TryGetFlowNode(componentData.m_End, out var flowNode3) && impl.TryGetFlowEdge(flowNode2, flowNode, out var flow, out var capacity, out var warning2) && impl.TryGetFlowEdge(flowNode, flowNode3, out var flow2, out var capacity2, out var warning3))
			{
				capacity = math.max(1, capacity);
				if (curveMapping.y < curveMapping.x)
				{
					capacity2 = -flow2;
					int num = -flow;
					flow = capacity2;
					flow2 = num;
				}
				int* ptr = stackalloc int[flows.Length];
				float warning4;
				if (m_ConnectedNodes.TryGetBuffer(edge, out var bufferData))
				{
					foreach (ConnectedNode item in bufferData)
					{
						if (impl.TryGetFlowNode(item.m_Node, out var flowNode4) && impl.TryGetFlowEdge(flowNode4, flowNode, out var flow3, out capacity2, out warning4))
						{
							AddTempFlow(flow3, item.m_CurvePosition, ptr, flows.Length, curveMapping);
						}
					}
				}
				if (impl.subObjects && m_SubObjects.TryGetBuffer(edge, out var bufferData2))
				{
					foreach (Game.Objects.SubObject item2 in bufferData2)
					{
						if (impl.TryGetFlowNode(item2.m_SubObject, out var flowNode5) && impl.TryGetFlowEdge(flowNode5, flowNode, out var flow4, out capacity2, out warning4) && m_CurveData.TryGetComponent(edge, out var componentData2) && m_TransformData.TryGetComponent(item2.m_SubObject, out var componentData3))
						{
							MathUtils.Distance(new Line3.Segment(componentData2.m_Bezier.a, componentData2.m_Bezier.d), componentData3.m_Position, out var t);
							AddTempFlow(flow4, t, ptr, flows.Length, curveMapping);
						}
					}
				}
				if (impl.connectedBuildings && impl.TryGetFlowEdge(flowNode, impl.sinkNode, out var flow5, out capacity2, out warning4) && m_ConnectedBuildings.TryGetBuffer(edge, out var bufferData3))
				{
					int totalDemand = 0;
					foreach (ConnectedBuilding item3 in bufferData3)
					{
						impl.GetConsumption(item3.m_Building, out var wantedConsumption, out capacity2, out warning4);
						totalDemand += wantedConsumption;
					}
					foreach (ConnectedBuilding item4 in bufferData3)
					{
						impl.GetConsumption(item4.m_Building, out var wantedConsumption2, out capacity2, out warning4);
						AddTempFlow(-FlowUtils.ConsumeFromTotal(wantedConsumption2, ref flow5, ref totalDemand), m_BuildingData[item4.m_Building].m_CurvePosition, ptr, flows.Length, curveMapping);
					}
				}
				int num2 = flow;
				for (int i = 0; i < flows.Length; i++)
				{
					num2 += ptr[i];
					flows[i] = GetSubFlow(impl.multiplier * num2, capacity);
				}
				if (MathUtils.Max(curveMapping) == 1f)
				{
					flows[flows.Length - 1] = GetSubFlow(impl.multiplier * flow2, capacity);
				}
				warning = math.max(warning2, warning3);
			}
			else
			{
				flows.Fill(default(SubFlow));
				warning = 0f;
			}
		}

		private unsafe static void AddTempFlow(int flow, float curvePosition, int* tempFlows, int length, float2 curveMapping)
		{
			float num = curveMapping.y - curveMapping.x;
			if (num != 0f)
			{
				float num2 = (curvePosition - curveMapping.x) / num;
				if (num2 < 0f)
				{
					*tempFlows += flow;
				}
				else if (num2 < 1f)
				{
					int num3 = math.clamp(Mathf.RoundToInt(num2 * (float)(length - 1)), 1, length - 1);
					tempFlows[num3] += flow;
				}
			}
			else if (curvePosition < curveMapping.x)
			{
				*tempFlows += flow;
			}
		}

		private static SubFlow GetSubFlow(int flow, int capacity)
		{
			int num = 127 * flow / capacity;
			return new SubFlow
			{
				m_Value = (sbyte)((num != 0) ? math.clamp(num, -127, 127) : math.clamp(flow, -1, 1))
			};
		}

		private void FillNodeFlow<T>(T impl, NativeArray<SubFlow> flows, EdgeMapping edgeMapping, out float warning) where T : struct, IFlowImplementation
		{
			FillNodeFlow(impl, flows, edgeMapping.m_Parent1, edgeMapping.m_Parent2, edgeMapping.m_CurveDelta1, out warning);
		}

		private void FillNodeFlow<T>(T impl, NativeArray<SubFlow> flows, Entity node, Entity edge, float2 curveMapping, out float warning) where T : struct, IFlowImplementation
		{
			float num = 0f;
			if (impl.TryGetFlowNode(node, out var flowNode) && impl.TryGetFlowNode(edge, out var flowNode2))
			{
				if (impl.TryGetFlowEdge(flowNode, flowNode2, out var flow, out var capacity, out warning))
				{
					num = (float)flow / (float)capacity;
				}
				else if (impl.TryGetFlowEdge(flowNode2, flowNode, out flow, out capacity, out warning))
				{
					num = (float)(-flow) / (float)capacity;
				}
			}
			else
			{
				warning = 0f;
			}
			num = math.select(num, 0f - num, curveMapping.y < curveMapping.x);
			SubFlow subFlow = GetSubFlow((float)impl.multiplier * num);
			flows.Fill(subFlow);
		}

		private void FillBuildingFlow<T>(T impl, NativeArray<SubFlow> flows, Entity building, out float warning) where T : struct, IFlowImplementation
		{
			impl.GetConsumption(building, out var _, out var fulfilledConsumption, out warning);
			float num = (0f - (float)fulfilledConsumption) / (float)(10000 + math.abs(fulfilledConsumption));
			SubFlow subFlow = GetSubFlow((float)impl.multiplier * num);
			flows.Fill(subFlow);
		}

		private SubFlow GetSubFlow(float value)
		{
			int num = math.clamp(Mathf.RoundToInt(value * 127f), -127, 127);
			num = math.select(num, 1, num == 0 && value > 0f);
			num = math.select(num, -1, num == 0 && value < 0f);
			return new SubFlow
			{
				m_Value = (sbyte)num
			};
		}

		private ElectricityFlow GetElectricityFlow()
		{
			return new ElectricityFlow
			{
				sinkNode = m_ElectricitySinkNode,
				m_NodeConnectionData = m_ElectricityNodeConnectionData,
				m_FlowEdgeData = m_ElectricityFlowEdgeData,
				m_BuildingConnectionData = m_ElectricityBuildingConnectionData,
				m_ConsumerData = m_ElectricityConsumerData,
				m_ConnectedFlowEdges = m_ConnectedFlowEdges
			};
		}

		private WaterFlow GetWaterFlow()
		{
			return new WaterFlow
			{
				sinkNode = m_WaterSinkNode,
				m_NodeConnectionData = m_WaterPipeNodeConnectionData,
				m_FlowEdgeData = m_WaterPipeEdgeData,
				m_BuildingConnectionData = m_WaterPipeBuildingConnectionData,
				m_ConsumerData = m_WaterConsumerData,
				m_ConnectedFlowEdges = m_ConnectedFlowEdges,
				m_MaxToleratedPollution = m_WaterPipeParameters.m_MaxToleratedPollution
			};
		}

		private SewageFlow GetSewageFlow()
		{
			return new SewageFlow
			{
				sinkNode = m_WaterSinkNode,
				m_NodeConnectionData = m_WaterPipeNodeConnectionData,
				m_FlowEdgeData = m_WaterPipeEdgeData,
				m_BuildingConnectionData = m_WaterPipeBuildingConnectionData,
				m_ConsumerData = m_WaterConsumerData,
				m_ConnectedFlowEdges = m_ConnectedFlowEdges
			};
		}

		private ResourceFlow GetResourceFlow()
		{
			return new ResourceFlow
			{
				sinkNode = m_WaterSinkNode,
				m_EdgeData = m_EdgeData,
				m_ResourceConnectionData = m_ResourceConnectionData,
				m_ConnectedEdges = m_ConnectedEdges
			};
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

		[ReadOnly]
		public ComponentTypeHandle<InfomodeActive> __Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> __Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewAvailabilityData> __Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetGeometryData> __Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> __Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainTrack> __Game_Net_TrainTrack_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TramTrack> __Game_Net_TramTrack_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Waterway> __Game_Net_Waterway_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SubwayTrack> __Game_Net_SubwayTrack_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCondition> __Game_Net_NetCondition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Pollution> __Game_Net_Pollution_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SecondaryFlow> __Game_Net_SecondaryFlow_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathwayData> __Game_Prefabs_PathwayData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ProcessEstimate> __Game_Zones_ProcessEstimate_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		public ComponentTypeHandle<EdgeColor> __Game_Net_EdgeColor_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<EdgeColor> __Game_Net_EdgeColor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Roundabout> __Game_Net_Roundabout_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Roundabout> __Game_Net_Roundabout_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		public ComponentTypeHandle<NodeColor> __Game_Net_NodeColor_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NodeColor> __Game_Net_NodeColor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NodeLane> __Game_Net_NodeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeMapping> __Game_Net_EdgeMapping_RO_ComponentTypeHandle;

		public ComponentTypeHandle<LaneColor> __Game_Net_LaneColor_RW_ComponentTypeHandle;

		public BufferTypeHandle<SubFlow> __Game_Net_SubFlow_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ResourceConnection> __Game_Net_ResourceConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfomodeActive>(isReadOnly: true);
			__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewCoverageData>(isReadOnly: true);
			__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewAvailabilityData>(isReadOnly: true);
			__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetGeometryData>(isReadOnly: true);
			__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetStatusData>(isReadOnly: true);
			__Game_Net_TrainTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainTrack>(isReadOnly: true);
			__Game_Net_TramTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TramTrack>(isReadOnly: true);
			__Game_Net_Waterway_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Waterway>(isReadOnly: true);
			__Game_Net_SubwayTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SubwayTrack>(isReadOnly: true);
			__Game_Net_NetCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCondition>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
			__Game_Net_Pollution_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Pollution>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Net_SecondaryFlow_RO_ComponentLookup = state.GetComponentLookup<SecondaryFlow>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Prefabs_PathwayData_RO_ComponentLookup = state.GetComponentLookup<PathwayData>(isReadOnly: true);
			__Game_Zones_ProcessEstimate_RO_BufferLookup = state.GetBufferLookup<ProcessEstimate>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Edge>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_EdgeColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeColor>();
			__Game_Net_EdgeColor_RO_ComponentLookup = state.GetComponentLookup<EdgeColor>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Roundabout_RO_ComponentLookup = state.GetComponentLookup<Roundabout>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Roundabout_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Roundabout>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_NodeColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NodeColor>();
			__Game_Net_NodeColor_RO_ComponentLookup = state.GetComponentLookup<NodeColor>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.SecondaryLane>(isReadOnly: true);
			__Game_Net_EdgeMapping_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeMapping>(isReadOnly: true);
			__Game_Net_LaneColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LaneColor>();
			__Game_Net_SubFlow_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubFlow>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_ResourceConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ResourceConnection>(isReadOnly: true);
			__Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
			__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityBuildingConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
			__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
			__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeBuildingConnection>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
		}
	}

	private EntityQuery m_ZonePreferenceParameterGroup;

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_NodeQuery;

	private EntityQuery m_LaneQuery;

	private EntityQuery m_InfomodeQuery;

	private EntityQuery m_ProcessQuery;

	private ToolSystem m_ToolSystem;

	private ZoneToolSystem m_ZoneToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private PrefabSystem m_PrefabSystem;

	private ResourceSystem m_ResourceSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private ElectricityFlowSystem m_ElectricityFlowSystem;

	private WaterPipeFlowSystem m_WaterPipeFlowSystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1733354667_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
		m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
		m_ZonePreferenceParameterGroup = GetEntityQuery(ComponentType.ReadOnly<ZonePreferenceData>());
		m_EdgeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadWrite<EdgeColor>(), ComponentType.Exclude<Deleted>());
		m_NodeQuery = GetEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadWrite<NodeColor>(), ComponentType.Exclude<Deleted>());
		m_LaneQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadWrite<LaneColor>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_InfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<InfoviewCoverageData>(),
				ComponentType.ReadOnly<InfoviewAvailabilityData>(),
				ComponentType.ReadOnly<InfoviewNetGeometryData>(),
				ComponentType.ReadOnly<InfoviewNetStatusData>()
			},
			None = new ComponentType[0]
		});
		m_ProcessQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeInfoview == null || (m_EdgeQuery.IsEmptyIgnoreFilter && m_NodeQuery.IsEmptyIgnoreFilter))
		{
			return;
		}
		ZonePreferenceData zonePreferences = ((m_ZonePreferenceParameterGroup.CalculateEntityCount() > 0) ? m_ZonePreferenceParameterGroup.GetSingleton<ZonePreferenceData>() : default(ZonePreferenceData));
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> infomodeChunks = m_InfomodeQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle);
		Entity zonePrefab = Entity.Null;
		if (m_ToolSystem.activeTool == m_ZoneToolSystem && m_ZoneToolSystem.prefab != null)
		{
			zonePrefab = m_PrefabSystem.GetEntity(m_ZoneToolSystem.prefab);
		}
		else if (m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.prefab != null)
		{
			PlaceholderBuilding component2;
			if (m_ObjectToolSystem.prefab.TryGet<SignatureBuilding>(out var component) && component.m_ZoneType != null)
			{
				zonePrefab = m_PrefabSystem.GetEntity(component.m_ZoneType);
			}
			else if (m_ObjectToolSystem.prefab.TryGet<PlaceholderBuilding>(out component2) && component2.m_ZoneType != null)
			{
				zonePrefab = m_PrefabSystem.GetEntity(component2.m_ZoneType);
			}
		}
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle deps;
		JobHandle deps2;
		JobHandle outJobHandle2;
		UpdateEdgeColorsJob jobData = new UpdateEdgeColorsJob
		{
			m_InfomodeChunks = infomodeChunks,
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewCoverageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewAvailabilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TramTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterwayType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubwayTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PollutionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceCoverageType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceAvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryFlowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryFlow_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Temps = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPathwayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathwayData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProcessEstimates = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceCoverageData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceAvailabilityData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZonePrefab = zonePrefab,
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_GroundPollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_IndustrialDemands = m_IndustrialDemandSystem.GetBuildingDemands(out deps),
			m_StorageDemands = m_IndustrialDemandSystem.GetStorageBuildingDemands(out deps2),
			m_Processes = m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_ZonePreferences = zonePreferences
		};
		UpdateNodeColorsJob jobData2 = new UpdateNodeColorsJob
		{
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryFlowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryFlow_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoundaboutData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Roundabout_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_InfomodeChunks = infomodeChunks,
			m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TramTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterwayType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubwayTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetConditionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PollutionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoundaboutType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Roundabout_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeColor_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		UpdateEdgeColors2Job jobData3 = new UpdateEdgeColors2Job
		{
			m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StartNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EndNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		LaneColorJob jobData4 = new LaneColorJob
		{
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SecondaryLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeMappingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeMapping_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ColorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LaneColor_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubFlowType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubFlow_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_InfomodeChunks = infomodeChunks,
			m_InfomodeActiveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityNodeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityFlowEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityBuildingConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeNodeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterPipeBuildingConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedFlowEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ElectricitySinkNode = m_ElectricityFlowSystem.sinkNode,
			m_WaterSinkNode = m_WaterPipeFlowSystem.sinkNode,
			m_WaterPipeParameters = __query_1733354667_0.GetSingleton<WaterPipeParameterData>()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_EdgeQuery, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, dependencies, dependencies3, dependencies2, deps, deps2));
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData2, m_NodeQuery, jobHandle), jobData: jobData3, query: m_EdgeQuery), jobData: jobData4, query: m_LaneQuery);
		infomodeChunks.Dispose(jobHandle2);
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_AirPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		m_IndustrialDemandSystem.AddReader(jobHandle);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		base.Dependency = jobHandle2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<WaterPipeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1733354667_0 = entityQueryBuilder2.Build(ref state);
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
	public NetColorSystem()
	{
	}
}
