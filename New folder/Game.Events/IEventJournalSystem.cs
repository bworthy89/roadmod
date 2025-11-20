using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Events;

public interface IEventJournalSystem
{
	NativeList<Entity> eventJournal { get; }

	IEnumerable<JournalEventComponent> eventPrefabs { get; }

	Action<Entity> eventEventDataChanged { get; set; }

	Action eventEntryAdded { get; set; }

	EventJournalEntry GetInfo(Entity journalEntity);

	Entity GetPrefab(Entity journalEntity);

	bool TryGetData(Entity journalEntity, out DynamicBuffer<EventJournalData> data);

	bool TryGetCityEffects(Entity journalEntity, out DynamicBuffer<EventJournalCityEffect> data);
}
