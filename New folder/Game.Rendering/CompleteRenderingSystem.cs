using Colossal.Rendering;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class CompleteRenderingSystem : GameSystemBase
{
	private BatchManagerSystem m_BatchManagerSystem;

	private ManagedBatchSystem m_ManagedBatchSystem;

	private ProceduralSkeletonSystem m_ProceduralSkeletonSystem;

	private ProceduralEmissiveSystem m_ProceduralEmissiveSystem;

	private WindTextureSystem m_WindTextureSystem;

	private BatchMeshSystem m_BatchMeshSystem;

	private UpdateSystem m_UpdateSystem;

	private OverlayInfomodeSystem m_OverlayInfomodeSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_ManagedBatchSystem = base.World.GetOrCreateSystemManaged<ManagedBatchSystem>();
		m_ProceduralSkeletonSystem = base.World.GetOrCreateSystemManaged<ProceduralSkeletonSystem>();
		m_ProceduralEmissiveSystem = base.World.GetOrCreateSystemManaged<ProceduralEmissiveSystem>();
		m_WindTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
		m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_OverlayInfomodeSystem = base.World.GetOrCreateSystemManaged<OverlayInfomodeSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		managedBatches.EndUpload(nativeBatchInstances);
		m_ProceduralSkeletonSystem.CompleteUpload();
		m_ProceduralEmissiveSystem.CompleteUpload();
		m_WindTextureSystem.CompleteUpdate();
		m_ManagedBatchSystem.CompleteVTRequests();
		m_BatchMeshSystem.CompleteMeshes();
		m_OverlayInfomodeSystem.ApplyOverlay();
		m_UpdateSystem.Update(SystemUpdatePhase.CompleteRendering);
	}

	[Preserve]
	public CompleteRenderingSystem()
	{
	}
}
