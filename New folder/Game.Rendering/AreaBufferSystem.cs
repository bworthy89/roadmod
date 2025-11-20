using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Serialization;
using Game.Tools;
using Game.UI;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AreaBufferSystem : GameSystemBase, IPreDeserialize
{
	private struct AreaTriangleData
	{
		public Vector3 m_APos;

		public Vector3 m_BPos;

		public Vector3 m_CPos;

		public Vector2 m_APrevXZ;

		public Vector2 m_BPrevXZ;

		public Vector2 m_CPrevXZ;

		public Vector2 m_ANextXZ;

		public Vector2 m_BNextXZ;

		public Vector2 m_CNextXZ;

		public Vector2 m_YMinMax;

		public Vector4 m_FillColor;

		public Vector4 m_EdgeColor;
	}

	private struct MaterialData
	{
		public Material m_Material;

		public bool m_HasMesh;
	}

	private class AreaTypeData
	{
		public EntityQuery m_UpdatedQuery;

		public EntityQuery m_AreaQuery;

		public NativeList<AreaTriangleData> m_BufferData;

		public NativeValue<Bounds3> m_Bounds;

		public JobHandle m_DataDependencies;

		public ComputeBuffer m_Buffer;

		public Material m_Material;

		public Material m_OriginalNameMaterial;

		public List<MaterialData> m_NameMaterials;

		public Mesh m_NameMesh;

		public Mesh.MeshDataArray m_NameMeshData;

		public bool m_BufferDataDirty;

		public bool m_BufferDirty;

		public bool m_HasNameMeshData;

		public bool m_HasNameMesh;
	}

	private struct ChunkData
	{
		public int m_TriangleOffset;

		public Bounds3 m_Bounds;
	}

	[BurstCompile]
	private struct ResetChunkDataJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		public NativeList<ChunkData> m_ChunkData;

		public NativeList<AreaTriangleData> m_AreaTriangleData;

		public void Execute()
		{
			m_ChunkData.ResizeUninitialized(m_Chunks.Length);
			ChunkData value = new ChunkData
			{
				m_Bounds = new Bounds3(float.MaxValue, float.MinValue)
			};
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				m_ChunkData[i] = value;
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				if (archetypeChunk.Has(ref m_HiddenType))
				{
					continue;
				}
				NativeArray<Area> nativeArray = archetypeChunk.GetNativeArray(ref m_AreaType);
				NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TempType);
				BufferAccessor<Triangle> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_TriangleType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					if ((nativeArray[j].m_Flags & AreaFlags.Slave) == 0 && (nativeArray2.Length == 0 || (nativeArray2[j].m_Flags & TempFlags.Hidden) == 0))
					{
						DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor[j];
						value.m_TriangleOffset += dynamicBuffer.Length;
					}
				}
			}
			m_AreaTriangleData.ResizeUninitialized(value.m_TriangleOffset);
		}
	}

	[BurstCompile]
	private struct FillMeshDataJob : IJobParallelForDefer
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Native> m_NativeType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_GeometryData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.AreaColorData> m_ColorData;

		[ReadOnly]
		public BufferLookup<SelectionElement> m_SelectionElements;

		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[NativeDisableParallelForRestriction]
		public NativeList<ChunkData> m_ChunkData;

		[NativeDisableParallelForRestriction]
		public NativeList<AreaTriangleData> m_AreaTriangleData;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			if (archetypeChunk.Has(ref m_HiddenType))
			{
				return;
			}
			ChunkData chunkData = m_ChunkData[index];
			NativeArray<Area> nativeArray = archetypeChunk.GetNativeArray(ref m_AreaType);
			NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_TriangleType);
			bool flag = m_EditorMode || archetypeChunk.Has(ref m_NativeType);
			if (m_SelectionElements.HasBuffer(m_SelectionEntity))
			{
				DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
				NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(dynamicBuffer.Length, Allocator.Temp);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					nativeParallelHashSet.Add(dynamicBuffer[i].m_Entity);
				}
				NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					if ((nativeArray[j].m_Flags & AreaFlags.Slave) != 0)
					{
						continue;
					}
					PrefabRef prefabRef = nativeArray3[j];
					DynamicBuffer<Node> nodes = bufferAccessor[j];
					DynamicBuffer<Triangle> triangles = bufferAccessor2[j];
					AreaGeometryData geometryData = m_GeometryData[prefabRef.m_Prefab];
					if (!m_ColorData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						componentData = Game.Prefabs.AreaColorData.GetDefaults();
					}
					Entity item;
					if (nativeArray2.Length != 0)
					{
						Temp temp = nativeArray2[j];
						if ((temp.m_Flags & TempFlags.Hidden) != 0)
						{
							continue;
						}
						item = temp.m_Original;
					}
					else
					{
						item = nativeArray4[j];
					}
					Color color;
					Color color2;
					if (nativeParallelHashSet.Contains(item))
					{
						color = ((Color)componentData.m_SelectionFillColor).linear;
						color2 = ((Color)componentData.m_SelectionEdgeColor).linear;
					}
					else
					{
						color = ((Color)componentData.m_FillColor).linear;
						color2 = ((Color)componentData.m_EdgeColor).linear;
					}
					if (!flag)
					{
						color = GetDisabledColor(color);
						color2 = GetDisabledColor(color2);
					}
					AddTriangles(nodes, triangles, color, color2, geometryData, ref chunkData);
				}
				nativeParallelHashSet.Dispose();
			}
			else
			{
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					if ((nativeArray[k].m_Flags & AreaFlags.Slave) == 0 && (nativeArray2.Length == 0 || (nativeArray2[k].m_Flags & TempFlags.Hidden) == 0))
					{
						PrefabRef prefabRef2 = nativeArray3[k];
						DynamicBuffer<Node> nodes2 = bufferAccessor[k];
						DynamicBuffer<Triangle> triangles2 = bufferAccessor2[k];
						AreaGeometryData geometryData2 = m_GeometryData[prefabRef2.m_Prefab];
						if (!m_ColorData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
						{
							componentData2 = Game.Prefabs.AreaColorData.GetDefaults();
						}
						Color color3 = ((Color)componentData2.m_FillColor).linear;
						Color color4 = ((Color)componentData2.m_EdgeColor).linear;
						if (!flag)
						{
							color3 = GetDisabledColor(color3);
							color4 = GetDisabledColor(color4);
						}
						AddTriangles(nodes2, triangles2, color3, color4, geometryData2, ref chunkData);
					}
				}
			}
			m_ChunkData[index] = chunkData;
		}

		private static Color GetDisabledColor(Color color)
		{
			color.a *= 0.25f;
			return color;
		}

		private void AddTriangles(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, Vector4 fillColor, Vector4 edgeColor, AreaGeometryData geometryData, ref ChunkData chunkData)
		{
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle triangle = triangles[i];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				Bounds3 bounds = MathUtils.Bounds(triangle2);
				int3 @int = math.select(triangle.m_Indices - 1, nodes.Length - 1, triangle.m_Indices == 0);
				int3 int2 = math.select(triangle.m_Indices + 1, 0, triangle.m_Indices == nodes.Length - 1);
				bounds.min.y += triangle.m_HeightRange.min - geometryData.m_SnapDistance * 2f;
				bounds.max.y += triangle.m_HeightRange.max + geometryData.m_SnapDistance * 2f;
				AreaTriangleData value = new AreaTriangleData
				{
					m_APos = triangle2.a,
					m_BPos = triangle2.b,
					m_CPos = triangle2.c,
					m_APrevXZ = nodes[@int.x].m_Position.xz,
					m_BPrevXZ = nodes[@int.y].m_Position.xz,
					m_CPrevXZ = nodes[@int.z].m_Position.xz,
					m_ANextXZ = nodes[int2.x].m_Position.xz,
					m_BNextXZ = nodes[int2.y].m_Position.xz,
					m_CNextXZ = nodes[int2.z].m_Position.xz,
					m_YMinMax = 
					{
						x = bounds.min.y,
						y = bounds.max.y
					},
					m_FillColor = fillColor,
					m_EdgeColor = edgeColor
				};
				m_AreaTriangleData[chunkData.m_TriangleOffset++] = value;
				chunkData.m_Bounds |= bounds;
			}
		}
	}

	[BurstCompile]
	private struct CalculateBoundsJob : IJob
	{
		[ReadOnly]
		public NativeList<ChunkData> m_ChunkData;

		public NativeValue<Bounds3> m_Bounds;

		public void Execute()
		{
			Bounds3 value = new Bounds3(float.MaxValue, float.MinValue);
			for (int i = 0; i < m_ChunkData.Length; i++)
			{
				value |= m_ChunkData[i].m_Bounds;
			}
			m_Bounds.value = value;
		}
	}

	private struct LabelVertexData
	{
		public Vector3 m_Position;

		public Color32 m_Color;

		public Vector2 m_UV0;

		public Vector2 m_UV1;

		public Vector3 m_UV2;
	}

	[BurstCompile]
	private struct FillNameDataJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Geometry> m_GeometryType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public BufferTypeHandle<LabelVertex> m_LabelVertexType;

		[ReadOnly]
		public ComponentLookup<AreaNameData> m_AreaNameData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public int m_SubMeshCount;

		public Mesh.MeshDataArray m_NameMeshData;

		public void Execute()
		{
			NativeArray<int> array = new NativeArray<int>(m_SubMeshCount, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				if (archetypeChunk.Has(ref m_HiddenType))
				{
					continue;
				}
				BufferAccessor<LabelVertex> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_LabelVertexType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<LabelVertex> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k += 4)
					{
						int material = dynamicBuffer[k].m_Material;
						array.ElementAt(material) += 4;
					}
				}
			}
			int num = 0;
			for (int l = 0; l < m_SubMeshCount; l++)
			{
				num += array[l];
			}
			Mesh.MeshData meshData = m_NameMeshData[0];
			NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
			{
				[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
				[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
				[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
				[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2),
				[4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord2)
			};
			meshData.SetVertexBufferParams(num, attributes);
			meshData.SetIndexBufferParams((num >> 2) * 6, IndexFormat.UInt32);
			attributes.Dispose();
			num = 0;
			meshData.subMeshCount = m_SubMeshCount;
			for (int m = 0; m < m_SubMeshCount; m++)
			{
				ref int reference = ref array.ElementAt(m);
				meshData.SetSubMesh(m, new SubMeshDescriptor
				{
					firstVertex = num,
					indexStart = (num >> 2) * 6,
					vertexCount = reference,
					indexCount = (reference >> 2) * 6,
					topology = MeshTopology.Triangles
				}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
				num += reference;
				reference = 0;
			}
			NativeArray<LabelVertexData> vertexData = meshData.GetVertexData<LabelVertexData>();
			NativeArray<uint> indexData = meshData.GetIndexData<uint>();
			LabelVertexData value = default(LabelVertexData);
			for (int n = 0; n < m_Chunks.Length; n++)
			{
				ArchetypeChunk archetypeChunk2 = m_Chunks[n];
				if (archetypeChunk2.Has(ref m_HiddenType))
				{
					continue;
				}
				NativeArray<Geometry> nativeArray = archetypeChunk2.GetNativeArray(ref m_GeometryType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Temp> nativeArray3 = archetypeChunk2.GetNativeArray(ref m_TempType);
				BufferAccessor<LabelVertex> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_LabelVertexType);
				for (int num2 = 0; num2 < bufferAccessor2.Length; num2++)
				{
					Geometry geometry = nativeArray[num2];
					PrefabRef prefabRef = nativeArray2[num2];
					DynamicBuffer<LabelVertex> dynamicBuffer2 = bufferAccessor2[num2];
					if (!m_AreaNameData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						componentData = AreaNameData.GetDefaults();
					}
					float3 @float = AreaUtils.CalculateLabelPosition(geometry);
					Color32 color = componentData.m_Color;
					if (nativeArray3.Length != 0 && (nativeArray3[num2].m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						color = componentData.m_SelectedColor;
					}
					SubMeshDescriptor subMeshDescriptor = default(SubMeshDescriptor);
					int num3 = -1;
					for (int num4 = 0; num4 < dynamicBuffer2.Length; num4 += 4)
					{
						int material2 = dynamicBuffer2[num4].m_Material;
						ref int reference2 = ref array.ElementAt(material2);
						if (material2 != num3)
						{
							subMeshDescriptor = meshData.GetSubMesh(material2);
							num3 = material2;
						}
						int num5 = subMeshDescriptor.firstVertex + reference2;
						int num6 = subMeshDescriptor.indexStart + (reference2 >> 2) * 6;
						reference2 += 4;
						indexData[num6] = (uint)num5;
						indexData[num6 + 1] = (uint)(num5 + 1);
						indexData[num6 + 2] = (uint)(num5 + 2);
						indexData[num6 + 3] = (uint)(num5 + 2);
						indexData[num6 + 4] = (uint)(num5 + 3);
						indexData[num6 + 5] = (uint)num5;
						for (int num7 = 0; num7 < 4; num7++)
						{
							LabelVertex labelVertex = dynamicBuffer2[num4 + num7];
							value.m_Position = labelVertex.m_Position;
							value.m_Color = new Color32((byte)(labelVertex.m_Color.r * color.r >> 8), (byte)(labelVertex.m_Color.g * color.g >> 8), (byte)(labelVertex.m_Color.b * color.b >> 8), (byte)(labelVertex.m_Color.a * color.a >> 8));
							value.m_UV0 = labelVertex.m_UV0;
							value.m_UV1 = labelVertex.m_UV1;
							value.m_UV2 = @float;
							vertexData[num5 + num7] = value;
						}
					}
				}
			}
			for (int num8 = 0; num8 < m_SubMeshCount; num8++)
			{
				SubMeshDescriptor subMesh = meshData.GetSubMesh(num8);
				meshData.SetSubMesh(num8, subMesh);
			}
			array.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> __Game_Tools_Hidden_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.AreaColorData> __Game_Prefabs_AreaColorData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SelectionElement> __Game_Tools_SelectionElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LabelVertex> __Game_Areas_LabelVertex_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<AreaNameData> __Game_Prefabs_AreaNameData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BatchesUpdated> __Game_Common_BatchesUpdated_RO_ComponentTypeHandle;

		public BufferTypeHandle<LabelExtents> __Game_Areas_LabelExtents_RW_BufferTypeHandle;

		public BufferTypeHandle<LabelVertex> __Game_Areas_LabelVertex_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Hidden>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_AreaColorData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.AreaColorData>(isReadOnly: true);
			__Game_Tools_SelectionElement_RO_BufferLookup = state.GetBufferLookup<SelectionElement>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(isReadOnly: true);
			__Game_Areas_LabelVertex_RO_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>(isReadOnly: true);
			__Game_Prefabs_AreaNameData_RO_ComponentLookup = state.GetComponentLookup<AreaNameData>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
			__Game_Common_BatchesUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatchesUpdated>(isReadOnly: true);
			__Game_Areas_LabelExtents_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelExtents>();
			__Game_Areas_LabelVertex_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>();
		}
	}

	private EntityQuery m_SettingsQuery;

	private RenderingSystem m_RenderingSystem;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private NameSystem m_NameSystem;

	private AreaTypeData[] m_AreaTypeData;

	private AreaType m_LastSelectionAreaType;

	private EntityQuery m_SelectionQuery;

	private bool m_Loaded;

	private int m_AreaParameters;

	private Dictionary<Entity, string> m_CachedLabels;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_AreaTypeData = new AreaTypeData[5];
		m_AreaTypeData[0] = InitializeAreaData<Lot>();
		m_AreaTypeData[1] = InitializeAreaData<District>();
		m_AreaTypeData[2] = InitializeAreaData<MapTile>();
		m_AreaTypeData[3] = InitializeAreaData<Game.Areas.Space>();
		m_AreaTypeData[4] = InitializeAreaData<Surface>();
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Game.Prefabs.AreaTypeData>());
		m_SelectionQuery = GetEntityQuery(ComponentType.ReadOnly<SelectionInfo>(), ComponentType.ReadOnly<SelectionElement>());
		m_AreaParameters = Shader.PropertyToID("colossal_AreaParameters");
		GameManager.instance.localizationManager.onActiveDictionaryChanged += OnDictionaryChanged;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		for (int i = 0; i < m_AreaTypeData.Length; i++)
		{
			AreaTypeData areaTypeData = m_AreaTypeData[i];
			if (areaTypeData.m_NameMaterials != null)
			{
				for (int j = 0; j < areaTypeData.m_NameMaterials.Count; j++)
				{
					MaterialData materialData = areaTypeData.m_NameMaterials[j];
					if (materialData.m_Material != null)
					{
						Object.Destroy(materialData.m_Material);
					}
				}
			}
			if (areaTypeData.m_BufferData.IsCreated)
			{
				areaTypeData.m_BufferData.Dispose();
			}
			if (areaTypeData.m_Bounds.IsCreated)
			{
				areaTypeData.m_Bounds.Dispose();
			}
			if (areaTypeData.m_Material != null)
			{
				Object.Destroy(areaTypeData.m_Material);
			}
			if (areaTypeData.m_NameMesh != null)
			{
				Object.Destroy(areaTypeData.m_NameMesh);
			}
			if (areaTypeData.m_Buffer != null)
			{
				areaTypeData.m_Buffer.Release();
			}
			if (areaTypeData.m_HasNameMeshData)
			{
				areaTypeData.m_NameMeshData.Dispose();
			}
		}
		GameManager.instance.localizationManager.onActiveDictionaryChanged -= OnDictionaryChanged;
		base.OnDestroy();
	}

	private void OnDictionaryChanged()
	{
		base.EntityManager.AddComponent<Updated>(m_AreaTypeData[1].m_AreaQuery);
	}

	private AreaTypeData InitializeAreaData<T>() where T : struct, IComponentData
	{
		AreaTypeData areaTypeData = new AreaTypeData();
		areaTypeData.m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<T>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Triangle>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		areaTypeData.m_AreaQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Triangle>(), ComponentType.Exclude<Deleted>());
		return areaTypeData;
	}

	public void PreDeserialize(Context context)
	{
		for (int i = 0; i < m_AreaTypeData.Length; i++)
		{
			AreaTypeData areaTypeData = m_AreaTypeData[i];
			if (areaTypeData.m_BufferData.IsCreated)
			{
				areaTypeData.m_BufferData.Dispose();
				areaTypeData.m_BufferData = default(NativeList<AreaTriangleData>);
			}
			if (areaTypeData.m_Buffer != null)
			{
				areaTypeData.m_Buffer.Release();
				areaTypeData.m_Buffer = null;
			}
			if (areaTypeData.m_NameMesh != null)
			{
				Object.Destroy(areaTypeData.m_NameMesh);
				areaTypeData.m_NameMesh = null;
			}
			if (areaTypeData.m_HasNameMeshData)
			{
				areaTypeData.m_NameMeshData.Dispose();
				areaTypeData.m_HasNameMeshData = false;
			}
		}
		if (m_CachedLabels != null)
		{
			m_CachedLabels.Clear();
		}
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
		bool loaded = GetLoaded();
		if (!m_SettingsQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_SettingsQuery.ToArchetypeChunkArray(Allocator.TempJob);
			ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NativeArray<PrefabData> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					AreaTypePrefab prefab = m_PrefabSystem.GetPrefab<AreaTypePrefab>(nativeArray2[j]);
					AreaTypeData areaTypeData = m_AreaTypeData[(int)prefab.m_Type];
					float minNodeDistance = AreaUtils.GetMinNodeDistance(prefab.m_Type);
					if (areaTypeData.m_Material != null)
					{
						Object.Destroy(areaTypeData.m_Material);
					}
					areaTypeData.m_Material = new Material(prefab.m_Material);
					areaTypeData.m_Material.name = "Area buffer (" + prefab.m_Material.name + ")";
					areaTypeData.m_Material.SetVector(m_AreaParameters, new Vector4(minNodeDistance * (1f / 32f), minNodeDistance * 0.25f, minNodeDistance * 2f, 0f));
					if (areaTypeData.m_NameMaterials != null)
					{
						for (int k = 0; k < areaTypeData.m_NameMaterials.Count; k++)
						{
							MaterialData materialData = areaTypeData.m_NameMaterials[k];
							if (materialData.m_Material != null)
							{
								Object.Destroy(materialData.m_Material);
							}
						}
						areaTypeData.m_NameMaterials = null;
					}
					areaTypeData.m_OriginalNameMaterial = prefab.m_NameMaterial;
					if (prefab.m_NameMaterial != null)
					{
						areaTypeData.m_NameMaterials = new List<MaterialData>(1);
					}
				}
			}
			nativeArray.Dispose();
		}
		JobHandle jobHandle = default(JobHandle);
		AreaType areaType = AreaType.None;
		Entity entity = Entity.Null;
		if (!m_SelectionQuery.IsEmptyIgnoreFilter)
		{
			entity = m_SelectionQuery.GetSingletonEntity();
			areaType = base.EntityManager.GetComponentData<SelectionInfo>(entity).m_AreaType;
		}
		if (m_LastSelectionAreaType != AreaType.None)
		{
			m_AreaTypeData[(int)m_LastSelectionAreaType].m_BufferDataDirty = true;
		}
		if (areaType != AreaType.None)
		{
			m_AreaTypeData[(int)areaType].m_BufferDataDirty = true;
		}
		m_LastSelectionAreaType = areaType;
		for (int l = 0; l < m_AreaTypeData.Length; l++)
		{
			AreaTypeData areaTypeData2 = m_AreaTypeData[l];
			EntityQuery entityQuery = (loaded ? areaTypeData2.m_AreaQuery : areaTypeData2.m_UpdatedQuery);
			if (!areaTypeData2.m_BufferDataDirty && entityQuery.IsEmptyIgnoreFilter)
			{
				continue;
			}
			if (areaTypeData2.m_AreaQuery.IsEmptyIgnoreFilter)
			{
				areaTypeData2.m_BufferDataDirty = false;
				areaTypeData2.m_BufferDirty = false;
				if (areaTypeData2.m_Buffer != null)
				{
					areaTypeData2.m_Buffer.Release();
					areaTypeData2.m_Buffer = null;
				}
				if (areaTypeData2.m_NameMesh != null)
				{
					Object.Destroy(areaTypeData2.m_NameMesh);
					areaTypeData2.m_NameMesh = null;
				}
			}
			else
			{
				areaTypeData2.m_BufferDataDirty = true;
			}
			if (areaTypeData2.m_NameMaterials != null && !entityQuery.IsEmptyIgnoreFilter)
			{
				UpdateLabelVertices(areaTypeData2, loaded);
			}
		}
		if (!m_RenderingSystem.hideOverlay)
		{
			for (int m = 0; m < m_AreaTypeData.Length; m++)
			{
				AreaTypeData areaTypeData3 = m_AreaTypeData[m];
				if (!areaTypeData3.m_BufferDataDirty || (areaTypeData3.m_NameMaterials == null && (m_ToolSystem.activeTool == null || ((uint)m_ToolSystem.activeTool.requireAreas & (uint)(1 << m)) == 0)))
				{
					continue;
				}
				areaTypeData3.m_BufferDataDirty = false;
				areaTypeData3.m_BufferDirty = true;
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> nativeList = areaTypeData3.m_AreaQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				NativeList<ChunkData> chunkData = new NativeList<ChunkData>(0, Allocator.TempJob);
				if (!areaTypeData3.m_BufferData.IsCreated)
				{
					areaTypeData3.m_BufferData = new NativeList<AreaTriangleData>(Allocator.Persistent);
				}
				if (!areaTypeData3.m_Bounds.IsCreated)
				{
					areaTypeData3.m_Bounds = new NativeValue<Bounds3>(Allocator.Persistent);
				}
				ResetChunkDataJob jobData = new ResetChunkDataJob
				{
					m_Chunks = nativeList,
					m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_ChunkData = chunkData,
					m_AreaTriangleData = areaTypeData3.m_BufferData
				};
				FillMeshDataJob jobData2 = new FillMeshDataJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NativeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaColorData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_SelectionEntity = entity,
					m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
					m_Chunks = nativeList.AsDeferredJobArray(),
					m_ChunkData = chunkData,
					m_AreaTriangleData = areaTypeData3.m_BufferData
				};
				CalculateBoundsJob jobData3 = new CalculateBoundsJob
				{
					m_ChunkData = chunkData,
					m_Bounds = areaTypeData3.m_Bounds
				};
				JobHandle dependsOn = JobHandle.CombineDependencies(base.Dependency, outJobHandle);
				JobHandle jobHandle2 = IJobParallelForDeferExtensions.Schedule(dependsOn: IJobExtensions.Schedule(jobData, dependsOn), jobData: jobData2, list: nativeList, innerloopBatchCount: 1);
				JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle2);
				chunkData.Dispose(jobHandle3);
				if (areaTypeData3.m_NameMaterials != null)
				{
					if (!areaTypeData3.m_HasNameMeshData)
					{
						areaTypeData3.m_HasNameMeshData = true;
						areaTypeData3.m_NameMeshData = Mesh.AllocateWritableMeshData(1);
					}
					JobHandle job = IJobExtensions.Schedule(new FillNameDataJob
					{
						m_GeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_LabelVertexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_LabelVertex_RO_BufferTypeHandle, ref base.CheckedStateRef),
						m_AreaNameData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaNameData_RO_ComponentLookup, ref base.CheckedStateRef),
						m_Chunks = nativeList,
						m_SubMeshCount = areaTypeData3.m_NameMaterials.Count,
						m_NameMeshData = areaTypeData3.m_NameMeshData
					}, dependsOn);
					nativeList.Dispose(JobHandle.CombineDependencies(jobHandle2, job));
					areaTypeData3.m_DataDependencies = JobHandle.CombineDependencies(jobHandle3, job);
				}
				else
				{
					nativeList.Dispose(jobHandle2);
					areaTypeData3.m_DataDependencies = jobHandle3;
				}
				jobHandle = JobHandle.CombineDependencies(jobHandle, areaTypeData3.m_DataDependencies);
			}
		}
		base.Dependency = jobHandle;
	}

	public unsafe bool GetAreaBuffer(AreaType type, out ComputeBuffer buffer, out Material material, out Bounds bounds)
	{
		AreaTypeData areaTypeData = m_AreaTypeData[(int)type];
		if (areaTypeData.m_BufferDirty)
		{
			areaTypeData.m_BufferDirty = false;
			areaTypeData.m_DataDependencies.Complete();
			areaTypeData.m_DataDependencies = default(JobHandle);
			if (areaTypeData.m_BufferData.IsCreated)
			{
				if (areaTypeData.m_Buffer != null && areaTypeData.m_Buffer.count != areaTypeData.m_BufferData.Length)
				{
					areaTypeData.m_Buffer.Release();
					areaTypeData.m_Buffer = null;
				}
				if (areaTypeData.m_BufferData.Length > 0)
				{
					if (areaTypeData.m_Buffer == null)
					{
						areaTypeData.m_Buffer = new ComputeBuffer(areaTypeData.m_BufferData.Length, sizeof(AreaTriangleData));
					}
					areaTypeData.m_Buffer.SetData(areaTypeData.m_BufferData.AsArray());
				}
				areaTypeData.m_BufferData.Dispose();
			}
		}
		buffer = areaTypeData.m_Buffer;
		material = areaTypeData.m_Material;
		if (areaTypeData.m_Bounds.IsCreated)
		{
			bounds = RenderingUtils.ToBounds(areaTypeData.m_Bounds.value);
		}
		else
		{
			bounds = default(Bounds);
		}
		if (areaTypeData.m_Buffer != null)
		{
			return areaTypeData.m_Buffer.count != 0;
		}
		return false;
	}

	private void UpdateLabelVertices(AreaTypeData data, bool isLoaded)
	{
		NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? data.m_AreaQuery : data.m_UpdatedQuery).ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			TextMeshPro textMesh = m_OverlayRenderSystem.GetTextMesh();
			textMesh.rectTransform.sizeDelta = new Vector2(250f, 100f);
			textMesh.fontSize = 200f;
			textMesh.alignment = TextAlignmentOptions.Center;
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Updated> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BatchesUpdated> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Temp> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<LabelExtents> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_LabelExtents_RW_BufferTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<LabelVertex> bufferTypeHandle2 = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_LabelVertex_RW_BufferTypeHandle, ref base.CheckedStateRef);
			LabelVertex value2 = default(LabelVertex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				if (isLoaded || archetypeChunk.Has(ref typeHandle) || archetypeChunk.Has(ref typeHandle2))
				{
					NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
					NativeArray<Temp> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
					BufferAccessor<LabelExtents> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
					BufferAccessor<LabelVertex> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle2);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity entity = nativeArray2[j];
						DynamicBuffer<LabelExtents> dynamicBuffer = bufferAccessor[j];
						DynamicBuffer<LabelVertex> dynamicBuffer2 = bufferAccessor2[j];
						string renderedLabelName;
						if (nativeArray3.Length != 0)
						{
							Temp temp = nativeArray3[j];
							if (!(temp.m_Original != Entity.Null))
							{
								if (m_CachedLabels != null && m_CachedLabels.ContainsKey(entity))
								{
									m_CachedLabels.Remove(entity);
								}
								dynamicBuffer2.Clear();
								continue;
							}
							renderedLabelName = m_NameSystem.GetRenderedLabelName(temp.m_Original);
						}
						else
						{
							renderedLabelName = m_NameSystem.GetRenderedLabelName(entity);
						}
						if (m_CachedLabels != null)
						{
							if (m_CachedLabels.TryGetValue(entity, out var value))
							{
								if (value == renderedLabelName)
								{
									continue;
								}
								m_CachedLabels[entity] = renderedLabelName;
							}
							else
							{
								m_CachedLabels.Add(entity, renderedLabelName);
							}
						}
						else
						{
							m_CachedLabels = new Dictionary<Entity, string>();
							m_CachedLabels.Add(entity, renderedLabelName);
						}
						TMP_TextInfo textInfo = textMesh.GetTextInfo(renderedLabelName);
						int num = 0;
						for (int k = 0; k < textInfo.meshInfo.Length; k++)
						{
							TMP_MeshInfo tMP_MeshInfo = textInfo.meshInfo[k];
							num += tMP_MeshInfo.vertexCount;
						}
						dynamicBuffer2.ResizeUninitialized(num);
						num = 0;
						for (int l = 0; l < textInfo.meshInfo.Length; l++)
						{
							TMP_MeshInfo tMP_MeshInfo2 = textInfo.meshInfo[l];
							if (tMP_MeshInfo2.vertexCount == 0)
							{
								continue;
							}
							Texture mainTexture = tMP_MeshInfo2.material.mainTexture;
							int num2 = -1;
							for (int m = 0; m < data.m_NameMaterials.Count; m++)
							{
								if (data.m_NameMaterials[m].m_Material.mainTexture == mainTexture)
								{
									num2 = m;
									break;
								}
							}
							if (num2 == -1)
							{
								MaterialData item = new MaterialData
								{
									m_Material = new Material(data.m_OriginalNameMaterial)
								};
								m_OverlayRenderSystem.CopyFontAtlasParameters(tMP_MeshInfo2.material, item.m_Material);
								num2 = data.m_NameMaterials.Count;
								data.m_NameMaterials.Add(item);
								item.m_Material.name = $"Area names {num2} ({data.m_OriginalNameMaterial.name})";
							}
							Vector3[] vertices = tMP_MeshInfo2.vertices;
							Vector2[] uvs = tMP_MeshInfo2.uvs0;
							Vector2[] uvs2 = tMP_MeshInfo2.uvs2;
							Color32[] colors = tMP_MeshInfo2.colors32;
							for (int n = 0; n < tMP_MeshInfo2.vertexCount; n++)
							{
								value2.m_Position = vertices[n];
								value2.m_Color = colors[n];
								value2.m_UV0 = uvs[n];
								value2.m_UV1 = uvs2[n];
								value2.m_Material = num2;
								dynamicBuffer2[num + n] = value2;
							}
							num += tMP_MeshInfo2.vertexCount;
						}
						dynamicBuffer.ResizeUninitialized(textInfo.lineCount);
						for (int num3 = 0; num3 < textInfo.lineCount; num3++)
						{
							Extents lineExtents = textInfo.lineInfo[num3].lineExtents;
							dynamicBuffer[num3] = new LabelExtents(lineExtents.min, lineExtents.max);
						}
					}
				}
				else if (m_CachedLabels != null)
				{
					NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(entityTypeHandle);
					for (int num4 = 0; num4 < nativeArray4.Length; num4++)
					{
						Entity key = nativeArray4[num4];
						m_CachedLabels.Remove(key);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	public bool GetNameMesh(AreaType type, out Mesh mesh, out int subMeshCount)
	{
		AreaTypeData areaTypeData = m_AreaTypeData[(int)type];
		if (areaTypeData.m_NameMaterials != null)
		{
			subMeshCount = areaTypeData.m_NameMaterials.Count;
		}
		else
		{
			subMeshCount = 0;
		}
		if (areaTypeData.m_HasNameMeshData)
		{
			areaTypeData.m_HasNameMeshData = false;
			areaTypeData.m_DataDependencies.Complete();
			areaTypeData.m_DataDependencies = default(JobHandle);
			if (areaTypeData.m_NameMesh == null)
			{
				areaTypeData.m_NameMesh = new Mesh();
				areaTypeData.m_NameMesh.name = $"Area names ({type})";
			}
			Mesh.ApplyAndDisposeWritableMeshData(areaTypeData.m_NameMeshData, areaTypeData.m_NameMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
			if (areaTypeData.m_Bounds.IsCreated && math.all(areaTypeData.m_Bounds.value.max >= areaTypeData.m_Bounds.value.min))
			{
				areaTypeData.m_NameMesh.bounds = RenderingUtils.ToBounds(areaTypeData.m_Bounds.value);
			}
			else
			{
				areaTypeData.m_NameMesh.RecalculateBounds();
			}
			areaTypeData.m_HasNameMesh = false;
			for (int i = 0; i < subMeshCount; i++)
			{
				MaterialData value = areaTypeData.m_NameMaterials[i];
				value.m_HasMesh = areaTypeData.m_NameMesh.GetSubMesh(i).vertexCount > 0;
				areaTypeData.m_HasNameMesh |= value.m_HasMesh;
				areaTypeData.m_NameMaterials[i] = value;
			}
		}
		mesh = areaTypeData.m_NameMesh;
		return areaTypeData.m_HasNameMesh;
	}

	public bool GetNameMaterial(AreaType type, int subMeshIndex, out Material material)
	{
		MaterialData materialData = m_AreaTypeData[(int)type].m_NameMaterials[subMeshIndex];
		material = materialData.m_Material;
		return materialData.m_HasMesh;
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
	public AreaBufferSystem()
	{
	}
}
