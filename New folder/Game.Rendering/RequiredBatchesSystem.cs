using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
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
public class RequiredBatchesSystem : GameSystemBase
{
	[BurstCompile]
	private struct RequiredBatchesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> m_ElevationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> m_ObjectMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> m_OrphanType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> m_UtilityLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Marker> m_NetMarkerType;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentTypeHandle<Warning> m_WarningType;

		[ReadOnly]
		public ComponentTypeHandle<Override> m_OverrideType;

		[ReadOnly]
		public ComponentTypeHandle<Highlighted> m_HighlightedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_PrefabSubMeshGroups;

		[ReadOnly]
		public BufferLookup<LodMesh> m_PrefabLodMeshes;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> m_PrefabCompositionMeshRef;

		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public BufferLookup<MeshMaterial> m_PrefabMeshMaterials;

		public ComponentLookup<NetCompositionMeshData> m_PrefabCompositionMeshData;

		public ComponentLookup<ZoneBlockData> m_PrefabZoneBlockData;

		public BufferLookup<BatchGroup> m_PrefabBatchGroups;

		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_ObjectType))
			{
				UpdateObjectBatches(chunk);
			}
			else if (chunk.Has(ref m_CompositionType) || chunk.Has(ref m_OrphanType))
			{
				UpdateNetBatches(chunk);
			}
			else if (chunk.Has(ref m_LaneType))
			{
				UpdateLaneBatches(chunk);
			}
			else if (chunk.Has(ref m_BlockType))
			{
				UpdateZoneBatches(chunk);
			}
		}

		private void UpdateObjectBatches(ArchetypeChunk chunk)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Objects.Elevation> nativeArray2 = chunk.GetNativeArray(ref m_ElevationType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<MeshGroup> bufferAccessor = chunk.GetBufferAccessor(ref m_MeshGroupType);
			bool flag = chunk.Has(ref m_InterpolatedTransformType) || chunk.Has(ref m_StoppedType);
			bool flag2 = chunk.Has(ref m_ObjectMarkerType);
			MeshLayer meshLayer = MeshLayer.Default;
			if (chunk.Has(ref m_ErrorType) || chunk.Has(ref m_WarningType) || chunk.Has(ref m_OverrideType) || chunk.Has(ref m_HighlightedType))
			{
				meshLayer |= MeshLayer.Outline;
			}
			SubMeshGroup subMeshGroup = default(SubMeshGroup);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				PrefabRef prefabRef = nativeArray4[i];
				if (!m_PrefabSubMeshes.HasBuffer(prefabRef.m_Prefab))
				{
					continue;
				}
				DynamicBuffer<SubMesh> dynamicBuffer = m_PrefabSubMeshes[prefabRef.m_Prefab];
				MeshLayer meshLayer2 = meshLayer;
				if (nativeArray3.Length != 0)
				{
					Temp temp = nativeArray3[i];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
					{
						meshLayer2 |= MeshLayer.Outline;
					}
				}
				if (flag2)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Marker;
				}
				else if (flag)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Moving;
				}
				else if (nativeArray2.Length != 0 && nativeArray2[i].m_Elevation < 0f)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Tunnel;
				}
				DynamicBuffer<MeshGroup> value = default(DynamicBuffer<MeshGroup>);
				int num = 1;
				if (m_PrefabSubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData) && CollectionUtils.TryGet(bufferAccessor, i, out value))
				{
					num = value.Length;
				}
				for (int j = 0; j < num; j++)
				{
					if (bufferData.IsCreated)
					{
						CollectionUtils.TryGet(value, j, out var value2);
						subMeshGroup = bufferData[value2.m_SubMeshGroup];
					}
					else
					{
						subMeshGroup.m_SubMeshRange = new int2(0, dynamicBuffer.Length);
					}
					for (int k = subMeshGroup.m_SubMeshRange.x; k < subMeshGroup.m_SubMeshRange.y; k++)
					{
						SubMesh subMesh = dynamicBuffer[k];
						MeshData value3 = m_PrefabMeshData[subMesh.m_SubMesh];
						MeshLayer meshLayer3 = meshLayer2;
						if ((value3.m_DefaultLayers != 0 && (meshLayer2 & (MeshLayer.Moving | MeshLayer.Marker)) == 0) || (value3.m_DefaultLayers & (MeshLayer.Pipeline | MeshLayer.SubPipeline)) != 0)
						{
							meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Moving | MeshLayer.Tunnel | MeshLayer.Marker);
							CollectionUtils.TryGet(nativeArray, i, out var value4);
							meshLayer3 |= Game.Net.SearchSystem.GetLayers(value4, default(Game.Net.UtilityLane), value3.m_DefaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData);
						}
						MeshLayer meshLayer4 = (MeshLayer)((uint)meshLayer3 & (uint)(ushort)(~(int)value3.m_AvailableLayers));
						MeshType meshType = (MeshType)(1 & (ushort)(~(int)value3.m_AvailableTypes));
						if (meshLayer4 != 0 || meshType != 0)
						{
							value3.m_AvailableLayers |= meshLayer4;
							value3.m_AvailableTypes |= meshType;
							m_PrefabMeshData[subMesh.m_SubMesh] = value3;
							InitializeBatchGroups(subMesh.m_SubMesh, meshLayer4, meshType, value3.m_AvailableLayers, value3.m_AvailableTypes, isNewPartition: false, 0);
						}
					}
				}
			}
		}

		private void UpdateNetBatches(ArchetypeChunk chunk)
		{
			NativeArray<Composition> nativeArray = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<Orphan> nativeArray2 = chunk.GetNativeArray(ref m_OrphanType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag = chunk.Has(ref m_NetMarkerType);
			MeshLayer meshLayer = MeshLayer.Default;
			if (chunk.Has(ref m_ErrorType) || chunk.Has(ref m_WarningType) || chunk.Has(ref m_HighlightedType))
			{
				meshLayer |= MeshLayer.Outline;
			}
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				MeshLayer meshLayer2 = meshLayer;
				if (nativeArray3.Length != 0)
				{
					Temp temp = nativeArray3[i];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
					{
						meshLayer2 |= MeshLayer.Outline;
					}
				}
				if (flag)
				{
					meshLayer2 &= ~MeshLayer.Default;
					meshLayer2 |= MeshLayer.Marker;
				}
				if (nativeArray.Length != 0)
				{
					Composition composition = nativeArray[i];
					UpdateNetBatches(composition.m_Edge, meshLayer2);
					UpdateNetBatches(composition.m_StartNode, meshLayer2);
					UpdateNetBatches(composition.m_EndNode, meshLayer2);
				}
				else if (nativeArray2.Length != 0)
				{
					UpdateNetBatches(nativeArray2[i].m_Composition, meshLayer2);
				}
			}
		}

		private void UpdateNetBatches(Entity composition, MeshLayer requiredLayers)
		{
			if (m_PrefabCompositionMeshRef.TryGetComponent(composition, out var componentData) && m_PrefabCompositionMeshData.TryGetComponent(componentData.m_Mesh, out var componentData2))
			{
				MeshLayer meshLayer = requiredLayers;
				if (componentData2.m_DefaultLayers != 0 && (requiredLayers & MeshLayer.Marker) == 0)
				{
					meshLayer &= ~MeshLayer.Default;
					meshLayer |= componentData2.m_DefaultLayers;
				}
				MeshLayer meshLayer2 = (MeshLayer)((uint)meshLayer & (uint)(ushort)(~(int)componentData2.m_AvailableLayers));
				if (meshLayer2 != 0)
				{
					componentData2.m_AvailableLayers |= meshLayer2;
					m_PrefabCompositionMeshData[componentData.m_Mesh] = componentData2;
					InitializeBatchGroups(componentData.m_Mesh, meshLayer2, (MeshType)0, componentData2.m_AvailableLayers, MeshType.Net, isNewPartition: false, 0);
				}
			}
		}

		private void UpdateLaneBatches(ArchetypeChunk chunk)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Net.UtilityLane> nativeArray2 = chunk.GetNativeArray(ref m_UtilityLaneType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			MeshLayer meshLayer = MeshLayer.Default;
			if (chunk.Has(ref m_ErrorType) || chunk.Has(ref m_WarningType) || chunk.Has(ref m_HighlightedType))
			{
				meshLayer |= MeshLayer.Outline;
			}
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				PrefabRef prefabRef = nativeArray4[i];
				if (!m_PrefabSubMeshes.HasBuffer(prefabRef.m_Prefab))
				{
					continue;
				}
				DynamicBuffer<SubMesh> dynamicBuffer = m_PrefabSubMeshes[prefabRef.m_Prefab];
				MeshLayer meshLayer2 = meshLayer;
				if (nativeArray3.Length != 0)
				{
					Temp temp = nativeArray3[i];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent | TempFlags.SubDetail)) != 0)
					{
						meshLayer2 |= MeshLayer.Outline;
					}
				}
				if (nativeArray.Length != 0)
				{
					Owner owner = nativeArray[i];
					if (IsNetOwnerTunnel(owner))
					{
						meshLayer2 &= ~MeshLayer.Default;
						meshLayer2 |= MeshLayer.Tunnel;
					}
				}
				int num = 256;
				if (nativeArray2.Length != 0 && m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_UtilityTypes & ~(UtilityTypes.StormwaterPipe | UtilityTypes.Fence | UtilityTypes.Catenary)) != UtilityTypes.None)
				{
					num = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float2(componentData.m_VisualCapacity)));
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					SubMesh subMesh = dynamicBuffer[j];
					MeshData value = m_PrefabMeshData[subMesh.m_SubMesh];
					MeshLayer meshLayer3 = meshLayer2;
					if ((subMesh.m_Flags & SubMeshFlags.RequireEditor) != 0)
					{
						meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Tunnel);
						meshLayer3 |= MeshLayer.Marker;
					}
					if ((value.m_DefaultLayers != 0 && (meshLayer3 & MeshLayer.Marker) == 0) || (value.m_DefaultLayers & (MeshLayer.Pipeline | MeshLayer.SubPipeline)) != 0)
					{
						meshLayer3 &= ~(MeshLayer.Default | MeshLayer.Tunnel | MeshLayer.Marker);
						CollectionUtils.TryGet(nativeArray, i, out var value2);
						CollectionUtils.TryGet(nativeArray2, i, out var value3);
						meshLayer3 |= Game.Net.SearchSystem.GetLayers(value2, value3, value.m_DefaultLayers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData);
					}
					MeshLayer meshLayer4 = (MeshLayer)((uint)meshLayer3 & (uint)(ushort)(~(int)value.m_AvailableLayers));
					MeshType meshType = (MeshType)(4 & (ushort)(~(int)value.m_AvailableTypes));
					if (meshLayer4 != 0 || meshType != 0)
					{
						value.m_AvailableLayers |= meshLayer4;
						value.m_AvailableTypes |= meshType;
						m_PrefabMeshData[subMesh.m_SubMesh] = value;
						InitializeBatchGroups(subMesh.m_SubMesh, meshLayer4, meshType, value.m_AvailableLayers, value.m_AvailableTypes, isNewPartition: false, value.m_MinLod);
						if (num < value.m_MinLod)
						{
							InitializeBatchGroups(subMesh.m_SubMesh, meshLayer4, meshType, value.m_AvailableLayers, value.m_AvailableTypes, isNewPartition: false, (ushort)num);
						}
					}
				}
			}
		}

		private void UpdateZoneBatches(ArchetypeChunk chunk)
		{
			NativeArray<Block> nativeArray = chunk.GetNativeArray(ref m_BlockType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Block block = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				ZoneBlockData value = m_PrefabZoneBlockData[prefabRef.m_Prefab];
				ushort num = (ushort)math.clamp(block.m_Size.x * block.m_Size.y - 1 >> 4, 0, 3);
				MeshLayer meshLayer = (MeshLayer)(1 & (ushort)(~(int)value.m_AvailableLayers));
				ushort num2 = (ushort)((1 << (int)num) & ~value.m_AvailablePartitions);
				if (meshLayer != 0 || num2 != 0)
				{
					value.m_AvailableLayers |= meshLayer;
					value.m_AvailablePartitions |= num2;
					m_PrefabZoneBlockData[prefabRef.m_Prefab] = value;
					InitializeBatchGroups(prefabRef.m_Prefab, meshLayer, (MeshType)0, value.m_AvailableLayers, MeshType.Zone, num2 != 0, num);
				}
			}
		}

		private void InitializeBatchGroups(Entity mesh, MeshLayer newLayers, MeshType newTypes, MeshLayer allLayers, MeshType allTypes, bool isNewPartition, ushort partition)
		{
			if (newLayers != 0)
			{
				MeshLayer meshLayer = MeshLayer.Default;
				while ((int)meshLayer <= 128)
				{
					if ((newLayers & meshLayer) != 0)
					{
						MeshType meshType = MeshType.Object;
						while ((int)meshType <= 8)
						{
							if ((allTypes & meshType) != 0)
							{
								InitializeBatchGroup(mesh, meshLayer, meshType, partition);
							}
							meshType = (MeshType)((uint)meshType << 1);
						}
					}
					meshLayer = (MeshLayer)((uint)meshLayer << 1);
				}
				allLayers = (MeshLayer)((uint)allLayers & (uint)(ushort)(~(int)newLayers));
			}
			if (newTypes != 0)
			{
				MeshType meshType2 = MeshType.Object;
				while ((int)meshType2 <= 8)
				{
					if ((newTypes & meshType2) != 0)
					{
						MeshLayer meshLayer2 = MeshLayer.Default;
						while ((int)meshLayer2 <= 128)
						{
							if ((allLayers & meshLayer2) != 0)
							{
								InitializeBatchGroup(mesh, meshLayer2, meshType2, partition);
							}
							meshLayer2 = (MeshLayer)((uint)meshLayer2 << 1);
						}
					}
					meshType2 = (MeshType)((uint)meshType2 << 1);
				}
				allTypes = (MeshType)((uint)allTypes & (uint)(ushort)(~(int)newTypes));
			}
			if (!isNewPartition)
			{
				return;
			}
			MeshLayer meshLayer3 = MeshLayer.Default;
			while ((int)meshLayer3 <= 128)
			{
				if ((allLayers & meshLayer3) != 0)
				{
					MeshType meshType3 = MeshType.Object;
					while ((int)meshType3 <= 8)
					{
						if ((allTypes & meshType3) != 0)
						{
							InitializeBatchGroup(mesh, meshLayer3, meshType3, partition);
						}
						meshType3 = (MeshType)((uint)meshType3 << 1);
					}
				}
				meshLayer3 = (MeshLayer)((uint)meshLayer3 << 1);
			}
		}

		private void InitializeBatchGroup(Entity mesh, MeshLayer layer, MeshType type, ushort partition)
		{
			DynamicBuffer<LodMesh> dynamicBuffer;
			Bounds3 bounds;
			MeshFlags meshFlags;
			int num;
			float num2;
			int num3;
			float bias;
			float bias2;
			int x;
			int valueToClamp;
			switch (type)
			{
			case MeshType.Zone:
				dynamicBuffer = default(DynamicBuffer<LodMesh>);
				bounds = ZoneMeshHelpers.GetBounds(new int2(10, 6));
				meshFlags = (MeshFlags)0u;
				num = 1;
				num2 = ZoneMeshHelpers.GetIndexCount(new int2(10, 6));
				num3 = 1;
				x = 0;
				valueToClamp = 0;
				bias = 0f;
				bias2 = 0f;
				break;
			case MeshType.Net:
			{
				NetCompositionMeshData netCompositionMeshData = m_PrefabCompositionMeshData[mesh];
				dynamicBuffer = default(DynamicBuffer<LodMesh>);
				bounds = new Bounds3(new float3(netCompositionMeshData.m_Width * -0.5f, netCompositionMeshData.m_HeightRange.min, 0f), new float3(netCompositionMeshData.m_Width * 0.5f, netCompositionMeshData.m_HeightRange.max, 0f));
				meshFlags = (MeshFlags)0u;
				num = m_PrefabMeshMaterials[mesh].Length;
				num2 = netCompositionMeshData.m_IndexFactor;
				num3 = 0;
				x = 0;
				valueToClamp = 0;
				bias = netCompositionMeshData.m_LodBias;
				bias2 = netCompositionMeshData.m_ShadowBias;
				if (m_PrefabLodMeshes.HasBuffer(mesh))
				{
					dynamicBuffer = m_PrefabLodMeshes[mesh];
					num3 = dynamicBuffer.Length;
				}
				break;
			}
			default:
			{
				MeshData meshData = m_PrefabMeshData[mesh];
				dynamicBuffer = default(DynamicBuffer<LodMesh>);
				bounds = RenderingUtils.SafeBounds(meshData.m_Bounds);
				meshFlags = meshData.m_State;
				num = math.select(meshData.m_SubMeshCount, meshData.m_SubMeshCount + 1, (meshFlags & MeshFlags.Base) != 0);
				num2 = meshData.m_IndexCount;
				num3 = 0;
				x = ((type == MeshType.Lane) ? partition : meshData.m_MinLod);
				valueToClamp = meshData.m_ShadowLod;
				bias = meshData.m_LodBias;
				bias2 = meshData.m_ShadowBias;
				if (m_PrefabLodMeshes.HasBuffer(mesh))
				{
					dynamicBuffer = m_PrefabLodMeshes[mesh];
					num3 = dynamicBuffer.Length;
				}
				break;
			}
			}
			DynamicBuffer<BatchGroup> dynamicBuffer2 = m_PrefabBatchGroups[mesh];
			int num4 = num;
			if (dynamicBuffer.IsCreated)
			{
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (type == MeshType.Net)
					{
						num4 += m_PrefabMeshMaterials[dynamicBuffer[i].m_LodMesh].Length;
						continue;
					}
					MeshData meshData2 = m_PrefabMeshData[dynamicBuffer[i].m_LodMesh];
					num4 += meshData2.m_SubMeshCount;
					num4 = math.select(num4, num4 + 1, (meshData2.m_State & MeshFlags.Base) != 0);
				}
			}
			float3 secondaryCenter = MathUtils.Center(bounds);
			float3 @float = MathUtils.Size(bounds);
			GroupData groupData = new GroupData
			{
				m_Mesh = mesh,
				m_SecondaryCenter = secondaryCenter,
				m_SecondarySize = @float * 0.4f,
				m_Layer = layer,
				m_MeshType = type,
				m_Partition = partition,
				m_LodCount = (byte)num3
			};
			for (int j = 0; j < 16; j++)
			{
				groupData.SetPropertyIndex(j, -1);
			}
			int groupIndex = m_NativeBatchGroups.CreateGroup(groupData, num4, m_NativeBatchInstances, m_NativeSubBatches);
			dynamicBuffer2.Add(new BatchGroup
			{
				m_GroupIndex = groupIndex,
				m_MergeIndex = -1,
				m_Layer = layer,
				m_Type = type,
				m_Partition = partition
			});
			StackDirection stackDirection = StackDirection.None;
			if ((meshFlags & MeshFlags.StackX) != 0)
			{
				stackDirection = StackDirection.Right;
			}
			if ((meshFlags & MeshFlags.StackY) != 0)
			{
				stackDirection = StackDirection.Up;
			}
			if ((meshFlags & MeshFlags.StackZ) != 0)
			{
				stackDirection = StackDirection.Forward;
			}
			float metersPerPixel = 0f;
			switch (type)
			{
			case MeshType.Object:
				metersPerPixel = RenderingUtils.GetRenderingSize(@float, stackDirection);
				break;
			case MeshType.Net:
				metersPerPixel = RenderingUtils.GetRenderingSize(@float.xy);
				x = RenderingUtils.CalculateLodLimit(metersPerPixel, bias);
				valueToClamp = RenderingUtils.CalculateLodLimit(RenderingUtils.GetShadowRenderingSize(@float.xy), bias2);
				break;
			case MeshType.Lane:
				metersPerPixel = RenderingUtils.GetRenderingSize(@float.xy);
				break;
			case MeshType.Zone:
				metersPerPixel = RenderingUtils.GetRenderingSize(@float);
				x = RenderingUtils.CalculateLodLimit(metersPerPixel, bias);
				valueToClamp = RenderingUtils.CalculateLodLimit(metersPerPixel, bias2);
				break;
			}
			x = math.min(x, 255 - num3);
			valueToClamp = math.clamp(valueToClamp, x, 255);
			for (int num5 = num3 - 1; num5 >= 0; num5--)
			{
				Entity entity;
				Bounds3 bounds2;
				int num6;
				float num7;
				switch (type)
				{
				case MeshType.Zone:
					entity = Entity.Null;
					bounds2 = bounds;
					num6 = 1;
					num7 = ZoneMeshHelpers.GetIndexCount(new int2(5, 3));
					break;
				case MeshType.Net:
				{
					entity = dynamicBuffer[num5].m_LodMesh;
					NetCompositionMeshData netCompositionMeshData2 = m_PrefabCompositionMeshData[entity];
					bounds2 = bounds;
					num6 = m_PrefabMeshMaterials[entity].Length;
					num7 = netCompositionMeshData2.m_IndexFactor;
					break;
				}
				default:
				{
					entity = dynamicBuffer[num5].m_LodMesh;
					MeshData meshData3 = m_PrefabMeshData[entity];
					bounds2 = meshData3.m_Bounds;
					num6 = math.select(meshData3.m_SubMeshCount, meshData3.m_SubMeshCount + 1, (meshData3.m_State & MeshFlags.Base) != 0);
					num7 = math.select(meshData3.m_IndexCount, num2 * 0.25f, (meshData3.m_State & (MeshFlags.Decal | MeshFlags.Impostor)) != 0);
					break;
				}
				}
				for (int k = 0; k < num6; k++)
				{
					BatchData batchData = new BatchData
					{
						m_LodMesh = entity,
						m_VTIndex0 = -1,
						m_VTIndex1 = -1,
						m_SubMeshIndex = (byte)k,
						m_MinLod = (byte)x,
						m_ShadowLod = (byte)valueToClamp,
						m_LodIndex = (byte)(num5 + 1)
					};
					if (m_NativeBatchGroups.CreateBatch(batchData, groupIndex, 16, m_NativeBatchInstances, m_NativeSubBatches) < 0)
					{
						UnityEngine.Debug.Log($"Too many batches in group (max: {16})");
						return;
					}
				}
				switch (type)
				{
				case MeshType.Object:
				case MeshType.Zone:
				{
					float3 meshSize = MathUtils.Size(bounds2);
					metersPerPixel = RenderingUtils.GetRenderingSize(@float, meshSize, num7, stackDirection);
					break;
				}
				case MeshType.Net:
				{
					float indexFactor2 = num7;
					metersPerPixel = RenderingUtils.GetRenderingSize(@float.xy, indexFactor2);
					break;
				}
				case MeshType.Lane:
				{
					float indexFactor = num7 / math.max(1f, MathUtils.Size(bounds2.z));
					metersPerPixel = RenderingUtils.GetRenderingSize(@float.xy, indexFactor);
					break;
				}
				}
				x = math.clamp(RenderingUtils.CalculateLodLimit(metersPerPixel, bias) + 1, x + 1, 255 - num5);
				valueToClamp = math.max(valueToClamp, x);
			}
			for (int l = 0; l < num; l++)
			{
				BatchData batchData2 = new BatchData
				{
					m_VTIndex0 = -1,
					m_VTIndex1 = -1,
					m_SubMeshIndex = (byte)l,
					m_MinLod = (byte)x,
					m_ShadowLod = (byte)valueToClamp
				};
				if (m_NativeBatchGroups.CreateBatch(batchData2, groupIndex, 16, m_NativeBatchInstances, m_NativeSubBatches) < 0)
				{
					UnityEngine.Debug.Log($"Too many batches in group (max: {16})");
					break;
				}
			}
		}

		private bool IsNetOwnerTunnel(Owner owner)
		{
			if (m_NetElevationData.TryGetComponent(owner.m_Owner, out var componentData) && math.cmin(componentData.m_Elevation) < 0f)
			{
				return true;
			}
			if (m_ConnectedEdges.TryGetBuffer(owner.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					ConnectedEdge connectedEdge = bufferData[i];
					if (m_NetElevationData.TryGetComponent(connectedEdge.m_Edge, out componentData) && math.cmin(componentData.m_Elevation) < 0f)
					{
						return true;
					}
				}
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Marker> __Game_Objects_Marker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> __Game_Net_Orphan_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Marker> __Game_Net_Marker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Warning> __Game_Tools_Warning_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Override> __Game_Tools_Override_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Highlighted> __Game_Tools_Highlighted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshRef> __Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RW_BufferLookup;

		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RW_BufferLookup;

		public BufferLookup<LodMesh> __Game_Prefabs_LodMesh_RW_BufferLookup;

		public BufferLookup<MeshMaterial> __Game_Prefabs_MeshMaterial_RW_BufferLookup;

		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RW_ComponentLookup;

		public ComponentLookup<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RW_ComponentLookup;

		public ComponentLookup<ZoneBlockData> __Game_Prefabs_ZoneBlockData_RW_ComponentLookup;

		public BufferLookup<BatchGroup> __Game_Prefabs_BatchGroup_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Object>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Marker>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Orphan>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Net_Marker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Marker>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Warning>(isReadOnly: true);
			__Game_Tools_Override_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Override>(isReadOnly: true);
			__Game_Tools_Highlighted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Highlighted>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshRef>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RW_BufferLookup = state.GetBufferLookup<SubMesh>();
			__Game_Prefabs_SubMeshGroup_RW_BufferLookup = state.GetBufferLookup<SubMeshGroup>();
			__Game_Prefabs_LodMesh_RW_BufferLookup = state.GetBufferLookup<LodMesh>();
			__Game_Prefabs_MeshMaterial_RW_BufferLookup = state.GetBufferLookup<MeshMaterial>();
			__Game_Prefabs_MeshData_RW_ComponentLookup = state.GetComponentLookup<MeshData>();
			__Game_Prefabs_NetCompositionMeshData_RW_ComponentLookup = state.GetComponentLookup<NetCompositionMeshData>();
			__Game_Prefabs_ZoneBlockData_RW_ComponentLookup = state.GetComponentLookup<ZoneBlockData>();
			__Game_Prefabs_BatchGroup_RW_BufferLookup = state.GetBufferLookup<BatchGroup>();
		}
	}

	private BatchManagerSystem m_BatchManagerSystem;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_AllQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<MeshBatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			}
		});
		m_AllQuery = GetEntityQuery(ComponentType.ReadOnly<MeshBatch>(), ComponentType.ReadOnly<PrefabRef>());
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
		EntityQuery query = (GetLoaded() ? m_AllQuery : m_UpdatedQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle dependencies3;
			JobHandle jobHandle = JobChunkExtensions.Schedule(new RequiredBatchesJob
			{
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ObjectMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NetMarkerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Marker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WarningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OverrideType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Override_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HighlightedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Highlighted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionMeshRef = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RW_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RW_BufferLookup, ref base.CheckedStateRef),
				m_PrefabLodMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LodMesh_RW_BufferLookup, ref base.CheckedStateRef),
				m_PrefabMeshMaterials = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshMaterial_RW_BufferLookup, ref base.CheckedStateRef),
				m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabZoneBlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneBlockData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBatchGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BatchGroup_RW_BufferLookup, ref base.CheckedStateRef),
				m_NativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies),
				m_NativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies2),
				m_NativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: false, out dependencies3)
			}, query, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3)));
			m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle);
			m_BatchManagerSystem.AddNativeBatchGroupsWriter(jobHandle);
			m_BatchManagerSystem.AddNativeSubBatchesWriter(jobHandle);
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
	public RequiredBatchesSystem()
	{
	}
}
