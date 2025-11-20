using System.Diagnostics;
using Unity.Entities;

namespace Game.Prefabs.Modes;

public abstract class LocalModePrefab : ModePrefab
{
	public abstract void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem);

	public abstract void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem);

	[Conditional("UNITY_EDITOR")]
	[Conditional("DEVELOPMENT_BUILD")]
	public virtual void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
	}
}
