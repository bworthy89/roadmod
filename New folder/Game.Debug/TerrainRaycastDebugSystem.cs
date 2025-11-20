using Colossal;
using Colossal.Mathematics;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class TerrainRaycastDebugSystem : BaseDebugSystem
{
	private struct TerrainRaycastGizmoJob : IJob
	{
		public GizmoBatcher m_GizmoBatcher;

		public float3 m_hitPos;

		public Bounds3 m_hitBounds;

		public void Execute()
		{
			m_GizmoBatcher.DrawWireNode(m_hitPos, 20f, Color.red);
			m_GizmoBatcher.DrawWireBounds((Bounds)m_hitBounds, Color.yellow);
		}
	}

	private GizmosSystem m_GizmosSystem;

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private float3 m_hitPos;

	private Bounds3 m_hitBounds;

	private Option m_HitWater;

	private Option m_SampleMaxHeight;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_HitWater = AddOption("Raycast can hit water", defaultEnabled: false);
		m_SampleMaxHeight = AddOption("Sample Max Height", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		GizmoBatcher gizmosBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies);
		dependencies.Complete();
		if (!m_CameraUpdateSystem.TryGetViewer(out var viewer))
		{
			return;
		}
		Line3.Segment segment = ToolRaycastSystem.CalculateRaycastLine(viewer.camera);
		Bounds3 hitBounds = default(Bounds3);
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		float t2;
		float3 normal;
		if (m_HitWater.enabled)
		{
			JobHandle deps;
			WaterSurfacesData watersData = m_WaterSystem.GetSurfacesData(out deps);
			if (WaterUtils.Raycast(ref watersData, ref data, segment, outside: true, out var t, out hitBounds))
			{
				m_hitBounds = hitBounds;
				m_hitPos = MathUtils.Position(segment, t);
			}
			if (m_SampleMaxHeight.enabled)
			{
				JobHandle deps2;
				WaterSurfaceData<half> maxHeightSurfaceData = m_WaterSystem.GetMexHeightSurfaceData(out deps2);
				m_hitPos.y = WaterUtils.SampleHeight(ref maxHeightSurfaceData, ref watersData, ref data, m_hitPos);
			}
		}
		else if (TerrainUtils.Raycast(ref data, segment, outside: true, out t2, out normal, out hitBounds))
		{
			m_hitBounds = hitBounds;
			m_hitPos = MathUtils.Position(segment, t2);
		}
		TerrainRaycastGizmoJob jobData = new TerrainRaycastGizmoJob
		{
			m_GizmoBatcher = gizmosBatcher,
			m_hitPos = m_hitPos,
			m_hitBounds = m_hitBounds
		};
		base.Dependency = jobData.Schedule(dependencies);
	}

	[Preserve]
	public TerrainRaycastDebugSystem()
	{
	}
}
