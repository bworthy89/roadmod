using Colossal.Rendering;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class BatchRendererSystem : GameSystemBase
{
	[BurstCompile]
	private struct ClearUpdatedMetaDatasJob : IJob
	{
		public NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> m_NativeBatchGroups;

		public void Execute()
		{
			m_NativeBatchGroups.ClearObsoleteManagedBatches();
			m_NativeBatchGroups.ClearUpdatedMetaDatas();
		}
	}

	private BatchManagerSystem m_BatchManagerSystem;

	private BatchMeshSystem m_BatchMeshSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: true, out dependencies);
		JobHandle dependencies2;
		NativeSubBatches<CullingData, GroupData, BatchData, InstanceData> nativeSubBatches = m_BatchManagerSystem.GetNativeSubBatches(readOnly: false, out dependencies2);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		dependencies2.Complete();
		ObsoleteManagedBatchEnumerator obsoleteManagedBatches = nativeBatchGroups.GetObsoleteManagedBatches();
		int managedBatchIndex;
		while (obsoleteManagedBatches.GetNextObsoleteBatch(out managedBatchIndex))
		{
			CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(managedBatchIndex);
			m_BatchMeshSystem.RemoveBatch(customBatch, managedBatchIndex);
			managedBatches.RemoveBatch(managedBatchIndex);
			customBatch.Dispose();
		}
		UpdatedMetaDataEnumerator updatedMetaDatas = nativeBatchGroups.GetUpdatedMetaDatas();
		int groupIndex;
		while (updatedMetaDatas.GetNextUpdatedGroup(out groupIndex))
		{
			nativeSubBatches.RecreateRenderers(groupIndex);
		}
		ObsoleteBatchRendererEnumerator obsoleteBatchRenderers = nativeSubBatches.GetObsoleteBatchRenderers();
		BatchID rendererIndex;
		while (obsoleteBatchRenderers.GetNextObsoleteRenderer(out rendererIndex))
		{
			managedBatches.RemoveRenderer(rendererIndex);
		}
		nativeSubBatches.ClearObsoleteBatchRenderers();
		UpdatedBatchRendererEnumerator updatedBatchRenderers = nativeSubBatches.GetUpdatedBatchRenderers();
		int groupIndex2;
		while (updatedBatchRenderers.GetNextUpdatedGroup(out groupIndex2))
		{
			NativeSubBatchAccessor<BatchData> subBatchAccessor = nativeSubBatches.GetSubBatchAccessor(groupIndex2);
			for (int i = 0; i < subBatchAccessor.Length; i++)
			{
				NativeBatchPropertyAccessor batchPropertyAccessor = nativeBatchGroups.GetBatchPropertyAccessor(groupIndex2, i);
				if (subBatchAccessor.GetBatchID(i) == BatchID.Null)
				{
					BatchID batchID = managedBatches.AddBatchRenderer(batchPropertyAccessor);
					nativeSubBatches.SetBatchID(groupIndex2, i, batchID);
				}
			}
		}
		nativeSubBatches.ClearUpdatedBatchRenderers();
		JobHandle jobHandle = new ClearUpdatedMetaDatasJob
		{
			m_NativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies)
		}.Schedule(dependencies);
		m_BatchManagerSystem.AddNativeBatchGroupsWriter(jobHandle);
	}

	[Preserve]
	public BatchRendererSystem()
	{
	}
}
