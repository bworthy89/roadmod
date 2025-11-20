using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class MapRequirementSystem : GameSystemBase
{
	[BurstCompile]
	public struct CollectResourcesJob : IJob
	{
		[ReadOnly]
		[DeallocateOnJobCompletion]
		public NativeArray<ArchetypeChunk> m_AreaChunks;

		[ReadOnly]
		public BufferTypeHandle<MapFeatureElement> m_MapFeatureElementType;

		public NativeArray<bool> m_Results;

		public void Execute()
		{
			for (int i = 0; i < m_Results.Length; i++)
			{
				m_Results[i] = false;
			}
			for (int j = 0; j < m_AreaChunks.Length; j++)
			{
				Check(m_AreaChunks[j].GetBufferAccessor(ref m_MapFeatureElementType));
			}
		}

		private void Check(BufferAccessor<MapFeatureElement> mapFeatureAccessor)
		{
			for (int i = 0; i < mapFeatureAccessor.Length; i++)
			{
				DynamicBuffer<MapFeatureElement> dynamicBuffer = mapFeatureAccessor[i];
				for (int j = 0; j < 9; j++)
				{
					m_Results[j] |= dynamicBuffer[j].m_Amount > 0f;
				}
			}
		}
	}

	[BurstCompile]
	public struct CollectStartingResourcesJob : IJob
	{
		[ReadOnly]
		public NativeArray<Entity> m_StartingTiles;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		public NativeArray<bool> m_Results;

		public void Execute()
		{
			for (int i = 0; i < m_Results.Length; i++)
			{
				m_Results[i] = false;
			}
			for (int j = 0; j < m_StartingTiles.Length; j++)
			{
				Check(m_StartingTiles[j]);
			}
		}

		private void Check(Entity entity)
		{
			DynamicBuffer<MapFeatureElement> dynamicBuffer = m_MapFeatureElements[entity];
			for (int i = 0; i < 9; i++)
			{
				m_Results[i] |= dynamicBuffer[i].m_Amount > 0f;
			}
		}
	}

	[BurstCompile]
	public struct CheckWaterJob : IJob
	{
		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_SurfaceData;

		[ReadOnly]
		public NativeArray<Entity> m_StartingTiles;

		[ReadOnly]
		public ComponentLookup<Geometry> m_GeometryData;

		public NativeValue<bool> m_Result;

		public void Execute()
		{
			m_Result.value = false;
			for (int i = 0; i < m_StartingTiles.Length; i++)
			{
				if (HasWater(m_GeometryData[m_StartingTiles[i]].m_Bounds))
				{
					m_Result.value = true;
					break;
				}
			}
		}

		private bool HasWater(Bounds3 bounds)
		{
			int2 @int = math.clamp((int2)math.floor(WaterUtils.ToSurfaceSpace(ref m_SurfaceData, bounds.min).xz), int2.zero, m_SurfaceData.resolution.xz);
			int2 int2 = math.clamp((int2)math.floor(WaterUtils.ToSurfaceSpace(ref m_SurfaceData, bounds.max).xz), int2.zero, m_SurfaceData.resolution.xz);
			for (int i = @int.y; i < int2.y; i++)
			{
				for (int j = @int.x; j < int2.x; j++)
				{
					if (m_SurfaceData.depths[i * m_SurfaceData.resolution.x + j].m_Depth > 0f)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Areas_MapFeatureElement_RO_BufferLookup = state.GetBufferLookup<MapFeatureElement>(isReadOnly: true);
			__Game_Areas_MapFeatureElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<MapFeatureElement>(isReadOnly: true);
		}
	}

	private MapTileSystem m_MapTileSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_TileQuery;

	private EntityQuery m_OutsideRoadNodeQuery;

	private EntityQuery m_OutsideTrainNodeQuery;

	private EntityQuery m_OutsideAirNodeQuery;

	private EntityQuery m_OutsideElectricityConnectionQuery;

	private JobHandle m_ResultDependency;

	private NativeValue<bool> m_WaterResult;

	private NativeArray<bool> m_StartingAreaResources;

	private NativeArray<bool> m_MapResources;

	private TypeHandle __TypeHandle;

	public bool hasStartingArea { get; private set; }

	public bool roadConnection { get; private set; }

	public bool trainConnection { get; private set; }

	public bool airConnection { get; private set; }

	public bool electricityConnection { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_MapTileSystem = base.World.GetOrCreateSystemManaged<MapTileSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TileQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<MapTile>(),
				ComponentType.ReadOnly<Geometry>(),
				ComponentType.ReadOnly<MapFeatureElement>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_OutsideRoadNodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<Road>(),
				ComponentType.ReadOnly<Game.Net.OutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_OutsideTrainNodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<TrainTrack>(),
				ComponentType.ReadOnly<Game.Net.OutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_OutsideAirNodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<AirplaneStop>(),
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_OutsideElectricityConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ElectricityOutsideConnection>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_WaterResult = new NativeValue<bool>(Allocator.Persistent);
		m_StartingAreaResources = new NativeArray<bool>(9, Allocator.Persistent);
		m_MapResources = new NativeArray<bool>(9, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ResultDependency.Complete();
		NativeArray<Entity> startingTiles = m_MapTileSystem.GetStartTiles().ToArray(Allocator.TempJob);
		hasStartingArea = startingTiles.Length != 0;
		roadConnection = !m_OutsideRoadNodeQuery.IsEmptyIgnoreFilter;
		trainConnection = !m_OutsideTrainNodeQuery.IsEmptyIgnoreFilter;
		airConnection = !m_OutsideAirNodeQuery.IsEmptyIgnoreFilter;
		electricityConnection = !m_OutsideElectricityConnectionQuery.IsEmptyIgnoreFilter;
		JobHandle deps;
		CheckWaterJob jobData = new CheckWaterJob
		{
			m_Result = m_WaterResult,
			m_SurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_StartingTiles = startingTiles,
			m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, deps));
		m_WaterSystem.AddSurfaceReader(base.Dependency);
		CollectStartingResourcesJob jobData2 = new CollectStartingResourcesJob
		{
			m_StartingTiles = startingTiles,
			m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_StartingAreaResources
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		startingTiles.Dispose(base.Dependency);
		CollectResourcesJob jobData3 = new CollectResourcesJob
		{
			m_AreaChunks = m_TileQuery.ToArchetypeChunkArray(Allocator.TempJob),
			m_MapFeatureElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Results = m_MapResources
		};
		base.Dependency = IJobExtensions.Schedule(jobData3, base.Dependency);
		m_ResultDependency = base.Dependency;
	}

	public bool StartingAreaHasResource(MapFeature feature)
	{
		m_ResultDependency.Complete();
		switch (feature)
		{
		case MapFeature.SurfaceWater:
			if (!m_StartingAreaResources[(int)feature])
			{
				return m_WaterResult.value;
			}
			return true;
		case MapFeature.Area:
		case MapFeature.BuildableLand:
		case MapFeature.FertileLand:
		case MapFeature.Forest:
		case MapFeature.Oil:
		case MapFeature.Ore:
		case MapFeature.GroundWater:
		case MapFeature.Fish:
			return m_StartingAreaResources[(int)feature];
		default:
			return false;
		}
	}

	public bool MapHasResource(MapFeature feature)
	{
		m_ResultDependency.Complete();
		if (feature > MapFeature.None && feature < MapFeature.Count)
		{
			return m_MapResources[(int)feature];
		}
		return false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_WaterResult.Dispose();
		m_StartingAreaResources.Dispose();
		m_MapResources.Dispose();
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
	public MapRequirementSystem()
	{
	}
}
