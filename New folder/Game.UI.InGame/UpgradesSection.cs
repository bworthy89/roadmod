using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Audio;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class UpgradesSection : InfoSectionBase
{
	private ToolSystem m_ToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private UIInitializeSystem m_UIInitializeSystem;

	private PoliciesUISystem m_PoliciesUISystem;

	private PolicyPrefab m_BuildingOutOfServicePolicy;

	private AudioManager m_AudioManager;

	private EntityQuery m_SoundQuery;

	protected override string group => "UpgradesSection";

	private NativeList<Entity> extensions { get; set; }

	private NativeList<Entity> subBuildings { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetExistingSystemManaged<ToolSystem>();
		m_ObjectToolSystem = base.World.GetExistingSystemManaged<ObjectToolSystem>();
		m_UIInitializeSystem = base.World.GetOrCreateSystemManaged<UIInitializeSystem>();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		extensions = new NativeList<Entity>(5, Allocator.Persistent);
		subBuildings = new NativeList<Entity>(10, Allocator.Persistent);
		AddBinding(new TriggerBinding<Entity>(group, "delete", OnDelete));
		AddBinding(new TriggerBinding<Entity>(group, "relocate", OnRelocate));
		AddBinding(new TriggerBinding<Entity>(group, "focus", OnFocus));
		AddBinding(new TriggerBinding<Entity>(group, "toggle", OnToggle));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		extensions.Dispose();
		subBuildings.Dispose();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		foreach (PolicyPrefab policy in m_UIInitializeSystem.policies)
		{
			if (policy.name == "Out of Service")
			{
				m_BuildingOutOfServicePolicy = policy;
			}
		}
	}

	private void OnToggle(Entity entity)
	{
		Building component;
		Extension component2;
		bool flag = (base.EntityManager.TryGetComponent<Building>(entity, out component) && BuildingUtils.CheckOption(component, BuildingOption.Inactive)) || (base.EntityManager.TryGetComponent<Extension>(entity, out component2) && (component2.m_Flags & ExtensionFlags.Disabled) != 0);
		m_PoliciesUISystem.SetSelectedInfoPolicy(entity, m_PrefabSystem.GetEntity(m_BuildingOutOfServicePolicy), !flag);
	}

	private void OnRelocate(Entity entity)
	{
		m_ObjectToolSystem.StartMoving(entity);
		m_ToolSystem.activeTool = m_ObjectToolSystem;
	}

	private void OnDelete(Entity entity)
	{
		if (base.EntityManager.Exists(entity))
		{
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_BulldozeSound);
			m_EndFrameBarrier.CreateCommandBuffer().AddComponent<Deleted>(entity);
		}
	}

	private void OnFocus(Entity entity)
	{
		bool flag = SelectedInfoUISystem.s_CameraController != null && SelectedInfoUISystem.s_CameraController.controllerEnabled && SelectedInfoUISystem.s_CameraController.followedEntity == entity;
		m_InfoUISystem.Focus((!flag) ? entity : Entity.Null);
	}

	protected override void Reset()
	{
		extensions.Clear();
		subBuildings.Clear();
	}

	private bool Visible()
	{
		bool result = false;
		if (base.EntityManager.TryGetComponent<PrefabRef>(GetUpgradable(selectedEntity), out var component) && base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<BuildingUpgradeElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity upgrade = buffer[i].m_Upgrade;
				if (base.EntityManager.HasComponent<UIObjectData>(upgrade))
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	private Entity GetUpgradable(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Attached>(entity, out var component))
		{
			return component.m_Parent;
		}
		return entity;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (!base.EntityManager.TryGetBuffer(GetUpgradable(selectedEntity), isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
		{
			return;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity value = buffer[i].m_Upgrade;
			if (base.EntityManager.HasComponent<UIObjectData>(base.EntityManager.GetComponentData<PrefabRef>(value).m_Prefab))
			{
				if (base.EntityManager.HasComponent<Extension>(value))
				{
					extensions.Add(in value);
				}
				else
				{
					subBuildings.Add(in value);
				}
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("extensions");
		writer.ArrayBegin(extensions.Length);
		for (int i = 0; i < extensions.Length; i++)
		{
			Entity entity = extensions[i];
			writer.TypeBegin(group + ".Upgrade");
			writer.PropertyName("name");
			m_NameSystem.BindName(writer, entity);
			writer.PropertyName("entity");
			writer.Write(entity);
			Extension component;
			bool value = base.EntityManager.TryGetComponent<Extension>(entity, out component) && (component.m_Flags & ExtensionFlags.Disabled) != 0;
			writer.PropertyName("disabled");
			writer.Write(value);
			bool value2 = SelectedInfoUISystem.s_CameraController != null && SelectedInfoUISystem.s_CameraController.controllerEnabled && SelectedInfoUISystem.s_CameraController.followedEntity == entity;
			writer.PropertyName("focused");
			writer.Write(value2);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		writer.PropertyName("subBuildings");
		writer.ArrayBegin(subBuildings.Length);
		for (int j = 0; j < subBuildings.Length; j++)
		{
			Entity entity2 = subBuildings[j];
			writer.TypeBegin(group + ".Upgrade");
			writer.PropertyName("name");
			m_NameSystem.BindName(writer, entity2);
			writer.PropertyName("entity");
			writer.Write(entity2);
			Building component2;
			bool value3 = base.EntityManager.TryGetComponent<Building>(entity2, out component2) && BuildingUtils.CheckOption(component2, BuildingOption.Inactive);
			writer.PropertyName("disabled");
			writer.Write(value3);
			bool value4 = SelectedInfoUISystem.s_CameraController != null && SelectedInfoUISystem.s_CameraController.controllerEnabled && SelectedInfoUISystem.s_CameraController.followedEntity == entity2;
			writer.PropertyName("focused");
			writer.Write(value4);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public UpgradesSection()
	{
	}
}
