using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
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
public class CityBoundaryMeshSystem : GameSystemBase, IPreDeserialize
{
	private struct Boundary
	{
		public Line3.Segment m_Line;

		public Color32 m_Color;
	}

	[BurstCompile]
	private struct FillBoundaryQueueJob : IJobChunk
	{
		private struct Iterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Line2.Segment m_Line;

			public Entity m_Area;

			public NativeParallelHashSet<Entity> m_StartTiles;

			public ComponentLookup<MapTile> m_MapTileData;

			public ComponentLookup<Native> m_NativeData;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public bool m_EditorMode;

			public bool m_IsNative;

			public bool m_TileFound;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float2 t;
				return MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds.xz, 1f), m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds.xz, 1f), m_Line, out var _) || m_Area == areaItem.m_Area || !m_MapTileData.HasComponent(areaItem.m_Area))
				{
					return;
				}
				if (!m_IsNative)
				{
					if (m_EditorMode)
					{
						if (!m_StartTiles.Contains(areaItem.m_Area))
						{
							return;
						}
					}
					else if (m_NativeData.HasComponent(areaItem.m_Area))
					{
						return;
					}
				}
				DynamicBuffer<Node> nodes = m_Nodes[areaItem.m_Area];
				Triangle triangle = m_Triangles[areaItem.m_Area][areaItem.m_Triangle];
				Triangle2 triangle2 = AreaUtils.GetTriangle2(nodes, triangle);
				bool3 @bool = AreaUtils.IsEdge(nodes, triangle);
				if (@bool.x)
				{
					CheckLine(triangle2.ab);
				}
				if (@bool.y)
				{
					CheckLine(triangle2.bc);
				}
				if (@bool.z)
				{
					CheckLine(triangle2.ca);
				}
			}

			private void CheckLine(Line2.Segment line)
			{
				m_TileFound |= (math.distancesq(line.a, m_Line.a) < 1f && math.distancesq(line.b, m_Line.b) < 1f) || (math.distancesq(line.b, m_Line.a) < 1f && math.distancesq(line.a, m_Line.b) < 1f);
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Native> m_NativeType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<MapTile> m_MapTileData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public NativeList<Entity> m_StartTiles;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Color32 m_CityBorderColor;

		[ReadOnly]
		public Color32 m_MapBorderColor;

		public NativeQueue<Boundary>.ParallelWriter m_BoundaryQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			if (m_EditorMode)
			{
				NativeParallelHashSet<Entity> startTiles = new NativeParallelHashSet<Entity>(m_StartTiles.Length, Allocator.Temp);
				for (int i = 0; i < m_StartTiles.Length; i++)
				{
					startTiles.Add(m_StartTiles[i]);
				}
				Line3.Segment line = default(Line3.Segment);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					bool isNative = !startTiles.Contains(entity);
					DynamicBuffer<Node> dynamicBuffer = bufferAccessor[j];
					if (dynamicBuffer.Length >= 2)
					{
						line.a = dynamicBuffer[dynamicBuffer.Length - 1].m_Position;
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							line.b = dynamicBuffer[k].m_Position;
							CheckLine(entity, line, isNative, startTiles);
							line.a = line.b;
						}
					}
				}
				startTiles.Dispose();
				return;
			}
			bool isNative2 = chunk.Has(ref m_NativeType);
			Line3.Segment line2 = default(Line3.Segment);
			for (int l = 0; l < nativeArray.Length; l++)
			{
				Entity area = nativeArray[l];
				DynamicBuffer<Node> dynamicBuffer2 = bufferAccessor[l];
				if (dynamicBuffer2.Length >= 2)
				{
					line2.a = dynamicBuffer2[dynamicBuffer2.Length - 1].m_Position;
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						line2.b = dynamicBuffer2[m].m_Position;
						CheckLine(area, line2, isNative2, default(NativeParallelHashSet<Entity>));
						line2.a = line2.b;
					}
				}
			}
		}

		private void CheckLine(Entity area, Line3.Segment line, bool isNative, NativeParallelHashSet<Entity> startTiles)
		{
			Iterator iterator = new Iterator
			{
				m_Line = line.xz,
				m_Area = area,
				m_StartTiles = startTiles,
				m_MapTileData = m_MapTileData,
				m_NativeData = m_NativeData,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles,
				m_EditorMode = m_EditorMode,
				m_IsNative = isNative
			};
			m_SearchTree.Iterate(ref iterator);
			if (!iterator.m_TileFound)
			{
				m_BoundaryQueue.Enqueue(new Boundary
				{
					m_Line = line,
					m_Color = (isNative ? m_MapBorderColor : m_CityBorderColor)
				});
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillBoundaryMeshDataJob : IJob
	{
		[ReadOnly]
		public float m_Width;

		[ReadOnly]
		public float m_TilingLength;

		public NativeQueue<Boundary> m_BoundaryQueue;

		public NativeList<float3> m_Vertices;

		public NativeList<float2> m_UVs;

		public NativeList<Color32> m_Colors;

		public NativeList<int> m_Indices;

		public NativeValue<Bounds3> m_Bounds;

		public void Execute()
		{
			int count = m_BoundaryQueue.Count;
			m_Vertices.Clear();
			m_UVs.Clear();
			m_Colors.Clear();
			m_Indices.Clear();
			int value = 0;
			Bounds3 value2 = new Bounds3(float.MaxValue, float.MinValue);
			for (int i = 0; i < count; i++)
			{
				Boundary boundary = m_BoundaryQueue.Dequeue();
				float num = MathUtils.Length(boundary.m_Line);
				if (num >= 1f)
				{
					float3 @float = new float3
					{
						xz = MathUtils.Right(boundary.m_Line.ab.xz) * (m_Width * 0.5f / num)
					};
					int num2 = math.max(1, Mathf.RoundToInt(num / m_TilingLength));
					float num3 = 1f / (float)num2;
					value2 |= boundary.m_Line.a + @float;
					value2 |= boundary.m_Line.a - @float;
					value2 |= boundary.m_Line.b + @float;
					value2 |= boundary.m_Line.b - @float;
					for (int j = 0; j < num2; j++)
					{
						float2 t = new float2((float)j + 0.25f, (float)j + 0.75f) * num3;
						Line3.Segment segment = MathUtils.Cut(boundary.m_Line, t);
						m_Indices.Add(in value);
						m_Indices.Add(value + 1);
						m_Indices.Add(value + 2);
						m_Indices.Add(value + 2);
						m_Indices.Add(value + 1);
						m_Indices.Add(value + 3);
						m_Vertices.Add(segment.a + @float);
						m_UVs.Add(new float2(1f, 0f));
						m_Colors.Add(in boundary.m_Color);
						m_Vertices.Add(segment.a - @float);
						m_UVs.Add(new float2(0f, 0f));
						m_Colors.Add(in boundary.m_Color);
						m_Vertices.Add(segment.b + @float);
						m_UVs.Add(new float2(1f, 1f));
						m_Colors.Add(in boundary.m_Color);
						m_Vertices.Add(segment.b - @float);
						m_UVs.Add(new float2(0f, 1f));
						m_Colors.Add(in boundary.m_Color);
						value += 4;
					}
				}
			}
			m_Bounds.value = value2;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<MapTile> __Game_Areas_MapTile_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentLookup = state.GetComponentLookup<MapTile>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private MapTileSystem m_MapTileSystem;

	private TerrainSystem m_TerrainSystem;

	private SearchSystem m_AreaSearchSystem;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_SettingsQuery;

	private Mesh m_BoundaryMesh;

	private Material m_BoundaryMaterial;

	private JobHandle m_MeshDependencies;

	private NativeList<float3> m_Vertices;

	private NativeList<float2> m_UVs;

	private NativeList<Color32> m_Colors;

	private NativeList<int> m_Indices;

	private NativeValue<Bounds3> m_Bounds;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_MapTileSystem = base.World.GetOrCreateSystemManaged<MapTileSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<MapTile>(),
				ComponentType.ReadOnly<Area>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Node>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<CityBoundaryData>());
		RequireForUpdate(m_SettingsQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		Clear();
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		Clear();
		m_Loaded = true;
	}

	private void Clear()
	{
		DisposeMeshData();
		DestroyMesh();
		m_BoundaryMaterial = null;
	}

	private void DestroyMesh()
	{
		if (m_BoundaryMesh != null)
		{
			Object.Destroy(m_BoundaryMesh);
			m_BoundaryMesh = null;
		}
	}

	private void DisposeMeshData()
	{
		if (m_Vertices.IsCreated)
		{
			m_MeshDependencies.Complete();
			m_MeshDependencies = default(JobHandle);
			m_Vertices.Dispose();
			m_UVs.Dispose();
			m_Colors.Dispose();
			m_Indices.Dispose();
			m_Bounds.Dispose();
		}
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
		if (GetLoaded() || !m_UpdatedQuery.IsEmptyIgnoreFilter)
		{
			DisposeMeshData();
			CityBoundaryPrefab prefab = m_PrefabSystem.GetPrefab<CityBoundaryPrefab>(m_SettingsQuery.GetSingletonEntity());
			m_BoundaryMaterial = prefab.m_Material;
			NativeQueue<Boundary> boundaryQueue = new NativeQueue<Boundary>(Allocator.TempJob);
			m_Vertices = new NativeList<float3>(Allocator.Persistent);
			m_UVs = new NativeList<float2>(Allocator.Persistent);
			m_Colors = new NativeList<Color32>(Allocator.Persistent);
			m_Indices = new NativeList<int>(Allocator.Persistent);
			m_Bounds = new NativeValue<Bounds3>(Allocator.Persistent);
			JobHandle dependencies;
			FillBoundaryQueueJob jobData = new FillBoundaryQueueJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NativeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_MapTileData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_SearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
				m_StartTiles = m_MapTileSystem.GetStartTiles(),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_CityBorderColor = prefab.m_CityBorderColor.linear,
				m_MapBorderColor = prefab.m_MapBorderColor.linear,
				m_BoundaryQueue = boundaryQueue.AsParallelWriter()
			};
			FillBoundaryMeshDataJob jobData2 = new FillBoundaryMeshDataJob
			{
				m_Width = prefab.m_Width,
				m_TilingLength = prefab.m_TilingLength,
				m_BoundaryQueue = boundaryQueue,
				m_Vertices = m_Vertices,
				m_UVs = m_UVs,
				m_Colors = m_Colors,
				m_Indices = m_Indices,
				m_Bounds = m_Bounds
			};
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_MapTileQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			boundaryQueue.Dispose(jobHandle2);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
			m_MeshDependencies = jobHandle2;
			base.Dependency = jobHandle;
		}
	}

	public bool GetBoundaryMesh(out Mesh mesh, out Material material)
	{
		if (m_Vertices.IsCreated)
		{
			m_MeshDependencies.Complete();
			m_MeshDependencies = default(JobHandle);
			if (m_Vertices.Length != 0)
			{
				if (m_BoundaryMesh == null)
				{
					m_BoundaryMesh = new Mesh();
					m_BoundaryMesh.name = "City boundaries";
				}
				else
				{
					m_BoundaryMesh.Clear();
				}
				m_BoundaryMesh.SetVertices(m_Vertices.AsArray());
				m_BoundaryMesh.SetUVs(0, m_UVs.AsArray());
				m_BoundaryMesh.SetColors(m_Colors.AsArray());
				m_BoundaryMesh.SetIndices(m_Indices.AsArray(), MeshTopology.Triangles, 0, calculateBounds: false);
				float2 heightScaleOffset = m_TerrainSystem.heightScaleOffset;
				Bounds3 value = m_Bounds.value;
				value.min.y = heightScaleOffset.y;
				value.max.y = heightScaleOffset.y + heightScaleOffset.x;
				m_BoundaryMesh.bounds = RenderingUtils.ToBounds(value);
			}
			else
			{
				DestroyMesh();
			}
			m_Vertices.Dispose();
			m_UVs.Dispose();
			m_Colors.Dispose();
			m_Indices.Dispose();
			m_Bounds.Dispose();
		}
		mesh = m_BoundaryMesh;
		material = m_BoundaryMaterial;
		return m_BoundaryMesh != null;
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
	public CityBoundaryMeshSystem()
	{
	}
}
