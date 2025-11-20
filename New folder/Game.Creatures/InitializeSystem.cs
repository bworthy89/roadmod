using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Creatures;

[CompilerGenerated]
public class InitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeCreaturesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> m_TripSourceType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		public ComponentTypeHandle<Human> m_HumanType;

		public ComponentTypeHandle<Animal> m_AnimalType;

		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

		public ComponentTypeHandle<HumanNavigation> m_HumanNavigationType;

		public ComponentTypeHandle<AnimalNavigation> m_AnimalNavigationType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Resident> m_ResidentData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberData;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHousehold;

		[ReadOnly]
		public ComponentLookup<Worker> m_WorkerData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_AnimalData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_TripSourceRemoveTypes;

		[ReadOnly]
		public float m_Temperature;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Human> nativeArray2 = chunk.GetNativeArray(ref m_HumanType);
			NativeArray<HumanCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			NativeArray<AnimalCurrentLane> nativeArray4 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			NativeArray<HumanNavigation> nativeArray5 = chunk.GetNativeArray(ref m_HumanNavigationType);
			NativeArray<AnimalNavigation> nativeArray6 = chunk.GetNativeArray(ref m_AnimalNavigationType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (nativeArray5.Length != 0)
			{
				NativeArray<CurrentVehicle> nativeArray7 = chunk.GetNativeArray(ref m_CurrentVehicleType);
				NativeArray<TripSource> nativeArray8 = chunk.GetNativeArray(ref m_TripSourceType);
				NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
				BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
				bool flag = chunk.Has(ref m_UnspawnedType);
				for (int i = 0; i < nativeArray5.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (flag && nativeArray8.Length != 0)
					{
						TripSource tripSource = nativeArray8[i];
						if (m_DeletedData.HasComponent(tripSource.m_Source))
						{
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_TripSourceRemoveTypes);
						}
						if (m_SpawnLocations.HasBuffer(tripSource.m_Source))
						{
							PathOwner pathOwner = nativeArray9[i];
							DynamicBuffer<PathElement> path = bufferAccessor[i];
							DynamicBuffer<SpawnLocationElement> spawnLocations = m_SpawnLocations[tripSource.m_Source];
							Transform value = CalculatePathTransform(entity, pathOwner, path);
							if (!FindClosestSpawnLocation(value.m_Position, out var spawnPosition, spawnLocations, path.Length == 0, ref random, HasAuthorization(entity, tripSource.m_Source)))
							{
								Transform transform = m_TransformData[tripSource.m_Source];
								PrefabRef prefabRef = m_PrefabRefData[tripSource.m_Source];
								if (m_BuildingData.HasComponent(prefabRef.m_Prefab))
								{
									BuildingData buildingData = m_BuildingData[prefabRef.m_Prefab];
									spawnPosition = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
								}
								else
								{
									spawnPosition = transform.m_Position;
								}
							}
							float3 value2 = value.m_Position - spawnPosition;
							if (MathUtils.TryNormalize(ref value2))
							{
								value.m_Position = spawnPosition;
								value.m_Rotation = quaternion.LookRotationSafe(value2, math.up());
							}
							m_TransformData[entity] = value;
						}
						else if (m_TransformData.HasComponent(tripSource.m_Source))
						{
							PathOwner pathOwner2 = nativeArray9[i];
							DynamicBuffer<PathElement> path2 = bufferAccessor[i];
							Transform transform2 = m_TransformData[tripSource.m_Source];
							Transform value3 = CalculatePathTransform(entity, pathOwner2, path2);
							float3 value4 = value3.m_Position - transform2.m_Position;
							if (MathUtils.TryNormalize(ref value4))
							{
								value3.m_Position = transform2.m_Position;
								value3.m_Rotation = quaternion.LookRotationSafe(value4, math.up());
							}
							m_TransformData[entity] = value3;
						}
					}
					Transform transform3 = m_TransformData[entity];
					HumanNavigation value5 = new HumanNavigation
					{
						m_TargetPosition = transform3.m_Position,
						m_TargetDirection = math.normalizesafe(math.forward(transform3.m_Rotation).xz)
					};
					if (CollectionUtils.TryGet(nativeArray7, i, out var value6) && CollectionUtils.TryGet(nativeArray3, i, out var value7) && (value6.m_Flags & CreatureVehicleFlags.Exiting) != 0 && (value7.m_Flags & CreatureLaneFlags.EndOfPath) != 0)
					{
						value5.m_TransformState = TransformState.Action;
						value5.m_LastActivity = 11;
						value5.m_TargetActivity = 11;
					}
					nativeArray5[i] = value5;
				}
			}
			if (nativeArray2.Length != 0)
			{
				NativeArray<PseudoRandomSeed> nativeArray10 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Human value8 = nativeArray2[j];
					value8.m_Flags &= ~(HumanFlags.Cold | HumanFlags.Homeless);
					PseudoRandomSeed value9;
					float num = ((!CollectionUtils.TryGet(nativeArray10, j, out value9)) ? random.NextFloat(15f, 20f) : value9.GetRandom(PseudoRandomSeed.kTemperatureLimit).NextFloat(15f, 20f));
					if (m_Temperature < num)
					{
						value8.m_Flags |= HumanFlags.Cold;
					}
					if (m_ResidentData.TryGetComponent(nativeArray[j], out var componentData) && m_HouseholdMemberData.TryGetComponent(componentData.m_Citizen, out var componentData2) && m_HomelessHousehold.HasComponent(componentData2.m_Household))
					{
						value8.m_Flags |= HumanFlags.Homeless;
					}
					nativeArray2[j] = value8;
				}
			}
			if (nativeArray3.Length != 0)
			{
				for (int k = 0; k < nativeArray3.Length; k++)
				{
					HumanCurrentLane value10 = nativeArray3[k];
					if (m_TransformData.HasComponent(value10.m_Lane))
					{
						value10.m_Flags |= CreatureLaneFlags.TransformTarget;
					}
					else if (m_ConnectionLaneData.HasComponent(value10.m_Lane))
					{
						if ((m_ConnectionLaneData[value10.m_Lane].m_Flags & ConnectionLaneFlags.Area) != 0)
						{
							value10.m_Flags |= CreatureLaneFlags.Area;
						}
						else
						{
							value10.m_Flags |= CreatureLaneFlags.Connection;
						}
					}
					value10.m_LanePosition = random.NextFloat(0f, 1f);
					value10.m_LanePosition *= value10.m_LanePosition;
					value10.m_LanePosition = math.select(0.5f - value10.m_LanePosition, value10.m_LanePosition - 0.5f, m_LeftHandTraffic != ((value10.m_Flags & CreatureLaneFlags.Backward) != 0));
					nativeArray3[k] = value10;
				}
			}
			if (nativeArray4.Length != 0)
			{
				NativeArray<Animal> nativeArray11 = chunk.GetNativeArray(ref m_AnimalType);
				for (int l = 0; l < nativeArray4.Length; l++)
				{
					Entity entity2 = nativeArray[l];
					Animal value11 = nativeArray11[l];
					AnimalCurrentLane value12 = nativeArray4[l];
					PrefabRef prefabRef2 = m_PrefabRefData[entity2];
					AnimalData animalData = m_AnimalData[prefabRef2.m_Prefab];
					if (m_TransformData.HasComponent(value12.m_Lane))
					{
						value12.m_Flags |= CreatureLaneFlags.TransformTarget;
					}
					else if (m_ConnectionLaneData.HasComponent(value12.m_Lane))
					{
						if ((m_ConnectionLaneData[value12.m_Lane].m_Flags & ConnectionLaneFlags.Area) != 0)
						{
							value12.m_Flags |= CreatureLaneFlags.Area;
						}
						else
						{
							value12.m_Flags |= CreatureLaneFlags.Connection;
						}
					}
					if (animalData.m_MoveSpeed == 0f && animalData.m_SwimSpeed > 0f && animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Swimming)
					{
						value11.m_Flags |= AnimalFlags.SwimmingTarget;
						value12.m_Flags |= CreatureLaneFlags.Swimming;
					}
					if ((value11.m_Flags & AnimalFlags.Roaming) != 0)
					{
						value12.m_LanePosition = random.NextFloat(-0.5f, 0.5f);
					}
					else
					{
						value12.m_LanePosition = random.NextFloat(0f, 1f);
						value12.m_LanePosition *= value12.m_LanePosition;
						value12.m_LanePosition = math.select(0.5f - value12.m_LanePosition, value12.m_LanePosition - 0.5f, m_LeftHandTraffic != ((value12.m_Flags & CreatureLaneFlags.Backward) != 0));
					}
					nativeArray11[l] = value11;
					nativeArray4[l] = value12;
				}
			}
			if (nativeArray6.Length == 0)
			{
				return;
			}
			NativeArray<TripSource> nativeArray12 = chunk.GetNativeArray(ref m_TripSourceType);
			bool flag2 = chunk.Has(ref m_UnspawnedType);
			for (int m = 0; m < nativeArray6.Length; m++)
			{
				Entity entity3 = nativeArray[m];
				if (flag2 && nativeArray12.Length != 0)
				{
					TripSource tripSource2 = nativeArray12[m];
					if (m_DeletedData.HasComponent(tripSource2.m_Source))
					{
						m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity3, in m_TripSourceRemoveTypes);
					}
					if (m_SpawnLocations.HasBuffer(tripSource2.m_Source))
					{
						DynamicBuffer<SpawnLocationElement> spawnLocations2 = m_SpawnLocations[tripSource2.m_Source];
						Transform value13 = m_TransformData[entity3];
						if (!FindClosestSpawnLocation(value13.m_Position, out var spawnPosition2, spawnLocations2, randomLocation: false, ref random, hasAuthorization: false))
						{
							Transform transform4 = m_TransformData[tripSource2.m_Source];
							PrefabRef prefabRef3 = m_PrefabRefData[tripSource2.m_Source];
							if (m_BuildingData.HasComponent(prefabRef3.m_Prefab))
							{
								BuildingData buildingData2 = m_BuildingData[prefabRef3.m_Prefab];
								spawnPosition2 = BuildingUtils.CalculateFrontPosition(transform4, buildingData2.m_LotSize.y);
							}
							else
							{
								spawnPosition2 = transform4.m_Position;
							}
						}
						float3 value14 = value13.m_Position - spawnPosition2;
						if (MathUtils.TryNormalize(ref value14))
						{
							value13.m_Position = spawnPosition2;
							value13.m_Rotation = quaternion.LookRotationSafe(value14, math.up());
						}
						m_TransformData[entity3] = value13;
					}
					else if (m_TransformData.HasComponent(tripSource2.m_Source))
					{
						PrefabRef prefabRef4 = m_PrefabRefData[entity3];
						AnimalData animalData2 = m_AnimalData[prefabRef4.m_Prefab];
						Transform transform5 = m_TransformData[tripSource2.m_Source];
						Transform value15 = m_TransformData[entity3];
						float3 value16 = value15.m_Position - transform5.m_Position;
						if (MathUtils.TryNormalize(ref value16))
						{
							value15.m_Position = transform5.m_Position;
							value15.m_Rotation = quaternion.LookRotationSafe(value16, math.up());
						}
						bool hasDepth;
						float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, value15.m_Position, out hasDepth);
						if (animalData2.m_PrimaryTravelMethod != AnimalTravelFlags.Swimming && num2 > value15.m_Position.y)
						{
							value15.m_Position.y = num2 + (hasDepth ? (-0.2f) : 0f);
						}
						else if (animalData2.m_SwimSpeed > 0f && (num2 - animalData2.m_SwimDepth.max > value15.m_Position.y || num2 - animalData2.m_SwimDepth.min < value15.m_Position.y))
						{
							value15.m_Position.y = random.NextFloat(num2 - animalData2.m_SwimDepth.max, num2 - animalData2.m_SwimDepth.min);
						}
						if (nativeArray4.Length != 0 && (nativeArray4[m].m_Flags & CreatureLaneFlags.Swimming) != 0)
						{
							value15.m_Position.y -= animalData2.m_SwimDepth.min;
						}
						m_TransformData[entity3] = value15;
					}
				}
				Transform transform6 = m_TransformData[entity3];
				nativeArray6[m] = new AnimalNavigation
				{
					m_TargetPosition = transform6.m_Position,
					m_TargetDirection = math.normalizesafe(math.forward(transform6.m_Rotation))
				};
			}
		}

		private Transform CalculatePathTransform(Entity creature, PathOwner pathOwner, DynamicBuffer<PathElement> path)
		{
			Transform result = m_TransformData[creature];
			if (path.Length > pathOwner.m_ElementIndex)
			{
				PathElement pathElement = path[pathOwner.m_ElementIndex];
				if (m_CurveData.HasComponent(pathElement.m_Target))
				{
					Curve curve = m_CurveData[pathElement.m_Target];
					result.m_Position = MathUtils.Position(curve.m_Bezier, pathElement.m_TargetDelta.x);
					float3 value = MathUtils.Tangent(curve.m_Bezier, pathElement.m_TargetDelta.x);
					if (MathUtils.TryNormalize(ref value))
					{
						result.m_Rotation = quaternion.LookRotationSafe(value, math.up());
					}
				}
			}
			return result;
		}

		private bool HasAuthorization(Entity entity, Entity building)
		{
			if (m_ResidentData.TryGetComponent(entity, out var componentData))
			{
				if (m_HouseholdMemberData.TryGetComponent(componentData.m_Citizen, out var componentData2) && m_PropertyRenterData.TryGetComponent(componentData2.m_Household, out var componentData3) && componentData3.m_Property == building)
				{
					return true;
				}
				if (m_WorkerData.TryGetComponent(componentData.m_Citizen, out var componentData4))
				{
					if (m_PropertyRenterData.TryGetComponent(componentData4.m_Workplace, out var componentData5))
					{
						if (componentData5.m_Property == building)
						{
							return true;
						}
					}
					else if (componentData4.m_Workplace == building)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool FindClosestSpawnLocation(float3 comparePosition, out float3 spawnPosition, DynamicBuffer<SpawnLocationElement> spawnLocations, bool randomLocation, ref Random random, bool hasAuthorization)
		{
			spawnPosition = comparePosition;
			float num = float.MaxValue;
			bool flag = true;
			bool result = false;
			for (int i = 0; i < spawnLocations.Length; i++)
			{
				if (spawnLocations[i].m_Type != SpawnLocationType.SpawnLocation && spawnLocations[i].m_Type != SpawnLocationType.HangaroundLocation)
				{
					continue;
				}
				Entity spawnLocation = spawnLocations[i].m_SpawnLocation;
				PrefabRef prefabRef = m_PrefabRefData[spawnLocation];
				if (!m_SpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || (componentData.m_ConnectionType != RouteConnectionType.Pedestrian && (componentData.m_ConnectionType != RouteConnectionType.Parking || componentData.m_RoadTypes != RoadTypes.Bicycle)))
				{
					continue;
				}
				bool flag2 = componentData.m_ActivityMask.m_Mask != 0;
				if (flag2 && !flag)
				{
					continue;
				}
				if (m_TransformData.HasComponent(spawnLocation))
				{
					Transform transform = m_TransformData[spawnLocation];
					float num2;
					if (randomLocation)
					{
						num2 = random.NextFloat();
						num2 += math.select(0f, 1f, hasAuthorization != componentData.m_RequireAuthorization);
					}
					else
					{
						num2 = math.distance(transform.m_Position, comparePosition);
					}
					if ((!flag2 && flag) || num2 < num)
					{
						spawnPosition = transform.m_Position;
						num = num2;
						flag = flag2;
						result = true;
					}
				}
				else
				{
					if (!m_AreaNodes.HasBuffer(spawnLocation))
					{
						continue;
					}
					DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[spawnLocation];
					DynamicBuffer<Triangle> dynamicBuffer = m_AreaTriangles[spawnLocation];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[j]);
						float num3;
						float2 @float;
						if (randomLocation)
						{
							num3 = random.NextFloat();
							num3 += math.select(0f, 1f, hasAuthorization != componentData.m_RequireAuthorization);
							@float = random.NextFloat2();
							@float = math.select(@float, 1f - @float, math.csum(@float) > 1f);
						}
						else
						{
							num3 = MathUtils.Distance(triangle, comparePosition, out @float);
						}
						if ((!flag2 && flag) || num3 < num)
						{
							spawnPosition = MathUtils.Position(triangle, @float);
							num = num3;
							flag = flag2;
							result = true;
						}
					}
				}
			}
			return result;
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
		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Human> __Game_Creatures_Human_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RW_ComponentTypeHandle;

		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<HumanNavigation> __Game_Creatures_HumanNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Creatures_Human_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Human>();
			__Game_Creatures_Animal_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>();
			__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>();
			__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanNavigation>();
			__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Resident>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ClimateSystem m_ClimateSystem;

	private EntityQuery m_CreatureQuery;

	private ComponentTypeSet m_TripSourceRemoveTypes;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Creature>(), ComponentType.ReadOnly<Updated>());
		m_TripSourceRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<TripSource>(), ComponentType.ReadWrite<Unspawned>());
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		InitializeCreaturesJob jobData = new InitializeCreaturesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HumanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Human_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHousehold = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TripSourceRemoveTypes = m_TripSourceRemoveTypes,
			m_Temperature = m_ClimateSystem.temperature,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatureQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
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
	public InitializeSystem()
	{
	}
}
