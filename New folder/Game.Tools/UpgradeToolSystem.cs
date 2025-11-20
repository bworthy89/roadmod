using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.PSI;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class UpgradeToolSystem : ObjectToolBaseSystem
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
		}
	}

	public const string kToolID = "Upgrade Tool";

	private CityConfigurationSystem m_CityConfigurationSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_ContainerQuery;

	private Entity m_UpgradingObject;

	private NativeList<ControlPoint> m_ControlPoints;

	private RandomSeed m_RandomSeed;

	private bool m_AlreadyCreated;

	private ObjectPrefab m_Prefab;

	private IProxyAction m_PlaceUpgrade;

	private IProxyAction m_Rebuild;

	private TypeHandle __TypeHandle;

	public override string toolID => "Upgrade Tool";

	public ObjectPrefab prefab
	{
		get
		{
			return m_Prefab;
		}
		set
		{
			if (value != m_Prefab)
			{
				m_Prefab = value;
				m_ForceUpdate = true;
			}
		}
	}

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_PlaceUpgrade;
			yield return m_Rebuild;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_ContainerQuery = GetContainerQuery();
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_PlaceUpgrade = InputManager.instance.toolActionCollection.GetActionState("Place Upgrade", "UpgradeToolSystem");
		m_Rebuild = InputManager.instance.toolActionCollection.GetActionState("Rebuild", "UpgradeToolSystem");
		m_ControlPoints = new NativeList<ControlPoint>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_RandomSeed = RandomSeed.Next();
		m_AlreadyCreated = false;
		base.requireZones = true;
		base.requireAreas = AreaTypeMask.Lots;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			base.applyActionOverride = ((prefab != null) ? m_PlaceUpgrade : m_Rebuild);
			base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
			base.cancelActionOverride = m_MouseCancel;
			base.cancelAction.shouldBeEnabled = base.actionsEnabled;
		}
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (!m_ToolSystem.actionMode.IsEditor() && prefab is ObjectPrefab objectPrefab && prefab.Has<Game.Prefabs.ServiceUpgrade>())
		{
			Entity entity = m_PrefabSystem.GetEntity(prefab);
			if (InternalCompilerInterface.HasComponentAfterCompletingDependency(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef, entity))
			{
				return false;
			}
			this.prefab = objectPrefab;
			return true;
		}
		return false;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		m_UpgradingObject = m_ToolSystem.selected;
		if (prefab != null)
		{
			if (!base.EntityManager.HasBuffer<InstalledUpgrade>(m_UpgradingObject))
			{
				m_UpgradingObject = Entity.Null;
			}
			if (m_PrefabSystem.TryGetComponentData<BuildingExtensionData>(prefab, out var component) && component.m_HasUndergroundElements)
			{
				base.requireNet |= Layer.Road;
			}
			UpdateInfoview(m_PrefabSystem.GetEntity(prefab));
		}
		else
		{
			if (!base.EntityManager.HasComponent<Destroyed>(m_UpgradingObject))
			{
				m_UpgradingObject = Entity.Null;
			}
			UpdateInfoview(Entity.Null);
		}
		GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
		UpdateActions();
		if (m_UpgradingObject != Entity.Null && !m_ToolSystem.fullUpdateRequired && (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
		{
			if (base.cancelAction.WasPressedThisFrame())
			{
				return Cancel(inputDeps);
			}
			if (base.applyAction.WasPressedThisFrame())
			{
				return Apply(inputDeps);
			}
			return Update(inputDeps);
		}
		return Clear(inputDeps);
	}

	private JobHandle Cancel(JobHandle inputDeps)
	{
		m_ToolSystem.activeTool = m_DefaultToolSystem;
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = false;
		return inputDeps;
	}

	private JobHandle Apply(JobHandle inputDeps)
	{
		if (GetAllowApply())
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
			base.applyMode = ApplyMode.Apply;
			m_RandomSeed = RandomSeed.Next();
			m_AlreadyCreated = false;
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceUpgradeSound);
			if (m_ToolSystem.actionMode.IsGame() && prefab != null)
			{
				Game.Objects.Transform componentData = base.EntityManager.GetComponentData<Game.Objects.Transform>(m_UpgradingObject);
				Telemetry.PlaceBuilding(m_UpgradingObject, prefab, componentData.m_Position);
			}
			return inputDeps;
		}
		m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
		return Update(inputDeps);
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		if (m_ToolSystem.selected == Entity.Null)
		{
			base.applyMode = ApplyMode.Clear;
			m_AlreadyCreated = false;
			return inputDeps;
		}
		if (m_AlreadyCreated && !m_ForceUpdate)
		{
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = true;
		return CreateTempObject(inputDeps);
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = false;
		return inputDeps;
	}

	private JobHandle CreateTempObject(JobHandle inputDeps)
	{
		Game.Objects.Transform componentData = base.EntityManager.GetComponentData<Game.Objects.Transform>(m_UpgradingObject);
		ControlPoint value = new ControlPoint
		{
			m_Position = componentData.m_Position,
			m_Rotation = componentData.m_Rotation
		};
		if (prefab != null && m_PrefabSystem.HasComponent<BuildingExtensionData>(prefab))
		{
			BuildingExtensionData componentData2 = m_PrefabSystem.GetComponentData<BuildingExtensionData>(prefab);
			value.m_Position = ObjectUtils.LocalToWorld(componentData, componentData2.m_Position);
		}
		m_ControlPoints.Clear();
		m_ControlPoints.Add(in value);
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (m_UpgradingObject != Entity.Null)
		{
			Entity objectPrefab = Entity.Null;
			if (prefab != null)
			{
				objectPrefab = m_PrefabSystem.GetEntity(prefab);
			}
			Entity laneContainer = Entity.Null;
			if (m_ToolSystem.actionMode.IsEditor())
			{
				GetContainers(m_ContainerQuery, out laneContainer, out var _);
			}
			jobHandle = JobHandle.CombineDependencies(jobHandle, CreateDefinitions(objectPrefab, Entity.Null, Entity.Null, m_UpgradingObject, Entity.Null, laneContainer, m_CityConfigurationSystem.defaultTheme, m_ControlPoints, default(NativeReference<AttachmentData>), m_ToolSystem.actionMode.IsEditor(), m_CityConfigurationSystem.leftHandTraffic, removing: false, stamping: false, base.brushSize, math.radians(base.brushAngle), base.brushStrength, 0f, UnityEngine.Time.deltaTime, m_RandomSeed, GetActualSnap(), AgeMask.Sapling, inputDeps));
		}
		return jobHandle;
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
	public UpgradeToolSystem()
	{
	}
}
