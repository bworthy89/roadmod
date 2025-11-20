using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Common;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CountCityStoredResourceSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateCityStoredResourcesJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourceType;

		public NativeQueue<int2>.ParallelWriter m_ResourceUpdateQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<Resources> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Resources resources = dynamicBuffer[j];
					if (resources.m_Resource != Resource.NoResource && resources.m_Amount > 0)
					{
						m_ResourceUpdateQueue.Enqueue(new int2(EconomyUtils.GetResourceIndex(resources.m_Resource), resources.m_Amount));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ProcessCityUpdateResourcesQueueJob : IJob
	{
		public NativeQueue<int2> m_ResourceUpdateQueue;

		public NativeArray<int> m_CityStoredResources;

		public void Execute()
		{
			int2 item;
			while (m_ResourceUpdateQueue.TryDequeue(out item))
			{
				int x = item.x;
				int y = item.y;
				if (x >= 0 && x < m_CityStoredResources.Length)
				{
					m_CityStoredResources[x] = math.clamp(m_CityStoredResources[x] + y, 0, int.MaxValue);
				}
			}
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private NativeArray<int> m_CityStoredResources;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_CityStoredResourcesTemp;

	private JobHandle m_Handle;

	private NativeQueue<int2> m_ResourceUpdateQueue;

	private EntityQuery m_CityResourcesQuery;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public NativeArray<int> GetCityStoredResources()
	{
		return m_CityStoredResourcesTemp;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		int resourceCount = EconomyUtils.ResourceCount;
		m_CityStoredResources = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ResourceUpdateQueue = new NativeQueue<int2>(Allocator.Persistent);
		m_CityStoredResourcesTemp = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_CityResourcesQuery = GetEntityQuery(ComponentType.ReadOnly<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Citizen>(), ComponentType.Exclude<OutsideConnection>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CityResourcesQuery.IsEmptyIgnoreFilter)
		{
			m_CityStoredResourcesTemp.CopyFrom(m_CityStoredResources);
			m_CityStoredResources.Fill(0);
			m_ResourceUpdateQueue.Clear();
			JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new UpdateCityStoredResourcesJob
			{
				m_ResourceType = GetBufferTypeHandle<Resources>(isReadOnly: true),
				m_ResourceUpdateQueue = m_ResourceUpdateQueue.AsParallelWriter()
			}, m_CityResourcesQuery, JobHandle.CombineDependencies(base.Dependency, m_Handle));
			ProcessCityUpdateResourcesQueueJob jobData = new ProcessCityUpdateResourcesQueueJob
			{
				m_ResourceUpdateQueue = m_ResourceUpdateQueue,
				m_CityStoredResources = m_CityStoredResources
			};
			base.Dependency = IJobExtensions.Schedule(jobData, dependsOn);
			m_Handle = JobHandle.CombineDependencies(base.Dependency, m_Handle);
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_CityStoredResources.Dispose();
		m_ResourceUpdateQueue.Dispose();
		m_CityStoredResourcesTemp.Dispose();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CityStoredResources);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(m_CityStoredResources);
		m_CityStoredResourcesTemp.CopyFrom(m_CityStoredResources);
	}

	public void SetDefaults(Context context)
	{
		m_CityStoredResources.Fill(0);
		m_CityStoredResourcesTemp.Fill(0);
	}

	[Preserve]
	public CountCityStoredResourceSystem()
	{
	}
}
