using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering;

public static class BatchMeshHelpers
{
	[BurstCompile]
	private struct GenerateBatchMeshJob : IJobParallelFor
	{
		private struct BasePoint
		{
			public float2 m_Position;

			public float2 m_Direction;

			public float2 m_PrevPos;

			public float m_Distance;
		}

		private struct BaseLine
		{
			public int m_StartIndex;

			public int m_EndIndex;
		}

		private struct VertexData
		{
			public float3 m_Position;

			public uint m_Normal;

			public uint m_Tangent;

			public Color32 m_Color;

			public half4 m_UV0;
		}

		[ReadOnly]
		public NativeList<Entity> m_Entities;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> m_CompositionMeshData;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> m_CompositionPieces;

		[ReadOnly]
		public BufferLookup<MeshVertex> m_MeshVertices;

		[ReadOnly]
		public BufferLookup<MeshNormal> m_MeshNormals;

		[ReadOnly]
		public BufferLookup<MeshTangent> m_MeshTangents;

		[ReadOnly]
		public BufferLookup<MeshUV0> m_MeshUV0s;

		[ReadOnly]
		public BufferLookup<MeshIndex> m_MeshIndices;

		[ReadOnly]
		public BufferLookup<MeshNode> m_MeshNodes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshMaterial> m_MeshMaterials;

		public Mesh.MeshDataArray m_MeshDataArray;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			NetCompositionMeshData componentData2;
			if (m_MeshData.TryGetComponent(entity, out var componentData))
			{
				DynamicBuffer<MeshVertex> cachedVertices = m_MeshVertices[entity];
				DynamicBuffer<MeshIndex> cachedIndices = m_MeshIndices[entity];
				DynamicBuffer<MeshNormal> cachedNormals = default(DynamicBuffer<MeshNormal>);
				if (m_MeshNormals.HasBuffer(entity))
				{
					cachedNormals = m_MeshNormals[entity];
				}
				DynamicBuffer<MeshNode> nodes = default(DynamicBuffer<MeshNode>);
				if (m_MeshNodes.HasBuffer(entity))
				{
					nodes = m_MeshNodes[entity];
				}
				GenerateObjectMesh(componentData, cachedVertices, cachedNormals, cachedIndices, nodes, m_MeshDataArray[index]);
			}
			else if (m_CompositionMeshData.TryGetComponent(entity, out componentData2))
			{
				DynamicBuffer<NetCompositionPiece> pieces = m_CompositionPieces[entity];
				DynamicBuffer<MeshMaterial> materials = m_MeshMaterials[entity];
				GenerateCompositionMesh(componentData2, pieces, materials, m_MeshDataArray[index]);
			}
		}

		private int GetMaterial(DynamicBuffer<MeshMaterial> materials, MeshMaterial pieceMaterial)
		{
			int length = materials.Length;
			for (int i = 0; i < length; i++)
			{
				if (materials[i].m_MaterialIndex == pieceMaterial.m_MaterialIndex)
				{
					return i;
				}
			}
			materials.Add(new MeshMaterial
			{
				m_MaterialIndex = pieceMaterial.m_MaterialIndex
			});
			return length;
		}

		private void GenerateObjectMesh(MeshData objectMeshData, DynamicBuffer<MeshVertex> cachedVertices, DynamicBuffer<MeshNormal> cachedNormals, DynamicBuffer<MeshIndex> cachedIndices, DynamicBuffer<MeshNode> nodes, Mesh.MeshData meshData)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			NativeList<BasePoint> basePoints = default(NativeList<BasePoint>);
			NativeList<BaseLine> baseLines = default(NativeList<BaseLine>);
			float baseOffset = 0f;
			if ((objectMeshData.m_State & MeshFlags.Base) != 0)
			{
				basePoints = new NativeList<BasePoint>(100, Allocator.Temp);
				baseLines = new NativeList<BaseLine>(100, Allocator.Temp);
				baseOffset = math.select(0f, objectMeshData.m_Bounds.min.y, (objectMeshData.m_State & MeshFlags.MinBounds) != 0);
				AddBaseLines(basePoints, baseLines, cachedVertices, cachedNormals, cachedIndices, nodes, baseOffset);
				num += basePoints.Length * 2;
				num2 += baseLines.Length * 6;
				num3++;
			}
			NativeArray<VertexAttributeDescriptor> nativeArray = new NativeArray<VertexAttributeDescriptor>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			SetupGeneratedMeshAttributes(nativeArray);
			bool flag = num2 >= 65536;
			meshData.SetVertexBufferParams(num, nativeArray);
			meshData.SetIndexBufferParams(num2, flag ? IndexFormat.UInt32 : IndexFormat.UInt16);
			nativeArray.Dispose();
			meshData.subMeshCount = num3;
			num = 0;
			num2 = 0;
			num3 = 0;
			if ((objectMeshData.m_State & MeshFlags.Base) != 0)
			{
				meshData.SetSubMesh(num3++, new SubMeshDescriptor
				{
					firstVertex = num,
					indexStart = num2,
					vertexCount = basePoints.Length * 2,
					indexCount = baseLines.Length * 6,
					topology = MeshTopology.Triangles
				}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			}
			NativeArray<VertexData> vertexData = meshData.GetVertexData<VertexData>();
			NativeArray<uint> indices = default(NativeArray<uint>);
			NativeArray<ushort> indices2 = default(NativeArray<ushort>);
			if (flag)
			{
				indices = meshData.GetIndexData<uint>();
			}
			else
			{
				indices2 = meshData.GetIndexData<ushort>();
			}
			num = 0;
			num2 = 0;
			if ((objectMeshData.m_State & MeshFlags.Base) != 0)
			{
				AddBaseVertices(basePoints, baseLines, vertexData, indices, indices2, flag, baseOffset, ref num, ref num2);
				basePoints.Dispose();
				baseLines.Dispose();
			}
		}

		private void AddBaseVertices(NativeList<BasePoint> basePoints, NativeList<BaseLine> baseLines, NativeArray<VertexData> vertices, NativeArray<uint> indices32, NativeArray<ushort> indices16, bool use32bitIndices, float baseOffset, ref int vertexCount, ref int indexCount)
		{
			for (int i = 0; i < baseLines.Length; i++)
			{
				BaseLine baseLine = baseLines[i];
				int num = vertexCount + baseLine.m_StartIndex * 2;
				int num2 = vertexCount + baseLine.m_EndIndex * 2;
				if (use32bitIndices)
				{
					indices32[indexCount++] = (uint)num;
					indices32[indexCount++] = (uint)(num + 1);
					indices32[indexCount++] = (uint)num2;
					indices32[indexCount++] = (uint)num2;
					indices32[indexCount++] = (uint)(num + 1);
					indices32[indexCount++] = (uint)(num2 + 1);
				}
				else
				{
					indices16[indexCount++] = (ushort)num;
					indices16[indexCount++] = (ushort)(num + 1);
					indices16[indexCount++] = (ushort)num2;
					indices16[indexCount++] = (ushort)num2;
					indices16[indexCount++] = (ushort)(num + 1);
					indices16[indexCount++] = (ushort)(num2 + 1);
				}
			}
			for (int j = 0; j < basePoints.Length; j++)
			{
				BasePoint basePoint = basePoints[j];
				float3 n = new float3(basePoint.m_Direction.x, 0f, basePoint.m_Direction.y);
				float4 t = new float4(n.z, 0f, 0f - n.x, 1f);
				uint normal = MathUtils.NormalToOctahedral(n);
				uint tangent = MathUtils.TangentToOctahedral(t);
				vertices[vertexCount++] = new VertexData
				{
					m_Position = new float3(basePoint.m_Position.x, baseOffset, basePoint.m_Position.y),
					m_Color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
					m_Normal = normal,
					m_Tangent = tangent,
					m_UV0 = new half4(new float4(basePoint.m_Distance, 1f, 0f, 0f))
				};
				vertices[vertexCount++] = new VertexData
				{
					m_Position = new float3(basePoint.m_Position.x, baseOffset - 1f, basePoint.m_Position.y),
					m_Color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
					m_Normal = normal,
					m_Tangent = tangent,
					m_UV0 = new half4(new float4(basePoint.m_Distance, 0f, 0f, 0f))
				};
			}
		}

		private unsafe void AddBaseLines(NativeList<BasePoint> basePoints, NativeList<BaseLine> baseLines, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshNormal> normals, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, float baseOffset)
		{
			NativeHashSet<float4> nativeHashSet = new NativeHashSet<float4>(100, Allocator.Temp);
			NativeHashMap<float2, int> nativeHashMap = new NativeHashMap<float2, int>(100, Allocator.Temp);
			NativeList<int> nativeList = new NativeList<int>(100, Allocator.Temp);
			float2 @float = new float2(baseOffset - 0.01f, baseOffset + 0.01f);
			int* ptr = stackalloc int[128];
			int num = 0;
			if (nodes.IsCreated && nodes.Length != 0)
			{
				ptr[num++] = 0;
			}
			while (--num >= 0)
			{
				int index = ptr[num];
				MeshNode meshNode = nodes[index];
				if (!(meshNode.m_Bounds.min.y < @float.y) || !(meshNode.m_Bounds.max.y > @float.x))
				{
					continue;
				}
				for (int i = meshNode.m_IndexRange.x; i < meshNode.m_IndexRange.y; i += 3)
				{
					int3 @int = new int3(i, i + 1, i + 2);
					@int = new int3(indices[@int.x].m_Index, indices[@int.y].m_Index, indices[@int.z].m_Index);
					Triangle3 triangle = new Triangle3(vertices[@int.x].m_Vertex, vertices[@int.y].m_Vertex, vertices[@int.z].m_Vertex);
					bool3 @bool = (triangle.y.abc > @float.x) & (triangle.y.abc < @float.y);
					bool3 x = (triangle.y.abc < @float.y) & @bool.yzx & @bool.zxy;
					if (math.any(x))
					{
						if (x.x)
						{
							nativeHashSet.Add(new float4(triangle.b.xz, triangle.c.xz));
						}
						if (x.y)
						{
							nativeHashSet.Add(new float4(triangle.c.xz, triangle.a.xz));
						}
						if (x.z)
						{
							nativeHashSet.Add(new float4(triangle.a.xz, triangle.b.xz));
						}
					}
				}
				ptr[num] = meshNode.m_SubNodes1.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.x != -1);
				ptr[num] = meshNode.m_SubNodes1.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.y != -1);
				ptr[num] = meshNode.m_SubNodes1.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.z != -1);
				ptr[num] = meshNode.m_SubNodes1.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.w != -1);
				ptr[num] = meshNode.m_SubNodes2.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.x != -1);
				ptr[num] = meshNode.m_SubNodes2.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.y != -1);
				ptr[num] = meshNode.m_SubNodes2.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.z != -1);
				ptr[num] = meshNode.m_SubNodes2.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.w != -1);
			}
			num = 0;
			if (nodes.IsCreated && nodes.Length != 0)
			{
				ptr[num++] = 0;
			}
			while (--num >= 0)
			{
				int index2 = ptr[num];
				MeshNode meshNode2 = nodes[index2];
				if (!(meshNode2.m_Bounds.min.y < @float.y) || !(meshNode2.m_Bounds.max.y > @float.x))
				{
					continue;
				}
				for (int j = meshNode2.m_IndexRange.x; j < meshNode2.m_IndexRange.y; j += 3)
				{
					int3 int2 = new int3(j, j + 1, j + 2);
					int2 = new int3(indices[int2.x].m_Index, indices[int2.y].m_Index, indices[int2.z].m_Index);
					Triangle3 triangle2 = new Triangle3(vertices[int2.x].m_Vertex, vertices[int2.y].m_Vertex, vertices[int2.z].m_Vertex);
					bool3 bool2 = (triangle2.y.abc > @float.x) & (triangle2.y.abc < @float.y);
					bool3 x2 = (triangle2.y.abc >= @float.y) & bool2.yzx & bool2.zxy;
					if (!math.any(x2))
					{
						continue;
					}
					BasePoint value;
					BasePoint value2;
					if (x2.x)
					{
						value = new BasePoint
						{
							m_Position = triangle2.b.xz,
							m_Direction = normals[int2.y].m_Normal.xz
						};
						value2 = new BasePoint
						{
							m_Position = triangle2.c.xz,
							m_Direction = normals[int2.z].m_Normal.xz
						};
					}
					else if (x2.y)
					{
						value = new BasePoint
						{
							m_Position = triangle2.c.xz,
							m_Direction = normals[int2.z].m_Normal.xz
						};
						value2 = new BasePoint
						{
							m_Position = triangle2.a.xz,
							m_Direction = normals[int2.x].m_Normal.xz
						};
					}
					else
					{
						value = new BasePoint
						{
							m_Position = triangle2.a.xz,
							m_Direction = normals[int2.x].m_Normal.xz
						};
						value2 = new BasePoint
						{
							m_Position = triangle2.b.xz,
							m_Direction = normals[int2.y].m_Normal.xz
						};
					}
					if (nativeHashSet.Contains(new float4(value.m_Position, value2.m_Position)) || nativeHashSet.Contains(new float4(value2.m_Position, value.m_Position)))
					{
						continue;
					}
					value.m_Distance = -1f;
					value2.m_Distance = -1f;
					value.m_PrevPos = float.NaN;
					value2.m_PrevPos = value.m_Position;
					if (nativeHashMap.TryGetValue(value.m_Position, out var item))
					{
						item = math.select(item, -1 - item, item < 0);
						if (!basePoints[item].m_Direction.Equals(value.m_Direction))
						{
							item = basePoints.Length;
							basePoints.Add(in value);
						}
					}
					else
					{
						item = basePoints.Length;
						nativeHashMap.Add(value.m_Position, item);
						basePoints.Add(in value);
					}
					if (nativeHashMap.TryGetValue(value2.m_Position, out var item2))
					{
						item2 = math.select(item2, -1 - item2, item2 < 0);
						ref BasePoint reference = ref basePoints.ElementAt(item2);
						if (!reference.m_Direction.Equals(value2.m_Direction))
						{
							item2 = basePoints.Length;
							basePoints.Add(in value2);
						}
						else
						{
							reference.m_PrevPos = value.m_Position;
						}
						nativeHashMap[value2.m_Position] = -1 - item2;
					}
					else
					{
						item2 = basePoints.Length;
						nativeHashMap.Add(value2.m_Position, -1 - item2);
						basePoints.Add(in value2);
					}
					baseLines.Add(new BaseLine
					{
						m_StartIndex = item,
						m_EndIndex = item2
					});
				}
				ptr[num] = meshNode2.m_SubNodes1.x;
				num = math.select(num, num + 1, meshNode2.m_SubNodes1.x != -1);
				ptr[num] = meshNode2.m_SubNodes1.y;
				num = math.select(num, num + 1, meshNode2.m_SubNodes1.y != -1);
				ptr[num] = meshNode2.m_SubNodes1.z;
				num = math.select(num, num + 1, meshNode2.m_SubNodes1.z != -1);
				ptr[num] = meshNode2.m_SubNodes1.w;
				num = math.select(num, num + 1, meshNode2.m_SubNodes1.w != -1);
				ptr[num] = meshNode2.m_SubNodes2.x;
				num = math.select(num, num + 1, meshNode2.m_SubNodes2.x != -1);
				ptr[num] = meshNode2.m_SubNodes2.y;
				num = math.select(num, num + 1, meshNode2.m_SubNodes2.y != -1);
				ptr[num] = meshNode2.m_SubNodes2.z;
				num = math.select(num, num + 1, meshNode2.m_SubNodes2.z != -1);
				ptr[num] = meshNode2.m_SubNodes2.w;
				num = math.select(num, num + 1, meshNode2.m_SubNodes2.w != -1);
			}
			for (int k = 0; k < baseLines.Length; k++)
			{
				BaseLine value3 = baseLines[k];
				ref BasePoint reference2 = ref basePoints.ElementAt(value3.m_StartIndex);
				ref BasePoint reference3 = ref basePoints.ElementAt(value3.m_EndIndex);
				if (reference2.m_Distance < 0f)
				{
					int num2 = nativeHashMap[reference2.m_Position];
					if (num2 >= 0)
					{
						reference2.m_Distance = 0f;
					}
					else
					{
						num2 = -1 - num2;
						ref BasePoint reference4 = ref basePoints.ElementAt(num2);
						if (reference4.m_Distance < 0f)
						{
							int num3 = num2;
							if (!math.isnan(reference4.m_PrevPos.x))
							{
								for (int l = 0; l <= basePoints.Length; l++)
								{
									int num4 = nativeHashMap[reference4.m_PrevPos];
									if (num4 >= 0)
									{
										num2 = num4;
										reference4 = ref basePoints.ElementAt(num2);
										break;
									}
									nativeList.Add(in num2);
									num2 = -1 - num4;
									reference4 = ref basePoints.ElementAt(num2);
									if (reference4.m_Distance >= 0f || num2 == num3 || math.isnan(reference4.m_PrevPos.x))
									{
										break;
									}
								}
							}
							if (reference4.m_Distance < 0f)
							{
								reference4.m_Distance = 0f;
							}
							for (int num5 = nativeList.Length - 1; num5 >= 0; num5--)
							{
								num2 = nativeList[num5];
								ref BasePoint reference5 = ref basePoints.ElementAt(num2);
								if (num5 != 0 || num2 != num3)
								{
									reference5.m_Distance = reference4.m_Distance + math.distance(reference4.m_Position, reference5.m_Position);
								}
								reference4 = ref reference5;
							}
							nativeList.Clear();
						}
						reference2.m_Distance = reference4.m_Distance;
					}
				}
				float num6 = reference2.m_Distance + math.distance(reference2.m_Position, reference3.m_Position);
				if (reference3.m_Distance >= 0f && num6 != reference3.m_Distance)
				{
					BasePoint value4 = reference3;
					value4.m_Distance = num6;
					value3.m_EndIndex = basePoints.Length;
					basePoints.Add(in value4);
					baseLines[k] = value3;
				}
				else
				{
					reference3.m_Distance = num6;
				}
			}
			nativeHashSet.Dispose();
			nativeHashMap.Dispose();
			nativeList.Dispose();
		}

		private void GenerateCompositionMesh(NetCompositionMeshData compositionMeshData, DynamicBuffer<NetCompositionPiece> pieces, DynamicBuffer<MeshMaterial> materials, Mesh.MeshData meshData)
		{
			bool flag = (compositionMeshData.m_Flags.m_General & CompositionFlags.General.Node) != 0;
			bool flag2 = (compositionMeshData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			float middleOffset = compositionMeshData.m_MiddleOffset;
			for (int i = 0; i < materials.Length; i++)
			{
				MeshMaterial value = materials[i];
				value.m_StartVertex = 0;
				value.m_StartIndex = 0;
				value.m_VertexCount = 0;
				value.m_IndexCount = 0;
				materials[i] = value;
			}
			float3 @float = default(float3);
			float3 float2 = default(float3);
			for (int j = 0; j < pieces.Length; j++)
			{
				NetCompositionPiece netCompositionPiece = pieces[j];
				bool flag3 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0;
				bool flag4 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.FlipMesh) != 0;
				bool flag5 = (netCompositionPiece.m_SectionFlags & NetSectionFlags.HalfLength) != 0;
				bool flag6 = (netCompositionPiece.m_PieceFlags & NetPieceFlags.PreserveShape) != 0;
				bool flag7 = (flag && !flag2 && !flag6) || flag5;
				bool flag8 = (netCompositionPiece.m_PieceFlags & NetPieceFlags.SkipBottomHalf) != 0;
				if (!m_MeshVertices.HasBuffer(netCompositionPiece.m_Piece))
				{
					continue;
				}
				DynamicBuffer<MeshVertex> dynamicBuffer = m_MeshVertices[netCompositionPiece.m_Piece];
				DynamicBuffer<MeshIndex> dynamicBuffer2 = m_MeshIndices[netCompositionPiece.m_Piece];
				DynamicBuffer<MeshMaterial> dynamicBuffer3 = m_MeshMaterials[netCompositionPiece.m_Piece];
				for (int k = 0; k < dynamicBuffer3.Length; k++)
				{
					MeshMaterial pieceMaterial = dynamicBuffer3[k];
					int material = GetMaterial(materials, pieceMaterial);
					MeshMaterial value2 = materials[material];
					value2.m_VertexCount += pieceMaterial.m_VertexCount;
					value2.m_IndexCount += pieceMaterial.m_IndexCount;
					num2 = math.max(num2, pieceMaterial.m_VertexCount);
					if (flag7)
					{
						for (int l = 0; l < pieceMaterial.m_VertexCount; l++)
						{
							value2.m_VertexCount -= math.select(1, 0, dynamicBuffer[pieceMaterial.m_StartVertex + l].m_Vertex.z >= -0.01f);
						}
						int3 @int = pieceMaterial.m_StartIndex + math.select(new int3(0, 1, 2), new int3(2, 1, 0), flag3 != flag4);
						for (int m = 0; m < pieceMaterial.m_IndexCount; m += 3)
						{
							int3 int2 = m + @int;
							@float.x = dynamicBuffer[dynamicBuffer2[int2.x].m_Index].m_Vertex.z;
							@float.y = dynamicBuffer[dynamicBuffer2[int2.y].m_Index].m_Vertex.z;
							@float.z = dynamicBuffer[dynamicBuffer2[int2.z].m_Index].m_Vertex.z;
							value2.m_IndexCount -= math.select(3, 0, math.all(@float >= -0.01f));
						}
					}
					else if (flag8)
					{
						for (int n = 0; n < pieceMaterial.m_VertexCount; n++)
						{
							value2.m_VertexCount -= math.select(1, 0, dynamicBuffer[pieceMaterial.m_StartVertex + n].m_Vertex.z <= 0.01f);
						}
						int3 int3 = pieceMaterial.m_StartIndex + math.select(new int3(0, 1, 2), new int3(2, 1, 0), flag3 != flag4);
						for (int num4 = 0; num4 < pieceMaterial.m_IndexCount; num4 += 3)
						{
							int3 int4 = num4 + int3;
							float2.x = dynamicBuffer[dynamicBuffer2[int4.x].m_Index].m_Vertex.z;
							float2.y = dynamicBuffer[dynamicBuffer2[int4.y].m_Index].m_Vertex.z;
							float2.z = dynamicBuffer[dynamicBuffer2[int4.z].m_Index].m_Vertex.z;
							value2.m_IndexCount -= math.select(3, 0, math.all(float2 >= -0.01f));
						}
					}
					materials[material] = value2;
				}
			}
			for (int num5 = 0; num5 < materials.Length; num5++)
			{
				MeshMaterial value3 = materials[num5];
				value3.m_StartVertex = num;
				value3.m_StartIndex = num3;
				num += value3.m_VertexCount;
				num3 += value3.m_IndexCount;
				materials[num5] = value3;
			}
			NativeArray<VertexAttributeDescriptor> nativeArray = new NativeArray<VertexAttributeDescriptor>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			SetupGeneratedMeshAttributes(nativeArray);
			bool flag9 = num3 >= 65536;
			meshData.SetVertexBufferParams(num, nativeArray);
			meshData.SetIndexBufferParams(num3, flag9 ? IndexFormat.UInt32 : IndexFormat.UInt16);
			nativeArray.Dispose();
			meshData.subMeshCount = materials.Length;
			for (int num6 = 0; num6 < materials.Length; num6++)
			{
				MeshMaterial value4 = materials[num6];
				meshData.SetSubMesh(num6, new SubMeshDescriptor
				{
					firstVertex = value4.m_StartVertex,
					indexStart = value4.m_StartIndex,
					vertexCount = value4.m_VertexCount,
					indexCount = value4.m_IndexCount,
					topology = MeshTopology.Triangles
				}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
				value4.m_VertexCount = 0;
				value4.m_IndexCount = 0;
				materials[num6] = value4;
			}
			NativeArray<VertexData> vertexData = meshData.GetVertexData<VertexData>();
			NativeArray<uint> nativeArray2 = default(NativeArray<uint>);
			NativeArray<ushort> nativeArray3 = default(NativeArray<ushort>);
			if (flag9)
			{
				nativeArray2 = meshData.GetIndexData<uint>();
			}
			else
			{
				nativeArray3 = meshData.GetIndexData<ushort>();
			}
			NativeArray<int> nativeArray4 = new NativeArray<int>(num2, Allocator.Temp);
			NativeArray<Bounds1> heightBounds = default(NativeArray<Bounds1>);
			float num7 = compositionMeshData.m_Width * 0.5f + middleOffset;
			float num8 = compositionMeshData.m_Width * 0.5f - middleOffset;
			for (int num9 = 0; num9 < pieces.Length; num9++)
			{
				NetCompositionPiece compositionPiece = pieces[num9];
				if (!m_MeshVertices.HasBuffer(compositionPiece.m_Piece))
				{
					continue;
				}
				bool flag10 = (compositionPiece.m_SectionFlags & NetSectionFlags.Invert) != 0;
				bool flag11 = (compositionPiece.m_SectionFlags & NetSectionFlags.FlipMesh) != 0;
				bool flag12 = (compositionPiece.m_SectionFlags & NetSectionFlags.Median) != 0;
				bool flag13 = (compositionPiece.m_SectionFlags & NetSectionFlags.Right) != 0;
				bool flag14 = (compositionPiece.m_SectionFlags & NetSectionFlags.HalfLength) != 0;
				bool flag15 = (compositionPiece.m_PieceFlags & NetPieceFlags.PreserveShape) != 0;
				bool flag16 = (compositionPiece.m_PieceFlags & NetPieceFlags.DisableTiling) != 0;
				bool flag17 = (compositionPiece.m_PieceFlags & NetPieceFlags.LowerBottomToTerrain) != 0;
				bool flag18 = (compositionPiece.m_PieceFlags & NetPieceFlags.RaiseTopToTerrain) != 0;
				bool flag19 = (compositionPiece.m_PieceFlags & NetPieceFlags.SmoothTopNormal) != 0;
				bool flag20 = (flag && !flag2 && !flag15) || flag14;
				bool flag21 = (compositionPiece.m_PieceFlags & NetPieceFlags.SkipBottomHalf) != 0;
				bool test = compositionPiece.m_Size.x == 0f;
				float4 float3 = math.select(new float4(-1f, 1f, -1f, 1f), new float4(1f, 1f, 1f, -1f), new bool4(flag10, y: false, flag11, flag10 != flag11));
				float3 offset = compositionPiece.m_Offset;
				float2 float4 = 1f / new float2(compositionMeshData.m_Width, compositionPiece.m_Size.z * 0.5f);
				float2 float5 = 1f / new float2(num7, compositionPiece.m_Size.z * 0.5f);
				float2 float6 = 1f / new float2(num8, compositionPiece.m_Size.z * 0.5f);
				float2 float7 = new float2(0.5f, 1f);
				float2 float8 = new float2(1f - middleOffset / num7, 1f);
				float2 float9 = new float2((0f - middleOffset) / num8, 1f);
				float z = compositionPiece.m_Offset.x / compositionMeshData.m_Width + 0.5f;
				float z2 = 1f + (compositionPiece.m_Offset.x - middleOffset) / num7;
				float z3 = (compositionPiece.m_Offset.x - middleOffset) / num8;
				if (flag && flag15)
				{
					float num10 = 0.5f * compositionPiece.m_Size.z / compositionMeshData.m_Width;
					float4.y *= num10;
					float7.y *= num10;
					z = 0.5f;
				}
				else if (flag2)
				{
					z2 = 0f;
					z3 = 1f;
				}
				else if (flag14)
				{
					float3.z *= 2f;
					offset.z += 0.5f * compositionPiece.m_Size.z;
				}
				DynamicBuffer<MeshVertex> pieceVertices = m_MeshVertices[compositionPiece.m_Piece];
				DynamicBuffer<MeshNormal> dynamicBuffer4 = m_MeshNormals[compositionPiece.m_Piece];
				DynamicBuffer<MeshTangent> dynamicBuffer5 = m_MeshTangents[compositionPiece.m_Piece];
				DynamicBuffer<MeshUV0> dynamicBuffer6 = m_MeshUV0s[compositionPiece.m_Piece];
				DynamicBuffer<MeshIndex> pieceIndices = m_MeshIndices[compositionPiece.m_Piece];
				DynamicBuffer<MeshMaterial> dynamicBuffer7 = m_MeshMaterials[compositionPiece.m_Piece];
				if (flag17 || flag18 || flag19)
				{
					if (!heightBounds.IsCreated)
					{
						heightBounds = new NativeArray<Bounds1>(257, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					}
					InitializeHeightBounds(heightBounds, compositionPiece, pieceVertices, pieceIndices);
				}
				for (int num11 = 0; num11 < dynamicBuffer7.Length; num11++)
				{
					MeshMaterial pieceMaterial2 = dynamicBuffer7[num11];
					int material2 = GetMaterial(materials, pieceMaterial2);
					MeshMaterial value5 = materials[material2];
					for (int num12 = 0; num12 < pieceMaterial2.m_VertexCount; num12++)
					{
						float z4 = pieceVertices[pieceMaterial2.m_StartVertex + num12].m_Vertex.z;
						if ((!flag20 || z4 >= -0.01f) && (!flag21 || z4 <= 0.01f))
						{
							nativeArray4[num12] = value5.m_StartVertex + value5.m_VertexCount++;
						}
						else
						{
							nativeArray4[num12] = -1;
						}
					}
					int3 int5 = pieceMaterial2.m_StartIndex + math.select(new int3(0, 1, 2), new int3(2, 1, 0), flag10 != flag11);
					float2 float10 = 0f;
					float x = 0f;
					bool flag22 = !flag16;
					for (int num13 = 0; num13 < pieceMaterial2.m_IndexCount; num13 += 3)
					{
						int3 int6 = num13 + int5;
						int6.x = pieceIndices[int6.x].m_Index;
						int6.y = pieceIndices[int6.y].m_Index;
						int6.z = pieceIndices[int6.z].m_Index;
						int3 int7 = int6 - pieceMaterial2.m_StartVertex;
						int7.x = nativeArray4[int7.x];
						int7.y = nativeArray4[int7.y];
						int7.z = nativeArray4[int7.z];
						if (math.all(int7 >= 0))
						{
							if (flag9)
							{
								nativeArray2[value5.m_StartIndex + value5.m_IndexCount++] = (uint)int7.x;
								nativeArray2[value5.m_StartIndex + value5.m_IndexCount++] = (uint)int7.y;
								nativeArray2[value5.m_StartIndex + value5.m_IndexCount++] = (uint)int7.z;
							}
							else
							{
								nativeArray3[value5.m_StartIndex + value5.m_IndexCount++] = (ushort)int7.x;
								nativeArray3[value5.m_StartIndex + value5.m_IndexCount++] = (ushort)int7.y;
								nativeArray3[value5.m_StartIndex + value5.m_IndexCount++] = (ushort)int7.z;
							}
							float3 float11 = new float3(dynamicBuffer6[int6.x].m_Uv.y, dynamicBuffer6[int6.y].m_Uv.y, dynamicBuffer6[int6.z].m_Uv.y);
							if (flag22 & math.all(float11 >= 0f))
							{
								float3 float12 = new float3(pieceVertices[int6.x].m_Vertex.z, pieceVertices[int6.y].m_Vertex.z, pieceVertices[int6.z].m_Vertex.z);
								float10 += new float2(math.csum(math.abs(float11.yzx - float11)), math.csum(math.abs(float12.yzx - float12)));
							}
							else
							{
								x = math.max(math.max(x, float11.x), math.max(float11.y, float11.z));
							}
						}
					}
					float trueValue = float10.x / float10.y;
					x = math.select(math.ceil(x), 0f, flag22);
					for (int num14 = 0; num14 < pieceMaterial2.m_VertexCount; num14++)
					{
						int num15 = nativeArray4[num14];
						if (num15 == -1)
						{
							continue;
						}
						int index = pieceMaterial2.m_StartVertex + num14;
						float3 vertex = pieceVertices[index].m_Vertex;
						float3 float13 = dynamicBuffer4[index].m_Normal;
						float4 tangent = dynamicBuffer5[index].m_Tangent;
						float4 v = new float4(dynamicBuffer6[index].m_Uv, 0f, 0f);
						int4 int8 = default(int4);
						if (flag17)
						{
							Bounds1 heightBounds2 = GetHeightBounds(heightBounds, compositionPiece, vertex.z);
							int8.z = math.select(int8.z, 1, vertex.y <= heightBounds2.min + 0.01f);
						}
						if (flag18)
						{
							Bounds1 heightBounds3 = GetHeightBounds(heightBounds, compositionPiece, vertex.z);
							int8.z = math.select(int8.z, 2, vertex.y >= heightBounds3.max - 0.01f);
						}
						if (flag19)
						{
							Bounds1 heightBounds4 = GetHeightBounds(heightBounds, compositionPiece, vertex.z);
							float13 = math.select(float13, new float3(0f, 1f, 0f), vertex.y >= heightBounds4.max - 0.01f);
						}
						vertex = vertex * float3.xyz + offset;
						float13 *= float3.xyz;
						tangent *= float3;
						v.y = math.select(v.y - x, trueValue, flag22 & (v.y >= 0f));
						if (flag)
						{
							if (flag15)
							{
								int8.xy = new int2(6, 7);
								v.z = z;
								vertex.xz = vertex.xz * float4 + float7;
								v.w = vertex.z * 20f;
							}
							else if (flag12)
							{
								float num16 = vertex.x - compositionPiece.m_Offset.x;
								vertex.x = math.select(vertex.x, compositionPiece.m_Offset.x, test);
								if (math.abs(num16) < 0.01f)
								{
									if (flag2)
									{
										int8.xy = math.select(new int2(4, 2), new int2(5, 3), vertex.z >= 0f);
										vertex.x = 0f;
										vertex.z = vertex.z * float4.y + float7.y;
										v.w = vertex.z * 40f;
									}
									else
									{
										int8.x = 4;
										vertex.x = 0f;
										vertex.z = vertex.z * float4.y + float7.y;
										v.w = vertex.z * 20f;
									}
								}
								else if (num16 > 0f)
								{
									if (flag2)
									{
										int8.xyw = math.select(new int3(4, 2, 4), new int3(5, 3, 132), vertex.z >= 0f);
										v.z = z3;
										vertex.xz = vertex.xz * float6 + float9;
										v.w = vertex.z * 40f;
										vertex.z -= int8.y - 2;
									}
									else
									{
										int8.xyw = new int3(1, 3, 4);
										v.z = z3;
										vertex.xz = vertex.xz * float6 + float9;
										v.w = vertex.z * 20f;
									}
								}
								else if (flag2)
								{
									int8.xyw = math.select(new int3(0, 4, 2), new int3(1, 5, 130), vertex.z >= 0f);
									v.z = z2;
									vertex.xz = vertex.xz * float5 + float8;
									v.w = vertex.z * 40f;
									vertex.z -= int8.x;
								}
								else
								{
									int8.yw = new int2(2, 2);
									v.z = z2;
									vertex.xz = vertex.xz * float5 + float8;
									v.w = vertex.z * 20f;
								}
							}
							else if (flag13)
							{
								if (flag2)
								{
									int8.xyw = math.select(new int3(4, 2, 4), new int3(5, 3, 132), vertex.z >= 0f);
									v.z = z3;
									vertex.xz = vertex.xz * float6 + float9;
									v.w = vertex.z * 40f;
									vertex.z -= int8.y - 2;
								}
								else
								{
									int8.xyw = new int3(1, 3, 4);
									v.z = z3;
									vertex.xz = vertex.xz * float6 + float9;
									v.w = vertex.z * 20f;
								}
							}
							else if (flag2)
							{
								int8.xyw = math.select(new int3(0, 4, 2), new int3(1, 5, 130), vertex.z >= 0f);
								v.z = z2;
								vertex.xz = vertex.xz * float5 + float8;
								v.w = vertex.z * 40f;
								vertex.z -= int8.x;
							}
							else
							{
								int8.yw = new int2(2, 2);
								v.z = z2;
								vertex.xz = vertex.xz * float5 + float8;
								v.w = vertex.z * 20f;
							}
							vertex.xz = math.saturate(vertex.xz);
						}
						else
						{
							int8.xy = math.select(new int2(0, 2), new int2(1, 3), vertex.z >= 0f);
							v.z = z;
							vertex.xz = vertex.xz * float4 + float7;
							v.w = vertex.z * 0.5f;
							vertex.z -= int8.x;
							vertex.xz = math.saturate(vertex.xz);
						}
						Color32 color = new Color32((byte)int8.x, (byte)int8.y, (byte)int8.z, (byte)int8.w);
						vertexData[num15] = new VertexData
						{
							m_Position = vertex,
							m_Color = color,
							m_Normal = MathUtils.NormalToOctahedral(float13),
							m_Tangent = MathUtils.TangentToOctahedral(tangent),
							m_UV0 = new half4(v)
						};
					}
					materials[material2] = value5;
				}
			}
			if (nativeArray4.IsCreated)
			{
				nativeArray4.Dispose();
			}
			if (heightBounds.IsCreated)
			{
				heightBounds.Dispose();
			}
		}

		private static void SetupGeneratedMeshAttributes(NativeArray<VertexAttributeDescriptor> attrs)
		{
			attrs[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
			attrs[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 2);
			attrs[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 1);
			attrs[3] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4);
			attrs[4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4);
		}

		private void InitializeHeightBounds(NativeArray<Bounds1> heightBounds, NetCompositionPiece compositionPiece, DynamicBuffer<MeshVertex> pieceVertices, DynamicBuffer<MeshIndex> pieceIndices)
		{
			int num = heightBounds.Length - 1;
			float num2 = (float)num / compositionPiece.m_Size.z;
			for (int i = 0; i <= num; i++)
			{
				heightBounds[i] = new Bounds1(float.MaxValue, float.MinValue);
			}
			for (int j = 0; j < pieceIndices.Length; j += 3)
			{
				float3 vertex = pieceVertices[pieceIndices[j].m_Index].m_Vertex;
				float3 vertex2 = pieceVertices[pieceIndices[j + 1].m_Index].m_Vertex;
				float3 vertex3 = pieceVertices[pieceIndices[j + 2].m_Index].m_Vertex;
				int num3 = math.clamp(Mathf.RoundToInt(vertex.z * num2) + (num >> 1), 0, num);
				int num4 = math.clamp(Mathf.RoundToInt(vertex2.z * num2) + (num >> 1), 0, num);
				int num5 = math.clamp(Mathf.RoundToInt(vertex3.z * num2) + (num >> 1), 0, num);
				AddHeightBounds(heightBounds, vertex, vertex2, num3, num4);
				AddHeightBounds(heightBounds, vertex2, vertex3, num4, num5);
				AddHeightBounds(heightBounds, vertex3, vertex, num5, num3);
			}
		}

		private void AddHeightBounds(NativeArray<Bounds1> heightBounds, float3 aVertex, float3 bVertex, int aIndex, int bIndex)
		{
			if (aIndex <= bIndex)
			{
				float num = 1f / (float)(bIndex - aIndex + 1);
				for (int i = aIndex; i <= bIndex; i++)
				{
					float num2 = math.lerp(aVertex.y, bVertex.y, (float)(i - aIndex) * num);
					heightBounds[i] |= num2;
				}
			}
			else
			{
				float num3 = 1f / (float)(aIndex - bIndex + 1);
				for (int j = bIndex; j <= aIndex; j++)
				{
					float num4 = math.lerp(bVertex.y, aVertex.y, (float)(j - bIndex) * num3);
					heightBounds[j] |= num4;
				}
			}
		}

		private Bounds1 GetHeightBounds(NativeArray<Bounds1> heightBounds, NetCompositionPiece compositionPiece, float z)
		{
			int num = heightBounds.Length - 1;
			float num2 = (float)num / compositionPiece.m_Size.z;
			int index = math.clamp(Mathf.RoundToInt(z * num2) + (num >> 1), 0, num);
			return heightBounds[index];
		}
	}

	public static JobHandle GenerateMeshes(BatchMeshSystem meshSystem, NativeList<Entity> meshes, Mesh.MeshDataArray meshDataArray, JobHandle dependencies)
	{
		return IJobParallelForExtensions.Schedule(new GenerateBatchMeshJob
		{
			m_Entities = meshes,
			m_MeshData = meshSystem.GetComponentLookup<MeshData>(isReadOnly: true),
			m_CompositionMeshData = meshSystem.GetComponentLookup<NetCompositionMeshData>(isReadOnly: true),
			m_CompositionPieces = meshSystem.GetBufferLookup<NetCompositionPiece>(isReadOnly: true),
			m_MeshVertices = meshSystem.GetBufferLookup<MeshVertex>(isReadOnly: true),
			m_MeshNormals = meshSystem.GetBufferLookup<MeshNormal>(isReadOnly: true),
			m_MeshTangents = meshSystem.GetBufferLookup<MeshTangent>(isReadOnly: true),
			m_MeshUV0s = meshSystem.GetBufferLookup<MeshUV0>(isReadOnly: true),
			m_MeshIndices = meshSystem.GetBufferLookup<MeshIndex>(isReadOnly: true),
			m_MeshNodes = meshSystem.GetBufferLookup<MeshNode>(isReadOnly: true),
			m_MeshMaterials = meshSystem.GetBufferLookup<MeshMaterial>(),
			m_MeshDataArray = meshDataArray
		}, meshes.Length, 1, dependencies);
	}
}
