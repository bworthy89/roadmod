using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Game.Simulation;

[BurstCompile]
internal class SurfaceDataReader : BaseDataReader<SurfaceWater, float4>
{
	public delegate void CopyWaterValuesInternal_00005E8E_0024PostfixBurstDelegate(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<SurfaceWater> cpu, ref NativeArray<float4> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, int readbackDistribution, int readbackIndex);

	internal static class CopyWaterValuesInternal_00005E8E_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CopyWaterValuesInternal_00005E8E_0024PostfixBurstDelegate).TypeHandle);
			}
			P_0 = Pointer;
		}

		private static IntPtr GetFunctionPointer()
		{
			nint result = 0;
			GetFunctionPointerDiscard(ref result);
			return result;
		}

		public static void Constructor()
		{
			DeferredCompilation = BurstCompiler.CompileILPPMethod2((RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/);
		}

		public static void Initialize()
		{
		}

		static CopyWaterValuesInternal_00005E8E_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<SurfaceWater> cpu, ref NativeArray<float4> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, int readbackDistribution, int readbackIndex)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref AsyncGPUReadbackRequest, ref NativeArray<SurfaceWater>, ref NativeArray<float4>, ref JobHandle, ref int2, ref bool, int, int, void>)functionPointer)(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, readbackDistribution, readbackIndex);
					return;
				}
			}
			CopyWaterValuesInternal_0024BurstManaged(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, readbackDistribution, readbackIndex);
		}
	}

	public SurfaceDataReader()
	{
	}

	public SurfaceDataReader(RenderTexture sourceTexture, int mapSize, GraphicsFormat graphicsFormat)
		: base(sourceTexture, mapSize, graphicsFormat)
	{
	}

	protected override WaterSurfaceData<SurfaceWater> GetSurface(int3 resolution, float3 scale, float3 offset, bool hasDepths)
	{
		return new WaterSurfaceData<SurfaceWater>(m_CPU, resolution, scale, offset, hasDepths);
	}

	public override void LoadData(NativeArray<float4> buffer)
	{
		for (int i = 0; i < m_CPU.Length; i++)
		{
			float4 data = buffer[i];
			m_CPU[i] = new SurfaceWater(data);
		}
	}

	protected override void CopyWaterValues(AsyncGPUReadbackRequest request)
	{
		CopyWaterValuesInternal(ref m_AsyncReadback, ref m_CPU, ref m_CPUTemp, ref m_Readers, ref m_TexSize, ref m_PendingReadback, m_ReadbackDistribution, m_ReadbackIndex);
	}

	[BurstCompile]
	private static void CopyWaterValuesInternal(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<SurfaceWater> cpu, ref NativeArray<float4> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, int readbackDistribution, int readbackIndex)
	{
		CopyWaterValuesInternal_00005E8E_0024BurstDirectCall.Invoke(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, readbackDistribution, readbackIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	public static void CopyWaterValuesInternal_0024BurstManaged(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<SurfaceWater> cpu, ref NativeArray<float4> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, int readbackDistribution, int readbackIndex)
	{
		if (!asyncReadback.hasError && cpu.IsCreated)
		{
			readers.Complete();
			BaseDataReader<SurfaceWater, float4>.GetReadbackBounds(texSize, readbackDistribution, readbackIndex, out var pos, out var size);
			for (int i = 0; i < size.y; i++)
			{
				for (int j = 0; j < size.x; j++)
				{
					int index = pos.x + j + (pos.y + i) * texSize.x;
					float4 data = cpuTemp[j + i * size.x];
					SurfaceWater value = new SurfaceWater(data);
					cpu[index] = value;
				}
			}
			pendingReadback = false;
		}
		else
		{
			UnityEngine.Debug.LogWarning("Error in readback");
		}
	}
}
