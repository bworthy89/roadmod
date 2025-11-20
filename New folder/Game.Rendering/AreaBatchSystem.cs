using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AreaBatchSystem : GameSystemBase, IPreDeserialize
{
	private class ManagedBatchData
	{
		public GraphicsBuffer m_VisibleIndices;

		public Material m_Material;

		public int m_RendererPriority;
	}

	private struct NativeBatchData
	{
		public UnsafeList<AreaMetaData> m_AreaMetaData;

		public UnsafeList<int> m_VisibleIndices;

		public Bounds3 m_Bounds;

		public Entity m_Prefab;

		public bool m_VisibleUpdated;

		public bool m_BoundsUpdated;

		public bool m_VisibleIndicesUpdated;

		public bool m_IsEnabled;
	}

	[BurstCompile]
	private struct TreeCullingJob1 : IJob
	{
		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float4 m_PrevLodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_PrevCameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float3 m_PrevCameraDirection;

		[ReadOnly]
		public BoundsMask m_VisibleMask;

		[ReadOnly]
		public BoundsMask m_PrevVisibleMask;

		public NativeArray<int> m_NodeBuffer;

		public NativeArray<int> m_SubDataBuffer;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<CullingAction>.Writer m_ActionQueue;

		public void Execute()
		{
			TreeCullingIterator iterator = new TreeCullingIterator
			{
				m_LodParameters = m_LodParameters,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = m_CameraPosition,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = m_CameraDirection,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_VisibleMask = m_VisibleMask,
				m_PrevVisibleMask = m_PrevVisibleMask,
				m_ActionQueue = m_ActionQueue
			};
			m_AreaSearchTree.Iterate(ref iterator, 3, m_NodeBuffer, m_SubDataBuffer);
		}
	}

	[BurstCompile]
	private struct TreeCullingJob2 : IJobParallelFor
	{
		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float4 m_PrevLodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_PrevCameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float3 m_PrevCameraDirection;

		[ReadOnly]
		public BoundsMask m_VisibleMask;

		[ReadOnly]
		public BoundsMask m_PrevVisibleMask;

		[ReadOnly]
		public NativeArray<int> m_NodeBuffer;

		[ReadOnly]
		public NativeArray<int> m_SubDataBuffer;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<CullingAction>.Writer m_ActionQueue;

		public void Execute(int index)
		{
			TreeCullingIterator iterator = new TreeCullingIterator
			{
				m_LodParameters = m_LodParameters,
				m_PrevLodParameters = m_PrevLodParameters,
				m_CameraPosition = m_CameraPosition,
				m_PrevCameraPosition = m_PrevCameraPosition,
				m_CameraDirection = m_CameraDirection,
				m_PrevCameraDirection = m_PrevCameraDirection,
				m_VisibleMask = m_VisibleMask,
				m_PrevVisibleMask = m_PrevVisibleMask,
				m_ActionQueue = m_ActionQueue
			};
			m_AreaSearchTree.Iterate(ref iterator, m_SubDataBuffer[index], m_NodeBuffer[index]);
		}
	}

	private struct TreeCullingIterator : INativeQuadTreeIteratorWithSubData<AreaSearchItem, QuadTreeBoundsXZ, int>, IUnsafeQuadTreeIteratorWithSubData<AreaSearchItem, QuadTreeBoundsXZ, int>
	{
		public float4 m_LodParameters;

		public float3 m_CameraPosition;

		public float3 m_CameraDirection;

		public float3 m_PrevCameraPosition;

		public float4 m_PrevLodParameters;

		public float3 m_PrevCameraDirection;

		public BoundsMask m_VisibleMask;

		public BoundsMask m_PrevVisibleMask;

		public NativeParallelQueue<CullingAction>.Writer m_ActionQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds, ref int subData)
		{
			switch (subData)
			{
			case 1:
			{
				BoundsMask boundsMask4 = m_VisibleMask & bounds.m_Mask;
				float num13 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num14 = RenderingUtils.CalculateLod(num13 * num13, m_LodParameters);
				if (boundsMask4 == (BoundsMask)0 || num14 < bounds.m_MinLod)
				{
					return false;
				}
				float num15 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num16 = RenderingUtils.CalculateLod(num15 * num15, m_PrevLodParameters);
				if (((uint)boundsMask4 & (uint)(ushort)(~(int)m_PrevVisibleMask)) == 0)
				{
					if (num16 < bounds.m_MaxLod)
					{
						return num14 > num16;
					}
					return false;
				}
				return true;
			}
			case 2:
			{
				BoundsMask boundsMask3 = m_PrevVisibleMask & bounds.m_Mask;
				float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num10 = RenderingUtils.CalculateLod(num9 * num9, m_PrevLodParameters);
				if (boundsMask3 == (BoundsMask)0 || num10 < bounds.m_MinLod)
				{
					return false;
				}
				float num11 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num12 = RenderingUtils.CalculateLod(num11 * num11, m_LodParameters);
				if (((uint)boundsMask3 & (uint)(ushort)(~(int)m_VisibleMask)) == 0)
				{
					if (num12 < bounds.m_MaxLod)
					{
						return num10 > num12;
					}
					return false;
				}
				return true;
			}
			default:
			{
				BoundsMask boundsMask = m_VisibleMask & bounds.m_Mask;
				BoundsMask boundsMask2 = m_PrevVisibleMask & bounds.m_Mask;
				float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num3 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
				int num4 = RenderingUtils.CalculateLod(num2 * num2, m_PrevLodParameters);
				subData = 0;
				if (boundsMask != 0 && num3 >= bounds.m_MinLod)
				{
					float num5 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
					int num6 = RenderingUtils.CalculateLod(num5 * num5, m_PrevLodParameters);
					subData |= math.select(0, 1, ((uint)boundsMask & (uint)(ushort)(~(int)m_PrevVisibleMask)) != 0 || (num6 < bounds.m_MaxLod && num3 > num6));
				}
				if (boundsMask2 != 0 && num4 >= bounds.m_MinLod)
				{
					float num7 = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					int num8 = RenderingUtils.CalculateLod(num7 * num7, m_LodParameters);
					subData |= math.select(0, 2, ((uint)boundsMask2 & (uint)(ushort)(~(int)m_VisibleMask)) != 0 || (num8 < bounds.m_MaxLod && num4 > num8));
				}
				return subData != 0;
			}
			}
		}

		public void Iterate(QuadTreeBoundsXZ bounds, int subData, AreaSearchItem item)
		{
			switch (subData)
			{
			case 1:
			{
				float num5 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				int num6 = RenderingUtils.CalculateLod(num5 * num5, m_LodParameters);
				if ((m_VisibleMask & bounds.m_Mask) != 0 && num6 >= bounds.m_MinLod)
				{
					float num7 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
					int num8 = RenderingUtils.CalculateLod(num7 * num7, m_PrevLodParameters);
					if ((m_PrevVisibleMask & bounds.m_Mask) == 0 || num8 < bounds.m_MaxLod)
					{
						m_ActionQueue.Enqueue(new CullingAction
						{
							m_Item = item,
							m_PassedCulling = true
						});
					}
				}
				break;
			}
			case 2:
			{
				float num9 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num10 = RenderingUtils.CalculateLod(num9 * num9, m_PrevLodParameters);
				if ((m_PrevVisibleMask & bounds.m_Mask) != 0 && num10 >= bounds.m_MinLod)
				{
					float num11 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
					int num12 = RenderingUtils.CalculateLod(num11 * num11, m_LodParameters);
					if ((m_VisibleMask & bounds.m_Mask) == 0 || num12 < bounds.m_MaxLod)
					{
						m_ActionQueue.Enqueue(new CullingAction
						{
							m_Item = item
						});
					}
				}
				break;
			}
			default:
			{
				float num = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_CameraPosition, m_CameraDirection, m_LodParameters);
				float num2 = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, m_PrevCameraPosition, m_PrevCameraDirection, m_PrevLodParameters);
				int num3 = RenderingUtils.CalculateLod(num * num, m_LodParameters);
				int num4 = RenderingUtils.CalculateLod(num2 * num2, m_PrevLodParameters);
				bool flag = (m_VisibleMask & bounds.m_Mask) != 0 && num3 >= bounds.m_MinLod;
				bool flag2 = (m_PrevVisibleMask & bounds.m_Mask) != 0 && num4 >= bounds.m_MaxLod;
				if (flag != flag2)
				{
					m_ActionQueue.Enqueue(new CullingAction
					{
						m_Item = item,
						m_PassedCulling = flag
					});
				}
				break;
			}
			}
		}
	}

	[BurstCompile]
	private struct QueryCullingJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Batch> m_BatchType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public BoundsMask m_VisibleMask;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelQueue<CullingAction>.Writer m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Batch> nativeArray2 = chunk.GetNativeArray(ref m_BatchType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity area = nativeArray[i];
					if (nativeArray2[i].m_AllocatedSize != 0)
					{
						m_ActionQueue.Enqueue(new CullingAction
						{
							m_Item = new AreaSearchItem(area, -1)
						});
					}
				}
				return;
			}
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
			BoundsMask boundsMask = BoundsMask.Debug | BoundsMask.NormalLayers | BoundsMask.NotOverridden | BoundsMask.NotWalkThrough;
			for (int j = 0; j < chunk.Count; j++)
			{
				Entity area2 = nativeArray[j];
				if (nativeArray2[j].m_AllocatedSize != 0)
				{
					m_ActionQueue.Enqueue(new CullingAction
					{
						m_Item = new AreaSearchItem(area2, -1)
					});
				}
				if ((m_VisibleMask & boundsMask) == 0)
				{
					continue;
				}
				PrefabRef prefabRef = nativeArray3[j];
				DynamicBuffer<Node> nodes = bufferAccessor[j];
				DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor2[j];
				AreaGeometryData areaData = m_PrefabAreaGeometryData[prefabRef.m_Prefab];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Triangle triangle = dynamicBuffer[k];
					Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
					float num = RenderingUtils.CalculateMinDistance(AreaUtils.GetBounds(triangle, triangle2, areaData), m_CameraPosition, m_CameraDirection, m_LodParameters);
					if (RenderingUtils.CalculateLod(num * num, m_LodParameters) >= triangle.m_MinLod)
					{
						m_ActionQueue.Enqueue(new CullingAction
						{
							m_Item = new AreaSearchItem(area2, k),
							m_PassedCulling = true
						});
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct AreaMetaData
	{
		public Entity m_Entity;

		public Bounds3 m_Bounds;

		public int m_StartIndex;

		public int m_VisibleCount;
	}

	private struct TriangleMetaData
	{
		public int m_Index;

		public bool m_IsVisible;
	}

	private struct TriangleSortData : IComparable<TriangleSortData>
	{
		public int m_Index;

		public int m_MinLod;

		public int CompareTo(TriangleSortData other)
		{
			return m_MinLod - other.m_MinLod;
		}
	}

	private struct CullingAction
	{
		public AreaSearchItem m_Item;

		public bool m_PassedCulling;

		public override int GetHashCode()
		{
			return m_Item.m_Area.GetHashCode();
		}
	}

	private struct AllocationAction
	{
		public Entity m_Entity;

		public int m_TriangleCount;
	}

	[BurstCompile]
	private struct CullingActionJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RenderedAreaData> m_PrefabRenderedAreaData;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public NativeParallelQueue<CullingAction>.Reader m_CullingActions;

		public NativeQueue<AllocationAction>.ParallelWriter m_AllocationActions;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Batch> m_BatchData;

		[NativeDisableParallelForRestriction]
		public NativeList<TriangleMetaData> m_TriangleMetaData;

		public void Execute(int index)
		{
			NativeParallelQueue<CullingAction>.Enumerator enumerator = m_CullingActions.GetEnumerator(index);
			while (enumerator.MoveNext())
			{
				CullingAction current = enumerator.Current;
				if (current.m_PassedCulling)
				{
					PassedCulling(current);
				}
				else
				{
					FailedCulling(current);
				}
			}
			enumerator.Dispose();
		}

		private void PassedCulling(CullingAction cullingAction)
		{
			ref Batch valueRW = ref m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
			if (valueRW.m_VisibleCount == 0)
			{
				valueRW.m_VisibleCount = -1;
				PrefabRef prefabRef = m_PrefabRefData[cullingAction.m_Item.m_Area];
				if (m_PrefabRenderedAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					valueRW.m_BatchIndex = componentData.m_BatchIndex;
					m_AllocationActions.Enqueue(new AllocationAction
					{
						m_Entity = cullingAction.m_Item.m_Area,
						m_TriangleCount = m_Triangles[cullingAction.m_Item.m_Area].Length
					});
				}
				else
				{
					valueRW.m_BatchIndex = -1;
				}
			}
		}

		private void FailedCulling(CullingAction cullingAction)
		{
			ref Batch valueRW = ref m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
			if (valueRW.m_AllocatedSize == 0)
			{
				return;
			}
			if (cullingAction.m_Item.m_Triangle == -1)
			{
				if (valueRW.m_VisibleCount > 0)
				{
					for (int i = 0; i < valueRW.m_AllocatedSize; i++)
					{
						m_TriangleMetaData.ElementAt((int)valueRW.m_BatchAllocation.Begin + i).m_IsVisible = false;
					}
					valueRW.m_VisibleCount = 0;
					m_AllocationActions.Enqueue(new AllocationAction
					{
						m_Entity = cullingAction.m_Item.m_Area
					});
				}
				return;
			}
			ref TriangleMetaData reference = ref m_TriangleMetaData.ElementAt((int)valueRW.m_BatchAllocation.Begin + cullingAction.m_Item.m_Triangle);
			if (reference.m_IsVisible)
			{
				reference.m_IsVisible = false;
				if (--valueRW.m_VisibleCount == 0)
				{
					m_AllocationActions.Enqueue(new AllocationAction
					{
						m_Entity = cullingAction.m_Item.m_Area
					});
				}
			}
		}
	}

	[BurstCompile]
	private struct BatchAllocationJob : IJob
	{
		public ComponentLookup<Batch> m_BatchData;

		public NativeList<NativeBatchData> m_NativeBatchData;

		public NativeList<TriangleMetaData> m_TriangleMetaData;

		public NativeList<AreaTriangleData> m_AreaTriangleData;

		public NativeList<AreaColorData> m_AreaColorData;

		public NativeList<NativeHeapBlock> m_UpdatedTriangles;

		public NativeQueue<AllocationAction> m_AllocationActions;

		public NativeHeapAllocator m_AreaBufferAllocator;

		public NativeReference<int> m_AllocationCount;

		public void Execute()
		{
			AllocationAction item;
			while (m_AllocationActions.TryDequeue(out item))
			{
				RefRW<Batch> refRW = m_BatchData.GetRefRW(item.m_Entity);
				ref Batch valueRW = ref refRW.ValueRW;
				ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
				reference.m_BoundsUpdated = true;
				if (item.m_TriangleCount != 0)
				{
					if (valueRW.m_AllocatedSize == 0)
					{
						Allocate(ref valueRW, item.m_TriangleCount);
						valueRW.m_MetaIndex = reference.m_AreaMetaData.Length;
						ref UnsafeList<AreaMetaData> reference2 = ref reference.m_AreaMetaData;
						AreaMetaData value = new AreaMetaData
						{
							m_Entity = item.m_Entity,
							m_StartIndex = (int)valueRW.m_BatchAllocation.Begin
						};
						reference2.Add(in value);
						m_AllocationCount.Value++;
					}
					else
					{
						ref AreaMetaData reference3 = ref reference.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
						reference3.m_VisibleCount = 0;
						if (item.m_TriangleCount != valueRW.m_AllocatedSize)
						{
							m_AreaBufferAllocator.Release(valueRW.m_BatchAllocation);
							Allocate(ref valueRW, item.m_TriangleCount);
							reference3.m_StartIndex = (int)valueRW.m_BatchAllocation.Begin;
						}
					}
					m_UpdatedTriangles.Add(in valueRW.m_BatchAllocation);
				}
				else if (valueRW.m_VisibleCount == 0)
				{
					m_AllocationCount.Value--;
					m_AreaBufferAllocator.Release(valueRW.m_BatchAllocation);
					valueRW.m_BatchAllocation = default(NativeHeapBlock);
					valueRW.m_AllocatedSize = 0;
					reference.m_AreaMetaData.RemoveAtSwapBack(valueRW.m_MetaIndex);
					reference.m_VisibleUpdated = true;
					if (valueRW.m_MetaIndex < reference.m_AreaMetaData.Length)
					{
						refRW = m_BatchData.GetRefRW(reference.m_AreaMetaData[valueRW.m_MetaIndex].m_Entity);
						refRW.ValueRW.m_MetaIndex = valueRW.m_MetaIndex;
					}
				}
			}
		}

		private void Allocate(ref Batch batch, int allocationSize)
		{
			batch.m_BatchAllocation = m_AreaBufferAllocator.Allocate((uint)allocationSize);
			batch.m_AllocatedSize = allocationSize;
			if (batch.m_BatchAllocation.Empty)
			{
				m_AreaBufferAllocator.Resize(m_AreaBufferAllocator.Size + 1048576 / GetTriangleSize());
				m_TriangleMetaData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
				m_AreaTriangleData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
				m_AreaColorData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
				batch.m_BatchAllocation = m_AreaBufferAllocator.Allocate((uint)allocationSize);
			}
		}
	}

	[BurstCompile]
	private struct TriangleUpdateJob : IJobParallelFor
	{
		private struct Border : IEquatable<Border>
		{
			public float3 m_StartPos;

			public float3 m_EndPos;

			public bool Equals(Border other)
			{
				return m_StartPos.Equals(other.m_StartPos) & m_EndPos.Equals(other.m_EndPos);
			}

			public override int GetHashCode()
			{
				return m_StartPos.GetHashCode();
			}
		}

		[ReadOnly]
		public ComponentLookup<Area> m_AreaData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RenderedAreaData> m_PrefabRenderedAreaData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<Expand> m_Expands;

		[ReadOnly]
		public NativeParallelQueue<CullingAction>.Reader m_CullingActions;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Batch> m_BatchData;

		[NativeDisableParallelForRestriction]
		public NativeList<TriangleMetaData> m_TriangleMetaData;

		[NativeDisableParallelForRestriction]
		public NativeList<AreaTriangleData> m_AreaTriangleData;

		[NativeDisableParallelForRestriction]
		public NativeList<NativeBatchData> m_NativeBatchData;

		[NativeDisableContainerSafetyRestriction]
		private NativeParallelHashMap<Border, int2> m_BorderMap;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<int2> m_AdjacentNodes;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<Node> m_NodeList;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<TriangleSortData> m_TriangleSortData;

		public void Execute(int index)
		{
			NativeParallelQueue<CullingAction>.Enumerator enumerator = m_CullingActions.GetEnumerator(index);
			while (enumerator.MoveNext())
			{
				CullingAction current = enumerator.Current;
				if (current.m_PassedCulling)
				{
					PassedCulling(current);
				}
				else
				{
					FailedCulling(current);
				}
			}
			enumerator.Dispose();
		}

		private void PassedCulling(CullingAction cullingAction)
		{
			ref Batch valueRW = ref m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
			if (valueRW.m_AllocatedSize == 0)
			{
				return;
			}
			if (valueRW.m_VisibleCount == -1)
			{
				GenerateTriangleData(cullingAction.m_Item.m_Area, ref valueRW);
				valueRW.m_VisibleCount = 0;
			}
			ref TriangleMetaData reference = ref m_TriangleMetaData.ElementAt((int)valueRW.m_BatchAllocation.Begin + cullingAction.m_Item.m_Triangle);
			if (reference.m_IsVisible)
			{
				return;
			}
			reference.m_IsVisible = true;
			valueRW.m_VisibleCount++;
			ref NativeBatchData reference2 = ref m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
			ref AreaMetaData reference3 = ref reference2.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
			if (reference.m_Index >= reference3.m_VisibleCount)
			{
				reference3.m_VisibleCount = reference.m_Index + 1;
				if (!reference2.m_VisibleUpdated)
				{
					reference2.m_VisibleUpdated = true;
				}
			}
		}

		private void FailedCulling(CullingAction cullingAction)
		{
			ref Batch valueRW = ref m_BatchData.GetRefRW(cullingAction.m_Item.m_Area).ValueRW;
			if (valueRW.m_AllocatedSize == 0 || cullingAction.m_Item.m_Triangle == -1)
			{
				return;
			}
			ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(valueRW.m_BatchIndex);
			ref AreaMetaData reference2 = ref reference.m_AreaMetaData.ElementAt(valueRW.m_MetaIndex);
			if (m_TriangleMetaData[(int)valueRW.m_BatchAllocation.Begin + cullingAction.m_Item.m_Triangle].m_Index == reference2.m_VisibleCount - 1)
			{
				reference2.m_VisibleCount = 0;
				for (int i = 0; i < valueRW.m_AllocatedSize; i++)
				{
					TriangleMetaData triangleMetaData = m_TriangleMetaData[(int)valueRW.m_BatchAllocation.Begin + i];
					reference2.m_VisibleCount = math.select(reference2.m_VisibleCount, triangleMetaData.m_Index + 1, triangleMetaData.m_IsVisible && triangleMetaData.m_Index >= reference2.m_VisibleCount);
				}
				if (!reference.m_VisibleUpdated)
				{
					reference.m_VisibleUpdated = true;
				}
			}
		}

		private void GenerateTriangleData(Entity entity, ref Batch batch)
		{
			Area area = m_AreaData[entity];
			DynamicBuffer<Node> nodes = m_Nodes[entity];
			DynamicBuffer<Triangle> triangles = m_Triangles[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			RenderedAreaData renderedAreaData = m_PrefabRenderedAreaData[prefabRef.m_Prefab];
			float4 offsetDir = new float4(0f, 0f, 0f, 1f);
			bool flag = false;
			if (m_OwnerData.TryGetComponent(entity, out var componentData))
			{
				while (true)
				{
					if (m_TransformData.TryGetComponent(componentData.m_Owner, out var componentData2))
					{
						offsetDir.xy = componentData2.m_Position.xz;
						offsetDir.zw = math.forward(componentData2.m_Rotation).xz;
						flag = true;
						break;
					}
					if (!m_OwnerData.TryGetComponent(componentData.m_Owner, out var componentData3))
					{
						break;
					}
					componentData = componentData3;
				}
			}
			if (!flag)
			{
				if (nodes.Length >= 1)
				{
					offsetDir.xy = nodes[0].m_Position.xz;
				}
				if (nodes.Length >= 2)
				{
					float2 value = nodes[1].m_Position.xz - offsetDir.xy;
					if (MathUtils.TryNormalize(ref value))
					{
						offsetDir.zw = MathUtils.Left(value);
					}
				}
			}
			ref AreaMetaData reference = ref m_NativeBatchData.ElementAt(batch.m_BatchIndex).m_AreaMetaData.ElementAt(batch.m_MetaIndex);
			bool isCounterClockwise = (area.m_Flags & AreaFlags.CounterClockwise) != 0;
			if (!m_BorderMap.IsCreated)
			{
				m_BorderMap = new NativeParallelHashMap<Border, int2>(nodes.Length, Allocator.Temp);
			}
			if (!m_AdjacentNodes.IsCreated)
			{
				m_AdjacentNodes = new NativeList<int2>(nodes.Length, Allocator.Temp);
			}
			if (!m_TriangleSortData.IsCreated)
			{
				m_TriangleSortData = new NativeList<TriangleSortData>(triangles.Length, Allocator.Temp);
			}
			SortTriangles(triangles, ref batch);
			if (m_Expands.TryGetBuffer(entity, out var bufferData))
			{
				if (!m_NodeList.IsCreated)
				{
					m_NodeList = new NativeList<Node>(nodes.Length, Allocator.Temp);
				}
				FillExpandedNodes(nodes, bufferData);
				AddBorders(m_NodeList, isCounterClockwise);
				AddNodes(m_NodeList, triangles, isCounterClockwise);
				reference.m_Bounds = AddTriangles(m_NodeList, triangles, renderedAreaData, (int)batch.m_BatchAllocation.Begin, offsetDir, isCounterClockwise);
			}
			else
			{
				AddBorders(nodes, isCounterClockwise);
				AddNodes(nodes, triangles, isCounterClockwise);
				reference.m_Bounds = AddTriangles(nodes, triangles, renderedAreaData, (int)batch.m_BatchAllocation.Begin, offsetDir, isCounterClockwise);
			}
		}

		private void SortTriangles(DynamicBuffer<Triangle> triangles, ref Batch batch)
		{
			m_TriangleSortData.ResizeUninitialized(triangles.Length);
			for (int i = 0; i < m_TriangleSortData.Length; i++)
			{
				m_TriangleSortData[i] = new TriangleSortData
				{
					m_Index = i,
					m_MinLod = triangles[i].m_MinLod
				};
			}
			m_TriangleSortData.Sort();
			for (int j = 0; j < m_TriangleSortData.Length; j++)
			{
				TriangleSortData triangleSortData = m_TriangleSortData[j];
				m_TriangleMetaData[(int)batch.m_BatchAllocation.Begin + triangleSortData.m_Index] = new TriangleMetaData
				{
					m_Index = j
				};
			}
		}

		private void FillExpandedNodes(DynamicBuffer<Node> nodes, DynamicBuffer<Expand> expands)
		{
			m_NodeList.ResizeUninitialized(nodes.Length);
			for (int i = 0; i < nodes.Length; i++)
			{
				Node value = nodes[i];
				Expand expand = expands[i];
				value.m_Position.xz += expand.m_Offset;
				m_NodeList[i] = value;
			}
		}

		private void AddBorders<TNodeList>(TNodeList nodes, bool isCounterClockwise) where TNodeList : INativeList<Node>
		{
			m_BorderMap.Clear();
			float3 @float = nodes[0].m_Position;
			for (int i = 1; i < nodes.Length; i++)
			{
				float3 position = nodes[i].m_Position;
				if (isCounterClockwise)
				{
					m_BorderMap.Add(new Border
					{
						m_StartPos = position,
						m_EndPos = @float
					}, new int2(i, i - 1));
				}
				else
				{
					m_BorderMap.Add(new Border
					{
						m_StartPos = @float,
						m_EndPos = position
					}, new int2(i - 1, i));
				}
				@float = position;
			}
			float3 position2 = nodes[0].m_Position;
			if (isCounterClockwise)
			{
				m_BorderMap.Add(new Border
				{
					m_StartPos = position2,
					m_EndPos = @float
				}, new int2(0, nodes.Length - 1));
			}
			else
			{
				m_BorderMap.Add(new Border
				{
					m_StartPos = @float,
					m_EndPos = position2
				}, new int2(nodes.Length - 1, 0));
			}
		}

		private void AddNodes<TNodeList>(TNodeList nodes, DynamicBuffer<Triangle> triangles, bool isCounterClockwise) where TNodeList : INativeList<Node>
		{
			m_AdjacentNodes.ResizeUninitialized(nodes.Length);
			for (int i = 0; i < m_AdjacentNodes.Length; i++)
			{
				m_AdjacentNodes[i] = i;
			}
			for (int j = 0; j < triangles.Length; j++)
			{
				Triangle triangle = triangles[j];
				int2 adjacentA = m_AdjacentNodes[triangle.m_Indices.x];
				int2 adjacentB = m_AdjacentNodes[triangle.m_Indices.y];
				int2 adjacentB2 = m_AdjacentNodes[triangle.m_Indices.z];
				CheckBorder(ref adjacentA, ref adjacentB, nodes, triangle.m_Indices.x, triangle.m_Indices.y, isCounterClockwise);
				CheckBorder(ref adjacentB, ref adjacentB2, nodes, triangle.m_Indices.y, triangle.m_Indices.z, isCounterClockwise);
				CheckBorder(ref adjacentB2, ref adjacentA, nodes, triangle.m_Indices.z, triangle.m_Indices.x, isCounterClockwise);
				m_AdjacentNodes[triangle.m_Indices.x] = adjacentA;
				m_AdjacentNodes[triangle.m_Indices.y] = adjacentB;
				m_AdjacentNodes[triangle.m_Indices.z] = adjacentB2;
			}
			for (int k = 0; k < m_AdjacentNodes.Length; k++)
			{
				int2 @int = m_AdjacentNodes[k];
				bool2 x = @int != k;
				if (!math.any(x))
				{
					continue;
				}
				if (x.x)
				{
					for (int l = 0; l < nodes.Length; l++)
					{
						int x2 = m_AdjacentNodes[@int.x].x;
						if (x2 == @int.x)
						{
							break;
						}
						if (x2 == k || x2 == -1)
						{
							@int.x = -1;
							break;
						}
						@int.x = x2;
					}
				}
				if (x.y)
				{
					for (int m = 0; m < nodes.Length; m++)
					{
						int y = m_AdjacentNodes[@int.y].y;
						if (y == @int.y)
						{
							break;
						}
						if (y == k || y == -1)
						{
							@int.y = -1;
							break;
						}
						@int.y = y;
					}
				}
				m_AdjacentNodes[k] = @int;
			}
			for (int n = 0; n < m_AdjacentNodes.Length; n++)
			{
				int2 int2 = m_AdjacentNodes[n];
				m_AdjacentNodes[n] = math.select(math.select(int2 + new int2(-1, 1), new int2(nodes.Length - 1, 0), int2 == new int2(0, nodes.Length - 1)), n, int2 == -1);
			}
		}

		private void CheckBorder<TNodeList>(ref int2 adjacentA, ref int2 adjacentB, TNodeList nodes, int nodeA, int nodeB, bool isCounterClockwise) where TNodeList : INativeList<Node>
		{
			Border key = new Border
			{
				m_StartPos = nodes[nodeA].m_Position,
				m_EndPos = nodes[nodeB].m_Position
			};
			if (m_BorderMap.TryGetValue(key, out var item))
			{
				if (isCounterClockwise)
				{
					adjacentB.x = item.y;
					adjacentA.y = item.x;
				}
				else
				{
					adjacentA.x = item.x;
					adjacentB.y = item.y;
				}
			}
		}

		private Bounds3 AddTriangles<TNodeList>(TNodeList nodes, DynamicBuffer<Triangle> triangles, RenderedAreaData renderedAreaData, int triangleOffset, float4 offsetDir, bool isCounterClockwise) where TNodeList : INativeList<Node>
		{
			Bounds3 result = new Bounds3(float.MaxValue, float.MinValue);
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle triangle = triangles[i];
				int2 @int = m_AdjacentNodes[triangle.m_Indices.x];
				int2 int2 = m_AdjacentNodes[triangle.m_Indices.y];
				int2 int3 = m_AdjacentNodes[triangle.m_Indices.z];
				int x = m_AdjacentNodes[@int.x].x;
				int x2 = m_AdjacentNodes[int2.x].x;
				int x3 = m_AdjacentNodes[int3.x].x;
				int y = m_AdjacentNodes[@int.y].y;
				int y2 = m_AdjacentNodes[int2.y].y;
				int y3 = m_AdjacentNodes[int3.y].y;
				AreaTriangleData value = new AreaTriangleData
				{
					m_APos = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.x, @int.x, @int.y, renderedAreaData.m_ExpandAmount, isCounterClockwise),
					m_BPos = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.y, int2.x, int2.y, renderedAreaData.m_ExpandAmount, isCounterClockwise),
					m_CPos = AreaUtils.GetExpandedNode(nodes, triangle.m_Indices.z, int3.x, int3.y, renderedAreaData.m_ExpandAmount, isCounterClockwise),
					m_APrevXZ = AreaUtils.GetExpandedNode(nodes, @int.x, x, triangle.m_Indices.x, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz,
					m_BPrevXZ = AreaUtils.GetExpandedNode(nodes, int2.x, x2, triangle.m_Indices.y, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz,
					m_CPrevXZ = AreaUtils.GetExpandedNode(nodes, int3.x, x3, triangle.m_Indices.z, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz,
					m_ANextXZ = AreaUtils.GetExpandedNode(nodes, @int.y, triangle.m_Indices.x, y, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz,
					m_BNextXZ = AreaUtils.GetExpandedNode(nodes, int2.y, triangle.m_Indices.y, y2, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz,
					m_CNextXZ = AreaUtils.GetExpandedNode(nodes, int3.y, triangle.m_Indices.z, y3, renderedAreaData.m_ExpandAmount, isCounterClockwise).xz
				};
				float3 x4 = new float3(value.m_APos.y, value.m_BPos.y, value.m_CPos.y);
				value.m_YMinMax.x = triangle.m_HeightRange.min - renderedAreaData.m_HeightOffset + math.cmin(x4);
				value.m_YMinMax.y = triangle.m_HeightRange.max + renderedAreaData.m_HeightOffset + math.cmax(x4);
				value.m_OffsetDir = offsetDir;
				value.m_LodDistanceFactor = RenderingUtils.CalculateDistanceFactor(triangle.m_MinLod);
				Bounds3 bounds = MathUtils.Bounds(new Triangle3(value.m_APos, value.m_BPos, value.m_CPos));
				bounds.min.y = value.m_YMinMax.x;
				bounds.max.y = value.m_YMinMax.y;
				result |= bounds;
				ref TriangleMetaData reference = ref m_TriangleMetaData.ElementAt(triangleOffset + i);
				m_AreaTriangleData[triangleOffset + reference.m_Index] = value;
			}
			return result;
		}
	}

	[BurstCompile]
	private struct VisibleUpdateJob : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeList<NativeBatchData> m_NativeBatchData;

		public void Execute(int index)
		{
			ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(index);
			if (reference.m_BoundsUpdated)
			{
				reference.m_Bounds = new Bounds3(float.MaxValue, float.MinValue);
				reference.m_BoundsUpdated = false;
				for (int i = 0; i < reference.m_AreaMetaData.Length; i++)
				{
					ref AreaMetaData reference2 = ref reference.m_AreaMetaData.ElementAt(i);
					reference.m_Bounds |= reference2.m_Bounds;
				}
			}
			if (!reference.m_VisibleUpdated)
			{
				return;
			}
			reference.m_VisibleIndices.Clear();
			reference.m_VisibleIndicesUpdated = true;
			reference.m_VisibleUpdated = false;
			if (!reference.m_IsEnabled)
			{
				return;
			}
			for (int j = 0; j < reference.m_AreaMetaData.Length; j++)
			{
				ref AreaMetaData reference3 = ref reference.m_AreaMetaData.ElementAt(j);
				reference.m_Bounds |= (float3)reference3.m_StartIndex;
				for (int k = 0; k < reference3.m_VisibleCount; k++)
				{
					reference.m_VisibleIndices.Add(reference3.m_StartIndex + k);
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

		[ReadOnly]
		public ComponentTypeHandle<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AreaGeometryData>(isReadOnly: true);
		}
	}

	public const uint AREABUFFER_MEMORY_DEFAULT = 4194304u;

	public const uint AREABUFFER_MEMORY_INCREMENT = 1048576u;

	private PrefabSystem m_PrefabSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private RenderingSystem m_RenderingSystem;

	private BatchDataSystem m_BatchDataSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private ComputeBuffer m_AreaTriangleBuffer;

	private ComputeBuffer m_AreaColorBuffer;

	private List<ManagedBatchData> m_ManagedBatchData;

	private NativeHeapAllocator m_AreaBufferAllocator;

	private NativeReference<int> m_AllocationCount;

	private NativeList<NativeBatchData> m_NativeBatchData;

	private NativeList<AreaTriangleData> m_AreaTriangleData;

	private NativeList<TriangleMetaData> m_TriangleMetaData;

	private NativeList<AreaColorData> m_AreaColorData;

	private NativeList<NativeHeapBlock> m_UpdatedTriangles;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_PrefabQuery;

	private JobHandle m_DataDependencies;

	private float3 m_PrevCameraPosition;

	private float3 m_PrevCameraDirection;

	private float4 m_PrevLodParameters;

	private int m_AreaParameters;

	private int m_DecalLayerMask;

	private bool m_Loaded;

	private bool m_ColorsUpdated;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected unsafe override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_BatchDataSystem = base.World.GetOrCreateSystemManaged<BatchDataSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Batch>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<RenderedAreaData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ManagedBatchData = new List<ManagedBatchData>();
		m_AreaBufferAllocator = new NativeHeapAllocator(4194304 / GetTriangleSize(), 1u, Allocator.Persistent);
		m_AllocationCount = new NativeReference<int>(0, Allocator.Persistent);
		m_NativeBatchData = new NativeList<NativeBatchData>(Allocator.Persistent);
		m_AreaTriangleData = new NativeList<AreaTriangleData>(Allocator.Persistent);
		m_TriangleMetaData = new NativeList<TriangleMetaData>(Allocator.Persistent);
		m_AreaColorData = new NativeList<AreaColorData>(Allocator.Persistent);
		m_UpdatedTriangles = new NativeList<NativeHeapBlock>(100, Allocator.Persistent);
		m_AreaTriangleData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
		m_TriangleMetaData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
		m_AreaColorData.ResizeUninitialized((int)m_AreaBufferAllocator.Size);
		m_AreaTriangleBuffer = new ComputeBuffer(m_AreaTriangleData.Capacity, sizeof(AreaTriangleData));
		m_AreaColorBuffer = new ComputeBuffer(m_AreaColorData.Capacity, sizeof(AreaColorData));
		m_AreaParameters = Shader.PropertyToID("colossal_AreaParameters");
		m_DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_AreaTriangleBuffer.Release();
		m_AreaColorBuffer.Release();
		for (int i = 0; i < m_ManagedBatchData.Count; i++)
		{
			ManagedBatchData managedBatchData = m_ManagedBatchData[i];
			if (managedBatchData.m_Material != null)
			{
				UnityEngine.Object.Destroy(managedBatchData.m_Material);
			}
			if (managedBatchData.m_VisibleIndices != null)
			{
				managedBatchData.m_VisibleIndices.Release();
			}
		}
		m_DataDependencies.Complete();
		for (int j = 0; j < m_NativeBatchData.Length; j++)
		{
			ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(j);
			if (reference.m_AreaMetaData.IsCreated)
			{
				reference.m_AreaMetaData.Dispose();
			}
			if (reference.m_VisibleIndices.IsCreated)
			{
				reference.m_VisibleIndices.Dispose();
			}
		}
		m_AreaBufferAllocator.Dispose();
		m_AllocationCount.Dispose();
		m_NativeBatchData.Dispose();
		m_AreaTriangleData.Dispose();
		m_TriangleMetaData.Dispose();
		m_AreaColorData.Dispose();
		m_UpdatedTriangles.Dispose();
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		m_DataDependencies.Complete();
		m_AllocationCount.Value = 0;
		m_AreaBufferAllocator.Clear();
		m_UpdatedTriangles.Clear();
		for (int i = 0; i < m_NativeBatchData.Length; i++)
		{
			ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(i);
			if (reference.m_AreaMetaData.IsCreated)
			{
				reference.m_AreaMetaData.Clear();
			}
			if (reference.m_VisibleIndices.IsCreated)
			{
				reference.m_VisibleIndices.Clear();
			}
		}
		m_Loaded = true;
	}

	public int GetBatchCount()
	{
		return m_ManagedBatchData.Count;
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

	public unsafe bool GetAreaBatch(int index, out ComputeBuffer buffer, out ComputeBuffer colors, out GraphicsBuffer indices, out Material material, out Bounds bounds, out int count, out int rendererPriority)
	{
		m_DataDependencies.Complete();
		ManagedBatchData managedBatchData = m_ManagedBatchData[index];
		ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(index);
		if (m_AreaTriangleBuffer.count != m_AreaTriangleData.Capacity)
		{
			m_AreaTriangleBuffer.Release();
			m_AreaTriangleBuffer = new ComputeBuffer(m_AreaTriangleData.Capacity, sizeof(AreaTriangleData));
			m_UpdatedTriangles.Clear();
			uint onePastHighestUsedAddress = m_AreaBufferAllocator.OnePastHighestUsedAddress;
			if (onePastHighestUsedAddress != 0)
			{
				m_UpdatedTriangles.Add(new NativeHeapBlock(new UnsafeHeapBlock(0u, onePastHighestUsedAddress)));
			}
		}
		if (m_UpdatedTriangles.Length != 0)
		{
			for (int i = 0; i < m_UpdatedTriangles.Length; i++)
			{
				NativeHeapBlock nativeHeapBlock = m_UpdatedTriangles[i];
				m_AreaTriangleBuffer.SetData(m_AreaTriangleData.AsArray(), (int)nativeHeapBlock.Begin, (int)nativeHeapBlock.Begin, (int)nativeHeapBlock.Length);
			}
			m_UpdatedTriangles.Clear();
		}
		if (m_AreaColorBuffer.count != m_AreaColorData.Capacity)
		{
			m_AreaColorBuffer.Release();
			m_AreaColorBuffer = new ComputeBuffer(m_AreaColorData.Capacity, sizeof(AreaColorData));
		}
		if (m_ColorsUpdated)
		{
			m_ColorsUpdated = false;
			uint onePastHighestUsedAddress2 = m_AreaBufferAllocator.OnePastHighestUsedAddress;
			if (onePastHighestUsedAddress2 != 0)
			{
				m_AreaColorBuffer.SetData(m_AreaColorData.AsArray(), 0, 0, (int)onePastHighestUsedAddress2);
			}
		}
		if (managedBatchData.m_VisibleIndices.count != reference.m_VisibleIndices.Capacity)
		{
			managedBatchData.m_VisibleIndices.Release();
			managedBatchData.m_VisibleIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, reference.m_VisibleIndices.Capacity, 4);
		}
		if (reference.m_VisibleIndicesUpdated)
		{
			reference.m_VisibleIndicesUpdated = false;
			if (reference.m_VisibleIndices.Length != 0)
			{
				NativeArray<int> data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(reference.m_VisibleIndices.Ptr, reference.m_VisibleIndices.Length, Allocator.None);
				managedBatchData.m_VisibleIndices.SetData(data, 0, 0, data.Length);
			}
		}
		buffer = m_AreaTriangleBuffer;
		colors = m_AreaColorBuffer;
		indices = managedBatchData.m_VisibleIndices;
		material = managedBatchData.m_Material;
		bounds = RenderingUtils.ToBounds(reference.m_Bounds);
		count = reference.m_VisibleIndices.Length;
		rendererPriority = managedBatchData.m_RendererPriority;
		return count != 0;
	}

	public NativeList<AreaColorData> GetColorData(out JobHandle dependencies)
	{
		m_ColorsUpdated = true;
		dependencies = m_DataDependencies;
		return m_AreaColorData;
	}

	public void AddColorWriter(JobHandle jobHandle)
	{
		m_DataDependencies = jobHandle;
	}

	public void GetAreaStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_DataDependencies.Complete();
		allocatedSize = m_AreaBufferAllocator.UsedSpace * GetTriangleSize();
		bufferSize = m_AreaBufferAllocator.Size * GetTriangleSize();
		count = (uint)m_AllocationCount.Value;
	}

	private unsafe static uint GetTriangleSize()
	{
		return (uint)(sizeof(AreaTriangleData) + sizeof(AreaColorData));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		m_DataDependencies.Complete();
		m_UpdatedTriangles.Clear();
		if (!m_PrefabQuery.IsEmptyIgnoreFilter)
		{
			UpdatePrefabs();
		}
		float3 @float = m_PrevCameraPosition;
		float3 float2 = m_PrevCameraDirection;
		float4 float3 = m_PrevLodParameters;
		if (m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters))
		{
			@float = lodParameters.cameraPosition;
			IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
			float3 = RenderingUtils.CalculateLodParameters(m_BatchDataSystem.GetLevelOfDetail(m_RenderingSystem.frameLod, activeCameraController), lodParameters);
			float2 = m_CameraUpdateSystem.activeViewer.forward;
		}
		BoundsMask visibleMask = BoundsMask.NormalLayers;
		BoundsMask prevVisibleMask = BoundsMask.NormalLayers;
		if (loaded)
		{
			m_PrevCameraPosition = @float;
			m_PrevCameraDirection = float2;
			m_PrevLodParameters = float3;
			prevVisibleMask = (BoundsMask)0;
		}
		NativeParallelQueue<CullingAction> nativeParallelQueue = new NativeParallelQueue<CullingAction>(Allocator.TempJob);
		NativeQueue<AllocationAction> allocationActions = new NativeQueue<AllocationAction>(Allocator.TempJob);
		NativeArray<int> nodeBuffer = new NativeArray<int>(512, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		NativeArray<int> subDataBuffer = new NativeArray<int>(512, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		JobHandle dependencies;
		TreeCullingJob1 jobData = new TreeCullingJob1
		{
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_LodParameters = float3,
			m_PrevLodParameters = m_PrevLodParameters,
			m_CameraPosition = @float,
			m_PrevCameraPosition = m_PrevCameraPosition,
			m_CameraDirection = float2,
			m_PrevCameraDirection = m_PrevCameraDirection,
			m_VisibleMask = visibleMask,
			m_PrevVisibleMask = prevVisibleMask,
			m_NodeBuffer = nodeBuffer,
			m_SubDataBuffer = subDataBuffer,
			m_ActionQueue = nativeParallelQueue.AsWriter()
		};
		TreeCullingJob2 jobData2 = new TreeCullingJob2
		{
			m_AreaSearchTree = jobData.m_AreaSearchTree,
			m_LodParameters = float3,
			m_PrevLodParameters = m_PrevLodParameters,
			m_CameraPosition = @float,
			m_PrevCameraPosition = m_PrevCameraPosition,
			m_CameraDirection = float2,
			m_PrevCameraDirection = m_PrevCameraDirection,
			m_VisibleMask = visibleMask,
			m_PrevVisibleMask = prevVisibleMask,
			m_NodeBuffer = nodeBuffer,
			m_SubDataBuffer = subDataBuffer,
			m_ActionQueue = nativeParallelQueue.AsWriter()
		};
		QueryCullingJob jobData3 = new QueryCullingJob
		{
			m_EntityType = GetEntityTypeHandle(),
			m_BatchType = GetComponentTypeHandle<Batch>(isReadOnly: true),
			m_DeletedType = GetComponentTypeHandle<Deleted>(isReadOnly: true),
			m_PrefabRefType = GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
			m_NodeType = GetBufferTypeHandle<Node>(isReadOnly: true),
			m_TriangleType = GetBufferTypeHandle<Triangle>(isReadOnly: true),
			m_PrefabAreaGeometryData = GetComponentLookup<AreaGeometryData>(isReadOnly: true),
			m_LodParameters = float3,
			m_CameraPosition = @float,
			m_CameraDirection = float2,
			m_VisibleMask = visibleMask,
			m_ActionQueue = nativeParallelQueue.AsWriter()
		};
		CullingActionJob jobData4 = new CullingActionJob
		{
			m_PrefabRefData = GetComponentLookup<PrefabRef>(isReadOnly: true),
			m_PrefabRenderedAreaData = GetComponentLookup<RenderedAreaData>(isReadOnly: true),
			m_Triangles = GetBufferLookup<Triangle>(isReadOnly: true),
			m_CullingActions = nativeParallelQueue.AsReader(),
			m_AllocationActions = allocationActions.AsParallelWriter(),
			m_BatchData = GetComponentLookup<Batch>(),
			m_TriangleMetaData = m_TriangleMetaData
		};
		BatchAllocationJob jobData5 = new BatchAllocationJob
		{
			m_BatchData = GetComponentLookup<Batch>(),
			m_NativeBatchData = m_NativeBatchData,
			m_TriangleMetaData = m_TriangleMetaData,
			m_AreaTriangleData = m_AreaTriangleData,
			m_AreaColorData = m_AreaColorData,
			m_UpdatedTriangles = m_UpdatedTriangles,
			m_AllocationActions = allocationActions,
			m_AreaBufferAllocator = m_AreaBufferAllocator,
			m_AllocationCount = m_AllocationCount
		};
		TriangleUpdateJob jobData6 = new TriangleUpdateJob
		{
			m_AreaData = GetComponentLookup<Area>(isReadOnly: true),
			m_OwnerData = GetComponentLookup<Owner>(isReadOnly: true),
			m_TransformData = GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
			m_PrefabRefData = GetComponentLookup<PrefabRef>(isReadOnly: true),
			m_PrefabRenderedAreaData = GetComponentLookup<RenderedAreaData>(isReadOnly: true),
			m_Nodes = GetBufferLookup<Node>(isReadOnly: true),
			m_Triangles = GetBufferLookup<Triangle>(isReadOnly: true),
			m_Expands = GetBufferLookup<Expand>(isReadOnly: true),
			m_CullingActions = nativeParallelQueue.AsReader(),
			m_BatchData = GetComponentLookup<Batch>(),
			m_TriangleMetaData = m_TriangleMetaData,
			m_AreaTriangleData = m_AreaTriangleData,
			m_NativeBatchData = m_NativeBatchData
		};
		VisibleUpdateJob jobData7 = new VisibleUpdateJob
		{
			m_NativeBatchData = m_NativeBatchData
		};
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(dependsOn: IJobExtensions.Schedule(jobData, dependencies), jobData: jobData2, arrayLength: nodeBuffer.Length, innerloopBatchCount: 1);
		JobHandle dependsOn = IJobParallelForExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.ScheduleParallel(jobData3, m_UpdatedQuery, base.Dependency)), jobData: jobData4, arrayLength: nativeParallelQueue.HashRange, innerloopBatchCount: 1);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData5, dependsOn);
		JobHandle jobHandle3 = IJobParallelForExtensions.Schedule(jobData6, nativeParallelQueue.HashRange, 1, jobHandle2);
		JobHandle dataDependencies = IJobParallelForExtensions.Schedule(jobData7, m_ManagedBatchData.Count, 1, jobHandle3);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		nativeParallelQueue.Dispose(jobHandle3);
		allocationActions.Dispose(jobHandle2);
		nodeBuffer.Dispose(jobHandle);
		subDataBuffer.Dispose(jobHandle);
		m_PrevCameraPosition = @float;
		m_PrevCameraDirection = float2;
		m_PrevLodParameters = float3;
		base.Dependency = jobHandle3;
		m_DataDependencies = dataDependencies;
	}

	public void EnabledShadersUpdated()
	{
		m_DataDependencies.Complete();
		for (int i = 0; i < m_ManagedBatchData.Count; i++)
		{
			ManagedBatchData managedBatchData = m_ManagedBatchData[i];
			ref NativeBatchData reference = ref m_NativeBatchData.ElementAt(i);
			bool flag = m_RenderingSystem.IsShaderEnabled(managedBatchData.m_Material.shader);
			reference.m_VisibleUpdated |= flag != reference.m_IsEnabled;
			reference.m_IsEnabled = flag;
		}
	}

	private void UpdatePrefabs()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<AreaGeometryData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<PrefabData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
				NativeArray<AreaGeometryData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle3);
				if (archetypeChunk.Has(ref typeHandle))
				{
					m_DataDependencies.Complete();
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity entity = nativeArray2[j];
						RenderedAreaData componentData = base.EntityManager.GetComponentData<RenderedAreaData>(entity);
						if (m_ManagedBatchData.Count <= componentData.m_BatchIndex)
						{
							continue;
						}
						ManagedBatchData managedBatchData = m_ManagedBatchData[componentData.m_BatchIndex];
						NativeBatchData nativeBatchData = m_NativeBatchData[componentData.m_BatchIndex];
						if (!(nativeBatchData.m_Prefab != entity))
						{
							if (managedBatchData.m_Material != null)
							{
								UnityEngine.Object.Destroy(managedBatchData.m_Material);
							}
							if (managedBatchData.m_VisibleIndices != null)
							{
								managedBatchData.m_VisibleIndices.Release();
							}
							if (nativeBatchData.m_AreaMetaData.IsCreated)
							{
								nativeBatchData.m_AreaMetaData.Dispose();
							}
							if (nativeBatchData.m_VisibleIndices.IsCreated)
							{
								nativeBatchData.m_VisibleIndices.Dispose();
							}
							if (componentData.m_BatchIndex != m_ManagedBatchData.Count - 1)
							{
								ManagedBatchData value = m_ManagedBatchData[m_ManagedBatchData.Count - 1];
								NativeBatchData value2 = m_NativeBatchData[m_ManagedBatchData.Count - 1];
								RenderedAreaData componentData2 = base.EntityManager.GetComponentData<RenderedAreaData>(value2.m_Prefab);
								componentData2.m_BatchIndex = componentData.m_BatchIndex;
								base.EntityManager.SetComponentData(value2.m_Prefab, componentData2);
								m_ManagedBatchData[componentData.m_BatchIndex] = value;
								m_NativeBatchData[componentData.m_BatchIndex] = value2;
							}
							m_ManagedBatchData.RemoveAt(m_ManagedBatchData.Count - 1);
							m_NativeBatchData.RemoveAt(m_NativeBatchData.Length - 1);
						}
					}
				}
				else
				{
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Entity entity2 = nativeArray2[k];
						RenderedArea component = m_PrefabSystem.GetPrefab<AreaPrefab>(nativeArray3[k]).GetComponent<RenderedArea>();
						float minNodeDistance = AreaUtils.GetMinNodeDistance(nativeArray4[k].m_Type);
						float num = minNodeDistance * 2f;
						float num2 = math.clamp(component.m_Roundness, 0.01f, 0.99f) * minNodeDistance;
						RenderedAreaData componentData3 = base.EntityManager.GetComponentData<RenderedAreaData>(entity2);
						componentData3.m_HeightOffset = num;
						componentData3.m_ExpandAmount = num2 * 0.5f;
						componentData3.m_BatchIndex = m_ManagedBatchData.Count;
						base.EntityManager.SetComponentData(entity2, componentData3);
						ManagedBatchData managedBatchData2 = new ManagedBatchData();
						managedBatchData2.m_Material = new Material(component.m_Material);
						managedBatchData2.m_Material.name = "Area batch (" + component.m_Material.name + ")";
						managedBatchData2.m_Material.renderQueue = component.m_Material.shader.renderQueue;
						managedBatchData2.m_Material.SetVector(m_AreaParameters, new Vector4(num2, num, 0f, 0f));
						managedBatchData2.m_Material.SetFloat(m_DecalLayerMask, math.asfloat((int)component.m_DecalLayerMask));
						managedBatchData2.m_RendererPriority = component.m_RendererPriority;
						NativeBatchData value3 = new NativeBatchData
						{
							m_AreaMetaData = new UnsafeList<AreaMetaData>(10, Allocator.Persistent),
							m_VisibleIndices = new UnsafeList<int>(100, Allocator.Persistent),
							m_Prefab = entity2,
							m_IsEnabled = m_RenderingSystem.IsShaderEnabled(managedBatchData2.m_Material.shader)
						};
						managedBatchData2.m_VisibleIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, value3.m_VisibleIndices.Capacity, 4);
						m_ManagedBatchData.Add(managedBatchData2);
						m_NativeBatchData.Add(in value3);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
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
	public AreaBatchSystem()
	{
	}
}
