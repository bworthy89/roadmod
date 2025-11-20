using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Common;

public class CleanUpSystem : GameSystemBase
{
	private NativeList<Entity> m_DeletedEntities;

	private NativeList<Entity> m_UpdatedEntities;

	private JobHandle m_DeletedDeps;

	private JobHandle m_UpdatedDeps;

	private ComponentTypeSet m_UpdateTypes;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateTypes = new ComponentTypeSet(new ComponentType[6]
		{
			ComponentType.ReadWrite<Created>(),
			ComponentType.ReadWrite<Updated>(),
			ComponentType.ReadWrite<Applied>(),
			ComponentType.ReadWrite<EffectsUpdated>(),
			ComponentType.ReadWrite<BatchesUpdated>(),
			ComponentType.ReadWrite<PathfindUpdated>()
		});
	}

	public void AddDeleted(NativeList<Entity> deletedEntities, JobHandle deletedDeps)
	{
		m_DeletedEntities = deletedEntities;
		m_DeletedDeps = deletedDeps;
	}

	public void AddUpdated(NativeList<Entity> updatedEntities, JobHandle updatedDeps)
	{
		m_UpdatedEntities = updatedEntities;
		m_UpdatedDeps = updatedDeps;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_DeletedDeps.Complete();
		m_UpdatedDeps.Complete();
		base.EntityManager.DestroyEntity(m_DeletedEntities.AsArray());
		base.EntityManager.RemoveComponent(m_UpdatedEntities.AsArray(), in m_UpdateTypes);
		m_DeletedEntities.Dispose();
		m_UpdatedEntities.Dispose();
	}

	[Preserve]
	public CleanUpSystem()
	{
	}
}
