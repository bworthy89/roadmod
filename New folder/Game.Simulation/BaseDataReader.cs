using Colossal.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Game.Simulation;

[BurstCompile]
internal abstract class BaseDataReader<T, V> where T : struct where V : struct
{
	public string Name = "BaseReader";

	private Bounds2 m_readbackArea;

	private bool m_areaIsSet;

	private bool m_fullReadback;

	protected int m_ReadbackDistribution = 8;

	protected int m_ReadbackIndex;

	protected int2 m_ReadbackPosition;

	protected int2 m_ReadbackSize;

	protected NativeArray<V> m_CPUTemp;

	protected NativeArray<T> m_CPU;

	protected JobHandle m_Writers;

	protected JobHandle m_Readers;

	protected int2 m_TexSize;

	protected int m_mapSize;

	protected AsyncGPUReadbackRequest m_AsyncReadback;

	protected bool m_PendingReadback;

	protected RenderTexture m_sourceTexture;

	protected GraphicsFormat m_graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;

	public JobHandle JobWriters => m_Writers;

	public bool FullReadback
	{
		get
		{
			return m_fullReadback;
		}
		set
		{
			m_fullReadback = value;
		}
	}

	public JobHandle JobReaders
	{
		get
		{
			return m_Readers;
		}
		set
		{
			m_Readers = value;
		}
	}

	public bool PendingReadback { get; set; }

	public NativeArray<T> WaterSurfaceCPUArray => m_CPU;

	public BaseDataReader(RenderTexture sourceTexture, int mapSize, GraphicsFormat graphicsFormat)
	{
		m_sourceTexture = sourceTexture;
		m_TexSize = new int2(sourceTexture.width, sourceTexture.height);
		m_mapSize = mapSize;
		m_graphicsFormat = graphicsFormat;
		m_CPU = new NativeArray<T>(m_TexSize.x * m_TexSize.y, Allocator.Persistent);
		GetReadbackBounds(out var _, out var size);
		m_CPUTemp = new NativeArray<V>(size.x * size.y, Allocator.Persistent);
		m_readbackArea.Reset();
		m_areaIsSet = false;
	}

	public void SetReadbackArea(Bounds2 area)
	{
		m_readbackArea = area;
		m_areaIsSet = true;
	}

	protected void ResetArea()
	{
		m_areaIsSet = false;
		m_fullReadback = false;
		m_readbackArea.Reset();
	}

	public void ExecuteReadBack()
	{
		if (!m_PendingReadback)
		{
			m_Writers.Complete();
			m_ReadbackIndex = (m_ReadbackIndex + 1) % (m_ReadbackDistribution * m_ReadbackDistribution);
			GetReadbackBounds(out var pos, out var size);
			m_AsyncReadback = AsyncGPUReadback.RequestIntoNativeArray(ref m_CPUTemp, m_sourceTexture, 0, pos.x, size.x, pos.y, size.y, 0, 1, m_graphicsFormat, CopyWaterValues);
			m_PendingReadback = true;
		}
	}

	public abstract void LoadData(NativeArray<V> buffer);

	protected abstract void CopyWaterValues(AsyncGPUReadbackRequest request);

	public WaterSurfaceData<T> GetSurfaceData(out JobHandle deps, bool hasDepths = true)
	{
		deps = m_Writers;
		int3 resolution = ((m_CPU.Length == m_TexSize.x * m_TexSize.y) ? new int3(m_TexSize.x, 1, m_TexSize.y) : new int3(2, 2, 2));
		float3 @float = new float3(m_mapSize, 1f, m_mapSize);
		float3 scale = new float3(resolution.x, resolution.y, resolution.z) / @float;
		float3 offset = -new float3((float)m_mapSize * -0.5f, 0f, (float)m_mapSize * -0.5f);
		return GetSurface(resolution, scale, offset, hasDepths);
	}

	protected abstract WaterSurfaceData<T> GetSurface(int3 resolution, float3 scale, float3 offset, bool hasDepths);

	public T GetSurface(int2 cell)
	{
		return m_CPU[cell.x + 1 + m_TexSize.x * cell.y];
	}

	protected void GetReadbackBounds(out int2 pos, out int2 size)
	{
		if (FullReadback)
		{
			pos = new int2(0, 0);
			size = m_TexSize;
			m_ReadbackPosition = pos;
			m_ReadbackSize = size;
		}
		else if (m_areaIsSet)
		{
			float2 @float = (float)WaterSystem.kMapSize / (float2)m_TexSize;
			float3 float2 = new float3(m_readbackArea.min.x, 0f, m_readbackArea.min.y);
			float3 float3 = new float3(m_readbackArea.max.x, 0f, m_readbackArea.max.y);
			int2 cell = WaterSystem.GetCell(float2 - new float3(@float.x / 2f, 0f, @float.y / 2f), WaterSystem.kMapSize, m_TexSize);
			int2 cell2 = WaterSystem.GetCell(float3 - new float3(@float.x / 2f, 0f, @float.y / 2f), WaterSystem.kMapSize, m_TexSize);
			pos = math.max(cell, int2.zero);
			size = math.min(cell2 - cell, m_TexSize - pos);
			m_ReadbackPosition = pos;
			m_ReadbackSize = size;
		}
		else
		{
			GetReadbackBounds(m_TexSize, m_ReadbackDistribution, m_ReadbackIndex, out pos, out size);
			m_ReadbackPosition = pos;
			m_ReadbackSize = size;
		}
	}

	protected static void GetReadbackBounds(int2 texSize, int readbackDistribution, int readbackIndex, out int2 pos, out int2 size)
	{
		size.x = texSize.x / readbackDistribution;
		size.y = texSize.y / readbackDistribution;
		pos.x = readbackIndex % readbackDistribution * size.x;
		pos.y = readbackIndex / readbackDistribution * size.y;
	}

	public void Dispose()
	{
		if (!m_AsyncReadback.done)
		{
			m_AsyncReadback.WaitForCompletion();
		}
		if (m_CPU.IsCreated)
		{
			m_CPU.Dispose();
		}
		if (m_CPUTemp.IsCreated)
		{
			m_CPUTemp.Dispose();
		}
		m_Readers.Complete();
	}

	public BaseDataReader()
	{
		m_mapSize = 0;
		m_TexSize = new int2(0, 0);
		m_CPU = new NativeArray<T>(4, Allocator.Persistent);
	}
}
