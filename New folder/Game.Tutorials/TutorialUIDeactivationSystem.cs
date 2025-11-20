using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialUIDeactivationSystem : TutorialDeactivationSystemBase, ITutorialUIDeactivationSystem
{
	private PrefabSystem m_PrefabSystem;

	private readonly HashSet<string> m_Deactivate = new HashSet<string>();

	private EntityQuery m_PendingTutorialQuery;

	private EntityQuery m_ActiveTutorialQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<UIActivationData>(), ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<UIActivationData>(), ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
	}

	public void DeactivateTag(string tag)
	{
		m_Deactivate.Add(tag);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_Deactivate.Count > 0)
		{
			if (!m_PendingTutorialQuery.IsEmptyIgnoreFilter)
			{
				CheckDeactivate(m_PendingTutorialQuery);
			}
			if (!m_ActiveTutorialQuery.IsEmptyIgnoreFilter && base.phaseCanDeactivate)
			{
				CheckDeactivate(m_ActiveTutorialQuery);
			}
		}
		m_Deactivate.Clear();
	}

	private void CheckDeactivate(EntityQuery query)
	{
		NativeArray<PrefabData> nativeArray = query.ToComponentDataArray<PrefabData>(Allocator.TempJob);
		NativeArray<Entity> nativeArray2 = query.ToEntityArray(Allocator.TempJob);
		EntityCommandBuffer entityCommandBuffer = m_BarrierSystem.CreateCommandBuffer();
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			string[] array = m_PrefabSystem.GetPrefab<TutorialPrefab>(nativeArray[i]).GetComponent<TutorialUIActivation>().m_UITagProvider.uiTag?.Split('|');
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (m_Deactivate.Contains(array[j].Trim()))
				{
					entityCommandBuffer.RemoveComponent<TutorialActivated>(nativeArray2[i]);
				}
			}
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
	}

	[Preserve]
	public TutorialUIDeactivationSystem()
	{
	}
}
