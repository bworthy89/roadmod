using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Modding;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Game.Serialization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.City;

[CompilerGenerated]
public class CityConfigurationSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	private string m_LoadedCityName;

	private NativeList<Entity> m_RequiredContent;

	private bool m_LoadedLeftHandTraffic;

	private bool m_LoadedNaturalDisasters;

	private bool m_UnlockAll;

	private bool m_LoadedUnlockAll;

	private bool m_UnlimitedMoney;

	private bool m_LoadedUnlimitedMoney;

	private bool m_UnlockMapTiles;

	private bool m_LoadedUnlockMapTiles;

	private PrefabSystem m_PrefabSystem;

	private UnlockAllSystem m_UnlockAllSystem;

	private FlipTrafficHandednessSystem m_FlipTrafficHandednessSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private EntityQuery m_ThemeQuery;

	private EntityQuery m_SubLaneQuery;

	public float3 m_CameraPivot;

	public float2 m_CameraAngle;

	public float m_CameraZoom;

	public Entity m_CameraFollow;

	private static readonly float3 kDefaultCameraPivot = new float3(0f, 0f, 0f);

	private static readonly float2 kDefaultCameraAngle = new float2(0f, 45f);

	private static readonly float kDefaultCameraZoom = 250f;

	private static readonly Entity kDefaultCameraFollow = Entity.Null;

	public string cityName { get; set; }

	public string overrideCityName { get; set; }

	[CanBeNull]
	public string overrideThemeName { get; set; }

	public Entity defaultTheme { get; set; }

	public Entity loadedDefaultTheme { get; set; }

	public ref NativeList<Entity> requiredContent => ref m_RequiredContent;

	public bool leftHandTraffic { get; set; }

	public bool overrideLeftHandTraffic { get; set; }

	public bool naturalDisasters { get; set; }

	public bool overrideNaturalDisasters { get; set; }

	public bool unlockAll
	{
		get
		{
			if (!m_LoadedUnlockAll)
			{
				return m_UnlockAll;
			}
			return true;
		}
		set
		{
			m_UnlockAll = value;
		}
	}

	public bool overrideUnlockAll { get; set; }

	public bool unlimitedMoney
	{
		get
		{
			if (!m_LoadedUnlimitedMoney)
			{
				return m_UnlimitedMoney;
			}
			return true;
		}
		set
		{
			m_UnlimitedMoney = value;
		}
	}

	public bool overrideUnlimitedMoney { get; set; }

	public bool unlockMapTiles
	{
		get
		{
			if (!m_LoadedUnlockMapTiles)
			{
				return m_UnlockMapTiles;
			}
			return true;
		}
		set
		{
			m_UnlockMapTiles = value;
		}
	}

	public bool overrideUnlockMapTiles { get; set; }

	public bool overrideLoadedOptions { get; set; }

	public HashSet<string> usedMods { get; private set; } = new HashSet<string>();

	public void PatchReferences(ref PrefabReferences references)
	{
		defaultTheme = references.Check(base.EntityManager, defaultTheme);
		loadedDefaultTheme = references.Check(base.EntityManager, loadedDefaultTheme);
		for (int i = 0; i < m_RequiredContent.Length; i++)
		{
			m_RequiredContent[i] = references.Check(base.EntityManager, m_RequiredContent[i]);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UnlockAllSystem = base.World.GetOrCreateSystemManaged<UnlockAllSystem>();
		m_FlipTrafficHandednessSystem = base.World.GetOrCreateSystemManaged<FlipTrafficHandednessSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_RequiredContent = new NativeList<Entity>(0, Allocator.Persistent);
		m_ThemeQuery = GetEntityQuery(ComponentType.ReadOnly<ThemeData>(), ComponentType.Exclude<Locked>());
		m_SubLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.SubLane>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_RequiredContent.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		cityName = overrideCityName;
		leftHandTraffic = overrideLeftHandTraffic;
		naturalDisasters = overrideNaturalDisasters;
		unlockAll = overrideUnlockAll;
		unlimitedMoney = overrideUnlimitedMoney;
		unlockMapTiles = overrideUnlockMapTiles;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_UnlockAllSystem.Enabled = unlockAll;
	}

	public void PostDeserialize(Context context)
	{
		if (defaultTheme == Entity.Null || !string.IsNullOrEmpty(overrideThemeName))
		{
			NativeArray<Entity> nativeArray = m_ThemeQuery.ToEntityArray(Allocator.TempJob);
			try
			{
				if (defaultTheme == Entity.Null && nativeArray.Length > 0)
				{
					defaultTheme = nativeArray[0];
				}
				if (!string.IsNullOrEmpty(overrideThemeName))
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						if (m_PrefabSystem.GetPrefab<ThemePrefab>(nativeArray[i]).name == overrideThemeName)
						{
							defaultTheme = nativeArray[i];
							break;
						}
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
				overrideThemeName = null;
			}
		}
		if (leftHandTraffic != m_LoadedLeftHandTraffic || defaultTheme != loadedDefaultTheme)
		{
			base.EntityManager.AddComponent<Updated>(m_SubLaneQuery);
		}
		if (leftHandTraffic != m_LoadedLeftHandTraffic)
		{
			m_FlipTrafficHandednessSystem.Update();
		}
		if (base.EntityManager.Exists(m_CameraFollow))
		{
			if (m_CameraUpdateSystem.orbitCameraController != null)
			{
				m_CameraUpdateSystem.orbitCameraController.pivot = m_CameraPivot;
				m_CameraUpdateSystem.orbitCameraController.rotation = new Vector3(m_CameraAngle.y, m_CameraAngle.x, 0f);
				m_CameraUpdateSystem.orbitCameraController.zoom = m_CameraZoom;
				m_CameraUpdateSystem.orbitCameraController.followedEntity = m_CameraFollow;
				m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
			}
		}
		else if (m_CameraUpdateSystem.gamePlayController != null)
		{
			m_CameraUpdateSystem.gamePlayController.pivot = m_CameraPivot;
			m_CameraUpdateSystem.gamePlayController.rotation = new Vector3(m_CameraAngle.y, m_CameraAngle.x, 0f);
			m_CameraUpdateSystem.gamePlayController.zoom = m_CameraZoom;
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.gamePlayController;
		}
		string[] modsEnabled = ModManager.GetModsEnabled();
		if (modsEnabled != null)
		{
			string[] array = modsEnabled;
			foreach (string item in array)
			{
				usedMods.Add(item);
			}
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		string value = cityName;
		writer.Write(value);
		Entity value2 = defaultTheme;
		writer.Write(value2);
		NativeList<Entity> value3 = m_RequiredContent;
		writer.Write(value3);
		bool value4 = leftHandTraffic;
		writer.Write(value4);
		bool value5 = naturalDisasters;
		writer.Write(value5);
		if (writer.context.purpose == Purpose.SaveMap)
		{
			float3 value6 = m_CameraPivot;
			writer.Write(value6);
			float2 value7 = m_CameraAngle;
			writer.Write(value7);
			float value8 = m_CameraZoom;
			writer.Write(value8);
			Entity value9 = kDefaultCameraFollow;
			writer.Write(value9);
		}
		else if (m_CameraUpdateSystem.activeCameraController != null)
		{
			float3 value10 = m_CameraUpdateSystem.activeCameraController.pivot;
			writer.Write(value10);
			Vector3 rotation = m_CameraUpdateSystem.activeCameraController.rotation;
			float2 value11 = new float2(rotation.y, rotation.x);
			writer.Write(value11);
			float zoom = m_CameraUpdateSystem.activeCameraController.zoom;
			writer.Write(zoom);
			if (m_CameraUpdateSystem.activeCameraController == m_CameraUpdateSystem.orbitCameraController)
			{
				Entity followedEntity = m_CameraUpdateSystem.orbitCameraController.followedEntity;
				writer.Write(followedEntity);
			}
			else
			{
				Entity value12 = kDefaultCameraFollow;
				writer.Write(value12);
			}
		}
		else
		{
			float3 value13 = kDefaultCameraPivot;
			writer.Write(value13);
			float2 value14 = kDefaultCameraAngle;
			writer.Write(value14);
			float value15 = kDefaultCameraZoom;
			writer.Write(value15);
			Entity value16 = kDefaultCameraFollow;
			writer.Write(value16);
		}
		bool value17 = m_UnlimitedMoney;
		writer.Write(value17);
		bool value18 = m_UnlockAll;
		writer.Write(value18);
		bool value19 = m_UnlockMapTiles;
		writer.Write(value19);
		if (writer.context.purpose == Purpose.SaveGame)
		{
			int count = usedMods.Count;
			writer.Write(count);
			{
				foreach (string usedMod in usedMods)
				{
					writer.Write(usedMod);
				}
				return;
			}
		}
		writer.Write(0);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.cityNameInConfig)
		{
			ref string value = ref m_LoadedCityName;
			reader.Read(out value);
		}
		else
		{
			m_LoadedCityName = "";
		}
		reader.Read(out Entity value2);
		loadedDefaultTheme = value2;
		if (reader.context.format.Has(FormatTags.ContentPrefabInCityConfiguration))
		{
			NativeList<Entity> value3 = m_RequiredContent;
			reader.Read(value3);
		}
		else
		{
			m_RequiredContent.ResizeUninitialized(0);
		}
		if (reader.context.version >= Version.leftHandTrafficOption)
		{
			ref bool value4 = ref m_LoadedLeftHandTraffic;
			reader.Read(out value4);
		}
		else
		{
			m_LoadedLeftHandTraffic = false;
		}
		if (reader.context.version >= Version.naturalDisasterOption)
		{
			ref bool value5 = ref m_LoadedNaturalDisasters;
			reader.Read(out value5);
		}
		else
		{
			m_LoadedNaturalDisasters = false;
		}
		if (reader.context.version >= Version.cameraPosition)
		{
			ref float3 value6 = ref m_CameraPivot;
			reader.Read(out value6);
			ref float2 value7 = ref m_CameraAngle;
			reader.Read(out value7);
			ref float value8 = ref m_CameraZoom;
			reader.Read(out value8);
			ref Entity value9 = ref m_CameraFollow;
			reader.Read(out value9);
		}
		else
		{
			ResetCameraProperties();
		}
		if (reader.context.version >= Version.unlimitedMoneyAndUnlockAllOptions)
		{
			ref bool value10 = ref m_LoadedUnlimitedMoney;
			reader.Read(out value10);
			ref bool value11 = ref m_LoadedUnlockAll;
			reader.Read(out value11);
		}
		if (reader.context.version >= Version.unlockMapTilesOption)
		{
			ref bool value12 = ref m_LoadedUnlockMapTiles;
			reader.Read(out value12);
		}
		else
		{
			m_LoadedUnlockMapTiles = false;
		}
		usedMods.Clear();
		if (reader.context.version >= Version.saveGameUsedMods)
		{
			reader.Read(out int value13);
			usedMods.EnsureCapacity(value13);
			for (int i = 0; i < value13; i++)
			{
				reader.Read(out string value14);
				usedMods.Add(value14);
			}
		}
		if (!overrideLoadedOptions)
		{
			cityName = m_LoadedCityName;
			naturalDisasters = m_LoadedNaturalDisasters;
			defaultTheme = loadedDefaultTheme;
			leftHandTraffic = m_LoadedLeftHandTraffic;
			unlimitedMoney = m_LoadedUnlimitedMoney;
			unlockAll = m_LoadedUnlockAll;
			unlockMapTiles = m_LoadedUnlockMapTiles;
		}
		else if (reader.context.purpose == Purpose.LoadGame)
		{
			defaultTheme = loadedDefaultTheme;
			leftHandTraffic = m_LoadedLeftHandTraffic;
		}
		overrideLoadedOptions = false;
	}

	private void ResetCameraProperties()
	{
		m_CameraPivot = kDefaultCameraPivot;
		m_CameraAngle = kDefaultCameraAngle;
		m_CameraZoom = kDefaultCameraZoom;
		m_CameraFollow = kDefaultCameraFollow;
	}

	public void SetDefaults(Context context)
	{
		m_LoadedCityName = "";
		loadedDefaultTheme = Entity.Null;
		m_LoadedLeftHandTraffic = false;
		m_LoadedNaturalDisasters = false;
		m_LoadedUnlimitedMoney = false;
		m_LoadedUnlockAll = false;
		m_LoadedUnlockMapTiles = false;
		m_RequiredContent.ResizeUninitialized(0);
		if (!overrideLoadedOptions)
		{
			cityName = m_LoadedCityName;
			defaultTheme = loadedDefaultTheme;
			leftHandTraffic = m_LoadedLeftHandTraffic;
			naturalDisasters = m_LoadedNaturalDisasters;
			unlimitedMoney = m_LoadedUnlimitedMoney;
			unlockAll = m_LoadedUnlockAll;
			unlockMapTiles = m_LoadedUnlockMapTiles;
		}
		overrideLoadedOptions = false;
		ResetCameraProperties();
		usedMods.Clear();
	}

	[Preserve]
	public CityConfigurationSystem()
	{
	}
}
