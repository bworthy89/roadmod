using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs.Modes;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GameModeNaturalResourcesAdjustSystem : GameSystemBase
{
	[BurstCompile]
	private struct BoostInitialNaturalResourcesJob : IJobParallelFor
	{
		public CellMapData<NaturalResourceCell> m_CellData;

		public float m_BoostMultiplier;

		public void Execute(int index)
		{
			NaturalResourceCell value = m_CellData.m_Buffer[index];
			value.m_Fertility.m_Base = (ushort)math.min((int)((float)(int)value.m_Fertility.m_Base * m_BoostMultiplier), 65535);
			value.m_Ore.m_Base = (ushort)math.min((int)((float)(int)value.m_Ore.m_Base * m_BoostMultiplier), 65535);
			value.m_Oil.m_Base = (ushort)math.min((int)((float)(int)value.m_Oil.m_Base * m_BoostMultiplier), 65535);
			m_CellData.m_Buffer[index] = value;
		}
	}

	[BurstCompile]
	private struct BoostInitialGroundWaterJob : IJobParallelFor
	{
		public CellMapData<GroundWater> m_CellData;

		public float m_BoostMultiplier;

		public void Execute(int index)
		{
			GroundWater value = m_CellData.m_Buffer[index];
			value.m_Amount = (short)math.min((int)((float)value.m_Amount * m_BoostMultiplier), 32767);
			value.m_Polluted = (short)math.min((int)((float)value.m_Polluted * m_BoostMultiplier), 32767);
			value.m_Max = (short)math.min((int)((float)value.m_Max * m_BoostMultiplier), 32767);
			m_CellData.m_Buffer[index] = value;
		}
	}

	[BurstCompile]
	private struct RefillNaturalResourcesJob : IJobParallelFor
	{
		public CellMapData<NaturalResourceCell> m_CellData;

		public ModeSettingData m_GlobalData;

		public void Execute(int index)
		{
			NaturalResourceCell value = m_CellData.m_Buffer[index];
			value.m_Oil.m_Used = (ushort)math.max(0f, (float)(int)value.m_Oil.m_Used - (float)(int)value.m_Oil.m_Base * ((float)m_GlobalData.m_PercentOilRefillAmountPerDay / 100f) / (float)kUpdatesPerDay);
			value.m_Ore.m_Used = (ushort)math.max(0f, (float)(int)value.m_Ore.m_Used - (float)(int)value.m_Ore.m_Base * ((float)m_GlobalData.m_PercentOreRefillAmountPerDay / 100f) / (float)kUpdatesPerDay);
			m_CellData.m_Buffer[index] = value;
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private EntityQuery m_GameModeSettingQuery;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_GameModeSettingQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		RequireForUpdate(m_GameModeSettingQuery);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			base.Enabled = false;
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable && singleton.m_EnableAdjustNaturalResources)
		{
			if (serializationContext.purpose == Purpose.NewGame)
			{
				BoostStartGameNaturalResources(singleton.m_InitialNaturalResourceBoostMultiplier);
			}
			base.Enabled = true;
		}
		else
		{
			base.Enabled = false;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
			if (singleton.m_Enable && singleton.m_EnableAdjustNaturalResources)
			{
				JobHandle dependencies;
				CellMapData<NaturalResourceCell> data = m_NaturalResourceSystem.GetData(readOnly: false, out dependencies);
				JobHandle jobHandle = IJobParallelForExtensions.Schedule(new RefillNaturalResourcesJob
				{
					m_CellData = data,
					m_GlobalData = singleton
				}, data.m_TextureSize.x * data.m_TextureSize.y, 64, dependencies);
				m_NaturalResourceSystem.AddWriter(jobHandle);
			}
		}
	}

	private void BoostStartGameNaturalResources(float boostMultiplier)
	{
		JobHandle dependencies;
		CellMapData<NaturalResourceCell> data = m_NaturalResourceSystem.GetData(readOnly: false, out dependencies);
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(new BoostInitialNaturalResourcesJob
		{
			m_CellData = data,
			m_BoostMultiplier = boostMultiplier
		}, data.m_TextureSize.x * data.m_TextureSize.y, 64, dependencies);
		m_NaturalResourceSystem.AddWriter(jobHandle);
		JobHandle dependencies2;
		CellMapData<GroundWater> data2 = m_GroundWaterSystem.GetData(readOnly: false, out dependencies2);
		JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(new BoostInitialGroundWaterJob
		{
			m_CellData = data2,
			m_BoostMultiplier = boostMultiplier
		}, data2.m_TextureSize.x * data2.m_TextureSize.y, 64, dependencies2);
		m_GroundWaterSystem.AddWriter(jobHandle2);
	}

	[Preserve]
	public GameModeNaturalResourcesAdjustSystem()
	{
	}
}
