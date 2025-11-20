using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class AirwaySystem : GameSystemBase, IJobSerializable
{
	[BurstCompile]
	internal struct SerializeJob<TWriter> : IJob where TWriter : struct, IWriter
	{
		[ReadOnly]
		public AirwayHelpers.AirwayMap m_HelicopterMap;

		[ReadOnly]
		public AirwayHelpers.AirwayMap m_AirplaneMap;

		public EntityWriterData m_WriterData;

		public void Execute()
		{
			TWriter writer = m_WriterData.GetWriter<TWriter>();
			m_HelicopterMap.Serialize(writer);
			m_AirplaneMap.Serialize(writer);
		}
	}

	[BurstCompile]
	internal struct DeserializeJob<TReader> : IJob where TReader : struct, IReader
	{
		public AirwayHelpers.AirwayMap m_HelicopterMap;

		public AirwayHelpers.AirwayMap m_AirplaneMap;

		public EntityReaderData m_ReaderData;

		public void Execute()
		{
			TReader reader = m_ReaderData.GetReader<TReader>();
			m_HelicopterMap.Deserialize(reader);
			if (reader.context.version >= Version.airplaneAirways)
			{
				m_AirplaneMap.Deserialize(reader);
			}
			else
			{
				m_AirplaneMap.SetDefaults(reader.context);
			}
		}
	}

	[BurstCompile]
	private struct SetDefaultsJob : IJob
	{
		[ReadOnly]
		public Context m_Context;

		public AirwayHelpers.AirwayMap m_HelicopterMap;

		public AirwayHelpers.AirwayMap m_AirplaneMap;

		public void Execute()
		{
			m_HelicopterMap.SetDefaults(m_Context);
			m_AirplaneMap.SetDefaults(m_Context);
		}
	}

	[BurstCompile]
	private struct GenerateAirwayLanesJob : IJobParallelFor
	{
		[ReadOnly]
		public AirwayHelpers.AirwayMap m_AirwayMap;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public RoadTypes m_RoadType;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Lane> m_LaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Curve> m_CurveData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		public void Execute(int entityIndex)
		{
			AirwayHelpers.LaneDirection direction;
			int2 cellIndex = m_AirwayMap.GetCellIndex(entityIndex, out direction);
			switch (direction)
			{
			case AirwayHelpers.LaneDirection.HorizontalZ:
				CreateLane(entityIndex, cellIndex, new int2(cellIndex.x, cellIndex.y + 1));
				break;
			case AirwayHelpers.LaneDirection.HorizontalX:
				CreateLane(entityIndex, cellIndex, new int2(cellIndex.x + 1, cellIndex.y));
				break;
			case AirwayHelpers.LaneDirection.Diagonal:
				CreateLane(entityIndex, cellIndex, cellIndex + 1);
				break;
			case AirwayHelpers.LaneDirection.DiagonalCross:
				CreateLane(entityIndex, new int2(cellIndex.x + 1, cellIndex.y), new int2(cellIndex.x, cellIndex.y + 1));
				break;
			}
		}

		private void CreateLane(int entityIndex, int2 startNode, int2 endNode)
		{
			Entity entity = m_AirwayMap.entities[entityIndex];
			Lane value = default(Lane);
			value.m_StartNode = m_AirwayMap.GetPathNode(startNode);
			value.m_MiddleNode = new PathNode(entity, 1);
			value.m_EndNode = m_AirwayMap.GetPathNode(endNode);
			float3 nodePosition = m_AirwayMap.GetNodePosition(startNode);
			float3 nodePosition2 = m_AirwayMap.GetNodePosition(endNode);
			nodePosition.y += WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, nodePosition);
			nodePosition2.y += WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, nodePosition2);
			Curve value2 = default(Curve);
			value2.m_Bezier = NetUtils.StraightCurve(nodePosition, nodePosition2);
			value2.m_Length = math.distance(value2.m_Bezier.a, value2.m_Bezier.d);
			ConnectionLane value3 = default(ConnectionLane);
			value3.m_AccessRestriction = Entity.Null;
			value3.m_Flags = ConnectionLaneFlags.AllowMiddle | ConnectionLaneFlags.Airway;
			value3.m_TrackTypes = TrackTypes.None;
			value3.m_RoadTypes = m_RoadType;
			m_PrefabRefData[entity] = new PrefabRef(m_Prefab);
			m_LaneData[entity] = value;
			m_CurveData[entity] = value2;
			m_ConnectionLaneData[entity] = value3;
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentLookup;

		public ComponentLookup<Lane> __Game_Net_Lane_RW_ComponentLookup;

		public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RW_ComponentLookup = state.GetComponentLookup<PrefabRef>();
			__Game_Net_Lane_RW_ComponentLookup = state.GetComponentLookup<Lane>();
			__Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>();
			__Game_Net_ConnectionLane_RW_ComponentLookup = state.GetComponentLookup<ConnectionLane>();
		}
	}

	private const float TERRAIN_SIZE = 14336f;

	private const int HELICOPTER_GRID_WIDTH = 28;

	private const int HELICOPTER_GRID_LENGTH = 28;

	private const float HELICOPTER_CELL_SIZE = 494.34482f;

	private const float HELICOPTER_PATH_HEIGHT = 200f;

	private const int AIRPLANE_GRID_WIDTH = 14;

	private const int AIRPLANE_GRID_LENGTH = 14;

	private const float AIRPLANE_CELL_SIZE = 988.68964f;

	private const float AIRPLANE_PATH_HEIGHT = 1000f;

	private LoadGameSystem m_LoadGameSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_AirplaneConnectionQuery;

	private EntityQuery m_OldConnectionQuery;

	private AirwayHelpers.AirwayData m_AirwayData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ConnectionLaneData>(), ComponentType.ReadOnly<PrefabData>());
		m_AirplaneConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<AirplaneStop>(), ComponentType.ReadOnly<Game.Routes.TakeoffLocation>(), ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_OldConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[1] { ComponentType.ReadOnly<ConnectionLane>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<OutsideConnection>(),
				ComponentType.ReadOnly<Owner>()
			}
		});
		RequireForUpdate(m_PrefabQuery);
		AirwayHelpers.AirwayMap helicopterMap = new AirwayHelpers.AirwayMap(new int2(28, 28), 494.34482f, 200f, Allocator.Persistent);
		AirwayHelpers.AirwayMap airplaneMap = new AirwayHelpers.AirwayMap(new int2(14, 14), 988.68964f, 1000f, Allocator.Persistent);
		m_AirwayData = new AirwayHelpers.AirwayData(helicopterMap, airplaneMap);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_AirwayData.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LoadGameSystem.context.purpose == Purpose.NewGame && m_OldConnectionQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_PrefabQuery.ToEntityArray(Allocator.TempJob);
			NetLaneArchetypeData componentData = base.EntityManager.GetComponentData<NetLaneArchetypeData>(nativeArray[0]);
			if (!m_AirplaneConnectionQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.AddComponent<Updated>(m_AirplaneConnectionQuery);
			}
			base.EntityManager.CreateEntity(componentData.m_LaneArchetype, m_AirwayData.helicopterMap.entities);
			base.EntityManager.CreateEntity(componentData.m_LaneArchetype, m_AirwayData.airplaneMap.entities);
			TerrainHeightData heightData = m_TerrainSystem.GetHeightData(waitForPending: true);
			JobHandle deps;
			WaterSurfaceData<SurfaceWater> surfaceData = m_WaterSystem.GetSurfaceData(out deps);
			GenerateAirwayLanesJob jobData = new GenerateAirwayLanesJob
			{
				m_AirwayMap = m_AirwayData.helicopterMap,
				m_Prefab = nativeArray[0],
				m_RoadType = RoadTypes.Helicopter,
				m_TerrainHeightData = heightData,
				m_WaterSurfaceData = surfaceData,
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			JobHandle jobHandle = new GenerateAirwayLanesJob
			{
				m_AirwayMap = m_AirwayData.airplaneMap,
				m_Prefab = nativeArray[0],
				m_RoadType = RoadTypes.Airplane,
				m_TerrainHeightData = heightData,
				m_WaterSurfaceData = surfaceData,
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RW_ComponentLookup, ref base.CheckedStateRef)
			}.Schedule(dependsOn: IJobParallelForExtensions.Schedule(jobData, m_AirwayData.helicopterMap.entities.Length, 4, JobHandle.CombineDependencies(base.Dependency, deps)), arrayLength: m_AirwayData.airplaneMap.entities.Length, innerloopBatchCount: 4);
			nativeArray.Dispose();
			m_TerrainSystem.AddCPUHeightReader(jobHandle);
			m_WaterSystem.AddSurfaceReader(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	public AirwayHelpers.AirwayData GetAirwayData()
	{
		return m_AirwayData;
	}

	public JobHandle Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps) where TWriter : struct, IWriter
	{
		return IJobExtensions.Schedule(new SerializeJob<TWriter>
		{
			m_HelicopterMap = m_AirwayData.helicopterMap,
			m_AirplaneMap = m_AirwayData.airplaneMap,
			m_WriterData = writerData
		}, inputDeps);
	}

	public JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps) where TReader : struct, IReader
	{
		return IJobExtensions.Schedule(new DeserializeJob<TReader>
		{
			m_HelicopterMap = m_AirwayData.helicopterMap,
			m_AirplaneMap = m_AirwayData.airplaneMap,
			m_ReaderData = readerData
		}, inputDeps);
	}

	public JobHandle SetDefaults(Context context)
	{
		return IJobExtensions.Schedule(new SetDefaultsJob
		{
			m_Context = context,
			m_HelicopterMap = m_AirwayData.helicopterMap,
			m_AirplaneMap = m_AirwayData.airplaneMap
		});
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
	public AirwaySystem()
	{
	}
}
