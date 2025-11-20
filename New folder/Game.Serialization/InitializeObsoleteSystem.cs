using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class InitializeObsoleteSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeObsoleteJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<StackData> m_StackType;

		[ReadOnly]
		public ComponentTypeHandle<AreaGeometryData> m_AreaGeometryType;

		public ComponentTypeHandle<ObjectData> m_ObjectType;

		public ComponentTypeHandle<MovingObjectData> m_MovingObjectType;

		public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

		public ComponentTypeHandle<NetData> m_NetType;

		public ComponentTypeHandle<NetGeometryData> m_NetGeometryType;

		public ComponentTypeHandle<AggregateNetData> m_AggregateNetType;

		public ComponentTypeHandle<NetNameData> m_NetNameType;

		public ComponentTypeHandle<NetArrowData> m_NetArrowType;

		public ComponentTypeHandle<NetLaneData> m_NetLaneType;

		public ComponentTypeHandle<NetLaneArchetypeData> m_NetLaneArchetypeType;

		public ComponentTypeHandle<AreaData> m_AreaType;

		public BufferTypeHandle<SubMesh> m_SubMeshType;

		public BufferTypeHandle<NetGeometrySection> m_NetGeometrySectionType;

		[ReadOnly]
		public MeshSettingsData m_MeshSettingsData;

		[ReadOnly]
		public EntityArchetype m_ObjectArchetype;

		[ReadOnly]
		public EntityArchetype m_ObjectGeometryArchetype;

		[ReadOnly]
		public EntityArchetype m_NetGeometryNodeArchetype;

		[ReadOnly]
		public EntityArchetype m_NetGeometryEdgeArchetype;

		[ReadOnly]
		public EntityArchetype m_NetNodeCompositionArchetype;

		[ReadOnly]
		public EntityArchetype m_NetEdgeCompositionArchetype;

		[ReadOnly]
		public EntityArchetype m_NetAggregateArchetype;

		[ReadOnly]
		public EntityArchetype m_AreaLotArchetype;

		[ReadOnly]
		public EntityArchetype m_AreaDistrictArchetype;

		[ReadOnly]
		public EntityArchetype m_AreaMapTileArchetype;

		[ReadOnly]
		public EntityArchetype m_AreaSpaceArchetype;

		[ReadOnly]
		public EntityArchetype m_AreaSurfaceArchetype;

		[ReadOnly]
		public NetLaneArchetypeData m_NetLaneArchetypeData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ObjectData> nativeArray = chunk.GetNativeArray(ref m_ObjectType);
			NativeArray<MovingObjectData> nativeArray2 = chunk.GetNativeArray(ref m_MovingObjectType);
			NativeArray<ObjectGeometryData> nativeArray3 = chunk.GetNativeArray(ref m_ObjectGeometryType);
			NativeArray<NetData> nativeArray4 = chunk.GetNativeArray(ref m_NetType);
			NativeArray<NetGeometryData> nativeArray5 = chunk.GetNativeArray(ref m_NetGeometryType);
			NativeArray<AggregateNetData> nativeArray6 = chunk.GetNativeArray(ref m_AggregateNetType);
			NativeArray<NetNameData> nativeArray7 = chunk.GetNativeArray(ref m_NetNameType);
			NativeArray<NetArrowData> nativeArray8 = chunk.GetNativeArray(ref m_NetArrowType);
			NativeArray<StackData> nativeArray9 = chunk.GetNativeArray(ref m_StackType);
			NativeArray<NetLaneData> nativeArray10 = chunk.GetNativeArray(ref m_NetLaneType);
			NativeArray<NetLaneArchetypeData> nativeArray11 = chunk.GetNativeArray(ref m_NetLaneArchetypeType);
			NativeArray<AreaData> nativeArray12 = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<AreaGeometryData> nativeArray13 = chunk.GetNativeArray(ref m_AreaGeometryType);
			BufferAccessor<SubMesh> bufferAccessor = chunk.GetBufferAccessor(ref m_SubMeshType);
			BufferAccessor<NetGeometrySection> bufferAccessor2 = chunk.GetBufferAccessor(ref m_NetGeometrySectionType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				if (nativeArray.Length != 0)
				{
					ref ObjectData reference = ref nativeArray.ElementAt(nextIndex);
					if (nativeArray3.Length != 0)
					{
						ref ObjectGeometryData reference2 = ref nativeArray3.ElementAt(nextIndex);
						reference.m_Archetype = m_ObjectGeometryArchetype;
						reference2.m_MinLod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(MathUtils.Size(reference2.m_Bounds)));
						reference2.m_Layers = MeshLayer.Default;
					}
					else
					{
						reference.m_Archetype = m_ObjectArchetype;
					}
					if (nativeArray2.Length != 0)
					{
						nativeArray2.ElementAt(nextIndex).m_StoppedArchetype = m_ObjectGeometryArchetype;
					}
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<SubMesh> dynamicBuffer = bufferAccessor[nextIndex];
					if (dynamicBuffer.Length == 0)
					{
						SubMesh elem = new SubMesh(m_MeshSettingsData.m_MissingObjectMesh, SubMeshFlags.DefaultMissingMesh, 0);
						if (nativeArray9.Length != 0)
						{
							elem.m_Flags |= SubMeshFlags.IsStackMiddle;
						}
						dynamicBuffer.Add(elem);
					}
				}
				if (nativeArray4.Length != 0)
				{
					ref NetData reference3 = ref nativeArray4.ElementAt(nextIndex);
					if (nativeArray5.Length != 0)
					{
						ref NetGeometryData reference4 = ref nativeArray5.ElementAt(nextIndex);
						reference3.m_NodeArchetype = m_NetGeometryNodeArchetype;
						reference3.m_EdgeArchetype = m_NetGeometryEdgeArchetype;
						reference4.m_NodeCompositionArchetype = m_NetNodeCompositionArchetype;
						reference4.m_EdgeCompositionArchetype = m_NetEdgeCompositionArchetype;
					}
				}
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<NetGeometrySection> dynamicBuffer2 = bufferAccessor2[nextIndex];
					if (dynamicBuffer2.Length == 0)
					{
						NetGeometrySection elem2 = new NetGeometrySection
						{
							m_Section = m_MeshSettingsData.m_MissingNetSection
						};
						dynamicBuffer2.Add(elem2);
					}
				}
				if (nativeArray6.Length != 0)
				{
					ref AggregateNetData reference5 = ref nativeArray6.ElementAt(nextIndex);
					if (nativeArray7.Length != 0)
					{
						ref NetNameData reference6 = ref nativeArray7.ElementAt(nextIndex);
						reference6.m_Color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 192);
						reference6.m_SelectedColor = new Color32(192, 192, byte.MaxValue, 192);
					}
					if (nativeArray8.Length != 0)
					{
						ref NetArrowData reference7 = ref nativeArray8.ElementAt(nextIndex);
						reference7.m_RoadColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 192);
						reference7.m_TrackColor = new Color32(byte.MaxValue, byte.MaxValue, 192, 192);
					}
					reference5.m_Archetype = m_NetAggregateArchetype;
				}
				if (nativeArray10.Length != 0)
				{
					nativeArray10.ElementAt(nextIndex).m_Flags &= ~LaneFlags.PseudoRandom;
					nativeArray11[nextIndex] = m_NetLaneArchetypeData;
				}
				if (nativeArray12.Length == 0)
				{
					continue;
				}
				ref AreaData reference8 = ref nativeArray12.ElementAt(nextIndex);
				if (CollectionUtils.TryGet(nativeArray13, nextIndex, out var value))
				{
					switch (value.m_Type)
					{
					case AreaType.Lot:
						reference8.m_Archetype = m_AreaLotArchetype;
						break;
					case AreaType.District:
						reference8.m_Archetype = m_AreaDistrictArchetype;
						break;
					case AreaType.MapTile:
						reference8.m_Archetype = m_AreaMapTileArchetype;
						break;
					case AreaType.Space:
						reference8.m_Archetype = m_AreaSpaceArchetype;
						break;
					case AreaType.Surface:
						reference8.m_Archetype = m_AreaSurfaceArchetype;
						break;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<StackData> __Game_Prefabs_StackData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectData> __Game_Prefabs_ObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<MovingObjectData> __Game_Prefabs_MovingObjectData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetData> __Game_Prefabs_NetData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetGeometryData> __Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AggregateNetData> __Game_Prefabs_AggregateNetData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetNameData> __Game_Prefabs_NetNameData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetArrowData> __Game_Prefabs_NetArrowData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetLaneData> __Game_Prefabs_NetLaneData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetLaneArchetypeData> __Game_Prefabs_NetLaneArchetypeData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AreaData> __Game_Prefabs_AreaData_RW_ComponentTypeHandle;

		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RW_BufferTypeHandle;

		public BufferTypeHandle<NetGeometrySection> __Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_StackData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StackData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectData>();
			__Game_Prefabs_MovingObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<MovingObjectData>();
			__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>();
			__Game_Prefabs_NetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetData>();
			__Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetGeometryData>();
			__Game_Prefabs_AggregateNetData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AggregateNetData>();
			__Game_Prefabs_NetNameData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetNameData>();
			__Game_Prefabs_NetArrowData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetArrowData>();
			__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneData>();
			__Game_Prefabs_NetLaneArchetypeData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetLaneArchetypeData>();
			__Game_Prefabs_AreaData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AreaData>();
			__Game_Prefabs_SubMesh_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>();
			__Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle = state.GetBufferTypeHandle<NetGeometrySection>();
		}
	}

	private EntityQuery m_ObsoleteQuery;

	private EntityQuery m_MeshSettingsQuery;

	private HashSet<ComponentType> m_ArchetypeComponents;

	private Dictionary<Type, PrefabBase> m_PrefabInstances;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObsoleteQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<ObjectData>(),
				ComponentType.ReadOnly<NetData>(),
				ComponentType.ReadOnly<AggregateNetData>(),
				ComponentType.ReadOnly<NetLaneArchetypeData>(),
				ComponentType.ReadOnly<AreaData>()
			},
			Disabled = new ComponentType[1] { ComponentType.ReadOnly<PrefabData>() }
		});
		m_MeshSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<MeshSettingsData>());
		RequireForUpdate(m_ObsoleteQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ArchetypeComponents = new HashSet<ComponentType>();
		m_PrefabInstances = new Dictionary<Type, PrefabBase>();
		EntityArchetype archetype = GetArchetype<ObjectPrefab>();
		EntityArchetype archetype2 = GetArchetype<StaticObjectPrefab>();
		EntityArchetype archetype3 = GetArchetype<NetGeometryPrefab, Game.Net.Node>();
		EntityArchetype archetype4 = GetArchetype<NetGeometryPrefab, Edge>();
		EntityArchetype archetype5 = GetArchetype<NetGeometryPrefab, NetCompositionData, NetCompositionCrosswalk>();
		EntityArchetype archetype6 = GetArchetype<NetGeometryPrefab, NetCompositionData, NetCompositionLane>();
		EntityArchetype archetype7 = GetArchetype<AggregateNetPrefab>();
		EntityArchetype archetype8 = GetArchetype<LotPrefab, Area>();
		EntityArchetype archetype9 = GetArchetype<DistrictPrefab, Area>();
		EntityArchetype archetype10 = GetArchetype<MapTilePrefab, Area>();
		EntityArchetype archetype11 = GetArchetype<SpacePrefab, Area>();
		EntityArchetype archetype12 = GetArchetype<SurfacePrefab, Area>();
		NetLaneArchetypeData netLaneArchetypeData = default(NetLaneArchetypeData);
		netLaneArchetypeData.m_LaneArchetype = GetArchetype<NetLanePrefab, Lane>();
		netLaneArchetypeData.m_AreaLaneArchetype = GetArchetype<NetLanePrefab, Lane, AreaLane>();
		netLaneArchetypeData.m_EdgeLaneArchetype = GetArchetype<NetLanePrefab, Lane, EdgeLane>();
		netLaneArchetypeData.m_NodeLaneArchetype = GetArchetype<NetLanePrefab, Lane, NodeLane>();
		netLaneArchetypeData.m_EdgeSlaveArchetype = GetArchetype<NetLanePrefab, Lane, SlaveLane, EdgeLane>();
		netLaneArchetypeData.m_NodeSlaveArchetype = GetArchetype<NetLanePrefab, Lane, SlaveLane, NodeLane>();
		netLaneArchetypeData.m_EdgeMasterArchetype = GetArchetype<NetLanePrefab, Lane, MasterLane, EdgeLane>();
		netLaneArchetypeData.m_NodeMasterArchetype = GetArchetype<NetLanePrefab, Lane, MasterLane, NodeLane>();
		foreach (KeyValuePair<Type, PrefabBase> item in m_PrefabInstances)
		{
			UnityEngine.Object.DestroyImmediate(item.Value);
		}
		m_ArchetypeComponents = null;
		m_PrefabInstances = null;
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new InitializeObsoleteJob
		{
			m_StackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AreaGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MovingObjectData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AggregateNetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AggregateNetData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetNameType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetNameData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetArrowType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetArrowData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetLaneArchetypeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetLaneArchetypeData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AreaData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubMeshType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_NetGeometrySectionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_NetGeometrySection_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_MeshSettingsData = m_MeshSettingsQuery.GetSingleton<MeshSettingsData>(),
			m_ObjectArchetype = archetype,
			m_ObjectGeometryArchetype = archetype2,
			m_NetGeometryNodeArchetype = archetype3,
			m_NetGeometryEdgeArchetype = archetype4,
			m_NetNodeCompositionArchetype = archetype5,
			m_NetEdgeCompositionArchetype = archetype6,
			m_NetAggregateArchetype = archetype7,
			m_AreaLotArchetype = archetype8,
			m_AreaDistrictArchetype = archetype9,
			m_AreaMapTileArchetype = archetype10,
			m_AreaSpaceArchetype = archetype11,
			m_AreaSurfaceArchetype = archetype12,
			m_NetLaneArchetypeData = netLaneArchetypeData
		}, m_ObsoleteQuery, base.Dependency);
		base.Dependency = dependency;
	}

	private T GetPrefabInstance<T>() where T : PrefabBase
	{
		if (m_PrefabInstances.TryGetValue(typeof(T), out var value))
		{
			return (T)value;
		}
		T val = ScriptableObject.CreateInstance<T>();
		m_PrefabInstances.Add(typeof(T), val);
		return val;
	}

	private EntityArchetype GetArchetype<T>() where T : PrefabBase
	{
		T prefabInstance = GetPrefabInstance<T>();
		m_ArchetypeComponents.Clear();
		prefabInstance.GetArchetypeComponents(m_ArchetypeComponents);
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Created>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Updated>());
		return base.EntityManager.CreateArchetype(PrefabUtils.ToArray(m_ArchetypeComponents));
	}

	private EntityArchetype GetArchetype<T, TComponentType>() where T : PrefabBase
	{
		T prefabInstance = GetPrefabInstance<T>();
		m_ArchetypeComponents.Clear();
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType>());
		prefabInstance.GetArchetypeComponents(m_ArchetypeComponents);
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Created>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Updated>());
		return base.EntityManager.CreateArchetype(PrefabUtils.ToArray(m_ArchetypeComponents));
	}

	private EntityArchetype GetArchetype<T, TComponentType1, TComponentType2>() where T : PrefabBase
	{
		T prefabInstance = GetPrefabInstance<T>();
		m_ArchetypeComponents.Clear();
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType1>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType2>());
		prefabInstance.GetArchetypeComponents(m_ArchetypeComponents);
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Created>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Updated>());
		return base.EntityManager.CreateArchetype(PrefabUtils.ToArray(m_ArchetypeComponents));
	}

	private EntityArchetype GetArchetype<T, TComponentType1, TComponentType2, TComponentType3>() where T : PrefabBase
	{
		T prefabInstance = GetPrefabInstance<T>();
		m_ArchetypeComponents.Clear();
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType1>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType2>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<TComponentType3>());
		prefabInstance.GetArchetypeComponents(m_ArchetypeComponents);
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Created>());
		m_ArchetypeComponents.Add(ComponentType.ReadWrite<Updated>());
		return base.EntityManager.CreateArchetype(PrefabUtils.ToArray(m_ArchetypeComponents));
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
	public InitializeObsoleteSystem()
	{
	}
}
