using Game.Areas;
using Game.Buildings;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct AreaPathfindSetup
{
	[BurstCompile]
	private struct SetupAreaLocationJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<CargoTransportStationData> m_CargoTransportStationData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			Random random = targetSeeker.m_RandomSeed.GetRandom(0);
			if (targetSeeker.m_AreaNode.HasBuffer(entity))
			{
				m_SubAreas.TryGetBuffer(entity, out var bufferData);
				int num = 0;
				if (m_SubObjects.TryGetBuffer(entity, out var bufferData2))
				{
					for (int i = 0; i < bufferData2.Length; i++)
					{
						Entity subObject = bufferData2[i].m_SubObject;
						if (!m_SecondaryData.HasComponent(subObject))
						{
							float cost = random.NextFloat(600f);
							num += targetSeeker.AddAreaTargets(ref random, subObject, entity, subObject, bufferData, cost, addDistanceCost: false, EdgeFlags.DefaultMask);
						}
					}
				}
				if (num == 0)
				{
					targetSeeker.m_SetupQueueTarget.m_RandomCost = 600f;
					targetSeeker.AddAreaTargets(ref random, entity, entity, Entity.Null, bufferData, 0f, addDistanceCost: false, EdgeFlags.DefaultMask);
				}
			}
			else
			{
				if (!targetSeeker.m_PrefabRef.TryGetComponent(entity, out var componentData) || !targetSeeker.m_Owner.TryGetComponent(entity, out var componentData2) || !m_CargoTransportStationData.HasComponent(componentData.m_Prefab) || !m_InstalledUpgrades.TryGetBuffer(componentData2.m_Owner, out var bufferData3))
				{
					return;
				}
				for (int j = 0; j < bufferData3.Length; j++)
				{
					InstalledUpgrade installedUpgrade = bufferData3[j];
					if (!(installedUpgrade.m_Upgrade == entity) && !BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
					{
						PrefabRef prefabRef = targetSeeker.m_PrefabRef[installedUpgrade.m_Upgrade];
						if (m_CargoTransportStationData.TryGetComponent(prefabRef.m_Prefab, out var componentData3) && componentData3.m_WorkMultiplier > 0f)
						{
							float cost2 = random.NextFloat(10000f);
							targetSeeker.FindTargets(installedUpgrade.m_Upgrade, installedUpgrade.m_Upgrade, cost2, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
						}
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct SetupWoodResourceJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public BufferLookup<WoodResource> m_WoodResources;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			VehicleWorkType value = (VehicleWorkType)targetSeeker.m_SetupQueueTarget.m_Value;
			if (!m_WoodResources.HasBuffer(entity))
			{
				return;
			}
			DynamicBuffer<WoodResource> dynamicBuffer = m_WoodResources[entity];
			Random random = targetSeeker.m_RandomSeed.GetRandom(0);
			m_SubAreas.TryGetBuffer(entity, out var bufferData);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity tree = dynamicBuffer[i].m_Tree;
				Tree tree2 = m_TreeData[tree];
				float num = random.NextFloat(15f);
				switch (value)
				{
				case VehicleWorkType.Harvest:
					if ((tree2.m_State & TreeState.Adult) != 0)
					{
						num += (float)(511 - tree2.m_Growth) * (15f / 128f);
						break;
					}
					if ((tree2.m_State & TreeState.Elderly) == 0)
					{
						continue;
					}
					num += (float)(255 - tree2.m_Growth) * (15f / 128f);
					break;
				case VehicleWorkType.Collect:
					if ((tree2.m_State & (TreeState.Stump | TreeState.Collected)) == TreeState.Stump)
					{
						num += (float)(int)tree2.m_Growth * (15f / 128f);
						break;
					}
					if ((tree2.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Collected)) != 0)
					{
						continue;
					}
					num += (float)(256 + tree2.m_Growth) * (15f / 128f);
					break;
				}
				targetSeeker.AddAreaTargets(ref random, tree, entity, tree, bufferData, num, addDistanceCost: false, EdgeFlags.DefaultMask);
			}
		}
	}

	private ComponentLookup<Tree> m_TreeData;

	private ComponentLookup<Secondary> m_SecondaryData;

	private ComponentLookup<CargoTransportStationData> m_CargoTransportStationData;

	private BufferLookup<Game.Objects.SubObject> m_SubObjects;

	private BufferLookup<WoodResource> m_WoodResources;

	private BufferLookup<Game.Areas.SubArea> m_SubAreas;

	private BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

	public AreaPathfindSetup(PathfindSetupSystem system)
	{
		m_TreeData = system.GetComponentLookup<Tree>(isReadOnly: true);
		m_SecondaryData = system.GetComponentLookup<Secondary>(isReadOnly: true);
		m_CargoTransportStationData = system.GetComponentLookup<CargoTransportStationData>(isReadOnly: true);
		m_SubObjects = system.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		m_WoodResources = system.GetBufferLookup<WoodResource>(isReadOnly: true);
		m_SubAreas = system.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
		m_InstalledUpgrades = system.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
	}

	public JobHandle SetupAreaLocation(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_SecondaryData.Update(system);
		m_CargoTransportStationData.Update(system);
		m_SubObjects.Update(system);
		m_SubAreas.Update(system);
		m_InstalledUpgrades.Update(system);
		return IJobParallelForExtensions.Schedule(new SetupAreaLocationJob
		{
			m_SecondaryData = m_SecondaryData,
			m_CargoTransportStationData = m_CargoTransportStationData,
			m_SubObjects = m_SubObjects,
			m_SubAreas = m_SubAreas,
			m_InstalledUpgrades = m_InstalledUpgrades,
			m_SetupData = setupData
		}, setupData.Length, 1, inputDeps);
	}

	public JobHandle SetupWoodResource(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_TreeData.Update(system);
		m_WoodResources.Update(system);
		m_SubAreas.Update(system);
		return IJobParallelForExtensions.Schedule(new SetupWoodResourceJob
		{
			m_TreeData = m_TreeData,
			m_WoodResources = m_WoodResources,
			m_SubAreas = m_SubAreas,
			m_SetupData = setupData
		}, setupData.Length, 1, inputDeps);
	}
}
