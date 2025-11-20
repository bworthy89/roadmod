using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.UI.InGame;
using Game.UI.Localization;
using Game.Vehicles;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI;

[CompilerGenerated]
public class NameSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public enum NameType
	{
		Custom,
		Localized,
		Formatted
	}

	public struct Name : IJsonWritable, ISerializable
	{
		private NameType m_NameType;

		private string m_NameID;

		private string[] m_NameArgs;

		public static Name CustomName(string name)
		{
			return new Name
			{
				m_NameType = NameType.Custom,
				m_NameID = name
			};
		}

		public static Name LocalizedName(string nameID)
		{
			return new Name
			{
				m_NameType = NameType.Localized,
				m_NameID = nameID
			};
		}

		public static Name FormattedName(string nameID, params string[] args)
		{
			return new Name
			{
				m_NameType = NameType.Formatted,
				m_NameID = nameID,
				m_NameArgs = args
			};
		}

		public void Write(IJsonWriter writer)
		{
			if (m_NameType == NameType.Custom)
			{
				BindCustomName(writer);
			}
			else if (m_NameType == NameType.Formatted)
			{
				BindFormattedName(writer);
			}
			else if (m_NameType == NameType.Localized)
			{
				BindLocalizedName(writer);
			}
		}

		private void BindCustomName(IJsonWriter writer)
		{
			writer.TypeBegin("names.CustomName");
			writer.PropertyName("name");
			writer.Write(m_NameID);
			writer.TypeEnd();
		}

		private void BindFormattedName(IJsonWriter writer)
		{
			writer.TypeBegin("names.FormattedName");
			writer.PropertyName("nameId");
			writer.Write(m_NameID);
			writer.PropertyName("nameArgs");
			int num = ((m_NameArgs != null) ? (m_NameArgs.Length / 2) : 0);
			writer.MapBegin(num);
			for (int i = 0; i < num; i++)
			{
				writer.Write(m_NameArgs[i * 2] ?? string.Empty);
				writer.Write(m_NameArgs[i * 2 + 1] ?? string.Empty);
			}
			writer.MapEnd();
			writer.TypeEnd();
		}

		private void BindLocalizedName(IJsonWriter writer)
		{
			writer.TypeBegin("names.LocalizedName");
			writer.PropertyName("nameId");
			writer.Write(m_NameID ?? string.Empty);
			writer.TypeEnd();
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			NameType value = m_NameType;
			writer.Write((int)value);
			string value2 = m_NameID ?? string.Empty;
			writer.Write(value2);
			int num = ((m_NameArgs != null) ? m_NameArgs.Length : 0);
			writer.Write(num);
			for (int i = 0; i < num; i++)
			{
				string value3 = m_NameArgs[i] ?? string.Empty;
				writer.Write(value3);
			}
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out int value);
			m_NameType = (NameType)value;
			ref string value2 = ref m_NameID;
			reader.Read(out value2);
			reader.Read(out int value3);
			m_NameArgs = new string[value3];
			for (int i = 0; i < value3; i++)
			{
				reader.Read(out string value4);
				m_NameArgs[i] = value4;
			}
		}
	}

	private EntityQuery m_DeletedQuery;

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private Dictionary<Entity, string> m_Names;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<CustomName>(), ComponentType.ReadOnly<Deleted>());
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_Names = new Dictionary<Entity, string>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_DeletedQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_DeletedQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (m_Names.ContainsKey(nativeArray[i]))
				{
					m_Names.Remove(nativeArray[i]);
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	public string GetDebugName(Entity entity)
	{
		string arg = null;
		if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component))
		{
			arg = ((!base.EntityManager.HasComponent<PrefabData>(component.m_Prefab)) ? "(invalid prefab)" : ((!m_PrefabSystem.TryGetPrefab<PrefabBase>(component.m_Prefab, out var prefab)) ? m_PrefabSystem.GetObsoleteID(component.m_Prefab).GetName() : prefab.name));
		}
		return $"{arg} {entity.Index}";
	}

	public bool TryGetCustomName(Entity entity, out string customName)
	{
		return m_Names.TryGetValue(entity, out customName);
	}

	public void SetCustomName(Entity entity, string name)
	{
		if (entity == Entity.Null)
		{
			return;
		}
		if (base.EntityManager.TryGetComponent<Controller>(entity, out var component))
		{
			entity = component.m_Controller;
		}
		if (name == string.Empty || string.IsNullOrWhiteSpace(name))
		{
			if (m_Names.ContainsKey(entity))
			{
				EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
				entityCommandBuffer.RemoveComponent<CustomName>(entity);
				entityCommandBuffer.AddComponent<BatchesUpdated>(entity);
				m_Names.Remove(entity);
			}
		}
		else
		{
			m_Names[entity] = name;
			EntityCommandBuffer entityCommandBuffer2 = m_EndFrameBarrier.CreateCommandBuffer();
			entityCommandBuffer2.AddComponent<CustomName>(entity);
			entityCommandBuffer2.AddComponent<BatchesUpdated>(entity);
		}
	}

	public string GetRenderedLabelName(Entity entity)
	{
		if (TryGetCustomName(entity, out var customName))
		{
			return customName;
		}
		string id = GetId(entity);
		if (!GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var value))
		{
			return id;
		}
		return value;
	}

	public void BindNameForVirtualKeyboard(IJsonWriter writer, Entity entity)
	{
		writer.Write(GetNameForVirtualKeyboard(entity));
	}

	public Name GetNameForVirtualKeyboard(Entity entity)
	{
		Entity entity2 = Entity.Null;
		if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component))
		{
			entity2 = component.m_Prefab;
		}
		if (entity2 != Entity.Null && (base.EntityManager.HasComponent<TransportLine>(entity) || base.EntityManager.HasComponent<WorkRoute>(entity)))
		{
			if (m_PrefabSystem.TryGetPrefab<RoutePrefab>(entity2, out var prefab))
			{
				return Name.LocalizedName(prefab.m_LocaleID + "[" + prefab.name + "]");
			}
			return Name.CustomName(m_PrefabSystem.GetPrefabName(entity2));
		}
		if (base.EntityManager.TryGetComponent<Controller>(entity, out var component2))
		{
			entity = component2.m_Controller;
		}
		return Name.LocalizedName(GetId(entity, useRandomLocalization: false));
	}

	public void BindName(IJsonWriter writer, Entity entity)
	{
		writer.Write(GetName(entity));
	}

	public Name GetName(Entity entity, bool omitBrand = false)
	{
		Entity entity2 = Entity.Null;
		if (base.EntityManager.TryGetComponent<Controller>(entity, out var component))
		{
			entity = component.m_Controller;
		}
		if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component2))
		{
			entity2 = component2.m_Prefab;
		}
		if (TryGetCustomName(entity, out var customName))
		{
			return Name.CustomName(customName);
		}
		if (base.EntityManager.TryGetComponent<TrainData>(entity2, out var component3) && component3.m_TrackType == TrackTypes.Train)
		{
			return GetTrainName(entity2);
		}
		if (base.EntityManager.TryGetComponent<CompanyData>(entity, out var component4))
		{
			string id = GetId(component4.m_Brand);
			if (id == null)
			{
				return Name.CustomName(m_PrefabSystem.GetPrefabName(entity2) + " brand is null!");
			}
			return Name.LocalizedName(id);
		}
		if (entity2 != Entity.Null && !base.EntityManager.HasComponent<SignatureBuildingData>(entity2) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(entity2, out var component5))
		{
			return GetSpawnableBuildingName(entity, component5.m_ZonePrefab, omitBrand);
		}
		if (base.EntityManager.HasComponent<Game.Routes.TransportStop>(entity) && !base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(entity) && !base.EntityManager.HasComponent<BicycleParking>(entity))
		{
			if (base.EntityManager.HasComponent<Game.Objects.Marker>(entity))
			{
				return GetMarkerTransportStopName(entity);
			}
			return GetStaticTransportStopName(entity);
		}
		if (entity2 != Entity.Null && (base.EntityManager.HasComponent<TransportLine>(entity) || base.EntityManager.HasComponent<WorkRoute>(entity)))
		{
			return GetRouteName(entity, entity2);
		}
		if (entity2 != Entity.Null && base.EntityManager.HasComponent<Citizen>(entity))
		{
			return GetCitizenName(entity);
		}
		if (entity2 != Entity.Null && base.EntityManager.HasComponent<Game.Creatures.Resident>(entity))
		{
			return GetResidentName(entity, entity2);
		}
		if (base.EntityManager.HasComponent<Game.Events.TrafficAccident>(entity))
		{
			return Name.LocalizedName("SelectedInfoPanel.TRAFFIC_ACCIDENT");
		}
		return Name.LocalizedName(GetId(entity));
	}

	private string GetGenderedLastNameId(Entity household, bool male)
	{
		if (household == Entity.Null)
		{
			return null;
		}
		if (base.EntityManager.TryGetComponent<PrefabRef>(household, out var component) && m_PrefabSystem.GetPrefab<PrefabBase>(component).TryGet<RandomGenderedLocalization>(out var component2))
		{
			string text = (male ? component2.m_MaleID : component2.m_FemaleID);
			if (base.EntityManager.TryGetBuffer(household, isReadOnly: true, out DynamicBuffer<RandomLocalizationIndex> buffer) && buffer.Length > 0)
			{
				return LocalizationUtils.AppendIndex(text, buffer[0]);
			}
			return text;
		}
		return GetId(household);
	}

	public void BindFamilyName(IJsonWriter writer, Entity household)
	{
		writer.Write(GetFamilyName(household));
	}

	private Name GetFamilyName(Entity household)
	{
		if (TryGetCustomName(household, out var customName))
		{
			return Name.CustomName(customName);
		}
		int num = 0;
		DynamicBuffer<HouseholdCitizen> buffer = base.EntityManager.GetBuffer<HouseholdCitizen>(household);
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity entity = buffer[i];
			if (base.EntityManager.TryGetComponent<Citizen>(entity, out var component) && (component.m_State & CitizenFlags.Male) != CitizenFlags.None)
			{
				num++;
			}
		}
		return Name.LocalizedName(GetGenderedLastNameId(household, num > 1));
	}

	private string GetId(Entity entity, bool useRandomLocalization = true)
	{
		if (entity == Entity.Null)
		{
			return null;
		}
		Entity entity2 = Entity.Null;
		if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component))
		{
			entity2 = component.m_Prefab;
		}
		if (!base.EntityManager.HasComponent<SignatureBuildingData>(entity2) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(entity2, out var component2))
		{
			entity2 = component2.m_ZonePrefab;
		}
		if (base.EntityManager.HasComponent<ChirperAccountData>(entity) || base.EntityManager.HasComponent<BrandData>(entity))
		{
			entity2 = entity;
		}
		if (entity2 != Entity.Null)
		{
			if (m_PrefabSystem.TryGetPrefab<PrefabBase>(entity2, out var prefab))
			{
				if (prefab.TryGet<Game.Prefabs.Localization>(out var component3))
				{
					if (useRandomLocalization && component3 is RandomLocalization && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RandomLocalizationIndex> buffer) && buffer.Length > 0)
					{
						return LocalizationUtils.AppendIndex(component3.m_LocalizationID, buffer[0]);
					}
					return component3.m_LocalizationID;
				}
				m_PrefabUISystem.GetTitleAndDescription(entity2, out var titleId, out var _);
				return titleId;
			}
			return m_PrefabSystem.GetObsoleteID(entity2).GetName();
		}
		return string.Empty;
	}

	private Name GetTrainName(Entity entity)
	{
		if (base.EntityManager.HasComponent<CargoTransportVehicleData>(entity))
		{
			return Name.LocalizedName("Assets.CARGO_TRAIN_NAME");
		}
		return Name.LocalizedName("Assets.PASSENGER_TRAIN_NAME");
	}

	private Name GetCitizenName(Entity entity)
	{
		string id = GetId(entity);
		HouseholdMember componentData = base.EntityManager.GetComponentData<HouseholdMember>(entity);
		Citizen component;
		bool male = base.EntityManager.TryGetComponent<Citizen>(entity, out component) && (component.m_State & CitizenFlags.Male) != 0;
		string genderedLastNameId = GetGenderedLastNameId(componentData.m_Household, male);
		return Name.FormattedName("Assets.CITIZEN_NAME_FORMAT", "FIRST_NAME", id, "LAST_NAME", genderedLastNameId);
	}

	private Name GetResidentName(Entity entity, Entity prefab)
	{
		base.EntityManager.TryGetComponent<PseudoRandomSeed>(entity, out var component);
		base.EntityManager.TryGetComponent<CreatureData>(prefab, out var component2);
		Random random = component.GetRandom(PseudoRandomSeed.kDummyName);
		bool flag = false;
		if (component2.m_Gender == GenderMask.Male)
		{
			flag = true;
		}
		else if (component2.m_Gender != GenderMask.Female)
		{
			flag = random.NextBool();
		}
		string text = (flag ? "Assets.CITIZEN_NAME_MALE" : "Assets.CITIZEN_NAME_FEMALE");
		string text2 = (flag ? "Assets.CITIZEN_SURNAME_MALE" : "Assets.CITIZEN_SURNAME_FEMALE");
		PrefabBase prefab2 = m_PrefabSystem.GetPrefab<PrefabBase>(prefab);
		int localizationIndexCount = RandomLocalization.GetLocalizationIndexCount(prefab2, text);
		int localizationIndexCount2 = RandomLocalization.GetLocalizationIndexCount(prefab2, text2);
		string text3 = LocalizationUtils.AppendIndex(text, new RandomLocalizationIndex(random.NextInt(localizationIndexCount)));
		string text4 = LocalizationUtils.AppendIndex(text2, new RandomLocalizationIndex(random.NextInt(localizationIndexCount2)));
		return Name.FormattedName("Assets.CITIZEN_NAME_FORMAT", "FIRST_NAME", text3, "LAST_NAME", text4);
	}

	private Name GetSpawnableBuildingName(Entity building, Entity zone, bool omitBrand = false)
	{
		BuildingUtils.GetAddress(base.EntityManager, building, out var road, out var number);
		if (!TryGetCustomName(road, out var customName))
		{
			customName = GetId(road);
		}
		if (customName == null)
		{
			return Name.LocalizedName(GetId(building));
		}
		if (!omitBrand && base.EntityManager.TryGetComponent<ZoneData>(zone, out var component) && component.m_AreaType != AreaType.Residential)
		{
			string brandId = GetBrandId(building);
			if (brandId != null)
			{
				return Name.FormattedName("Assets.NAMED_ADDRESS_NAME_FORMAT", "NAME", brandId, "ROAD", customName, "NUMBER", number.ToString());
			}
		}
		return Name.FormattedName("Assets.ADDRESS_NAME_FORMAT", "ROAD", customName, "NUMBER", number.ToString());
	}

	private string GetBrandId(Entity building)
	{
		DynamicBuffer<Renter> buffer = base.EntityManager.GetBuffer<Renter>(building, isReadOnly: true);
		for (int i = 0; i < buffer.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<CompanyData>(buffer[i].m_Renter, out var component))
			{
				return GetId(component.m_Brand);
			}
		}
		return null;
	}

	private Name GetStaticTransportStopName(Entity stop)
	{
		BuildingUtils.GetAddress(base.EntityManager, stop, out var road, out var number);
		if (!TryGetCustomName(road, out var customName))
		{
			customName = GetId(road);
		}
		if (customName == null)
		{
			return Name.LocalizedName(GetId(stop));
		}
		return Name.FormattedName("Assets.ADDRESS_NAME_FORMAT", "ROAD", customName, "NUMBER", number.ToString());
	}

	private Name GetMarkerTransportStopName(Entity stop)
	{
		Entity entity = stop;
		for (int i = 0; i < 8; i++)
		{
			if (!base.EntityManager.TryGetComponent<Owner>(entity, out var component))
			{
				break;
			}
			entity = component.m_Owner;
		}
		return Name.LocalizedName(GetId(entity));
	}

	private Name GetRouteName(Entity route, Entity prefab)
	{
		string text = "";
		if (base.EntityManager.TryGetComponent<RouteNumber>(route, out var component))
		{
			text = component.m_Number.ToString();
		}
		if (m_PrefabSystem.TryGetPrefab<RoutePrefab>(prefab, out var prefab2))
		{
			return Name.FormattedName(prefab2.m_LocaleID + "[" + prefab2.name + "]", "NUMBER", text);
		}
		return Name.CustomName(m_PrefabSystem.GetPrefabName(prefab) + " " + text);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int count = m_Names.Count;
		writer.Write(count);
		foreach (KeyValuePair<Entity, string> item in m_Names)
		{
			Entity key = item.Key;
			writer.Write(key);
			string value = item.Value;
			writer.Write(value);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Names.Clear();
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			reader.Read(out string value3);
			if (value2 != Entity.Null)
			{
				m_Names.Add(value2, value3);
			}
		}
	}

	public void SetDefaults(Context context)
	{
		m_Names.Clear();
	}

	[Preserve]
	public NameSystem()
	{
	}
}
