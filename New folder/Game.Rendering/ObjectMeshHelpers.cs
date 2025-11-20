using System;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering;

public static class ObjectMeshHelpers
{
	private struct TreeNode
	{
		public Bounds3 m_Bounds;

		public int m_FirstTriangle;

		public int m_ItemCount;

		public int m_NodeIndex;
	}

	[BurstCompile]
	private struct CacheMeshDataJob : IJob
	{
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_Positions;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_Normals;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<byte> m_Indices;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public GeometryAsset.Data m_Data;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public IndexFormat m_IndexFormat;

		[ReadOnly]
		public Bounds3 m_MeshBounds;

		[ReadOnly]
		public int m_VertexCount;

		[ReadOnly]
		public int m_IndexCount;

		[ReadOnly]
		public bool m_CacheNormals;

		public EntityCommandBuffer m_CommandBuffer;

		public unsafe void Execute()
		{
			DynamicBuffer<MeshVertex> dst = m_CommandBuffer.AddBuffer<MeshVertex>(m_Entity);
			DynamicBuffer<MeshIndex> dynamicBuffer = m_CommandBuffer.AddBuffer<MeshIndex>(m_Entity);
			DynamicBuffer<MeshNode> dynamicBuffer2 = m_CommandBuffer.AddBuffer<MeshNode>(m_Entity);
			DynamicBuffer<MeshNormal> dst2 = default(DynamicBuffer<MeshNormal>);
			if (m_CacheNormals)
			{
				dst2 = m_CommandBuffer.AddBuffer<MeshNormal>(m_Entity);
			}
			IndexFormat indexFormat = IndexFormat.UInt16;
			NativeArray<byte> nativeArray = default(NativeArray<byte>);
			int num = 0;
			if (m_Data.IsValid)
			{
				int allVertexCount = GeometryAsset.GetAllVertexCount(ref m_Data);
				dst.ResizeUninitialized(allVertexCount);
				if (m_CacheNormals)
				{
					dst2.ResizeUninitialized(allVertexCount);
				}
				allVertexCount = 0;
				for (int i = 0; i < m_Data.meshCount; i++)
				{
					int vertexCount = GeometryAsset.GetVertexCount(ref m_Data, i);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Position, out var format, out var dimension);
					if (dimension == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a position");
					}
					NativeSlice<byte> attributeData = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Position);
					NativeArray<MeshVertex> subArray = dst.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					MeshVertex.Unpack(attributeData, subArray, vertexCount, format, dimension);
					if (m_CacheNormals)
					{
						GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Normal, out var format2, out var dimension2);
						if (dimension2 == 0)
						{
							throw new Exception("Cannot cache geometry asset: mesh do not have a normal");
						}
						NativeSlice<byte> attributeData2 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Normal);
						NativeArray<MeshNormal> subArray2 = dst2.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
						MeshNormal.Unpack(attributeData2, subArray2, vertexCount, format2, dimension2);
					}
					allVertexCount += vertexCount;
				}
				indexFormat = IndexFormat.UInt32;
				nativeArray = GeometryAsset.ConvertAllIndicesTo32(ref m_Data, Allocator.Temp).Reinterpret<byte>(4);
				num = GeometryAsset.GetAllIndicesCount(ref m_Data);
			}
			else
			{
				MeshVertex.Unpack(m_Positions, dst, m_VertexCount, VertexAttributeFormat.Float32, 3);
				if (m_CacheNormals)
				{
					MeshNormal.Unpack(m_Normals, dst2, m_VertexCount, VertexAttributeFormat.Float32, 3);
				}
				indexFormat = m_IndexFormat;
				nativeArray = m_Indices;
				num = m_IndexCount;
			}
			if (num != 0)
			{
				dynamicBuffer.ResizeUninitialized(num);
				CalculateTreeSize(num, m_MeshBounds, out var treeDepth, out var treeSize, out var sizeFactor, out var sizeOffset);
				NativeArray<TreeNode> nativeArray2 = new NativeArray<TreeNode>(treeSize, Allocator.Temp);
				NativeArray<int> nextTriangle = new NativeArray<int>(num / 3, Allocator.Temp);
				InitializeTree(nativeArray2, treeSize);
				if (indexFormat == IndexFormat.UInt32)
				{
					FillTreeNodes(nativeArray2, nextTriangle, dst.AsNativeArray(), nativeArray.Reinterpret<int>(1), sizeOffset, sizeFactor, treeDepth);
				}
				else
				{
					FillTreeNodes(nativeArray2, nextTriangle, dst.AsNativeArray(), nativeArray.Reinterpret<ushort>(1), sizeOffset, sizeFactor, treeDepth);
				}
				int* ptr = stackalloc int[16];
				UpdateNodes(nativeArray2, treeDepth, ptr);
				dynamicBuffer2.ResizeUninitialized(ptr[treeDepth]);
				if (indexFormat == IndexFormat.UInt32)
				{
					FillMeshData(nativeArray2, nextTriangle, dynamicBuffer2.AsNativeArray(), nativeArray.Reinterpret<int>(1), dynamicBuffer.AsNativeArray(), treeDepth, ptr);
				}
				else
				{
					FillMeshData(nativeArray2, nextTriangle, dynamicBuffer2.AsNativeArray(), nativeArray.Reinterpret<ushort>(1), dynamicBuffer.AsNativeArray(), treeDepth, ptr);
				}
				nextTriangle.Dispose();
				nativeArray2.Dispose();
				if (m_Data.IsValid)
				{
					nativeArray.Dispose();
				}
			}
		}
	}

	[BurstCompile]
	private struct CacheProceduralMeshDataJob : IJob
	{
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_Positions;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_Normals;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_BoneIds;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_BoneInfluences;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<byte> m_Indices;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public GeometryAsset.Data m_Data;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public IndexFormat m_IndexFormat;

		[ReadOnly]
		public int m_VertexCount;

		[ReadOnly]
		public int m_IndexCount;

		[ReadOnly]
		public int m_BoneCount;

		[ReadOnly]
		public bool m_CacheNormals;

		public EntityCommandBuffer m_CommandBuffer;

		public unsafe void Execute()
		{
			DynamicBuffer<MeshVertex> dst = m_CommandBuffer.AddBuffer<MeshVertex>(m_Entity);
			DynamicBuffer<MeshIndex> dynamicBuffer = m_CommandBuffer.AddBuffer<MeshIndex>(m_Entity);
			DynamicBuffer<MeshNode> dynamicBuffer2 = m_CommandBuffer.AddBuffer<MeshNode>(m_Entity);
			DynamicBuffer<MeshNormal> dst2 = default(DynamicBuffer<MeshNormal>);
			if (m_CacheNormals)
			{
				dst2 = m_CommandBuffer.AddBuffer<MeshNormal>(m_Entity);
			}
			IndexFormat indexFormat = IndexFormat.UInt16;
			NativeArray<byte> nativeArray = default(NativeArray<byte>);
			int num = 0;
			NativeArray<BoneData> nativeArray2 = default(NativeArray<BoneData>);
			NativeArray<int> nativeArray3 = default(NativeArray<int>);
			if (m_Data.IsValid)
			{
				int allVertexCount = GeometryAsset.GetAllVertexCount(ref m_Data);
				dst.ResizeUninitialized(allVertexCount);
				if (m_CacheNormals)
				{
					dst2.ResizeUninitialized(allVertexCount);
				}
				indexFormat = IndexFormat.UInt32;
				nativeArray = GeometryAsset.ConvertAllIndicesTo32(ref m_Data, Allocator.Temp).Reinterpret<byte>(4);
				num = GeometryAsset.GetAllIndicesCount(ref m_Data);
				nativeArray2 = new NativeArray<BoneData>(m_BoneCount, Allocator.Temp);
				nativeArray3 = new NativeArray<int>(num / 3, Allocator.Temp);
				InitializeBones(nativeArray2, num);
				allVertexCount = 0;
				num = 0;
				for (int i = 0; i < m_Data.meshCount; i++)
				{
					int vertexCount = GeometryAsset.GetVertexCount(ref m_Data, i);
					int indicesCount = GeometryAsset.GetIndicesCount(ref m_Data, i);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Position, out var format, out var dimension);
					if (dimension == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a position");
					}
					NativeSlice<byte> attributeData = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Position);
					NativeArray<MeshVertex> subArray = dst.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					MeshVertex.Unpack(attributeData, subArray, vertexCount, format, dimension);
					if (m_CacheNormals)
					{
						GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Normal, out var format2, out var dimension2);
						if (dimension2 == 0)
						{
							throw new Exception("Cannot cache geometry asset: mesh do not have a normal");
						}
						NativeSlice<byte> attributeData2 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Normal);
						NativeArray<MeshNormal> subArray2 = dst2.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
						MeshNormal.Unpack(attributeData2, subArray2, vertexCount, format2, dimension2);
					}
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.BlendIndices, out var format3, out var dimension3);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.BlendWeight, out var format4, out var dimension4);
					if (dimension3 == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have bone ID data");
					}
					if (format3 != VertexAttributeFormat.UInt32 && format3 != VertexAttributeFormat.UInt8)
					{
						throw new Exception("Cannot cache geometry asset: only UInt32 or UInt8 bone IDs format is supported");
					}
					if (dimension4 != 0 && format4 != VertexAttributeFormat.Float32 && format4 != VertexAttributeFormat.UNorm8)
					{
						throw new Exception("Cannot cache geometry asset: only Float32 or UNorm8 bone weights formats are supported");
					}
					NativeSlice<byte> attributeData3 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.BlendIndices);
					NativeSlice<byte> attributeData4 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.BlendWeight);
					FillBoneData(nativeArray2, nativeArray3, subArray, allVertexCount, attributeData3, dimension3, format3, attributeData4, dimension4, format4, nativeArray.GetSubArray(num * 4, indicesCount * 4), IndexFormat.UInt32, num);
					allVertexCount += vertexCount;
					num += indicesCount;
				}
			}
			else
			{
				MeshVertex.Unpack(m_Positions, dst, m_VertexCount, VertexAttributeFormat.Float32, 3);
				if (m_CacheNormals)
				{
					MeshNormal.Unpack(m_Normals, dst2, m_VertexCount, VertexAttributeFormat.Float32, 3);
				}
				indexFormat = m_IndexFormat;
				nativeArray = m_Indices;
				num = m_IndexCount;
				NativeSlice<byte> boneIds = m_BoneIds;
				NativeSlice<byte> boneInfluences = m_BoneInfluences;
				nativeArray2 = new NativeArray<BoneData>(m_BoneCount, Allocator.Temp);
				nativeArray3 = new NativeArray<int>(num / 3, Allocator.Temp);
				InitializeBones(nativeArray2, num);
				FillBoneData(nativeArray2, nativeArray3, dst.AsNativeArray(), 0, boneIds, 4, VertexAttributeFormat.UInt32, boneInfluences, 4, VertexAttributeFormat.Float32, nativeArray, indexFormat, 0);
			}
			if (num == 0)
			{
				return;
			}
			dynamicBuffer.ResizeUninitialized(num);
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			for (int j = 0; j < m_BoneCount; j++)
			{
				BoneData boneData = nativeArray2[j];
				if (boneData.m_TriangleCount != 0)
				{
					num2 += boneData.m_TriangleCount;
					num4 = math.max(num4, boneData.m_TriangleCount);
					CalculateTreeSize(boneData.m_TriangleCount * 3, boneData.m_Bounds, out var _, out var treeSize, out var _, out var _);
					num3 += treeSize;
					num5 = math.max(num5, treeSize);
				}
				else
				{
					num3++;
				}
			}
			NativeArray<TreeNode> nativeArray4 = new NativeArray<TreeNode>(num5, Allocator.Temp);
			NativeArray<int> nativeArray5 = new NativeArray<int>(num4, Allocator.Temp);
			NativeArray<int> nativeArray6 = new NativeArray<int>(num4 * 3, Allocator.Temp);
			dynamicBuffer.ResizeUninitialized(num2 * 3);
			dynamicBuffer2.ResizeUninitialized(num3);
			int num6 = 0;
			int num7 = m_BoneCount - 1;
			int* ptr = stackalloc int[16];
			for (int k = 0; k < m_BoneCount; k++)
			{
				BoneData boneData2 = nativeArray2[k];
				if (boneData2.m_TriangleCount != 0)
				{
					CalculateTreeSize(boneData2.m_TriangleCount * 3, boneData2.m_Bounds, out var treeDepth2, out var treeSize2, out var sizeFactor2, out var sizeOffset2);
					NativeArray<TreeNode> subArray3 = nativeArray4.GetSubArray(0, treeSize2);
					NativeArray<int> subArray4 = nativeArray5.GetSubArray(0, boneData2.m_TriangleCount);
					NativeArray<int> subArray5 = nativeArray6.GetSubArray(0, boneData2.m_TriangleCount * 3);
					InitializeTree(subArray3, treeSize2);
					if (indexFormat == IndexFormat.UInt32)
					{
						FillIndices(boneData2, nativeArray3, nativeArray.Reinterpret<int>(1), subArray5, k);
					}
					else
					{
						FillIndices(boneData2, nativeArray3, nativeArray.Reinterpret<ushort>(1), subArray5, k);
					}
					FillTreeNodes(subArray3, subArray4, dst.AsNativeArray(), subArray5, sizeOffset2, sizeFactor2, treeDepth2);
					UpdateNodes(subArray3, treeDepth2, ptr);
					NativeArray<MeshIndex> subArray6 = dynamicBuffer.AsNativeArray().GetSubArray(num6, boneData2.m_TriangleCount * 3);
					NativeArray<MeshNode> subArray7 = dynamicBuffer2.AsNativeArray().GetSubArray(num7, ptr[treeDepth2]);
					MeshNode value = subArray7[0];
					FillMeshData(subArray3, subArray4, subArray7, subArray5, subArray6, treeDepth2, ptr);
					for (int l = 0; l < subArray7.Length; l++)
					{
						MeshNode value2 = subArray7[l];
						value2.m_IndexRange += num6;
						value2.m_SubNodes1 = math.select(value2.m_SubNodes1, value2.m_SubNodes1 + num7, value2.m_SubNodes1 != -1);
						value2.m_SubNodes2 = math.select(value2.m_SubNodes2, value2.m_SubNodes2 + num7, value2.m_SubNodes2 != -1);
						subArray7[l] = value2;
					}
					if (num7 != k)
					{
						dynamicBuffer2[k] = subArray7[0];
						subArray7[0] = value;
					}
					num6 += subArray6.Length;
					num7 += subArray7.Length - 1;
				}
				else
				{
					dynamicBuffer2[k] = new MeshNode
					{
						m_IndexRange = num6,
						m_SubNodes1 = -1,
						m_SubNodes2 = -1
					};
				}
			}
			nativeArray5.Dispose();
			nativeArray4.Dispose();
			nativeArray2.Dispose();
			nativeArray3.Dispose();
			dynamicBuffer2.Length = num7 + 1;
			dynamicBuffer2.TrimExcess();
			if (m_Data.IsValid)
			{
				nativeArray.Dispose();
			}
		}
	}

	private struct BoneData
	{
		public Bounds3 m_Bounds;

		public int2 m_TriangleRange;

		public int m_TriangleCount;
	}

	public static Mesh CreateDefaultMesh()
	{
		int num = 36;
		Vector3[] vertices = new Vector3[24];
		Vector3[] normals = new Vector3[24];
		Vector4[] tangents = new Vector4[24];
		Vector2[] uv = new Vector2[24];
		int[] array = new int[num];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(-1f, 0f, 0f), new float3(0f, 1f, 0f));
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, -1f, 0f), new float3(0f, 0f, 1f));
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, 0f, -1f), new float3(1f, 0f, 0f));
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(1f, 0f, 0f), new float3(0f, -1f, 0f));
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, 1f, 0f), new float3(0f, 0f, -1f));
		AddFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, 0f, 1f), new float3(-1f, 0f, 0f));
		return new Mesh
		{
			name = "Default object",
			vertices = vertices,
			normals = normals,
			tangents = tangents,
			uv = uv,
			triangles = array
		};
	}

	public static Mesh CreateDefaultBaseMesh()
	{
		int num = 24;
		Vector3[] vertices = new Vector3[16];
		Vector3[] normals = new Vector3[16];
		Vector4[] tangents = new Vector4[16];
		Vector2[] uv = new Vector2[16];
		int[] array = new int[num];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddBaseFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(-1f, 0f, 0f), new float3(0f, 0f, -1f));
		AddBaseFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, 0f, -1f), new float3(1f, 0f, 0f));
		AddBaseFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(1f, 0f, 0f), new float3(0f, 0f, 1f));
		AddBaseFace(vertices, normals, tangents, uv, array, ref vertexIndex, ref indexIndex, new float3(0f, 0f, 1f), new float3(-1f, 0f, 0f));
		return new Mesh
		{
			name = "Default base",
			vertices = vertices,
			normals = normals,
			tangents = tangents,
			uv = uv,
			triangles = array
		};
	}

	public static JobHandle CacheMeshData(RenderPrefab meshPrefab, GeometryAsset meshData, Entity entity, int boneCount, bool cacheNormals, EntityCommandBuffer commandBuffer)
	{
		if (boneCount != 0)
		{
			return IJobExtensions.Schedule(new CacheProceduralMeshDataJob
			{
				m_Data = meshData.data,
				m_Entity = entity,
				m_BoneCount = boneCount,
				m_CacheNormals = cacheNormals,
				m_CommandBuffer = commandBuffer
			});
		}
		return IJobExtensions.Schedule(new CacheMeshDataJob
		{
			m_Data = meshData.data,
			m_Entity = entity,
			m_MeshBounds = meshPrefab.bounds,
			m_CacheNormals = cacheNormals,
			m_CommandBuffer = commandBuffer
		});
	}

	public static void CacheMeshData(Mesh mesh, Entity entity, bool cacheNormals, EntityCommandBuffer commandBuffer)
	{
		DynamicBuffer<MeshVertex> dynamicBuffer = commandBuffer.AddBuffer<MeshVertex>(entity);
		DynamicBuffer<MeshIndex> dynamicBuffer2 = commandBuffer.AddBuffer<MeshIndex>(entity);
		DynamicBuffer<MeshNormal> dynamicBuffer3 = default(DynamicBuffer<MeshNormal>);
		if (cacheNormals)
		{
			dynamicBuffer3 = commandBuffer.AddBuffer<MeshNormal>(entity);
		}
		Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
		Mesh.MeshData meshData = meshDataArray[0];
		int num = 0;
		int subMeshCount = meshData.subMeshCount;
		for (int i = 0; i < subMeshCount; i++)
		{
			num += meshData.GetSubMesh(i).indexCount;
		}
		dynamicBuffer.ResizeUninitialized(meshData.vertexCount);
		dynamicBuffer2.ResizeUninitialized(num);
		meshData.GetVertices(dynamicBuffer.AsNativeArray().Reinterpret<Vector3>());
		if (cacheNormals)
		{
			dynamicBuffer3.ResizeUninitialized(meshData.vertexCount);
			meshData.GetNormals(dynamicBuffer3.AsNativeArray().Reinterpret<Vector3>());
		}
		num = 0;
		for (int j = 0; j < subMeshCount; j++)
		{
			int indexCount = meshData.GetSubMesh(j).indexCount;
			meshData.GetIndices(dynamicBuffer2.AsNativeArray().GetSubArray(num, indexCount).Reinterpret<int>(), j);
			num += indexCount;
		}
		meshDataArray.Dispose();
	}

	public static void UncacheMeshData(Entity entity, EntityCommandBuffer commandBuffer)
	{
		commandBuffer.RemoveComponent<MeshVertex>(entity);
		commandBuffer.RemoveComponent<MeshNormal>(entity);
		commandBuffer.RemoveComponent<MeshIndex>(entity);
		commandBuffer.RemoveComponent<MeshNode>(entity);
	}

	private static void AddFace(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uv, int[] indices, ref int vertexIndex, ref int indexIndex, float3 normal, float3 tangent)
	{
		float3 @float = math.cross(normal, tangent);
		vertices[vertexIndex] = normal + tangent + @float;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(1f, 0f);
		vertexIndex++;
		vertices[vertexIndex] = normal - tangent + @float;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(0f, 0f);
		vertexIndex++;
		vertices[vertexIndex] = normal - tangent - @float;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(0f, 1f);
		vertexIndex++;
		vertices[vertexIndex] = normal + tangent - @float;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(1f, 1f);
		vertexIndex++;
		indices[indexIndex++] = vertexIndex - 4;
		indices[indexIndex++] = vertexIndex - 3;
		indices[indexIndex++] = vertexIndex - 2;
		indices[indexIndex++] = vertexIndex - 2;
		indices[indexIndex++] = vertexIndex - 1;
		indices[indexIndex++] = vertexIndex - 4;
	}

	private static void AddBaseFace(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uv, int[] indices, ref int vertexIndex, ref int indexIndex, float3 normal, float3 tangent)
	{
		float3 @float = math.cross(normal, tangent) * 0.5f;
		float3 float2 = new float3(0f, -1.5f, 0f);
		vertices[vertexIndex] = normal + tangent + @float + float2;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(1f, 0f);
		vertexIndex++;
		vertices[vertexIndex] = normal - tangent + @float + float2;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(0f, 0f);
		vertexIndex++;
		vertices[vertexIndex] = normal - tangent - @float + float2;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(0f, 1f);
		vertexIndex++;
		vertices[vertexIndex] = normal + tangent - @float + float2;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uv[vertexIndex] = new Vector2(1f, 1f);
		vertexIndex++;
		indices[indexIndex++] = vertexIndex - 4;
		indices[indexIndex++] = vertexIndex - 3;
		indices[indexIndex++] = vertexIndex - 2;
		indices[indexIndex++] = vertexIndex - 2;
		indices[indexIndex++] = vertexIndex - 1;
		indices[indexIndex++] = vertexIndex - 4;
	}

	private static void InitializeBones(NativeArray<BoneData> bones, int indexCount)
	{
		int length = bones.Length;
		int x = indexCount / 3;
		for (int i = 0; i < length; i++)
		{
			bones[i] = new BoneData
			{
				m_Bounds = new Bounds3(float.MaxValue, float.MinValue),
				m_TriangleRange = new int2(x, -1)
			};
		}
	}

	private unsafe static void FillBoneData(NativeArray<BoneData> bones, NativeArray<int> boneIndex, NativeArray<MeshVertex> vertices, int vertexOffset, NativeSlice<byte> boneIdsData, int boneIdsDim, VertexAttributeFormat boneIdsFormat, NativeSlice<byte> weightsData, int weightsDim, VertexAttributeFormat weightsFormat, NativeArray<byte> indexData, IndexFormat indexFormat, int indexOffset)
	{
		int* ptr = (int*)indexData.GetUnsafeReadOnlyPtr();
		ushort* ptr2 = (ushort*)indexData.GetUnsafeReadOnlyPtr();
		bool flag = boneIdsFormat == VertexAttributeFormat.UInt8;
		byte* unsafeReadOnlyPtr = (byte*)boneIdsData.GetUnsafeReadOnlyPtr();
		bool flag2 = weightsFormat == VertexAttributeFormat.UNorm8;
		byte* unsafeReadOnlyPtr2 = (byte*)weightsData.GetUnsafeReadOnlyPtr();
		int num = ((indexFormat == IndexFormat.UInt16) ? 2 : 4);
		int num2 = indexData.Length / (3 * num);
		indexOffset /= 3;
		int3 @int = default(int3);
		for (int i = 0; i < num2; i++)
		{
			if (num == 2)
			{
				@int.x = *ptr2;
				@int.y = ptr2[1];
				@int.z = ptr2[2];
			}
			else
			{
				@int.x = *ptr;
				@int.y = ptr[1];
				@int.z = ptr[2];
			}
			@int -= vertexOffset;
			Triangle3 triangle = new Triangle3(vertices[@int.x].m_Vertex, vertices[@int.y].m_Vertex, vertices[@int.z].m_Vertex);
			int3 int2 = @int * boneIdsDim;
			int3 falseValue = ((!flag) ? new int3(((int*)unsafeReadOnlyPtr)[int2.x], ((int*)unsafeReadOnlyPtr)[int2.y], ((int*)unsafeReadOnlyPtr)[int2.z]) : new int3(unsafeReadOnlyPtr[int2.x], unsafeReadOnlyPtr[int2.y], unsafeReadOnlyPtr[int2.z]));
			int3 int3 = @int * weightsDim;
			falseValue = math.select(test: (weightsDim != 0) ? ((!flag2) ? (new float3(((float*)unsafeReadOnlyPtr2)[int3.x], ((float*)unsafeReadOnlyPtr2)[int3.y], ((float*)unsafeReadOnlyPtr2)[int3.z]) < new float3(0.5f)) : (new float3((int)unsafeReadOnlyPtr2[int3.x], (int)unsafeReadOnlyPtr2[int3.y], (int)unsafeReadOnlyPtr2[int3.z]) < new float3(128))) : ((bool3)false), falseValue: falseValue, trueValue: new int3(-1));
			AddTriangle(bones, boneIndex, triangle, falseValue, indexOffset + i);
			ptr += 3;
			ptr2 += 3;
		}
	}

	private static void AddTriangle(NativeArray<BoneData> bones, NativeArray<int> boneIndex, Triangle3 triangle, int3 boneID, int triangleIndex)
	{
		if (boneID.x >= 0 && boneID.x < bones.Length && math.all(boneID.xx == boneID.yz))
		{
			BoneData value = bones[boneID.x];
			value.m_Bounds |= MathUtils.Bounds(triangle);
			value.m_TriangleRange.x = math.min(value.m_TriangleRange.x, triangleIndex);
			value.m_TriangleRange.y = math.max(value.m_TriangleRange.y, triangleIndex);
			value.m_TriangleCount++;
			bones[boneID.x] = value;
			boneIndex[triangleIndex] = boneID.x;
		}
		else
		{
			boneIndex[triangleIndex] = -1;
		}
	}

	private static void FillIndices(BoneData boneData, NativeArray<int> boneIndex, NativeArray<int> sourceIndices, NativeArray<int> targetIndices, int bone)
	{
		int num = 0;
		for (int i = boneData.m_TriangleRange.x; i <= boneData.m_TriangleRange.y; i++)
		{
			if (boneIndex[i] == bone)
			{
				int3 @int = i * 3 + new int3(0, 1, 2);
				targetIndices[num++] = sourceIndices[@int.x];
				targetIndices[num++] = sourceIndices[@int.y];
				targetIndices[num++] = sourceIndices[@int.z];
			}
		}
	}

	private static void FillIndices(BoneData boneData, NativeArray<int> boneIndex, NativeArray<ushort> sourceIndices, NativeArray<int> targetIndices, int bone)
	{
		int num = 0;
		for (int i = boneData.m_TriangleRange.x; i <= boneData.m_TriangleRange.y; i++)
		{
			if (boneIndex[i] == bone)
			{
				int3 @int = i * 3 + new int3(0, 1, 2);
				targetIndices[num++] = sourceIndices[@int.x];
				targetIndices[num++] = sourceIndices[@int.y];
				targetIndices[num++] = sourceIndices[@int.z];
			}
		}
	}

	private static void CalculateTreeSize(int indexCount, Bounds3 bounds, out int treeDepth, out int treeSize, out float3 sizeFactor, out float3 sizeOffset)
	{
		treeDepth = 1;
		treeSize = 1;
		for (int num = indexCount / 3; num >= 32; num >>= 3)
		{
			treeSize += 1 << 3 * treeDepth++;
		}
		sizeFactor = 1f / math.max(0.001f, MathUtils.Size(bounds));
		sizeOffset = 0.5f - MathUtils.Center(bounds) * sizeFactor;
	}

	private static void InitializeTree(NativeArray<TreeNode> treeNodes, int treeSize)
	{
		for (int i = 0; i < treeSize; i++)
		{
			treeNodes[i] = new TreeNode
			{
				m_Bounds = new Bounds3(float.MaxValue, float.MinValue),
				m_FirstTriangle = -1
			};
		}
	}

	private static void FillTreeNodes(NativeArray<TreeNode> treeNodes, NativeArray<int> nextTriangle, NativeArray<MeshVertex> vertices, NativeArray<int> indices, float3 sizeOffset, float3 sizeFactor, int treeDepth)
	{
		int length = nextTriangle.Length;
		for (int i = 0; i < length; i++)
		{
			int3 @int = i * 3 + new int3(0, 1, 2);
			AddTriangle(triangle: new Triangle3(vertices[indices[@int.x]].m_Vertex, vertices[indices[@int.y]].m_Vertex, vertices[indices[@int.z]].m_Vertex), treeNodes: treeNodes, nextTriangle: nextTriangle, sizeOffset: sizeOffset, sizeFactor: sizeFactor, treeDepth: treeDepth, index: i);
		}
	}

	private static void FillTreeNodes(NativeArray<TreeNode> treeNodes, NativeArray<int> nextTriangle, NativeArray<MeshVertex> vertices, NativeArray<ushort> indices, float3 sizeOffset, float3 sizeFactor, int treeDepth)
	{
		int length = nextTriangle.Length;
		for (int i = 0; i < length; i++)
		{
			int3 @int = i * 3 + new int3(0, 1, 2);
			AddTriangle(triangle: new Triangle3(vertices[indices[@int.x]].m_Vertex, vertices[indices[@int.y]].m_Vertex, vertices[indices[@int.z]].m_Vertex), treeNodes: treeNodes, nextTriangle: nextTriangle, sizeOffset: sizeOffset, sizeFactor: sizeFactor, treeDepth: treeDepth, index: i);
		}
	}

	private static void AddTriangle(NativeArray<TreeNode> treeNodes, NativeArray<int> nextTriangle, float3 sizeOffset, float3 sizeFactor, int treeDepth, Triangle3 triangle, int index)
	{
		Bounds3 bounds = MathUtils.Bounds(triangle);
		float3 @float = MathUtils.Center(bounds) * sizeFactor + sizeOffset;
		float num = math.cmax(MathUtils.Size(bounds) * sizeFactor);
		int num2 = treeDepth - 1;
		int num3 = 0;
		int num4 = 0;
		while (num <= 0.5f && num4 < num2)
		{
			num3 += 1 << 3 * num4++;
			num *= 2f;
		}
		int num5 = 1 << num4;
		int3 x = math.clamp((int3)(@float * num5), 0, num5 - 1);
		num3 += math.dot(x, new int3(1, num5, num5 * num5));
		TreeNode value = treeNodes[num3];
		nextTriangle[index] = value.m_FirstTriangle;
		value.m_Bounds |= bounds;
		value.m_FirstTriangle = index;
		value.m_ItemCount++;
		treeNodes[num3] = value;
	}

	private unsafe static void UpdateNodes(NativeArray<TreeNode> treeNodes, int treeDepth, int* depthOffsets)
	{
		int num = treeDepth - 1;
		int num2 = 0;
		int num3 = 0;
		while (num3 < num)
		{
			num2 += 1 << 3 * num3++;
		}
		int3 x = default(int3);
		while (num3 > 0)
		{
			int num4 = 1 << num3;
			int num5 = num2;
			num2 -= 1 << 3 * --num3;
			int num6 = 1 << num3;
			int num7 = num2;
			int3 y = new int3(2, num4 << 1, num4 * num4 << 1);
			int3 y2 = new int3(1, num6, num6 * num6);
			int4 @int = new int4(0, 1, num4, num4 + 1);
			int4 int2 = num4 * num4 + @int;
			int sourceSize = 0;
			x.z = 0;
			while (x.z < num6)
			{
				x.y = 0;
				while (x.y < num6)
				{
					x.x = 0;
					while (x.x < num6)
					{
						int num8 = num5 + math.dot(x, y);
						int index = num7 + math.dot(x, y2);
						int4 int3 = num8 + @int;
						int4 int4 = num8 + int2;
						TreeNode targetNode = treeNodes[index];
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int3.x);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int3.y);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int3.z);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int3.w);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int4.x);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int4.y);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int4.z);
						AddBounds(ref targetNode, ref sourceSize, treeNodes, int4.w);
						treeNodes[index] = targetNode;
						x.x++;
					}
					x.y++;
				}
				x.z++;
			}
			depthOffsets[num3 + 2] = sourceSize;
		}
		*depthOffsets = 0;
		depthOffsets[1] = 1;
		for (int i = 1; i <= treeDepth; i++)
		{
			depthOffsets[i] += depthOffsets[i - 1];
		}
	}

	private static void AddBounds(ref TreeNode targetNode, ref int sourceSize, NativeArray<TreeNode> treeNodes, int sourceIndex)
	{
		TreeNode value = treeNodes[sourceIndex];
		if (value.m_ItemCount != 0)
		{
			targetNode.m_Bounds |= value.m_Bounds;
			targetNode.m_ItemCount++;
			if (value.m_ItemCount != 1)
			{
				value.m_NodeIndex = sourceSize++;
				treeNodes[sourceIndex] = value;
			}
		}
	}

	private unsafe static void FillMeshData(NativeArray<TreeNode> sourceNodes, NativeArray<int> nextTriangle, NativeArray<MeshNode> targetNodes, NativeArray<int> sourceIndices, NativeArray<MeshIndex> targetIndices, int treeDepth, int* depthOffsets)
	{
		int* ptr = stackalloc int[128];
		int* ptr2 = stackalloc int[128];
		int* ptr3 = stackalloc int[128];
		int num = 1;
		int num2 = 0;
		*ptr = 0;
		*ptr2 = 0;
		*ptr3 = 0;
		while (--num >= 0)
		{
			int num3 = ptr[num];
			int num4 = ptr2[num];
			int num5 = ptr3[num];
			int num6 = 1 << num5;
			TreeNode treeNode = sourceNodes[num3 + num4];
			int x = num2;
			int num7 = treeNode.m_FirstTriangle;
			while (num7 >= 0)
			{
				int3 @int = num7 * 3 + new int3(0, 1, 2);
				int3 int2 = num2 + new int3(0, 1, 2);
				targetIndices[int2.x] = new MeshIndex(sourceIndices[@int.x]);
				targetIndices[int2.y] = new MeshIndex(sourceIndices[@int.y]);
				targetIndices[int2.z] = new MeshIndex(sourceIndices[@int.z]);
				num7 = nextTriangle[num7];
				num2 += 3;
			}
			int num8 = 0;
			int4 subNodes = -1;
			int4 subNodes2 = -1;
			if (num5 + 1 < treeDepth)
			{
				int3 int3 = new int3(num4, num4 >> num5, num4 >> num5 + num5) & (num6 - 1);
				for (int i = 0; i < 8; i++)
				{
					int num9 = num3 + (1 << 3 * num5);
					int num10 = num5 + 1;
					int3 x2 = int3 * 2 + math.select((int3)0, (int3)1, (i & new int3(1, 2, 4)) != 0);
					while (num10 < treeDepth)
					{
						int num11 = 1 << num10;
						int num12 = math.dot(x2, new int3(1, num11, num11 * num11));
						TreeNode treeNode2 = sourceNodes[num9 + num12];
						if (treeNode2.m_ItemCount == 1)
						{
							if (treeNode2.m_FirstTriangle != -1)
							{
								int3 int4 = treeNode2.m_FirstTriangle * 3 + new int3(0, 1, 2);
								int3 int5 = num2 + new int3(0, 1, 2);
								targetIndices[int5.x] = new MeshIndex(sourceIndices[int4.x]);
								targetIndices[int5.y] = new MeshIndex(sourceIndices[int4.y]);
								targetIndices[int5.z] = new MeshIndex(sourceIndices[int4.z]);
								num2 += 3;
								break;
							}
							num9 += 1 << 3 * num10++;
							x2 *= 2;
							continue;
						}
						if (treeNode2.m_ItemCount != 0)
						{
							if (num8 < 4)
							{
								subNodes[num8++] = depthOffsets[num10] + treeNode2.m_NodeIndex;
							}
							else
							{
								subNodes2[num8++ - 4] = depthOffsets[num10] + treeNode2.m_NodeIndex;
							}
							ptr[num] = num9;
							ptr2[num] = num12;
							ptr3[num] = num10;
							num++;
							break;
						}
						if (num10 == num5 + 1)
						{
							break;
						}
						if ((x2.x & 1) == 0)
						{
							x2.x++;
							continue;
						}
						if ((x2.y & 1) == 0)
						{
							x2.xy += new int2(-1, 1);
							continue;
						}
						if ((x2.z & 1) != 0)
						{
							break;
						}
						x2 += new int3(-1, -1, 1);
					}
				}
			}
			int index = depthOffsets[num5] + treeNode.m_NodeIndex;
			targetNodes[index] = new MeshNode
			{
				m_Bounds = treeNode.m_Bounds,
				m_IndexRange = new int2(x, num2),
				m_SubNodes1 = subNodes,
				m_SubNodes2 = subNodes2
			};
		}
	}

	private unsafe static void FillMeshData(NativeArray<TreeNode> sourceNodes, NativeArray<int> nextTriangle, NativeArray<MeshNode> targetNodes, NativeArray<ushort> sourceIndices, NativeArray<MeshIndex> targetIndices, int treeDepth, int* depthOffsets)
	{
		int* ptr = stackalloc int[128];
		int* ptr2 = stackalloc int[128];
		int* ptr3 = stackalloc int[128];
		int num = 1;
		int num2 = 0;
		*ptr = 0;
		*ptr2 = 0;
		*ptr3 = 0;
		while (--num >= 0)
		{
			int num3 = ptr[num];
			int num4 = ptr2[num];
			int num5 = ptr3[num];
			int num6 = 1 << num5;
			TreeNode treeNode = sourceNodes[num3 + num4];
			int x = num2;
			int num7 = treeNode.m_FirstTriangle;
			while (num7 >= 0)
			{
				int3 @int = num7 * 3 + new int3(0, 1, 2);
				int3 int2 = num2 + new int3(0, 1, 2);
				targetIndices[int2.x] = new MeshIndex(sourceIndices[@int.x]);
				targetIndices[int2.y] = new MeshIndex(sourceIndices[@int.y]);
				targetIndices[int2.z] = new MeshIndex(sourceIndices[@int.z]);
				num7 = nextTriangle[num7];
				num2 += 3;
			}
			int num8 = 0;
			int4 subNodes = -1;
			int4 subNodes2 = -1;
			if (num5 + 1 < treeDepth)
			{
				int3 int3 = new int3(num4, num4 >> num5, num4 >> num5 + num5) & (num6 - 1);
				for (int i = 0; i < 8; i++)
				{
					int num9 = num3 + (1 << 3 * num5);
					int num10 = num5 + 1;
					int3 x2 = int3 * 2 + math.select((int3)0, (int3)1, (i & new int3(1, 2, 4)) != 0);
					while (num10 < treeDepth)
					{
						int num11 = 1 << num10;
						int num12 = math.dot(x2, new int3(1, num11, num11 * num11));
						TreeNode treeNode2 = sourceNodes[num9 + num12];
						if (treeNode2.m_ItemCount == 1)
						{
							if (treeNode2.m_FirstTriangle != -1)
							{
								int3 int4 = treeNode2.m_FirstTriangle * 3 + new int3(0, 1, 2);
								int3 int5 = num2 + new int3(0, 1, 2);
								targetIndices[int5.x] = new MeshIndex(sourceIndices[int4.x]);
								targetIndices[int5.y] = new MeshIndex(sourceIndices[int4.y]);
								targetIndices[int5.z] = new MeshIndex(sourceIndices[int4.z]);
								num2 += 3;
								break;
							}
							num9 += 1 << 3 * num10++;
							x2 *= 2;
							continue;
						}
						if (treeNode2.m_ItemCount != 0)
						{
							if (num8 < 4)
							{
								subNodes[num8++] = depthOffsets[num10] + treeNode2.m_NodeIndex;
							}
							else
							{
								subNodes2[num8++ - 4] = depthOffsets[num10] + treeNode2.m_NodeIndex;
							}
							ptr[num] = num9;
							ptr2[num] = num12;
							ptr3[num] = num10;
							num++;
							break;
						}
						if (num10 == num5 + 1)
						{
							break;
						}
						if ((x2.x & 1) == 0)
						{
							x2.x++;
							continue;
						}
						if ((x2.y & 1) == 0)
						{
							x2.xy += new int2(-1, 1);
							continue;
						}
						if ((x2.z & 1) != 0)
						{
							break;
						}
						x2 += new int3(-1, -1, 1);
					}
				}
			}
			int index = depthOffsets[num5] + treeNode.m_NodeIndex;
			targetNodes[index] = new MeshNode
			{
				m_Bounds = treeNode.m_Bounds,
				m_IndexRange = new int2(x, num2),
				m_SubNodes1 = subNodes,
				m_SubNodes2 = subNodes2
			};
		}
	}
}
