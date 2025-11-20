using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Debug;
using Game.Objects;
using Game.Prefabs;
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
public class CountStudyPositionsSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CountStudyPositionsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_SchoolChunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Student> m_StudentType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_UpgradeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabDatas;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolDatas;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		public NativeArray<int> m_StudyPositionByEducation;

		public void Execute()
		{
			for (int i = 0; i < 5; i++)
			{
				m_StudyPositionByEducation[i] = 0;
			}
			for (int j = 0; j < m_SchoolChunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_SchoolChunks[j];
				BufferAccessor<Student> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_StudentType);
				NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
				BufferAccessor<InstalledUpgrade> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_UpgradeType);
				bool flag = bufferAccessor2.Length != 0;
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					Entity prefab = nativeArray[k].m_Prefab;
					if (!m_OutsideConnectionDatas.HasComponent(prefab) && m_SchoolDatas.HasComponent(prefab))
					{
						SchoolData data = m_SchoolDatas[prefab];
						if (flag)
						{
							UpgradeUtils.CombineStats(ref data, bufferAccessor2[k], ref m_PrefabDatas, ref m_SchoolDatas);
						}
						DynamicBuffer<Student> dynamicBuffer = bufferAccessor[k];
						m_StudyPositionByEducation[m_SchoolDatas[prefab].m_EducationLevel] += math.max(0, data.m_StudentCapacity / 2 - dynamicBuffer.Length);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Student> __Game_Buildings_Student_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Student_RO_BufferTypeHandle = state.GetBufferTypeHandle<Student>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
		}
	}

	private EntityQuery m_SchoolQuery;

	[DebugWatchValue]
	private NativeArray<int> m_StudyPositionByEducation;

	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public NativeArray<int> GetStudyPositionsByEducation(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_StudyPositionByEducation;
	}

	public void AddReader(JobHandle reader)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SchoolQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.School>(), ComponentType.ReadOnly<Student>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_StudyPositionByEducation = new NativeArray<int>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_StudyPositionByEducation.Dispose();
		base.OnDestroy();
	}

	public void SetDefaults(Context context)
	{
		for (int i = 0; i < 5; i++)
		{
			m_StudyPositionByEducation[i] = 0;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_StudyPositionByEducation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.economyFix)
		{
			reader.Read(out int _);
			return;
		}
		NativeArray<int> value2 = m_StudyPositionByEducation;
		reader.Read(value2);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		CountStudyPositionsJob jobData = new CountStudyPositionsJob
		{
			m_SchoolChunks = m_SchoolQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_StudentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Student_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SchoolDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StudyPositionByEducation = m_StudyPositionByEducation
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle));
		m_WriteDependencies = base.Dependency;
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
	public CountStudyPositionsSystem()
	{
	}
}
