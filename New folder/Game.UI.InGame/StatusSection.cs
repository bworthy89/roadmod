using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class StatusSection : InfoSectionBase
{
	private ImageSystem m_ImageSystem;

	private bool m_Dead;

	protected override string group => "StatusSection";

	private NativeList<CitizenCondition> conditions { get; set; }

	private NativeList<Notification> notifications { get; set; }

	private CitizenHappiness happiness { get; set; }

	protected override void Reset()
	{
		conditions.Clear();
		notifications.Clear();
		happiness = default(CitizenHappiness);
		m_Dead = false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		conditions = new NativeList<CitizenCondition>(Allocator.Persistent);
		notifications = new NativeList<Notification>(Allocator.Persistent);
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		conditions.Dispose();
		notifications.Dispose();
		base.OnDestroy();
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Citizen>(selectedEntity))
		{
			return base.EntityManager.HasComponent<HouseholdMember>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Citizen componentData = base.EntityManager.GetComponentData<Citizen>(selectedEntity);
		HouseholdMember componentData2 = base.EntityManager.GetComponentData<HouseholdMember>(selectedEntity);
		happiness = CitizenUIUtils.GetCitizenHappiness(componentData);
		conditions = CitizenUIUtils.GetCitizenConditions(base.EntityManager, selectedEntity, componentData, componentData2, conditions);
		notifications = NotificationsSection.GetNotifications(base.EntityManager, selectedEntity, notifications);
		if (base.EntityManager.TryGetComponent<CurrentTransport>(selectedEntity, out var component))
		{
			notifications = NotificationsSection.GetNotifications(base.EntityManager, component.m_CurrentTransport, notifications);
		}
		m_Dead = CitizenUtils.IsDead(base.EntityManager, selectedEntity);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("happiness");
		if (m_Dead)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(happiness);
		}
		writer.PropertyName("conditions");
		if (m_Dead)
		{
			writer.WriteEmptyArray();
		}
		else
		{
			writer.ArrayBegin(conditions.Length);
			for (int i = 0; i < conditions.Length; i++)
			{
				writer.Write(conditions[i]);
			}
			writer.ArrayEnd();
		}
		writer.PropertyName("notifications");
		writer.ArrayBegin(notifications.Length);
		for (int j = 0; j < notifications.Length; j++)
		{
			Entity entity = notifications[j].entity;
			NotificationIconPrefab prefab = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(entity);
			writer.TypeBegin("selectedInfo.NotificationData");
			writer.PropertyName("key");
			writer.Write(prefab.name);
			writer.PropertyName("iconPath");
			writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public StatusSection()
	{
	}
}
