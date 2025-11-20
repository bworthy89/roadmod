using System.IO;
using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class WindSimulationSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct WindCell : ISerializable
	{
		public float m_Pressure;

		public float3 m_Velocities;

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			float pressure = m_Pressure;
			writer.Write(pressure);
			float3 velocities = m_Velocities;
			writer.Write(velocities);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref float pressure = ref m_Pressure;
			reader.Read(out pressure);
			ref float3 velocities = ref m_Velocities;
			reader.Read(out velocities);
		}
	}

	[BurstCompile]
	private struct UpdateWindVelocityJob : IJobFor
	{
		public NativeArray<WindCell> m_Cells;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public float2 m_TerrainRange;

		public void Execute(int index)
		{
			int3 @int = new int3(index % kResolution.x, index / kResolution.x % kResolution.y, index / (kResolution.x * kResolution.y));
			bool3 @bool = new bool3(@int.x >= kResolution.x - 1, @int.y >= kResolution.y - 1, @int.z >= kResolution.z - 1);
			if (!@bool.x && !@bool.y && !@bool.z)
			{
				int3 position = new int3(@int.x, @int.y + 1, @int.z);
				int3 position2 = new int3(@int.x + 1, @int.y, @int.z);
				float3 cellCenter = GetCellCenter(index);
				cellCenter.y = math.lerp(m_TerrainRange.x, m_TerrainRange.y, ((float)@int.z + 0.5f) / (float)kResolution.z);
				float num = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, cellCenter);
				float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, cellCenter);
				float num3 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, cellCenter);
				float num4 = 65535f / (m_TerrainHeightData.scale.y * (float)kResolution.z);
				float num5 = math.saturate((0.5f * (num4 + num + num2) - cellCenter.y) / num4);
				float num6 = math.saturate((0.5f * (num4 + num + num3) - cellCenter.y) / num4);
				WindCell value = m_Cells[index];
				WindCell cell = GetCell(new int3(@int.x, @int.y, @int.z + 1), m_Cells);
				WindCell cell2 = GetCell(position, m_Cells);
				WindCell cell3 = GetCell(position2, m_Cells);
				value.m_Velocities.x *= math.lerp(kAirSlowdown, kTerrainSlowdown, num6);
				value.m_Velocities.y *= math.lerp(kAirSlowdown, kTerrainSlowdown, num5);
				value.m_Velocities.z *= kVerticalSlowdown;
				value.m_Velocities.x += kChangeFactor * (1f - num6) * (value.m_Pressure - cell3.m_Pressure);
				value.m_Velocities.y += kChangeFactor * (1f - num5) * (value.m_Pressure - cell2.m_Pressure);
				value.m_Velocities.z += kChangeFactor * (value.m_Pressure - cell.m_Pressure);
				m_Cells[index] = value;
			}
		}
	}

	[BurstCompile]
	private struct UpdatePressureJob : IJobFor
	{
		public NativeArray<WindCell> m_Cells;

		public float2 m_Wind;

		public void Execute(int index)
		{
			int3 @int = new int3(index % kResolution.x, index / kResolution.x % kResolution.y, index / (kResolution.x * kResolution.y));
			bool3 @bool = new bool3(@int.x == 0, @int.y == 0, @int.z == 0);
			bool3 bool2 = new bool3(@int.x >= kResolution.x - 1, @int.y >= kResolution.y - 1, @int.z >= kResolution.z - 1);
			if (!bool2.x && !bool2.y && !bool2.z)
			{
				WindCell value = m_Cells[index];
				value.m_Pressure -= value.m_Velocities.x + value.m_Velocities.y + value.m_Velocities.z;
				if (!@bool.x)
				{
					WindCell cell = GetCell(new int3(@int.x - 1, @int.y, @int.z), m_Cells);
					value.m_Pressure += cell.m_Velocities.x;
				}
				if (!@bool.y)
				{
					WindCell cell2 = GetCell(new int3(@int.x, @int.y - 1, @int.z), m_Cells);
					value.m_Pressure += cell2.m_Velocities.y;
				}
				if (!@bool.z)
				{
					WindCell cell3 = GetCell(new int3(@int.x, @int.y, @int.z - 1), m_Cells);
					value.m_Pressure += cell3.m_Velocities.z;
				}
				m_Cells[index] = value;
			}
			if (@bool.x || @bool.y || bool2.x || bool2.y)
			{
				WindCell value2 = m_Cells[index];
				float num = math.dot(math.normalize(new float2(@int.x - kResolution.x / 2, @int.y - kResolution.y / 2)), math.normalize(m_Wind));
				float num2 = math.pow((1f + (float)@int.z) / (1f + (float)kResolution.z), 1f / 7f);
				float num3 = 0.1f * (2f - num);
				float num4 = (40f - 20f * (1f + num)) * math.length(m_Wind) * num2;
				value2.m_Pressure = ((num4 > value2.m_Pressure) ? math.min(num4, value2.m_Pressure + num3) : math.max(num4, value2.m_Pressure - num3));
				m_Cells[index] = value2;
			}
		}
	}

	public static readonly int kUpdateInterval = 512;

	public static readonly int3 kResolution = new int3(WindSystem.kTextureSize, WindSystem.kTextureSize, 16);

	public static readonly float kChangeFactor = 0.02f;

	public static readonly float kTerrainSlowdown = 0.99f;

	public static readonly float kAirSlowdown = 0.995f;

	public static readonly float kVerticalSlowdown = 0.9f;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ClimateSystem m_ClimateSystem;

	private bool m_Odd;

	private JobHandle m_Deps;

	private NativeArray<WindCell> m_Cells;

	public float2 constantWind { get; set; }

	private float m_ConstantPressure { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase != SystemUpdatePhase.GameSimulation)
		{
			return 1;
		}
		return kUpdateInterval;
	}

	public unsafe byte[] CreateByteArray<T>(NativeArray<T> src) where T : struct
	{
		int num = UnsafeUtility.SizeOf<T>() * src.Length;
		byte* unsafeReadOnlyPtr = (byte*)src.GetUnsafeReadOnlyPtr();
		byte[] array = new byte[num];
		fixed (byte* ptr = array)
		{
			UnsafeUtility.MemCpy(ptr, unsafeReadOnlyPtr, num);
		}
		return array;
	}

	public void DebugSave()
	{
		m_Deps.Complete();
		using System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(File.OpenWrite(Application.streamingAssetsPath + "/wind_temp.dat"));
		binaryWriter.Write(kResolution.x);
		binaryWriter.Write(kResolution.y);
		binaryWriter.Write(kResolution.z);
		binaryWriter.Write(CreateByteArray(m_Cells));
	}

	public unsafe void DebugLoad()
	{
		m_Deps.Complete();
		using System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(File.OpenRead(Application.streamingAssetsPath + "/wind_temp.dat"));
		int num = binaryReader.ReadInt32();
		int num2 = binaryReader.ReadInt32();
		int num3 = binaryReader.ReadInt32();
		int num4 = num * num2 * num3 * UnsafeUtility.SizeOf<WindCell>();
		byte[] array = new byte[num4];
		binaryReader.Read(array, 0, num * num2 * num3 * sizeof(WindCell));
		byte* unsafePtr = (byte*)m_Cells.GetUnsafePtr();
		for (int i = 0; i < num4; i++)
		{
			unsafePtr[i] = array[i];
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int length = m_Cells.Length;
		writer.Write(length);
		NativeArray<WindCell> cells = m_Cells;
		writer.Write(cells);
		float2 value = constantWind;
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (!(reader.context.version > Version.stormWater))
		{
			return;
		}
		if (reader.context.version > Version.cellMapLengths)
		{
			reader.Read(out int value);
			if (m_Cells.Length == value)
			{
				NativeArray<WindCell> cells = m_Cells;
				reader.Read(cells);
			}
			if (reader.context.version > Version.windDirection)
			{
				reader.Read(out float2 value2);
				constantWind = value2;
			}
			else
			{
				constantWind = new float2(0.275f, 0.275f);
			}
		}
		else
		{
			NativeArray<WindCell> cells2 = m_Cells;
			reader.Read(cells2);
		}
	}

	public void SetDefaults(Context context)
	{
		m_Deps.Complete();
		for (int i = 0; i < m_Cells.Length; i++)
		{
			m_Cells[i] = new WindCell
			{
				m_Pressure = m_ConstantPressure,
				m_Velocities = new float3(constantWind, 0f)
			};
		}
	}

	public void SetWind(float2 direction, float pressure)
	{
		m_Deps.Complete();
		constantWind = direction;
		m_ConstantPressure = pressure;
		SetDefaults(default(Context));
	}

	public static float3 GetCenterVelocity(int3 cell, NativeArray<WindCell> cells)
	{
		float3 velocities = GetCell(cell, cells).m_Velocities;
		float3 @float = ((cell.x > 0) ? GetCell(cell + new int3(-1, 0, 0), cells).m_Velocities : velocities);
		float3 float2 = ((cell.y > 0) ? GetCell(cell + new int3(0, -1, 0), cells).m_Velocities : velocities);
		float3 float3 = ((cell.z > 0) ? GetCell(cell + new int3(0, 0, -1), cells).m_Velocities : velocities);
		return 0.5f * new float3(velocities.x + @float.x, velocities.y + float2.y, velocities.z + float3.z);
	}

	public static float3 GetCellCenter(int index)
	{
		int3 @int = new int3(index % kResolution.x, index / kResolution.x % kResolution.y, index / (kResolution.x * kResolution.y));
		float3 result = CellMapSystem<Wind>.kMapSize * new float3(((float)@int.x + 0.5f) / (float)kResolution.x, 0f, ((float)@int.y + 0.5f) / (float)kResolution.y) - CellMapSystem<Wind>.kMapSize / 2;
		result.y = 100f + 1024f * ((float)@int.z + 0.5f) / (float)kResolution.z;
		return result;
	}

	public NativeArray<WindCell> GetCells(out JobHandle deps)
	{
		deps = m_Deps;
		return m_Cells;
	}

	public void AddReader(JobHandle reader)
	{
		m_Deps = JobHandle.CombineDependencies(m_Deps, reader);
	}

	[Preserve]
	protected override void OnCreate()
	{
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		constantWind = new float2(0.275f, 0.275f);
		m_ConstantPressure = 40f;
		m_Cells = new NativeArray<WindCell>(kResolution.x * kResolution.y * kResolution.z, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Cells.Dispose();
	}

	private WindCell GetCell(int3 position)
	{
		return m_Cells[position.x + position.y * kResolution.x + position.z * kResolution.x * kResolution.y];
	}

	public static WindCell GetCell(int3 position, NativeArray<WindCell> cells)
	{
		int num = position.x + position.y * kResolution.x + position.z * kResolution.x * kResolution.y;
		if (num < 0 || num >= cells.Length)
		{
			return default(WindCell);
		}
		return cells[num];
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_TerrainSystem.heightmap != null)
		{
			m_Odd = !m_Odd;
			if (!m_Odd)
			{
				TerrainHeightData data = m_TerrainSystem.GetHeightData();
				float x = TerrainUtils.ToWorldSpace(ref data, 0f);
				float y = TerrainUtils.ToWorldSpace(ref data, 65535f);
				float2 terrainRange = new float2(x, y);
				JobHandle deps;
				UpdateWindVelocityJob jobData = new UpdateWindVelocityJob
				{
					m_Cells = m_Cells,
					m_TerrainHeightData = data,
					m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
					m_TerrainRange = terrainRange
				};
				m_Deps = jobData.Schedule(kResolution.x * kResolution.y * kResolution.z, JobHandle.CombineDependencies(m_Deps, deps, base.Dependency));
				m_WaterSystem.AddSurfaceReader(m_Deps);
				m_TerrainSystem.AddCPUHeightReader(m_Deps);
			}
			else
			{
				UpdatePressureJob jobData2 = new UpdatePressureJob
				{
					m_Cells = m_Cells,
					m_Wind = constantWind / 10f
				};
				m_Deps = jobData2.Schedule(kResolution.x * kResolution.y * kResolution.z, JobHandle.CombineDependencies(m_Deps, base.Dependency));
			}
			base.Dependency = m_Deps;
		}
	}

	[Preserve]
	public WindSimulationSystem()
	{
	}
}
