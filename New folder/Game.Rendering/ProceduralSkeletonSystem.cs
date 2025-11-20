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

public class ProceduralSkeletonSystem : GameSystemBase, IPreDeserialize
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

	public const uint SKELETON_MEMORY_DEFAULT = 4194304u;

	public const uint SKELETON_MEMORY_INCREMENT = 1048576u;

	public const uint UPLOADER_CHUNK_SIZE = 524288u;

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

	private bool m_AreMotionVectorsEnabled;

	private bool m_ForceHistoryUpdate;

	public bool isMotionBlurEnabled => m_AreMotionVectorsEnabled;

	public bool forceHistoryUpdate => m_ForceHistoryUpdate;

	[Preserve]
	protected unsafe override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_HeapAllocator = new NativeHeapAllocator(4194304u / (uint)sizeof(float4x4), 1u, Allocator.Persistent);
		m_SparseUploader = new SparseUploader("Procedural skeleton uploader", null, 524288);
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
		bool motionVectors = m_RenderingSystem.motionVectors;
		int num = ((!motionVectors) ? 1 : 2);
		m_ForceHistoryUpdate = m_AreMotionVectorsEnabled != motionVectors;
		CompleteUpload();
		m_HeapDeps.Complete();
		if (m_IsAllocating || m_ForceHistoryUpdate)
		{
			m_IsAllocating = false;
			m_AreMotionVectorsEnabled = motionVectors;
			m_HeapAllocatorByteSize = (int)m_HeapAllocator.Size * sizeof(float4x4);
			int num2 = m_HeapAllocatorByteSize * num;
			int num3 = ((m_ComputeBuffer != null) ? (m_ComputeBuffer.count * m_ComputeBuffer.stride) : 0);
			if (num2 != num3)
			{
				GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, num2 / 4, 4);
				graphicsBuffer.name = "Procedural bone buffer";
				Shader.SetGlobalBuffer("_BoneTransforms", graphicsBuffer);
				if (motionVectors && !m_ForceHistoryUpdate)
				{
					m_SparseUploader.ReplaceBuffer(graphicsBuffer, copyFromPrevious: true, num3 / 2);
				}
				else
				{
					m_SparseUploader.ReplaceBuffer(graphicsBuffer, copyFromPrevious: true);
				}
				if (m_ComputeBuffer == null)
				{
					graphicsBuffer.SetData(new List<float4x4> { float4x4.identity }, 0, 0, 1);
				}
				if (motionVectors && (m_ComputeBuffer == null || m_ForceHistoryUpdate))
				{
					graphicsBuffer.SetData(new List<float4x4> { float4x4.identity }, 0, (int)m_HeapAllocator.Size, 1);
				}
				if (m_ComputeBuffer != null)
				{
					m_ComputeBuffer.Release();
				}
				m_ComputeBuffer = graphicsBuffer;
			}
			Shader.SetGlobalInt("_BonePreviousTransformsByteOffset", m_HeapAllocatorByteSize);
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

	public ThreadedSparseUploader BeginUpload(int opCount, uint dataSize, uint maxOpSize, out int historyByteOffset)
	{
		m_ThreadedSparseUploader = m_SparseUploader.Begin((int)dataSize, (int)maxOpSize, opCount);
		m_IsUploading = true;
		historyByteOffset = m_HeapAllocatorByteSize;
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
		int num = ((!m_RenderingSystem.motionVectors) ? 1 : 2);
		allocatedSize = (uint)((int)m_HeapAllocator.UsedSpace * sizeof(float4x4) * num);
		bufferSize = (uint)((int)m_HeapAllocator.Size * sizeof(float4x4) * num);
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

	public static void GetSkinMatrices(Skeleton skeleton, in DynamicBuffer<ProceduralBone> proceduralBones, in DynamicBuffer<Bone> bones, NativeList<float4x4> tempMatrices)
	{
		for (int i = 0; i < proceduralBones.Length; i++)
		{
			ProceduralBone proceduralBone = proceduralBones[i];
			Bone bone = bones[skeleton.m_BoneOffset + i];
			float4x4 float4x = float4x4.TRS(bone.m_Position, bone.m_Rotation, bone.m_Scale);
			if (proceduralBone.m_ParentIndex >= 0)
			{
				float4x = math.mul(tempMatrices[proceduralBone.m_ParentIndex], float4x);
			}
			tempMatrices[i] = float4x;
			tempMatrices[proceduralBones.Length + proceduralBone.m_BindIndex] = math.mul(float4x, proceduralBone.m_BindPose);
		}
	}

	[Preserve]
	public ProceduralSkeletonSystem()
	{
	}
}
