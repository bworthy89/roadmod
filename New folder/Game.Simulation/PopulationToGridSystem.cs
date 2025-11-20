using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PopulationToGridSystem : CellMapSystem<PopulationCell>, IJobSerializable
{
	[BurstCompile]
	private struct PopulationToGridJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_Entities;

		public NativeArray<PopulationCell> m_PopulationMap;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public void Execute()
		{
			for (int i = 0; i < kTextureSize * kTextureSize; i++)
			{
				m_PopulationMap[i] = default(PopulationCell);
			}
			for (int j = 0; j < m_Entities.Length; j++)
			{
				Entity entity = m_Entities[j];
				int num = 0;
				DynamicBuffer<Renter> dynamicBuffer = m_Renters[entity];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity renter = dynamicBuffer[k].m_Renter;
					if (m_HouseholdCitizens.HasBuffer(renter))
					{
						num += m_HouseholdCitizens[renter].Length;
					}
				}
				int2 cell = CellMapSystem<PopulationCell>.GetCell(m_Transforms[entity].m_Position, CellMapSystem<PopulationCell>.kMapSize, kTextureSize);
				if (cell.x >= 0 && cell.y >= 0 && cell.x < kTextureSize && cell.y < kTextureSize)
				{
					int index = cell.x + cell.y * kTextureSize;
					PopulationCell value = m_PopulationMap[index];
					value.m_Population += num;
					m_PopulationMap[index] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
		}
	}

	public static readonly int kTextureSize = 64;

	public static readonly int kUpdatesPerDay = 32;

	private EntityQuery m_ResidentialPropertyQuery;

	private TypeHandle __TypeHandle;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<PopulationCell>.GetCellCenter(index, kTextureSize);
	}

	public static PopulationCell GetPopulation(float3 position, NativeArray<PopulationCell> populationMap)
	{
		PopulationCell result = default(PopulationCell);
		int2 cell = CellMapSystem<PopulationCell>.GetCell(position, CellMapSystem<PopulationCell>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<PopulationCell>.GetCellCoords(position, CellMapSystem<PopulationCell>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return result;
		}
		float population = populationMap[cell.x + kTextureSize * cell.y].m_Population;
		float end = ((cell.x < kTextureSize - 1) ? populationMap[cell.x + 1 + kTextureSize * cell.y].m_Population : 0f);
		float start = ((cell.y < kTextureSize - 1) ? populationMap[cell.x + kTextureSize * (cell.y + 1)].m_Population : 0f);
		float end2 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? populationMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_Population : 0f);
		result.m_Population = math.lerp(math.lerp(population, end, cellCoords.x - (float)cell.x), math.lerp(start, end2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
		m_ResidentialPropertyQuery = GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<Transform>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		PopulationToGridJob jobData = new PopulationToGridJob
		{
			m_Entities = m_ResidentialPropertyQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_PopulationMap = m_Map,
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(outJobHandle, m_WriteDependencies, m_ReadDependencies, base.Dependency));
		AddWriter(base.Dependency);
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
	public PopulationToGridSystem()
	{
	}
}
