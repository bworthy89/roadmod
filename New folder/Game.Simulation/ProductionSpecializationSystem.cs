using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Economy;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ProductionSpecializationSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	public struct ProducedResource
	{
		public Resource m_Resource;

		public int m_Amount;
	}

	[BurstCompile]
	private struct SpecializationJob : IJob
	{
		public BufferLookup<SpecializationBonus> m_Bonuses;

		public NativeQueue<ProducedResource> m_Queue;

		public Entity m_City;

		public void Execute()
		{
			DynamicBuffer<SpecializationBonus> dynamicBuffer = m_Bonuses[m_City];
			ProducedResource item;
			while (m_Queue.TryDequeue(out item))
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(item.m_Resource);
				while (dynamicBuffer.Length <= resourceIndex)
				{
					dynamicBuffer.Add(new SpecializationBonus
					{
						m_Value = 0
					});
				}
				SpecializationBonus value = dynamicBuffer[resourceIndex];
				value.m_Value += item.m_Amount;
				dynamicBuffer[resourceIndex] = value;
			}
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				SpecializationBonus value2 = dynamicBuffer[i];
				value2.m_Value = Mathf.FloorToInt(0.999f * (float)value2.m_Value);
				dynamicBuffer[i] = value2;
			}
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<SpecializationBonus> __Game_City_SpecializationBonus_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_SpecializationBonus_RW_BufferLookup = state.GetBufferLookup<SpecializationBonus>();
		}
	}

	public static readonly int kUpdatesPerDay = 512;

	private CitySystem m_CitySystem;

	private EntityQuery m_BonusQuery;

	private NativeQueue<ProducedResource> m_ProductionQueue;

	private JobHandle m_QueueWriters;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public NativeQueue<ProducedResource> GetQueue(out JobHandle deps)
	{
		deps = m_QueueWriters;
		return m_ProductionQueue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_QueueWriters = JobHandle.CombineDependencies(m_QueueWriters, handle);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_BonusQuery = GetEntityQuery(ComponentType.ReadOnly<SpecializationBonus>());
		m_ProductionQueue = new NativeQueue<ProducedResource>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ProductionQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SpecializationJob jobData = new SpecializationJob
		{
			m_Bonuses = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_SpecializationBonus_RW_BufferLookup, ref base.CheckedStateRef),
			m_Queue = m_ProductionQueue,
			m_City = m_CitySystem.City
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_QueueWriters, base.Dependency));
		m_QueueWriters = base.Dependency;
	}

	public void SetDefaults(Context context)
	{
		m_ProductionQueue.Clear();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		m_QueueWriters.Complete();
		NativeArray<int> nativeArray = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
		int num = -1;
		for (int i = 0; i < m_ProductionQueue.Count; i++)
		{
			ProducedResource producedResource = m_ProductionQueue.Dequeue();
			int resourceIndex = EconomyUtils.GetResourceIndex(producedResource.m_Resource);
			nativeArray[resourceIndex] += producedResource.m_Amount;
			num = math.max(num, resourceIndex);
		}
		writer.Write(num + 1);
		for (int j = 0; j <= num; j++)
		{
			int value = nativeArray[j];
			writer.Write(value);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_ProductionQueue.Clear();
		reader.Read(out int value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out int value2);
			if (value2 > 0)
			{
				m_ProductionQueue.Enqueue(new ProducedResource
				{
					m_Resource = EconomyUtils.GetResource(i),
					m_Amount = value2
				});
			}
		}
	}

	public void PostDeserialize(Context context)
	{
		if ((context.purpose == Purpose.NewGame || context.purpose == Purpose.LoadGame) && m_BonusQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddBuffer<SpecializationBonus>(m_CitySystem.City);
		}
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
	public ProductionSpecializationSystem()
	{
	}
}
