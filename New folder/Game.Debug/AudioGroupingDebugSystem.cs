using System.Runtime.CompilerServices;
using Colossal;
using Game.Prefabs;
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
public class AudioGroupingDebugSystem : GameSystemBase
{
	private struct AudioGroupingGizmoJob : IJob
	{
		public Entity m_SettingsEntity;

		[ReadOnly]
		public NativeArray<TrafficAmbienceCell> m_TrafficMap;

		[ReadOnly]
		public NativeArray<ZoneAmbienceCell> m_ZoneMap;

		[ReadOnly]
		public TerrainHeightData m_HeightData;

		[ReadOnly]
		public BufferLookup<AudioGroupingSettingsData> m_Settings;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			DynamicBuffer<AudioGroupingSettingsData> dynamicBuffer = m_Settings[m_SettingsEntity];
			for (int i = 0; i < m_TrafficMap.Length; i++)
			{
				TrafficAmbienceCell trafficAmbienceCell = m_TrafficMap[i];
				float num = math.saturate(dynamicBuffer[14].m_Scale * trafficAmbienceCell.m_Traffic);
				if (trafficAmbienceCell.m_Traffic > 0f)
				{
					float3 @float = TrafficAmbienceSystem.GetCellCenter(i) + new float3(-60f, 0f, 0f);
					float num2 = TerrainUtils.SampleHeight(ref m_HeightData, @float);
					@float.y += 100f * num / 2f + num2;
					m_GizmoBatcher.DrawWireCube(@float, new float3(30f, 100f * num, 30f), Color.white);
				}
			}
			for (int j = 0; j < m_ZoneMap.Length; j++)
			{
				ZoneAmbienceCell zoneAmbienceCell = m_ZoneMap[j];
				for (int k = 0; k < 23; k++)
				{
					float num3 = math.saturate(dynamicBuffer[k].m_Scale * zoneAmbienceCell.m_Value.GetAmbience(dynamicBuffer[k].m_Type));
					if (num3 > 0f)
					{
						float3 float2 = ZoneAmbienceSystem.GetCellCenter(j) + new float3(15f * (float)(k % 5) - 40f, 0f, 15f * (float)(k / 5));
						float num4 = TerrainUtils.SampleHeight(ref m_HeightData, float2);
						float2.y += 100f * num3 / 2f + num4;
						Color color;
						switch (dynamicBuffer[k].m_Type)
						{
						case GroupAmbienceType.ResidentialLow:
						case GroupAmbienceType.ResidentialMedium:
						case GroupAmbienceType.ResidentialHigh:
						case GroupAmbienceType.ResidentialMixed:
						case GroupAmbienceType.ResidentialLowRent:
							color = Color.green;
							break;
						case GroupAmbienceType.CommercialLow:
						case GroupAmbienceType.CommercialHigh:
							color = Color.blue;
							break;
						case GroupAmbienceType.Industrial:
							color = Color.yellow;
							break;
						case GroupAmbienceType.OfficeLow:
						case GroupAmbienceType.OfficeHigh:
							color = Color.cyan;
							break;
						case GroupAmbienceType.Forest:
							color = new Color(34f, 139f, 34f);
							break;
						default:
							color = Color.grey;
							break;
						}
						m_GizmoBatcher.DrawWireCube(float2, new float3(15f, 100f * num3, 15f), color);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<AudioGroupingSettingsData> __Game_Prefabs_AudioGroupingSettingsData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_AudioGroupingSettingsData_RO_BufferLookup = state.GetBufferLookup<AudioGroupingSettingsData>(isReadOnly: true);
		}
	}

	private TrafficAmbienceSystem m_TrafficAmbienceSystem;

	private ZoneAmbienceSystem m_ZoneAmbienceSystem;

	private TerrainSystem m_TerrainSystem;

	private GizmosSystem m_GizmosSystem;

	private EntityQuery m_AudioGroupingConfigurationQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_TrafficAmbienceSystem = base.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>();
		m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		base.Enabled = false;
		m_AudioGroupingConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<AudioGroupingSettingsData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		AudioGroupingGizmoJob jobData = new AudioGroupingGizmoJob
		{
			m_SettingsEntity = m_AudioGroupingConfigurationQuery.GetSingletonEntity(),
			m_Settings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AudioGroupingSettingsData_RO_BufferLookup, ref base.CheckedStateRef),
			m_TrafficMap = m_TrafficAmbienceSystem.GetMap(readOnly: true, out dependencies),
			m_ZoneMap = m_ZoneAmbienceSystem.GetMap(readOnly: true, out dependencies2),
			m_HeightData = m_TerrainSystem.GetHeightData(),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies3)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, dependencies3, JobHandle.CombineDependencies(dependencies2, dependencies)));
		m_TrafficAmbienceSystem.AddReader(base.Dependency);
		m_ZoneAmbienceSystem.AddReader(base.Dependency);
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
	public AudioGroupingDebugSystem()
	{
	}
}
