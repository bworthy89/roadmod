using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Net;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ResetBuildOrderSystem : GameSystemBase
{
	[BurstCompile]
	private struct ResetBuildOrderJob : IJob
	{
		private struct OrderItem : IComparable<OrderItem>
		{
			public uint m_Min;

			public uint m_Max;

			public OrderItem(uint min, uint max)
			{
				m_Min = min;
				m_Max = max;
			}

			public OrderItem(Game.Net.BuildOrder buildOrder)
			{
				m_Min = math.min(buildOrder.m_Start, buildOrder.m_End);
				m_Max = math.max(buildOrder.m_Start, buildOrder.m_End);
			}

			public OrderItem(Game.Zones.BuildOrder buildOrder)
			{
				m_Min = buildOrder.m_Order;
				m_Max = buildOrder.m_Order;
			}

			public int CompareTo(OrderItem other)
			{
				return math.select(0, math.select(-1, 1, m_Min > other.m_Min), m_Min != other.m_Min);
			}
		}

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public ComponentTypeHandle<Game.Net.BuildOrder> m_NetBuildOrderType;

		public ComponentTypeHandle<Game.Zones.BuildOrder> m_ZoneBuildOrderType;

		public NativeValue<uint> m_BuildOrder;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			if (num != 0)
			{
				NativeArray<OrderItem> array = new NativeArray<OrderItem>(num, Allocator.Temp);
				NativeParallelHashMap<uint, uint> nativeParallelHashMap = new NativeParallelHashMap<uint, uint>(num, Allocator.Temp);
				num = 0;
				for (int j = 0; j < m_Chunks.Length; j++)
				{
					ArchetypeChunk archetypeChunk = m_Chunks[j];
					NativeArray<Game.Net.BuildOrder> nativeArray = archetypeChunk.GetNativeArray(ref m_NetBuildOrderType);
					NativeArray<Game.Zones.BuildOrder> nativeArray2 = archetypeChunk.GetNativeArray(ref m_ZoneBuildOrderType);
					for (int k = 0; k < nativeArray.Length; k++)
					{
						array[num++] = new OrderItem(nativeArray[k]);
					}
					for (int l = 0; l < nativeArray2.Length; l++)
					{
						array[num++] = new OrderItem(nativeArray2[l]);
					}
				}
				array.Sort();
				OrderItem orderItem = array[0];
				OrderItem orderItem2 = new OrderItem(0u, orderItem.m_Max - orderItem.m_Min);
				nativeParallelHashMap.TryAdd(orderItem.m_Min, orderItem2.m_Min);
				for (int m = 1; m < num; m++)
				{
					OrderItem orderItem3 = array[m];
					if (orderItem3.m_Min <= orderItem.m_Max)
					{
						if (orderItem3.m_Max > orderItem.m_Max)
						{
							orderItem.m_Max = orderItem3.m_Max;
							orderItem2.m_Max = orderItem2.m_Min + (orderItem3.m_Max - orderItem.m_Min);
						}
						if (orderItem3.m_Min > orderItem.m_Min)
						{
							nativeParallelHashMap.TryAdd(orderItem3.m_Min, orderItem2.m_Min + (orderItem3.m_Min - orderItem.m_Min));
						}
					}
					else
					{
						orderItem = orderItem3;
						orderItem2.m_Min = orderItem2.m_Max + 1;
						orderItem2.m_Max = orderItem2.m_Min + (orderItem3.m_Max - orderItem3.m_Min);
						nativeParallelHashMap.TryAdd(orderItem3.m_Min, orderItem2.m_Min);
					}
				}
				Game.Net.BuildOrder value = default(Game.Net.BuildOrder);
				for (int n = 0; n < m_Chunks.Length; n++)
				{
					ArchetypeChunk archetypeChunk2 = m_Chunks[n];
					NativeArray<Game.Net.BuildOrder> nativeArray3 = archetypeChunk2.GetNativeArray(ref m_NetBuildOrderType);
					NativeArray<Game.Zones.BuildOrder> nativeArray4 = archetypeChunk2.GetNativeArray(ref m_ZoneBuildOrderType);
					for (int num2 = 0; num2 < nativeArray3.Length; num2++)
					{
						Game.Net.BuildOrder buildOrder = nativeArray3[num2];
						if (buildOrder.m_End >= buildOrder.m_Start)
						{
							value.m_Start = nativeParallelHashMap[buildOrder.m_Start];
							value.m_End = value.m_Start + (buildOrder.m_End - buildOrder.m_Start);
						}
						else
						{
							value.m_End = nativeParallelHashMap[buildOrder.m_End];
							value.m_Start = value.m_End + (buildOrder.m_Start - buildOrder.m_End);
						}
						nativeArray3[num2] = value;
					}
					for (int num3 = 0; num3 < nativeArray4.Length; num3++)
					{
						Game.Zones.BuildOrder value2 = nativeArray4[num3];
						value2.m_Order = nativeParallelHashMap[value2.m_Order];
						nativeArray4[num3] = value2;
					}
				}
				m_BuildOrder.value = orderItem2.m_Max + 1;
			}
			else
			{
				m_BuildOrder.value = 0u;
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<Game.Net.BuildOrder> __Game_Net_BuildOrder_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Zones.BuildOrder> __Game_Zones_BuildOrder_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_BuildOrder_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.BuildOrder>();
			__Game_Zones_BuildOrder_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Zones.BuildOrder>();
		}
	}

	private GenerateEdgesSystem m_GenerateEdgesSystem;

	private EntityQuery m_BuildOrderQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GenerateEdgesSystem = base.World.GetOrCreateSystemManaged<GenerateEdgesSystem>();
		m_BuildOrderQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Net.BuildOrder>(),
				ComponentType.ReadOnly<Game.Zones.BuildOrder>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_BuildOrderQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new ResetBuildOrderJob
		{
			m_Chunks = chunks,
			m_NetBuildOrderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_BuildOrder_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneBuildOrderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_BuildOrder_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildOrder = m_GenerateEdgesSystem.GetBuildOrder()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
		base.Dependency = jobHandle;
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
	public ResetBuildOrderSystem()
	{
	}
}
