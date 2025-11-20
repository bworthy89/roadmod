using System;
using Colossal.Serialization.Entities;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class WindSystem : CellMapSystem<Wind>, IJobSerializable
{
	[BurstCompile]
	private struct WindCopyJob : IJobFor
	{
		public NativeArray<Wind> m_WindMap;

		[ReadOnly]
		public NativeArray<WindSimulationSystem.WindCell> m_Source;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public void Execute(int index)
		{
			float3 cellCenter = WindSimulationSystem.GetCellCenter(index);
			cellCenter.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cellCenter) + 25f;
			float num = math.max(0f, (float)WindSimulationSystem.kResolution.z * (cellCenter.y - TerrainUtils.ToWorldSpace(ref m_TerrainHeightData, 0f)) / TerrainUtils.ToWorldSpace(ref m_TerrainHeightData, 65535f) - 0.5f);
			int3 cell = new int3(index % kTextureSize, index / kTextureSize, Math.Min(Mathf.FloorToInt(num), WindSimulationSystem.kResolution.z - 1));
			int3 cell2 = new int3(cell.x, cell.y, Math.Min(cell.z + 1, WindSimulationSystem.kResolution.z - 1));
			float2 xy = WindSimulationSystem.GetCenterVelocity(cell, m_Source).xy;
			float2 xy2 = WindSimulationSystem.GetCenterVelocity(cell2, m_Source).xy;
			float2 wind = math.lerp(xy, xy2, math.frac(num));
			m_WindMap[index] = new Wind
			{
				m_Wind = wind
			};
		}
	}

	public static readonly int kTextureSize = 64;

	public static readonly int kUpdateInterval = 512;

	public WindSimulationSystem m_WindSimulationSystem;

	public WindTextureSystem m_WindTextureSystem;

	public TerrainSystem m_TerrainSystem;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase != SystemUpdatePhase.GameSimulation)
		{
			return 1;
		}
		return kUpdateInterval;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<Wind>.GetCellCenter(index, kTextureSize);
	}

	public static Wind GetWind(float3 position, NativeArray<Wind> windMap)
	{
		int2 cell = CellMapSystem<Wind>.GetCell(position, CellMapSystem<Wind>.kMapSize, kTextureSize);
		cell = math.clamp(cell, 0, kTextureSize - 1);
		float2 cellCoords = CellMapSystem<Wind>.GetCellCoords(position, CellMapSystem<Wind>.kMapSize, kTextureSize);
		int num = math.min(kTextureSize - 1, cell.x + 1);
		int num2 = math.min(kTextureSize - 1, cell.y + 1);
		return new Wind
		{
			m_Wind = math.lerp(math.lerp(windMap[cell.x + kTextureSize * cell.y].m_Wind, windMap[num + kTextureSize * cell.y].m_Wind, cellCoords.x - (float)cell.x), math.lerp(windMap[cell.x + kTextureSize * num2].m_Wind, windMap[num + kTextureSize * num2].m_Wind, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y)
		};
	}

	public override JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps)
	{
		m_WindTextureSystem.RequireUpdate();
		if (readerData.GetReader<TReader>().context.version > Version.cellMapLengths)
		{
			return base.Deserialize<TReader>(readerData, inputDeps);
		}
		m_Map.Dispose();
		m_Map = new NativeArray<Wind>(65536, Allocator.Persistent);
		inputDeps = base.Deserialize<TReader>(readerData, inputDeps);
		inputDeps.Complete();
		m_Map.Dispose();
		m_Map = new NativeArray<Wind>(kTextureSize * kTextureSize, Allocator.Persistent);
		return inputDeps;
	}

	public override JobHandle SetDefaults(Context context)
	{
		m_WindTextureSystem.RequireUpdate();
		for (int i = 0; i < m_Map.Length; i++)
		{
			m_Map[i] = new Wind
			{
				m_Wind = m_WindSimulationSystem.constantWind
			};
		}
		return default(JobHandle);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
		m_WindTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		CreateTextures(kTextureSize);
		for (int i = 0; i < m_Map.Length; i++)
		{
			m_Map[i] = new Wind
			{
				m_Wind = m_WindSimulationSystem.constantWind
			};
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
		if (heightData.isCreated)
		{
			JobHandle deps;
			WindCopyJob jobData = new WindCopyJob
			{
				m_WindMap = m_Map,
				m_Source = m_WindSimulationSystem.GetCells(out deps),
				m_TerrainHeightData = heightData
			};
			base.Dependency = jobData.Schedule(m_Map.Length, JobHandle.CombineDependencies(deps, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, base.Dependency)));
			AddWriter(base.Dependency);
			m_TerrainSystem.AddCPUHeightReader(base.Dependency);
			m_WindSimulationSystem.AddReader(base.Dependency);
			m_WindTextureSystem.RequireUpdate();
		}
	}

	[Preserve]
	public WindSystem()
	{
	}
}
