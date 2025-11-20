using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class BatchDataSystem : GameSystemBase, IPostDeserialize
{
	private struct UpdateMask
	{
		private uint m_Mask;

		public void UpdateAll()
		{
			m_Mask = uint.MaxValue;
		}

		public void UpdateTransform()
		{
			m_Mask |= 1u;
		}

		public bool ShouldUpdateAll()
		{
			return m_Mask == uint.MaxValue;
		}

		public bool ShouldUpdateNothing()
		{
			return m_Mask == 0;
		}

		public bool ShouldUpdateTransform()
		{
			return (m_Mask & 1) != 0;
		}

		public void UpdateProperty(ObjectProperty property)
		{
			m_Mask |= (uint)(2 << (int)property);
		}

		public bool ShouldUpdateProperty(ObjectProperty property)
		{
			return (m_Mask & (uint)(2 << (int)property)) != 0;
		}

		public bool ShouldUpdateProperty(ObjectProperty property, in GroupData groupData, out int index)
		{
			index = -1;
			if ((m_Mask & (uint)(2 << (int)property)) != 0)
			{
				return groupData.GetPropertyIndex((int)property, out index);
			}
			return false;
		}

		public void UpdateProperty(NetProperty property)
		{
			m_Mask |= (uint)(2 << (int)property);
		}

		public bool ShouldUpdateProperty(NetProperty property)
		{
			return (m_Mask & (uint)(2 << (int)property)) != 0;
		}

		public bool ShouldUpdateProperty(NetProperty property, in GroupData groupData, out int index)
		{
			index = -1;
			if ((m_Mask & (uint)(2 << (int)property)) != 0)
			{
				return groupData.GetPropertyIndex((int)property, out index);
			}
			return false;
		}

		public void UpdateProperty(LaneProperty property)
		{
			m_Mask |= (uint)(2 << (int)property);
		}

		public bool ShouldUpdateProperty(LaneProperty property, in GroupData groupData, out int index)
		{
			index = -1;
			if ((m_Mask & (uint)(2 << (int)property)) != 0)
			{
				return groupData.GetPropertyIndex((int)property, out index);
			}
			return false;
		}

		public bool ShouldUpdateProperty(LaneProperty property)
		{
			return (m_Mask & (uint)(2 << (int)property)) != 0;
		}

		public void UpdateProperty(ZoneProperty property)
		{
			m_Mask |= (uint)(2 << (int)property);
		}

		public bool ShouldUpdateProperty(ZoneProperty property, in GroupData groupData, out int index)
		{
			index = -1;
			if ((m_Mask & (uint)(2 << (int)property)) != 0)
			{
				return groupData.GetPropertyIndex((int)property, out index);
			}
			return false;
		}

		public bool ShouldUpdateProperty(ZoneProperty property)
		{
			return (m_Mask & (uint)(2 << (int)property)) != 0;
		}
	}

	private struct UpdateMasks
	{
		public UpdateMask m_ObjectMask;

		public UpdateMask m_NetMask;

		public UpdateMask m_LaneMask;

		public UpdateMask m_ZoneMask;

		public void UpdateAll()
		{
			m_ObjectMask.UpdateAll();
			m_NetMask.UpdateAll();
			m_LaneMask.UpdateAll();
			m_ZoneMask.UpdateAll();
		}

		public bool ShouldUpdateAll()
		{
			return m_ObjectMask.ShouldUpdateAll();
		}
	}

	private enum SmoothingType
	{
		SurfaceWetness,
		SurfaceDamage,
		SurfaceDirtyness,
		ColorMask
	}

	private struct SmoothingNeeded : IAccumulable<SmoothingNeeded>
	{
		private uint m_Value;

		public SmoothingNeeded(SmoothingType type)
		{
			m_Value = (uint)(1 << (int)type);
		}

		public bool IsNeeded(SmoothingType type)
		{
			return (m_Value & (uint)(1 << (int)type)) != 0;
		}

		public void Accumulate(SmoothingNeeded other)
		{
			m_Value |= other.m_Value;
		}
	}

	private struct CellTypes
	{
		public float4x4 m_CellTypes0;

		public float4x4 m_CellTypes1;

		public float4x4 m_CellTypes2;

		public float4x4 m_CellTypes3;
	}

	[BurstCompile]
	private struct BatchDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Error> m_ErrorData;

		[ReadOnly]
		public ComponentLookup<Warning> m_WarningData;

		[ReadOnly]
		public ComponentLookup<Override> m_OverrideData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[ReadOnly]
		public BufferLookup<FadeBatch> m_FadeBatches;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<Animated> m_Animateds;

		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Emissive> m_Emissives;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_ObjectTransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> m_ObjectColorData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ObjectElevationData;

		[ReadOnly]
		public ComponentLookup<Surface> m_ObjectSurfaceData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_ObjectDamagedData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<CitizenPresence> m_CitizenPresenceData;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_BuildingAbandonedData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public BufferLookup<Passenger> m_Passengers;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<EdgeColor> m_NetEdgeColorData;

		[ReadOnly]
		public ComponentLookup<NodeColor> m_NetNodeColorData;

		[ReadOnly]
		public ComponentLookup<LaneColor> m_LaneColorData;

		[ReadOnly]
		public ComponentLookup<LaneCondition> m_LaneConditionData;

		[ReadOnly]
		public ComponentLookup<HangingLane> m_HangingLaneData;

		[ReadOnly]
		public BufferLookup<SubFlow> m_SubFlows;

		[ReadOnly]
		public BufferLookup<CutRange> m_CutRanges;

		[ReadOnly]
		public ComponentLookup<Block> m_ZoneBlockData;

		[ReadOnly]
		public BufferLookup<Cell> m_ZoneCells;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> m_PrefabGrowthScaleData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PrefabPublicTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_PrefabSubMeshGroups;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_PrefabAnimationClips;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public float m_LightFactor;

		[ReadOnly]
		public float m_FrameDelta;

		[ReadOnly]
		public float4 m_BuildingStateOverride;

		[ReadOnly]
		public PreCullingFlags m_CullingFlags;

		[ReadOnly]
		public float m_SmoothnessDelta;

		[ReadOnly]
		public UpdateMasks m_UpdateMasks;

		[ReadOnly]
		public RenderingSettingsData m_RenderingSettingsData;

		[ReadOnly]
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public CellMapData<Wind> m_WindData;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceWriter m_NativeBatchInstances;

		public NativeAccumulator<SmoothingNeeded>.ParallelWriter m_SmoothingNeeded;

		public void Execute(int index)
		{
			PreCullingData preCullingData = m_CullingData[index];
			if ((preCullingData.m_Flags & m_CullingFlags) != 0 && (preCullingData.m_Flags & PreCullingFlags.NearCamera) != 0)
			{
				UpdateMasks updateMasks = m_UpdateMasks;
				if (!updateMasks.ShouldUpdateAll() && (preCullingData.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated)) != 0)
				{
					updateMasks.UpdateAll();
				}
				if ((preCullingData.m_Flags & PreCullingFlags.Object) != 0)
				{
					UpdateObjectData(preCullingData, updateMasks.m_ObjectMask);
				}
				else if ((preCullingData.m_Flags & PreCullingFlags.Net) != 0)
				{
					UpdateNetData(preCullingData, updateMasks.m_NetMask);
				}
				else if ((preCullingData.m_Flags & PreCullingFlags.Lane) != 0)
				{
					UpdateLaneData(preCullingData, updateMasks.m_LaneMask);
				}
				else if ((preCullingData.m_Flags & PreCullingFlags.Zone) != 0)
				{
					UpdateZoneData(preCullingData, updateMasks.m_ZoneMask);
				}
				else if ((preCullingData.m_Flags & PreCullingFlags.FadeContainer) != 0)
				{
					UpdateFadeData(preCullingData);
				}
			}
		}

		private void UpdateObjectData(PreCullingData preCullingData, UpdateMask updateMask)
		{
			if (!updateMask.ShouldUpdateTransform() && (m_CullingFlags & preCullingData.m_Flags & (PreCullingFlags.TreeGrowth | PreCullingFlags.InterpolatedTransform)) != 0)
			{
				updateMask.UpdateTransform();
			}
			if (!updateMask.ShouldUpdateProperty(ObjectProperty.ColorMask1) && (preCullingData.m_Flags & PreCullingFlags.ColorsUpdated) != 0)
			{
				updateMask.UpdateProperty(ObjectProperty.ColorMask1);
				updateMask.UpdateProperty(ObjectProperty.ColorMask2);
				updateMask.UpdateProperty(ObjectProperty.ColorMask3);
			}
			if (updateMask.ShouldUpdateNothing() || !m_MeshBatches.TryGetBuffer(preCullingData.m_Entity, out var bufferData))
			{
				return;
			}
			m_MeshGroups.TryGetBuffer(preCullingData.m_Entity, out var bufferData2);
			for (int i = 0; i < bufferData.Length; i++)
			{
				MeshBatch meshBatch = bufferData[i];
				GroupData groupData = m_NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);
				if (updateMask.ShouldUpdateAll() && (preCullingData.m_Flags & PreCullingFlags.Temp) != 0)
				{
					Temp temp = m_TempData[preCullingData.m_Entity];
					if (m_MeshBatches.TryGetBuffer(temp.m_Original, out var bufferData3))
					{
						for (int j = 0; j < bufferData3.Length; j++)
						{
							MeshBatch meshBatch2 = bufferData3[j];
							if (meshBatch2.m_MeshGroup == meshBatch.m_MeshGroup && meshBatch2.m_MeshIndex == meshBatch.m_MeshIndex && meshBatch2.m_TileIndex == meshBatch.m_TileIndex)
							{
								ref CullingData reference = ref m_NativeBatchInstances.AccessCullingData(meshBatch2.m_GroupIndex, meshBatch2.m_InstanceIndex);
								ref CullingData reference2 = ref m_NativeBatchInstances.AccessCullingData(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
								reference2.lodFade = reference.lodFade;
								int2 zw = reference2.lodFade.zw;
								int4 @int = math.select(new int4(zw, -zw), new int4(-zw, zw), (((groupData.m_LodCount - reference2.lodFade.xy) & 1) != 0).xyxy);
								@int.xz = (1065353471 - @int.xz) | (255 - @int.yw << 11);
								if (updateMask.ShouldUpdateProperty(ObjectProperty.LodFade0, in groupData, out var index))
								{
									m_NativeBatchInstances.SetPropertyValue(@int.x, meshBatch.m_GroupIndex, index, meshBatch.m_InstanceIndex);
								}
								if (updateMask.ShouldUpdateProperty(ObjectProperty.LodFade1, in groupData, out var index2))
								{
									m_NativeBatchInstances.SetPropertyValue(@int.z, meshBatch.m_GroupIndex, index2, meshBatch.m_InstanceIndex);
								}
								if (m_NativeBatchInstances.InitializeTransform(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex, meshBatch2.m_GroupIndex, meshBatch2.m_InstanceIndex))
								{
									break;
								}
							}
						}
					}
				}
				if (updateMask.ShouldUpdateTransform())
				{
					CullingInfo cullingInfo = m_CullingInfoData[preCullingData.m_Entity];
					PrefabRef prefabRef = m_PrefabRefData[preCullingData.m_Entity];
					int num = meshBatch.m_MeshIndex;
					if (CollectionUtils.TryGet(bufferData2, meshBatch.m_MeshGroup, out var value) && m_PrefabSubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData4))
					{
						num += bufferData4[value.m_SubMeshGroup].m_SubMeshRange.x;
					}
					SubMesh subMesh = m_PrefabSubMeshes[prefabRef.m_Prefab][num];
					float3 subMeshScale = 1f;
					if ((preCullingData.m_Flags & PreCullingFlags.TreeGrowth) != 0 && m_PrefabGrowthScaleData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						BatchDataHelpers.CalculateTreeSubMeshData(m_TreeData[preCullingData.m_Entity], componentData, out var scale);
						subMeshScale *= scale;
					}
					if ((subMesh.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd)) != 0 && m_StackData.TryGetComponent(preCullingData.m_Entity, out var componentData2) && m_PrefabStackData.HasComponent(prefabRef.m_Prefab))
					{
						StackData stackData = m_PrefabStackData[prefabRef.m_Prefab];
						BatchDataHelpers.CalculateStackSubMeshData(componentData2, stackData, out var _, out var offsets, out var scale2);
						BatchDataHelpers.CalculateStackSubMeshData(stackData, offsets, scale2, meshBatch.m_TileIndex, subMesh.m_Flags, ref subMesh.m_Position, ref subMeshScale);
					}
					Game.Objects.Transform transform = (((preCullingData.m_Flags & PreCullingFlags.InterpolatedTransform) == 0) ? m_ObjectTransformData[preCullingData.m_Entity] : m_InterpolatedTransformData[preCullingData.m_Entity].ToTransform());
					float3 @float;
					quaternion quaternion;
					if ((subMesh.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd | SubMeshFlags.HasTransform)) != 0)
					{
						@float = transform.m_Position + math.rotate(transform.m_Rotation, subMesh.m_Position);
						quaternion = math.mul(transform.m_Rotation, subMesh.m_Rotation);
					}
					else
					{
						@float = transform.m_Position;
						quaternion = transform.m_Rotation;
					}
					int lodOffset = 0;
					float3x4 float3x;
					float3x4 secondaryValue;
					if ((subMesh.m_Flags & SubMeshFlags.DefaultMissingMesh) != 0)
					{
						MeshData meshData = m_PrefabMeshData[subMesh.m_SubMesh];
						ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
						if ((subMesh.m_Flags & SubMeshFlags.IsStackMiddle) != 0)
						{
							switch (m_PrefabStackData[prefabRef.m_Prefab].m_Direction)
							{
							case StackDirection.Right:
								objectGeometryData.m_Bounds.x = new Bounds1(-1f, 1f);
								break;
							case StackDirection.Up:
								objectGeometryData.m_Bounds.y = new Bounds1(-1f, 1f);
								break;
							case StackDirection.Forward:
								objectGeometryData.m_Bounds.z = new Bounds1(-1f, 1f);
								break;
							}
						}
						float3 translation = @float + math.rotate(quaternion, subMeshScale * MathUtils.Center(objectGeometryData.m_Bounds));
						float3 scale3 = subMeshScale * MathUtils.Size(objectGeometryData.m_Bounds) * 0.5f;
						float3x = TransformHelper.TRS(translation, quaternion, scale3);
						secondaryValue = float3x;
						lodOffset = objectGeometryData.m_MinLod - meshData.m_MinLod;
					}
					else
					{
						float3 translation2 = @float + math.rotate(quaternion, subMeshScale * groupData.m_SecondaryCenter);
						float3 scale4 = subMeshScale * groupData.m_SecondarySize;
						float3x = TransformHelper.TRS(@float, quaternion, subMeshScale);
						secondaryValue = TransformHelper.TRS(translation2, quaternion, scale4);
					}
					ref CullingData reference3 = ref m_NativeBatchInstances.SetTransformValue(float3x, secondaryValue, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
					reference3.m_Bounds = cullingInfo.m_Bounds;
					reference3.isHidden = m_HiddenData.HasComponent(preCullingData.m_Entity);
					reference3.lodOffset = lodOffset;
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.InfoviewColor, in groupData, out var index3))
				{
					float2 value2 = default(float2);
					if ((preCullingData.m_Flags & PreCullingFlags.InfoviewColor) != 0)
					{
						Owner componentData4;
						if (m_ObjectColorData.TryGetComponent(preCullingData.m_Entity, out var componentData3))
						{
							value2 = new float2((float)(int)componentData3.m_Index + 0.5f, (float)(int)componentData3.m_Value * 0.003921569f);
						}
						else if (m_OwnerData.TryGetComponent(preCullingData.m_Entity, out componentData4))
						{
							Game.Objects.Elevation componentData5;
							bool flag = m_ObjectElevationData.TryGetComponent(preCullingData.m_Entity, out componentData5) && (componentData5.m_Flags & ElevationFlags.OnGround) == 0;
							while (true)
							{
								if (m_ObjectColorData.TryGetComponent(componentData4.m_Owner, out var componentData6))
								{
									if (flag || componentData6.m_SubColor)
									{
										value2 = new float2((float)(int)componentData6.m_Index + 0.5f, (float)(int)componentData6.m_Value * 0.003921569f);
									}
									break;
								}
								if (!m_OwnerData.TryGetComponent(componentData4.m_Owner, out var componentData7))
								{
									break;
								}
								flag &= m_ObjectElevationData.TryGetComponent(componentData4.m_Owner, out componentData5) && (componentData5.m_Flags & ElevationFlags.OnGround) == 0;
								componentData4 = componentData7;
							}
						}
					}
					m_NativeBatchInstances.SetPropertyValue(value2, meshBatch.m_GroupIndex, index3, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.BuildingState, in groupData, out var index4))
				{
					float4 value3;
					if (m_EditorMode)
					{
						value3 = m_BuildingStateOverride;
					}
					else
					{
						Entity entity = preCullingData.m_Entity;
						if ((preCullingData.m_Flags & PreCullingFlags.Temp) != 0)
						{
							Temp temp2 = m_TempData[preCullingData.m_Entity];
							if (temp2.m_Original != Entity.Null)
							{
								entity = temp2.m_Original;
							}
						}
						m_PseudoRandomSeedData.TryGetComponent(entity, out var componentData8);
						bool flag2 = m_DestroyedData.HasComponent(entity);
						if (m_VehicleData.HasComponent(entity))
						{
							int num2 = 0;
							int passengersCount = 0;
							if (m_PublicTransportData.TryGetComponent(entity, out var componentData9))
							{
								PrefabRef prefabRef2 = m_PrefabRefData[entity];
								if (m_PrefabPublicTransportVehicleData.TryGetComponent(prefabRef2.m_Prefab, out var componentData10))
								{
									num2 = componentData10.m_PassengerCapacity;
								}
								DynamicBuffer<Passenger> bufferData5;
								if ((componentData9.m_State & PublicTransportFlags.DummyTraffic) != 0)
								{
									passengersCount = componentData8.GetRandom(PseudoRandomSeed.kDummyPassengers).NextInt(0, num2 + 1);
								}
								else if (m_Passengers.TryGetBuffer(entity, out bufferData5))
								{
									passengersCount = bufferData5.Length;
								}
							}
							else
							{
								Unity.Mathematics.Random random = componentData8.GetRandom(PseudoRandomSeed.kDummyPassengers);
								num2 = 1000;
								passengersCount = random.NextInt(0, num2 + 1);
							}
							value3 = BatchDataHelpers.GetBuildingState(componentData8, passengersCount, num2, m_LightFactor, flag2);
						}
						else
						{
							if (m_OwnerData.TryGetComponent(entity, out var componentData11))
							{
								entity = componentData11.m_Owner;
							}
							m_CitizenPresenceData.TryGetComponent(entity, out var componentData12);
							bool flag3 = m_BuildingAbandonedData.HasComponent(entity);
							bool electricity = true;
							if (m_BuildingData.TryGetComponent(entity, out var componentData13))
							{
								electricity = (componentData13.m_Flags & Game.Buildings.BuildingFlags.Illuminated) != 0;
							}
							value3 = BatchDataHelpers.GetBuildingState(componentData8, componentData12, m_LightFactor, flag3 || flag2, electricity);
						}
					}
					m_NativeBatchInstances.SetPropertyValue(value3, meshBatch.m_GroupIndex, index4, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.ColorMask1))
				{
					UnityEngine.Color color;
					UnityEngine.Color color2;
					UnityEngine.Color color3;
					if (m_MeshColors.TryGetBuffer(preCullingData.m_Entity, out var bufferData6))
					{
						int num3 = meshBatch.m_MeshIndex;
						if (CollectionUtils.TryGet(bufferData2, meshBatch.m_MeshGroup, out var value4))
						{
							num3 += value4.m_ColorOffset;
						}
						MeshColor meshColor = bufferData6[num3];
						color = meshColor.m_ColorSet.m_Channel0.linear;
						color2 = meshColor.m_ColorSet.m_Channel1.linear;
						color3 = meshColor.m_ColorSet.m_Channel2.linear;
					}
					else
					{
						color = UnityEngine.Color.white;
						color2 = UnityEngine.Color.white;
						color3 = UnityEngine.Color.white;
					}
					bool smooth = (preCullingData.m_Flags & PreCullingFlags.SmoothColor) != 0;
					UpdateColorMask(in groupData, in meshBatch, updateMask, ObjectProperty.ColorMask1, color, smooth);
					UpdateColorMask(in groupData, in meshBatch, updateMask, ObjectProperty.ColorMask2, color2, smooth);
					UpdateColorMask(in groupData, in meshBatch, updateMask, ObjectProperty.ColorMask3, color3, smooth);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.BoneParameters, in groupData, out var index5))
				{
					float2 value6;
					if (m_Skeletons.TryGetBuffer(preCullingData.m_Entity, out var bufferData7))
					{
						int num4 = meshBatch.m_MeshIndex;
						if (CollectionUtils.TryGet(bufferData2, meshBatch.m_MeshGroup, out var value5))
						{
							num4 += value5.m_MeshOffset;
						}
						value6 = BatchDataHelpers.GetBoneParameters(bufferData7[num4]);
					}
					else if ((preCullingData.m_Flags & PreCullingFlags.Animated) != 0)
					{
						DynamicBuffer<Animated> dynamicBuffer = m_Animateds[preCullingData.m_Entity];
						PrefabRef prefabRef3 = m_PrefabRefData[preCullingData.m_Entity];
						int index6 = meshBatch.m_MeshIndex;
						if (m_PrefabSubMeshGroups.HasBuffer(prefabRef3.m_Prefab))
						{
							index6 = ((bufferData2.IsCreated && bufferData2.Length != 0) ? meshBatch.m_MeshGroup : 0);
						}
						value6 = BatchDataHelpers.GetBoneParameters(dynamicBuffer[index6]);
					}
					else
					{
						value6 = Unity.Mathematics.float2.zero;
					}
					m_NativeBatchInstances.SetPropertyValue(value6, meshBatch.m_GroupIndex, index5, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.LightParameters, in groupData, out var index7))
				{
					int num5 = meshBatch.m_MeshIndex;
					if (CollectionUtils.TryGet(bufferData2, meshBatch.m_MeshGroup, out var value7))
					{
						num5 += value7.m_MeshOffset;
					}
					DynamicBuffer<Emissive> bufferData8;
					float2 value8 = ((!m_Emissives.TryGetBuffer(preCullingData.m_Entity, out bufferData8)) ? Unity.Mathematics.float2.zero : BatchDataHelpers.GetLightParameters(bufferData8[num5]));
					m_NativeBatchInstances.SetPropertyValue(value8, meshBatch.m_GroupIndex, index7, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.OutlineColors, in groupData, out var index8))
				{
					UnityEngine.Color linear = (m_ErrorData.HasComponent(preCullingData.m_Entity) ? m_RenderingSettingsData.m_ErrorColor : (m_WarningData.HasComponent(preCullingData.m_Entity) ? m_RenderingSettingsData.m_WarningColor : (m_OverrideData.HasComponent(preCullingData.m_Entity) ? m_RenderingSettingsData.m_OverrideColor : (((preCullingData.m_Flags & PreCullingFlags.Temp) == 0) ? m_RenderingSettingsData.m_HoveredColor : (((m_TempData[preCullingData.m_Entity].m_Flags & TempFlags.Parent) == 0) ? m_RenderingSettingsData.m_HoveredColor : m_RenderingSettingsData.m_OwnerColor))))).linear;
					m_NativeBatchInstances.SetPropertyValue(linear, meshBatch.m_GroupIndex, index8, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.MetaParameters, in groupData, out var index9))
				{
					float value9 = 0f;
					if ((preCullingData.m_Flags & PreCullingFlags.Animated) != 0)
					{
						DynamicBuffer<Animated> dynamicBuffer2 = m_Animateds[preCullingData.m_Entity];
						PrefabRef prefabRef4 = m_PrefabRefData[preCullingData.m_Entity];
						int index10 = meshBatch.m_MeshIndex;
						if (m_PrefabSubMeshGroups.HasBuffer(prefabRef4.m_Prefab))
						{
							index10 = ((bufferData2.IsCreated && bufferData2.Length != 0) ? meshBatch.m_MeshGroup : 0);
						}
						value9 = math.asfloat(dynamicBuffer2[index10].m_MetaIndex);
					}
					m_NativeBatchInstances.SetPropertyValue(value9, meshBatch.m_GroupIndex, index9, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.SurfaceWetness, in groupData, out var index11))
				{
					float4 float2 = default(float4);
					if ((preCullingData.m_Flags & PreCullingFlags.SurfaceState) != 0)
					{
						Owner componentData15;
						if (m_ObjectSurfaceData.TryGetComponent(preCullingData.m_Entity, out var componentData14))
						{
							float2 = BatchDataHelpers.GetWetness(componentData14);
						}
						else if (m_OwnerData.TryGetComponent(preCullingData.m_Entity, out componentData15))
						{
							while (true)
							{
								if (m_ObjectSurfaceData.TryGetComponent(componentData15.m_Owner, out var componentData16))
								{
									float2 = BatchDataHelpers.GetWetness(componentData16);
									break;
								}
								if (!m_OwnerData.HasComponent(componentData15.m_Owner))
								{
									break;
								}
								componentData15 = m_OwnerData[componentData15.m_Owner];
							}
						}
					}
					if (m_NativeBatchInstances.GetPropertyValue<float4>(out var value10, meshBatch.m_GroupIndex, index11, meshBatch.m_InstanceIndex))
					{
						if (math.any(value10 != float2))
						{
							bool4 test = float2 < value10;
							value10 += math.select(m_SmoothnessDelta, 0f - m_SmoothnessDelta, test);
							value10 = math.clamp(value10, math.select(0f, float2, test), math.select(float2, 1f, test));
							m_NativeBatchInstances.SetPropertyValue(value10, meshBatch.m_GroupIndex, index11, meshBatch.m_InstanceIndex);
							if (math.any(value10 != float2))
							{
								m_SmoothingNeeded.Accumulate(new SmoothingNeeded(SmoothingType.SurfaceWetness));
							}
						}
					}
					else
					{
						m_NativeBatchInstances.SetPropertyValue(float2, meshBatch.m_GroupIndex, index11, meshBatch.m_InstanceIndex);
					}
				}
				if (updateMask.ShouldUpdateProperty(ObjectProperty.BaseState, in groupData, out var index12))
				{
					Entity entity2 = preCullingData.m_Entity;
					float value11 = 0f;
					PrefabRef prefabRef5 = m_PrefabRefData[preCullingData.m_Entity];
					if (m_PrefabBuildingExtensionData.TryGetComponent(prefabRef5.m_Prefab, out var componentData17) && !componentData17.m_External && m_OwnerData.TryGetComponent(preCullingData.m_Entity, out var componentData18))
					{
						entity2 = componentData18.m_Owner;
					}
					if (m_ObjectElevationData.TryGetComponent(entity2, out var componentData19) && (componentData19.m_Flags & (ElevationFlags.Stacked | ElevationFlags.OnGround | ElevationFlags.Lowered)) != ElevationFlags.OnGround)
					{
						value11 = 1f;
					}
					m_NativeBatchInstances.SetPropertyValue(value11, meshBatch.m_GroupIndex, index12, meshBatch.m_InstanceIndex);
				}
			}
		}

		private void UpdateColorMask(in GroupData groupData, in MeshBatch meshBatch, UpdateMask updateMask, ObjectProperty property, UnityEngine.Color color, bool smooth)
		{
			if (updateMask.ShouldUpdateProperty(property, in groupData, out var index))
			{
				UpdateColorMask(in meshBatch, color, index, smooth);
			}
		}

		private void UpdateColorMask(in GroupData groupData, in MeshBatch meshBatch, UpdateMask updateMask, LaneProperty property, UnityEngine.Color color, bool smooth)
		{
			if (updateMask.ShouldUpdateProperty(property, in groupData, out var index))
			{
				UpdateColorMask(in meshBatch, color, index, smooth);
			}
		}

		private void UpdateColorMask(in MeshBatch meshBatch, UnityEngine.Color color, int colorMask, bool smooth)
		{
			if (smooth && m_NativeBatchInstances.GetPropertyValue<UnityEngine.Color>(out var value, meshBatch.m_GroupIndex, colorMask, meshBatch.m_InstanceIndex))
			{
				float4 @float = (Vector4)color;
				float4 float2 = (Vector4)value;
				float4 float3 = @float - float2;
				if (math.any(float3 != 0f))
				{
					bool4 test = float3 < 0f;
					float2 += float3 * (m_SmoothnessDelta / math.cmax(math.abs(float3)));
					float2 = math.clamp(float2, math.select(0f, @float, test), math.select(@float, 1f, test));
					value = (Vector4)float2;
					m_NativeBatchInstances.SetPropertyValue(value, meshBatch.m_GroupIndex, colorMask, meshBatch.m_InstanceIndex);
					if (math.any(float2 != @float))
					{
						m_SmoothingNeeded.Accumulate(new SmoothingNeeded(SmoothingType.ColorMask));
					}
				}
			}
			else
			{
				m_NativeBatchInstances.SetPropertyValue(color, meshBatch.m_GroupIndex, colorMask, meshBatch.m_InstanceIndex);
			}
		}

		private void UpdateNetData(PreCullingData preCullingData, UpdateMask updateMask)
		{
			if (updateMask.ShouldUpdateNothing() || !m_MeshBatches.TryGetBuffer(preCullingData.m_Entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				MeshBatch meshBatch = bufferData[i];
				NetSubMesh meshIndex = (NetSubMesh)meshBatch.m_MeshIndex;
				GroupData groupData = m_NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);
				if (updateMask.ShouldUpdateTransform())
				{
					CullingInfo cullingInfo = m_CullingInfoData[preCullingData.m_Entity];
					BatchDataHelpers.CompositionParameters compositionParameters = default(BatchDataHelpers.CompositionParameters);
					switch (meshIndex)
					{
					case NetSubMesh.Edge:
					case NetSubMesh.RotatedEdge:
						BatchDataHelpers.CalculateEdgeParameters(m_EdgeGeometryData[preCullingData.m_Entity], meshIndex == NetSubMesh.RotatedEdge, out compositionParameters);
						break;
					case NetSubMesh.StartNode:
					case NetSubMesh.SubStartNode:
					{
						Composition composition2 = m_CompositionData[preCullingData.m_Entity];
						StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[preCullingData.m_Entity];
						BatchDataHelpers.CalculateNodeParameters(prefabCompositionData: m_PrefabCompositionData[composition2.m_StartNode], nodeGeometry: startNodeGeometry.m_Geometry, compositionParameters: out compositionParameters);
						break;
					}
					case NetSubMesh.EndNode:
					case NetSubMesh.SubEndNode:
					{
						Composition composition = m_CompositionData[preCullingData.m_Entity];
						EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[preCullingData.m_Entity];
						BatchDataHelpers.CalculateNodeParameters(prefabCompositionData: m_PrefabCompositionData[composition.m_EndNode], nodeGeometry: endNodeGeometry.m_Geometry, compositionParameters: out compositionParameters);
						break;
					}
					case NetSubMesh.Orphan1:
					case NetSubMesh.Orphan2:
					{
						Node node = m_NodeData[preCullingData.m_Entity];
						Orphan orphan = m_OrphanData[preCullingData.m_Entity];
						NodeGeometry nodeGeometry = m_NodeGeometryData[preCullingData.m_Entity];
						NetCompositionData prefabCompositionData = m_PrefabCompositionData[orphan.m_Composition];
						BatchDataHelpers.CalculateOrphanParameters(node, nodeGeometry, prefabCompositionData, meshIndex == NetSubMesh.Orphan1, out compositionParameters);
						break;
					}
					}
					ref CullingData reference = ref m_NativeBatchInstances.SetTransformValue(compositionParameters.m_TransformMatrix, compositionParameters.m_TransformMatrix, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
					reference.m_Bounds = cullingInfo.m_Bounds;
					reference.isHidden = m_HiddenData.HasComponent(preCullingData.m_Entity);
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix0, in groupData, out var index))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix0, meshBatch.m_GroupIndex, index, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix1, in groupData, out var index2))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix1, meshBatch.m_GroupIndex, index2, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix2, in groupData, out var index3))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix2, meshBatch.m_GroupIndex, index3, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix3, in groupData, out var index4))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix3, meshBatch.m_GroupIndex, index4, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix4, in groupData, out var index5))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix4, meshBatch.m_GroupIndex, index5, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix5, in groupData, out var index6))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix5, meshBatch.m_GroupIndex, index6, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix6, in groupData, out var index7))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix6, meshBatch.m_GroupIndex, index7, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionMatrix7, in groupData, out var index8))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionMatrix7, meshBatch.m_GroupIndex, index8, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionSync0, in groupData, out var index9))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionSync0, meshBatch.m_GroupIndex, index9, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionSync1, in groupData, out var index10))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionSync1, meshBatch.m_GroupIndex, index10, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionSync2, in groupData, out var index11))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionSync2, meshBatch.m_GroupIndex, index11, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(NetProperty.CompositionSync3, in groupData, out var index12))
					{
						m_NativeBatchInstances.SetPropertyValue(compositionParameters.m_CompositionSync3, meshBatch.m_GroupIndex, index12, meshBatch.m_InstanceIndex);
					}
				}
				if (updateMask.ShouldUpdateProperty(NetProperty.InfoviewColor, in groupData, out var index13))
				{
					float4 value = default(float4);
					if ((preCullingData.m_Flags & PreCullingFlags.InfoviewColor) != 0)
					{
						NodeColor componentData4;
						if (m_NetEdgeColorData.TryGetComponent(preCullingData.m_Entity, out var componentData))
						{
							Edge edge = m_EdgeData[preCullingData.m_Entity];
							float2 @float = new float2((float)(int)componentData.m_Index + 0.5f, (float)(int)componentData.m_Value0 * 0.003921569f);
							float2 float2 = new float2((float)(int)componentData.m_Index + 0.5f, (float)(int)componentData.m_Value1 * 0.003921569f);
							float2 zw = @float;
							float2 zw2 = float2;
							if (m_NetNodeColorData.TryGetComponent(edge.m_Start, out var componentData2))
							{
								zw = new float2((float)(int)componentData2.m_Index + 0.5f, (float)(int)componentData2.m_Value * 0.003921569f);
							}
							if (m_NetNodeColorData.TryGetComponent(edge.m_End, out var componentData3))
							{
								zw2 = new float2((float)(int)componentData3.m_Index + 0.5f, (float)(int)componentData3.m_Value * 0.003921569f);
							}
							switch (meshIndex)
							{
							case NetSubMesh.Edge:
							case NetSubMesh.SubStartNode:
							case NetSubMesh.SubEndNode:
								value = new float4(@float, float2);
								break;
							case NetSubMesh.RotatedEdge:
								value = new float4(float2, @float);
								break;
							case NetSubMesh.StartNode:
								value = new float4(@float, zw);
								break;
							case NetSubMesh.EndNode:
								value = new float4(float2, zw2);
								break;
							}
						}
						else if (m_NetNodeColorData.TryGetComponent(preCullingData.m_Entity, out componentData4))
						{
							value = new float4((float)(int)componentData4.m_Index + 0.5f, (float)(int)componentData4.m_Value * 0.003921569f, (float)(int)componentData4.m_Index + 0.5f, (float)(int)componentData4.m_Value * 0.003921569f);
						}
					}
					m_NativeBatchInstances.SetPropertyValue(value, meshBatch.m_GroupIndex, index13, meshBatch.m_InstanceIndex);
				}
				if (!updateMask.ShouldUpdateProperty(NetProperty.OutlineColors, in groupData, out var index14))
				{
					continue;
				}
				UnityEngine.Color color;
				if (m_ErrorData.HasComponent(preCullingData.m_Entity))
				{
					color = m_RenderingSettingsData.m_ErrorColor;
				}
				else if (m_WarningData.HasComponent(preCullingData.m_Entity))
				{
					color = m_RenderingSettingsData.m_WarningColor;
				}
				else if ((preCullingData.m_Flags & PreCullingFlags.Temp) != 0)
				{
					Temp temp = m_TempData[preCullingData.m_Entity];
					color = m_RenderingSettingsData.m_HoveredColor;
					if ((temp.m_Flags & TempFlags.Parent) != 0)
					{
						color = m_RenderingSettingsData.m_OwnerColor;
					}
					if ((temp.m_Flags & TempFlags.SubDetail) != 0)
					{
						switch (meshIndex)
						{
						case NetSubMesh.StartNode:
						case NetSubMesh.SubStartNode:
						{
							Edge edge3 = m_EdgeData[preCullingData.m_Entity];
							if (m_TempData.TryGetComponent(edge3.m_Start, out var componentData6) && (componentData6.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent))
							{
								color = m_RenderingSettingsData.m_OwnerColor;
							}
							break;
						}
						case NetSubMesh.EndNode:
						case NetSubMesh.SubEndNode:
						{
							Edge edge2 = m_EdgeData[preCullingData.m_Entity];
							if (m_TempData.TryGetComponent(edge2.m_End, out var componentData5) && (componentData5.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent))
							{
								color = m_RenderingSettingsData.m_OwnerColor;
							}
							break;
						}
						}
					}
				}
				else
				{
					color = m_RenderingSettingsData.m_HoveredColor;
				}
				color = color.linear;
				m_NativeBatchInstances.SetPropertyValue(color, meshBatch.m_GroupIndex, index14, meshBatch.m_InstanceIndex);
			}
		}

		private void UpdateLaneData(PreCullingData preCullingData, UpdateMask updateMask)
		{
			if (!updateMask.ShouldUpdateProperty(LaneProperty.ColorMask1) && (preCullingData.m_Flags & PreCullingFlags.ColorsUpdated) != 0)
			{
				updateMask.UpdateProperty(LaneProperty.ColorMask1);
				updateMask.UpdateProperty(LaneProperty.ColorMask2);
				updateMask.UpdateProperty(LaneProperty.ColorMask3);
			}
			if (updateMask.ShouldUpdateNothing() || !m_MeshBatches.TryGetBuffer(preCullingData.m_Entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				MeshBatch meshBatch = bufferData[i];
				GroupData groupData = m_NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);
				if (updateMask.ShouldUpdateTransform())
				{
					CullingInfo cullingInfo = m_CullingInfoData[preCullingData.m_Entity];
					Curve curve = m_CurveData[preCullingData.m_Entity];
					PrefabRef prefabRef = m_PrefabRefData[preCullingData.m_Entity];
					SubMesh subMesh = m_PrefabSubMeshes[prefabRef.m_Prefab][meshBatch.m_MeshIndex];
					MeshData meshData = m_PrefabMeshData[subMesh.m_SubMesh];
					float3 xyz = MathUtils.Size(meshData.m_Bounds);
					float4 size = new float4(xyz, MathUtils.Center(meshData.m_Bounds).y);
					bool flag = (meshData.m_State & MeshFlags.Tiling) != 0;
					int num = 1;
					int clipCount = 0;
					int num2 = meshBatch.m_TileIndex;
					if (m_CutRanges.TryGetBuffer(preCullingData.m_Entity, out var bufferData2))
					{
						float num3 = 0f;
						for (int j = 0; j <= bufferData2.Length; j++)
						{
							float num4;
							float num5;
							if (j < bufferData2.Length)
							{
								CutRange cutRange = bufferData2[j];
								num4 = cutRange.m_CurveDelta.min;
								num5 = cutRange.m_CurveDelta.max;
							}
							else
							{
								num4 = 1f;
								num5 = 1f;
							}
							if (num4 >= num3)
							{
								Curve curve2 = new Curve
								{
									m_Length = curve.m_Length * (num4 - num3)
								};
								if (curve2.m_Length > 0.1f)
								{
									num = BatchDataHelpers.GetTileCount(curve2, size.z, meshData.m_TilingCount, flag, out clipCount);
									if (num > num2)
									{
										curve2.m_Bezier = MathUtils.Cut(curve.m_Bezier, new Bounds1(num3, num4));
										curve = curve2;
										break;
									}
									num2 -= num;
								}
							}
							num3 = num5;
						}
					}
					else if (flag)
					{
						num = BatchDataHelpers.GetTileCount(curve, size.z, meshData.m_TilingCount, geometryTiling: true, out clipCount);
					}
					if ((meshData.m_State & MeshFlags.Invert) != 0)
					{
						curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
					}
					if (num > 1)
					{
						int2 @int = new int2(num2, num2 + 1) * clipCount / num;
						float2 @float = curve.m_Length * (float2)@int / clipCount;
						float2 t = new float2(0f, 1f);
						if (@int.x != 0)
						{
							Bounds1 t2 = new Bounds1(0f, 1f);
							MathUtils.ClampLength(curve.m_Bezier, ref t2, @float.x);
							t.x = t2.max;
						}
						if (@int.y != clipCount)
						{
							Bounds1 t3 = new Bounds1(0f, 1f);
							MathUtils.ClampLength(curve.m_Bezier, ref t3, @float.y);
							t.y = t3.max;
						}
						curve.m_Bezier = MathUtils.Cut(curve.m_Bezier, t);
						curve.m_Length = @float.y - @float.x;
					}
					NodeLane componentData;
					bool flag2 = m_NodeLaneData.TryGetComponent(preCullingData.m_Entity, out componentData);
					float4 float2;
					if (!flag2)
					{
						float2 = ((!m_EdgeLaneData.TryGetComponent(preCullingData.m_Entity, out var componentData2)) ? BatchDataHelpers.BuildCurveScale() : BatchDataHelpers.BuildCurveScale(componentData2));
					}
					else
					{
						NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
						float2 = BatchDataHelpers.BuildCurveScale(componentData, netLaneData);
					}
					bool isDecal = (meshData.m_State & MeshFlags.Decal) != 0;
					float3x4 float3x = TransformHelper.Convert(BatchDataHelpers.BuildTransformMatrix(curve, size, float2, meshData.m_SmoothingDistance, isDecal, isLoaded: true));
					float3x4 secondaryValue = TransformHelper.Convert(BatchDataHelpers.BuildTransformMatrix(curve, size, float2, meshData.m_SmoothingDistance, isDecal, isLoaded: false));
					float4x4 value = BatchDataHelpers.BuildCurveMatrix(curve, float3x, size, meshData.m_TilingCount);
					ref CullingData reference = ref m_NativeBatchInstances.SetTransformValue(float3x, secondaryValue, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
					reference.m_Bounds = cullingInfo.m_Bounds;
					reference.isHidden = m_HiddenData.HasComponent(preCullingData.m_Entity);
					if (updateMask.ShouldUpdateProperty(LaneProperty.CurveMatrix, in groupData, out var index))
					{
						m_NativeBatchInstances.SetPropertyValue(value, meshBatch.m_GroupIndex, index, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(LaneProperty.CurveParams, in groupData, out var index2))
					{
						EdgeLane componentData3;
						Game.Net.Elevation componentData4;
						float4 value2 = (flag2 ? BatchDataHelpers.BuildCurveParams(size, componentData) : (m_EdgeLaneData.TryGetComponent(preCullingData.m_Entity, out componentData3) ? BatchDataHelpers.BuildCurveParams(size, componentData3) : ((!m_NetElevationData.TryGetComponent(preCullingData.m_Entity, out componentData4)) ? BatchDataHelpers.BuildCurveParams(size) : BatchDataHelpers.BuildCurveParams(size, componentData4))));
						m_NativeBatchInstances.SetPropertyValue(value2, meshBatch.m_GroupIndex, index2, meshBatch.m_InstanceIndex);
					}
					if (updateMask.ShouldUpdateProperty(LaneProperty.CurveScale, in groupData, out var index3))
					{
						m_NativeBatchInstances.SetPropertyValue(float2, meshBatch.m_GroupIndex, index3, meshBatch.m_InstanceIndex);
					}
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.InfoviewColor, in groupData, out var index4))
				{
					float4 value3 = default(float4);
					if ((preCullingData.m_Flags & PreCullingFlags.InfoviewColor) != 0)
					{
						Owner componentData6;
						if (m_LaneColorData.TryGetComponent(preCullingData.m_Entity, out var componentData5))
						{
							value3 = new float4((float)(int)componentData5.m_Index + 0.5f, (float)(int)componentData5.m_Value0 * 0.003921569f, (float)(int)componentData5.m_Index + 0.5f, (float)(int)componentData5.m_Value1 * 0.003921569f);
						}
						else if (m_OwnerData.TryGetComponent(preCullingData.m_Entity, out componentData6))
						{
							if (m_EdgeLaneData.HasComponent(preCullingData.m_Entity))
							{
								if (m_NetEdgeColorData.TryGetComponent(componentData6.m_Owner, out var componentData7))
								{
									value3 = new float4((float)(int)componentData7.m_Index + 0.5f, (float)(int)componentData7.m_Value0 * 0.003921569f, (float)(int)componentData7.m_Index + 0.5f, (float)(int)componentData7.m_Value1 * 0.003921569f);
								}
							}
							else if (m_NodeLaneData.HasComponent(preCullingData.m_Entity))
							{
								if (m_NetNodeColorData.TryGetComponent(componentData6.m_Owner, out var componentData8))
								{
									value3 = new float4((float)(int)componentData8.m_Index + 0.5f, (float)(int)componentData8.m_Value * 0.003921569f, (float)(int)componentData8.m_Index + 0.5f, (float)(int)componentData8.m_Value * 0.003921569f);
								}
							}
							else
							{
								while (true)
								{
									if (m_ObjectColorData.TryGetComponent(componentData6.m_Owner, out var componentData9))
									{
										if (componentData9.m_SubColor)
										{
											value3 = new float4((float)(int)componentData9.m_Index + 0.5f, (float)(int)componentData9.m_Value * 0.003921569f, (float)(int)componentData9.m_Index + 0.5f, (float)(int)componentData9.m_Value * 0.003921569f);
										}
										break;
									}
									if (!m_OwnerData.TryGetComponent(componentData6.m_Owner, out var componentData10))
									{
										break;
									}
									componentData6 = componentData10;
								}
							}
						}
					}
					m_NativeBatchInstances.SetPropertyValue(value3, meshBatch.m_GroupIndex, index4, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.ColorMask1))
				{
					UnityEngine.Color color;
					UnityEngine.Color color2;
					UnityEngine.Color color3;
					if (m_MeshColors.TryGetBuffer(preCullingData.m_Entity, out var bufferData3))
					{
						MeshColor meshColor = bufferData3[meshBatch.m_MeshIndex];
						color = meshColor.m_ColorSet.m_Channel0.linear;
						color2 = meshColor.m_ColorSet.m_Channel1.linear;
						color3 = meshColor.m_ColorSet.m_Channel2.linear;
					}
					else
					{
						color = UnityEngine.Color.white;
						color2 = UnityEngine.Color.white;
						color3 = UnityEngine.Color.white;
					}
					bool smooth = (preCullingData.m_Flags & PreCullingFlags.SmoothColor) != 0;
					UpdateColorMask(in groupData, in meshBatch, updateMask, LaneProperty.ColorMask1, color, smooth);
					UpdateColorMask(in groupData, in meshBatch, updateMask, LaneProperty.ColorMask2, color2, smooth);
					UpdateColorMask(in groupData, in meshBatch, updateMask, LaneProperty.ColorMask3, color3, smooth);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.FlowMatrix, in groupData, out var index5))
				{
					float4x4 value4 = default(float4x4);
					if ((preCullingData.m_Flags & PreCullingFlags.InfoviewColor) != 0 && m_SubFlows.TryGetBuffer(preCullingData.m_Entity, out var bufferData4) && bufferData4.Length == 16)
					{
						value4.c0 = new float4(bufferData4[0].m_Value, bufferData4[4].m_Value, bufferData4[8].m_Value, bufferData4[12].m_Value) * (1f / 127f);
						value4.c1 = new float4(bufferData4[1].m_Value, bufferData4[5].m_Value, bufferData4[9].m_Value, bufferData4[13].m_Value) * (1f / 127f);
						value4.c2 = new float4(bufferData4[2].m_Value, bufferData4[6].m_Value, bufferData4[10].m_Value, bufferData4[14].m_Value) * (1f / 127f);
						value4.c3 = new float4(bufferData4[3].m_Value, bufferData4[7].m_Value, bufferData4[11].m_Value, bufferData4[15].m_Value) * (1f / 127f);
					}
					m_NativeBatchInstances.SetPropertyValue(value4, meshBatch.m_GroupIndex, index5, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.FlowOffset, in groupData, out var index6))
				{
					float value5 = 0f;
					if ((preCullingData.m_Flags & PreCullingFlags.InfoviewColor) != 0 && m_OwnerData.TryGetComponent(preCullingData.m_Entity, out var componentData11) && m_PseudoRandomSeedData.TryGetComponent(componentData11.m_Owner, out var componentData12))
					{
						value5 = componentData12.GetRandom(PseudoRandomSeed.kFlowOffset).NextFloat(1f);
					}
					m_NativeBatchInstances.SetPropertyValue(value5, meshBatch.m_GroupIndex, index6, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.CurveDeterioration, in groupData, out var index7))
				{
					float4 value6 = 0f;
					if ((preCullingData.m_Flags & PreCullingFlags.LaneCondition) != 0 && m_LaneConditionData.TryGetComponent(preCullingData.m_Entity, out var componentData13))
					{
						float num6 = componentData13.m_Wear / 10f;
						value6 = new float4(num6, num6, num6, 0f);
					}
					m_NativeBatchInstances.SetPropertyValue(value6, meshBatch.m_GroupIndex, index7, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.OutlineColors, in groupData, out var index8))
				{
					UnityEngine.Color linear = (m_ErrorData.HasComponent(preCullingData.m_Entity) ? m_RenderingSettingsData.m_ErrorColor : (m_WarningData.HasComponent(preCullingData.m_Entity) ? m_RenderingSettingsData.m_WarningColor : (((preCullingData.m_Flags & PreCullingFlags.Temp) == 0) ? m_RenderingSettingsData.m_HoveredColor : (((m_TempData[preCullingData.m_Entity].m_Flags & TempFlags.Parent) == 0) ? m_RenderingSettingsData.m_HoveredColor : m_RenderingSettingsData.m_OwnerColor)))).linear;
					m_NativeBatchInstances.SetPropertyValue(linear, meshBatch.m_GroupIndex, index8, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(LaneProperty.HangingDistances, in groupData, out var index9))
				{
					float4 value7 = 0f;
					if (m_PrefabRefData.TryGetComponent(preCullingData.m_Entity, out var componentData14) && m_CurveData.TryGetComponent(preCullingData.m_Entity, out var componentData15) && m_PrefabUtilityLaneData.TryGetComponent(componentData14.m_Prefab, out var componentData16))
					{
						m_HangingLaneData.TryGetComponent(preCullingData.m_Entity, out var componentData17);
						value7.xw = componentData17.m_Distances.xy;
						value7.yz = (componentData17.m_Distances.xy + componentData16.m_Hanging * componentData15.m_Length) * (2f / 3f);
						float4 x = new float4(math.lengthsq(Wind.SampleWind(m_WindData, componentData15.m_Bezier.a)), math.lengthsq(Wind.SampleWind(m_WindData, componentData15.m_Bezier.b)), math.lengthsq(Wind.SampleWind(m_WindData, componentData15.m_Bezier.c)), math.lengthsq(Wind.SampleWind(m_WindData, componentData15.m_Bezier.d)));
						value7 *= math.sqrt(x);
					}
					m_NativeBatchInstances.SetPropertyValue(value7, meshBatch.m_GroupIndex, index9, meshBatch.m_InstanceIndex);
				}
			}
		}

		private unsafe void UpdateZoneData(PreCullingData preCullingData, UpdateMask updateMask)
		{
			if (updateMask.ShouldUpdateNothing() || !m_MeshBatches.TryGetBuffer(preCullingData.m_Entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				MeshBatch meshBatch = bufferData[i];
				GroupData groupData = m_NativeBatchGroups.GetGroupData(meshBatch.m_GroupIndex);
				if (updateMask.ShouldUpdateTransform())
				{
					CullingInfo cullingInfo = m_CullingInfoData[preCullingData.m_Entity];
					Block block = m_ZoneBlockData[preCullingData.m_Entity];
					float3 position = block.m_Position;
					float2 @float = (float2)(block.m_Size - new int2(10, 6)) * 4f;
					position.xz += block.m_Direction * @float.y;
					position.xz += MathUtils.Right(block.m_Direction) * @float.x;
					float3x4 float3x = TransformHelper.TRS(position, ZoneUtils.GetRotation(block), new float3(1f, 1f, 1f));
					ref CullingData reference = ref m_NativeBatchInstances.SetTransformValue(float3x, float3x, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex);
					reference.m_Bounds = cullingInfo.m_Bounds;
					reference.isHidden = m_HiddenData.HasComponent(preCullingData.m_Entity);
				}
				if (!updateMask.ShouldUpdateProperty(ZoneProperty.CellType0))
				{
					continue;
				}
				Block block2 = m_ZoneBlockData[preCullingData.m_Entity];
				DynamicBuffer<Cell> dynamicBuffer = m_ZoneCells[preCullingData.m_Entity];
				CellTypes cellTypes = default(CellTypes);
				void* destination = &cellTypes;
				for (int j = 0; j < block2.m_Size.y; j++)
				{
					for (int k = 0; k < block2.m_Size.x; k++)
					{
						Cell cell = dynamicBuffer[j * block2.m_Size.x + k];
						int colorIndex = ZoneUtils.GetColorIndex(cell.m_State, cell.m_Zone);
						int index = j + k * 6;
						UnsafeUtility.WriteArrayElement(destination, index, (float)colorIndex);
					}
				}
				if (updateMask.ShouldUpdateProperty(ZoneProperty.CellType0, in groupData, out var index2))
				{
					m_NativeBatchInstances.SetPropertyValue(cellTypes.m_CellTypes0, meshBatch.m_GroupIndex, index2, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ZoneProperty.CellType1, in groupData, out var index3))
				{
					m_NativeBatchInstances.SetPropertyValue(cellTypes.m_CellTypes1, meshBatch.m_GroupIndex, index3, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ZoneProperty.CellType2, in groupData, out var index4))
				{
					m_NativeBatchInstances.SetPropertyValue(cellTypes.m_CellTypes2, meshBatch.m_GroupIndex, index4, meshBatch.m_InstanceIndex);
				}
				if (updateMask.ShouldUpdateProperty(ZoneProperty.CellType3, in groupData, out var index5))
				{
					m_NativeBatchInstances.SetPropertyValue(cellTypes.m_CellTypes3, meshBatch.m_GroupIndex, index5, meshBatch.m_InstanceIndex);
				}
			}
		}

		private void UpdateFadeData(PreCullingData preCullingData)
		{
			DynamicBuffer<MeshBatch> dynamicBuffer = m_MeshBatches[preCullingData.m_Entity];
			DynamicBuffer<FadeBatch> dynamicBuffer2 = m_FadeBatches[preCullingData.m_Entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				MeshBatch meshBatch = dynamicBuffer[i];
				FadeBatch fadeBatch = dynamicBuffer2[i];
				if (!fadeBatch.m_Velocity.Equals(default(float3)) && m_NativeBatchInstances.GetTransformValue(meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex, out var value, out var secondaryValue))
				{
					float3 @float = fadeBatch.m_Velocity * (m_FrameDelta * (1f / 60f));
					value.c3 += @float;
					secondaryValue.c3 += @float;
					m_NativeBatchInstances.SetTransformValue(value, secondaryValue, meshBatch.m_GroupIndex, meshBatch.m_InstanceIndex).m_Bounds += @float;
				}
			}
		}
	}

	[BurstCompile]
	private struct BatchLodJob : IJobParallelFor
	{
		private struct LodData
		{
			public int2 m_MinLod;

			public int m_MaxPriority;

			public int m_SelectLod;
		}

		[ReadOnly]
		public bool m_DisableLods;

		[ReadOnly]
		public bool m_UseLodFade;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float m_PixelSizeFactor;

		[ReadOnly]
		public int m_LodFadeDelta;

		[ReadOnly]
		public NativeList<MeshLoadingState> m_LoadingState;

		[ReadOnly]
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelCullingWriter m_NativeBatchInstances;

		[NativeDisableParallelForRestriction]
		public NativeList<int> m_BatchPriority;

		[NativeDisableParallelForRestriction]
		public NativeList<float> m_VTRequestsMaxPixels0;

		[NativeDisableParallelForRestriction]
		public NativeList<float> m_VTRequestsMaxPixels1;

		public void Execute(int index)
		{
			int groupIndex = m_NativeBatchInstances.GetGroupIndex(index);
			GroupData groupData = m_NativeBatchGroups.GetGroupData(groupIndex);
			if ((groupData.m_LodCount == 0) | m_DisableLods)
			{
				SingleLod(index, groupIndex, in groupData);
			}
			else
			{
				MultiLod(index, groupIndex, in groupData);
			}
		}

		private bool GetLodPropertyIndex(in GroupData groupData, int dataIndex, out int index)
		{
			switch (groupData.m_MeshType)
			{
			case MeshType.Object:
				return groupData.GetPropertyIndex(8 + dataIndex, out index);
			case MeshType.Lane:
				return groupData.GetPropertyIndex(6 + dataIndex, out index);
			case MeshType.Net:
				return groupData.GetPropertyIndex(14 + dataIndex, out index);
			default:
				index = -1;
				return false;
			}
		}

		private unsafe void SingleLod(int activeGroup, int groupIndex, in GroupData groupData)
		{
			NativeBatchAccessor<BatchData> batchAccessor = m_NativeBatchGroups.GetBatchAccessor(groupIndex);
			WriteableCullingAccessor<CullingData> cullingAccessor = m_NativeBatchInstances.GetCullingAccessor(activeGroup);
			MergeIndexAccessor mergeIndexAccessor = m_NativeBatchInstances.GetMergeIndexAccessor(groupIndex);
			WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData> writeablePropertyAccessor = default(WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData>);
			WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData> writeablePropertyAccessor2 = default(WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData>);
			bool flag = false;
			if (m_UseLodFade)
			{
				if (GetLodPropertyIndex(in groupData, 0, out var index))
				{
					writeablePropertyAccessor = m_NativeBatchInstances.GetPropertyAccessor(activeGroup, index);
					flag = true;
				}
				if (GetLodPropertyIndex(in groupData, 1, out var index2))
				{
					writeablePropertyAccessor2 = m_NativeBatchInstances.GetPropertyAccessor(activeGroup, index2);
					flag = true;
				}
			}
			int length = batchAccessor.Length;
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
			bool useSecondaryMatrix = false;
			BatchData batchData = batchAccessor.GetBatchData(0);
			int2 @int = new int2(batchData.m_MinLod, batchData.m_ShadowLod);
			int num = -1000000;
			int num2 = 0;
			int num3 = 0;
			int num4 = @int.x;
			bool flag2 = (batchData.m_VTIndex0 >= 0) | (batchData.m_VTIndex1 >= 0);
			for (int i = 1; i < length; i++)
			{
				batchData = batchAccessor.GetBatchData(i);
				flag2 |= (batchData.m_VTIndex0 >= 0) | (batchData.m_VTIndex1 >= 0);
				if (batchData.m_MinLod != num4)
				{
					num4 = batchData.m_MinLod;
					num3 = i;
					num2++;
				}
			}
			int num5 = 1;
			if (flag2)
			{
				num5 += m_NativeBatchGroups.GetMergedGroupCount(groupIndex);
			}
			float* ptr = stackalloc float[num5];
			for (int j = 0; j < num5; j++)
			{
				ptr[j] = float.MaxValue;
			}
			int managedBatchIndex = batchAccessor.GetManagedBatchIndex(num3);
			if (managedBatchIndex >= 0)
			{
				useSecondaryMatrix = m_LoadingState[managedBatchIndex] < MeshLoadingState.Complete;
			}
			for (int k = 0; k < cullingAccessor.Length; k++)
			{
				ref CullingData reference = ref cullingAccessor.Get(k);
				float num6 = RenderingUtils.CalculateMinDistance(reference.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num7 = RenderingUtils.CalculateLod(num6 * num6, m_LodParameters);
				int2 int2 = @int + reference.lodOffset;
				bool flag3 = !reference.isHidden;
				bool2 test = (num7 >= int2) & !reference.isFading;
				int4 int3 = new int4(num2, num2, math.select(new int2(0), new int2(255), test));
				if (flag)
				{
					int4 lodFade = reference.lodFade;
					if (math.any(lodFade != int3))
					{
						lodFade.xy = num2;
						lodFade.zw = math.clamp(lodFade.zw + math.select(-m_LodFadeDelta, m_LodFadeDelta, test), 0, 255);
						test = lodFade.zw != 0;
						if (test.x)
						{
							int2 int4 = math.select(lodFade.zw, lodFade.zw - 255, reference.isFading);
							int4 int5 = math.select(new int4(int4, -int4), new int4(-int4, int4), (((groupData.m_LodCount - lodFade.xy) & 1) != 0).xyxy);
							num = math.max(num, 0);
							int5.xz = (1065353471 - int5.xz) | (255 - int5.yw << 11);
							if (writeablePropertyAccessor.Length != 0)
							{
								writeablePropertyAccessor.SetPropertyValue(int5.x, k);
							}
							if (writeablePropertyAccessor2.Length != 0)
							{
								writeablePropertyAccessor2.SetPropertyValue(int5.z, k);
							}
						}
						reference.lodFade = lodFade;
					}
				}
				else
				{
					reference.lodFade = int3;
				}
				int4 lodRange = 0;
				test &= flag3;
				if (test.x)
				{
					lodRange.xy = new int2(num2, num2 + 1);
					bounds |= reference.m_Bounds;
					if (test.y)
					{
						lodRange.zw = new int2(num2, num2 + 1);
						bounds2 |= reference.m_Bounds;
					}
					if (flag2)
					{
						int num8 = ((num5 != 1) ? (1 + mergeIndexAccessor.Get(k)) : 0);
						ptr[num8] = math.min(ptr[num8], num6);
					}
				}
				reference.lodRange = lodRange;
				num = math.max(num, num7 - int2.x);
			}
			for (int l = num3; l < length; l++)
			{
				if (flag2)
				{
					batchData = batchAccessor.GetBatchData(l);
					if ((batchData.m_VTIndex0 >= 0) | (batchData.m_VTIndex1 >= 0))
					{
						if (*ptr != float.MaxValue)
						{
							AddVTRequests(in batchData, *ptr);
						}
						for (int m = 1; m < num5; m++)
						{
							if (ptr[m] != float.MaxValue)
							{
								int mergedGroupIndex = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, m - 1);
								AddVTRequests(m_NativeBatchGroups.GetBatchData(mergedGroupIndex, l), ptr[m]);
							}
						}
					}
				}
				managedBatchIndex = batchAccessor.GetManagedBatchIndex(l);
				if (managedBatchIndex >= 0)
				{
					ref int reference2 = ref m_BatchPriority.ElementAt(managedBatchIndex);
					reference2 = math.max(reference2, num);
				}
			}
			float3 boundsCenter;
			float3 boundsExtents;
			if (bounds.min.x != float.MaxValue)
			{
				boundsCenter = MathUtils.Center(bounds);
				boundsExtents = MathUtils.Extents(bounds);
			}
			else
			{
				boundsCenter = default(float3);
				boundsExtents = float.MinValue;
			}
			float3 shadowBoundsCenter;
			float3 shadowBoundsExtents;
			if (bounds2.min.x != float.MaxValue)
			{
				shadowBoundsCenter = MathUtils.Center(bounds2);
				shadowBoundsExtents = MathUtils.Extents(bounds2);
			}
			else
			{
				shadowBoundsCenter = default(float3);
				shadowBoundsExtents = float.MinValue;
			}
			m_NativeBatchInstances.UpdateCulling(activeGroup, boundsCenter, boundsExtents, shadowBoundsCenter, shadowBoundsExtents, useSecondaryMatrix);
		}

		private unsafe void MultiLod(int activeGroup, int groupIndex, in GroupData groupData)
		{
			NativeBatchAccessor<BatchData> batchAccessor = m_NativeBatchGroups.GetBatchAccessor(groupIndex);
			WriteableCullingAccessor<CullingData> cullingAccessor = m_NativeBatchInstances.GetCullingAccessor(activeGroup);
			MergeIndexAccessor mergeIndexAccessor = m_NativeBatchInstances.GetMergeIndexAccessor(groupIndex);
			WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData> writeablePropertyAccessor = default(WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData>);
			WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData> writeablePropertyAccessor2 = default(WriteablePropertyAccessor<CullingData, GroupData, BatchData, InstanceData>);
			bool flag = false;
			if (m_UseLodFade)
			{
				if (GetLodPropertyIndex(in groupData, 0, out var index))
				{
					writeablePropertyAccessor = m_NativeBatchInstances.GetPropertyAccessor(activeGroup, index);
					flag = true;
				}
				if (GetLodPropertyIndex(in groupData, 1, out var index2))
				{
					writeablePropertyAccessor2 = m_NativeBatchInstances.GetPropertyAccessor(activeGroup, index2);
					flag = true;
				}
			}
			int length = batchAccessor.Length;
			int num = 0;
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
			bool useSecondaryMatrix = false;
			bool flag2 = false;
			LodData* ptr = stackalloc LodData[groupData.m_LodCount + 1];
			ref LodData reference = ref *ptr;
			reference.m_MinLod = -1;
			int managedBatchIndex;
			for (int i = 0; i < length; i++)
			{
				BatchData batchData = batchAccessor.GetBatchData(i);
				flag2 |= (batchData.m_VTIndex0 >= 0) | (batchData.m_VTIndex1 >= 0);
				if (batchData.m_MinLod != reference.m_MinLod.x)
				{
					reference = ref ptr[num++];
					reference.m_MinLod = new int2(batchData.m_MinLod, batchData.m_ShadowLod);
					reference.m_MaxPriority = -1000000;
					reference.m_SelectLod = num - 1;
				}
				if (num != 1)
				{
					managedBatchIndex = batchAccessor.GetManagedBatchIndex(i);
					int num2 = ptr[num - 2].m_SelectLod;
					reference.m_SelectLod = math.select(num - 1, num2, reference.m_SelectLod == num2 || (managedBatchIndex >= 0 && m_LoadingState[managedBatchIndex] < MeshLoadingState.Complete));
				}
			}
			int num3 = 1;
			int num4 = 1;
			if (flag2)
			{
				num3 += m_NativeBatchGroups.GetMergedGroupCount(groupIndex);
				num4 = num3 * num;
			}
			float* ptr2 = stackalloc float[num4];
			for (int j = 0; j < num4; j++)
			{
				ptr2[j] = float.MaxValue;
			}
			managedBatchIndex = batchAccessor.GetManagedBatchIndex(0);
			if (managedBatchIndex >= 0)
			{
				useSecondaryMatrix = m_LoadingState[managedBatchIndex] < MeshLoadingState.Complete;
			}
			for (int k = 0; k < cullingAccessor.Length; k++)
			{
				ref CullingData reference2 = ref cullingAccessor.Get(k);
				reference = ref *ptr;
				float num5 = RenderingUtils.CalculateMinDistance(reference2.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num6 = RenderingUtils.CalculateLod(num5 * num5, m_LodParameters);
				bool flag3 = !reference2.isHidden;
				bool2 test = num6 >= reference.m_MinLod;
				int2 @int = math.select(-1, reference.m_SelectLod, test);
				int2 int2 = 1;
				reference.m_MaxPriority = math.max(reference.m_MaxPriority, num6 - reference.m_MinLod.x);
				test &= !reference2.isFading;
				for (int l = 1; l < num; l++)
				{
					reference = ref ptr[l];
					@int = math.select(@int, reference.m_SelectLod, num6 >= reference.m_MinLod);
					reference.m_MaxPriority = math.max(reference.m_MaxPriority, num6 - reference.m_MinLod.x);
				}
				if (flag)
				{
					int4 int3 = new int4(@int, math.select(new int2(0), new int2(255), test));
					int4 int4 = reference2.lodFade;
					if (math.any(int4 != int3))
					{
						if (reference2.isFading)
						{
							int4.zw -= m_LodFadeDelta;
							int3.xy = math.select(0, @int, @int >= 0);
							int4 = math.select(int4, int3, ((int4.xy == 255) | (int4.zw <= 0)).xyxy);
							test = int4.zw != 0;
							if (test.x)
							{
								reference = ref ptr[int4.x];
								reference.m_MaxPriority = math.max(reference.m_MaxPriority, 0);
								@int.x = reference.m_SelectLod;
								int2.x = 1;
								if (test.y)
								{
									@int.y = ptr[int4.y].m_SelectLod;
									int2.y = 1;
								}
								int2 int5 = int4.zw - 255;
								int4 int6 = math.select(new int4(int5, -int5), new int4(-int5, int5), (((groupData.m_LodCount - int4.xy) & 1) != 0).xyxy);
								int6.xz = (1065353471 - int6.xz) | (255 - int6.yw << 11);
								if (writeablePropertyAccessor.Length != 0)
								{
									writeablePropertyAccessor.SetPropertyValue(int6.x, k);
								}
								if (writeablePropertyAccessor2.Length != 0)
								{
									writeablePropertyAccessor2.SetPropertyValue(int6.z, k);
								}
							}
						}
						else
						{
							if (@int.x >= int4.x)
							{
								int4.z += m_LodFadeDelta;
								int4.xz += math.select(0, new int2(1, -255), int4.z >= 256);
								int4.xz = math.select(int4.xz, new int2(@int.x, 255), int4.x > @int.x);
							}
							else
							{
								int4.z -= m_LodFadeDelta;
								int4.xz += math.select(0, new int2(-1, 255), int4.z <= 0);
								int3.xz = math.select(0, new int2(@int.x, 255), test.x);
								int4.xz = math.select(int4.xz, int3.xz, (int4.x == @int.x) | (int4.x >= 254));
							}
							if (@int.y >= int4.y)
							{
								int4.w += m_LodFadeDelta;
								int4.yw += math.select(0, new int2(1, -255), int4.w >= 256);
								int4.yw = math.select(int4.yw, new int2(@int.y, 255), int4.y > @int.y);
							}
							else
							{
								int4.w -= m_LodFadeDelta;
								int4.yw += math.select(0, new int2(-1, 255), int4.w <= 0);
								int3.yw = math.select(0, new int2(@int.y, 255), test.y);
								int4.yw = math.select(int4.yw, int3.yw, (int4.y == @int.y) | (int4.y >= 254));
							}
							test = int4.zw != 0;
							if (test.x)
							{
								int num7 = int4.x - math.select(0, 1, (int4.z != 255) & (int4.x != 0));
								reference = ref ptr[num7];
								reference.m_MaxPriority = math.max(reference.m_MaxPriority, 0);
								@int.x = reference.m_SelectLod;
								reference = ref ptr[int4.x];
								reference.m_MaxPriority = math.max(reference.m_MaxPriority, 0);
								int2.x = reference.m_SelectLod - @int.x + 1;
								if (test.y)
								{
									num7 = int4.y - math.select(0, 1, (int4.w != 255) & (int4.y != 0));
									@int.y = ptr[num7].m_SelectLod;
									int2.y = ptr[int4.y].m_SelectLod - @int.y + 1;
								}
								int2 zw = int4.zw;
								int4 int7 = math.select(new int4(zw, -zw), new int4(-zw, zw), (((groupData.m_LodCount - int4.xy) & 1) != 0).xyxy);
								int7.xz = (1065353471 - int7.xz) | (255 - int7.yw << 11);
								if (writeablePropertyAccessor.Length != 0)
								{
									writeablePropertyAccessor.SetPropertyValue(int7.x, k);
								}
								if (writeablePropertyAccessor2.Length != 0)
								{
									writeablePropertyAccessor2.SetPropertyValue(int7.z, k);
								}
							}
						}
						reference2.lodFade = int4;
					}
				}
				else
				{
					reference2.lodFade = math.select(0, new int4(@int, 255, 255), test.xyxy);
				}
				int4 lodRange = 0;
				test &= flag3;
				if (test.x)
				{
					lodRange.xy = new int2(@int.x, @int.x + int2.x);
					bounds |= reference2.m_Bounds;
					if (test.y)
					{
						lodRange.zw = new int2(@int.y, @int.y + int2.y);
						bounds2 |= reference2.m_Bounds;
					}
					if (flag2)
					{
						int num8 = ((num3 != 1) ? (1 + mergeIndexAccessor.Get(k)) : 0);
						int num9 = @int.x * num3 + num8;
						ptr2[num9] = math.min(ptr2[num9], num5);
					}
				}
				reference2.lodRange = lodRange;
			}
			num = 1;
			reference = ref *ptr;
			int num10 = 0;
			for (int m = 0; m < length; m++)
			{
				BatchData batchData2 = batchAccessor.GetBatchData(m);
				if (batchData2.m_MinLod != reference.m_MinLod.x)
				{
					reference = ref ptr[num++];
					num10 += num3;
				}
				if ((batchData2.m_VTIndex0 >= 0) | (batchData2.m_VTIndex1 >= 0))
				{
					if (ptr2[num10] != float.MaxValue)
					{
						AddVTRequests(in batchData2, ptr2[num10]);
					}
					for (int n = 1; n < num3; n++)
					{
						if (ptr2[num10 + n] != float.MaxValue)
						{
							int mergedGroupIndex = m_NativeBatchGroups.GetMergedGroupIndex(groupIndex, n - 1);
							AddVTRequests(m_NativeBatchGroups.GetBatchData(mergedGroupIndex, m), ptr2[num10 + n]);
						}
					}
				}
				managedBatchIndex = batchAccessor.GetManagedBatchIndex(m);
				if (managedBatchIndex >= 0)
				{
					ref int reference3 = ref m_BatchPriority.ElementAt(managedBatchIndex);
					reference3 = math.max(reference3, reference.m_MaxPriority);
				}
			}
			float3 boundsCenter;
			float3 boundsExtents;
			if (bounds.min.x != float.MaxValue)
			{
				boundsCenter = MathUtils.Center(bounds);
				boundsExtents = MathUtils.Extents(bounds);
			}
			else
			{
				boundsCenter = default(float3);
				boundsExtents = float.MinValue;
			}
			float3 shadowBoundsCenter;
			float3 shadowBoundsExtents;
			if (bounds2.min.x != float.MaxValue)
			{
				shadowBoundsCenter = MathUtils.Center(bounds2);
				shadowBoundsExtents = MathUtils.Extents(bounds2);
			}
			else
			{
				shadowBoundsCenter = default(float3);
				shadowBoundsExtents = float.MinValue;
			}
			m_NativeBatchInstances.UpdateCulling(activeGroup, boundsCenter, boundsExtents, shadowBoundsCenter, shadowBoundsExtents, useSecondaryMatrix);
		}

		private void AddVTRequests(in BatchData batchData, float minDistance)
		{
			float num = math.atan(batchData.m_VTSizeFactor / minDistance) * m_PixelSizeFactor;
			if (batchData.m_VTIndex0 >= 0)
			{
				ref float reference = ref m_VTRequestsMaxPixels0.ElementAt(batchData.m_VTIndex0);
				if (num > reference)
				{
					float num2 = num;
					float num3 = num;
					do
					{
						num3 = num2;
						num2 = Interlocked.Exchange(ref reference, num3);
					}
					while (num2 > num3);
				}
			}
			if (batchData.m_VTIndex1 < 0)
			{
				return;
			}
			ref float reference2 = ref m_VTRequestsMaxPixels1.ElementAt(batchData.m_VTIndex1);
			if (num > reference2)
			{
				float num4 = num;
				float num5 = num;
				do
				{
					num5 = num4;
					num4 = Interlocked.Exchange(ref reference2, num5);
				}
				while (num4 > num5);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Error> __Game_Tools_Error_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Warning> __Game_Tools_Warning_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Override> __Game_Tools_Override_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<FadeBatch> __Game_Rendering_FadeBatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Animated> __Game_Rendering_Animated_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Emissive> __Game_Rendering_Emissive_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Surface> __Game_Objects_Surface_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CitizenPresence> __Game_Buildings_CitizenPresence_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneColor> __Game_Net_LaneColor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HangingLane> __Game_Net_HangingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeColor> __Game_Net_EdgeColor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeColor> __Game_Net_NodeColor_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubFlow> __Game_Net_SubFlow_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CutRange> __Game_Net_CutRange_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> __Game_Prefabs_GrowthScaleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentLookup = state.GetComponentLookup<Error>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentLookup = state.GetComponentLookup<Warning>(isReadOnly: true);
			__Game_Tools_Override_RO_ComponentLookup = state.GetComponentLookup<Override>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Rendering_MeshBatch_RO_BufferLookup = state.GetBufferLookup<MeshBatch>(isReadOnly: true);
			__Game_Rendering_FadeBatch_RO_BufferLookup = state.GetBufferLookup<FadeBatch>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Rendering_Animated_RO_BufferLookup = state.GetBufferLookup<Animated>(isReadOnly: true);
			__Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
			__Game_Rendering_Emissive_RO_BufferLookup = state.GetBufferLookup<Emissive>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Surface_RO_ComponentLookup = state.GetComponentLookup<Surface>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_CitizenPresence_RO_ComponentLookup = state.GetComponentLookup<CitizenPresence>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentLookup = state.GetComponentLookup<NodeLane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_LaneColor_RO_ComponentLookup = state.GetComponentLookup<LaneColor>(isReadOnly: true);
			__Game_Net_LaneCondition_RO_ComponentLookup = state.GetComponentLookup<LaneCondition>(isReadOnly: true);
			__Game_Net_HangingLane_RO_ComponentLookup = state.GetComponentLookup<HangingLane>(isReadOnly: true);
			__Game_Net_EdgeColor_RO_ComponentLookup = state.GetComponentLookup<EdgeColor>(isReadOnly: true);
			__Game_Net_NodeColor_RO_ComponentLookup = state.GetComponentLookup<NodeColor>(isReadOnly: true);
			__Game_Net_SubFlow_RO_BufferLookup = state.GetBufferLookup<SubFlow>(isReadOnly: true);
			__Game_Net_CutRange_RO_BufferLookup = state.GetBufferLookup<CutRange>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_GrowthScaleData_RO_ComponentLookup = state.GetComponentLookup<GrowthScaleData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
		}
	}

	public const float LOD_FADE_DURATION = 0.25f;

	public const float DEBUG_FADE_DURATION = 2.5f;

	private RenderingSystem m_RenderingSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private BatchMeshSystem m_BatchMeshSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private PreCullingSystem m_PreCullingSystem;

	private LightingSystem m_LightingSystem;

	private MeshColorSystem m_MeshColorSystem;

	private SimulationSystem m_SimulationSystem;

	private CitizenPresenceSystem m_CitizenPresenceSystem;

	private TreeGrowthSystem m_TreeGrowthSystem;

	private WetnessSystem m_WetnessSystem;

	private WindSystem m_WindSystem;

	private DirtynessSystem m_DirtynessSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_RenderingSettingsQuery;

	private NativeAccumulator<SmoothingNeeded> m_SmoothingNeeded;

	private int m_SHCoefficients;

	private int m_LodParameters;

	private bool m_UpdateAll;

	private float m_LastLightFactor;

	private float m_LodFadeTimer;

	private float4 m_LastBuildingStateOverride;

	private uint m_LastCitizenPresenceVersion;

	private uint m_LastTreeGrowthVersion;

	private uint m_LastWetnessVersion;

	private uint m_LastDirtynessVersion;

	private uint m_LastFireDamageVersion;

	private uint m_LastWaterDamageVersion;

	private uint m_LastWeatherDamageVersion;

	private uint m_LastLaneConditionFrame;

	private uint m_LastDamagedFrame;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_MeshColorSystem = base.World.GetOrCreateSystemManaged<MeshColorSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitizenPresenceSystem = base.World.GetOrCreateSystemManaged<CitizenPresenceSystem>();
		m_TreeGrowthSystem = base.World.GetOrCreateSystemManaged<TreeGrowthSystem>();
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_WetnessSystem = base.World.GetOrCreateSystemManaged<WetnessSystem>();
		m_DirtynessSystem = base.World.GetOrCreateSystemManaged<DirtynessSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_RenderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<RenderingSettingsData>());
		m_SmoothingNeeded = new NativeAccumulator<SmoothingNeeded>(Allocator.Persistent);
		m_SHCoefficients = Shader.PropertyToID("unity_SHCoefficients");
		m_LodParameters = Shader.PropertyToID("colossal_LodParameters");
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SmoothingNeeded.Dispose();
		base.OnDestroy();
	}

	public void InstancePropertiesUpdated()
	{
		m_UpdateAll = true;
	}

	public void PostDeserialize(Context context)
	{
		m_LastLaneConditionFrame = m_SimulationSystem.frameIndex;
		m_LastDamagedFrame = m_SimulationSystem.frameIndex;
	}

	public float GetLevelOfDetail(float levelOfDetail, IGameCameraController cameraController)
	{
		if (cameraController != null)
		{
			levelOfDetail *= 1f - 1f / (2f + 0.01f * cameraController.zoom);
		}
		return levelOfDetail;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float4 lodParameters = 1f;
		float3 cameraPosition = 0f;
		float3 cameraDirection = 0f;
		float pixelSizeFactor = 1f;
		if (m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters2))
		{
			IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
			lodParameters = RenderingUtils.CalculateLodParameters(GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), lodParameters2);
			cameraPosition = lodParameters2.cameraPosition;
			cameraDirection = m_CameraUpdateSystem.activeViewer.forward;
			pixelSizeFactor = (float)lodParameters2.cameraPixelHeight / math.radians(lodParameters2.fieldOfView);
		}
		Shader.SetGlobalVector(m_LodParameters, new Vector4(lodParameters.x, lodParameters.y, 0f, 0f));
		bool flag = m_BatchManagerSystem.IsLodFadeEnabled();
		int lodFadeDelta = 0;
		if (flag)
		{
			m_LodFadeTimer += UnityEngine.Time.deltaTime * (m_RenderingSystem.debugCrossFade ? 102f : 1020f);
			lodFadeDelta = Mathf.FloorToInt(m_LodFadeTimer);
			m_LodFadeTimer -= lodFadeDelta;
			lodFadeDelta = math.clamp(lodFadeDelta, 0, 255);
		}
		m_BatchMeshSystem.UpdateMeshes();
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies2);
		GetDataQuery(out var cullingFlags, out var updateMasks);
		dependencies2.Complete();
		UpdateGlobalValues(nativeBatchInstances);
		int activeGroupCount = nativeBatchInstances.GetActiveGroupCount();
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.CullingWriter cullingWriter = nativeBatchInstances.BeginCulling(Allocator.TempJob);
		JobHandle dependencies3;
		JobHandle dependencies4;
		BatchDataJob jobData = new BatchDataJob
		{
			m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ErrorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Error_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WarningData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OverrideData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Override_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_FadeBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_FadeBatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_Animateds = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Animated_RO_BufferLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
			m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RO_BufferLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectSurfaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Surface_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectDamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenPresenceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CitizenPresence_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingAbandonedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HangingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_HangingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetEdgeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetNodeColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubFlows = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubFlow_RO_BufferLookup, ref base.CheckedStateRef),
			m_CutRanges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_CutRange_RO_BufferLookup, ref base.CheckedStateRef),
			m_ZoneBlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneCells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGrowthScaleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GrowthScaleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabAnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_LightFactor = m_LastLightFactor,
			m_FrameDelta = m_RenderingSystem.frameDelta,
			m_SmoothnessDelta = m_RenderingSystem.frameDelta * 0.0016666667f,
			m_BuildingStateOverride = m_LastBuildingStateOverride,
			m_CullingFlags = cullingFlags,
			m_UpdateMasks = updateMasks,
			m_NativeBatchGroups = nativeBatchGroups,
			m_CullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies3),
			m_WindData = m_WindSystem.GetData(readOnly: true, out dependencies4),
			m_NativeBatchInstances = nativeBatchInstances.AsParallelInstanceWriter(),
			m_SmoothingNeeded = m_SmoothingNeeded.AsParallelWriter()
		};
		JobHandle dependencies5;
		JobHandle dependencies6;
		BatchLodJob jobData2 = new BatchLodJob
		{
			m_DisableLods = m_RenderingSystem.disableLodModels,
			m_UseLodFade = flag,
			m_LodParameters = lodParameters,
			m_CameraPosition = cameraPosition,
			m_CameraDirection = cameraDirection,
			m_PixelSizeFactor = pixelSizeFactor,
			m_LodFadeDelta = lodFadeDelta,
			m_LoadingState = m_BatchMeshSystem.GetLoadingState(out dependencies5),
			m_NativeBatchGroups = nativeBatchGroups,
			m_NativeBatchInstances = cullingWriter.AsParallel(),
			m_BatchPriority = m_BatchMeshSystem.GetBatchPriority(out dependencies6)
		};
		if (!m_RenderingSettingsQuery.IsEmptyIgnoreFilter)
		{
			jobData.m_RenderingSettingsData = m_RenderingSettingsQuery.GetSingleton<RenderingSettingsData>();
		}
		JobHandle vTRequestMaxPixels = m_ManagedBatchSystem.GetVTRequestMaxPixels(out jobData2.m_VTRequestsMaxPixels0, out jobData2.m_VTRequestsMaxPixels1);
		JobHandle jobHandle = jobData.Schedule(jobData.m_CullingData, 16, JobUtils.CombineDependencies(base.Dependency, dependencies3, dependencies4, dependencies));
		JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(jobData2, activeGroupCount, 1, JobUtils.CombineDependencies(jobHandle, vTRequestMaxPixels, dependencies6, dependencies5));
		JobHandle jobHandle3 = nativeBatchInstances.EndCulling(cullingWriter, jobHandle2);
		m_BatchManagerSystem.AddNativeBatchGroupsReader(jobHandle2);
		m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle3);
		m_BatchMeshSystem.AddBatchPriorityWriter(jobHandle2);
		m_BatchMeshSystem.AddLoadingStateReader(jobHandle2);
		m_ManagedBatchSystem.AddVTRequestWriter(jobHandle2);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
		m_WindSystem.AddReader(jobHandle);
		m_BatchMeshSystem.UpdateBatchPriorities();
		base.Dependency = jobHandle;
	}

	private void GetDataQuery(out PreCullingFlags cullingFlags, out UpdateMasks updateMasks)
	{
		cullingFlags = PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.BatchesUpdated | PreCullingFlags.FadeContainer | PreCullingFlags.InterpolatedTransform | PreCullingFlags.ColorsUpdated;
		updateMasks = default(UpdateMasks);
		if (!m_RenderingSystem.editorBuildingStateOverride.Equals(m_LastBuildingStateOverride))
		{
			m_LastBuildingStateOverride = m_RenderingSystem.editorBuildingStateOverride;
			m_LastCitizenPresenceVersion--;
		}
		uint lastSystemVersion = m_CitizenPresenceSystem.LastSystemVersion;
		uint lastSystemVersion2 = m_TreeGrowthSystem.LastSystemVersion;
		uint lastSystemVersion3 = m_WetnessSystem.LastSystemVersion;
		uint lastSystemVersion4 = m_DirtynessSystem.LastSystemVersion;
		uint num = ((m_RenderingSystem.frameIndex >= m_LastLaneConditionFrame + 128) ? m_RenderingSystem.frameIndex : m_LastLaneConditionFrame);
		uint num2 = ((m_RenderingSystem.frameIndex >= m_LastDamagedFrame + 128) ? m_RenderingSystem.frameIndex : m_LastDamagedFrame);
		float num3 = CalculateLightFactor();
		if (m_UpdateAll)
		{
			cullingFlags |= PreCullingFlags.NearCamera | PreCullingFlags.InfoviewColor | PreCullingFlags.BuildingState | PreCullingFlags.TreeGrowth | PreCullingFlags.LaneCondition | PreCullingFlags.SurfaceState | PreCullingFlags.SurfaceDamage | PreCullingFlags.SmoothColor;
			updateMasks.UpdateAll();
			m_UpdateAll = false;
		}
		else
		{
			SmoothingNeeded result = m_SmoothingNeeded.GetResult();
			if (m_ToolSystem.activeInfoview != null)
			{
				cullingFlags |= PreCullingFlags.InfoviewColor;
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.InfoviewColor);
				updateMasks.m_NetMask.UpdateProperty(NetProperty.InfoviewColor);
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.InfoviewColor);
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.FlowMatrix);
			}
			if (lastSystemVersion != m_LastCitizenPresenceVersion || num3 != m_LastLightFactor)
			{
				cullingFlags |= PreCullingFlags.BuildingState;
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.BuildingState);
			}
			if (lastSystemVersion2 != m_LastTreeGrowthVersion)
			{
				cullingFlags |= PreCullingFlags.TreeGrowth;
			}
			if (lastSystemVersion3 != m_LastWetnessVersion || result.IsNeeded(SmoothingType.SurfaceWetness))
			{
				cullingFlags |= PreCullingFlags.SurfaceState;
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.SurfaceWetness);
			}
			if (lastSystemVersion4 != m_LastDirtynessVersion || result.IsNeeded(SmoothingType.SurfaceDirtyness))
			{
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.SurfaceDamage);
			}
			if (num != m_LastLaneConditionFrame)
			{
				cullingFlags |= PreCullingFlags.LaneCondition;
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.CurveDeterioration);
			}
			if (num2 != m_LastDamagedFrame || result.IsNeeded(SmoothingType.SurfaceDamage))
			{
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.SurfaceDamage);
			}
			if (m_MeshColorSystem.smoothColorsUpdated || result.IsNeeded(SmoothingType.ColorMask))
			{
				cullingFlags |= PreCullingFlags.SmoothColor;
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.ColorMask1);
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.ColorMask2);
				updateMasks.m_ObjectMask.UpdateProperty(ObjectProperty.ColorMask3);
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.ColorMask1);
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.ColorMask2);
				updateMasks.m_LaneMask.UpdateProperty(LaneProperty.ColorMask3);
			}
		}
		m_SmoothingNeeded.Clear();
		m_LastCitizenPresenceVersion = lastSystemVersion;
		m_LastTreeGrowthVersion = lastSystemVersion2;
		m_LastWetnessVersion = lastSystemVersion3;
		m_LastDirtynessVersion = lastSystemVersion4;
		m_LastLightFactor = num3;
		m_LastLaneConditionFrame = num;
		m_LastDamagedFrame = num2;
	}

	private float CalculateLightFactor()
	{
		float dayLightBrightness = m_LightingSystem.dayLightBrightness;
		return math.saturate(1f - math.round(dayLightBrightness * 100f) * 0.01f);
	}

	private void UpdateGlobalValues(NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances)
	{
		SHCoefficients value = new SHCoefficients(RenderSettings.ambientProbe);
		nativeBatchInstances.SetGlobalValue(value, m_SHCoefficients);
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
	public BatchDataSystem()
	{
	}
}
