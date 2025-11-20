using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Serialization;
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
public class BatchManagerSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	private struct MergeGroupsJob : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[NativeDisableParallelForRestriction]
		public BufferLookup<BatchGroup> m_BatchGroups;

		[ReadOnly]
		public NativeList<Entity> m_MergeMeshes;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, int> m_MergeGroups;

		[NativeDisableParallelForRestriction]
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData>.ParallelGroupUpdater m_BatchGroupUpdater;

		[NativeDisableParallelForRestriction]
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceUpdater m_BatchInstanceUpdater;

		public void Execute(int index)
		{
			Entity entity = m_MergeMeshes[index];
			DynamicBuffer<BatchGroup> groups = m_BatchGroups[entity];
			if (!m_MergeGroups.TryGetFirstValue(entity, out var item, out var it))
			{
				return;
			}
			do
			{
				GroupUpdater<CullingData, GroupData, BatchData, InstanceData> groupUpdater = m_BatchGroupUpdater.BeginGroup(item);
				GroupInstanceUpdater<CullingData, GroupData, BatchData, InstanceData> groupInstanceUpdater = m_BatchInstanceUpdater.BeginGroup(item);
				GroupData groupData = groupUpdater.GetGroupData();
				DynamicBuffer<BatchGroup> groups2 = m_BatchGroups[groupData.m_Mesh];
				int groupIndex = GetGroupIndex(groups, groupData);
				GroupUpdater<CullingData, GroupData, BatchData, InstanceData> groupUpdater2 = m_BatchGroupUpdater.BeginGroup(groupIndex);
				GroupInstanceUpdater<CullingData, GroupData, BatchData, InstanceData> groupInstanceUpdater2 = m_BatchInstanceUpdater.BeginGroup(groupIndex);
				int mergeIndex = groupUpdater2.MergeGroup(item, groupInstanceUpdater2);
				SetGroupIndex(groups2, groupData, groupIndex, mergeIndex);
				for (int num = groupInstanceUpdater.GetInstanceCount() - 1; num >= 0; num--)
				{
					InstanceData instanceData = groupInstanceUpdater.GetInstanceData(num);
					CullingData cullingData = groupInstanceUpdater.GetCullingData(num);
					groupInstanceUpdater.RemoveInstance(num);
					int targetInstance = groupInstanceUpdater2.AddInstance(instanceData, cullingData, mergeIndex);
					DynamicBuffer<MeshBatch> meshBatches = m_MeshBatches[instanceData.m_Entity];
					SetInstanceIndex(meshBatches, item, num, groupIndex, targetInstance);
				}
				m_BatchInstanceUpdater.EndGroup(groupInstanceUpdater);
				m_BatchGroupUpdater.EndGroup(groupUpdater);
				m_BatchInstanceUpdater.EndGroup(groupInstanceUpdater2);
				m_BatchGroupUpdater.EndGroup(groupUpdater2);
			}
			while (m_MergeGroups.TryGetNextValue(out item, ref it));
		}

		private int GetGroupIndex(DynamicBuffer<BatchGroup> groups, GroupData groupData)
		{
			for (int i = 0; i < groups.Length; i++)
			{
				ref BatchGroup reference = ref groups.ElementAt(i);
				if (reference.m_Layer == groupData.m_Layer && reference.m_Type == groupData.m_MeshType && reference.m_Partition == groupData.m_Partition)
				{
					return reference.m_GroupIndex;
				}
			}
			return -1;
		}

		private void SetGroupIndex(DynamicBuffer<BatchGroup> groups, GroupData groupData, int groupIndex, int mergeIndex)
		{
			for (int i = 0; i < groups.Length; i++)
			{
				ref BatchGroup reference = ref groups.ElementAt(i);
				if (reference.m_Layer == groupData.m_Layer && reference.m_Type == groupData.m_MeshType && reference.m_Partition == groupData.m_Partition)
				{
					reference.m_GroupIndex = groupIndex;
					reference.m_MergeIndex = mergeIndex;
					break;
				}
			}
		}

		private void SetInstanceIndex(DynamicBuffer<MeshBatch> meshBatches, int sourceGroup, int sourceInstance, int targetGroup, int targetInstance)
		{
			for (int i = 0; i < meshBatches.Length; i++)
			{
				ref MeshBatch reference = ref meshBatches.ElementAt(i);
				if (reference.m_GroupIndex == sourceGroup && reference.m_InstanceIndex == sourceInstance)
				{
					reference.m_GroupIndex = targetGroup;
					reference.m_InstanceIndex = targetInstance;
					break;
				}
			}
		}
	}

	[BurstCompile]
	private struct MergeCleanupJob : IJob
	{
		public NativeList<Entity> m_MergeMeshes;

		public NativeParallelMultiHashMap<Entity, int> m_MergeGroups;

		public void Execute()
		{
			m_MergeMeshes.Clear();
			m_MergeGroups.Clear();
		}
	}

	[BurstCompile]
	private struct InitializeLodFadeJob : IJobParallelFor
	{
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelInstanceWriter m_NativeBatchInstances;

		public void Execute(int index)
		{
			WriteableCullingAccessor<CullingData> cullingAccessor = m_NativeBatchInstances.GetCullingAccessor(index);
			for (int i = 0; i < cullingAccessor.Length; i++)
			{
				cullingAccessor.Get(i).lodFade = 255;
			}
		}
	}

	[BurstCompile]
	private struct AllocateBuffersJob : IJob
	{
		[ReadOnly]
		public NativeList<PropertyData> m_ObjectProperties;

		[ReadOnly]
		public NativeList<PropertyData> m_NetProperties;

		[ReadOnly]
		public NativeList<PropertyData> m_LaneProperties;

		[ReadOnly]
		public NativeList<PropertyData> m_ZoneProperties;

		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		public void Execute()
		{
			UpdatedPropertiesEnumerator updatedProperties = m_NativeBatchGroups.GetUpdatedProperties();
			int groupIndex;
			while (updatedProperties.GetNextUpdatedGroup(out groupIndex))
			{
				m_NativeBatchGroups.AllocatePropertyBuffers(groupIndex, 16777216u, m_NativeBatchInstances);
				GroupData groupData = m_NativeBatchGroups.GetGroupData(groupIndex);
				switch (groupData.m_MeshType)
				{
				case MeshType.Object:
					SetPropertyIndices(m_ObjectProperties, groupIndex, ref groupData);
					break;
				case MeshType.Net:
					SetPropertyIndices(m_NetProperties, groupIndex, ref groupData);
					break;
				case MeshType.Lane:
					SetPropertyIndices(m_LaneProperties, groupIndex, ref groupData);
					break;
				case MeshType.Zone:
					SetPropertyIndices(m_ZoneProperties, groupIndex, ref groupData);
					break;
				}
				m_NativeBatchGroups.SetGroupData(groupIndex, groupData);
			}
			m_NativeBatchGroups.ClearUpdatedProperties();
			UpdatedInstanceEnumerator updatedInstances = m_NativeBatchInstances.GetUpdatedInstances();
			int groupIndex2;
			while (updatedInstances.GetNextUpdatedGroup(out groupIndex2))
			{
				m_NativeBatchInstances.AllocateInstanceBuffers(groupIndex2, 16777216u, m_NativeBatchGroups);
			}
			m_NativeBatchInstances.ClearUpdatedInstances();
		}

		private void SetPropertyIndices(NativeList<PropertyData> properties, int groupIndex, ref GroupData groupData)
		{
			NativeGroupPropertyAccessor groupPropertyAccessor = m_NativeBatchGroups.GetGroupPropertyAccessor(groupIndex);
			for (int i = 0; i < properties.Length; i++)
			{
				groupData.SetPropertyIndex(i, -1);
			}
			int num = groupPropertyAccessor.PropertyCount;
			if (num > 30)
			{
				UnityEngine.Debug.Log($"Too many group properties ({num})!");
				num = 30;
			}
			for (int j = 0; j < num; j++)
			{
				int propertyName = groupPropertyAccessor.GetPropertyName(j);
				int dataIndex = groupPropertyAccessor.GetDataIndex(j);
				for (int k = 0; k < properties.Length; k++)
				{
					PropertyData propertyData = properties[k];
					if (propertyName == propertyData.m_NameID && dataIndex == propertyData.m_DataIndex)
					{
						groupData.SetPropertyIndex(k, j);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct GenerateSubBatchesJob : IJob
	{
		public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

		public void Execute()
		{
			UpdatedSubBatchEnumerator updatedSubBatches = m_NativeSubBatches.GetUpdatedSubBatches();
			int groupIndex;
			while (updatedSubBatches.GetNextUpdatedGroup(out groupIndex))
			{
				m_NativeSubBatches.GenerateSubBatches(groupIndex);
			}
			m_NativeSubBatches.ClearUpdatedSubBatches();
		}
	}

	private struct ActiveGroupData
	{
		public int m_BatchOffset;

		public int m_InstanceOffset;
	}

	[BurstCompile]
	private struct AllocateCullingJob : IJob
	{
		[ReadOnly]
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		[ReadOnly]
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		[ReadOnly]
		public BatchRenderFlags m_RequiredFlagMask;

		[ReadOnly]
		public int m_MaxSplitBatchCount;

		public BatchCullingOutput m_CullingOutput;

		public NativeArray<ActiveGroupData> m_ActiveGroupData;

		public unsafe void Execute()
		{
			ref BatchCullingOutputDrawCommands reference = ref m_CullingOutput.drawCommands.ElementAt(0);
			reference = default(BatchCullingOutputDrawCommands);
			int activeGroupCount = m_NativeBatchInstances.GetActiveGroupCount();
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < activeGroupCount; i++)
			{
				int groupIndex = m_NativeBatchInstances.GetGroupIndex(i);
				GroupData groupData = m_NativeBatchGroups.GetGroupData(groupIndex);
				if ((groupData.m_RenderFlags & m_RequiredFlagMask) == m_RequiredFlagMask)
				{
					m_ActiveGroupData[i] = new ActiveGroupData
					{
						m_BatchOffset = num,
						m_InstanceOffset = num2
					};
					int instanceCount = m_NativeBatchInstances.GetInstanceCount(groupIndex);
					num += m_NativeBatchGroups.GetBatchCount(groupIndex);
					num2 += instanceCount * (1 + groupData.m_LodCount);
					if (instanceCount > 16777216)
					{
						UnityEngine.Debug.Log($"Too many batch instances: {instanceCount} > 16777216");
					}
				}
			}
			int num3 = (reference.drawCommandCount = num * m_MaxSplitBatchCount);
			reference.drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>() * num3, UnsafeUtility.AlignOf<BatchDrawCommand>(), Allocator.TempJob);
			reference.visibleInstanceCount = num2;
			reference.visibleInstances = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * num2, UnsafeUtility.AlignOf<int>(), Allocator.TempJob);
			reference.drawRangeCount = num;
			reference.drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>() * num, UnsafeUtility.AlignOf<BatchDrawRange>(), Allocator.TempJob);
		}
	}

	private struct CullingSplitData
	{
		public ulong m_PlaneMask;

		public int m_SplitMask;

		public float m_ShadowHeightThreshold;

		public float m_ShadowVolumeThreshold;
	}

	[BurstCompile]
	private struct CullingPlanesJob : IJob
	{
		[ReadOnly]
		public NativeArray<Plane> m_CullingPlanes;

		[ReadOnly]
		public NativeArray<CullingSplit> m_CullingSplits;

		[ReadOnly]
		public float3 m_ShadowCullingData;

		public NativeList<CullingSplitData> m_SplitData;

		public NativeList<FrustumPlanes.PlanePacket4> m_PlanePackets;

		private const float kGuardPixels = 5f;

		public void Execute()
		{
			NativeArray<Plane> cullingPlanes = new NativeArray<Plane>(m_CullingPlanes.Length, Allocator.Temp);
			int num = 0;
			for (int i = 0; i < m_CullingSplits.Length; i++)
			{
				CullingSplit cullingSplit = m_CullingSplits[i];
				CullingSplitData value = new CullingSplitData
				{
					m_SplitMask = 1 << i,
					m_ShadowHeightThreshold = 0f,
					m_ShadowVolumeThreshold = 0f
				};
				for (int j = 0; j < cullingSplit.cullingPlaneCount; j++)
				{
					Plane value2 = m_CullingPlanes[cullingSplit.cullingPlaneOffset + j];
					int num2 = -1;
					for (int k = 0; k < num; k++)
					{
						Plane plane = cullingPlanes[k];
						if (math.all((float3)value2.normal == (float3)plane.normal) && value2.distance == plane.distance)
						{
							num2 = k;
							break;
						}
					}
					if (num2 == -1)
					{
						num2 = num++;
						cullingPlanes[num2] = value2;
					}
					value.m_PlaneMask |= (ulong)(1L << num2);
				}
				if (m_ShadowCullingData.x > 0f)
				{
					float num3 = CalculateCascadePixelToMeters(cullingSplit.sphereRadius, m_ShadowCullingData.x);
					value.m_ShadowHeightThreshold = num3 * m_ShadowCullingData.y;
					value.m_ShadowVolumeThreshold = num3 * num3 * m_ShadowCullingData.z;
				}
				m_SplitData.Add(in value);
			}
			FrustumPlanes.BuildSOAPlanePackets(cullingPlanes, num, m_PlanePackets);
			if (num > 64)
			{
				UnityEngine.Debug.Log($"Too many unique culling planes: {num} > 64");
			}
			if (m_CullingSplits.Length > 8)
			{
				UnityEngine.Debug.Log($"Too many culling splits: {m_CullingSplits.Length} > 8");
			}
			cullingPlanes.Dispose();
		}

		private float CalculateCascadePixelToMeters(float radius, float shadowResolution)
		{
			float num = radius * 2f;
			return (num + 10f * (num / shadowResolution)) / shadowResolution;
		}
	}

	[BurstCompile]
	private struct FinalizeCullingJob : IJob
	{
		public BatchCullingOutput m_CullingOutput;

		public unsafe void Execute()
		{
			ref BatchCullingOutputDrawCommands reference = ref m_CullingOutput.drawCommands.ElementAt(0);
			BatchDrawRange batchDrawRange = default(BatchDrawRange);
			int num = 0;
			int drawRangeCount = 0;
			for (int i = 0; i < reference.drawRangeCount; i++)
			{
				BatchDrawRange batchDrawRange2 = reference.drawRanges[i];
				if (batchDrawRange2.drawCommandsCount == 0)
				{
					continue;
				}
				int drawCommandsBegin = (int)batchDrawRange2.drawCommandsBegin;
				batchDrawRange2.drawCommandsBegin = (uint)num;
				for (int j = 0; j < batchDrawRange2.drawCommandsCount; j++)
				{
					reference.drawCommands[num++] = reference.drawCommands[drawCommandsBegin + j];
				}
				if (UnsafeUtility.MemCmp(&batchDrawRange.filterSettings, &batchDrawRange2.filterSettings, sizeof(BatchFilterSettings)) != 0)
				{
					if (batchDrawRange.drawCommandsCount != 0)
					{
						reference.drawRanges[drawRangeCount++] = batchDrawRange;
					}
					batchDrawRange = batchDrawRange2;
				}
				else
				{
					batchDrawRange.drawCommandsCount += batchDrawRange2.drawCommandsCount;
				}
			}
			if (batchDrawRange.drawCommandsCount != 0)
			{
				reference.drawRanges[drawRangeCount++] = batchDrawRange;
			}
			reference.drawCommandCount = num;
			reference.drawRangeCount = drawRangeCount;
		}
	}

	[BurstCompile]
	private struct BatchCullingJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		[ReadOnly]
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

		[ReadOnly]
		public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

		[ReadOnly]
		public BatchRenderFlags m_RequiredFlagMask;

		[ReadOnly]
		public BatchRenderFlags m_RenderFlagMask;

		[ReadOnly]
		public int m_MaxSplitBatchCount;

		[ReadOnly]
		public bool m_IsShadowCulling;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ActiveGroupData> m_ActiveGroupData;

		[ReadOnly]
		public NativeList<CullingSplitData> m_SplitData;

		[ReadOnly]
		public NativeList<FrustumPlanes.PlanePacket4> m_CullingPlanePackets;

		[NativeDisableParallelForRestriction]
		public BatchCullingOutput m_CullingOutput;

		public unsafe void Execute(int index)
		{
			int groupIndex = m_NativeBatchInstances.GetGroupIndex(index);
			GroupData groupData = m_NativeBatchGroups.GetGroupData(groupIndex);
			if ((groupData.m_RenderFlags & m_RequiredFlagMask) != m_RequiredFlagMask)
			{
				return;
			}
			ActiveGroupData activeGroupData = m_ActiveGroupData[index];
			NativeBatchAccessor<BatchData> batchAccessor = m_NativeBatchGroups.GetBatchAccessor(groupIndex);
			NativeCullingAccessor<CullingData> cullingAccessor = m_NativeBatchInstances.GetCullingAccessor(groupIndex);
			NativeSubBatchAccessor<BatchData> subBatchAccessor = m_NativeSubBatches.GetSubBatchAccessor(groupIndex);
			ref BatchCullingOutputDrawCommands reference = ref m_CullingOutput.drawCommands.ElementAt(0);
			float3 boundsCenter;
			float3 boundsExtents;
			if (m_IsShadowCulling)
			{
				m_NativeBatchInstances.GetShadowBounds(groupIndex, out boundsCenter, out boundsExtents);
			}
			else
			{
				m_NativeBatchInstances.GetBounds(groupIndex, out boundsCenter, out boundsExtents);
			}
			FrustumPlanes.PlanePacket4* unsafeReadOnlyPtr = m_CullingPlanePackets.GetUnsafeReadOnlyPtr();
			int length = m_CullingPlanePackets.Length;
			CullingSplitData* unsafeReadOnlyPtr2 = m_SplitData.GetUnsafeReadOnlyPtr();
			int length2 = m_SplitData.Length;
			int batchCount = m_NativeBatchGroups.GetBatchCount(groupIndex);
			int instanceCount = m_NativeBatchInstances.GetInstanceCount(groupIndex);
			int* ptr = reference.visibleInstances + activeGroupData.m_InstanceOffset;
			int** ptr2 = stackalloc int*[16];
			bool flag = length2 == 4 && m_IsShadowCulling;
			if (length2 == 1)
			{
				CullingSplitData cullingSplitData = *unsafeReadOnlyPtr2;
				for (int i = 0; i <= groupData.m_LodCount; i++)
				{
					int num = i * instanceCount;
					ptr2[i] = ptr + num;
				}
				switch (FrustumPlanes.CalculateIntersectResult(unsafeReadOnlyPtr, length, boundsCenter, boundsExtents))
				{
				case FrustumPlanes.IntersectResult.In:
				{
					for (int l = 0; l < instanceCount; l++)
					{
						int4 lodRange2 = cullingAccessor.Get(l).lodRange;
						lodRange2.xy = math.select(lodRange2.xy, lodRange2.zw, m_IsShadowCulling);
						for (int m = lodRange2.x; m < lodRange2.y; m++)
						{
							int** num3 = ptr2 + m;
							int* ptr3 = *num3;
							*num3 = ptr3 + 1;
							*ptr3 = l;
						}
					}
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
					for (int j = 0; j < instanceCount; j++)
					{
						CullingData cullingData = cullingAccessor.Get(j);
						float3 center = MathUtils.Center(cullingData.m_Bounds);
						float3 extents = MathUtils.Extents(cullingData.m_Bounds);
						if (FrustumPlanes.Intersect(unsafeReadOnlyPtr, length, center, extents))
						{
							int4 lodRange = cullingData.lodRange;
							lodRange.xy = math.select(lodRange.xy, lodRange.zw, m_IsShadowCulling);
							for (int k = lodRange.x; k < lodRange.y; k++)
							{
								int** num2 = ptr2 + k;
								int* ptr3 = *num2;
								*num2 = ptr3 + 1;
								*ptr3 = j;
							}
						}
					}
					break;
				}
				}
				int num4 = -1;
				int visibleOffset = 0;
				int num5 = 0;
				for (int n = 0; n < batchCount; n++)
				{
					BatchData batchData = batchAccessor.GetBatchData(n);
					int num6 = activeGroupData.m_BatchOffset + n;
					if (batchData.m_LodIndex != num4)
					{
						int num7 = groupData.m_LodCount - batchData.m_LodIndex;
						num4 = batchData.m_LodIndex;
						visibleOffset = num7 * instanceCount;
						num5 = (int)(ptr2[num7] - (ptr + visibleOffset));
						visibleOffset += activeGroupData.m_InstanceOffset;
					}
					if (num5 != 0 && (batchData.m_RenderFlags & m_RequiredFlagMask) == m_RequiredFlagMask)
					{
						batchAccessor.GetRenderData(n, out var meshID, out var materialID, out var subMeshIndex);
						BatchID batchID = subBatchAccessor.GetBatchID(n);
						BatchRenderFlags num8 = batchData.m_RenderFlags & m_RenderFlagMask;
						BatchDrawCommand batchDrawCommand = new BatchDrawCommand
						{
							visibleOffset = (uint)visibleOffset,
							visibleCount = (uint)num5,
							batchID = batchID,
							materialID = materialID,
							meshID = meshID,
							submeshIndex = (ushort)subMeshIndex,
							splitVisibilityMask = (ushort)cullingSplitData.m_SplitMask
						};
						BatchFilterSettings filterSettings = new BatchFilterSettings
						{
							layer = batchData.m_Layer,
							renderingLayerMask = uint.MaxValue,
							motionMode = MotionVectorGenerationMode.ForceNoMotion,
							receiveShadows = ((batchData.m_RenderFlags & BatchRenderFlags.ReceiveShadows) != 0),
							shadowCastingMode = (ShadowCastingMode)batchData.m_ShadowCastingMode
						};
						if ((num8 & BatchRenderFlags.MotionVectors) != 0)
						{
							batchDrawCommand.flags |= BatchDrawCommandFlags.HasMotion;
							filterSettings.motionMode = MotionVectorGenerationMode.Object;
						}
						reference.drawCommands[num6] = batchDrawCommand;
						reference.drawRanges[num6] = new BatchDrawRange
						{
							drawCommandsBegin = (uint)num6,
							drawCommandsCount = 1u,
							filterSettings = filterSettings
						};
					}
					else
					{
						reference.drawRanges[num6] = default(BatchDrawRange);
					}
				}
				return;
			}
			for (int num9 = 0; num9 <= groupData.m_LodCount; num9++)
			{
				int num10 = num9 * instanceCount;
				ptr2[num9] = ptr + num10;
			}
			FrustumPlanes.Intersect(unsafeReadOnlyPtr, length, boundsCenter, boundsExtents, out var inMask, out var outMask);
			int num11 = 0;
			ulong num12 = 0uL;
			int num13 = length2;
			int num14 = 0;
			for (int num15 = 0; num15 < length2; num15++)
			{
				CullingSplitData cullingSplitData2 = unsafeReadOnlyPtr2[num15];
				if ((cullingSplitData2.m_PlaneMask & outMask) == 0L)
				{
					if ((cullingSplitData2.m_PlaneMask & inMask) == cullingSplitData2.m_PlaneMask)
					{
						num11 |= cullingSplitData2.m_SplitMask;
						continue;
					}
					num12 |= cullingSplitData2.m_PlaneMask & ~inMask;
					num13 = math.min(num13, num15);
					num14 = num15;
				}
			}
			if (num12 != 0L)
			{
				int num16 = length;
				int num17 = 0;
				for (int num18 = 0; num18 < length; num18++)
				{
					if ((num12 & (ulong)(15L << (num18 << 2))) != 0L)
					{
						num16 = math.min(num16, num18);
						num17 = num18;
					}
				}
				FrustumPlanes.PlanePacket4* cullingPlanePackets = unsafeReadOnlyPtr + num16;
				int length3 = num17 - num16 + 1;
				int num19 = num16 << 2;
				if (num17 < 8)
				{
					for (int num20 = 0; num20 < instanceCount; num20++)
					{
						CullingData cullingData2 = cullingAccessor.Get(num20);
						int num21 = num11;
						float3 center2 = MathUtils.Center(cullingData2.m_Bounds);
						float3 extents2 = MathUtils.Extents(cullingData2.m_Bounds);
						FrustumPlanes.Intersect(cullingPlanePackets, length3, center2, extents2, out uint outMask2);
						outMask2 <<= num19;
						for (int num22 = num13; num22 <= num14; num22++)
						{
							CullingSplitData cullingSplitData3 = unsafeReadOnlyPtr2[num22];
							num21 |= math.select(0, cullingSplitData3.m_SplitMask, ((uint)(int)cullingSplitData3.m_PlaneMask & outMask2) == 0);
						}
						if (num21 != 0)
						{
							int num23 = (num21 << 24) | num20;
							int4 lodRange3 = cullingData2.lodRange;
							lodRange3.xy = math.select(lodRange3.xy, lodRange3.zw, m_IsShadowCulling);
							for (int num24 = lodRange3.x; num24 < lodRange3.y; num24++)
							{
								int** num25 = ptr2 + num24;
								int* ptr3 = *num25;
								*num25 = ptr3 + 1;
								*ptr3 = num23;
							}
						}
					}
				}
				else
				{
					for (int num26 = 0; num26 < instanceCount; num26++)
					{
						CullingData cullingData3 = cullingAccessor.Get(num26);
						int num27 = num11;
						float3 center3 = MathUtils.Center(cullingData3.m_Bounds);
						float3 extents3 = MathUtils.Extents(cullingData3.m_Bounds);
						FrustumPlanes.Intersect(cullingPlanePackets, length3, center3, extents3, out ulong outMask3);
						outMask3 <<= num19;
						for (int num28 = num13; num28 <= num14; num28++)
						{
							CullingSplitData cullingSplitData4 = unsafeReadOnlyPtr2[num28];
							num27 |= math.select(0, cullingSplitData4.m_SplitMask, (cullingSplitData4.m_PlaneMask & outMask3) == 0);
						}
						if (num27 != 0)
						{
							int num29 = (num27 << 24) | num26;
							int4 lodRange4 = cullingData3.lodRange;
							lodRange4.xy = math.select(lodRange4.xy, lodRange4.zw, m_IsShadowCulling);
							for (int num30 = lodRange4.x; num30 < lodRange4.y; num30++)
							{
								int** num31 = ptr2 + num30;
								int* ptr3 = *num31;
								*num31 = ptr3 + 1;
								*ptr3 = num29;
							}
						}
					}
				}
			}
			else if (num11 != 0)
			{
				for (int num32 = 0; num32 < instanceCount; num32++)
				{
					int4 lodRange5 = cullingAccessor.Get(num32).lodRange;
					lodRange5.xy = math.select(lodRange5.xy, lodRange5.zw, m_IsShadowCulling);
					for (int num33 = lodRange5.x; num33 < lodRange5.y; num33++)
					{
						int** num34 = ptr2 + num33;
						int* ptr3 = *num34;
						*num34 = ptr3 + 1;
						*ptr3 = num32;
					}
				}
			}
			int num35 = -1;
			int num36 = 0;
			int num37 = 0;
			int* ptr4 = stackalloc int[15];
			int* ptr5 = stackalloc int[15];
			int* ptr6 = stackalloc int[15];
			for (int num38 = 0; num38 < batchCount; num38++)
			{
				BatchData batchData2 = batchAccessor.GetBatchData(num38);
				int num39 = activeGroupData.m_BatchOffset + num38;
				int num40 = num39 * m_MaxSplitBatchCount;
				if (batchData2.m_LodIndex != num35)
				{
					int num41 = groupData.m_LodCount - batchData2.m_LodIndex;
					num35 = batchData2.m_LodIndex;
					int num42 = num41 * instanceCount;
					int* ptr7 = ptr + num42;
					num36 = (int)(ptr2[num41] - ptr7);
					num42 += activeGroupData.m_InstanceOffset;
					if (num36 != 0)
					{
						if (num12 != 0L)
						{
							if (num36 >= 3)
							{
								NativeSortExtension.Sort(ptr7, num36);
							}
							num37 = 0;
							int num43 = 0;
							while (num43 < num36)
							{
								int num44 = num43;
								int* ptr8 = ptr7 + num43++;
								int num45 = *ptr8 >>> 24;
								*ptr8 &= 16777215;
								if (num37 < m_MaxSplitBatchCount - 1)
								{
									for (; num43 < num36; num43++)
									{
										ptr8 = ptr7 + num43;
										if (*ptr8 >>> 24 != num45)
										{
											break;
										}
										*ptr8 &= 16777215;
									}
								}
								else
								{
									for (; num43 < num36; num43++)
									{
										ptr8 = ptr7 + num43;
										num45 |= *ptr8 >>> 24;
										*ptr8 &= 16777215;
									}
								}
								ptr4[num37] = num42 + num44;
								ptr5[num37] = num43 - num44;
								ptr6[num37] = num45;
								num37++;
							}
						}
						else
						{
							num37 = 1;
							*ptr4 = num42;
							*ptr5 = num36;
							*ptr6 = num11;
						}
					}
				}
				int num46 = (flag ? CalculateBatchMask(batchData2, unsafeReadOnlyPtr2, length2) : (-1));
				if (num36 != 0 && (batchData2.m_RenderFlags & m_RequiredFlagMask) == m_RequiredFlagMask)
				{
					batchAccessor.GetRenderData(num38, out var meshID2, out var materialID2, out var subMeshIndex2);
					BatchID batchID2 = subBatchAccessor.GetBatchID(num38);
					BatchRenderFlags num47 = batchData2.m_RenderFlags & m_RenderFlagMask;
					BatchDrawCommand batchDrawCommand2 = new BatchDrawCommand
					{
						batchID = batchID2,
						materialID = materialID2,
						meshID = meshID2,
						submeshIndex = (ushort)subMeshIndex2
					};
					BatchFilterSettings filterSettings2 = new BatchFilterSettings
					{
						layer = batchData2.m_Layer,
						renderingLayerMask = uint.MaxValue,
						motionMode = MotionVectorGenerationMode.ForceNoMotion,
						receiveShadows = ((batchData2.m_RenderFlags & BatchRenderFlags.ReceiveShadows) != 0),
						shadowCastingMode = (ShadowCastingMode)batchData2.m_ShadowCastingMode
					};
					if ((num47 & BatchRenderFlags.MotionVectors) != 0)
					{
						batchDrawCommand2.flags |= BatchDrawCommandFlags.HasMotion;
						filterSettings2.motionMode = MotionVectorGenerationMode.Object;
					}
					int num48 = 0;
					for (int num49 = 0; num49 < num37; num49++)
					{
						batchDrawCommand2.visibleOffset = (uint)ptr4[num49];
						batchDrawCommand2.visibleCount = (uint)ptr5[num49];
						batchDrawCommand2.splitVisibilityMask = (ushort)(ptr6[num49] & num46);
						if (batchDrawCommand2.splitVisibilityMask != 0)
						{
							reference.drawCommands[num40 + num48] = batchDrawCommand2;
							num48++;
						}
					}
					if (num48 > 0)
					{
						reference.drawRanges[num39] = new BatchDrawRange
						{
							drawCommandsBegin = (uint)num40,
							drawCommandsCount = (uint)num48,
							filterSettings = filterSettings2
						};
					}
					else
					{
						reference.drawRanges[num39] = default(BatchDrawRange);
					}
				}
				else
				{
					reference.drawRanges[num39] = default(BatchDrawRange);
				}
			}
		}

		private unsafe int CalculateBatchMask(BatchData batchData, CullingSplitData* cullSplitPtr, int length)
		{
			int num = 0;
			for (int i = 0; i < length; i++)
			{
				CullingSplitData cullingSplitData = cullSplitPtr[i];
				num |= ((!(batchData.m_ShadowArea < cullingSplitData.m_ShadowVolumeThreshold) && !(batchData.m_ShadowHeight < cullingSplitData.m_ShadowHeightThreshold)) ? (1 << i) : 0);
			}
			return num;
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

		public BufferLookup<BatchGroup> __Game_Prefabs_BatchGroup_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
			__Game_Prefabs_BatchGroup_RW_BufferLookup = state.GetBufferLookup<BatchGroup>();
		}
	}

	public const uint GPU_INSTANCE_MEMORY_DEFAULT = 67108864u;

	public const uint GPU_INSTANCE_MEMORY_INCREMENT = 16777216u;

	public const uint GPU_UPLOADER_CHUNK_SIZE = 2097152u;

	public const uint GPU_UPLOADER_OPERATION_SIZE = 65536u;

	public const int MAX_GROUP_BATCH_COUNT = 16;

	private RenderingSystem m_RenderingSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private BatchDataSystem m_BatchDataSystem;

	private TextureStreamingSystem m_TextureStreamingSystem;

	private PrefabSystem m_PrefabSystem;

	private NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

	private NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchInstances;

	private NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> m_NativeSubBatches;

	private ManagedBatches<OptionalProperties> m_ManagedBatches;

	private NativeList<PropertyData> m_MaterialProperties;

	private NativeList<PropertyData> m_ObjectProperties;

	private NativeList<PropertyData> m_NetProperties;

	private NativeList<PropertyData> m_LaneProperties;

	private NativeList<PropertyData> m_ZoneProperties;

	private NativeList<Entity> m_MergeMeshes;

	private NativeParallelMultiHashMap<Entity, int> m_MergeGroups;

	private EntityQuery m_MeshSettingsQuery;

	private bool m_LastMotionVectorsEnabled;

	private bool m_LastLodFadeEnabled;

	private bool m_PropertiesChanged;

	private bool m_MotionVectorsChanged;

	private bool m_LodFadeChanged;

	private bool m_VirtualTexturingChanged;

	private JobHandle m_NativeBatchGroupsReadDependencies;

	private JobHandle m_NativeBatchGroupsWriteDependencies;

	private JobHandle m_NativeBatchInstancesReadDependencies;

	private JobHandle m_NativeBatchInstancesWriteDependencies;

	private JobHandle m_NativeSubBatchesReadDependencies;

	private JobHandle m_NativeSubBatchesWriteDependencies;

	private JobHandle m_MergeDependencies;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
		m_TextureStreamingSystem = base.World.GetOrCreateSystemManaged<TextureStreamingSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_NativeBatchGroups = new NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData>(67108864u, 65536u, Allocator.Persistent);
		m_NativeBatchInstances = new NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>(m_NativeBatchGroups);
		m_NativeSubBatches = new NativeSubBatches<CullingData, GroupData, BatchData, InstanceData>(m_NativeBatchGroups);
		m_ManagedBatches = ManagedBatches<OptionalProperties>.Create(m_NativeBatchInstances, OnPerformCulling, 2097152u, new OptionalProperties(BatchFlags.MotionVectors, MeshType.Object));
		InitializeMaterialProperties<MaterialProperty>(out m_MaterialProperties);
		InitializeInstanceProperties<ObjectProperty>(out m_ObjectProperties, MeshType.Object);
		InitializeInstanceProperties<NetProperty>(out m_NetProperties, MeshType.Net);
		InitializeInstanceProperties<LaneProperty>(out m_LaneProperties, MeshType.Lane);
		InitializeInstanceProperties<ZoneProperty>(out m_ZoneProperties, MeshType.Zone);
		m_MergeMeshes = new NativeList<Entity>(10, Allocator.Persistent);
		m_MergeGroups = new NativeParallelMultiHashMap<Entity, int>(10, Allocator.Persistent);
		m_MeshSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<MeshSettingsData>());
		m_LastMotionVectorsEnabled = m_RenderingSystem.motionVectors;
		m_LastLodFadeEnabled = m_RenderingSystem.lodCrossFade;
	}

	private void InitializeMaterialProperties<T>(out NativeList<PropertyData> properties)
	{
		Array values = Enum.GetValues(typeof(T));
		properties = new NativeList<PropertyData>(values.Length, Allocator.Persistent);
		foreach (T item in values)
		{
			object[] customAttributes = typeof(T).GetMember(item.ToString())[0].GetCustomAttributes(typeof(MaterialPropertyAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				MaterialPropertyAttribute materialPropertyAttribute = (MaterialPropertyAttribute)customAttributes[0];
				PropertyData value = new PropertyData
				{
					m_NameID = Shader.PropertyToID(materialPropertyAttribute.ShaderPropertyName)
				};
				properties.Add(in value);
				m_ManagedBatches.RegisterMaterialPropertyType(materialPropertyAttribute.ShaderPropertyName, materialPropertyAttribute.DataType, overridenInBatch: false, default(MaterialPropertyDefaultValue), globalVariable: false, materialPropertyAttribute.IsBuiltin);
			}
		}
	}

	private void InitializeInstanceProperties<T>(out NativeList<PropertyData> properties, MeshType meshType)
	{
		Array values = Enum.GetValues(typeof(T));
		properties = new NativeList<PropertyData>(values.Length, Allocator.Persistent);
		foreach (T item in values)
		{
			object[] customAttributes = typeof(T).GetMember(item.ToString())[0].GetCustomAttributes(typeof(InstancePropertyAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				InstancePropertyAttribute instancePropertyAttribute = (InstancePropertyAttribute)customAttributes[0];
				PropertyData value = new PropertyData
				{
					m_NameID = Shader.PropertyToID(instancePropertyAttribute.ShaderPropertyName),
					m_DataIndex = instancePropertyAttribute.DataIndex
				};
				properties.Add(in value);
				m_ManagedBatches.RegisterMaterialPropertyType(instancePropertyAttribute.ShaderPropertyName, instancePropertyAttribute.DataType, overridenInBatch: true, default(MaterialPropertyDefaultValue), globalVariable: false, instancePropertyAttribute.IsBuiltin, new OptionalProperties(instancePropertyAttribute.RequiredFlags, meshType), new OptionalProperties((BatchFlags)0, meshType));
			}
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_NativeBatchGroupsReadDependencies.Complete();
		m_NativeBatchGroupsWriteDependencies.Complete();
		m_NativeBatchInstancesReadDependencies.Complete();
		m_NativeBatchInstancesWriteDependencies.Complete();
		m_NativeSubBatchesReadDependencies.Complete();
		m_NativeSubBatchesWriteDependencies.Complete();
		m_MergeDependencies.Complete();
		m_ManagedBatches.EndUpload(m_NativeBatchInstances);
		m_ManagedBatches.Dispose();
		m_NativeSubBatches.Dispose();
		m_NativeBatchInstances.Dispose();
		m_NativeBatchGroups.Dispose();
		m_MaterialProperties.Dispose();
		m_ObjectProperties.Dispose();
		m_NetProperties.Dispose();
		m_LaneProperties.Dispose();
		m_ZoneProperties.Dispose();
		m_MergeMeshes.Dispose();
		m_MergeGroups.Dispose();
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		m_NativeBatchGroupsReadDependencies.Complete();
		m_NativeBatchGroupsWriteDependencies.Complete();
		m_NativeBatchInstancesReadDependencies.Complete();
		m_NativeBatchInstancesWriteDependencies.Complete();
		m_NativeSubBatchesReadDependencies.Complete();
		m_NativeSubBatchesWriteDependencies.Complete();
		m_ManagedBatches.EndUpload(m_NativeBatchInstances);
		int groupCount = m_NativeBatchGroups.GetGroupCount();
		for (int i = 0; i < groupCount; i++)
		{
			if (m_NativeBatchGroups.IsValidGroup(i))
			{
				m_NativeBatchInstances.RemoveInstances(i, m_NativeSubBatches);
			}
		}
	}

	public bool CheckPropertyUpdates()
	{
		bool motionVectors = m_RenderingSystem.motionVectors;
		if (motionVectors != m_LastMotionVectorsEnabled)
		{
			m_LastMotionVectorsEnabled = motionVectors;
			m_MotionVectorsChanged = true;
		}
		bool lodCrossFade = m_RenderingSystem.lodCrossFade;
		if (lodCrossFade != m_LastLodFadeEnabled)
		{
			m_LastLodFadeEnabled = lodCrossFade;
			m_LodFadeChanged = true;
		}
		if (!m_PropertiesChanged && !m_MotionVectorsChanged && !m_LodFadeChanged)
		{
			return m_VirtualTexturingChanged;
		}
		return true;
	}

	public void VirtualTexturingUpdated()
	{
		m_VirtualTexturingChanged = true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_PropertiesChanged || m_MotionVectorsChanged || m_LodFadeChanged || m_VirtualTexturingChanged)
		{
			try
			{
				RefreshProperties(m_PropertiesChanged, m_MotionVectorsChanged, m_LodFadeChanged, m_VirtualTexturingChanged);
			}
			finally
			{
				m_PropertiesChanged = false;
				m_MotionVectorsChanged = false;
				m_LodFadeChanged = false;
				m_VirtualTexturingChanged = false;
			}
		}
		m_MergeDependencies.Complete();
		if (m_MergeMeshes.Length != 0)
		{
			MergeGroups();
		}
		JobHandle dependencies;
		JobHandle dependencies2;
		AllocateBuffersJob jobData = new AllocateBuffersJob
		{
			m_ObjectProperties = m_ObjectProperties,
			m_NetProperties = m_NetProperties,
			m_LaneProperties = m_LaneProperties,
			m_ZoneProperties = m_ZoneProperties,
			m_NativeBatchGroups = GetNativeBatchGroups(readOnly: false, out dependencies),
			m_NativeBatchInstances = GetNativeBatchInstances(readOnly: false, out dependencies2)
		};
		JobHandle dependencies3;
		GenerateSubBatchesJob jobData2 = new GenerateSubBatchesJob
		{
			m_NativeSubBatches = GetNativeSubBatches(readOnly: false, out dependencies3)
		};
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(dependencies, dependencies2));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, dependencies3);
		AddNativeBatchGroupsWriter(jobHandle);
		AddNativeBatchInstancesWriter(jobHandle);
		AddNativeSubBatchesWriter(jobHandle2);
	}

	public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> GetNativeBatchGroups(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_NativeBatchGroupsWriteDependencies : JobHandle.CombineDependencies(m_NativeBatchGroupsReadDependencies, m_NativeBatchGroupsWriteDependencies));
		return m_NativeBatchGroups;
	}

	public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> GetNativeBatchInstances(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_NativeBatchInstancesWriteDependencies : JobHandle.CombineDependencies(m_NativeBatchInstancesReadDependencies, m_NativeBatchInstancesWriteDependencies));
		return m_NativeBatchInstances;
	}

	public NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> GetNativeSubBatches(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_NativeSubBatchesWriteDependencies : JobHandle.CombineDependencies(m_NativeSubBatchesReadDependencies, m_NativeSubBatchesWriteDependencies));
		return m_NativeSubBatches;
	}

	public ManagedBatches<OptionalProperties> GetManagedBatches()
	{
		return m_ManagedBatches;
	}

	public bool IsMotionVectorsEnabled()
	{
		return m_LastMotionVectorsEnabled;
	}

	public bool IsLodFadeEnabled()
	{
		return m_LastLodFadeEnabled;
	}

	public void AddNativeBatchGroupsReader(JobHandle jobHandle)
	{
		m_NativeBatchGroupsReadDependencies = JobHandle.CombineDependencies(m_NativeBatchGroupsReadDependencies, jobHandle);
	}

	public void AddNativeBatchGroupsWriter(JobHandle jobHandle)
	{
		m_NativeBatchGroupsWriteDependencies = jobHandle;
	}

	public void AddNativeBatchInstancesReader(JobHandle jobHandle)
	{
		m_NativeBatchInstancesReadDependencies = JobHandle.CombineDependencies(m_NativeBatchInstancesReadDependencies, jobHandle);
	}

	public void AddNativeBatchInstancesWriter(JobHandle jobHandle)
	{
		m_NativeBatchInstancesWriteDependencies = jobHandle;
	}

	public void AddNativeSubBatchesReader(JobHandle jobHandle)
	{
		m_NativeSubBatchesReadDependencies = JobHandle.CombineDependencies(m_NativeSubBatchesReadDependencies, jobHandle);
	}

	public void AddNativeSubBatchesWriter(JobHandle jobHandle)
	{
		m_NativeSubBatchesWriteDependencies = jobHandle;
	}

	public void MergeGroups(Entity meshEntity, int mergeIndex)
	{
		m_MergeDependencies.Complete();
		if (!m_MergeGroups.ContainsKey(meshEntity))
		{
			m_MergeMeshes.Add(in meshEntity);
		}
		m_MergeGroups.Add(meshEntity, mergeIndex);
	}

	public (int, int) GetVTTextureParamBlockID(int stackConfigIndex)
	{
		return stackConfigIndex switch
		{
			0 => (m_MaterialProperties[0].m_NameID, m_MaterialProperties[2].m_NameID), 
			1 => (m_MaterialProperties[1].m_NameID, m_MaterialProperties[3].m_NameID), 
			_ => throw new IndexOutOfRangeException("stackConfigIndex cannot be greated than 2"), 
		};
	}

	public PropertyData GetPropertyData(MaterialProperty property)
	{
		return m_MaterialProperties[(int)property];
	}

	public PropertyData GetPropertyData(ObjectProperty property)
	{
		return m_ObjectProperties[(int)property];
	}

	public PropertyData GetPropertyData(NetProperty property)
	{
		return m_NetProperties[(int)property];
	}

	public PropertyData GetPropertyData(LaneProperty property)
	{
		return m_LaneProperties[(int)property];
	}

	public PropertyData GetPropertyData(ZoneProperty property)
	{
		return m_ZoneProperties[(int)property];
	}

	private void RefreshProperties(bool propertiesChanged, bool motionVectorsChanged, bool lodFadeChanged, bool virtualTexturingChanged)
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = GetNativeBatchGroups(readOnly: false, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = GetNativeBatchInstances(readOnly: false, out dependencies2);
		JobHandle dependencies3;
		NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = GetNativeSubBatches(readOnly: false, out dependencies3);
		dependencies.Complete();
		dependencies2.Complete();
		dependencies3.Complete();
		int groupCount = nativeBatchGroups.GetGroupCount();
		bool flag = false;
		Dictionary<BatchPropertiesKey<OptionalProperties>, bool> dictionary = null;
		if (propertiesChanged)
		{
			dictionary = new Dictionary<BatchPropertiesKey<OptionalProperties>, bool>();
		}
		MeshSettingsData meshSettingsData = default(MeshSettingsData);
		if (!m_MeshSettingsQuery.IsEmptyIgnoreFilter)
		{
			meshSettingsData = m_MeshSettingsQuery.GetSingleton<MeshSettingsData>();
		}
		for (int i = 0; i < groupCount; i++)
		{
			if (!nativeBatchGroups.IsValidGroup(i))
			{
				continue;
			}
			int batchCount = nativeBatchGroups.GetBatchCount(i);
			GroupData groupData = nativeBatchGroups.GetGroupData(i);
			for (int j = 0; j < batchCount; j++)
			{
				int managedBatchIndex = nativeBatchGroups.GetManagedBatchIndex(i, j);
				if (managedBatchIndex < 0)
				{
					continue;
				}
				CustomBatch customBatch = (CustomBatch)m_ManagedBatches.GetBatch(managedBatchIndex);
				BatchFlags batchFlags = customBatch.sourceFlags;
				if (!IsMotionVectorsEnabled())
				{
					batchFlags &= ~BatchFlags.MotionVectors;
				}
				if (!IsLodFadeEnabled())
				{
					batchFlags &= ~BatchFlags.LodFade;
				}
				OptionalProperties optionalProperties = new OptionalProperties(batchFlags, customBatch.sourceType);
				bool flag2 = ((customBatch.sourceFlags & BatchFlags.MotionVectors) != 0 && motionVectorsChanged) || ((customBatch.sourceFlags & BatchFlags.LodFade) != 0 && lodFadeChanged);
				if ((customBatch.sourceType & (MeshType.Net | MeshType.Zone)) == 0)
				{
					RenderPrefab renderPrefab = m_PrefabSystem.GetPrefab<RenderPrefab>(customBatch.sourceMeshEntity);
					if (virtualTexturingChanged)
					{
						DecalProperties decalProperties = renderPrefab.GetComponent<DecalProperties>();
						if (decalProperties != null && groupData.m_Layer == MeshLayer.Outline)
						{
							decalProperties = null;
						}
						VTAtlassingInfo[] array = customBatch.sourceSurface.VTAtlassingInfos;
						if (array == null)
						{
							array = customBatch.sourceSurface.PreReservedAtlassingInfos;
						}
						if (array != null)
						{
							if (decalProperties != null || renderPrefab.manualVTRequired || renderPrefab.isImpostor)
							{
								BatchData batchData = nativeBatchGroups.GetBatchData(i, j);
								Bounds2 bounds = MathUtils.Bounds(new float2(0f, 0f), new float2(1f, 1f));
								batchData.m_VTIndex0 = -1;
								batchData.m_VTIndex1 = -1;
								if (decalProperties != null)
								{
									bounds = MathUtils.Bounds(decalProperties.m_TextureArea.min, decalProperties.m_TextureArea.max);
								}
								if (array.Length >= 1 && array[0].indexInStack >= 0)
								{
									batchData.m_VTIndex0 = m_ManagedBatchSystem.VTTextureRequester.RegisterTexture(0, array[0].stackGlobalIndex, array[0].indexInStack, bounds);
								}
								if (array.Length >= 2 && array[1].indexInStack >= 0)
								{
									batchData.m_VTIndex1 = m_ManagedBatchSystem.VTTextureRequester.RegisterTexture(1, array[1].stackGlobalIndex, array[1].indexInStack, bounds);
								}
								nativeBatchGroups.SetBatchData(i, j, batchData);
							}
							if (!renderPrefab.Has<DefaultMesh>())
							{
								for (int k = 0; k < 2; k++)
								{
									if (array.Length > k && array[k].indexInStack >= 0)
									{
										customBatch.customProps.SetTextureParamBlock(GetVTTextureParamBlockID(k), m_TextureStreamingSystem.GetTextureParamBlock(array[k]));
										flag2 = true;
									}
								}
							}
						}
					}
					if (customBatch.generatedType == GeneratedType.ObjectBase)
					{
						BaseProperties component = renderPrefab.GetComponent<BaseProperties>();
						if (component == null && (customBatch.sourceFlags & BatchFlags.Lod) != 0)
						{
							renderPrefab = m_PrefabSystem.GetPrefab<RenderPrefab>(groupData.m_Mesh);
							component = renderPrefab.GetComponent<BaseProperties>();
						}
						renderPrefab = ((!(component != null)) ? m_PrefabSystem.GetPrefab<RenderPrefab>(meshSettingsData.m_DefaultBaseMesh) : component.m_BaseType);
					}
					m_ManagedBatchSystem.SetupVT(renderPrefab, customBatch.material, customBatch.sourceSubMeshIndex);
				}
				if (propertiesChanged)
				{
					BatchPropertiesKey<OptionalProperties> key = new BatchPropertiesKey<OptionalProperties>(customBatch.material.shader, optionalProperties);
					if (!dictionary.TryGetValue(key, out var value))
					{
						value = m_ManagedBatches.RegenerateBatchProperties(customBatch.material.shader, optionalProperties);
						dictionary.Add(key, value);
					}
					flag2 = flag2 || value;
				}
				if (flag2)
				{
					NativeBatchProperties batchProperties = m_ManagedBatches.GetBatchProperties(customBatch.material.shader, optionalProperties);
					nativeBatchGroups.SetBatchProperties(i, j, batchProperties);
					nativeSubBatches.RecreateRenderers(i, j);
					WriteableBatchDefaultsAccessor batchDefaultsAccessor = nativeBatchGroups.GetBatchDefaultsAccessor(i, j);
					if (customBatch.sourceSurface != null)
					{
						m_ManagedBatches.SetDefaults(ManagedBatchSystem.GetTemplate(customBatch.sourceSurface), customBatch.sourceSurface.floats, customBatch.sourceSurface.ints, customBatch.sourceSurface.vectors, customBatch.sourceSurface.colors, customBatch.customProps, batchProperties, batchDefaultsAccessor);
					}
					else
					{
						m_ManagedBatches.SetDefaults(customBatch.sourceMaterial, customBatch.customProps, batchProperties, batchDefaultsAccessor);
					}
					flag |= nativeBatchInstances.GetInstanceCount(i) != 0;
				}
			}
		}
		if (flag)
		{
			m_BatchDataSystem.InstancePropertiesUpdated();
			if (IsLodFadeEnabled())
			{
				JobHandle jobHandle = IJobParallelForExtensions.Schedule(new InitializeLodFadeJob
				{
					m_NativeBatchInstances = nativeBatchInstances.AsParallelInstanceWriter()
				}, nativeBatchInstances.GetActiveGroupCount(), 1);
				AddNativeBatchInstancesWriter(jobHandle);
			}
		}
	}

	private void MergeGroups()
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = GetNativeBatchGroups(readOnly: false, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = GetNativeBatchInstances(readOnly: false, out dependencies2);
		JobHandle dependencies3;
		NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = GetNativeSubBatches(readOnly: false, out dependencies3);
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData>.GroupUpdater groupUpdater = nativeBatchGroups.BeginGroupUpdate(Allocator.TempJob);
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.InstanceUpdater instanceUpdater = nativeBatchInstances.BeginInstanceUpdate(Allocator.TempJob);
		MergeGroupsJob jobData = new MergeGroupsJob
		{
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_BatchGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BatchGroup_RW_BufferLookup, ref base.CheckedStateRef),
			m_MergeMeshes = m_MergeMeshes,
			m_MergeGroups = m_MergeGroups,
			m_BatchGroupUpdater = groupUpdater.AsParallel(int.MaxValue),
			m_BatchInstanceUpdater = instanceUpdater.AsParallel(int.MaxValue)
		};
		MergeCleanupJob jobData2 = new MergeCleanupJob
		{
			m_MergeMeshes = m_MergeMeshes,
			m_MergeGroups = m_MergeGroups
		};
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, m_MergeMeshes.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		JobHandle mergeDependencies = IJobExtensions.Schedule(jobData2, jobHandle);
		JobHandle jobHandle2 = nativeBatchGroups.EndGroupUpdate(groupUpdater, jobHandle);
		JobHandle jobHandle3 = nativeBatchInstances.EndInstanceUpdate(instanceUpdater, JobHandle.CombineDependencies(jobHandle, dependencies3), nativeSubBatches);
		AddNativeBatchGroupsWriter(jobHandle2);
		AddNativeBatchInstancesWriter(jobHandle3);
		AddNativeSubBatchesWriter(jobHandle3);
		base.Dependency = jobHandle;
		m_MergeDependencies = mergeDependencies;
	}

	private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = GetNativeBatchGroups(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = GetNativeBatchInstances(readOnly: true, out dependencies2);
		JobHandle dependencies3;
		NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = GetNativeSubBatches(readOnly: true, out dependencies3);
		dependencies2.Complete();
		int activeGroupCount = nativeBatchInstances.GetActiveGroupCount();
		int maxSplitBatchCount = (cullingContext.cullingSplits.Length << 1) - 1;
		NativeArray<ActiveGroupData> activeGroupData = new NativeArray<ActiveGroupData>(activeGroupCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		NativeList<CullingSplitData> splitData = new NativeList<CullingSplitData>(cullingContext.cullingSplits.Length, Allocator.TempJob);
		NativeList<FrustumPlanes.PlanePacket4> nativeList = new NativeList<FrustumPlanes.PlanePacket4>(FrustumPlanes.GetPacketCount(cullingContext.cullingPlanes.Length), Allocator.TempJob);
		BatchRenderFlags batchRenderFlags = BatchRenderFlags.IsEnabled;
		BatchRenderFlags batchRenderFlags2 = BatchRenderFlags.All;
		if (cullingContext.viewType == BatchCullingViewType.Light)
		{
			batchRenderFlags |= BatchRenderFlags.CastShadows;
		}
		if (!IsMotionVectorsEnabled())
		{
			batchRenderFlags2 &= ~BatchRenderFlags.MotionVectors;
		}
		AllocateCullingJob jobData = new AllocateCullingJob
		{
			m_NativeBatchGroups = nativeBatchGroups,
			m_NativeBatchInstances = nativeBatchInstances,
			m_RequiredFlagMask = batchRenderFlags,
			m_MaxSplitBatchCount = maxSplitBatchCount,
			m_CullingOutput = cullingOutput,
			m_ActiveGroupData = activeGroupData
		};
		bool flag = cullingContext.projectionType == BatchCullingProjectionType.Orthographic && cullingContext.viewType == BatchCullingViewType.Light && cullingContext.cullingSplits.Length == 4;
		CullingPlanesJob jobData2 = new CullingPlanesJob
		{
			m_CullingPlanes = cullingContext.cullingPlanes,
			m_CullingSplits = cullingContext.cullingSplits,
			m_ShadowCullingData = (flag ? m_RenderingSystem.GetShadowCullingData() : float3.zero),
			m_SplitData = splitData,
			m_PlanePackets = nativeList
		};
		BatchCullingJob jobData3 = new BatchCullingJob
		{
			m_NativeBatchGroups = nativeBatchGroups,
			m_NativeBatchInstances = nativeBatchInstances,
			m_NativeSubBatches = nativeSubBatches,
			m_RequiredFlagMask = batchRenderFlags,
			m_RenderFlagMask = batchRenderFlags2,
			m_MaxSplitBatchCount = maxSplitBatchCount,
			m_IsShadowCulling = (cullingContext.viewType == BatchCullingViewType.Light),
			m_ActiveGroupData = activeGroupData,
			m_SplitData = splitData,
			m_CullingPlanePackets = nativeList,
			m_CullingOutput = cullingOutput
		};
		FinalizeCullingJob jobData4 = new FinalizeCullingJob
		{
			m_CullingOutput = cullingOutput
		};
		JobHandle job = IJobExtensions.Schedule(jobData, dependencies);
		JobHandle job2 = IJobExtensions.Schedule(jobData2);
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData3, activeGroupCount, 1, JobHandle.CombineDependencies(job, job2, dependencies3));
		JobHandle result = IJobExtensions.Schedule(jobData4, jobHandle);
		splitData.Dispose(jobHandle);
		nativeList.Dispose(jobHandle);
		AddNativeBatchInstancesReader(jobHandle);
		AddNativeBatchGroupsReader(jobHandle);
		AddNativeSubBatchesReader(jobHandle);
		return result;
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
	public BatchManagerSystem()
	{
	}
}
