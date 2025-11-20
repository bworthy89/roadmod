using Game.Common;
using Game.Input;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialInputTriggerSystem : TutorialTriggerSystemBase
{
	private EntityArchetype m_UnlockEventArchetype;

	private PrefabSystem m_PrefabSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<InputTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.Exclude<TriggerCompleted>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		NativeArray<InputTriggerData> nativeArray = m_ActiveTriggerQuery.ToComponentDataArray<InputTriggerData>(Allocator.TempJob);
		NativeArray<Entity> nativeArray2 = m_ActiveTriggerQuery.ToEntityArray(Allocator.TempJob);
		EntityCommandBuffer commandBuffer = m_BarrierSystem.CreateCommandBuffer();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			TutorialInputTriggerPrefab prefab = m_PrefabSystem.GetPrefab<TutorialInputTriggerPrefab>(nativeArray2[i]);
			if (Performed(prefab))
			{
				commandBuffer.AddComponent<TriggerCompleted>(nativeArray2[i]);
				TutorialSystem.ManualUnlock(nativeArray2[i], m_UnlockEventArchetype, base.EntityManager, commandBuffer);
			}
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
	}

	private bool Performed(TutorialInputTriggerPrefab prefab)
	{
		for (int i = 0; i < prefab.m_Actions.Length; i++)
		{
			if (InputManager.instance.TryFindAction(prefab.m_Actions[i].m_Map, prefab.m_Actions[i].m_Action, out var action) && action.WasPerformedThisFrame())
			{
				return true;
			}
		}
		return false;
	}

	[Preserve]
	public TutorialInputTriggerSystem()
	{
	}
}
