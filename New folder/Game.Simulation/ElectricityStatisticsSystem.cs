using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityStatisticsSystem : GameSystemBase, IElectricityStatisticsSystem, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CountElectricityProductionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ElectricityProducer> m_ProducerType;

		public NativePerThreadSumInt.Concurrent m_Production;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ElectricityProducer> nativeArray = chunk.GetNativeArray(ref m_ProducerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ElectricityProducer electricityProducer = nativeArray[i];
				m_Production.Add(electricityProducer.m_Capacity);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountElectricityConsumptionJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> m_ConsumerType;

		public NativePerThreadSumInt.Concurrent m_Consumption;

		public NativePerThreadSumInt.Concurrent m_FulfilledConsumption;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ElectricityConsumer> nativeArray = chunk.GetNativeArray(ref m_ConsumerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ElectricityConsumer electricityConsumer = nativeArray[i];
				m_Consumption.Add(electricityConsumer.m_WantedConsumption);
				m_FulfilledConsumption.Add(electricityConsumer.m_FulfilledConsumption);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CountBatteryCapacityJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Battery> m_BatteryType;

		public NativePerThreadSumInt.Concurrent m_Charge;

		public NativePerThreadSumInt.Concurrent m_Capacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Battery> nativeArray = chunk.GetNativeArray(ref m_BatteryType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Battery battery = nativeArray[i];
				m_Charge.Add(battery.storedEnergyHours);
				m_Capacity.Add(battery.m_Capacity);
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
		public ComponentTypeHandle<ElectricityProducer> __Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Battery> __Game_Buildings_Battery_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityProducer>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_Battery_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Battery>(isReadOnly: true);
		}
	}

	private EntityQuery m_ProducerGroup;

	private EntityQuery m_ConsumerGroup;

	private EntityQuery m_BatteryGroup;

	private NativePerThreadSumInt m_Production;

	private NativePerThreadSumInt m_Consumption;

	private NativePerThreadSumInt m_FulfilledConsumption;

	private NativePerThreadSumInt m_BatteryCharge;

	private NativePerThreadSumInt m_BatteryCapacity;

	private int m_LastProduction;

	private int m_LastConsumption;

	private int m_LastFulfilledConsumption;

	private int m_LastBatteryCharge;

	private int m_LastBatteryCapacity;

	private TypeHandle __TypeHandle;

	public int production => m_LastProduction;

	public int consumption => m_LastConsumption;

	public int fulfilledConsumption => m_LastFulfilledConsumption;

	public int batteryCharge => m_LastBatteryCharge;

	public int batteryCapacity => m_LastBatteryCapacity;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 127;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ProducerGroup = GetEntityQuery(ComponentType.ReadOnly<ElectricityProducer>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ConsumerGroup = GetEntityQuery(ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_BatteryGroup = GetEntityQuery(ComponentType.ReadOnly<Battery>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_Production = new NativePerThreadSumInt(Allocator.Persistent);
		m_Consumption = new NativePerThreadSumInt(Allocator.Persistent);
		m_FulfilledConsumption = new NativePerThreadSumInt(Allocator.Persistent);
		m_BatteryCharge = new NativePerThreadSumInt(Allocator.Persistent);
		m_BatteryCapacity = new NativePerThreadSumInt(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Production.Dispose();
		m_Consumption.Dispose();
		m_FulfilledConsumption.Dispose();
		m_BatteryCharge.Dispose();
		m_BatteryCapacity.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LastProduction;
		writer.Write(value);
		int value2 = m_LastConsumption;
		writer.Write(value2);
		int value3 = m_LastFulfilledConsumption;
		writer.Write(value3);
		int value4 = m_LastBatteryCharge;
		writer.Write(value4);
		int value5 = m_LastBatteryCapacity;
		writer.Write(value5);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.electricityStats)
		{
			ref int value = ref m_LastProduction;
			reader.Read(out value);
			ref int value2 = ref m_LastConsumption;
			reader.Read(out value2);
			ref int value3 = ref m_LastFulfilledConsumption;
			reader.Read(out value3);
			if (reader.context.version >= Version.batteryStats)
			{
				ref int value4 = ref m_LastBatteryCharge;
				reader.Read(out value4);
				ref int value5 = ref m_LastBatteryCapacity;
				reader.Read(out value5);
			}
		}
		else if (reader.context.version > Version.seekerReferences)
		{
			reader.Read(out float _);
			ref int value7 = ref m_LastConsumption;
			reader.Read(out value7);
			ref int value8 = ref m_LastProduction;
			reader.Read(out value8);
			reader.Read(out int _);
			if (reader.context.version > Version.transmittedElectricity)
			{
				ref int value10 = ref m_LastFulfilledConsumption;
				reader.Read(out value10);
			}
		}
	}

	public void SetDefaults(Context context)
	{
		m_LastProduction = 0;
		m_LastConsumption = 0;
		m_LastFulfilledConsumption = 0;
		m_LastBatteryCharge = 0;
		m_LastBatteryCapacity = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_LastProduction = m_Production.Count;
		m_LastConsumption = m_Consumption.Count;
		m_LastFulfilledConsumption = m_FulfilledConsumption.Count;
		m_LastBatteryCharge = m_BatteryCharge.Count;
		m_LastBatteryCapacity = m_BatteryCapacity.Count;
		m_Production.Count = 0;
		m_Consumption.Count = 0;
		m_FulfilledConsumption.Count = 0;
		m_BatteryCharge.Count = 0;
		m_BatteryCapacity.Count = 0;
		JobHandle job = default(JobHandle);
		if (!m_ProducerGroup.IsEmptyIgnoreFilter)
		{
			job = JobChunkExtensions.ScheduleParallel(new CountElectricityProductionJob
			{
				m_ProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Production = m_Production.ToConcurrent()
			}, m_ProducerGroup, base.Dependency);
		}
		JobHandle job2 = default(JobHandle);
		if (!m_ConsumerGroup.IsEmptyIgnoreFilter)
		{
			job2 = JobChunkExtensions.ScheduleParallel(new CountElectricityConsumptionJob
			{
				m_ConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Consumption = m_Consumption.ToConcurrent(),
				m_FulfilledConsumption = m_FulfilledConsumption.ToConcurrent()
			}, m_ConsumerGroup, base.Dependency);
		}
		JobHandle job3 = default(JobHandle);
		if (!m_ConsumerGroup.IsEmptyIgnoreFilter)
		{
			job3 = JobChunkExtensions.ScheduleParallel(new CountBatteryCapacityJob
			{
				m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Battery_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Charge = m_BatteryCharge.ToConcurrent(),
				m_Capacity = m_BatteryCapacity.ToConcurrent()
			}, m_BatteryGroup, base.Dependency);
		}
		base.Dependency = JobHandle.CombineDependencies(job, job2, job3);
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
	public ElectricityStatisticsSystem()
	{
	}
}
