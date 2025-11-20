using Game.Buildings;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialFireActivationSystem : GameSystemBase
{
	protected EntityCommandBufferSystem m_BarrierSystem;

	private EntityQuery m_BuildingFireQuery;

	private EntityQuery m_ForestFireQuery;

	private EntityQuery m_BuildingFireTutorialQuery;

	private EntityQuery m_ForestFireTutorialQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_BuildingFireQuery = GetEntityQuery(ComponentType.ReadOnly<OnFire>(), ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ForestFireQuery = GetEntityQuery(ComponentType.ReadOnly<OnFire>(), ComponentType.ReadOnly<Tree>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_BuildingFireTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingFireActivationData>(), ComponentType.Exclude<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>());
		m_ForestFireTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<ForestFireActivationData>(), ComponentType.Exclude<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_ForestFireQuery.IsEmptyIgnoreFilter && !m_ForestFireTutorialQuery.IsEmptyIgnoreFilter;
		bool flag2 = !m_BuildingFireQuery.IsEmptyIgnoreFilter && !m_BuildingFireTutorialQuery.IsEmptyIgnoreFilter;
		if (flag || flag2)
		{
			EntityCommandBuffer entityCommandBuffer = m_BarrierSystem.CreateCommandBuffer();
			if (flag)
			{
				entityCommandBuffer.AddComponent<TutorialActivated>(m_ForestFireTutorialQuery, EntityQueryCaptureMode.AtPlayback);
			}
			if (flag2)
			{
				entityCommandBuffer.AddComponent<TutorialActivated>(m_BuildingFireTutorialQuery, EntityQueryCaptureMode.AtPlayback);
			}
		}
	}

	[Preserve]
	public TutorialFireActivationSystem()
	{
	}
}
