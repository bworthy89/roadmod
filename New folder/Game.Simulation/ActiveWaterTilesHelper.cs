using Colossal.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

internal class ActiveWaterTilesHelper
{
	private NativeArray<int> m_ActiveCPUTemp;

	private int2 m_GridSize;

	private ComputeBuffer m_Active;

	private ComputeBuffer m_CurrentActiveTilesIndices;

	private NativeArray<int> m_ActiveCPU;

	private AsyncGPUReadbackHelper m_AsyncGPUReadback;

	private int m_FailCount;

	private bool m_PendingActiveReadback;

	private JobHandle m_ActiveReaders;

	private int m_textureSize;

	public int numThreadGroupsTotal { get; private set; }

	public int numThreadGroupsX { get; private set; }

	public int numThreadGroupsY { get; private set; }

	public int2 GridSize => m_GridSize;

	public int GridCellSize => m_textureSize / m_GridSize.x;

	public ActiveWaterTilesHelper(int2 gridSize, int textureSize, JobHandle activeReaders)
	{
		m_ActiveReaders = activeReaders;
		m_GridSize = gridSize;
		m_textureSize = textureSize;
		InitBuffers();
	}

	private void InitBuffers()
	{
		m_ActiveCPU = new NativeArray<int>(m_GridSize.x * m_GridSize.y, Allocator.Persistent);
		m_Active = new ComputeBuffer(m_GridSize.x * m_GridSize.y, UnsafeUtility.SizeOf<int>(), ComputeBufferType.Default);
		m_CurrentActiveTilesIndices = new ComputeBuffer(m_GridSize.x * m_GridSize.y, UnsafeUtility.SizeOf<int2>(), ComputeBufferType.Default);
	}

	public NativeArray<int> GetActive()
	{
		return m_ActiveCPU;
	}

	internal void Dispose()
	{
		if (m_Active != null)
		{
			m_Active.Release();
		}
		if (m_CurrentActiveTilesIndices != null)
		{
			m_CurrentActiveTilesIndices.Release();
		}
		if (m_ActiveCPU.IsCreated)
		{
			m_ActiveCPU.Dispose();
		}
	}

	internal void Reset(int2 gridCount)
	{
		if (m_AsyncGPUReadback.isPending)
		{
			m_AsyncGPUReadback.WaitForCompletion();
			UpdateGPUReadback();
		}
		if (m_ActiveCPU.IsCreated)
		{
			m_ActiveCPU.Dispose();
			m_Active.Dispose();
			m_CurrentActiveTilesIndices.Dispose();
		}
		m_GridSize = gridCount;
		InitBuffers();
	}

	internal void RequestReadback()
	{
		m_PendingActiveReadback = true;
		m_AsyncGPUReadback.Request(m_Active);
	}

	internal ComputeBuffer GetActiveBuffer()
	{
		return m_Active;
	}

	internal ComputeBuffer GetActiveTilesIndices()
	{
		return m_CurrentActiveTilesIndices;
	}

	public void UpdateGPUReadback()
	{
		if (!m_AsyncGPUReadback.isPending)
		{
			return;
		}
		if (!m_AsyncGPUReadback.hasError)
		{
			if (m_AsyncGPUReadback.done)
			{
				m_ActiveCPUTemp = m_AsyncGPUReadback.GetData<int>();
				m_ActiveReaders.Complete();
				m_ActiveCPU.CopyFrom(m_ActiveCPUTemp);
				m_PendingActiveReadback = false;
				m_FailCount = 0;
			}
			m_AsyncGPUReadback.IncrementFrame();
		}
		else if (++m_FailCount < 10)
		{
			m_AsyncGPUReadback.Request(m_Active);
		}
		else
		{
			m_AsyncGPUReadback.Request(m_Active);
		}
	}

	internal bool RequestReadbackIfNotPending()
	{
		if (!m_PendingActiveReadback)
		{
			m_AsyncGPUReadback.Request(m_Active);
			m_PendingActiveReadback = true;
			return true;
		}
		return false;
	}

	internal bool IsActive(uint x, uint y)
	{
		return m_ActiveCPU[(int)(x + m_GridSize.x * y)] > 0;
	}

	internal int UpdateActiveIndices(bool forceAllActive)
	{
		int result = 0;
		uint2[] array = new uint2[GridSize.y * GridSize.x];
		for (uint num = 0u; num < GridSize.x; num++)
		{
			for (uint num2 = 0u; num2 < GridSize.y; num2++)
			{
				if (forceAllActive || IsActive(num, num2))
				{
					uint2 @uint = new uint2(num, num2);
					array[result++] = @uint;
				}
			}
		}
		m_CurrentActiveTilesIndices.SetData(array);
		numThreadGroupsX = result;
		numThreadGroupsY = m_textureSize / m_GridSize.x / 8;
		numThreadGroupsTotal = numThreadGroupsX * numThreadGroupsY;
		return result;
	}
}
