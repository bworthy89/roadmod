using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Game.Objects;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class WaterDebugSystem : BaseDebugSystem
{
	private struct WaterGizmoJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_WaterSources;

		[ReadOnly]
		public ComponentLookup<WaterSourceData> m_SourceDatas;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public NativeArray<int> m_WaterActive;

		[ReadOnly]
		public NativeArray<SurfaceWater> m_WaterDepths;

		public GizmoBatcher m_GizmoBatcher;

		public float2 m_MapSize;

		public bool m_ShowCulling;

		public bool m_showSurface;

		public float m_GridCellInMeters;

		public float m_CellInMeters;

		public float3 m_PositionOffset;

		public void Execute()
		{
			for (int i = 0; i < m_WaterSources.Length; i++)
			{
				Entity entity = m_WaterSources[i];
				WaterSourceData waterSourceData = m_SourceDatas[entity];
				Game.Objects.Transform transform = m_Transforms[entity];
				UnityEngine.Color cyan = UnityEngine.Color.cyan;
				float3 position = transform.m_Position;
				position.y += m_PositionOffset.y;
				m_GizmoBatcher.DrawWireCylinder(position, waterSourceData.m_Radius, position.y, cyan);
			}
			for (int j = 0; j < m_WaterActive.Length; j++)
			{
				int2 @int = new int2(Mathf.RoundToInt(m_MapSize.x / m_GridCellInMeters), Mathf.RoundToInt(m_MapSize.x / m_GridCellInMeters));
				int num = j % @int.x;
				int num2 = j / @int.x;
				float3 @float = new float3(((float)num + 0.5f) * m_GridCellInMeters, 200f, ((float)num2 + 0.5f) * m_GridCellInMeters);
				@float += m_PositionOffset;
				UnityEngine.Color color = ((m_WaterActive[j] > 0) ? UnityEngine.Color.white : UnityEngine.Color.red);
				if (m_ShowCulling)
				{
					m_GizmoBatcher.DrawWireCube(@float, new float3(m_GridCellInMeters, 400f, m_GridCellInMeters), color);
				}
				if (!m_showSurface || m_WaterActive[j] <= 0)
				{
					continue;
				}
				@float.y = 200f;
				int2 int2 = new int2(Mathf.RoundToInt(m_GridCellInMeters / m_CellInMeters), Mathf.RoundToInt(m_GridCellInMeters / m_CellInMeters));
				for (int k = 0; k < int2.x; k += 16)
				{
					for (int l = 0; l < int2.y; l += 16)
					{
						int num3 = num * int2.x + k;
						int num4 = num2 * int2.y + l;
						int num5 = num3 + num4 * Mathf.RoundToInt(m_MapSize.x / m_CellInMeters);
						if (num5 < m_WaterDepths.Length)
						{
							SurfaceWater surfaceWater = m_WaterDepths[num5];
							if (surfaceWater.m_Depth > 0f)
							{
								UnityEngine.Color color2 = UnityEngine.Color.Lerp(UnityEngine.Color.blue, new UnityEngine.Color(0.54f, 0.27f, 0.07f), surfaceWater.m_Polluted);
								m_GizmoBatcher.DrawWireCube(@float + new float3((float)k * m_CellInMeters - 0.5f * m_GridCellInMeters, 0f, (float)l * m_CellInMeters - 0.5f * m_GridCellInMeters), new float3(m_CellInMeters, surfaceWater.m_Depth, m_CellInMeters), color2);
							}
						}
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_WaterSourceData_RO_ComponentLookup = state.GetComponentLookup<WaterSourceData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
		}
	}

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private GizmosSystem m_GizmosSystem;

	private EntityQuery m_WaterSourceGroup;

	private Option m_ShowCulling;

	private Option m_showSurface;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_showSurface = AddOption("Show Surface Boxes", defaultEnabled: true);
		m_ShowCulling = AddOption("Show Culling Boxes", defaultEnabled: false);
		m_WaterSourceGroup = GetEntityQuery(ComponentType.ReadOnly<WaterSourceData>());
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		JobHandle deps;
		JobHandle dependencies;
		WaterGizmoJob jobData = new WaterGizmoJob
		{
			m_WaterSources = m_WaterSourceGroup.ToEntityListAsync(Allocator.TempJob, out outJobHandle),
			m_SourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterActive = m_WaterSystem.GetActive(),
			m_WaterDepths = m_WaterSystem.GetDepths(out deps),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_GridCellInMeters = (float)m_WaterSystem.GridSize * m_WaterSystem.CellSize,
			m_CellInMeters = m_WaterSystem.CellSize,
			m_MapSize = m_WaterSystem.MapSize,
			m_PositionOffset = m_TerrainSystem.positionOffset,
			m_ShowCulling = m_ShowCulling.enabled,
			m_showSurface = m_showSurface.enabled
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, deps, dependencies, outJobHandle));
		jobData.m_WaterSources.Dispose(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
		m_WaterSystem.AddActiveReader(base.Dependency);
		m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
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
	public WaterDebugSystem()
	{
	}
}
