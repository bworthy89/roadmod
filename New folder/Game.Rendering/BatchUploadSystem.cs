using Colossal.Rendering;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class BatchUploadSystem : GameSystemBase
{
	[BurstCompile]
	private struct BatchUploadJob : IJobParallelFor
	{
		public NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData>.ParallelUploadWriter m_NativeBatchInstances;

		public void Execute(int index)
		{
			m_NativeBatchInstances.UploadInstances(index);
		}
	}

	private BatchManagerSystem m_BatchManagerSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies);
		JobHandle dependencies2;
		NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: true, out dependencies2);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		dependencies2.Complete();
		managedBatches.StartUpload(nativeBatchInstances, nativeSubBatches);
		int activeGroupCount = nativeBatchInstances.GetActiveGroupCount();
		BatchUploadJob jobData = new BatchUploadJob
		{
			m_NativeBatchInstances = nativeBatchInstances.BeginParallelUpload()
		};
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, activeGroupCount, 1);
		JobHandle jobHandle2 = nativeBatchInstances.EndParallelUpload(jobData.m_NativeBatchInstances, jobHandle);
		m_BatchManagerSystem.AddNativeSubBatchesReader(jobHandle);
		m_BatchManagerSystem.AddNativeBatchInstancesWriter(jobHandle2);
	}

	[Preserve]
	public BatchUploadSystem()
	{
	}
}
