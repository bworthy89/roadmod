using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class RouteRenderSystem : GameSystemBase
{
	[Flags]
	private enum QueryFlags
	{
		TransportLine = 1,
		WorkRoute = 2,
		LivePath = 4,
		VerifiedPath = 8
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RouteBufferIndex> __Game_Rendering_RouteBufferIndex_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.Color> __Game_Routes_Color_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Highlighted> __Game_Tools_Highlighted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Rendering_RouteBufferIndex_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RouteBufferIndex>(isReadOnly: true);
			__Game_Routes_Color_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.Color>(isReadOnly: true);
			__Game_Tools_Highlighted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Highlighted>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private RouteBufferSystem m_RouteBufferSystem;

	private RenderingSystem m_RenderingSystem;

	private EntityQuery m_InfomodeQuery;

	private Dictionary<QueryFlags, EntityQuery> m_RouteQueries;

	private Mesh m_Mesh;

	private ComputeBuffer m_ArgsBuffer;

	private List<uint> m_ArgsArray;

	private HashSet<Entity> m_RouteSet;

	private int m_RouteSegmentBuffer;

	private int m_RouteColor;

	private int m_RouteSize;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_RouteBufferSystem = base.World.GetOrCreateSystemManaged<RouteBufferSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_InfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewRouteData>());
		m_RouteQueries = new Dictionary<QueryFlags, EntityQuery>();
		m_RouteSegmentBuffer = Shader.PropertyToID("colossal_RouteSegmentBuffer");
		m_RouteColor = Shader.PropertyToID("colossal_RouteColor");
		m_RouteSize = Shader.PropertyToID("colossal_RouteSize");
		RenderPipelineManager.beginContextRendering += Render;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		RenderPipelineManager.beginContextRendering -= Render;
		if (m_Mesh != null)
		{
			UnityEngine.Object.Destroy(m_Mesh);
		}
		if (m_ArgsBuffer != null)
		{
			m_ArgsBuffer.Release();
		}
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	private void Render(ScriptableRenderContext context, List<Camera> cameras)
	{
		try
		{
			Entity selected = m_ToolSystem.selected;
			NativeArray<ArchetypeChunk> nativeArray = default(NativeArray<ArchetypeChunk>);
			EntityQuery query;
			bool flag = ShouldRenderRoutes(out query);
			HashSet<Entity> routeSet;
			bool flag2 = ShouldRenderRoutes(out routeSet);
			if (!flag && !flag2)
			{
				return;
			}
			try
			{
				int num = 0;
				if (flag)
				{
					nativeArray = query.ToArchetypeChunkArray(Allocator.Temp);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						num += nativeArray[i].Count * 5;
					}
				}
				if (flag2)
				{
					num += routeSet.Count * 5;
				}
				if (m_ArgsArray == null)
				{
					m_ArgsArray = new List<uint>(num);
				}
				else
				{
					m_ArgsArray.Clear();
				}
				if (m_ArgsBuffer != null && m_ArgsBuffer.count < num)
				{
					m_ArgsBuffer.Release();
					m_ArgsBuffer = null;
				}
				if (m_ArgsBuffer == null)
				{
					m_ArgsBuffer = new ComputeBuffer(num, 4, ComputeBufferType.DrawIndirect);
					m_ArgsBuffer.name = "Route args buffer";
				}
				EnsureMesh();
				uint indexCount = m_Mesh.GetIndexCount(0);
				uint indexStart = m_Mesh.GetIndexStart(0);
				uint baseVertex = m_Mesh.GetBaseVertex(0);
				if (flag)
				{
					CompleteDependency();
					EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
					ComponentTypeHandle<RouteBufferIndex> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_RouteBufferIndex_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					ComponentTypeHandle<Game.Routes.Color> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Color_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					ComponentTypeHandle<Highlighted> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Highlighted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					ComponentTypeHandle<Temp> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						ArchetypeChunk archetypeChunk = nativeArray[j];
						NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
						NativeArray<RouteBufferIndex> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
						NativeArray<Game.Routes.Color> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
						NativeArray<Temp> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle4);
						bool flag3 = archetypeChunk.Has(ref typeHandle3);
						for (int k = 0; k < nativeArray3.Length; k++)
						{
							if (!flag2 || !routeSet.Contains(nativeArray2[k]))
							{
								RouteBufferIndex routeBufferIndex = nativeArray3[k];
								Game.Routes.Color color = nativeArray4[k];
								Temp value;
								bool isHighlighted = flag3 || (CollectionUtils.TryGet(nativeArray5, k, out value) && (value.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify)) != 0);
								RenderRoute(cameras, indexCount, indexStart, baseVertex, routeBufferIndex, color, isHighlighted);
							}
						}
					}
				}
				if (flag2)
				{
					foreach (Entity item in routeSet)
					{
						RouteBufferIndex componentData = base.EntityManager.GetComponentData<RouteBufferIndex>(item);
						Game.Routes.Color componentData2 = base.EntityManager.GetComponentData<Game.Routes.Color>(item);
						bool isHighlighted2 = base.EntityManager.HasComponent<Highlighted>(item) || selected == item;
						RenderRoute(cameras, indexCount, indexStart, baseVertex, componentData, componentData2, isHighlighted2);
					}
				}
			}
			finally
			{
				if (nativeArray.IsCreated)
				{
					nativeArray.Dispose();
				}
			}
			if (m_ArgsArray.Count > 0)
			{
				m_ArgsBuffer.SetData(m_ArgsArray, 0, 0, m_ArgsArray.Count);
			}
		}
		finally
		{
		}
	}

	private void RenderRoute(List<Camera> cameras, uint indexCount, uint indexStart, uint baseVertex, RouteBufferIndex routeBufferIndex, Game.Routes.Color color, bool isHighlighted)
	{
		m_RouteBufferSystem.GetBuffer(routeBufferIndex.m_Index, out var material, out var segmentBuffer, out var originalRenderQueue, out var bounds, out var size);
		if (material == null || segmentBuffer == null)
		{
			return;
		}
		int count = m_ArgsArray.Count;
		m_ArgsArray.Add(indexCount);
		m_ArgsArray.Add((uint)segmentBuffer.count);
		m_ArgsArray.Add(indexStart);
		m_ArgsArray.Add(baseVertex);
		m_ArgsArray.Add(0u);
		if (isHighlighted)
		{
			color.m_Color.a = byte.MaxValue;
			size.x *= 1.3333334f;
			material.renderQueue = originalRenderQueue + 1;
		}
		else
		{
			color.m_Color.a = 128;
			material.renderQueue = originalRenderQueue;
		}
		material.SetBuffer(m_RouteSegmentBuffer, segmentBuffer);
		material.SetColor(m_RouteColor, color.m_Color);
		material.SetVector(m_RouteSize, size);
		bounds.Expand(size.x);
		foreach (Camera camera in cameras)
		{
			if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
			{
				Graphics.DrawMeshInstancedIndirect(m_Mesh, 0, material, bounds, m_ArgsBuffer, count * 4, null, ShadowCastingMode.Off, receiveShadows: false, 0, camera);
			}
		}
	}

	private bool ShouldRenderRoutes(out HashSet<Entity> routeSet)
	{
		if (m_RouteSet == null)
		{
			m_RouteSet = new HashSet<Entity>();
		}
		else
		{
			m_RouteSet.Clear();
		}
		Entity entity = m_ToolSystem.selected;
		if (!TryAddRoute(entity))
		{
			if (base.EntityManager.HasComponent<Building>(entity) && base.EntityManager.TryGetComponent<Attached>(entity, out var component))
			{
				entity = component.m_Parent;
			}
			if (base.EntityManager.TryGetComponent<CurrentRoute>(entity, out var component2))
			{
				TryAddRoute(component2.m_Route);
			}
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer))
			{
				AddRoutes(buffer);
			}
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<SubRoute> buffer2))
			{
				AddRoutes(buffer2);
			}
		}
		routeSet = m_RouteSet;
		return m_RouteSet.Count != 0;
	}

	private bool TryAddRoute(Entity entity)
	{
		if (base.EntityManager.HasComponent<Route>(entity))
		{
			m_RouteSet.Add(entity);
			return true;
		}
		return false;
	}

	private void AddRoutes(DynamicBuffer<Game.Objects.SubObject> subObjects)
	{
		for (int i = 0; i < subObjects.Length; i++)
		{
			Game.Objects.SubObject subObject = subObjects[i];
			if (base.EntityManager.TryGetBuffer(subObject.m_SubObject, isReadOnly: true, out DynamicBuffer<ConnectedRoute> buffer))
			{
				AddRoutes(buffer);
			}
			if (base.EntityManager.TryGetBuffer(subObject.m_SubObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer2))
			{
				AddRoutes(buffer2);
			}
		}
	}

	private void AddRoutes(DynamicBuffer<ConnectedRoute> connectedRoutes)
	{
		for (int i = 0; i < connectedRoutes.Length; i++)
		{
			ConnectedRoute connectedRoute = connectedRoutes[i];
			if (base.EntityManager.TryGetComponent<Owner>(connectedRoute.m_Waypoint, out var component))
			{
				TryAddRoute(component.m_Owner);
			}
		}
	}

	private void AddRoutes(DynamicBuffer<SubRoute> subRoutes)
	{
		for (int i = 0; i < subRoutes.Length; i++)
		{
			TryAddRoute(subRoutes[i].m_Route);
		}
	}

	private bool ShouldRenderRoutes(out EntityQuery query)
	{
		QueryFlags queryFlags = (QueryFlags)0;
		if (m_ToolSystem.activeTool != null)
		{
			queryFlags |= GetQueryFlag(m_ToolSystem.activeTool.requireRoutes);
		}
		if (!m_InfomodeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<InfoviewRouteData> nativeArray = m_InfomodeQuery.ToComponentDataArray<InfoviewRouteData>(Allocator.Temp);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				queryFlags |= GetQueryFlag(nativeArray[i].m_Type);
			}
			nativeArray.Dispose();
		}
		if (!m_RenderingSystem.hideOverlay)
		{
			queryFlags |= QueryFlags.LivePath | QueryFlags.VerifiedPath;
		}
		if (queryFlags == (QueryFlags)0)
		{
			query = default(EntityQuery);
			return false;
		}
		if (!m_RouteQueries.TryGetValue(queryFlags, out query))
		{
			List<ComponentType> list = new List<ComponentType>(5) { ComponentType.ReadOnly<Highlighted>() };
			if ((queryFlags & QueryFlags.TransportLine) != 0)
			{
				list.Add(ComponentType.ReadOnly<TransportLine>());
			}
			if ((queryFlags & QueryFlags.WorkRoute) != 0)
			{
				list.Add(ComponentType.ReadOnly<WorkRoute>());
			}
			if ((queryFlags & QueryFlags.LivePath) != 0)
			{
				list.Add(ComponentType.ReadOnly<LivePath>());
			}
			if ((queryFlags & QueryFlags.VerifiedPath) != 0)
			{
				list.Add(ComponentType.ReadOnly<VerifiedPath>());
			}
			query = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<Route>() },
				Any = list.ToArray(),
				None = new ComponentType[3]
				{
					ComponentType.ReadOnly<HiddenRoute>(),
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Hidden>()
				}
			});
			m_RouteQueries.Add(queryFlags, query);
		}
		return !query.IsEmptyIgnoreFilter;
	}

	private QueryFlags GetQueryFlag(RouteType routeType)
	{
		return routeType switch
		{
			RouteType.TransportLine => QueryFlags.TransportLine, 
			RouteType.WorkRoute => QueryFlags.WorkRoute, 
			_ => (QueryFlags)0, 
		};
	}

	private void EnsureMesh()
	{
		if (!(m_Mesh == null))
		{
			return;
		}
		Vector3[] array = new Vector3[68];
		Vector2[] array2 = new Vector2[array.Length];
		int[] array3 = new int[192];
		int num = 0;
		int num2 = 0;
		for (int i = 0; i <= 16; i++)
		{
			float num3 = (float)i / 16f;
			array[num] = new Vector3(-1f, 0f, num3);
			array2[num] = new Vector2(0f, num3);
			num++;
			array[num] = new Vector3(1f, 0f, num3);
			array2[num] = new Vector2(0f, num3);
			num++;
			if (i != 0)
			{
				array3[num2++] = num - 4;
				array3[num2++] = num - 3;
				array3[num2++] = num - 2;
				array3[num2++] = num - 2;
				array3[num2++] = num - 3;
				array3[num2++] = num - 1;
			}
		}
		for (int j = 0; j <= 16; j++)
		{
			float num4 = (float)j / 16f;
			array[num] = new Vector3(0f, -1f, num4);
			array2[num] = new Vector2(1f, num4);
			num++;
			array[num] = new Vector3(0f, 1f, num4);
			array2[num] = new Vector2(1f, num4);
			num++;
			if (j != 0)
			{
				array3[num2++] = num - 4;
				array3[num2++] = num - 3;
				array3[num2++] = num - 2;
				array3[num2++] = num - 2;
				array3[num2++] = num - 3;
				array3[num2++] = num - 1;
			}
		}
		m_Mesh = new Mesh();
		m_Mesh.name = "Route segment";
		m_Mesh.vertices = array;
		m_Mesh.uv = array2;
		m_Mesh.triangles = array3;
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
	public RouteRenderSystem()
	{
	}
}
