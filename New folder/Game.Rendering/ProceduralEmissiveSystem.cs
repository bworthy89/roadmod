using System.Collections.Generic;
using Colossal.Collections;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class ProceduralEmissiveSystem : GameSystemBase, IPreDeserialize
{
	public struct AllocationInfo
	{
		public uint m_AllocationCount;
	}

	public struct AllocationRemove
	{
		public NativeHeapBlock m_Allocation;

		public int m_RemoveTime;
	}

	[BurstCompile]
	private struct RemoveAllocationsJob : IJob
	{
		public NativeHeapAllocator m_HeapAllocator;

		public NativeReference<AllocationInfo> m_AllocationInfo;

		public NativeQueue<AllocationRemove> m_AllocationRemoves;

		public int m_CurrentTime;

		public void Execute()
		{
			ref AllocationInfo reference = ref m_AllocationInfo.ValueAsRef();
			while (!m_AllocationRemoves.IsEmpty())
			{
				AllocationRemove allocationRemove = m_AllocationRemoves.Peek();
				int num = m_CurrentTime - allocationRemove.m_RemoveTime;
				num += math.select(0, 65536, num < 0);
				if (num >= 255)
				{
					m_AllocationRemoves.Dequeue();
					m_HeapAllocator.Release(allocationRemove.m_Allocation);
					reference.m_AllocationCount--;
					continue;
				}
				break;
			}
		}
	}

	public const uint EMISSIVE_MEMORY_DEFAULT = 2097152u;

	public const uint EMISSIVE_MEMORY_INCREMENT = 1048576u;

	public const uint UPLOADER_CHUNK_SIZE = 131072u;

	private RenderingSystem m_RenderingSystem;

	private NativeHeapAllocator m_HeapAllocator;

	private SparseUploader m_SparseUploader;

	private ThreadedSparseUploader m_ThreadedSparseUploader;

	private NativeReference<AllocationInfo> m_AllocationInfo;

	private NativeQueue<AllocationRemove> m_AllocationRemoves;

	private bool m_IsAllocating;

	private bool m_IsUploading;

	private GraphicsBuffer m_ComputeBuffer;

	private JobHandle m_HeapDeps;

	private JobHandle m_UploadDeps;

	private int m_HeapAllocatorByteSize;

	private int m_CurrentTime;

	[Preserve]
	protected unsafe override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_HeapAllocator = new NativeHeapAllocator(2097152u / (uint)sizeof(float4), 1u, Allocator.Persistent);
		m_SparseUploader = new SparseUploader("Procedural emissive uploader", null, 131072);
		m_AllocationInfo = new NativeReference<AllocationInfo>(Allocator.Persistent);
		m_AllocationRemoves = new NativeQueue<AllocationRemove>(Allocator.Persistent);
		AllocateIdentityEntry();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		CompleteUpload();
		m_HeapDeps.Complete();
		if (m_HeapAllocator.IsCreated)
		{
			m_HeapAllocator.Dispose();
			m_SparseUploader.Dispose();
			m_AllocationInfo.Dispose();
			m_AllocationRemoves.Dispose();
		}
		if (m_ComputeBuffer != null)
		{
			m_ComputeBuffer.Release();
		}
		base.OnDestroy();
	}

	[Preserve]
	protected unsafe override void OnUpdate()
	{
		CompleteUpload();
		m_HeapDeps.Complete();
		if (m_IsAllocating)
		{
			m_IsAllocating = false;
			m_HeapAllocatorByteSize = (int)m_HeapAllocator.Size * sizeof(float4);
			int heapAllocatorByteSize = m_HeapAllocatorByteSize;
			int num = ((m_ComputeBuffer != null) ? (m_ComputeBuffer.count * m_ComputeBuffer.stride) : 0);
			if (heapAllocatorByteSize != num)
			{
				GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, heapAllocatorByteSize / 4, 4);
				graphicsBuffer.name = "Procedural emissive buffer";
				Shader.SetGlobalBuffer("_LightInfo", graphicsBuffer);
				m_SparseUploader.ReplaceBuffer(graphicsBuffer, copyFromPrevious: true);
				if (m_ComputeBuffer != null)
				{
					m_ComputeBuffer.Release();
				}
				else
				{
					graphicsBuffer.SetData(new List<float4> { float4.zero }, 0, 0, 1);
				}
				m_ComputeBuffer = graphicsBuffer;
			}
		}
		if (!m_AllocationRemoves.IsEmpty())
		{
			m_CurrentTime = (m_CurrentTime + m_RenderingSystem.lodTimerDelta) & 0xFFFF;
			RemoveAllocationsJob jobData = new RemoveAllocationsJob
			{
				m_HeapAllocator = m_HeapAllocator,
				m_AllocationInfo = m_AllocationInfo,
				m_AllocationRemoves = m_AllocationRemoves,
				m_CurrentTime = m_CurrentTime
			};
			m_HeapDeps = IJobExtensions.Schedule(jobData);
		}
	}

	public ThreadedSparseUploader BeginUpload(int opCount, uint dataSize, uint maxOpSize)
	{
		m_ThreadedSparseUploader = m_SparseUploader.Begin((int)dataSize, (int)maxOpSize, opCount);
		m_IsUploading = true;
		return m_ThreadedSparseUploader;
	}

	public void AddUploadWriter(JobHandle handle)
	{
		m_UploadDeps = handle;
	}

	public void CompleteUpload()
	{
		if (m_IsUploading)
		{
			m_UploadDeps.Complete();
			m_IsUploading = false;
			m_SparseUploader.EndAndCommit(m_ThreadedSparseUploader);
		}
	}

	public void PreDeserialize(Context context)
	{
		m_HeapDeps.Complete();
		m_HeapAllocator.Clear();
		m_AllocationRemoves.Clear();
		AllocateIdentityEntry();
	}

	public NativeHeapAllocator GetHeapAllocator(out NativeReference<AllocationInfo> allocationInfo, out NativeQueue<AllocationRemove> allocationRemoves, out int currentTime, out JobHandle dependencies)
	{
		dependencies = m_HeapDeps;
		allocationInfo = m_AllocationInfo;
		allocationRemoves = m_AllocationRemoves;
		currentTime = m_CurrentTime;
		m_IsAllocating = true;
		return m_HeapAllocator;
	}

	public void AddHeapWriter(JobHandle handle)
	{
		m_HeapDeps = handle;
	}

	public unsafe void GetMemoryStats(out uint allocatedSize, out uint bufferSize, out uint currentUpload, out uint uploadSize, out int allocationCount)
	{
		m_HeapDeps.Complete();
		allocatedSize = m_HeapAllocator.UsedSpace * (uint)sizeof(float4);
		bufferSize = m_HeapAllocator.Size * (uint)sizeof(float4);
		allocationCount = (int)m_AllocationInfo.Value.m_AllocationCount;
		SparseUploaderStats sparseUploaderStats = m_SparseUploader.ComputeStats();
		currentUpload = (uint)sparseUploaderStats.BytesGPUMemoryUploadedCurr;
		uploadSize = (uint)sparseUploaderStats.BytesGPUMemoryUsed;
	}

	private void AllocateIdentityEntry()
	{
		m_IsAllocating = true;
		m_HeapAllocator.Allocate(1u);
		m_AllocationInfo.Value = new AllocationInfo
		{
			m_AllocationCount = 0u
		};
	}

	public static void GetGpuLights(Emissive emissive, in DynamicBuffer<ProceduralLight> proceduralLights, in DynamicBuffer<LightState> lights, NativeList<float4> gpuLights)
	{
		gpuLights[0] = default(float4);
		for (int i = 0; i < proceduralLights.Length; i++)
		{
			ProceduralLight proceduralLight = proceduralLights[i];
			LightState lightState = lights[emissive.m_LightOffset + i];
			float4 value = math.lerp(proceduralLight.m_Color, proceduralLight.m_Color2, lightState.m_Color);
			value.w *= lightState.m_Intensity;
			gpuLights[i + 1] = value;
		}
	}

	[Preserve]
	public ProceduralEmissiveSystem()
	{
	}
}
