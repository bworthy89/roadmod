using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TransportUsageTrackSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct TransportUsageTrackJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<UnlockFilterData> m_UnlockFilterDatas;

		public NativeQueue<TransportUsageEvent> m_TransportUsageQueue;

		public NativeHashMap<Entity, TransportUsageData> m_BuildingPrefabTransportUsageData;

		public NativeHashMap<int, TransportUsageData> m_FilteredTransportUsageData;

		public void Execute()
		{
			TransportUsageEvent item;
			while (m_TransportUsageQueue.TryDequeue(out item))
			{
				if (!m_PrefabRefs.TryGetComponent(item.m_Building, out var componentData))
				{
					continue;
				}
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				switch (item.m_TransportType)
				{
				case TransportType.Airplane:
					num5 = item.m_TransportedCargo;
					num6 = item.m_TransportedPassenger;
					break;
				case TransportType.Ship:
					num3 = item.m_TransportedCargo;
					num4 = item.m_TransportedPassenger;
					break;
				case TransportType.Train:
					num = item.m_TransportedCargo;
					num2 = item.m_TransportedPassenger;
					break;
				}
				if (m_BuildingPrefabTransportUsageData.TryGetValue(componentData.m_Prefab, out var item2))
				{
					item2.m_TrainTransportCargo += num;
					item2.m_TrainTransportPassenger += num2;
					item2.m_AirplaneTransportCargo += num5;
					item2.m_AirplaneTransportPassenger += num6;
					item2.m_ShipTransportCargo += num3;
					item2.m_ShipTransportPassenger += num4;
					m_BuildingPrefabTransportUsageData[componentData.m_Prefab] = item2;
				}
				else
				{
					m_BuildingPrefabTransportUsageData.Add(componentData.m_Prefab, new TransportUsageData
					{
						m_TrainTransportCargo = num,
						m_TrainTransportPassenger = num2,
						m_AirplaneTransportCargo = num5,
						m_AirplaneTransportPassenger = num6,
						m_ShipTransportCargo = num3,
						m_ShipTransportPassenger = num4
					});
				}
				if (m_UnlockFilterDatas.TryGetComponent(componentData.m_Prefab, out var componentData2) && componentData2.m_UnlockUniqueID != 0)
				{
					if (m_FilteredTransportUsageData.TryGetValue(componentData2.m_UnlockUniqueID, out var _))
					{
						item2.m_TrainTransportCargo += num;
						item2.m_TrainTransportPassenger += num2;
						item2.m_AirplaneTransportCargo += num5;
						item2.m_AirplaneTransportPassenger += num6;
						item2.m_ShipTransportCargo += num3;
						item2.m_ShipTransportPassenger += num4;
						m_FilteredTransportUsageData[componentData2.m_UnlockUniqueID] = item2;
					}
					else
					{
						m_FilteredTransportUsageData.Add(componentData2.m_UnlockUniqueID, new TransportUsageData
						{
							m_TrainTransportCargo = num,
							m_TrainTransportPassenger = num2,
							m_AirplaneTransportCargo = num5,
							m_AirplaneTransportPassenger = num6,
							m_ShipTransportCargo = num3,
							m_ShipTransportPassenger = num4
						});
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnlockFilterData> __Game_Prefabs_UnlockFilterData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_UnlockFilterData_RO_ComponentLookup = state.GetComponentLookup<UnlockFilterData>(isReadOnly: true);
		}
	}

	private NativeHashMap<Entity, TransportUsageData> m_BuildingPrefabTransportUsageData;

	private NativeQueue<TransportUsageEvent> m_TransportUsageQueue;

	private NativeHashMap<int, TransportUsageData> m_FilteredTransportUsageData;

	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private TypeHandle __TypeHandle;

	public NativeQueue<TransportUsageEvent> GetQueue(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_TransportUsageQueue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, handle);
	}

	public NativeHashMap<Entity, TransportUsageData> GetBuildingPrefabTransportUsageData(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_BuildingPrefabTransportUsageData;
	}

	public NativeHashMap<int, TransportUsageData> GetFilteredTransportUsageData(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_FilteredTransportUsageData;
	}

	public void AddTransportUsageDataReader(JobHandle handle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, handle);
	}

	public void PatchReferences(ref PrefabReferences references)
	{
		NativeKeyValueArrays<Entity, TransportUsageData> keyValueArrays = m_BuildingPrefabTransportUsageData.GetKeyValueArrays(Allocator.Temp);
		m_BuildingPrefabTransportUsageData.Clear();
		for (int i = 0; i < keyValueArrays.Length; i++)
		{
			Entity key = references.Check(base.EntityManager, keyValueArrays.Keys[i]);
			m_BuildingPrefabTransportUsageData.Add(key, keyValueArrays.Values[i]);
		}
		keyValueArrays.Dispose();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildingPrefabTransportUsageData = new NativeHashMap<Entity, TransportUsageData>(1, Allocator.Persistent);
		m_FilteredTransportUsageData = new NativeHashMap<int, TransportUsageData>(1, Allocator.Persistent);
		m_TransportUsageQueue = new NativeQueue<TransportUsageEvent>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_BuildingPrefabTransportUsageData.Dispose();
		m_FilteredTransportUsageData.Dispose();
		m_TransportUsageQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TransportUsageTrackJob jobData = new TransportUsageTrackJob
		{
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnlockFilterDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UnlockFilterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportUsageQueue = m_TransportUsageQueue,
			m_BuildingPrefabTransportUsageData = m_BuildingPrefabTransportUsageData,
			m_FilteredTransportUsageData = m_FilteredTransportUsageData
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency));
		m_WriteDependencies = base.Dependency;
		m_ReadDependencies = default(JobHandle);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_BuildingPrefabTransportUsageData.Count;
		writer.Write(value);
		NativeHashMap<Entity, TransportUsageData>.Enumerator enumerator = m_BuildingPrefabTransportUsageData.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Entity key = enumerator.Current.Key;
			writer.Write(key);
			TransportUsageData value2 = enumerator.Current.Value;
			writer.Write(value2);
		}
		enumerator.Dispose();
		byte value3 = (byte)m_FilteredTransportUsageData.Count;
		writer.Write(value3);
		NativeHashMap<int, TransportUsageData>.Enumerator enumerator2 = m_FilteredTransportUsageData.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			int key2 = enumerator2.Current.Key;
			writer.Write(key2);
			TransportUsageData value4 = enumerator2.Current.Value;
			writer.Write(value4);
		}
		enumerator2.Dispose();
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_BuildingPrefabTransportUsageData.Clear();
		reader.Read(out byte value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			reader.Read(out TransportUsageData value3);
			m_BuildingPrefabTransportUsageData.Add(value2, value3);
		}
		m_FilteredTransportUsageData.Clear();
		reader.Read(out value);
		for (int j = 0; j < value; j++)
		{
			reader.Read(out int value4);
			reader.Read(out TransportUsageData value5);
			m_FilteredTransportUsageData.Add(value4, value5);
		}
	}

	public void SetDefaults(Context context)
	{
		m_BuildingPrefabTransportUsageData.Clear();
		m_FilteredTransportUsageData.Clear();
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
	public TransportUsageTrackSystem()
	{
	}
}
