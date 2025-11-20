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
internal class HeightDataReader : BaseDataReader<half, half>
{
	public delegate void CopyWaterValuesInternal_00005E87_0024PostfixBurstDelegate(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<half> cpu, ref NativeArray<half> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, ref int2 readbackPos, ref int2 readbackSize);

	internal static class CopyWaterValuesInternal_00005E87_0024BurstDirectCall
	{
		private static IntPtr Pointer;

		private static IntPtr DeferredCompilation;

		[BurstDiscard]
		private unsafe static void GetFunctionPointerDiscard(ref IntPtr P_0)
		{
			if (Pointer == (IntPtr)0)
			{
				Pointer = (nint)BurstCompiler.GetILPPMethodFunctionPointer2(DeferredCompilation, (RuntimeMethodHandle)/*OpCode not supported: LdMemberToken*/, typeof(CopyWaterValuesInternal_00005E87_0024PostfixBurstDelegate).TypeHandle);
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

		static CopyWaterValuesInternal_00005E87_0024BurstDirectCall()
		{
			Constructor();
		}

		public unsafe static void Invoke(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<half> cpu, ref NativeArray<half> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, ref int2 readbackPos, ref int2 readbackSize)
		{
			if (BurstCompiler.IsEnabled)
			{
				IntPtr functionPointer = GetFunctionPointer();
				if (functionPointer != (IntPtr)0)
				{
					((delegate* unmanaged[Cdecl]<ref AsyncGPUReadbackRequest, ref NativeArray<half>, ref NativeArray<half>, ref JobHandle, ref int2, ref bool, ref int2, ref int2, void>)functionPointer)(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, ref readbackPos, ref readbackSize);
					return;
				}
			}
			CopyWaterValuesInternal_0024BurstManaged(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, ref readbackPos, ref readbackSize);
		}
	}

	public HeightDataReader(RenderTexture sourceTexture, int mapSize, GraphicsFormat graphicsFormat)
		: base(sourceTexture, mapSize, graphicsFormat)
	{
		Name = "HeightDataReader";
		m_CPUTemp.Dispose();
		m_CPUTemp = new NativeArray<half>(m_TexSize.x * m_TexSize.y, Allocator.Persistent);
	}

	public override void LoadData(NativeArray<half> buffer)
	{
		for (int i = 0; i < m_CPU.Length; i++)
		{
			half value = buffer[i];
			m_CPU[i] = value;
		}
	}

	protected override void CopyWaterValues(AsyncGPUReadbackRequest request)
	{
		CopyWaterValuesInternal(ref m_AsyncReadback, ref m_CPU, ref m_CPUTemp, ref m_Readers, ref m_TexSize, ref m_PendingReadback, ref m_ReadbackPosition, ref m_ReadbackSize);
		ResetArea();
	}

	[BurstCompile]
	private static void CopyWaterValuesInternal(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<half> cpu, ref NativeArray<half> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, ref int2 readbackPos, ref int2 readbackSize)
	{
		CopyWaterValuesInternal_00005E87_0024BurstDirectCall.Invoke(ref asyncReadback, ref cpu, ref cpuTemp, ref readers, ref texSize, ref pendingReadback, ref readbackPos, ref readbackSize);
	}

	protected override WaterSurfaceData<half> GetSurface(int3 resolution, float3 scale, float3 offset, bool hasDepths)
	{
		return new WaterSurfaceData<half>(m_CPU, resolution, scale, offset, hasDepths);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[BurstCompile]
	public static void CopyWaterValuesInternal_0024BurstManaged(ref AsyncGPUReadbackRequest asyncReadback, ref NativeArray<half> cpu, ref NativeArray<half> cpuTemp, ref JobHandle readers, ref int2 texSize, ref bool pendingReadback, ref int2 readbackPos, ref int2 readbackSize)
	{
		if (!asyncReadback.hasError && cpu.IsCreated)
		{
			readers.Complete();
			for (int i = 0; i < readbackSize.y; i++)
			{
				for (int j = 0; j < readbackSize.x; j++)
				{
					int index = readbackPos.x + j + (readbackPos.y + i) * texSize.x;
					half value = cpuTemp[j + i * readbackSize.x];
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
