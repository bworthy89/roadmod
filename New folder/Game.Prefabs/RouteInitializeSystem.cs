using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Routes;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class RouteInitializeSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<RouteData> __Game_Prefabs_RouteData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkRouteData> __Game_Prefabs_WorkRouteData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MailBoxData> __Game_Prefabs_MailBoxData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkStopData> __Game_Prefabs_WorkStopData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_RouteData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RouteData>();
			__Game_Prefabs_TransportLineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_WorkRouteData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkRouteData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_MailBoxData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MailBoxData>(isReadOnly: true);
			__Game_Prefabs_WorkStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkStopData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_PrefabQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<RouteData>(),
				ComponentType.ReadOnly<TransportStopData>(),
				ComponentType.ReadOnly<MailBoxData>(),
				ComponentType.ReadOnly<WorkStopData>()
			}
		});
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<RouteData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_RouteData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<TransportLineData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<WorkRouteData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WorkRouteData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<TransportStopData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<MailBoxData> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MailBoxData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<WorkStopData> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WorkStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ObjectGeometryData> typeHandle8 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		CompleteDependency();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ArchetypeChunk archetypeChunk = nativeArray[i];
			NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
			NativeArray<RouteData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
			if (nativeArray3.Length != 0)
			{
				float x = 0f;
				RouteType type = RouteType.None;
				if (archetypeChunk.Has(ref typeHandle3))
				{
					x = 8f;
					type = RouteType.TransportLine;
				}
				if (archetypeChunk.Has(ref typeHandle4))
				{
					x = 8f;
					type = RouteType.WorkRoute;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					RoutePrefab prefab = m_PrefabSystem.GetPrefab<RoutePrefab>(nativeArray2[j]);
					RouteData value = nativeArray3[j];
					value.m_SnapDistance = math.max(x, prefab.m_Width);
					value.m_Type = type;
					value.m_Color = prefab.m_Color;
					value.m_Width = prefab.m_Width;
					value.m_SegmentLength = prefab.m_SegmentLength;
					nativeArray3[j] = value;
				}
			}
			if (archetypeChunk.Has(ref typeHandle5) || archetypeChunk.Has(ref typeHandle6) || archetypeChunk.Has(ref typeHandle7))
			{
				NativeArray<ObjectGeometryData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle8);
				for (int k = 0; k < nativeArray4.Length; k++)
				{
					ObjectGeometryData value2 = nativeArray4[k];
					value2.m_Flags &= ~(GeometryFlags.Overridable | GeometryFlags.Brushable);
					nativeArray4[k] = value2;
				}
			}
		}
		nativeArray.Dispose();
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
	public RouteInitializeSystem()
	{
	}
}
