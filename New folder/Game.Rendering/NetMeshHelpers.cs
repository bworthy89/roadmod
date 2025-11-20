using System;
using Colossal.IO.AssetDatabase;
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

public static class NetMeshHelpers
{
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
		public NativeSlice<byte> m_Tangents;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeSlice<byte> m_TexCoords0;

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
		public VertexAttributeFormat m_PositionsFormat;

		[ReadOnly]
		public VertexAttributeFormat m_NormalsFormat;

		[ReadOnly]
		public VertexAttributeFormat m_TangentsFormat;

		[ReadOnly]
		public VertexAttributeFormat m_TexCoords0Format;

		[ReadOnly]
		public int m_PositionsDim;

		[ReadOnly]
		public int m_NormalsDim;

		[ReadOnly]
		public int m_TangentsDim;

		[ReadOnly]
		public int m_TexCoords0Dim;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			DynamicBuffer<MeshVertex> dst = m_CommandBuffer.AddBuffer<MeshVertex>(m_Entity);
			DynamicBuffer<MeshNormal> dst2 = m_CommandBuffer.AddBuffer<MeshNormal>(m_Entity);
			DynamicBuffer<MeshTangent> dst3 = m_CommandBuffer.AddBuffer<MeshTangent>(m_Entity);
			DynamicBuffer<MeshUV0> dst4 = m_CommandBuffer.AddBuffer<MeshUV0>(m_Entity);
			DynamicBuffer<MeshIndex> dst5 = m_CommandBuffer.AddBuffer<MeshIndex>(m_Entity);
			if (m_Data.IsValid)
			{
				int allVertexCount = GeometryAsset.GetAllVertexCount(ref m_Data);
				int allIndicesCount = GeometryAsset.GetAllIndicesCount(ref m_Data);
				dst.ResizeUninitialized(allVertexCount);
				dst2.ResizeUninitialized(allVertexCount);
				dst3.ResizeUninitialized(allVertexCount);
				dst4.ResizeUninitialized(allVertexCount);
				dst5.ResizeUninitialized(allIndicesCount);
				allVertexCount = 0;
				allIndicesCount = 0;
				for (int i = 0; i < m_Data.meshCount; i++)
				{
					int vertexCount = GeometryAsset.GetVertexCount(ref m_Data, i);
					int indicesCount = GeometryAsset.GetIndicesCount(ref m_Data, i);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Position, out var format, out var dimension);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Normal, out var format2, out var dimension2);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.Tangent, out var format3, out var dimension3);
					GeometryAsset.GetAttributeFormat(ref m_Data, i, VertexAttribute.TexCoord0, out var format4, out var dimension4);
					IndexFormat indexFormat = GeometryAsset.GetIndexFormat(ref m_Data, i);
					if (dimension == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a position");
					}
					if (dimension2 == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a normal");
					}
					if (dimension3 == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a tangent");
					}
					if (dimension4 == 0)
					{
						throw new Exception("Cannot cache geometry asset: mesh do not have a UV0");
					}
					NativeSlice<byte> attributeData = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Position);
					NativeSlice<byte> attributeData2 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Normal);
					NativeSlice<byte> attributeData3 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.Tangent);
					NativeSlice<byte> attributeData4 = GeometryAsset.GetAttributeData(ref m_Data, i, VertexAttribute.TexCoord0);
					NativeArray<byte> indices = GeometryAsset.GetIndices(ref m_Data, i);
					NativeArray<MeshVertex> subArray = dst.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					NativeArray<MeshNormal> subArray2 = dst2.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					NativeArray<MeshTangent> subArray3 = dst3.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					NativeArray<MeshUV0> subArray4 = dst4.AsNativeArray().GetSubArray(allVertexCount, vertexCount);
					NativeArray<MeshIndex> subArray5 = dst5.AsNativeArray().GetSubArray(allIndicesCount, indicesCount);
					MeshVertex.Unpack(attributeData, subArray, vertexCount, format, dimension);
					MeshNormal.Unpack(attributeData2, subArray2, vertexCount, format2, dimension2);
					MeshTangent.Unpack(attributeData3, subArray3, vertexCount, format3, dimension3);
					MeshUV0.Unpack(attributeData4, subArray4, vertexCount, format4, dimension4);
					MeshIndex.Unpack(indices, subArray5, indicesCount, indexFormat, allVertexCount);
					allVertexCount += vertexCount;
					allIndicesCount += indicesCount;
				}
			}
			else
			{
				MeshVertex.Unpack(m_Positions, dst, m_VertexCount, m_PositionsFormat, m_PositionsDim);
				MeshNormal.Unpack(m_Normals, dst2, m_VertexCount, m_NormalsFormat, m_NormalsDim);
				MeshTangent.Unpack(m_Tangents, dst3, m_VertexCount, m_TangentsFormat, m_TangentsDim);
				MeshUV0.Unpack(m_TexCoords0, dst4, m_VertexCount, m_TexCoords0Format, m_TexCoords0Dim);
				MeshIndex.Unpack(m_Indices, dst5, m_IndexCount, m_IndexFormat);
			}
		}
	}

	private static readonly float3 v_left = new float3(-1f, 0f, 0f);

	private static readonly float3 v_up = new float3(0f, 1f, 0f);

	private static readonly float3 v_right = new float3(1f, 0f, 0f);

	private static readonly float3 v_down = new float3(0f, -1f, 0f);

	private static readonly float3 v_forward = new float3(0f, 0f, 1f);

	private static readonly float3 v_backward = new float3(0f, 0f, -1f);

	public static Mesh CreateDefaultLaneMesh()
	{
		int num = 4;
		int num2 = num * 8 + 16;
		int num3 = num * 24 + 12;
		Vector3[] vertices = new Vector3[num2];
		Vector3[] normals = new Vector3[num2];
		Vector4[] tangents = new Vector4[num2];
		Vector2[] array = new Vector2[num2];
		int[] array2 = new int[num3];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, -1f, -1f), v_backward, v_right, new float2(0f, 0f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, 1f, -1f), v_backward, v_right, new float2(0f, 1f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, 1f, -1f), v_backward, v_right, new float2(1f, 1f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, -1f, -1f), v_backward, v_right, new float2(1f, 0f));
		AddQuad(array2, ref indexIndex, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2, vertexIndex - 1);
		for (int i = 0; i <= num; i++)
		{
			float num4 = (float)i / (float)num;
			float z = num4 * 2f - 1f;
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, -1f, z), v_left, v_up, new float2(0f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, 1f, z), v_left, v_up, new float2(1f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, 1f, z), v_up, v_right, new float2(0f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, 1f, z), v_up, v_right, new float2(1f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, 1f, z), v_right, v_down, new float2(0f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, -1f, z), v_right, v_down, new float2(1f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, -1f, z), v_down, v_left, new float2(0f, num4));
			AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, -1f, z), v_down, v_left, new float2(1f, num4));
			if (i != 0)
			{
				AddQuad(array2, ref indexIndex, vertexIndex - 16, vertexIndex - 8, vertexIndex - 7, vertexIndex - 15);
				AddQuad(array2, ref indexIndex, vertexIndex - 14, vertexIndex - 6, vertexIndex - 5, vertexIndex - 13);
				AddQuad(array2, ref indexIndex, vertexIndex - 12, vertexIndex - 4, vertexIndex - 3, vertexIndex - 11);
				AddQuad(array2, ref indexIndex, vertexIndex - 10, vertexIndex - 2, vertexIndex - 1, vertexIndex - 9);
			}
		}
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, -1f, 1f), v_forward, v_left, new float2(0f, 0f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(1f, 1f, 1f), v_forward, v_left, new float2(0f, 1f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, 1f, 1f), v_forward, v_left, new float2(1f, 1f));
		AddVertex(vertices, normals, tangents, array, ref vertexIndex, new float3(-1f, -1f, 1f), v_forward, v_left, new float2(1f, 0f));
		AddQuad(array2, ref indexIndex, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2, vertexIndex - 1);
		return new Mesh
		{
			name = "Default lane",
			vertices = vertices,
			normals = normals,
			tangents = tangents,
			uv = array,
			triangles = array2
		};
	}

	public static Mesh CreateDefaultRoundaboutMesh()
	{
		int num = 4;
		int num2 = num * 10 + 22;
		int num3 = num * 36 + 24;
		Vector3[] vertices = new Vector3[num2];
		Vector3[] normals = new Vector3[num2];
		Vector4[] tangents = new Vector4[num2];
		Color32[] colors = new Color32[num2];
		Vector4[] uvs = new Vector4[num2];
		int[] indices = new int[num3];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -2f), new int2(0, 4), 0f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -1f), new int2(0, 4), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.5f, -1f), new int2(4, 2), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -1f), new int2(4, 2), 1f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -2f), new int2(4, 2), 1f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.5f, -2f), new int2(4, 2), 0f, -2f, 0f);
		AddQuad(indices, ref indexIndex, vertexIndex - 6, vertexIndex - 5, vertexIndex - 4, vertexIndex - 1);
		AddQuad(indices, ref indexIndex, vertexIndex - 1, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2);
		for (int i = 0; i <= num; i++)
		{
			int3 @int = new int3(0, 4, 2);
			float num4 = (float)i / ((float)num * 0.5f);
			float y = num4 - 3f;
			if (i >= num >> 1)
			{
				@int += 1;
				num4 -= 1f;
			}
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(0f, y), @int.xy, 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(1f, y), @int.xy, 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0f, y), @int.xy, 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0.5f, y), @int.yz, 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(1f, y), @int.yz, 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(0f, y), @int.yz, 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(1f, y), @int.yz, 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0f, y), @int.yz, 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0.5f, y), @int.yz, 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(1f, y), @int.xy, 0f, -2f, num4);
			if (i != 0)
			{
				AddQuad(indices, ref indexIndex, vertexIndex - 20, vertexIndex - 10, vertexIndex - 9, vertexIndex - 19);
				AddQuad(indices, ref indexIndex, vertexIndex - 18, vertexIndex - 8, vertexIndex - 7, vertexIndex - 17);
				AddQuad(indices, ref indexIndex, vertexIndex - 17, vertexIndex - 7, vertexIndex - 6, vertexIndex - 16);
				AddQuad(indices, ref indexIndex, vertexIndex - 15, vertexIndex - 5, vertexIndex - 4, vertexIndex - 14);
				AddQuad(indices, ref indexIndex, vertexIndex - 13, vertexIndex - 3, vertexIndex - 2, vertexIndex - 12);
				AddQuad(indices, ref indexIndex, vertexIndex - 12, vertexIndex - 2, vertexIndex - 1, vertexIndex - 11);
			}
		}
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -2f), new int2(5, 3), 1f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -1f), new int2(5, 3), 1f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.5f, -1f), new int2(5, 3), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -1f), new int2(1, 5), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -2f), new int2(1, 5), 0f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.5f, -2f), new int2(5, 3), 0f, -2f, 1f);
		AddQuad(indices, ref indexIndex, vertexIndex - 6, vertexIndex - 5, vertexIndex - 4, vertexIndex - 1);
		AddQuad(indices, ref indexIndex, vertexIndex - 1, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2);
		return CreateMesh("Default roundabout", vertices, normals, tangents, colors, uvs, indices);
	}

	public static Mesh CreateDefaultNodeMesh()
	{
		int num = 2;
		int num2 = num * 14 + 34;
		int num3 = num * 60 + 48;
		Vector3[] vertices = new Vector3[num2];
		Vector3[] normals = new Vector3[num2];
		Vector4[] tangents = new Vector4[num2];
		Color32[] colors = new Color32[num2];
		Vector4[] uvs = new Vector4[num2];
		int[] indices = new int[num3];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -2f), new int2(0, 2), 0f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -1f), new int2(0, 2), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.25f, -1f), new int2(0, 2), 1f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.5f, -1f), new int2(4, 0), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.75f, -1f), new int2(1, 3), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -1f), new int2(1, 3), 1f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -2f), new int2(1, 3), 1f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.75f, -2f), new int2(1, 3), 0f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.5f, -2f), new int2(4, 0), 0f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0.25f, -2f), new int2(0, 2), 1f, -2f, 0f);
		AddQuad(indices, ref indexIndex, vertexIndex - 10, vertexIndex - 9, vertexIndex - 8, vertexIndex - 1);
		AddQuad(indices, ref indexIndex, vertexIndex - 1, vertexIndex - 8, vertexIndex - 7, vertexIndex - 2);
		AddQuad(indices, ref indexIndex, vertexIndex - 2, vertexIndex - 7, vertexIndex - 6, vertexIndex - 3);
		AddQuad(indices, ref indexIndex, vertexIndex - 3, vertexIndex - 6, vertexIndex - 5, vertexIndex - 4);
		for (int i = 0; i <= num; i++)
		{
			float num4 = (float)i / (float)num;
			float y = num4 - 2f;
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(0f, y), new int2(0, 2), 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(1f, y), new int2(0, 2), 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0f, y), new int2(0, 2), 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0.25f, y), new int2(0, 2), 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0.5f, y), new int2(4, 0), 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0.75f, y), new int2(1, 3), 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(1f, y), new int2(1, 3), 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(0f, y), new int2(1, 3), 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(1f, y), new int2(1, 3), 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0f, y), new int2(1, 3), 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0.25f, y), new int2(1, 3), 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0.5f, y), new int2(4, 0), 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0.75f, y), new int2(0, 2), 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(1f, y), new int2(0, 2), 0f, -2f, num4);
			if (i != 0)
			{
				AddQuad(indices, ref indexIndex, vertexIndex - 28, vertexIndex - 14, vertexIndex - 13, vertexIndex - 27);
				AddQuad(indices, ref indexIndex, vertexIndex - 26, vertexIndex - 12, vertexIndex - 11, vertexIndex - 25);
				AddQuad(indices, ref indexIndex, vertexIndex - 25, vertexIndex - 11, vertexIndex - 10, vertexIndex - 24);
				AddQuad(indices, ref indexIndex, vertexIndex - 24, vertexIndex - 10, vertexIndex - 9, vertexIndex - 23);
				AddQuad(indices, ref indexIndex, vertexIndex - 23, vertexIndex - 9, vertexIndex - 8, vertexIndex - 22);
				AddQuad(indices, ref indexIndex, vertexIndex - 21, vertexIndex - 7, vertexIndex - 6, vertexIndex - 20);
				AddQuad(indices, ref indexIndex, vertexIndex - 19, vertexIndex - 5, vertexIndex - 4, vertexIndex - 18);
				AddQuad(indices, ref indexIndex, vertexIndex - 18, vertexIndex - 4, vertexIndex - 3, vertexIndex - 17);
				AddQuad(indices, ref indexIndex, vertexIndex - 17, vertexIndex - 3, vertexIndex - 2, vertexIndex - 16);
				AddQuad(indices, ref indexIndex, vertexIndex - 16, vertexIndex - 2, vertexIndex - 1, vertexIndex - 15);
			}
		}
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -2f), new int2(1, 3), 1f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -1f), new int2(1, 3), 1f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.25f, -1f), new int2(1, 3), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.5f, -1f), new int2(4, 0), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.75f, -1f), new int2(0, 2), 1f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -1f), new int2(0, 2), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -2f), new int2(0, 2), 0f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.75f, -2f), new int2(0, 2), 1f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.5f, -2f), new int2(4, 0), 0f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0.25f, -2f), new int2(1, 3), 0f, -2f, 1f);
		AddQuad(indices, ref indexIndex, vertexIndex - 10, vertexIndex - 9, vertexIndex - 8, vertexIndex - 1);
		AddQuad(indices, ref indexIndex, vertexIndex - 1, vertexIndex - 8, vertexIndex - 7, vertexIndex - 2);
		AddQuad(indices, ref indexIndex, vertexIndex - 2, vertexIndex - 7, vertexIndex - 6, vertexIndex - 3);
		AddQuad(indices, ref indexIndex, vertexIndex - 3, vertexIndex - 6, vertexIndex - 5, vertexIndex - 4);
		return CreateMesh("Default node", vertices, normals, tangents, colors, uvs, indices);
	}

	public static Mesh CreateDefaultEdgeMesh()
	{
		int num = 4;
		int num2 = num * 8 + 16;
		int num3 = num * 24 + 12;
		Vector3[] vertices = new Vector3[num2];
		Vector3[] normals = new Vector3[num2];
		Vector4[] tangents = new Vector4[num2];
		Color32[] colors = new Color32[num2];
		Vector4[] uvs = new Vector4[num2];
		int[] indices = new int[num3];
		int vertexIndex = 0;
		int indexIndex = 0;
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -2f), new int2(0, 2), 0f, -2f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(0f, -1f), new int2(0, 2), 0f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -1f), new int2(0, 2), 1f, 0f, 0f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_backward, v_right, new float2(1f, -2f), new int2(0, 2), 1f, -2f, 0f);
		AddQuad(indices, ref indexIndex, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2, vertexIndex - 1);
		for (int i = 0; i <= num; i++)
		{
			int2 m = new int2(0, 2);
			float num4 = (float)i / ((float)num * 0.5f);
			float y = num4 - 3f;
			if (i >= num >> 1)
			{
				m += 1;
				num4 -= 1f;
			}
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(0f, y), m, 0f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_left, v_up, new float2(1f, y), m, 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(0f, y), m, 0f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_up, v_right, new float2(1f, y), m, 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(0f, y), m, 1f, 0f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_right, v_down, new float2(1f, y), m, 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(0f, y), m, 1f, -2f, num4);
			AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_down, v_left, new float2(1f, y), m, 0f, -2f, num4);
			if (i != 0)
			{
				AddQuad(indices, ref indexIndex, vertexIndex - 16, vertexIndex - 8, vertexIndex - 7, vertexIndex - 15);
				AddQuad(indices, ref indexIndex, vertexIndex - 14, vertexIndex - 6, vertexIndex - 5, vertexIndex - 13);
				AddQuad(indices, ref indexIndex, vertexIndex - 12, vertexIndex - 4, vertexIndex - 3, vertexIndex - 11);
				AddQuad(indices, ref indexIndex, vertexIndex - 10, vertexIndex - 2, vertexIndex - 1, vertexIndex - 9);
			}
		}
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -2f), new int2(1, 3), 1f, -2f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(0f, -1f), new int2(1, 3), 1f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -1f), new int2(1, 3), 0f, 0f, 1f);
		AddVertex(vertices, normals, tangents, colors, uvs, ref vertexIndex, v_forward, v_left, new float2(1f, -2f), new int2(1, 3), 0f, -2f, 1f);
		AddQuad(indices, ref indexIndex, vertexIndex - 4, vertexIndex - 3, vertexIndex - 2, vertexIndex - 1);
		return CreateMesh("Default edge", vertices, normals, tangents, colors, uvs, indices);
	}

	public static JobHandle CacheMeshData(GeometryAsset meshData, Entity entity, EntityManager entityManager, EntityCommandBuffer commandBuffer)
	{
		DynamicBuffer<MeshMaterial> buffer = entityManager.GetBuffer<MeshMaterial>(entity);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < meshData.meshCount; i++)
		{
			int subMeshCount = meshData.GetSubMeshCount(i);
			for (int j = 0; j < subMeshCount; j++)
			{
				SubMeshDescriptor subMeshDesc = meshData.GetSubMeshDesc(i, j);
				ref MeshMaterial reference = ref buffer.ElementAt(num++);
				reference.m_StartIndex = num2 + subMeshDesc.indexStart;
				reference.m_IndexCount = subMeshDesc.indexCount;
				reference.m_StartVertex = num3 + subMeshDesc.firstVertex;
				reference.m_VertexCount = subMeshDesc.vertexCount;
			}
			num2 += meshData.GetIndicesCount(i);
			num3 += meshData.GetVertexCount(i);
		}
		return IJobExtensions.Schedule(new CacheMeshDataJob
		{
			m_Data = meshData.data,
			m_Entity = entity,
			m_CommandBuffer = commandBuffer
		});
	}

	public static void CacheMeshData(Mesh mesh, Entity entity, EntityManager entityManager, EntityCommandBuffer commandBuffer)
	{
		DynamicBuffer<MeshVertex> dynamicBuffer = commandBuffer.AddBuffer<MeshVertex>(entity);
		DynamicBuffer<MeshNormal> dynamicBuffer2 = commandBuffer.AddBuffer<MeshNormal>(entity);
		DynamicBuffer<MeshTangent> dynamicBuffer3 = commandBuffer.AddBuffer<MeshTangent>(entity);
		DynamicBuffer<MeshUV0> dynamicBuffer4 = commandBuffer.AddBuffer<MeshUV0>(entity);
		DynamicBuffer<MeshIndex> dynamicBuffer5 = commandBuffer.AddBuffer<MeshIndex>(entity);
		DynamicBuffer<MeshMaterial> buffer = entityManager.GetBuffer<MeshMaterial>(entity);
		Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
		Mesh.MeshData meshData = meshDataArray[0];
		int num = 0;
		int subMeshCount = meshData.subMeshCount;
		for (int i = 0; i < subMeshCount; i++)
		{
			num += meshData.GetSubMesh(i).indexCount;
		}
		dynamicBuffer.ResizeUninitialized(meshData.vertexCount);
		dynamicBuffer2.ResizeUninitialized(meshData.vertexCount);
		dynamicBuffer3.ResizeUninitialized(meshData.vertexCount);
		dynamicBuffer4.ResizeUninitialized(meshData.vertexCount);
		dynamicBuffer5.ResizeUninitialized(num);
		meshData.GetVertices(dynamicBuffer.AsNativeArray().Reinterpret<Vector3>());
		meshData.GetNormals(dynamicBuffer2.AsNativeArray().Reinterpret<Vector3>());
		meshData.GetTangents(dynamicBuffer3.AsNativeArray().Reinterpret<Vector4>());
		meshData.GetUVs(0, dynamicBuffer4.AsNativeArray().Reinterpret<Vector2>());
		num = 0;
		for (int j = 0; j < subMeshCount; j++)
		{
			SubMeshDescriptor subMesh = meshData.GetSubMesh(j);
			meshData.GetIndices(dynamicBuffer5.AsNativeArray().GetSubArray(num, subMesh.indexCount).Reinterpret<int>(), j);
			MeshMaterial value = buffer[j];
			value.m_StartIndex = subMesh.indexStart;
			value.m_IndexCount = subMesh.indexCount;
			value.m_StartVertex = subMesh.firstVertex;
			value.m_VertexCount = subMesh.vertexCount;
			buffer[j] = value;
			num += subMesh.indexCount;
		}
		meshDataArray.Dispose();
	}

	public static void UncacheMeshData(Entity entity, EntityCommandBuffer commandBuffer)
	{
		commandBuffer.RemoveComponent<MeshVertex>(entity);
		commandBuffer.RemoveComponent<MeshNormal>(entity);
		commandBuffer.RemoveComponent<MeshTangent>(entity);
		commandBuffer.RemoveComponent<MeshUV0>(entity);
		commandBuffer.RemoveComponent<MeshIndex>(entity);
	}

	private static void AddVertex(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, ref int vertexIndex, float3 position, float3 normal, float3 tangent, float2 uv)
	{
		vertices[vertexIndex] = position;
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		uvs[vertexIndex] = uv;
		vertexIndex++;
	}

	private static void AddQuad(int[] indices, ref int indexIndex, int a, int b, int c, int d)
	{
		indices[indexIndex++] = a;
		indices[indexIndex++] = b;
		indices[indexIndex++] = c;
		indices[indexIndex++] = c;
		indices[indexIndex++] = d;
		indices[indexIndex++] = a;
	}

	private static Mesh CreateMesh(string name, Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Color32[] colors, Vector4[] uvs, int[] indices)
	{
		Mesh mesh = new Mesh();
		mesh.name = name;
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.tangents = tangents;
		mesh.colors32 = colors;
		mesh.SetUVs(0, uvs);
		mesh.triangles = indices;
		mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
		return mesh;
	}

	private static void AddVertex(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Color32[] colors, Vector4[] uvs, ref int vertexIndex, float3 normal, float3 tangent, float2 uv, int2 m, float tx, float y, float tz)
	{
		vertices[vertexIndex] = new Vector3(tx, y, tz);
		normals[vertexIndex] = normal;
		tangents[vertexIndex] = new float4(tangent, -1f);
		colors[vertexIndex] = new Color32((byte)m.x, (byte)m.y, 0, 0);
		uvs[vertexIndex] = new Vector4(uv.x, uv.y, 0.5f, 0f);
		vertexIndex++;
	}
}
