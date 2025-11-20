using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AvailabilityInfoToGridSystem : CellMapSystem<AvailabilityInfoCell>, IJobSerializable
{
	private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public AvailabilityInfoCell m_TotalWeight;

		public AvailabilityInfoCell m_Result;

		public float m_CellSize;

		public Bounds3 m_Bounds;

		public BufferLookup<ResourceAvailability> m_Availabilities;

		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		private void AddData(float2 attractiveness2, float2 uneducated2, float2 educated2, float2 services2, float2 workplaces2, float2 t, float3 curvePos, float weight)
		{
			float num = math.lerp(attractiveness2.x, attractiveness2.y, t.y);
			float num2 = 0.5f * math.lerp(uneducated2.x + educated2.x, uneducated2.y + educated2.y, t.y);
			float num3 = math.lerp(services2.x, services2.y, t.y);
			float num4 = math.lerp(workplaces2.x, workplaces2.y, t.y);
			m_Result.AddAttractiveness(weight * num);
			m_TotalWeight.AddAttractiveness(weight);
			m_Result.AddConsumers(weight * num2);
			m_TotalWeight.AddConsumers(weight);
			m_Result.AddServices(weight * num3);
			m_TotalWeight.AddServices(weight);
			m_Result.AddWorkplaces(weight * num4);
			m_TotalWeight.AddWorkplaces(weight);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_Availabilities.HasBuffer(entity) && m_EdgeGeometryData.HasComponent(entity))
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer = m_Availabilities[entity];
				float2 availability = dynamicBuffer[18].m_Availability;
				float2 availability2 = dynamicBuffer[2].m_Availability;
				float2 availability3 = dynamicBuffer[3].m_Availability;
				float2 availability4 = dynamicBuffer[1].m_Availability;
				float2 availability5 = dynamicBuffer[0].m_Availability;
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
				int num = (int)math.ceil(edgeGeometry.m_Start.middleLength * 0.05f);
				int num2 = (int)math.ceil(edgeGeometry.m_End.middleLength * 0.05f);
				float3 @float = 0.5f * (m_Bounds.min + m_Bounds.max);
				for (int i = 1; i <= num; i++)
				{
					float2 t = i / new float2(num, num + num2);
					float3 curvePos = math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, t.x), MathUtils.Position(edgeGeometry.m_Start.m_Right, t.x), 0.5f);
					float weight = math.max(0f, 1f - math.distance(@float.xz, curvePos.xz) / (1.5f * m_CellSize));
					AddData(availability, availability2, availability3, availability4, availability5, t, curvePos, weight);
				}
				for (int j = 1; j <= num2; j++)
				{
					float2 t2 = new float2(j, num + j) / new float2(num2, num + num2);
					float3 curvePos2 = math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, t2.x), MathUtils.Position(edgeGeometry.m_End.m_Right, t2.x), 0.5f);
					float weight2 = math.max(0f, 1f - math.distance(@float.xz, curvePos2.xz) / (1.5f * m_CellSize));
					AddData(availability, availability2, availability3, availability4, availability5, t2, curvePos2, weight2);
				}
			}
		}
	}

	[BurstCompile]
	private struct AvailabilityInfoToGridJob : IJobParallelFor
	{
		public NativeArray<AvailabilityInfoCell> m_AvailabilityInfoMap;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_AvailabilityData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		public float m_CellSize;

		public void Execute(int index)
		{
			float3 cellCenter = CellMapSystem<AvailabilityInfoCell>.GetCellCenter(index, kTextureSize);
			NetIterator iterator = new NetIterator
			{
				m_TotalWeight = default(AvailabilityInfoCell),
				m_Result = default(AvailabilityInfoCell),
				m_Bounds = new Bounds3(cellCenter - new float3(1.5f * m_CellSize, 10000f, 1.5f * m_CellSize), cellCenter + new float3(1.5f * m_CellSize, 10000f, 1.5f * m_CellSize)),
				m_CellSize = m_CellSize,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_Availabilities = m_AvailabilityData
			};
			m_NetSearchTree.Iterate(ref iterator);
			AvailabilityInfoCell value = m_AvailabilityInfoMap[index];
			value.m_AvailabilityInfo = math.select(iterator.m_Result.m_AvailabilityInfo / iterator.m_TotalWeight.m_AvailabilityInfo, 0f, iterator.m_TotalWeight.m_AvailabilityInfo == 0f);
			m_AvailabilityInfoMap[index] = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
		}
	}

	public static readonly int kTextureSize = 128;

	public static readonly int kUpdatesPerDay = 32;

	private SearchSystem m_NetSearchSystem;

	private TypeHandle __TypeHandle;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<AvailabilityInfoCell>.GetCellCenter(index, kTextureSize);
	}

	public static AvailabilityInfoCell GetAvailabilityInfo(float3 position, NativeArray<AvailabilityInfoCell> AvailabilityInfoMap)
	{
		AvailabilityInfoCell result = default(AvailabilityInfoCell);
		int2 cell = CellMapSystem<AvailabilityInfoCell>.GetCell(position, CellMapSystem<AvailabilityInfoCell>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<AvailabilityInfoCell>.GetCellCoords(position, CellMapSystem<AvailabilityInfoCell>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return default(AvailabilityInfoCell);
		}
		float4 availabilityInfo = AvailabilityInfoMap[cell.x + kTextureSize * cell.y].m_AvailabilityInfo;
		float4 end = ((cell.x < kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + kTextureSize * cell.y].m_AvailabilityInfo : ((float4)0));
		float4 start = ((cell.y < kTextureSize - 1) ? AvailabilityInfoMap[cell.x + kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
		float4 end2 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
		result.m_AvailabilityInfo = math.lerp(math.lerp(availabilityInfo, end, cellCoords.x - (float)cell.x), math.lerp(start, end2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		AvailabilityInfoToGridJob jobData = new AvailabilityInfoToGridJob
		{
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_AvailabilityInfoMap = m_Map,
			m_AvailabilityData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CellSize = (float)CellMapSystem<AvailabilityInfoCell>.kMapSize / (float)kTextureSize
		};
		base.Dependency = IJobParallelForExtensions.Schedule(jobData, kTextureSize * kTextureSize, kTextureSize, JobHandle.CombineDependencies(dependencies, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, base.Dependency)));
		AddWriter(base.Dependency);
		m_NetSearchSystem.AddNetSearchTreeReader(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
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
	public AvailabilityInfoToGridSystem()
	{
	}
}
