using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI;

[CompilerGenerated]
public class ImageSystem : GameSystemBase
{
	private const string kPlaceholderIcon = "Media/Placeholder.svg";

	private const string kCitizenIcon = "Media/Game/Icons/Citizen.svg";

	private const string kTouristIcon = "Media/Game/Icons/Tourist.svg";

	private const string kCommuterIcon = "Media/Game/Icons/Commuter.svg";

	private const string kAnimalIcon = "Media/Game/Icons/Animal.svg";

	private const string kPetIcon = "Media/Game/Icons/Pet.svg";

	private const string kHealthcareIcon = "Media/Game/Icons/Healthcare.svg";

	private const string kDeathcareIcon = "Media/Game/Icons/Deathcare.svg";

	private const string kPoliceIcon = "Media/Game/Icons/Police.svg";

	private const string kGarbageIcon = "Media/Game/Icons/Garbage.svg";

	private const string kFireIcon = "Media/Game/Icons/FireSafety.svg";

	private const string kPostIcon = "Media/Game/Icons/PostService.svg";

	private const string kDeliveryIcon = "Media/Game/Icons/DeliveryVan.svg";

	private PrefabSystem m_PrefabSystem;

	public string placeholderIcon => "Media/Placeholder.svg";

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	[CanBeNull]
	public static string GetIcon(PrefabBase prefab)
	{
		if (prefab.TryGet<UIObject>(out var component) && !string.IsNullOrEmpty(component.m_Icon))
		{
			return component.m_Icon;
		}
		return null;
	}

	[CanBeNull]
	public string GetGroupIcon(Entity prefabEntity)
	{
		if (base.EntityManager.TryGetComponent<UIObjectData>(prefabEntity, out var component) && component.m_Group != Entity.Null && m_PrefabSystem.TryGetPrefab<PrefabBase>(component.m_Group, out var prefab))
		{
			return GetIcon(prefab);
		}
		return null;
	}

	[CanBeNull]
	public string GetIconOrGroupIcon(Entity prefabEntity)
	{
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabEntity, out var prefab))
		{
			return GetIcon(prefab) ?? GetGroupIcon(prefabEntity);
		}
		return null;
	}

	[CanBeNull]
	public string GetThumbnail(Entity prefabEntity)
	{
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabEntity, out var prefab))
		{
			return GetThumbnail(prefab);
		}
		return null;
	}

	[CanBeNull]
	public static string GetThumbnail(PrefabBase prefab)
	{
		string icon = GetIcon(prefab);
		if (icon != null)
		{
			return icon;
		}
		if (GameManager.instance.configuration.noThumbnails)
		{
			return "Media/Placeholder.svg";
		}
		string text = $"{prefab.thumbnailUrl}?width={128}&height={128}";
		COSystemBase.baseLog.VerboseFormat("GetThumbnail - {0}", text);
		return text;
	}

	[CanBeNull]
	public string GetInstanceIcon(Entity instanceEntity)
	{
		return GetInstanceIcon(instanceEntity, base.EntityManager.GetComponentData<PrefabRef>(instanceEntity).m_Prefab);
	}

	[CanBeNull]
	public string GetInstanceIcon(Entity instanceEntity, Entity prefabEntity)
	{
		if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(prefabEntity, out var component))
		{
			string iconOrGroupIcon = GetIconOrGroupIcon(component.m_ZonePrefab);
			if (iconOrGroupIcon != null)
			{
				return iconOrGroupIcon;
			}
		}
		string iconOrGroupIcon2 = GetIconOrGroupIcon(prefabEntity);
		if (iconOrGroupIcon2 != null)
		{
			return iconOrGroupIcon2;
		}
		if (base.EntityManager.HasComponent<Citizen>(instanceEntity) && base.EntityManager.TryGetComponent<HouseholdMember>(instanceEntity, out var component2))
		{
			if (base.EntityManager.HasChunkComponent<CommuterHousehold>(component2.m_Household))
			{
				return "Media/Game/Icons/Commuter.svg";
			}
			if (base.EntityManager.HasComponent<TouristHousehold>(component2.m_Household))
			{
				return "Media/Game/Icons/Tourist.svg";
			}
			return "Media/Game/Icons/Citizen.svg";
		}
		if (base.EntityManager.HasComponent<Animal>(instanceEntity))
		{
			return "Media/Game/Icons/Animal.svg";
		}
		if (base.EntityManager.HasComponent<HouseholdPet>(instanceEntity))
		{
			return "Media/Game/Icons/Pet.svg";
		}
		if (base.EntityManager.HasComponent<AmbulanceData>(prefabEntity))
		{
			return "Media/Game/Icons/Healthcare.svg";
		}
		if (base.EntityManager.HasComponent<PoliceCarData>(prefabEntity))
		{
			return "Media/Game/Icons/Police.svg";
		}
		if (base.EntityManager.HasComponent<FireEngineData>(prefabEntity))
		{
			return "Media/Game/Icons/FireSafety.svg";
		}
		if (base.EntityManager.HasComponent<DeliveryTruckData>(prefabEntity))
		{
			return "Media/Game/Icons/DeliveryVan.svg";
		}
		if (base.EntityManager.HasComponent<PostVanData>(prefabEntity))
		{
			return "Media/Game/Icons/PostService.svg";
		}
		if (base.EntityManager.HasComponent<HearseData>(prefabEntity))
		{
			return "Media/Game/Icons/Deathcare.svg";
		}
		if (base.EntityManager.HasComponent<GarbageTruckData>(prefabEntity))
		{
			return "Media/Game/Icons/Garbage.svg";
		}
		if (base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(instanceEntity) && base.EntityManager.TryGetComponent<Owner>(instanceEntity, out var component3) && base.EntityManager.TryGetComponent<PrefabRef>(component3.m_Owner, out var component4))
		{
			instanceEntity = component3.m_Owner;
			prefabEntity = component4.m_Prefab;
		}
		if (base.EntityManager.TryGetComponent<ServiceObjectData>(prefabEntity, out var component5))
		{
			string iconOrGroupIcon3 = GetIconOrGroupIcon(component5.m_Service);
			if (iconOrGroupIcon3 != null)
			{
				return iconOrGroupIcon3;
			}
		}
		if (base.EntityManager.TryGetBuffer(instanceEntity, isReadOnly: true, out DynamicBuffer<AggregateElement> buffer) && buffer.Length != 0 && base.EntityManager.TryGetComponent<PrefabRef>(buffer[0].m_Edge, out var component6))
		{
			string iconOrGroupIcon4 = GetIconOrGroupIcon(component6.m_Prefab);
			if (iconOrGroupIcon4 != null)
			{
				return iconOrGroupIcon4;
			}
		}
		if (base.EntityManager.TryGetComponent<Owner>(instanceEntity, out var component7) && base.EntityManager.TryGetComponent<PropertyRenter>(component7.m_Owner, out var component8) && base.EntityManager.TryGetComponent<PrefabRef>(component8.m_Property, out var component9))
		{
			SpawnableBuildingData component10;
			Entity prefabEntity2 = (base.EntityManager.TryGetComponent<SpawnableBuildingData>(component9.m_Prefab, out component10) ? component10.m_ZonePrefab : component9.m_Prefab);
			string iconOrGroupIcon5 = GetIconOrGroupIcon(prefabEntity2);
			if (iconOrGroupIcon5 != null)
			{
				return iconOrGroupIcon5;
			}
		}
		return null;
	}

	[Preserve]
	public ImageSystem()
	{
	}
}
