using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class EditorAssetCategorySystem : GameSystemBase
{
	public interface IEditorAssetCategoryFilter
	{
		bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem);
	}

	public class ServiceTypeFilter : IEditorAssetCategoryFilter
	{
		private Entity m_Service;

		public ServiceTypeFilter(Entity service)
		{
			m_Service = service;
		}

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem _)
		{
			if (entityManager.TryGetComponent<ServiceObjectData>(prefab, out var component))
			{
				return component.m_Service == m_Service;
			}
			if (entityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<ServiceUpgradeBuilding> buffer))
			{
				foreach (ServiceUpgradeBuilding item in buffer)
				{
					if (entityManager.TryGetComponent<ServiceObjectData>(item.m_Building, out component) && component.m_Service == m_Service)
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}
	}

	public class ZoneTypeFilter : IEditorAssetCategoryFilter
	{
		private Entity m_Zone;

		public ZoneTypeFilter(Entity zone)
		{
			m_Zone = zone;
		}

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem _)
		{
			if (entityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component))
			{
				return component.m_ZonePrefab == m_Zone;
			}
			if (entityManager.TryGetComponent<PlaceholderBuildingData>(prefab, out var component2))
			{
				return component2.m_ZonePrefab == m_Zone;
			}
			return true;
		}
	}

	public class PassengerCountFilter : IEditorAssetCategoryFilter
	{
		public enum FilterType
		{
			Equals,
			NotEquals,
			MoreThan,
			LessThan
		}

		public FilterType m_Type;

		public int m_Count;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem _)
		{
			if (entityManager.TryGetComponent<PersonalCarData>(prefab, out var component))
			{
				return Check(component);
			}
			return true;
		}

		private bool Check(PersonalCarData data)
		{
			return m_Type switch
			{
				FilterType.Equals => data.m_PassengerCapacity == m_Count, 
				FilterType.NotEquals => data.m_PassengerCapacity != m_Count, 
				FilterType.MoreThan => data.m_PassengerCapacity > m_Count, 
				FilterType.LessThan => data.m_PassengerCapacity < m_Count, 
				_ => false, 
			};
		}
	}

	public class PublicTransportTypeFilter : IEditorAssetCategoryFilter
	{
		public TransportType m_TransportType;

		public PublicTransportPurpose m_Purpose;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem)
		{
			if (entityManager.TryGetComponent<PublicTransportVehicleData>(prefab, out var component))
			{
				if (m_TransportType == TransportType.None || component.m_TransportType == m_TransportType)
				{
					if (m_Purpose != 0)
					{
						return (component.m_PurposeMask & m_Purpose) != 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public class MaintenanceTypeFilter : IEditorAssetCategoryFilter
	{
		public MaintenanceType m_Type;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem)
		{
			if (entityManager.TryGetComponent<MaintenanceVehicleData>(prefab, out var component))
			{
				return (component.m_MaintenanceType & m_Type) != 0;
			}
			return true;
		}
	}

	public class ThemeFilter : IEditorAssetCategoryFilter
	{
		public Entity m_Theme;

		public bool m_DefaultResult = true;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem)
		{
			if (!prefabSystem.TryGetPrefab<PrefabBase>(prefab, out var prefab2))
			{
				return false;
			}
			ThemeObject component = prefab2.GetComponent<ThemeObject>();
			if (component == null)
			{
				return m_DefaultResult;
			}
			return prefabSystem.GetEntity(component.m_Theme) == m_Theme;
		}
	}

	public class TrackTypeFilter : IEditorAssetCategoryFilter
	{
		public TrackTypes m_TrackType;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem)
		{
			if (!prefabSystem.TryGetPrefab<TrackPrefab>(prefab, out var prefab2))
			{
				return false;
			}
			if (prefab2 != null)
			{
				return (prefab2.m_TrackType & m_TrackType) != 0;
			}
			return true;
		}
	}

	public class SignatureBuildingFilter : IEditorAssetCategoryFilter
	{
		public AreaType m_AreaType;

		public bool m_Office;

		public Entity m_Theme;

		public bool Contains(Entity prefab, EntityManager entityManager, PrefabSystem prefabSystem)
		{
			if (!prefabSystem.TryGetPrefab<PrefabBase>(prefab, out var prefab2))
			{
				return false;
			}
			SignatureBuilding component = prefab2.GetComponent<SignatureBuilding>();
			if (component != null)
			{
				ThemeObject component2 = prefab2.GetComponent<ThemeObject>();
				Entity entity = Entity.Null;
				if (component2 != null)
				{
					entity = prefabSystem.GetEntity(component2.m_Theme);
				}
				if (m_Theme == entity && component.m_ZoneType.m_AreaType == m_AreaType)
				{
					return component.m_ZoneType.m_Office == m_Office;
				}
				return false;
			}
			return true;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private List<EditorAssetCategory> m_Categories = new List<EditorAssetCategory>();

	private Dictionary<string, EditorAssetCategory> m_PathMap = new Dictionary<string, EditorAssetCategory>();

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_ServiceQuery;

	private EntityQuery m_ZoneQuery;

	private EntityQuery m_ThemeQuery;

	private EntityQuery m_Overrides;

	private EntityQuery m_PrefabModificationQuery;

	private bool m_Dirty = true;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ServiceQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceData>());
		m_ZoneQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>());
		m_ThemeQuery = GetEntityQuery(ComponentType.ReadOnly<ThemeData>());
		m_Overrides = GetEntityQuery(ComponentType.ReadOnly<EditorAssetCategoryOverrideData>());
		m_PrefabModificationQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<PrefabData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_PrefabModificationQuery.IsEmptyIgnoreFilter)
		{
			m_Dirty = true;
		}
	}

	public IEnumerable<EditorAssetCategory> GetCategories(bool ignoreEmpty = true)
	{
		if (m_Dirty)
		{
			GenerateCategories();
		}
		foreach (var item in GetCategoriesImpl(m_Categories, 0, ignoreEmpty))
		{
			yield return item.Item1;
		}
	}

	public IEnumerable<HierarchyItem<EditorAssetCategory>> GetHierarchy(bool ignoreEmpty = true)
	{
		if (m_Dirty)
		{
			GenerateCategories();
		}
		foreach (var (editorAssetCategory, level) in GetCategoriesImpl(m_Categories, 0, ignoreEmpty))
		{
			yield return editorAssetCategory.ToHierarchyItem(level);
		}
	}

	private void AddCategory(EditorAssetCategory category, EditorAssetCategory parent = null)
	{
		string text = category.id.Trim('/');
		category.path = ((parent != null) ? (parent.path + "/" + text) : text);
		if (parent == null)
		{
			m_Categories.Add(category);
		}
		else
		{
			parent.AddSubCategory(category);
		}
		m_PathMap[category.path] = category;
	}

	private void ClearCategories()
	{
		m_Categories.Clear();
		m_PathMap.Clear();
	}

	private IEnumerable<(EditorAssetCategory, int)> GetCategoriesImpl(IEnumerable<EditorAssetCategory> categories, int level, bool ignoreEmpty)
	{
		foreach (EditorAssetCategory category in categories)
		{
			if (ignoreEmpty && category.IsEmpty(base.EntityManager, m_PrefabSystem, InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef)))
			{
				continue;
			}
			yield return (category, level);
			foreach (var (item, item2) in GetCategoriesImpl(category.subCategories, level + 1, ignoreEmpty))
			{
				yield return (item, item2);
			}
		}
	}

	private void GenerateCategories()
	{
		ClearCategories();
		GenerateBuildingCategories();
		GenerateVehicleCategories();
		GeneratePropCategories();
		GenerateFoliageCategories();
		GenerateCharacterCategories();
		GenerateAreaCategories();
		GenerateSurfaceCategories();
		GenerateBridgeCategory();
		GenerateRoadCategory();
		GenerateTrackCategories();
		GenerateEffectCategories();
		GenerateLocationCategories();
		AddOverrides();
		m_Dirty = false;
	}

	private void GenerateBuildingCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Buildings",
			entityQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<BuildingData>(),
					ComponentType.ReadOnly<BuildingExtensionData>()
				}
			}),
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		GenerateServiceBuildingCategories(editorAssetCategory);
		GenerateSpawnableBuildingCategories(editorAssetCategory);
		GenerateMiscBuildingCategory(editorAssetCategory);
	}

	private void GenerateServiceBuildingCategories(EditorAssetCategory parent)
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Services"
		};
		AddCategory(editorAssetCategory, parent);
		NativeArray<Entity> nativeArray = m_ServiceQuery.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (m_PrefabSystem.TryGetPrefab<PrefabBase>(nativeArray[i], out var prefab))
			{
				EditorAssetCategory category = new EditorAssetCategory
				{
					id = prefab.name,
					entityQuery = GetEntityQuery(new EntityQueryDesc
					{
						All = new ComponentType[2]
						{
							ComponentType.ReadOnly<BuildingData>(),
							ComponentType.ReadOnly<ServiceObjectData>()
						},
						None = new ComponentType[1] { ComponentType.ReadOnly<TrafficSpawnerData>() }
					}, new EntityQueryDesc
					{
						All = new ComponentType[1] { ComponentType.ReadOnly<ServiceUpgradeBuilding>() },
						Any = new ComponentType[2]
						{
							ComponentType.ReadOnly<BuildingData>(),
							ComponentType.ReadOnly<BuildingExtensionData>()
						},
						None = new ComponentType[1] { ComponentType.ReadOnly<TrafficSpawnerData>() }
					}),
					filter = new ServiceTypeFilter(nativeArray[i]),
					icon = ImageSystem.GetIcon(prefab)
				};
				AddCategory(category, editorAssetCategory);
			}
		}
	}

	private void GenerateSpawnableBuildingCategories(EditorAssetCategory parent)
	{
		GenerateZoneCategories(AreaType.Residential, office: false, parent);
		GenerateZoneCategories(AreaType.Commercial, office: false, parent);
		GenerateZoneCategories(AreaType.Industrial, office: false, parent);
		GenerateZoneCategories(AreaType.Industrial, office: true, parent);
	}

	private void GenerateZoneCategories(AreaType areaType, bool office, EditorAssetCategory parent)
	{
		string id = (office ? "Office" : areaType.ToString());
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = id
		};
		AddCategory(editorAssetCategory, parent);
		NativeArray<Entity> nativeArray = m_ZoneQuery.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (m_PrefabSystem.TryGetPrefab<ZonePrefab>(nativeArray[i], out var prefab) && prefab.m_AreaType == areaType && prefab.m_Office == office)
			{
				EditorAssetCategory category = new EditorAssetCategory
				{
					id = prefab.name,
					entityQuery = GetEntityQuery(new EntityQueryDesc
					{
						All = new ComponentType[1] { ComponentType.ReadOnly<BuildingData>() },
						Any = new ComponentType[2]
						{
							ComponentType.ReadOnly<SpawnableBuildingData>(),
							ComponentType.ReadOnly<PlaceholderBuildingData>()
						}
					}),
					filter = new ZoneTypeFilter(nativeArray[i]),
					icon = ImageSystem.GetIcon(prefab)
				};
				AddCategory(category, editorAssetCategory);
			}
		}
		if (areaType == AreaType.Industrial && !office)
		{
			EditorAssetCategory category2 = new EditorAssetCategory
			{
				id = "Extractors",
				entityQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<ExtractorFacilityData>())
			};
			AddCategory(category2, editorAssetCategory);
		}
		NativeArray<Entity> nativeArray2 = m_ThemeQuery.ToEntityArray(Allocator.Temp);
		for (int j = 0; j < nativeArray2.Length; j++)
		{
			if (m_PrefabSystem.TryGetPrefab<ThemePrefab>(nativeArray2[j], out var prefab2))
			{
				EditorAssetCategory category3 = new EditorAssetCategory
				{
					id = prefab2.assetPrefix + " Signature",
					entityQuery = GetEntityQuery(ComponentType.ReadOnly<SignatureBuildingData>()),
					filter = new SignatureBuildingFilter
					{
						m_AreaType = areaType,
						m_Office = office,
						m_Theme = nativeArray2[j]
					}
				};
				AddCategory(category3, editorAssetCategory);
			}
		}
		EditorAssetCategory category4 = new EditorAssetCategory
		{
			id = "Signature",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<SignatureBuildingData>()),
			filter = new SignatureBuildingFilter
			{
				m_AreaType = areaType,
				m_Office = office,
				m_Theme = Entity.Null
			}
		};
		AddCategory(category4, editorAssetCategory);
		nativeArray.Dispose();
	}

	private void GenerateMiscBuildingCategory(EditorAssetCategory parent)
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Misc",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.Exclude<ServiceObjectData>(), ComponentType.Exclude<SpawnableBuildingData>(), ComponentType.Exclude<SignatureBuildingData>(), ComponentType.Exclude<ServiceUpgradeBuilding>(), ComponentType.Exclude<ExtractorFacilityData>())
		};
		AddCategory(category, parent);
	}

	private void GenerateVehicleCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Vehicles",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleData>()),
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		GenerateResidentialVehicleCategory(editorAssetCategory);
		GenerateIndustrialVehicleCategory(editorAssetCategory);
		GenerateServiceVehicleCategories(editorAssetCategory);
	}

	private void GenerateResidentialVehicleCategory(EditorAssetCategory parent)
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Residential"
		};
		AddCategory(editorAssetCategory, parent);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Cars",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleData>(), ComponentType.ReadOnly<PersonalCarData>()),
			filter = new PassengerCountFilter
			{
				m_Type = PassengerCountFilter.FilterType.NotEquals,
				m_Count = 1
			},
			icon = "Media/Game/Icons/GenericVehicle.svg"
		};
		AddCategory(category, editorAssetCategory);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Bikes",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleData>(), ComponentType.ReadOnly<PersonalCarData>()),
			filter = new PassengerCountFilter
			{
				m_Type = PassengerCountFilter.FilterType.Equals,
				m_Count = 1
			},
			icon = "Media/Game/Icons/Bicycle.svg"
		};
		AddCategory(category2, editorAssetCategory);
	}

	private void GenerateServiceVehicleCategories(EditorAssetCategory parent)
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Services"
		};
		AddCategory(editorAssetCategory, parent);
		GeneratePublicTransportVehicleCategory(TransportType.Bus, editorAssetCategory);
		GeneratePublicTransportVehicleCategory(TransportType.Taxi, editorAssetCategory);
		GeneratePublicTransportVehicleCategory(TransportType.Tram, editorAssetCategory);
		GeneratePublicTransportVehicleCategory(TransportType.Train, editorAssetCategory);
		GeneratePublicTransportVehicleCategory(TransportType.Subway, editorAssetCategory);
		GeneratePublicTransportVehicleCategory(TransportType.Ship, editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Aircraft",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<AircraftData>(), ComponentType.Exclude<CargoTransportVehicleData>()),
			icon = "Media/Game/Icons/airplane.svg"
		};
		AddCategory(category, editorAssetCategory);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Healthcare",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<AmbulanceData>()),
			icon = "Media/Game/Icons/Healthcare.svg"
		};
		AddCategory(category2, editorAssetCategory);
		EditorAssetCategory category3 = new EditorAssetCategory
		{
			id = "Police",
			entityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<VehicleData>() },
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<PoliceCarData>(),
					ComponentType.ReadOnly<PublicTransportVehicleData>()
				}
			}),
			icon = "Media/Game/Icons/Police.svg",
			filter = new PublicTransportTypeFilter
			{
				m_TransportType = TransportType.None,
				m_Purpose = PublicTransportPurpose.PrisonerTransport
			}
		};
		AddCategory(category3, editorAssetCategory);
		EditorAssetCategory category4 = new EditorAssetCategory
		{
			id = "Deathcare",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<HearseData>()),
			icon = "Media/Game/Icons/Deathcare.svg"
		};
		AddCategory(category4, editorAssetCategory);
		EditorAssetCategory category5 = new EditorAssetCategory
		{
			id = "FireRescue",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<FireEngineData>()),
			icon = "Media/Game/Icons/FireSafety.svg"
		};
		AddCategory(category5, editorAssetCategory);
		EditorAssetCategory category6 = new EditorAssetCategory
		{
			id = "Garbage",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageTruckData>()),
			icon = "Media/Game/Icons/Garbage.svg"
		};
		AddCategory(category6, editorAssetCategory);
		EditorAssetCategory category7 = new EditorAssetCategory
		{
			id = "Parks",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<MaintenanceVehicleData>()),
			icon = "Media/Game/Icons/ParksAndRecreation.svg",
			filter = new MaintenanceTypeFilter
			{
				m_Type = MaintenanceType.Park
			}
		};
		AddCategory(category7, editorAssetCategory);
		EditorAssetCategory category8 = new EditorAssetCategory
		{
			id = "Roads",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<MaintenanceVehicleData>()),
			icon = "Media/Game/Icons/Roads.svg",
			filter = new MaintenanceTypeFilter
			{
				m_Type = (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)
			}
		};
		AddCategory(category8, editorAssetCategory);
	}

	private void GeneratePublicTransportVehicleCategory(TransportType transportType, EditorAssetCategory parent)
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = transportType.ToString(),
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleData>(), ComponentType.ReadOnly<PublicTransportVehicleData>()),
			icon = $"Media/Game/Icons/{transportType}.svg",
			filter = new PublicTransportTypeFilter
			{
				m_TransportType = transportType,
				m_Purpose = PublicTransportPurpose.TransportLine
			}
		};
		AddCategory(category, parent);
	}

	private void GenerateIndustrialVehicleCategory(EditorAssetCategory parent)
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Industrial",
			entityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<VehicleData>() },
				Any = new ComponentType[3]
				{
					ComponentType.ReadOnly<CargoTransportVehicleData>(),
					ComponentType.ReadOnly<DeliveryTruckData>(),
					ComponentType.ReadOnly<WorkVehicleData>()
				}
			}),
			icon = "Media/Game/Icons/ZoneIndustrial.svg"
		};
		AddCategory(category, parent);
	}

	private void GeneratePropCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Props",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<StaticObjectData>(), ComponentType.Exclude<BuildingData>(), ComponentType.Exclude<NetObjectData>(), ComponentType.Exclude<BuildingExtensionData>(), ComponentType.Exclude<PillarData>(), ComponentType.Exclude<PlantData>(), ComponentType.Exclude<BrandObjectData>()),
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Brand Graphics",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<StaticObjectData>(), ComponentType.ReadOnly<BrandObjectData>(), ComponentType.Exclude<BuildingData>(), ComponentType.Exclude<NetObjectData>(), ComponentType.Exclude<BuildingExtensionData>(), ComponentType.Exclude<PillarData>(), ComponentType.Exclude<PlantData>())
		};
		AddCategory(category, editorAssetCategory);
	}

	private void GenerateFoliageCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Foliage",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<PlantData>()),
			icon = "Media/Game/Icons/Vegetation.svg",
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		GenerateTreeCategories(editorAssetCategory);
		GenerateBushCategories(editorAssetCategory);
	}

	private void GenerateTreeCategories(EditorAssetCategory parent)
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Trees"
		};
		AddCategory(editorAssetCategory, parent);
		foreach (Entity item in m_ThemeQuery.ToEntityArray(Allocator.Temp))
		{
			if (m_PrefabSystem.TryGetPrefab<ThemePrefab>(item, out var prefab))
			{
				string icon = prefab.GetComponent<UIObject>()?.m_Icon;
				EditorAssetCategory category = new EditorAssetCategory
				{
					id = prefab.assetPrefix,
					entityQuery = GetEntityQuery(ComponentType.ReadOnly<TreeData>()),
					icon = icon,
					filter = new ThemeFilter
					{
						m_Theme = item,
						m_DefaultResult = false
					}
				};
				AddCategory(category, editorAssetCategory);
			}
		}
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Shared",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<TreeData>()),
			filter = new ThemeFilter
			{
				m_Theme = Entity.Null,
				m_DefaultResult = true
			}
		};
		AddCategory(category2, editorAssetCategory);
	}

	private void GenerateBushCategories(EditorAssetCategory parent)
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Bushes",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<PlantData>(), ComponentType.Exclude<TreeData>())
		};
		AddCategory(category, parent);
	}

	private void GenerateRoadCategory()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Roads"
		};
		AddCategory(editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Roads",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<RoadData>(), ComponentType.Exclude<BridgeData>()),
			icon = "Media/Game/Icons/Roads.svg"
		};
		AddCategory(category, editorAssetCategory);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Intersections",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<AssetStampData>(), ComponentType.ReadOnly<Game.Prefabs.SubNet>())
		};
		AddCategory(category2, editorAssetCategory);
	}

	private void GenerateTrackCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Tracks",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<TrackData>()),
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		GenerateTrackTypeCategory(TrackTypes.Train, editorAssetCategory);
		GenerateTrackTypeCategory(TrackTypes.Tram, editorAssetCategory);
		GenerateTrackTypeCategory(TrackTypes.Subway, editorAssetCategory);
	}

	private void GenerateTrackTypeCategory(TrackTypes trackTypes, EditorAssetCategory parent)
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = trackTypes.ToString(),
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<TrackData>()),
			filter = new TrackTypeFilter
			{
				m_TrackType = trackTypes
			}
		};
		AddCategory(category, parent);
	}

	private void GenerateEffectCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Effects",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<EffectData>())
		};
		AddCategory(editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "VFX",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<EffectData>(), ComponentType.ReadOnly<VFXData>())
		};
		AddCategory(category, editorAssetCategory);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Audio",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<EffectData>(), ComponentType.ReadOnly<AudioEffectData>())
		};
		AddCategory(category2, editorAssetCategory);
		EditorAssetCategory category3 = new EditorAssetCategory
		{
			id = "Lights",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<EffectData>(), ComponentType.ReadOnly<LightEffectData>())
		};
		AddCategory(category3, editorAssetCategory);
	}

	private void GenerateLocationCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Locations"
		};
		AddCategory(editorAssetCategory);
		EditorAssetCategory editorAssetCategory2 = new EditorAssetCategory
		{
			id = "Spawners"
		};
		AddCategory(editorAssetCategory2, editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Animals",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureSpawnData>())
		};
		AddCategory(category, editorAssetCategory2);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Vehicles",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<TrafficSpawnerData>())
		};
		AddCategory(category2, editorAssetCategory2);
	}

	private void GenerateBridgeCategory()
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Bridges",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<BridgeData>()),
			icon = "Media/Game/Icons/CableStayed.svg"
		};
		AddCategory(category);
	}

	private void GenerateCharacterCategories()
	{
		EditorAssetCategory editorAssetCategory = new EditorAssetCategory
		{
			id = "Characters",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureData>()),
			includeChildCategories = false
		};
		AddCategory(editorAssetCategory);
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "People",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<HumanData>())
		};
		AddCategory(category, editorAssetCategory);
		EditorAssetCategory editorAssetCategory2 = new EditorAssetCategory
		{
			id = "Animals",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<AnimalData>())
		};
		AddCategory(editorAssetCategory2, editorAssetCategory);
		EditorAssetCategory category2 = new EditorAssetCategory
		{
			id = "Pets",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<PetData>())
		};
		AddCategory(category2, editorAssetCategory2);
		EditorAssetCategory category3 = new EditorAssetCategory
		{
			id = "Livestock",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<DomesticatedData>())
		};
		AddCategory(category3, editorAssetCategory2);
		EditorAssetCategory category4 = new EditorAssetCategory
		{
			id = "Wildlife",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<WildlifeData>())
		};
		AddCategory(category4, editorAssetCategory2);
	}

	private void GenerateAreaCategories()
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Areas",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<AreaData>(), ComponentType.Exclude<SurfaceData>())
		};
		AddCategory(category);
	}

	private void GenerateSurfaceCategories()
	{
		EditorAssetCategory category = new EditorAssetCategory
		{
			id = "Surfaces",
			entityQuery = GetEntityQuery(ComponentType.ReadOnly<SurfaceData>())
		};
		AddCategory(category);
	}

	private void AddOverrides()
	{
		NativeArray<Entity> nativeArray = m_Overrides.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(nativeArray[i], out var prefab))
			{
				continue;
			}
			EditorAssetCategoryOverride component = prefab.GetComponent<EditorAssetCategoryOverride>();
			if (component.m_IncludeCategories != null)
			{
				for (int j = 0; j < component.m_IncludeCategories.Length; j++)
				{
					string text = component.m_IncludeCategories[j];
					if (m_PathMap.TryGetValue(text, out var value))
					{
						value.AddEntity(nativeArray[i]);
					}
					else
					{
						CreateCategory(text).AddEntity(nativeArray[i]);
					}
				}
			}
			if (component.m_ExcludeCategories == null)
			{
				continue;
			}
			for (int k = 0; k < component.m_ExcludeCategories.Length; k++)
			{
				string key = component.m_ExcludeCategories[k];
				if (m_PathMap.TryGetValue(key, out var value2))
				{
					value2.AddExclusion(nativeArray[i]);
				}
			}
		}
	}

	private EditorAssetCategory CreateCategory(string path)
	{
		string[] array = path.Split("/");
		EditorAssetCategory editorAssetCategory = null;
		string text = null;
		for (int i = 0; i < array.Length; i++)
		{
			text = ((text != null) ? string.Join("/", text, array[i]) : array[i]);
			if (m_PathMap.TryGetValue(text, out var value))
			{
				editorAssetCategory = value;
				continue;
			}
			EditorAssetCategory editorAssetCategory2 = new EditorAssetCategory
			{
				id = array[i]
			};
			AddCategory(editorAssetCategory2, editorAssetCategory);
			editorAssetCategory = editorAssetCategory2;
		}
		return editorAssetCategory;
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
	public EditorAssetCategorySystem()
	{
	}
}
