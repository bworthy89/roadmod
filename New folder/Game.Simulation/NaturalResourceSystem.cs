#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NaturalResourceSystem : CellMapSystem<NaturalResourceCell>, IJobSerializable, IPostDeserialize
{
	[BurstCompile]
	private struct RegenerateNaturalResourcesJob : IJobParallelFor
	{
		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public BufferLookup<MapFeatureElement> m_MapFeatureElements;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_MapFeatureElements.HasBuffer(item.m_Area))
				{
					Triangle2 triangle = AreaUtils.GetTriangle2(m_Nodes[item.m_Area], m_Triangles[item.m_Area][item.m_Triangle]);
					if (MathUtils.Intersect(m_Bounds, triangle))
					{
						m_UpdateBuffer.Enqueue(item.m_Area);
					}
				}
			}
		}

		[ReadOnly]
		public int m_ZOffset;

		[ReadOnly]
		public int m_FertilityRegenerationRate;

		[ReadOnly]
		public int m_FishRegenerationRate;

		[ReadOnly]
		public float m_PollutionRate;

		[ReadOnly]
		public float m_WaterCellFactor;

		[ReadOnly]
		public float m_MapOffset;

		[ReadOnly]
		public float m_CellSize;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public int2 m_WaterResolutionFactor;

		[ReadOnly]
		public CellMapData<GroundPollution> m_GroundPollutionData;

		[ReadOnly]
		public CellMapData<NoisePollution> m_NoisePollutionData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[NativeDisableParallelForRestriction]
		public CellMapData<NaturalResourceCell> m_CellData;

		public NativeQueue<Entity>.ParallelWriter m_UpdateQueue;

		public void Execute(int zIndex)
		{
			zIndex += m_ZOffset;
			int num = zIndex * m_CellData.m_TextureSize.x;
			int num2 = zIndex * m_WaterResolutionFactor.y * m_WaterSurfaceData.resolution.x;
			AreaIterator iterator = new AreaIterator
			{
				m_MapFeatureElements = m_MapFeatureElements,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles,
				m_UpdateBuffer = m_UpdateQueue
			};
			for (int i = 0; i < m_CellData.m_TextureSize.x; i++)
			{
				NaturalResourceCell value = m_CellData.m_Buffer[num];
				GroundPollution groundPollution = m_GroundPollutionData.m_Buffer[num];
				NoisePollution noisePollution = m_NoisePollutionData.m_Buffer[num];
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(1 + num);
				value.m_Fertility.m_Used = (ushort)math.min(value.m_Fertility.m_Base, math.max(0, value.m_Fertility.m_Used - m_FertilityRegenerationRate + MathUtils.RoundToIntRandom(ref random, (float)groundPollution.m_Pollution * m_PollutionRate)));
				int num3 = num2;
				float num4 = 0f;
				float num5 = 0f;
				for (int j = 0; j < m_WaterResolutionFactor.y; j++)
				{
					int num6 = num3;
					for (int k = 0; k < m_WaterResolutionFactor.x; k++)
					{
						SurfaceWater surfaceWater = m_WaterSurfaceData.depths[num6++];
						float num7 = math.max(0f, surfaceWater.m_Depth - 2f);
						num4 += num7;
						num5 += num7 * surfaceWater.m_Polluted;
					}
					num3 += m_WaterSurfaceData.resolution.x;
				}
				num4 *= m_WaterCellFactor;
				num5 *= m_WaterCellFactor;
				num5 += num4 * (float)noisePollution.m_Pollution * 6.25E-05f;
				int2 @int = new int2((int)math.min(10000f, num4), value.m_Fish.m_Base);
				int num8 = (int)math.clamp(num5 * 50f, 0f, 10000f);
				@int = math.select(@int, new int2(0, 20), (@int > 0) & (@int < 20));
				if (math.abs(@int.x - @int.y) >= 20)
				{
					value.m_Fish.m_Base = (ushort)@int.x;
					iterator.m_Bounds.min = m_MapOffset + new float2(i, zIndex) * m_CellSize;
					iterator.m_Bounds.max = iterator.m_Bounds.min + m_CellSize;
					m_AreaTree.Iterate(ref iterator);
				}
				if (value.m_Fish.m_Used < num8)
				{
					value.m_Fish.m_Used = (ushort)math.min(num8, value.m_Fish.m_Used + MathUtils.RoundToIntRandom(ref random, num5 * 3.125f));
				}
				else
				{
					value.m_Fish.m_Used = (ushort)math.max(num8, value.m_Fish.m_Used - m_FishRegenerationRate);
				}
				m_CellData.m_Buffer[num++] = value;
				num2 += m_WaterResolutionFactor.x;
			}
		}
	}

	[BurstCompile]
	private struct CollectUpdatedAreasJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_UpdateBuffer;

		public NativeList<Entity> m_UpdateList;

		public void Execute()
		{
			int count = m_UpdateBuffer.Count;
			m_UpdateList.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_UpdateList[i] = m_UpdateBuffer.Dequeue();
			}
			m_UpdateList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_UpdateList.Length)
			{
				Entity entity2 = m_UpdateList[num++];
				if (entity2 != entity)
				{
					m_UpdateList[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_UpdateList.Length)
			{
				m_UpdateList.RemoveRange(num2, m_UpdateList.Length - num2);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		public ComponentLookup<Extractor> __Game_Areas_Extractor_RW_ComponentLookup;

		public BufferLookup<WoodResource> __Game_Areas_WoodResource_RW_BufferLookup;

		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_MapFeatureElement_RO_BufferLookup = state.GetBufferLookup<MapFeatureElement>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Areas_Extractor_RW_ComponentLookup = state.GetComponentLookup<Extractor>();
			__Game_Areas_WoodResource_RW_BufferLookup = state.GetBufferLookup<WoodResource>();
			__Game_Areas_MapFeatureElement_RW_BufferLookup = state.GetBufferLookup<MapFeatureElement>();
		}
	}

	public const int MAX_BASE_RESOURCES = 10000;

	public const int FERTILITY_REGENERATION_RATE = 800;

	public const int FISH_REGENERATION_RATE = 800;

	public const int UPDATES_PER_DAY = 32;

	public const int EDITOR_ROWS_PER_TICK = 4;

	public static readonly int kTextureSize = 256;

	public ToolSystem m_ToolSystem;

	public SimulationSystem m_SimulationSystem;

	public GroundPollutionSystem m_GroundPollutionSystem;

	public NoisePollutionSystem m_NoisePollutionSystem;

	public TerrainSystem m_TerrainSystem;

	public WaterSystem m_WaterSystem;

	public GroundWaterSystem m_GroundWaterSystem;

	public Game.Areas.SearchSystem m_AreaSearchSystem;

	public Game.Objects.SearchSystem m_ObjectSearchSystem;

	public CitySystem m_CitySystem;

	private bool m_UpdateAll;

	private EntityQuery m_PollutionParameterQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1628842289_0;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase != SystemUpdatePhase.GameSimulation)
		{
			return 1;
		}
		return 8192;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_PollutionParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		CreateTextures(kTextureSize);
	}

	public override JobHandle SetDefaults(Context context)
	{
		JobHandle result = base.SetDefaults(context);
		if (context.purpose == Purpose.NewGame)
		{
			result.Complete();
			float3 float4 = default(float3);
			for (int i = 0; i < m_Map.Length; i++)
			{
				float num = (float)(i % kTextureSize) / (float)kTextureSize;
				float num2 = (float)(i / kTextureSize) / (float)kTextureSize;
				float3 @float = new float3(6.1f, 13.9f, 10.7f);
				float3 float2 = num * @float;
				float3 float3 = num2 * @float;
				float4.x = Mathf.PerlinNoise(float2.x, float3.x);
				float4.y = Mathf.PerlinNoise(float2.y, float3.y);
				float4.z = Mathf.PerlinNoise(float2.z, float3.z);
				float4 = (float4 - new float3(0.4f, 0.7f, 0.7f)) * new float3(5f, 10f, 10f);
				float4 = 10000f * math.saturate(float4);
				NaturalResourceCell value = new NaturalResourceCell
				{
					m_Fertility = 
					{
						m_Base = (ushort)float4.x
					},
					m_Ore = 
					{
						m_Base = (ushort)float4.y
					},
					m_Oil = 
					{
						m_Base = (ushort)float4.z
					}
				};
				m_Map[i] = value;
			}
		}
		return result;
	}

	public void PostDeserialize(Context context)
	{
		if (!context.format.Has(FormatTags.FishResource))
		{
			Update();
			m_UpdateAll = true;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> surfaceData = m_WaterSystem.GetSurfaceData(out deps);
		int2 @int = surfaceData.resolution.xz / kTextureSize;
		Assert.AreEqual(GroundPollutionSystem.kTextureSize, kTextureSize, "Ground pollution and Natural resources need to have the same resolution");
		Assert.AreEqual(NoisePollutionSystem.kTextureSize, kTextureSize, "Noise pollution and Natural resources need to have the same resolution");
		Assert.IsTrue(math.all(surfaceData.resolution.xz == @int * kTextureSize), "Water resolution much be dividable with natural resources resolution");
		NativeQueue<Entity> updateBuffer = new NativeQueue<Entity>(Allocator.TempJob);
		NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		RegenerateNaturalResourcesJob jobData = new RegenerateNaturalResourcesJob
		{
			m_FertilityRegenerationRate = 25,
			m_FishRegenerationRate = 25,
			m_PollutionRate = m_PollutionParameterQuery.GetSingleton<PollutionParameterData>().m_FertilityGroundMultiplier / 32f,
			m_WaterCellFactor = 300f / (float)(@int.x * @int.y),
			m_MapOffset = -0.5f * (float)CellMapSystem<NaturalResourceCell>.kMapSize,
			m_CellSize = (float)CellMapSystem<NaturalResourceCell>.kMapSize / (float)kTextureSize,
			m_RandomSeed = RandomSeed.Next(),
			m_WaterResolutionFactor = @int,
			m_GroundPollutionData = m_GroundPollutionSystem.GetData(readOnly: true, out dependencies),
			m_NoisePollutionData = m_NoisePollutionSystem.GetData(readOnly: true, out dependencies2),
			m_WaterSurfaceData = surfaceData,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
			m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_CellData = GetData(readOnly: false, out dependencies4),
			m_UpdateQueue = updateBuffer.AsParallelWriter()
		};
		CollectUpdatedAreasJob jobData2 = new CollectUpdatedAreasJob
		{
			m_UpdateBuffer = updateBuffer,
			m_UpdateList = nativeList
		};
		JobHandle dependencies5;
		JobHandle dependencies6;
		AreaResourceSystem.UpdateAreaResourcesJob jobData3 = new AreaResourceSystem.UpdateAreaResourcesJob
		{
			m_City = m_CitySystem.City,
			m_FullUpdate = false,
			m_UpdateList = nativeList.AsDeferredJobArray(),
			m_ObjectTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies5),
			m_NaturalResourceData = jobData.m_CellData,
			m_GroundWaterResourceData = m_GroundWaterSystem.GetData(readOnly: true, out dependencies6),
			m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WoodResources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_WoodResource_RW_BufferLookup, ref base.CheckedStateRef),
			m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = jobData.m_WaterSurfaceData,
			m_BuildableLandMaxSlope = __query_1628842289_0.GetSingleton<AreasConfigurationData>().m_BuildableLandMaxSlope
		};
		int num = kTextureSize;
		if (m_ToolSystem.actionMode.IsEditor() && !m_UpdateAll)
		{
			num = 4;
			jobData.m_ZOffset = (int)((m_SimulationSystem.frameIndex * num) & (kTextureSize - 1));
		}
		m_UpdateAll = false;
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, num, 1, JobUtils.CombineDependencies(dependencies, dependencies2, dependencies3, dependencies4, deps, base.Dependency));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		JobHandle jobHandle3 = jobData3.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle2, dependencies5, dependencies6));
		updateBuffer.Dispose(jobHandle2);
		nativeList.Dispose(jobHandle3);
		AddWriter(jobHandle);
		AddReader(jobHandle3);
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle3);
		m_WaterSystem.AddSurfaceReader(jobHandle3);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle3);
		m_GroundWaterSystem.AddReader(jobHandle3);
		base.Dependency = jobHandle3;
	}

	public float ResourceAmountToArea(float amount)
	{
		float2 @float = (float2)CellMapSystem<NaturalResourceCell>.kMapSize / (float2)TextureSize;
		return amount * @float.x * @float.y / 10000f;
	}

	public static NaturalResourceAmount GetFertilityAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Fertility);
	}

	public static NaturalResourceAmount GetOilAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Oil);
	}

	public static NaturalResourceAmount GetOreAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Ore);
	}

	public static NaturalResourceAmount GetFishAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Fish);
	}

	private static NaturalResourceAmount GetResource(float3 position, NativeArray<NaturalResourceCell> map, Func<NaturalResourceCell, NaturalResourceAmount> getter)
	{
		float num = (float)CellMapSystem<NaturalResourceCell>.kMapSize / (float)kTextureSize;
		int2 cell = CellMapSystem<NaturalResourceCell>.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystem<NaturalResourceCell>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<NaturalResourceCell>.GetCellCoords(position, CellMapSystem<NaturalResourceCell>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		cell = math.clamp(cell, 0, kTextureSize - 2);
		NaturalResourceAmount naturalResourceAmount = getter(map[cell.x + kTextureSize * cell.y]);
		NaturalResourceAmount naturalResourceAmount2 = getter(map[cell.x + 1 + kTextureSize * cell.y]);
		NaturalResourceAmount naturalResourceAmount3 = getter(map[cell.x + kTextureSize * (cell.y + 1)]);
		NaturalResourceAmount naturalResourceAmount4 = getter(map[cell.x + 1 + kTextureSize * (cell.y + 1)]);
		return new NaturalResourceAmount
		{
			m_Base = FilteringValue(naturalResourceAmount.m_Base, naturalResourceAmount2.m_Base, naturalResourceAmount3.m_Base, naturalResourceAmount4.m_Base),
			m_Used = FilteringValue(naturalResourceAmount.m_Used, naturalResourceAmount2.m_Used, naturalResourceAmount3.m_Used, naturalResourceAmount4.m_Used)
		};
		ushort FilteringValue(ushort p1, ushort p2, ushort p3, ushort p4)
		{
			return (ushort)math.round(math.lerp(math.lerp((int)p1, (int)p2, cellCoords.x - (float)cell.x), math.lerp((int)p3, (int)p4, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<AreasConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1628842289_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public NaturalResourceSystem()
	{
	}
}
