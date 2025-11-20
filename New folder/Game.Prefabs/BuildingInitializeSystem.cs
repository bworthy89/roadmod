using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Mathematics;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class BuildingInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindConnectionRequirementsJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingDataType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> m_ServiceUpgradeDataType;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorFacilityData> m_ExtractorFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<ConsumptionData> m_ConsumptionDataType;

		[ReadOnly]
		public ComponentTypeHandle<WorkplaceData> m_WorkplaceDataType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> m_WaterPumpingStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> m_WaterTowerDataType;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> m_SewageOutletDataType;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlantData> m_WastewaterTreatmentPlantDataType;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> m_TransformerDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> m_ParkingFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<PublicTransportStationData> m_PublicTransportStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<CargoTransportStationData> m_CargoTransportStationDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> m_ParkDataType;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> m_CoverageDataType;

		[ReadOnly]
		public BufferTypeHandle<SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> m_SubMeshType;

		[ReadOnly]
		public BufferTypeHandle<ServiceUpgradeBuilding> m_ServiceUpgradeBuildingType;

		public ComponentTypeHandle<BuildingData> m_BuildingDataType;

		public ComponentTypeHandle<PlaceableObjectData> m_PlaceableObjectDataType;

		public BufferTypeHandle<Effect> m_EffectType;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<GateData> m_GateData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<EffectData> m_EffectData;

		[ReadOnly]
		public ComponentLookup<VFXData> m_VFXData;

		[ReadOnly]
		public BufferLookup<AudioSourceData> m_AudioSourceData;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> m_AudioSpotData;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> m_AudioEffectData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			NativeArray<BuildingData> nativeArray = archetypeChunk.GetNativeArray(ref m_BuildingDataType);
			BufferAccessor<SubMesh> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_SubMeshType);
			BufferAccessor<SubObject> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<Effect> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref m_EffectType);
			if (archetypeChunk.Has(ref m_SpawnableBuildingDataType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					BuildingData value = nativeArray[i];
					value.m_Flags |= BuildingFlags.RequireRoad | BuildingFlags.RestrictedPedestrian | BuildingFlags.RestrictedCar | BuildingFlags.RestrictedParking | BuildingFlags.RestrictedTrack;
					if (bufferAccessor[i].Length == 0)
					{
						value.m_Flags |= BuildingFlags.ColorizeLot;
					}
					if (CollectionUtils.TryGet(bufferAccessor2, i, out var value2))
					{
						CheckPropFlags(ref value.m_Flags, value2);
					}
					nativeArray[i] = value;
				}
			}
			else if (archetypeChunk.Has(ref m_ServiceUpgradeDataType) || archetypeChunk.Has(ref m_ExtractorFacilityDataType))
			{
				NativeArray<PlaceableObjectData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PlaceableObjectDataType);
				BufferAccessor<ServiceUpgradeBuilding> bufferAccessor4 = archetypeChunk.GetBufferAccessor(ref m_ServiceUpgradeBuildingType);
				BufferAccessor<SubNet> bufferAccessor5 = archetypeChunk.GetBufferAccessor(ref m_SubNetType);
				bool isParkingFacility = archetypeChunk.Has(ref m_ParkingFacilityDataType);
				bool isPublicTransportStation = archetypeChunk.Has(ref m_PublicTransportStationDataType);
				bool isCargoTransportStation = archetypeChunk.Has(ref m_CargoTransportStationDataType);
				bool flag = archetypeChunk.Has(ref m_ExtractorFacilityDataType);
				bool flag2 = archetypeChunk.Has(ref m_CoverageDataType);
				BuildingFlags restrictionFlags = GetRestrictionFlags(isParkingFacility, isPublicTransportStation, isCargoTransportStation);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					BuildingData value3 = nativeArray[j];
					value3.m_Flags |= BuildingFlags.NoRoadConnection | restrictionFlags;
					if (bufferAccessor[j].Length == 0)
					{
						value3.m_Flags |= BuildingFlags.ColorizeLot;
					}
					DynamicBuffer<ServiceUpgradeBuilding> value4;
					bool flag3 = CollectionUtils.TryGet(bufferAccessor4, j, out value4) && IsGateUpgrade(value4);
					if (flag2 || flag3)
					{
						value3.m_Flags &= ~BuildingFlags.NoRoadConnection;
						if ((value3.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
						{
							value3.m_Flags |= BuildingFlags.RequireRoad;
						}
						if (CollectionUtils.TryGet(nativeArray2, j, out var value5))
						{
							value5.m_Flags &= ~Game.Objects.PlacementFlags.OwnerSide;
							nativeArray2[j] = value5;
						}
					}
					if (CollectionUtils.TryGet(bufferAccessor2, j, out var value6))
					{
						CheckPropFlags(ref value3.m_Flags, value6);
					}
					if (flag && bufferAccessor5.Length != 0)
					{
						DynamicBuffer<SubNet> dynamicBuffer = bufferAccessor5[j];
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							SubNet subNet = dynamicBuffer[k];
							if (m_NetData.HasComponent(subNet.m_Prefab) && (m_NetData[subNet.m_Prefab].m_RequiredLayers & Layer.ResourceLine) != Layer.None)
							{
								value3.m_Flags |= BuildingFlags.HasResourceNode;
							}
						}
					}
					nativeArray[j] = value3;
				}
			}
			else
			{
				NativeArray<ConsumptionData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_ConsumptionDataType);
				NativeArray<WorkplaceData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_WorkplaceDataType);
				BufferAccessor<SubNet> bufferAccessor6 = archetypeChunk.GetBufferAccessor(ref m_SubNetType);
				bool flag4 = archetypeChunk.Has(ref m_WaterPumpingStationDataType);
				bool flag5 = archetypeChunk.Has(ref m_WaterTowerDataType);
				bool flag6 = archetypeChunk.Has(ref m_SewageOutletDataType);
				bool flag7 = archetypeChunk.Has(ref m_WastewaterTreatmentPlantDataType);
				bool flag8 = archetypeChunk.Has(ref m_TransformerDataType);
				bool flag9 = archetypeChunk.Has(ref m_ParkingFacilityDataType);
				bool flag10 = archetypeChunk.Has(ref m_PublicTransportStationDataType);
				bool flag11 = archetypeChunk.Has(ref m_CargoTransportStationDataType);
				bool flag12 = archetypeChunk.Has(ref m_ParkDataType);
				BuildingFlags restrictionFlags2 = GetRestrictionFlags(flag9, flag10, flag11);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Layer layer = Layer.None;
					Layer layer2 = Layer.None;
					Layer layer3 = Layer.None;
					if (nativeArray3.Length != 0)
					{
						ConsumptionData consumptionData = nativeArray3[l];
						if (consumptionData.m_ElectricityConsumption > 0f)
						{
							layer |= Layer.PowerlineLow;
						}
						if (consumptionData.m_GarbageAccumulation > 0f)
						{
							layer |= Layer.Road;
						}
						if (consumptionData.m_WaterConsumption > 0f)
						{
							layer |= Layer.WaterPipe | Layer.SewagePipe;
						}
					}
					if (nativeArray4.Length != 0 && nativeArray4[l].m_MaxWorkers > 0)
					{
						layer |= Layer.Road;
					}
					if (flag4 || flag5)
					{
						layer |= Layer.WaterPipe;
					}
					if (flag6 || flag7)
					{
						layer |= Layer.SewagePipe;
					}
					if (flag8)
					{
						layer |= Layer.PowerlineLow;
					}
					if (layer != Layer.None && bufferAccessor6.Length != 0)
					{
						DynamicBuffer<SubNet> dynamicBuffer2 = bufferAccessor6[l];
						for (int m = 0; m < dynamicBuffer2.Length; m++)
						{
							SubNet subNet2 = dynamicBuffer2[m];
							if (m_NetData.HasComponent(subNet2.m_Prefab))
							{
								NetData netData = m_NetData[subNet2.m_Prefab];
								if ((netData.m_RequiredLayers & Layer.Road) == 0)
								{
									layer2 |= netData.m_RequiredLayers | netData.m_LocalConnectLayers;
									layer3 |= netData.m_RequiredLayers;
								}
							}
						}
					}
					BuildingData value7 = nativeArray[l];
					value7.m_Flags |= restrictionFlags2;
					if ((layer & ~layer2) != Layer.None)
					{
						value7.m_Flags |= BuildingFlags.RequireRoad;
					}
					else if (flag9 || flag10 || flag11 || flag12)
					{
						value7.m_Flags |= BuildingFlags.RequireAccess;
					}
					if ((layer3 & Layer.PowerlineLow) != Layer.None)
					{
						value7.m_Flags |= BuildingFlags.HasLowVoltageNode;
					}
					if ((layer3 & Layer.WaterPipe) != Layer.None)
					{
						value7.m_Flags |= BuildingFlags.HasWaterNode;
					}
					if ((layer3 & Layer.SewagePipe) != Layer.None)
					{
						value7.m_Flags |= BuildingFlags.HasSewageNode;
					}
					if (bufferAccessor[l].Length == 0)
					{
						value7.m_Flags |= BuildingFlags.ColorizeLot;
					}
					if (flag12 && (value7.m_Flags & (BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess)) != 0)
					{
						value7.m_Flags &= ~BuildingFlags.RestrictedPedestrian;
					}
					if (CollectionUtils.TryGet(bufferAccessor2, l, out var value8))
					{
						CheckPropFlags(ref value7.m_Flags, value8);
					}
					nativeArray[l] = value7;
				}
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
			for (int n = 0; n < bufferAccessor3.Length; n++)
			{
				DynamicBuffer<Effect> dynamicBuffer3 = bufferAccessor3[n];
				DynamicBuffer<SubMesh> dynamicBuffer4 = bufferAccessor[n];
				bool2 x = new bool2(x: false, nativeArray.Length == 0);
				int num = 0;
				while (true)
				{
					if (num < dynamicBuffer3.Length)
					{
						if (m_EffectData.TryGetComponent(dynamicBuffer3[num].m_Effect, out var componentData) && (componentData.m_Flags.m_RequiredFlags & EffectConditionFlags.Collapsing) != EffectConditionFlags.None)
						{
							x.x |= m_VFXData.HasComponent(dynamicBuffer3[num].m_Effect);
							x.y |= m_AudioSourceData.HasBuffer(dynamicBuffer3[num].m_Effect);
							if (math.all(x))
							{
								break;
							}
						}
						num++;
						continue;
					}
					for (int num2 = 0; num2 < dynamicBuffer4.Length; num2++)
					{
						SubMesh subMesh = dynamicBuffer4[num2];
						if (!m_MeshData.TryGetComponent(subMesh.m_SubMesh, out var componentData2))
						{
							continue;
						}
						float2 @float = MathUtils.Center(componentData2.m_Bounds.xz);
						float2 float2 = MathUtils.Size(componentData2.m_Bounds.xz);
						float3 float3 = subMesh.m_Position + math.rotate(subMesh.m_Rotation, new float3(@float.x, 0f, @float.y));
						if (!x.y)
						{
							int2 @int = math.max(0, (int2)(float2 * m_BuildingConfigurationData.m_CollapseSFXDensity));
							if (@int.x * @int.y > 1)
							{
								float2 float4 = float2 / @int;
								float3 float5 = math.rotate(subMesh.m_Rotation, new float3(float4.x, 0f, 0f));
								float3 float6 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, float4.y));
								float3 float7 = float3 - (float5 * ((float)@int.x * 0.5f - 0.5f) + float6 * ((float)@int.y * 0.5f - 0.5f));
								dynamicBuffer3.Capacity = dynamicBuffer3.Length + @int.x * @int.y;
								for (int num3 = 0; num3 < @int.y; num3++)
								{
									for (int num4 = 0; num4 < @int.x; num4++)
									{
										float2 float8 = new float2(num4, num3);
										dynamicBuffer3.Add(new Effect
										{
											m_Effect = m_BuildingConfigurationData.m_CollapseSFX,
											m_Position = float7 + float5 * float8.x + float6 * float8.y,
											m_Rotation = subMesh.m_Rotation,
											m_Scale = 0.5f,
											m_Intensity = 0.5f,
											m_ParentMesh = num2,
											m_AnimationIndex = -1,
											m_Procedural = true
										});
									}
								}
							}
							else
							{
								dynamicBuffer3.Add(new Effect
								{
									m_Effect = m_BuildingConfigurationData.m_CollapseSFX,
									m_Position = float3,
									m_Rotation = subMesh.m_Rotation,
									m_Scale = 1f,
									m_Intensity = 1f,
									m_ParentMesh = num2,
									m_AnimationIndex = -1,
									m_Procedural = true
								});
							}
						}
						if (x.x)
						{
							continue;
						}
						int2 int2 = math.max(1, (int2)(math.sqrt(float2) * 0.5f));
						float2 float9 = float2 / int2;
						float3 float10 = math.rotate(subMesh.m_Rotation, new float3(float9.x, 0f, 0f));
						float3 float11 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, float9.y));
						float3 scale = new float3(float9.x * 0.05f, 1f, float9.y * 0.05f);
						float3 -= float10 * ((float)int2.x * 0.5f - 0.5f) + float11 * ((float)int2.y * 0.5f - 0.5f);
						scale.y = (scale.x + scale.y) * 0.5f;
						dynamicBuffer3.Capacity = dynamicBuffer3.Length + int2.x * int2.y;
						for (int num5 = 0; num5 < int2.y; num5++)
						{
							for (int num6 = 0; num6 < int2.x; num6++)
							{
								float2 float12 = new float2(num6, num5) + random.NextFloat2(-0.25f, 0.25f);
								dynamicBuffer3.Add(new Effect
								{
									m_Effect = m_BuildingConfigurationData.m_CollapseVFX,
									m_Position = float3 + float10 * float12.x + float11 * float12.y,
									m_Rotation = subMesh.m_Rotation,
									m_Scale = scale,
									m_Intensity = 1f,
									m_ParentMesh = num2,
									m_AnimationIndex = -1,
									m_Procedural = true
								});
							}
						}
					}
					break;
				}
			}
			NativeList<Effect> nativeList = new NativeList<Effect>(Allocator.Temp);
			NativeList<float3> nativeList2 = new NativeList<float3>(Allocator.Temp);
			float num7 = 125f;
			for (int num8 = 0; num8 < bufferAccessor3.Length; num8++)
			{
				DynamicBuffer<Effect> effects = bufferAccessor3[num8];
				bool2 hasFireSfxEffects = new bool2(x: false, y: false);
				for (int num9 = 0; num9 < effects.Length; num9++)
				{
					if (m_EffectData.TryGetComponent(effects[num9].m_Effect, out var componentData3) && (componentData3.m_Flags.m_RequiredFlags & EffectConditionFlags.OnFire) != EffectConditionFlags.None)
					{
						hasFireSfxEffects.x |= m_AudioEffectData.HasComponent(effects[num9].m_Effect);
						hasFireSfxEffects.y |= m_AudioSpotData.HasComponent(effects[num9].m_Effect);
						nativeList.Add(effects[num9]);
					}
				}
				for (int num10 = 0; num10 < nativeList.Length; num10++)
				{
					Effect effect = nativeList[num10];
					bool flag13 = false;
					for (int num11 = 0; num11 < nativeList2.Length; num11++)
					{
						if (math.distancesq(effect.m_Position, nativeList2[num11]) < num7 * num7)
						{
							flag13 = true;
							break;
						}
					}
					if (!flag13)
					{
						nativeList2.Add(in effect.m_Position);
						AddFireSfxToBuilding(ref hasFireSfxEffects, effects, effect.m_Position, effect.m_Rotation, effect.m_ParentMesh);
					}
				}
				nativeList.Clear();
				nativeList2.Clear();
			}
		}

		private bool IsGateUpgrade(DynamicBuffer<ServiceUpgradeBuilding> serviceUpgradeBuildings)
		{
			for (int i = 0; i < serviceUpgradeBuildings.Length; i++)
			{
				if (m_GateData.HasComponent(serviceUpgradeBuildings[i].m_Building))
				{
					return true;
				}
			}
			return false;
		}

		private BuildingFlags GetRestrictionFlags(bool isParkingFacility, bool isPublicTransportStation, bool isCargoTransportStation)
		{
			BuildingFlags buildingFlags = (BuildingFlags)0u;
			if (!isParkingFacility && !isPublicTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedPedestrian;
			}
			if (!isParkingFacility && !isCargoTransportStation && !isPublicTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedCar;
			}
			if (!isParkingFacility)
			{
				buildingFlags |= BuildingFlags.RestrictedParking;
			}
			if (!isPublicTransportStation && !isCargoTransportStation)
			{
				buildingFlags |= BuildingFlags.RestrictedTrack;
			}
			return buildingFlags;
		}

		private void AddFireSfxToBuilding(ref bool2 hasFireSfxEffects, DynamicBuffer<Effect> effects, float3 position, quaternion rotation, int parent)
		{
			if (!hasFireSfxEffects.x)
			{
				effects.Add(new Effect
				{
					m_Effect = m_BuildingConfigurationData.m_FireLoopSFX,
					m_Position = position,
					m_Rotation = rotation,
					m_Scale = 1f,
					m_Intensity = 1f,
					m_ParentMesh = parent,
					m_AnimationIndex = -1,
					m_Procedural = true
				});
			}
			if (!hasFireSfxEffects.y)
			{
				effects.Add(new Effect
				{
					m_Effect = m_BuildingConfigurationData.m_FireSpotSFX,
					m_Position = position,
					m_Rotation = rotation,
					m_Scale = 1f,
					m_Intensity = 1f,
					m_ParentMesh = parent,
					m_AnimationIndex = -1,
					m_Procedural = true
				});
			}
		}

		private void CheckPropFlags(ref BuildingFlags flags, DynamicBuffer<SubObject> subObjects, int maxDepth = 10)
		{
			if (--maxDepth < 0)
			{
				return;
			}
			for (int i = 0; i < subObjects.Length; i++)
			{
				SubObject subObject = subObjects[i];
				if (m_SpawnLocationData.TryGetComponent(subObject.m_Prefab, out var componentData) && componentData.m_ActivityMask.m_Mask == 0 && (componentData.m_ConnectionType == RouteConnectionType.Pedestrian || (componentData.m_ConnectionType == RouteConnectionType.Parking && componentData.m_RoadTypes == RoadTypes.Bicycle)))
				{
					flags |= BuildingFlags.HasInsideRoom;
				}
				if (m_SubObjects.TryGetBuffer(subObject.m_Prefab, out var bufferData))
				{
					CheckPropFlags(ref flags, bufferData, maxDepth);
				}
			}
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

		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;

		public BufferTypeHandle<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;

		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorFacilityData> __Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WastewaterTreatmentPlantData> __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> __Game_Prefabs_TransformerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PublicTransportStationData> __Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> __Game_Prefabs_ParkData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RO_BufferTypeHandle;

		public BufferTypeHandle<Effect> __Game_Prefabs_Effect_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GateData> __Game_Prefabs_GateData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VFXData> __Game_Prefabs_VFXData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<AudioSpotData> __Game_Prefabs_AudioSpotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>();
			__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingExtensionData>();
			__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingTerraformData>();
			__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>();
			__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>();
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableObjectData>();
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPoweredData>(isReadOnly: true);
			__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SewageOutletData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpgradeBuilding>(isReadOnly: true);
			__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpkeepData>();
			__Game_Prefabs_ZoneData_RW_ComponentLookup = state.GetComponentLookup<ZoneData>();
			__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ZoneServiceConsumptionData>(isReadOnly: true);
			__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorFacilityData>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkplaceData>(isReadOnly: true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPumpingStationData>(isReadOnly: true);
			__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterTowerData>(isReadOnly: true);
			__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WastewaterTreatmentPlantData>(isReadOnly: true);
			__Game_Prefabs_TransformerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransformerData>(isReadOnly: true);
			__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingFacilityData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PublicTransportStationData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CargoTransportStationData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkData>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CoverageData>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>(isReadOnly: true);
			__Game_Prefabs_Effect_RW_BufferTypeHandle = state.GetBufferTypeHandle<Effect>();
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_GateData_RO_ComponentLookup = state.GetComponentLookup<GateData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_VFXData_RO_ComponentLookup = state.GetComponentLookup<VFXData>(isReadOnly: true);
			__Game_Prefabs_AudioSourceData_RO_BufferLookup = state.GetBufferLookup<AudioSourceData>(isReadOnly: true);
			__Game_Prefabs_AudioSpotData_RO_ComponentLookup = state.GetComponentLookup<AudioSpotData>(isReadOnly: true);
			__Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
		}
	}

	private static ILog log;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_ConfigurationQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_547773813_0;

	[Preserve]
	protected override void OnCreate()
	{
		log = LogManager.GetLogger("Simulation");
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
				ComponentType.ReadWrite<BuildingData>(),
				ComponentType.ReadWrite<BuildingExtensionData>(),
				ComponentType.ReadWrite<ServiceUpgradeData>(),
				ComponentType.ReadWrite<SpawnableBuildingData>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadWrite<ServiceUpgradeData>()
			}
		});
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
		NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BuildingData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BuildingExtensionData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BuildingTerraformData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ConsumptionData> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ObjectGeometryData> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<SpawnableBuildingData> typeHandle8 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<SignatureBuildingData> typeHandle9 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PlaceableObjectData> typeHandle10 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ServiceUpgradeData> typeHandle11 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BuildingPropertyData> typeHandle12 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<WaterPoweredData> typeHandle13 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<SewageOutletData> typeHandle14 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<ServiceUpgradeBuilding> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CollectedServiceBuildingBudgetData> typeHandle15 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<ServiceUpkeepData> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle, ref base.CheckedStateRef);
		ComponentLookup<ZoneData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ZoneServiceConsumptionData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef);
		CompleteDependency();
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(entityTypeHandle);
			BufferAccessor<ServiceUpgradeBuilding> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
			if (archetypeChunk.Has(ref typeHandle))
			{
				if (bufferAccessor.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					Entity upgrade = nativeArray[j];
					DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						ServiceUpgradeBuilding serviceUpgradeBuilding = dynamicBuffer[k];
						CollectionUtils.RemoveValue(base.EntityManager.GetBuffer<BuildingUpgradeElement>(serviceUpgradeBuilding.m_Building), new BuildingUpgradeElement(upgrade));
					}
				}
				continue;
			}
			NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
			NativeArray<ObjectGeometryData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle7);
			NativeArray<BuildingData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle3);
			NativeArray<BuildingExtensionData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle4);
			NativeArray<ConsumptionData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle6);
			NativeArray<SpawnableBuildingData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle8);
			NativeArray<PlaceableObjectData> nativeArray8 = archetypeChunk.GetNativeArray(ref typeHandle10);
			NativeArray<ServiceUpgradeData> nativeArray9 = archetypeChunk.GetNativeArray(ref typeHandle11);
			NativeArray<BuildingPropertyData> nativeArray10 = archetypeChunk.GetNativeArray(ref typeHandle12);
			BufferAccessor<ServiceUpkeepData> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
			bool flag = archetypeChunk.Has(ref typeHandle15);
			bool flag2 = archetypeChunk.Has(ref typeHandle9);
			bool flag3 = archetypeChunk.Has(ref typeHandle13);
			bool flag4 = archetypeChunk.Has(ref typeHandle14);
			if (nativeArray4.Length != 0)
			{
				NativeArray<BuildingTerraformData> nativeArray11 = archetypeChunk.GetNativeArray(ref typeHandle5);
				for (int l = 0; l < nativeArray4.Length; l++)
				{
					BuildingPrefab prefab = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray2[l]);
					BuildingTerraformOverride component = prefab.GetComponent<BuildingTerraformOverride>();
					ObjectGeometryData objectGeometryData = nativeArray3[l];
					BuildingTerraformData buildingTerraformData = nativeArray11[l];
					BuildingData buildingData = nativeArray4[l];
					InitializeLotSize(prefab, component, ref objectGeometryData, ref buildingTerraformData, ref buildingData);
					if (nativeArray7.Length != 0 && !flag2)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.DeleteOverridden;
					}
					else
					{
						objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Overridable;
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
					}
					if (flag3)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
					}
					else if (flag4 && prefab.GetComponent<SewageOutlet>().m_AllowSubmerged)
					{
						objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
					}
					objectGeometryData.m_Flags &= ~Game.Objects.GeometryFlags.Brushable;
					objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot;
					if (CollectionUtils.TryGet(nativeArray8, l, out var value))
					{
						if ((value.m_Flags & (Game.Objects.PlacementFlags.OnGround | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Swaying)) == (Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Swaying))
						{
							objectGeometryData.m_Flags &= ~(Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.OccupyZone);
						}
						switch (prefab.m_AccessType)
						{
						case BuildingAccessType.OnRoad:
							value.m_Flags |= Game.Objects.PlacementFlags.NetObject | Game.Objects.PlacementFlags.RoadEdge;
							value.m_SubReplacementType = SubReplacementType.None;
							buildingData.m_Flags |= BuildingFlags.CanBeOnRoad;
							break;
						case BuildingAccessType.OnRoadArea:
							value.m_Flags |= Game.Objects.PlacementFlags.NetObject | Game.Objects.PlacementFlags.RoadEdge;
							value.m_SubReplacementType = SubReplacementType.None;
							buildingData.m_Flags |= BuildingFlags.CanBeOnRoadArea;
							break;
						}
						nativeArray8[l] = value;
					}
					nativeArray3[l] = objectGeometryData;
					nativeArray11[l] = buildingTerraformData;
					nativeArray4[l] = buildingData;
				}
			}
			if (nativeArray5.Length != 0)
			{
				NativeArray<BuildingTerraformData> nativeArray12 = archetypeChunk.GetNativeArray(ref typeHandle5);
				for (int m = 0; m < nativeArray5.Length; m++)
				{
					BuildingExtensionPrefab prefab2 = m_PrefabSystem.GetPrefab<BuildingExtensionPrefab>(nativeArray2[m]);
					ObjectGeometryData value2 = nativeArray3[m];
					Bounds2 flatBounds;
					if ((value2.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
					{
						float2 xz = value2.m_Pivot.xz;
						float2 @float = value2.m_LegSize.xz * 0.5f + value2.m_LegOffset;
						flatBounds = new Bounds2(xz - @float, xz + @float);
					}
					else
					{
						flatBounds = value2.m_Bounds.xz;
					}
					value2.m_Bounds.min = math.min(value2.m_Bounds.min, new float3(-0.5f, 0f, -0.5f));
					value2.m_Bounds.max = math.max(value2.m_Bounds.max, new float3(0.5f, 5f, 0.5f));
					value2.m_Flags &= ~(Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Brushable);
					value2.m_Flags |= Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot;
					BuildingExtensionData value3 = nativeArray5[m];
					value3.m_Position = prefab2.m_Position;
					value3.m_LotSize = prefab2.m_OverrideLotSize;
					value3.m_External = prefab2.m_ExternalLot;
					if (prefab2.m_OverrideHeight > 0f)
					{
						value2.m_Bounds.max.y = prefab2.m_OverrideHeight;
					}
					Bounds2 lotBounds;
					if (math.all(value3.m_LotSize > 0))
					{
						float2 float2 = value3.m_LotSize;
						float2 *= 8f;
						lotBounds = new Bounds2(float2 * -0.5f, float2 * 0.5f);
						float2 -= 0.4f;
						value2.m_Bounds.min.xz = float2 * -0.5f;
						value2.m_Bounds.max.xz = float2 * 0.5f;
						if (bufferAccessor.Length != 0)
						{
							value2.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
						}
					}
					else
					{
						Bounds3 bounds = value2.m_Bounds;
						lotBounds = value2.m_Bounds.xz;
						if (bufferAccessor.Length != 0)
						{
							DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer2 = bufferAccessor[m];
							for (int n = 0; n < dynamicBuffer2.Length; n++)
							{
								ServiceUpgradeBuilding serviceUpgradeBuilding2 = dynamicBuffer2[n];
								BuildingPrefab prefab3 = m_PrefabSystem.GetPrefab<BuildingPrefab>(serviceUpgradeBuilding2.m_Building);
								float2 float3 = new int2(prefab3.m_LotWidth, prefab3.m_LotDepth);
								float3 *= 8f;
								float2 float4 = float3;
								float3 -= 0.4f;
								if ((value2.m_Flags & Game.Objects.GeometryFlags.Standing) == 0 && prefab3.TryGet<StandingObject>(out var component2))
								{
									float3 = component2.m_LegSize.xz + math.select(default(float2), component2.m_LegSize.xz + component2.m_LegGap, component2.m_LegGap != 0f);
									float3 -= 0.4f;
									float4 = float3;
									if (component2.m_CircularLeg)
									{
										value2.m_Flags |= Game.Objects.GeometryFlags.Circular;
									}
								}
								if (n == 0)
								{
									bounds.xz = new Bounds2(float3 * -0.5f, float3 * 0.5f) - prefab2.m_Position.xz;
									lotBounds = new Bounds2(float4 * -0.5f, float4 * 0.5f) - prefab2.m_Position.xz;
								}
								else
								{
									bounds.xz &= new Bounds2(float3 * -0.5f, float3 * 0.5f) - prefab2.m_Position.xz;
									lotBounds &= new Bounds2(float4 * -0.5f, float4 * 0.5f) - prefab2.m_Position.xz;
								}
							}
							value2.m_Bounds.xz = bounds.xz;
							value2.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
						}
						float2 float5 = math.min(-bounds.min.xz, bounds.max.xz) * 0.25f - 0.01f;
						value3.m_LotSize.x = math.max(1, Mathf.CeilToInt(float5.x));
						value3.m_LotSize.y = math.max(1, Mathf.CeilToInt(float5.y));
					}
					if (value3.m_External)
					{
						float2 float6 = value3.m_LotSize;
						float6 *= 8f;
						value2.m_Layers |= MeshLayer.Default;
						value2.m_MinLod = math.min(value2.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(float6.x, 0f, float6.y))));
					}
					if (nativeArray12.Length != 0)
					{
						BuildingTerraformOverride component3 = prefab2.GetComponent<BuildingTerraformOverride>();
						BuildingTerraformData buildingTerraformData2 = nativeArray12[m];
						InitializeTerraformData(component3, ref buildingTerraformData2, lotBounds, flatBounds);
						nativeArray12[m] = buildingTerraformData2;
					}
					value2.m_Size = math.max(ObjectUtils.GetSize(value2.m_Bounds), new float3(1f, 5f, 1f));
					nativeArray3[m] = value2;
					nativeArray5[m] = value3;
				}
			}
			if (nativeArray7.Length != 0)
			{
				for (int num = 0; num < nativeArray7.Length; num++)
				{
					Entity e = nativeArray[num];
					BuildingPrefab prefab4 = m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray2[num]);
					BuildingPropertyData buildingPropertyData = ((nativeArray10.Length != 0) ? nativeArray10[num] : default(BuildingPropertyData));
					SpawnableBuildingData spawnableBuildingData = nativeArray7[num];
					if (!(spawnableBuildingData.m_ZonePrefab != Entity.Null))
					{
						continue;
					}
					Entity zonePrefab = spawnableBuildingData.m_ZonePrefab;
					ZoneData value4 = componentLookup[zonePrefab];
					if (!flag2)
					{
						entityCommandBuffer.SetSharedComponent(e, new BuildingSpawnGroupData(value4.m_ZoneType));
						ushort num2 = (ushort)math.clamp(Mathf.CeilToInt(nativeArray3[num].m_Size.y), 0, 65535);
						if (spawnableBuildingData.m_Level == 1)
						{
							if (prefab4.m_LotWidth == 1 && (value4.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0)
							{
								value4.m_ZoneFlags |= ZoneFlags.SupportNarrow;
								componentLookup[zonePrefab] = value4;
							}
							if (prefab4.m_AccessType == BuildingAccessType.LeftCorner && (value4.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == 0)
							{
								value4.m_ZoneFlags |= ZoneFlags.SupportLeftCorner;
								componentLookup[zonePrefab] = value4;
							}
							if (prefab4.m_AccessType == BuildingAccessType.RightCorner && (value4.m_ZoneFlags & ZoneFlags.SupportRightCorner) == 0)
							{
								value4.m_ZoneFlags |= ZoneFlags.SupportRightCorner;
								componentLookup[zonePrefab] = value4;
							}
							if (prefab4.m_AccessType == BuildingAccessType.Front && prefab4.m_LotWidth <= 3 && prefab4.m_LotDepth <= 2)
							{
								if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 3) && num2 < value4.m_MinOddHeight)
								{
									value4.m_MinOddHeight = num2;
									componentLookup[zonePrefab] = value4;
								}
								if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 2) && num2 < value4.m_MinEvenHeight)
								{
									value4.m_MinEvenHeight = num2;
									componentLookup[zonePrefab] = value4;
								}
							}
						}
						if (num2 > value4.m_MaxHeight)
						{
							value4.m_MaxHeight = num2;
							componentLookup[zonePrefab] = value4;
						}
					}
					int level = spawnableBuildingData.m_Level;
					BuildingData buildingData2 = nativeArray4[num];
					int lotSize = buildingData2.m_LotSize.x * buildingData2.m_LotSize.y;
					if (nativeArray6.Length != 0 && !prefab4.Has<ServiceConsumption>() && componentLookup2.HasComponent(zonePrefab))
					{
						ZoneServiceConsumptionData zoneServiceConsumptionData = componentLookup2[zonePrefab];
						ref ConsumptionData reference = ref nativeArray6.ElementAt(num);
						if (flag2)
						{
							level = 2;
						}
						bool isStorage = buildingPropertyData.m_AllowedStored != Resource.NoResource;
						EconomyParameterData economyParameterData = __query_547773813_0.GetSingleton<EconomyParameterData>();
						reference.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, zoneServiceConsumptionData.m_Upkeep, lotSize, value4.m_AreaType, ref economyParameterData, isStorage);
					}
				}
			}
			if (nativeArray8.Length != 0)
			{
				if (nativeArray9.Length != 0)
				{
					for (int num3 = 0; num3 < nativeArray8.Length; num3++)
					{
						PlaceableObjectData value5 = nativeArray8[num3];
						ObjectGeometryData value6 = nativeArray3[num3];
						ServiceUpgradeData serviceUpgradeData = nativeArray9[num3];
						if (nativeArray4.Length != 0)
						{
							value5.m_Flags |= Game.Objects.PlacementFlags.OwnerSide;
							if (serviceUpgradeData.m_MaxPlacementDistance != 0f)
							{
								value5.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
							}
						}
						if ((value5.m_Flags & Game.Objects.PlacementFlags.NetObject) != Game.Objects.PlacementFlags.None)
						{
							value6.m_Flags |= Game.Objects.GeometryFlags.IgnoreLegCollision;
							if (nativeArray4.Length != 0)
							{
								BuildingData value7 = nativeArray4[num3];
								if ((value7.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
								{
									value7.m_Flags |= BuildingFlags.CanBeOnRoad;
									nativeArray4[num3] = value7;
								}
							}
							if ((value5.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
							{
								value5.m_Flags &= ~(Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.OwnerSide);
							}
						}
						if (nativeArray4.Length != 0 && (value5.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
						{
							BuildingData value8 = nativeArray4[num3];
							value8.m_Flags |= BuildingFlags.CanBeRoadSide;
							nativeArray4[num3] = value8;
						}
						value5.m_ConstructionCost = serviceUpgradeData.m_UpgradeCost;
						nativeArray8[num3] = value5;
						nativeArray3[num3] = value6;
					}
				}
				else
				{
					for (int num4 = 0; num4 < nativeArray8.Length; num4++)
					{
						PlaceableObjectData value9 = nativeArray8[num4];
						ObjectGeometryData value10 = nativeArray3[num4];
						if (nativeArray4.Length != 0)
						{
							value9.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
						}
						if ((value9.m_Flags & Game.Objects.PlacementFlags.NetObject) != Game.Objects.PlacementFlags.None)
						{
							value10.m_Flags |= Game.Objects.GeometryFlags.IgnoreLegCollision;
							if (nativeArray4.Length != 0)
							{
								BuildingData value11 = nativeArray4[num4];
								if ((value11.m_Flags & (BuildingFlags.CanBeOnRoad | BuildingFlags.CanBeOnRoadArea)) == 0)
								{
									value11.m_Flags |= BuildingFlags.CanBeOnRoad;
									nativeArray4[num4] = value11;
								}
							}
							if ((value9.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None)
							{
								value9.m_Flags &= ~Game.Objects.PlacementFlags.RoadSide;
							}
						}
						if (nativeArray4.Length != 0 && (value9.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
						{
							BuildingData value12 = nativeArray4[num4];
							value12.m_Flags |= BuildingFlags.CanBeRoadSide;
							nativeArray4[num4] = value12;
						}
						nativeArray8[num4] = value9;
						nativeArray3[num4] = value10;
					}
				}
			}
			bool flag5 = false;
			if (flag)
			{
				for (int num5 = 0; num5 < nativeArray.Length; num5++)
				{
					if (nativeArray6.Length == 0 || nativeArray6[num5].m_Upkeep <= 0)
					{
						continue;
					}
					bool flag6 = false;
					DynamicBuffer<ServiceUpkeepData> dynamicBuffer3 = bufferAccessor2[num5];
					for (int num6 = 0; num6 < dynamicBuffer3.Length; num6++)
					{
						if (dynamicBuffer3[num6].m_Upkeep.m_Resource == Resource.Money)
						{
							log.WarnFormat("Warning: {0} has monetary upkeep in both ConsumptionData and CityServiceUpkeep", m_PrefabSystem.GetPrefab<PrefabBase>(nativeArray[num5]).name);
						}
					}
					if (!flag6)
					{
						dynamicBuffer3.Add(new ServiceUpkeepData
						{
							m_ScaleWithUsage = false,
							m_Upkeep = new ResourceStack
							{
								m_Amount = nativeArray6[num5].m_Upkeep,
								m_Resource = Resource.Money
							}
						});
						flag5 = true;
					}
				}
			}
			if (bufferAccessor.Length == 0)
			{
				continue;
			}
			for (int num7 = 0; num7 < bufferAccessor.Length; num7++)
			{
				Entity upgrade2 = nativeArray[num7];
				DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer4 = bufferAccessor[num7];
				for (int num8 = 0; num8 < dynamicBuffer4.Length; num8++)
				{
					ServiceUpgradeBuilding serviceUpgradeBuilding3 = dynamicBuffer4[num8];
					base.EntityManager.GetBuffer<BuildingUpgradeElement>(serviceUpgradeBuilding3.m_Building).Add(new BuildingUpgradeElement(upgrade2));
				}
				if (!flag5 && nativeArray6.Length != 0 && nativeArray6[num7].m_Upkeep > 0)
				{
					bufferAccessor2[num7].Add(new ServiceUpkeepData
					{
						m_ScaleWithUsage = false,
						m_Upkeep = new ResourceStack
						{
							m_Amount = nativeArray6[num7].m_Upkeep,
							m_Resource = Resource.Money
						}
					});
				}
			}
		}
		IJobParallelForExtensions.Schedule(new FindConnectionRequirementsJob
		{
			m_SpawnableBuildingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConsumptionDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkplaceDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPumpingStationDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterTowerDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WastewaterTreatmentPlantDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformerDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PublicTransportStationDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CargoTransportStationDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CoverageDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubMeshType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeBuildingType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableObjectDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EffectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_Effect_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GateData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VFXData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VFXData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AudioSourceData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup, ref base.CheckedStateRef),
			m_AudioSpotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioSpotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AudioEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_Chunks = chunks,
			m_BuildingConfigurationData = m_ConfigurationQuery.GetSingleton<BuildingConfigurationData>()
		}, chunks.Length, 1).Complete();
		chunks.Dispose();
		entityCommandBuffer.Playback(base.EntityManager);
		entityCommandBuffer.Dispose();
	}

	private void InitializeLotSize(BuildingPrefab buildingPrefab, BuildingTerraformOverride terraformOverride, ref ObjectGeometryData objectGeometryData, ref BuildingTerraformData buildingTerraformData, ref BuildingData buildingData)
	{
		buildingData.m_LotSize = new int2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
		float2 @float = new float2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
		@float *= 8f;
		bool flag = false;
		Bounds2 flatBounds;
		if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
		{
			int2 @int = default(int2);
			@int.x = Mathf.RoundToInt((objectGeometryData.m_LegSize.x + objectGeometryData.m_LegOffset.x * 2f) / 8f);
			@int.y = Mathf.RoundToInt((objectGeometryData.m_LegSize.z + objectGeometryData.m_LegOffset.y * 2f) / 8f);
			flag = math.all(@int == buildingData.m_LotSize);
			buildingData.m_LotSize = @int;
			float2 xz = objectGeometryData.m_Pivot.xz;
			float2 float2 = objectGeometryData.m_LegSize.xz * 0.5f + objectGeometryData.m_LegOffset;
			flatBounds = new Bounds2(xz - float2, xz + float2);
			objectGeometryData.m_LegSize.xz = (float2)@int * 8f - objectGeometryData.m_LegOffset * 2f - 0.4f;
		}
		else
		{
			flatBounds = objectGeometryData.m_Bounds.xz;
		}
		Bounds2 lotBounds = default(Bounds2);
		lotBounds.max = (float2)buildingData.m_LotSize * 4f;
		lotBounds.min = -lotBounds.max;
		InitializeTerraformData(terraformOverride, ref buildingTerraformData, lotBounds, flatBounds);
		objectGeometryData.m_Layers |= MeshLayer.Default;
		objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(@float.x, 0f, @float.y))));
		switch (buildingPrefab.m_AccessType)
		{
		case BuildingAccessType.LeftCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess;
			break;
		case BuildingAccessType.RightCorner:
			buildingData.m_Flags |= BuildingFlags.RightAccess;
			break;
		case BuildingAccessType.LeftAndRightCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess;
			break;
		case BuildingAccessType.LeftAndBackCorner:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.RightAndBackCorner:
			buildingData.m_Flags |= BuildingFlags.RightAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.FrontAndBack:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.All:
			buildingData.m_Flags |= BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.OnRoad:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		case BuildingAccessType.OnRoadArea:
			buildingData.m_Flags |= BuildingFlags.BackAccess;
			break;
		}
		if (!flag)
		{
			if (math.any(objectGeometryData.m_Size.xz > @float + 0.5f) && AssetDatabase.global.AreAssetsWarningsEnabled(buildingPrefab.asset))
			{
				log.WarnFormat("Building geometry doesn't fit inside the lot ({0}): {1}m x {2}m ({3}x{4})", buildingPrefab.name, objectGeometryData.m_Size.x, objectGeometryData.m_Size.z, buildingData.m_LotSize.x, buildingData.m_LotSize.y);
			}
			@float -= 0.4f;
			objectGeometryData.m_Size.xz = @float;
			objectGeometryData.m_Bounds.min.xz = @float * -0.5f;
			objectGeometryData.m_Bounds.max.xz = @float * 0.5f;
		}
		objectGeometryData.m_Size.y = math.max(objectGeometryData.m_Size.y, 5f);
		objectGeometryData.m_Bounds.min.y = math.min(objectGeometryData.m_Bounds.min.y, 0f);
		objectGeometryData.m_Bounds.max.y = math.max(objectGeometryData.m_Bounds.max.y, 5f);
	}

	public static void InitializeTerraformData(BuildingTerraformOverride terraformOverride, ref BuildingTerraformData buildingTerraformData, Bounds2 lotBounds, Bounds2 flatBounds)
	{
		float3 @float = new float3(1f, 0f, 1f);
		float3 float2 = new float3(1f, 0f, 1f);
		float3 float3 = new float3(1f, 0f, 1f);
		float3 float4 = new float3(1f, 0f, 1f);
		buildingTerraformData.m_Smooth.xy = lotBounds.min;
		buildingTerraformData.m_Smooth.zw = lotBounds.max;
		if (terraformOverride != null)
		{
			flatBounds.min += terraformOverride.m_LevelMinOffset;
			flatBounds.max += terraformOverride.m_LevelMaxOffset;
			@float.x = terraformOverride.m_LevelBackRight.x;
			@float.z = terraformOverride.m_LevelFrontRight.x;
			float2.x = terraformOverride.m_LevelBackRight.y;
			float2.z = terraformOverride.m_LevelBackLeft.y;
			float3.x = terraformOverride.m_LevelBackLeft.x;
			float3.z = terraformOverride.m_LevelFrontLeft.x;
			float4.x = terraformOverride.m_LevelFrontRight.y;
			float4.z = terraformOverride.m_LevelFrontLeft.y;
			buildingTerraformData.m_Smooth.xy += terraformOverride.m_SmoothMinOffset;
			buildingTerraformData.m_Smooth.zw += terraformOverride.m_SmoothMaxOffset;
			buildingTerraformData.m_HeightOffset = terraformOverride.m_HeightOffset;
			buildingTerraformData.m_DontRaise = terraformOverride.m_DontRaise;
			buildingTerraformData.m_DontLower = terraformOverride.m_DontLower;
		}
		float3 float5 = flatBounds.min.x + @float;
		float3 float6 = flatBounds.min.y + float2;
		float3 float7 = flatBounds.max.x - float3;
		float3 float8 = flatBounds.max.y - float4;
		float3 x = (float5 + float7) * 0.5f;
		float3 x2 = (float6 + float8) * 0.5f;
		buildingTerraformData.m_FlatX0 = math.min(float5, math.max(x, float7));
		buildingTerraformData.m_FlatZ0 = math.min(float6, math.max(x2, float8));
		buildingTerraformData.m_FlatX1 = math.max(float7, math.min(x, float5));
		buildingTerraformData.m_FlatZ1 = math.max(float8, math.min(x2, float6));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_547773813_0 = entityQueryBuilder2.Build(ref state);
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
	public BuildingInitializeSystem()
	{
	}
}
