using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Input;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class UpgradeMenuUISystem : UISystemBase
{
	public struct SortableEntity : IComparable<SortableEntity>
	{
		public Entity m_Entity;

		public int m_Priority;

		public int CompareTo(SortableEntity other)
		{
			return m_Priority.CompareTo(other.m_Priority);
		}
	}

	private const string kGroup = "upgradeMenu";

	private EntityQuery m_UnlockedUpgradeQuery;

	private EntityQuery m_ChangedUpgradeQuery;

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private ObjectToolSystem m_ObjectToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private NetToolSystem m_NetToolSystem;

	private RouteToolSystem m_RouteToolSystem;

	private UpgradeToolSystem m_UpgradeToolSystem;

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private ToolbarUISystem m_ToolbarUISystem;

	private SelectedInfoUISystem m_SelectedInfoUISystem;

	private UniqueAssetTrackingSystem m_UniqueAssetTrackingSystem;

	private RawMapBinding<Entity> m_UpgradesBinding;

	private RawMapBinding<Entity> m_UpgradeDetailsBinding;

	private ValueBinding<Entity> m_SelectedUpgradeBinding;

	private ValueBinding<bool> m_UpgradingBinding;

	private NativeList<SortableEntity> m_Upgrades;

	private NativeList<SortableEntity> m_Modules;

	private bool m_UniqueAssetStatusChanged;

	private bool m_SelectedWasDestroed;

	private bool m_HasUnlockedUpgradesLastFrame;

	public override GameMode gameMode => GameMode.Game;

	public bool upgrading
	{
		get
		{
			ValueBinding<Entity> valueBinding = m_SelectedUpgradeBinding;
			if (valueBinding != null && valueBinding.active)
			{
				return m_SelectedUpgradeBinding.value != Entity.Null;
			}
			return false;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UnlockedUpgradeQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_ChangedUpgradeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_ToolbarUISystem = base.World.GetOrCreateSystemManaged<ToolbarUISystem>();
		m_SelectedInfoUISystem = base.World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_UpgradeToolSystem = base.World.GetOrCreateSystemManaged<UpgradeToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_RouteToolSystem = base.World.GetOrCreateSystemManaged<RouteToolSystem>();
		m_UniqueAssetTrackingSystem = base.World.GetOrCreateSystemManaged<UniqueAssetTrackingSystem>();
		UniqueAssetTrackingSystem uniqueAssetTrackingSystem = m_UniqueAssetTrackingSystem;
		uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged = (Action<Entity, bool>)Delegate.Combine(uniqueAssetTrackingSystem.EventUniqueAssetStatusChanged, new Action<Entity, bool>(OnUniqueAssetStatusChanged));
		AddBinding(m_UpgradesBinding = new RawMapBinding<Entity>("upgradeMenu", "upgrades", BindUpgrades));
		AddBinding(m_SelectedUpgradeBinding = new ValueBinding<Entity>("upgradeMenu", "selectedUpgrade", Entity.Null));
		AddBinding(m_UpgradeDetailsBinding = new RawMapBinding<Entity>("upgradeMenu", "upgradeDetails", BindUpgradeDetails));
		AddBinding(new TriggerBinding<Entity, Entity>("upgradeMenu", "selectUpgrade", SelectUpgrade));
		AddBinding(new TriggerBinding("upgradeMenu", "clearUpgradeSelection", ClearUpgradeSelection));
		AddBinding(m_UpgradingBinding = new ValueBinding<bool>("upgradeMenu", "upgrading", initialValue: false));
		m_Upgrades = new NativeList<SortableEntity>(9, Allocator.Persistent);
		m_Modules = new NativeList<SortableEntity>(9, Allocator.Persistent);
	}

	private void OnUniqueAssetStatusChanged(Entity prefabEntity, bool placed)
	{
		m_UniqueAssetStatusChanged = true;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Upgrades.Dispose();
		m_Modules.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		SelectedInfoUISystem selectedInfoUISystem = m_SelectedInfoUISystem;
		selectedInfoUISystem.eventSelectionChanged = (Action<Entity, Entity, float3>)Delegate.Combine(selectedInfoUISystem.eventSelectionChanged, new Action<Entity, Entity, float3>(OnSelectionChanged));
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		SelectedInfoUISystem selectedInfoUISystem = m_SelectedInfoUISystem;
		selectedInfoUISystem.eventSelectionChanged = (Action<Entity, Entity, float3>)Delegate.Remove(selectedInfoUISystem.eventSelectionChanged, new Action<Entity, Entity, float3>(OnSelectionChanged));
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Remove(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = HasDestroed(m_SelectedInfoUISystem.selectedEntity);
		Entity upgradable = GetUpgradable(m_SelectedInfoUISystem.selectedEntity);
		if (m_HasUnlockedUpgradesLastFrame)
		{
			m_HasUnlockedUpgradesLastFrame = false;
			m_UpgradesBinding.UpdateAll();
			m_UpgradeDetailsBinding.UpdateAll();
		}
		else if (HasUnlockedUpgrade(upgradable) || flag || PrefabUtils.HasUnlockedPrefabAll<BuildingUpgradeElement, UIObjectData>(base.EntityManager, m_UnlockedUpgradeQuery) || PrefabUtils.HasUnlockedPrefabAll<BuildingModuleData, UIObjectData>(base.EntityManager, m_UnlockedUpgradeQuery) || HasChangedUniqueUpgrade() || m_UniqueAssetStatusChanged || (base.EntityManager.HasComponent<Updated>(upgradable) && base.EntityManager.HasComponent<Destroyed>(upgradable) != (m_Upgrades.Length == 0)))
		{
			m_UpgradesBinding.UpdateAll();
			m_UpgradeDetailsBinding.UpdateAll();
		}
		m_UniqueAssetStatusChanged = false;
	}

	private bool HasChangedUniqueUpgrade()
	{
		if (!m_ChangedUpgradeQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<PrefabRef> nativeArray = m_ChangedUpgradeQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (base.EntityManager.HasComponent<BuildingExtensionData>(nativeArray[i].m_Prefab) || (base.EntityManager.TryGetComponent<ServiceUpgradeData>(nativeArray[i].m_Prefab, out var component) && component.m_ForbidMultiple))
					{
						return true;
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		return false;
	}

	private bool HasDestroed(Entity upgradable)
	{
		if (base.EntityManager.HasComponent<Updated>(upgradable) && base.EntityManager.HasComponent<Destroyed>(upgradable) != m_SelectedWasDestroed)
		{
			m_SelectedWasDestroed = !m_SelectedWasDestroed;
			return true;
		}
		return false;
	}

	private bool HasUnlockedUpgrade(Entity upgradable)
	{
		if (m_UnlockedUpgradeQuery.IsEmpty)
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<PrefabRef>(upgradable, out var component))
		{
			return false;
		}
		using (NativeArray<Unlock> nativeArray = m_UnlockedUpgradeQuery.ToComponentDataArray<Unlock>(Allocator.TempJob))
		{
			foreach (Unlock item in nativeArray)
			{
				if (!base.EntityManager.HasComponent<UIObjectData>(item.m_Prefab) || !base.EntityManager.TryGetBuffer(item.m_Prefab, isReadOnly: true, out DynamicBuffer<ServiceUpgradeBuilding> buffer))
				{
					continue;
				}
				foreach (ServiceUpgradeBuilding item2 in buffer)
				{
					if (item2.m_Building == component.m_Prefab)
					{
						m_HasUnlockedUpgradesLastFrame = true;
						return true;
					}
				}
			}
		}
		return false;
	}

	private Entity GetUpgradable(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Attached>(entity, out var component))
		{
			return component.m_Parent;
		}
		return entity;
	}

	private void ClearUpgradeSelection()
	{
		if (m_SelectedUpgradeBinding.value != Entity.Null)
		{
			m_ToolSystem.activeTool = m_DefaultTool;
			SelectUpgrade(Entity.Null, Entity.Null);
		}
	}

	private void BindUpgrades(IJsonWriter writer, Entity upgradable)
	{
		if (base.EntityManager.HasComponent<Destroyed>(upgradable))
		{
			writer.WriteEmptyArray();
			return;
		}
		upgradable = GetUpgradable(upgradable);
		if (!base.EntityManager.Exists(upgradable) || !base.EntityManager.TryGetComponent<PrefabRef>(upgradable, out var component) || base.EntityManager.HasComponent<Destroyed>(upgradable))
		{
			writer.WriteEmptyArray();
			return;
		}
		m_Upgrades.Clear();
		m_Modules.Clear();
		if (base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<BuildingUpgradeElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity upgrade = buffer[i].m_Upgrade;
				if (base.EntityManager.TryGetComponent<UIObjectData>(upgrade, out var component2))
				{
					ref NativeList<SortableEntity> reference = ref m_Upgrades;
					SortableEntity value = new SortableEntity
					{
						m_Entity = upgrade,
						m_Priority = component2.m_Priority
					};
					reference.Add(in value);
				}
			}
		}
		if (base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<BuildingModule> buffer2))
		{
			for (int j = 0; j < buffer2.Length; j++)
			{
				Entity module = buffer2[j].m_Module;
				if (base.EntityManager.TryGetComponent<UIObjectData>(module, out var component3))
				{
					ref NativeList<SortableEntity> reference2 = ref m_Modules;
					SortableEntity value = new SortableEntity
					{
						m_Entity = module,
						m_Priority = component3.m_Priority
					};
					reference2.Add(in value);
				}
			}
		}
		m_Upgrades.Sort();
		m_Modules.Sort();
		writer.ArrayBegin(m_Upgrades.Length + m_Modules.Length);
		for (int k = 0; k < m_Upgrades.Length; k++)
		{
			SortableEntity sortableEntity = m_Upgrades[k];
			var (unique, placed) = CheckExtensionBuiltStatus(upgradable, sortableEntity.m_Entity);
			m_ToolbarUISystem.BindAsset(writer, sortableEntity.m_Entity, unique, placed);
		}
		for (int l = 0; l < m_Modules.Length; l++)
		{
			SortableEntity sortableEntity2 = m_Modules[l];
			m_ToolbarUISystem.BindAsset(writer, sortableEntity2.m_Entity);
		}
		writer.ArrayEnd();
	}

	private void BindUpgradeDetails(IJsonWriter writer, Entity upgrade)
	{
		Entity upgradable = GetUpgradable(m_SelectedInfoUISystem.selectedEntity);
		var (unique, placed) = CheckExtensionBuiltStatus(upgradable, upgrade);
		m_PrefabUISystem.BindPrefabDetails(writer, upgrade, unique, placed);
	}

	private (bool extension, bool built) CheckExtensionBuiltStatus(Entity upgradableEntity, Entity upgradeEntity)
	{
		bool flag = false;
		bool flag2 = false;
		if (ForbidMultipleUpgrades(upgradeEntity) && base.EntityManager.TryGetBuffer(upgradableEntity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
		{
			flag = true;
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(buffer[i].m_Upgrade, out var component) && component.m_Prefab == upgradeEntity)
				{
					flag2 = true;
				}
			}
		}
		bool flag3 = m_UniqueAssetTrackingSystem.IsUniqueAsset(upgradeEntity);
		bool flag4 = m_UniqueAssetTrackingSystem.IsPlacedUniqueAsset(upgradeEntity);
		return (extension: flag || flag3, built: flag2 || flag4);
	}

	private bool ForbidMultipleUpgrades(Entity upgradeEntity)
	{
		if (base.EntityManager.HasComponent<BuildingExtensionData>(upgradeEntity))
		{
			return true;
		}
		if (base.EntityManager.TryGetComponent<ServiceUpgradeData>(upgradeEntity, out var component) && component.m_ForbidMultiple)
		{
			return true;
		}
		return false;
	}

	private void SelectUpgrade(Entity upgradable, Entity upgrade)
	{
		upgradable = GetUpgradable(upgradable);
		m_SelectedUpgradeBinding.Update(upgrade);
		bool item = CheckExtensionBuiltStatus(upgradable, upgrade).built;
		if (upgradable != Entity.Null && upgrade != Entity.Null && !base.EntityManager.HasEnabledComponent<Locked>(upgrade) && !item)
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(upgrade);
			base.EntityManager.RemoveComponent<UIHighlight>(upgrade);
			m_UpgradingBinding.Update(newValue: true);
			m_UpgradesBinding.UpdateAll();
			m_ToolSystem.ActivatePrefabTool(prefab);
			return;
		}
		PrefabBase prefab2 = m_ToolSystem.activeTool.GetPrefab();
		if (prefab2 == null)
		{
			m_ToolSystem.activeTool = m_DefaultTool;
			return;
		}
		Entity entity = m_PrefabSystem.GetEntity(prefab2);
		if (!(m_SelectedUpgradeBinding.value != Entity.Null) || !base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<BuildingUpgradeElement> buffer))
		{
			return;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			if (!(m_SelectedUpgradeBinding.value != buffer[i].m_Upgrade))
			{
				m_ToolSystem.activeTool = m_DefaultTool;
				break;
			}
		}
	}

	private void OnSelectionChanged(Entity entity, Entity prefab, float3 position)
	{
		if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse)
		{
			ClearUpgradeSelection();
		}
		m_SelectedWasDestroed = base.EntityManager.HasComponent<Destroyed>(entity);
	}

	private void OnToolChanged(ToolBaseSystem tool)
	{
		if (tool == m_DefaultTool && InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse)
		{
			ClearUpgradeSelection();
		}
		bool flag = tool == m_ObjectToolSystem && m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Upgrade;
		bool flag2 = tool == m_NetToolSystem && m_NetToolSystem.serviceUpgrade;
		bool flag3 = tool == m_RouteToolSystem && m_RouteToolSystem.serviceUpgrade;
		Owner component;
		bool flag4 = m_ToolSystem.activeTool == m_AreaToolSystem && base.EntityManager.TryGetComponent<Owner>(m_AreaToolSystem.recreate, out component) && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(component.m_Owner);
		PrefabBase prefab = tool.GetPrefab();
		Entity entity = (((object)prefab != null) ? m_PrefabSystem.GetEntity(prefab) : Entity.Null);
		m_UpgradingBinding.Update(base.EntityManager.HasComponent<BuildingModuleData>(entity) || flag || flag2 || flag3 || flag4 || tool == m_UpgradeToolSystem);
	}

	[Preserve]
	public UpgradeMenuUISystem()
	{
	}
}
