using System;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class EventJournalUISystem : UISystemBase
{
	private const string kGroup = "eventJournal";

	private const int kMaxMessages = 100;

	private IEventJournalSystem m_EventJournalSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_TimeDataQuery;

	private RawMapBinding<Entity> m_EventMap;

	private RawValueBinding m_Events;

	public Action eventJournalOpened { get; set; }

	public Action eventJournalClosed { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_EventJournalSystem = base.World.GetOrCreateSystemManaged<EventJournalSystem>();
		IEventJournalSystem eventJournalSystem = m_EventJournalSystem;
		eventJournalSystem.eventEventDataChanged = (Action<Entity>)Delegate.Combine(eventJournalSystem.eventEventDataChanged, new Action<Entity>(OnEventDataChanged));
		IEventJournalSystem eventJournalSystem2 = m_EventJournalSystem;
		eventJournalSystem2.eventEntryAdded = (Action)Delegate.Combine(eventJournalSystem2.eventEntryAdded, new Action(OnEntryAdded));
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		AddBinding(new TriggerBinding("eventJournal", "openJournal", delegate
		{
			eventJournalOpened?.Invoke();
		}));
		AddBinding(new TriggerBinding("eventJournal", "closeJournal", delegate
		{
			eventJournalClosed?.Invoke();
		}));
		AddBinding(m_Events = new RawValueBinding("eventJournal", "events", BindEvents));
		AddBinding(m_EventMap = new RawMapBinding<Entity>("eventJournal", "eventMap", delegate(IJsonWriter binder, Entity entity)
		{
			BindJournalEntry(entity, binder);
		}));
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	private void OnEventDataChanged(Entity entity)
	{
		m_EventMap.Update(entity);
	}

	private void OnEntryAdded()
	{
		m_Events.Update();
	}

	private void BindEvents(IJsonWriter binder)
	{
		binder.ArrayBegin(Math.Min(m_EventJournalSystem.eventJournal.Length, 100));
		int num = m_EventJournalSystem.eventJournal.Length - 1;
		while (num >= 0 && num >= m_EventJournalSystem.eventJournal.Length - 100)
		{
			binder.Write(m_EventJournalSystem.eventJournal[num]);
			num--;
		}
		binder.ArrayEnd();
	}

	private void BindJournalEntry(Entity entity, IJsonWriter binder)
	{
		EventJournalEntry info = m_EventJournalSystem.GetInfo(entity);
		Entity prefab = m_EventJournalSystem.GetPrefab(entity);
		EventPrefab prefab2 = m_PrefabSystem.GetPrefab<EventPrefab>(prefab);
		JournalEventComponent component = prefab2.GetComponent<JournalEventComponent>();
		binder.TypeBegin("eventJournal.EventInfo");
		binder.PropertyName("id");
		binder.Write(prefab2.name);
		binder.PropertyName("icon");
		binder.Write(component.m_Icon);
		binder.PropertyName("date");
		binder.Write(info.m_StartFrame - TimeData.GetSingleton(m_TimeDataQuery).m_FirstFrame);
		binder.PropertyName("data");
		if (m_EventJournalSystem.TryGetData(entity, out var data))
		{
			binder.ArrayBegin(data.Length);
			for (int i = 0; i < data.Length; i++)
			{
				binder.TypeBegin("eventJournal.UIEventData");
				binder.PropertyName("type");
				binder.Write(Enum.GetName(typeof(EventDataTrackingType), data[i].m_Type));
				binder.PropertyName("value");
				binder.Write(data[i].m_Value);
				binder.TypeEnd();
			}
			binder.ArrayEnd();
		}
		else
		{
			binder.WriteNull();
		}
		binder.PropertyName("effects");
		if (m_EventJournalSystem.TryGetCityEffects(entity, out var data2))
		{
			binder.ArrayBegin(data2.Length);
			for (int j = 0; j < data2.Length; j++)
			{
				binder.TypeBegin("eventJournal.UIEventData");
				binder.PropertyName("type");
				binder.Write(Enum.GetName(typeof(EventCityEffectTrackingType), data2[j].m_Type));
				binder.PropertyName("value");
				binder.Write(EventJournalUtils.GetPercentileChange(data2[j]));
				binder.TypeEnd();
			}
			binder.ArrayEnd();
		}
		else
		{
			binder.WriteNull();
		}
		binder.TypeEnd();
	}

	[Preserve]
	public EventJournalUISystem()
	{
	}
}
