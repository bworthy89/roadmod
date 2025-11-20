using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tools;

public class HeatmapPreviewSystem : GameSystemBase
{
	private TelecomPreviewSystem m_TelecomPreviewSystem;

	private EntityQuery m_InfomodeQuery;

	private ComponentSystemBase m_LastPreviewSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TelecomPreviewSystem = base.World.GetOrCreateSystemManaged<TelecomPreviewSystem>();
		m_InfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewHeatmapData>());
		RequireForUpdate(m_InfomodeQuery);
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		if (m_LastPreviewSystem != null)
		{
			m_LastPreviewSystem.Enabled = false;
			m_LastPreviewSystem.Update();
			m_LastPreviewSystem = null;
		}
		base.OnStopRunning();
	}

	private ComponentSystemBase GetPreviewSystem()
	{
		if (m_InfomodeQuery.IsEmptyIgnoreFilter)
		{
			return null;
		}
		NativeArray<InfoviewHeatmapData> nativeArray = m_InfomodeQuery.ToComponentDataArray<InfoviewHeatmapData>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_Type == HeatmapData.TelecomCoverage)
				{
					return m_TelecomPreviewSystem;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		return null;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ComponentSystemBase previewSystem = GetPreviewSystem();
		if (previewSystem != m_LastPreviewSystem)
		{
			if (m_LastPreviewSystem != null)
			{
				m_LastPreviewSystem.Enabled = false;
				m_LastPreviewSystem.Update();
			}
			m_LastPreviewSystem = previewSystem;
			if (m_LastPreviewSystem != null)
			{
				m_LastPreviewSystem.Enabled = true;
			}
		}
		previewSystem?.Update();
	}

	[Preserve]
	public HeatmapPreviewSystem()
	{
	}
}
