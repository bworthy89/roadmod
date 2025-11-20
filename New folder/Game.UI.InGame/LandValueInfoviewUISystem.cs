using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LandValueInfoviewUISystem : InfoviewUISystemBase
{
	[BurstCompile]
	private struct CalculateAverageLandValueJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingTypeHandle;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Building> nativeArray = chunk.GetNativeArray(ref m_BuildingTypeHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Building building = nativeArray[i];
				if (m_LandValues.HasComponent(building.m_RoadEdge))
				{
					LandValue landValue = m_LandValues[building.m_RoadEdge];
					m_Results[0] += landValue.m_LandValue;
					m_Results[1] += 1f;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
		}
	}

	private const string kGroup = "landValueInfo";

	private ValueBinding<float> m_AverageLandValue;

	private EntityQuery m_LandValueQuery;

	private NativeArray<float> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active)
			{
				return m_AverageLandValue.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LandValueQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<BuildingCondition>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		AddBinding(m_AverageLandValue = new ValueBinding<float>("landValueInfo", "averageLandValue", 0f));
		m_Results = new NativeArray<float>(2, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_Results.Dispose();
	}

	protected override void PerformUpdate()
	{
		UpdateLandValue();
	}

	private void UpdateLandValue()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0f;
		}
		JobChunkExtensions.Schedule(new CalculateAverageLandValueJob
		{
			m_BuildingTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_LandValueQuery, base.Dependency).Complete();
		float num = m_Results[1];
		float newValue = ((num > 0f) ? (m_Results[0] / num) : 0f);
		m_AverageLandValue.Update(newValue);
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
	public LandValueInfoviewUISystem()
	{
	}
}
