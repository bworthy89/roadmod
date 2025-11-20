using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

public abstract class EntityQueryModePrefab : ModePrefab
{
	public abstract EntityQueryDesc GetEntityQueryDesc();

	public virtual void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> requestedQuery, PrefabSystem prefabSystem)
	{
	}

	public abstract void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem);

	public abstract JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps);

	[Conditional("UNITY_EDITOR")]
	[Conditional("DEVELOPMENT_BUILD")]
	public void RecordChanges(EntityManager entityManager, ref NativeArray<Entity> entities)
	{
		foreach (Entity entity in entities)
		{
			_ = entity;
		}
	}

	[Conditional("UNITY_EDITOR")]
	[Conditional("DEVELOPMENT_BUILD")]
	protected virtual void RecordChanges(EntityManager entityManager, Entity entity)
	{
	}
}
