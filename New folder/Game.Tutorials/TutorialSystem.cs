using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Audio;
using Game.City;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.PSI;
using Game.SceneFlow;
using Game.Serialization;
using Game.Settings;
using Game.Simulation;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialSystem : GameSystemBase, ITutorialSystem, IPreDeserialize
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<TutorialCompleted> __Game_Tutorials_TutorialCompleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TutorialAlternative> __Game_Tutorials_TutorialAlternative_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_TutorialCompleted_RO_ComponentLookup = state.GetComponentLookup<TutorialCompleted>(isReadOnly: true);
			__Game_Tutorials_TutorialAlternative_RO_BufferLookup = state.GetBufferLookup<TutorialAlternative>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private AudioManager m_AudioManager;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private GameScreenUISystem m_GameScreenUISystem;

	private static readonly float kBalloonCompletionDelay = 0f;

	private static readonly float kCompletionDelay = 1.5f;

	private static readonly float kActivationDelay = 3f;

	private static readonly string kWelcomeIntroKey = "WelcomeIntro";

	protected static readonly string kListIntroKey = "ListIntro";

	private static readonly string kListOutroKey = "ListOutro";

	private EntityQuery m_TutorialConfigurationQuery;

	protected EntityQuery m_TutorialQuery;

	private EntityQuery m_TutorialListQuery;

	private EntityQuery m_TutorialPhaseQuery;

	private EntityQuery m_ActiveTutorialListQuery;

	protected EntityQuery m_ActiveTutorialQuery;

	private EntityQuery m_ActiveTutorialPhaseQuery;

	protected EntityQuery m_PendingTutorialListQuery;

	protected EntityQuery m_PendingTutorialQuery;

	protected EntityQuery m_PendingPriorityTutorialQuery;

	protected EntityQuery m_LockedTutorialQuery;

	private EntityQuery m_LockedTutorialPhaseQuery;

	private EntityQuery m_LockedTutorialTriggerQuery;

	private EntityQuery m_LockedTutorialListQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_ForceAdvisorQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private float m_AccumulatedDelay;

	protected TutorialMode m_Mode;

	protected Setting m_Setting = SharedSettings.instance.userState;

	private TypeHandle __TypeHandle;

	protected virtual Dictionary<string, bool> ShownTutorials => SharedSettings.instance.userState.shownTutorials;

	public TutorialMode mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			if (value != m_Mode)
			{
				if (m_Mode == TutorialMode.Intro)
				{
					UpdateSettings(kWelcomeIntroKey, passed: true);
				}
				else if (m_Mode == TutorialMode.ListIntro)
				{
					UpdateSettings(kListIntroKey, passed: true);
				}
				else if (m_Mode == TutorialMode.ListOutro)
				{
					UpdateSettings(kListOutroKey, passed: true);
				}
			}
			m_Mode = value;
		}
	}

	public Entity activeTutorial
	{
		get
		{
			if (!m_ActiveTutorialQuery.IsEmptyIgnoreFilter)
			{
				return m_ActiveTutorialQuery.GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public Entity activeTutorialPhase
	{
		get
		{
			if (!m_ActiveTutorialPhaseQuery.IsEmptyIgnoreFilter)
			{
				return m_ActiveTutorialPhaseQuery.GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public virtual bool tutorialEnabled
	{
		get
		{
			return SharedSettings.instance.gameplay.showTutorials;
		}
		set
		{
			SharedSettings.instance.gameplay.showTutorials = value;
			if (!value)
			{
				mode = TutorialMode.Default;
			}
		}
	}

	public Entity activeTutorialList
	{
		get
		{
			if (!m_ActiveTutorialListQuery.IsEmptyIgnoreFilter)
			{
				return m_ActiveTutorialListQuery.GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public Entity tutorialPending => FindNextTutorial();

	public Entity nextListTutorial
	{
		get
		{
			Entity entity = activeTutorialList;
			if (entity != Entity.Null)
			{
				DynamicBuffer<TutorialRef> buffer = base.EntityManager.GetBuffer<TutorialRef>(entity, isReadOnly: true);
				ComponentLookup<TutorialCompleted> componentLookup = GetComponentLookup<TutorialCompleted>(isReadOnly: true);
				BufferLookup<TutorialAlternative> bufferLookup = GetBufferLookup<TutorialAlternative>(isReadOnly: true);
				foreach (TutorialRef item in buffer)
				{
					if (!IsCompleted(item.m_Tutorial, bufferLookup, componentLookup))
					{
						if (!base.EntityManager.HasComponent<TutorialActive>(item.m_Tutorial))
						{
							return item.m_Tutorial;
						}
						break;
					}
				}
			}
			return Entity.Null;
		}
	}

	public bool showListReminder => activeTutorialList == m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>().m_TutorialsIntroList;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		base.Enabled = false;
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_GameScreenUISystem = base.World.GetOrCreateSystemManaged<GameScreenUISystem>();
		m_TutorialConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialsConfigurationData>());
		m_TutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.Exclude<EditorTutorial>());
		m_TutorialPhaseQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialPhaseData>(), ComponentType.Exclude<EditorTutorial>());
		m_TutorialListQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialListData>());
		m_ActiveTutorialListQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialListData>(), ComponentType.ReadOnly<TutorialRef>(), ComponentType.ReadOnly<TutorialActive>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActive>());
		m_ActiveTutorialPhaseQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialPhaseData>(), ComponentType.ReadOnly<TutorialPhaseActive>());
		m_PendingTutorialListQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialListData>(), ComponentType.ReadOnly<TutorialRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>());
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialPhaseRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<EditorTutorial>());
		m_PendingPriorityTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialPhaseRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<ReplaceActiveData>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<EditorTutorial>());
		m_LockedTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<Locked>(), ComponentType.Exclude<EditorTutorial>());
		m_LockedTutorialPhaseQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialPhaseData>(), ComponentType.ReadOnly<Locked>());
		m_LockedTutorialTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialTriggerData>(), ComponentType.ReadOnly<Locked>());
		m_LockedTutorialListQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialListData>(), ComponentType.ReadOnly<Locked>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_ForceAdvisorQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<AdvisorActivationData>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Unlock>());
	}

	public virtual void OnResetTutorials()
	{
		if (GameManager.instance.gameMode.IsGameOrEditor())
		{
			ResetState();
			ClearComponents();
			m_Mode = TutorialMode.Intro;
		}
	}

	private bool IsCompleted(Entity tutorial, BufferLookup<TutorialAlternative> alternativeData, ComponentLookup<TutorialCompleted> completionData)
	{
		if (completionData.HasComponent(tutorial))
		{
			return true;
		}
		if (alternativeData.TryGetBuffer(tutorial, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				if (completionData.HasComponent(bufferData[i].m_Alternative))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode gameMode)
	{
		base.OnGamePreload(purpose, gameMode);
		ResetState();
		base.Enabled = gameMode.IsGame();
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode gameMode)
	{
		if (gameMode == GameMode.Game && tutorialEnabled && !ShownTutorials.ContainsKey(kWelcomeIntroKey))
		{
			m_Mode = TutorialMode.Intro;
		}
		ReadSettings();
		if (m_CityConfigurationSystem.unlockMapTiles && (!tutorialEnabled || (ShownTutorials.TryGetValue(kListIntroKey, out var value) && value)))
		{
			TutorialsConfigurationData singleton = m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>();
			if (base.EntityManager.HasEnabledComponent<Locked>(singleton.m_MapTilesFeature))
			{
				Entity entity = base.EntityManager.CreateEntity(m_UnlockEventArchetype);
				base.EntityManager.SetComponentData(entity, new Unlock(singleton.m_MapTilesFeature));
			}
			m_MapTilePurchaseSystem.UnlockMapTiles();
		}
		ForceAdvisorVisibility();
	}

	private void ResetState()
	{
		m_Mode = TutorialMode.Default;
		m_AccumulatedDelay = 0f;
		SetTutorial(Entity.Null);
		SetTutorialList(Entity.Null);
	}

	private void ReadSettings()
	{
		NativeArray<Entity> nativeArray = m_TutorialListQuery.ToEntityArray(Allocator.TempJob);
		foreach (Entity item in nativeArray)
		{
			if (m_PrefabSystem.TryGetPrefab<PrefabBase>(item, out var prefab) && ShownTutorials.TryGetValue(prefab.name, out var value))
			{
				if (value)
				{
					CleanupTutorialList(item, passed: true, updateSettings: false);
				}
				else
				{
					SetTutorialShown(item, updateSettings: false);
				}
			}
		}
		nativeArray.Dispose();
		NativeArray<Entity> nativeArray2 = m_TutorialQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray2.Length; i++)
		{
			Entity entity = nativeArray2[i];
			if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefab2) || !ShownTutorials.TryGetValue(prefab2.name, out var value2))
			{
				continue;
			}
			if (value2)
			{
				CleanupTutorial(entity, passed: true, updateSettings: false);
				continue;
			}
			SetTutorialShown(entity, updateSettings: false);
			NativeArray<TutorialPhaseRef> nativeArray3 = base.EntityManager.GetBuffer<TutorialPhaseRef>(entity, isReadOnly: true).ToNativeArray(Allocator.TempJob);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity phase = nativeArray3[j].m_Phase;
				if (m_PrefabSystem.TryGetPrefab<PrefabBase>(phase, out var prefab3) && ShownTutorials.ContainsKey(prefab3.name))
				{
					SetTutorialShown(phase, updateSettings: false);
				}
			}
			nativeArray3.Dispose();
		}
		nativeArray2.Dispose();
	}

	private void SetTutorialShown(Entity entity, bool updateSettings = true)
	{
		if (base.EntityManager.HasComponent<TutorialPhaseData>(entity))
		{
			base.EntityManager.AddComponent<TutorialPhaseShown>(entity);
			if (updateSettings)
			{
				UpdateSettings(entity);
			}
		}
		else
		{
			base.EntityManager.AddComponent<TutorialShown>(entity);
			if (updateSettings)
			{
				UpdateSettings(entity);
			}
		}
		if (base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component) && component.m_Group != Entity.Null)
		{
			SetTutorialShown(component.m_Group, updateSettings: false);
		}
	}

	private void ForceAdvisorVisibility()
	{
		NativeArray<Entity> nativeArray = m_ForceAdvisorQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ForceAdvisor(nativeArray[i]);
		}
		nativeArray.Dispose();
	}

	private void ForceAdvisor(Entity entity)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<TutorialPhaseRef> buffer))
		{
			NativeArray<TutorialPhaseRef> nativeArray = buffer.ToNativeArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity phase = nativeArray[i].m_Phase;
				ForceAdvisor(phase);
			}
			nativeArray.Dispose();
		}
		if (base.EntityManager.TryGetComponent<UIObjectData>(entity, out var component) && component.m_Group != Entity.Null)
		{
			ForceAdvisor(component.m_Group);
		}
		if (!base.EntityManager.HasComponent<ForceAdvisor>(entity))
		{
			base.EntityManager.AddComponent<ForceAdvisor>(entity);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (mode == TutorialMode.Default)
		{
			if (tutorialEnabled)
			{
				UpdateActiveTutorialList();
			}
			if (tutorialEnabled || base.EntityManager.HasComponent<AdvisorActivation>(activeTutorial))
			{
				UpdateActiveTutorial();
				return;
			}
			ClearTutorialLocks();
			SetTutorial(Entity.Null);
			SetTutorialList(Entity.Null);
		}
	}

	public void ForceTutorial(Entity tutorial, Entity phase, bool advisorActivation)
	{
		if (tutorial != Entity.Null)
		{
			base.EntityManager.AddComponent<ForceActivation>(tutorial);
			if (advisorActivation)
			{
				base.EntityManager.AddComponent<AdvisorActivation>(tutorial);
			}
		}
		SetTutorial(tutorial, phase, passed: false);
	}

	private void SetTutorial(Entity tutorial, bool passed = false)
	{
		SetTutorial(tutorial, Entity.Null, passed);
	}

	public void SetTutorial(Entity tutorial, Entity phase, bool passed)
	{
		Entity entity = activeTutorial;
		Entity entity2 = activeTutorialPhase;
		if (tutorial != entity)
		{
			if (entity != Entity.Null)
			{
				CleanupTutorial(entity, passed);
			}
			if (tutorial != Entity.Null)
			{
				SetTutorialShown(tutorial);
				base.EntityManager.AddComponent<TutorialActive>(tutorial);
				Entity firstTutorialPhase = GetFirstTutorialPhase(tutorial);
				SetTutorialPhase((phase == Entity.Null) ? firstTutorialPhase : phase, passed);
			}
			else
			{
				SetTutorialPhase(Entity.Null, passed);
			}
			m_AccumulatedDelay = 0f;
		}
		else if (phase != entity2)
		{
			SetTutorialPhase(phase, passed);
			m_AccumulatedDelay = 0f;
		}
	}

	public void SetTutorial(Entity tutorial, Entity phase)
	{
		SetTutorial(tutorial, phase, passed: false);
	}

	private void SetTutorialPhase(Entity tutorialPhase, bool passed)
	{
		Entity entity = activeTutorialPhase;
		if (!(tutorialPhase != entity))
		{
			return;
		}
		if (entity != Entity.Null)
		{
			CleanupTutorialPhase(entity, passed);
		}
		if (tutorialPhase != Entity.Null)
		{
			base.EntityManager.AddComponent<TutorialPhaseActive>(tutorialPhase);
			ManualUnlock(tutorialPhase, m_UnlockEventArchetype, base.EntityManager);
			SetTutorialShown(tutorialPhase);
			if (!m_SoundQuery.IsEmptyIgnoreFilter && !m_GameScreenUISystem.isMenuActive && !GameManager.instance.isGameLoading)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TutorialStartedSound);
			}
			if (base.EntityManager.TryGetComponent<TutorialTrigger>(tutorialPhase, out var component))
			{
				base.EntityManager.AddComponent<TriggerActive>(component.m_Trigger);
			}
		}
	}

	public void CompleteCurrentTutorialPhase()
	{
		Entity entity = activeTutorialPhase;
		if (entity != Entity.Null && base.EntityManager.TryGetComponent<TutorialTrigger>(entity, out var component) && base.EntityManager.TryGetComponent<TutorialNextPhase>(component.m_Trigger, out var component2))
		{
			CompleteCurrentTutorialPhase(component2.m_NextPhase);
		}
		else
		{
			CompleteCurrentTutorialPhase(Entity.Null);
		}
	}

	public void CompleteCurrentTutorialPhase(Entity nextPhase)
	{
		Entity entity = activeTutorial;
		if (entity != Entity.Null)
		{
			Entity entity2 = GetNextPhase(entity, activeTutorialPhase, nextPhase);
			if (base.EntityManager.HasComponent<ForceTutorialCompletion>(activeTutorialPhase))
			{
				entity2 = Entity.Null;
			}
			if (entity2 != Entity.Null)
			{
				SetTutorialPhase(entity2, passed: true);
			}
			else
			{
				CompleteTutorial(entity);
			}
		}
	}

	private void UpdateActiveTutorial()
	{
		if (activeTutorial != Entity.Null)
		{
			if (CheckCurrentPhaseCompleted(out var nextPhase))
			{
				CompleteCurrentTutorialPhase(nextPhase);
			}
			if (ShouldReplaceActiveTutorial())
			{
				ActivateNextTutorial();
			}
		}
		if (activeTutorial == Entity.Null)
		{
			ActivateNextTutorial(delay: true);
		}
	}

	private bool ShouldReplaceActiveTutorial()
	{
		Entity entity = activeTutorial;
		if (entity != Entity.Null)
		{
			if (base.EntityManager.HasComponent<ForceActivation>(entity))
			{
				return false;
			}
			if (!base.EntityManager.HasComponent<TutorialActivated>(entity))
			{
				return true;
			}
			if (!base.EntityManager.HasComponent<ReplaceActiveData>(entity))
			{
				return !m_PendingPriorityTutorialQuery.IsEmptyIgnoreFilter;
			}
			return false;
		}
		return false;
	}

	private void CleanupTutorial(Entity tutorial, bool passed = false, bool updateSettings = true)
	{
		if (!(tutorial != Entity.Null))
		{
			return;
		}
		base.EntityManager.RemoveComponent<AdvisorActivation>(tutorial);
		base.EntityManager.RemoveComponent<TutorialActive>(tutorial);
		base.EntityManager.RemoveComponent<ForceActivation>(tutorial);
		NativeArray<TutorialPhaseRef> nativeArray = base.EntityManager.GetBuffer<TutorialPhaseRef>(tutorial, isReadOnly: true).ToNativeArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity phase = nativeArray[i].m_Phase;
			CleanupTutorialPhase(phase, passed, updateSettings);
		}
		nativeArray.Dispose();
		if (!passed)
		{
			return;
		}
		SetTutorialShown(tutorial);
		base.EntityManager.AddComponent<TutorialCompleted>(tutorial);
		if (updateSettings)
		{
			UpdateSettings(tutorial, passed: true);
			if (base.EntityManager.HasComponent<TutorialFireTelemetry>(tutorial))
			{
				Telemetry.TutorialEvent(tutorial);
			}
		}
		ManualUnlock(tutorial, m_UnlockEventArchetype, base.EntityManager);
	}

	private void UpdateSettings(Entity tutorial, bool passed = false)
	{
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(tutorial, out var prefab))
		{
			UpdateSettings(prefab.name, passed);
		}
	}

	private void UpdateSettings(string name, bool passed)
	{
		Setting setting = m_Setting;
		if (ShownTutorials.TryAdd(name, passed))
		{
			setting.ApplyAndSave();
		}
		else if (passed)
		{
			ShownTutorials[name] = true;
			setting.ApplyAndSave();
		}
	}

	private void CleanupTutorialPhase(Entity tutorialPhase, bool passed = false, bool updateSettings = true)
	{
		if (!(tutorialPhase != Entity.Null))
		{
			return;
		}
		base.EntityManager.RemoveComponent<TutorialPhaseActive>(tutorialPhase);
		if (passed)
		{
			SetTutorialShown(tutorialPhase);
			base.EntityManager.AddComponent<TutorialPhaseCompleted>(tutorialPhase);
			ManualUnlock(tutorialPhase, m_UnlockEventArchetype, base.EntityManager);
			if (updateSettings)
			{
				UpdateSettings(tutorialPhase, passed: true);
			}
		}
		if (base.EntityManager.TryGetComponent<TutorialTrigger>(tutorialPhase, out var component))
		{
			base.EntityManager.RemoveComponent<TriggerActive>(component.m_Trigger);
			base.EntityManager.RemoveComponent<TriggerCompleted>(component.m_Trigger);
			base.EntityManager.RemoveComponent<TriggerPreCompleted>(component.m_Trigger);
			base.EntityManager.RemoveComponent<TutorialNextPhase>(component.m_Trigger);
			if (passed)
			{
				ManualUnlock(component.m_Trigger, m_UnlockEventArchetype, base.EntityManager);
			}
		}
	}

	private void ActivateNextTutorial(bool delay = false)
	{
		if (delay)
		{
			m_AccumulatedDelay += UnityEngine.Time.deltaTime;
			if (m_AccumulatedDelay < kActivationDelay)
			{
				return;
			}
			m_AccumulatedDelay = 0f;
		}
		Entity tutorial = FindNextTutorial();
		SetTutorial(tutorial);
	}

	private Entity FindNextTutorial()
	{
		int num = -1;
		int num2 = -1;
		Entity entity = FindNextTutorial(m_PendingPriorityTutorialQuery);
		if (entity == Entity.Null)
		{
			entity = FindNextTutorial(m_PendingTutorialQuery);
		}
		if (entity != Entity.Null)
		{
			num = base.EntityManager.GetComponentData<TutorialData>(entity).m_Priority;
		}
		Entity entity2 = activeTutorialList;
		if (entity2 != Entity.Null)
		{
			num2 = base.EntityManager.GetComponentData<TutorialListData>(entity2).m_Priority;
		}
		if (entity2 != Entity.Null && (num2 < num || entity == Entity.Null))
		{
			Entity entity3 = nextListTutorial;
			if (base.EntityManager.HasComponent<TutorialActivated>(entity3))
			{
				return entity3;
			}
		}
		else if (entity != Entity.Null && (num <= num2 || entity2 == Entity.Null))
		{
			return entity;
		}
		return Entity.Null;
	}

	private Entity FindNextTutorial(EntityQuery query)
	{
		if (!query.IsEmptyIgnoreFilter)
		{
			NativeArray<TutorialData> nativeArray = query.ToComponentDataArray<TutorialData>(Allocator.TempJob);
			NativeArray<Entity> nativeArray2 = query.ToEntityArray(Allocator.TempJob);
			int index = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_Priority < nativeArray[index].m_Priority)
				{
					index = i;
				}
			}
			Entity result = nativeArray2[index];
			nativeArray.Dispose();
			nativeArray2.Dispose();
			return result;
		}
		return Entity.Null;
	}

	private bool CheckCurrentPhaseCompleted(out Entity nextPhase)
	{
		nextPhase = Entity.Null;
		if (base.EntityManager.TryGetComponent<TutorialPhaseData>(activeTutorialPhase, out var component) && base.EntityManager.TryGetComponent<TutorialTrigger>(activeTutorialPhase, out var component2))
		{
			if (!base.EntityManager.HasComponent<TriggerCompleted>(component2.m_Trigger))
			{
				return false;
			}
			if (m_AccumulatedDelay < GetCompletionDelay(component))
			{
				m_AccumulatedDelay += UnityEngine.Time.deltaTime;
				return false;
			}
			m_AccumulatedDelay = 0f;
			if (base.EntityManager.TryGetComponent<TutorialNextPhase>(activeTutorialPhase, out var component3))
			{
				nextPhase = component3.m_NextPhase;
			}
			if (base.EntityManager.TryGetComponent<TutorialNextPhase>(component2.m_Trigger, out component3))
			{
				nextPhase = component3.m_NextPhase;
			}
			return true;
		}
		return false;
	}

	private static float GetCompletionDelay(TutorialPhaseData phase)
	{
		if (phase.m_OverrideCompletionDelay >= 0f)
		{
			return phase.m_OverrideCompletionDelay;
		}
		if (phase.m_Type == TutorialPhaseType.Balloon)
		{
			return kBalloonCompletionDelay;
		}
		return kCompletionDelay;
	}

	private Entity GetNextPhase(Entity tutorial, Entity currentPhase, Entity nextPhase)
	{
		NativeArray<TutorialPhaseRef> nativeArray = base.EntityManager.GetBuffer<TutorialPhaseRef>(tutorial, isReadOnly: true).ToNativeArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity phase = nativeArray[i].m_Phase;
			if (nextPhase == Entity.Null)
			{
				if (!(phase == currentPhase))
				{
					continue;
				}
				for (int j = i; j < nativeArray.Length - 1; j++)
				{
					nextPhase = nativeArray[j + 1].m_Phase;
					if (IsValidControlScheme(nextPhase, m_PrefabSystem))
					{
						nativeArray.Dispose();
						return nextPhase;
					}
				}
			}
			else if (phase == nextPhase)
			{
				nativeArray.Dispose();
				if (!IsValidControlScheme(nextPhase, m_PrefabSystem))
				{
					return Entity.Null;
				}
				return nextPhase;
			}
		}
		nativeArray.Dispose();
		return Entity.Null;
	}

	private Entity GetFirstTutorialPhase(Entity tutorial)
	{
		DynamicBuffer<TutorialPhaseRef> buffer = base.EntityManager.GetBuffer<TutorialPhaseRef>(tutorial, isReadOnly: true);
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity phase = buffer[i].m_Phase;
			if (IsValidControlScheme(phase, m_PrefabSystem))
			{
				return phase;
			}
		}
		return Entity.Null;
	}

	public static bool IsValidControlScheme(Entity phase, PrefabSystem prefabSystem)
	{
		TutorialPhasePrefab prefab = prefabSystem.GetPrefab<TutorialPhasePrefab>(phase);
		if (((object)prefab == null || (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.All) != TutorialPhasePrefab.ControlScheme.All) && (InputManager.instance.activeControlScheme != InputManager.ControlScheme.KeyboardAndMouse || (object)prefab == null || (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.KeyboardAndMouse) != TutorialPhasePrefab.ControlScheme.KeyboardAndMouse))
		{
			if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.Gamepad)
			{
				if ((object)prefab == null)
				{
					return false;
				}
				return (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.Gamepad) == TutorialPhasePrefab.ControlScheme.Gamepad;
			}
			return false;
		}
		return true;
	}

	private void ClearTutorialLocks()
	{
		ClearLocks(m_LockedTutorialQuery);
		ClearLocks(m_LockedTutorialPhaseQuery);
		ClearLocks(m_LockedTutorialTriggerQuery);
		ClearLocks(m_LockedTutorialListQuery);
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			TutorialsConfigurationData singleton = m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>();
			if (base.EntityManager.HasEnabledComponent<Locked>(singleton.m_MapTilesFeature))
			{
				Entity entity = base.EntityManager.CreateEntity(m_UnlockEventArchetype);
				base.EntityManager.SetComponentData(entity, new Unlock(singleton.m_MapTilesFeature));
			}
			m_MapTilePurchaseSystem.UnlockMapTiles();
		}
	}

	private void ClearLocks(EntityQuery query)
	{
		if (!query.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = query.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ClearLock(nativeArray[i]);
			}
			nativeArray.Dispose();
		}
	}

	private void ClearLock(Entity entity)
	{
		NativeArray<UnlockRequirement> nativeArray = base.EntityManager.GetBuffer<UnlockRequirement>(entity, isReadOnly: true).ToNativeArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			UnlockRequirement unlockRequirement = nativeArray[i];
			if (unlockRequirement.m_Prefab == entity && (unlockRequirement.m_Flags & UnlockFlags.RequireAll) != 0)
			{
				ManualUnlock(entity, m_UnlockEventArchetype, base.EntityManager);
				nativeArray.Dispose();
				return;
			}
		}
		nativeArray.Dispose();
	}

	public void SetAllTutorialsShown()
	{
		if (!m_TutorialQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_TutorialQuery.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				SetTutorialShown(nativeArray[i]);
			}
			nativeArray.Dispose();
		}
		if (!m_TutorialPhaseQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray2 = m_TutorialPhaseQuery.ToEntityArray(Allocator.TempJob);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				SetTutorialShown(nativeArray2[j]);
			}
			nativeArray2.Dispose();
		}
	}

	public void CompleteTutorial(Entity tutorial)
	{
		if (!m_SoundQuery.IsEmptyIgnoreFilter)
		{
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TutorialCompletedSound);
		}
		CleanupTutorial(tutorial, passed: true);
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, EntityManager entityManager)
	{
		if (!entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<UnlockRequirement> buffer) || buffer.Length <= 0 || !(buffer[0].m_Prefab == entity) || (buffer[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity entity2 = entityManager.CreateEntity(unlockEventArchetype);
		entityManager.SetComponentData(entity2, new Unlock(entity));
		if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ForceUIGroupUnlockData> buffer2))
		{
			NativeArray<ForceUIGroupUnlockData> nativeArray = buffer2.ToNativeArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity3 = entityManager.CreateEntity(unlockEventArchetype);
				entityManager.SetComponentData(entity3, new Unlock(nativeArray[i].m_Entity));
			}
			nativeArray.Dispose();
		}
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, EntityManager entityManager, EntityCommandBuffer commandBuffer)
	{
		if (!entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<UnlockRequirement> buffer) || buffer.Length <= 0 || !(buffer[0].m_Prefab == entity) || (buffer[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity e = commandBuffer.CreateEntity(unlockEventArchetype);
		commandBuffer.SetComponent(e, new Unlock(entity));
		if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ForceUIGroupUnlockData> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				Entity e2 = commandBuffer.CreateEntity(unlockEventArchetype);
				commandBuffer.SetComponent(e2, new Unlock(buffer2[i].m_Entity));
			}
		}
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, ref BufferLookup<ForceUIGroupUnlockData> forcedUnlocksFromEntity, ref BufferLookup<UnlockRequirement> unlockRequirementsFromEntity, EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey)
	{
		if (!unlockRequirementsFromEntity.TryGetBuffer(entity, out var bufferData) || bufferData.Length <= 0 || !(bufferData[0].m_Prefab == entity) || (bufferData[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity e = commandBuffer.CreateEntity(sortKey, unlockEventArchetype);
		commandBuffer.SetComponent(sortKey, e, new Unlock(entity));
		if (forcedUnlocksFromEntity.TryGetBuffer(entity, out var bufferData2))
		{
			for (int i = 0; i < bufferData2.Length; i++)
			{
				Entity e2 = commandBuffer.CreateEntity(sortKey, unlockEventArchetype);
				commandBuffer.SetComponent(sortKey, e2, new Unlock(bufferData2[i].m_Entity));
			}
		}
	}

	private void UpdateActiveTutorialList()
	{
		if (activeTutorialList != Entity.Null)
		{
			CheckListIntro();
			if (CheckCurrentTutorialListCompleted())
			{
				CompleteCurrentTutorialList();
			}
			if (ShouldReplaceActiveTutorialList())
			{
				ActivateNextTutorialList();
			}
		}
		else
		{
			ActivateNextTutorialList();
		}
	}

	private void CompleteCurrentTutorialList()
	{
		Entity entity = activeTutorialList;
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			TutorialsConfigurationData singleton = m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>();
			if (activeTutorialList == singleton.m_TutorialsIntroList)
			{
				if (base.EntityManager.HasEnabledComponent<Locked>(singleton.m_MapTilesFeature))
				{
					Entity entity2 = base.EntityManager.CreateEntity(m_UnlockEventArchetype);
					base.EntityManager.SetComponentData(entity2, new Unlock(singleton.m_MapTilesFeature));
				}
				m_MapTilePurchaseSystem.UnlockMapTiles();
			}
		}
		if (entity != Entity.Null)
		{
			SetTutorialList(Entity.Null, passed: true);
		}
	}

	private void ActivateNextTutorialList()
	{
		Entity tutorialList = FindNextTutorialList(m_PendingTutorialListQuery);
		SetTutorialList(tutorialList);
	}

	private void CheckListIntro()
	{
		TutorialsConfigurationData singleton = m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>();
		if (activeTutorialList == singleton.m_TutorialsIntroList && !ShownTutorials.ContainsKey(kListIntroKey) && activeTutorial == Entity.Null && !NonListTutorialPending())
		{
			mode = TutorialMode.ListIntro;
			UpdateSettings(kListIntroKey, passed: true);
		}
	}

	private bool NonListTutorialPending()
	{
		Entity entity = FindNextTutorial();
		if (entity != Entity.Null)
		{
			DynamicBuffer<TutorialRef> buffer = base.EntityManager.GetBuffer<TutorialRef>(activeTutorialList, isReadOnly: true);
			for (int i = 0; i < buffer.Length; i++)
			{
				if (buffer[i].m_Tutorial == entity)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	private bool CheckCurrentTutorialListCompleted()
	{
		if (base.EntityManager.HasComponent<TutorialRef>(activeTutorialList))
		{
			ComponentLookup<TutorialCompleted> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tutorials_TutorialCompleted_RO_ComponentLookup, ref base.CheckedStateRef);
			BufferLookup<TutorialAlternative> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tutorials_TutorialAlternative_RO_BufferLookup, ref base.CheckedStateRef);
			DynamicBuffer<TutorialRef> buffer = base.EntityManager.GetBuffer<TutorialRef>(activeTutorialList, isReadOnly: true);
			for (int i = 0; i < buffer.Length; i++)
			{
				if (!IsCompleted(buffer[i].m_Tutorial, bufferLookup, componentLookup))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool ShouldReplaceActiveTutorialList()
	{
		Entity entity = activeTutorialList;
		if (entity != Entity.Null)
		{
			return !base.EntityManager.HasComponent<TutorialActivated>(entity);
		}
		return false;
	}

	private Entity FindNextTutorialList(EntityQuery query)
	{
		if (!query.IsEmptyIgnoreFilter)
		{
			NativeArray<TutorialListData> nativeArray = query.ToComponentDataArray<TutorialListData>(Allocator.TempJob);
			NativeArray<Entity> nativeArray2 = query.ToEntityArray(Allocator.TempJob);
			int index = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_Priority < nativeArray[index].m_Priority)
				{
					index = i;
				}
			}
			Entity result = nativeArray2[index];
			nativeArray.Dispose();
			nativeArray2.Dispose();
			return result;
		}
		return Entity.Null;
	}

	private void SetTutorialList(Entity tutorialList, bool passed = false, bool updateSettings = true)
	{
		Entity entity = activeTutorialList;
		if (!(tutorialList != entity))
		{
			return;
		}
		if (entity != Entity.Null)
		{
			CleanupTutorialList(entity, passed, updateSettings);
			if (passed)
			{
				TutorialsConfigurationData singleton = m_TutorialConfigurationQuery.GetSingleton<TutorialsConfigurationData>();
				if (updateSettings && entity == singleton.m_TutorialsIntroList && !ShownTutorials.ContainsKey(kListOutroKey))
				{
					mode = TutorialMode.ListOutro;
					UpdateSettings(kListOutroKey, passed: true);
				}
			}
		}
		if (tutorialList != Entity.Null)
		{
			SetTutorialShown(tutorialList, updateSettings);
			base.EntityManager.AddComponent<TutorialActive>(tutorialList);
		}
	}

	private void CleanupTutorialList(Entity tutorialList, bool passed = false, bool updateSettings = true)
	{
		if (!(tutorialList != Entity.Null))
		{
			return;
		}
		base.EntityManager.RemoveComponent<TutorialActivated>(tutorialList);
		base.EntityManager.RemoveComponent<TutorialActive>(tutorialList);
		if (!passed)
		{
			return;
		}
		SetTutorialShown(tutorialList);
		base.EntityManager.AddComponent<TutorialCompleted>(tutorialList);
		ManualUnlock(tutorialList, m_UnlockEventArchetype, base.EntityManager);
		if (base.EntityManager.HasComponent<TutorialRef>(tutorialList))
		{
			NativeArray<TutorialRef> nativeArray = base.EntityManager.GetBuffer<TutorialRef>(tutorialList, isReadOnly: true).ToNativeArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CleanupTutorial(nativeArray[i].m_Tutorial, passed, updateSettings);
			}
			nativeArray.Dispose();
		}
		if (updateSettings)
		{
			UpdateSettings(tutorialList, passed: true);
		}
	}

	public void SkipActiveList()
	{
		CompleteCurrentTutorialList();
	}

	public void PreDeserialize(Context context)
	{
		ClearComponents();
	}

	private void ClearComponents()
	{
		base.EntityManager.RemoveComponent<TutorialActive>(m_TutorialListQuery);
		base.EntityManager.RemoveComponent<TutorialCompleted>(m_TutorialListQuery);
		base.EntityManager.RemoveComponent<TutorialShown>(m_TutorialListQuery);
		base.EntityManager.RemoveComponent<AdvisorActivation>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<TutorialActive>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<TutorialCompleted>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<TutorialShown>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<ForceActivation>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<TutorialActivated>(m_TutorialQuery);
		base.EntityManager.RemoveComponent<TutorialPhaseActive>(m_TutorialPhaseQuery);
		base.EntityManager.RemoveComponent<TutorialPhaseCompleted>(m_TutorialPhaseQuery);
		base.EntityManager.RemoveComponent<TutorialPhaseShown>(m_TutorialPhaseQuery);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public TutorialSystem()
	{
	}
}
