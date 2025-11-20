using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Prefabs.Modes;

public class GameModeSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	private PrefabSystem m_PrefabSystem;

	private ModeSetting m_ModeSetting;

	private ModeSetting m_NextMode;

	private EntityQuery m_ModeSettingQuery;

	private EntityQuery m_ModeInfoQuery;

	[CanBeNull]
	public string overrideMode { get; set; }

	public ModeSetting modeSetting => m_ModeSetting;

	public string currentModeName { get; private set; }

	public List<GameModeInfo> GetGameModeInfo()
	{
		List<GameModeInfo> list = new List<GameModeInfo>();
		NativeArray<Entity> nativeArray = m_ModeInfoQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			GameModeInfo gameModeInfo = m_PrefabSystem.GetPrefab<GameModeInfoPrefab>(entity).GetGameModeInfo();
			list.Add(gameModeInfo);
		}
		nativeArray.Dispose();
		return list;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<GameModeSettingData>());
		m_ModeInfoQuery = GetEntityQuery(ComponentType.ReadOnly<GameModeInfoData>());
		currentModeName = string.Empty;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (overrideMode != null)
		{
			m_NextMode = GetModeSetting(overrideMode);
			overrideMode = null;
		}
		if (m_ModeSetting != null)
		{
			COSystemBase.baseLog.Debug("Clean up " + m_ModeSetting.prefab.name);
			m_ModeSetting.RestoreDefaultData(base.EntityManager, m_PrefabSystem);
			m_ModeSetting = null;
		}
		m_ModeSetting = m_NextMode;
		m_NextMode = null;
		if (m_ModeSetting == null)
		{
			NativeArray<Entity> nativeArray = m_ModeSettingQuery.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				ModeSetting prefab = m_PrefabSystem.GetPrefab<ModeSetting>(entity);
				if (prefab.prefab.name == "NormalMode")
				{
					m_ModeSetting = prefab;
					break;
				}
			}
			nativeArray.Dispose();
		}
		if (m_ModeSetting != null)
		{
			COSystemBase.baseLog.Debug("Apply " + m_ModeSetting.prefab.name);
			m_ModeSetting.StoreDefaultData(base.EntityManager, m_PrefabSystem);
			base.Dependency = m_ModeSetting.ApplyMode(base.EntityManager, m_PrefabSystem, base.Dependency);
			currentModeName = m_ModeSetting.prefab.name;
		}
	}

	public ModeSetting GetModeSetting(string mode)
	{
		ModeSetting result = null;
		NativeArray<Entity> nativeArray = m_ModeSettingQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			ModeSetting prefab = m_PrefabSystem.GetPrefab<ModeSetting>(entity);
			if (prefab.prefab.name == mode)
			{
				result = prefab;
			}
		}
		nativeArray.Dispose();
		return result;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		if (m_ModeSetting != null)
		{
			PrefabID prefabID = m_ModeSetting.prefab.GetPrefabID();
			writer.Write(prefabID);
		}
		else
		{
			writer.Write(default(PrefabID));
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out PrefabID value);
		if (m_PrefabSystem.TryGetPrefab(value, out var prefab) && prefab.TryGetExactly<ModeSetting>(out var component))
		{
			m_NextMode = component;
		}
		else
		{
			m_NextMode = null;
		}
	}

	public void SetDefaults(Context context)
	{
	}

	[Preserve]
	public GameModeSystem()
	{
	}
}
