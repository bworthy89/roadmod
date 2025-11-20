using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Prefabs.Modes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GameModeWealthSupportSystem : GameSystemBase
{
	[BurstCompile]
	private struct SupportWageJob : IJobChunk
	{
		public uint m_UpdateFrameIndex;

		public int m_MinimumWealth;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (m_UpdateFrameIndex != chunk.GetSharedComponent(m_UpdateFrameType).m_Index)
			{
				return;
			}
			NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Household householdData = nativeArray[i];
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(householdData, resources);
				if (householdTotalWealth <= m_MinimumWealth)
				{
					EconomyUtils.AddResources(Resource.Money, m_MinimumWealth - householdTotalWealth, resources);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_GameModeSettingQuery;

	private EntityQuery m_HouseholdGroup;

	private int m_MinimumWealth;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_GameModeSettingQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_HouseholdGroup = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_GameModeSettingQuery);
		RequireForUpdate(m_HouseholdGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		SupportWageJob jobData = new SupportWageJob
		{
			m_UpdateFrameIndex = updateFrame,
			m_MinimumWealth = m_MinimumWealth,
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_HouseholdType = GetComponentTypeHandle<Household>(isReadOnly: true),
			m_ResourcesType = GetBufferTypeHandle<Resources>()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_HouseholdGroup, base.Dependency);
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
		if (singleton.m_Enable && singleton.m_SupportPoorCitizens)
		{
			m_MinimumWealth = singleton.m_MinimumWealth;
			base.Enabled = true;
		}
		else
		{
			base.Enabled = false;
		}
	}

	[Preserve]
	public GameModeWealthSupportSystem()
	{
	}
}
