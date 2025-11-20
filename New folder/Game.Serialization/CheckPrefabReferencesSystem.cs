using System.Runtime.CompilerServices;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class CheckPrefabReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckPrefabReferencesJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Entity> m_PrefabArray;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PrefabData> m_PrefabData;

		public UnsafeList<bool> m_ReferencedPrefabs;

		public void Execute(int index)
		{
			if (m_ReferencedPrefabs[index])
			{
				m_PrefabData.SetComponentEnabled(m_PrefabArray[index], value: true);
				m_ReferencedPrefabs[index] = false;
			}
		}
	}

	private NativeArray<Entity> m_PrefabArray;

	private UnsafeList<bool> m_ReferencedPrefabs;

	private JobHandle m_DataDeps;

	private JobHandle m_UserDeps;

	private bool m_IsLoading;

	[Preserve]
	protected override void OnUpdate()
	{
		base.Dependency = (m_UserDeps = (m_DataDeps = IJobParallelForExtensions.Schedule(new CheckPrefabReferencesJob
		{
			m_PrefabArray = m_PrefabArray,
			m_PrefabData = GetComponentLookup<PrefabData>(),
			m_ReferencedPrefabs = m_ReferencedPrefabs
		}, m_PrefabArray.Length, 64, JobHandle.CombineDependencies(m_DataDeps, m_UserDeps, base.Dependency))));
	}

	public void BeginPrefabCheck(NativeArray<Entity> array, bool isLoading, JobHandle dependencies)
	{
		m_PrefabArray = array;
		m_ReferencedPrefabs = new UnsafeList<bool>(0, Allocator.TempJob);
		m_ReferencedPrefabs.Resize(array.Length, NativeArrayOptions.ClearMemory);
		m_DataDeps = dependencies;
		m_IsLoading = isLoading;
	}

	public void EndPrefabCheck(out JobHandle dependencies)
	{
		dependencies = JobHandle.CombineDependencies(m_DataDeps, m_UserDeps);
		m_ReferencedPrefabs.Dispose(dependencies);
		m_PrefabArray = default(NativeArray<Entity>);
		m_ReferencedPrefabs = default(UnsafeList<bool>);
		m_DataDeps = default(JobHandle);
		m_UserDeps = default(JobHandle);
	}

	public PrefabReferences GetPrefabReferences(SystemBase system, out JobHandle dependencies)
	{
		dependencies = m_DataDeps;
		return new PrefabReferences(m_PrefabArray, m_ReferencedPrefabs, system.GetComponentLookup<PrefabData>(isReadOnly: true), m_IsLoading);
	}

	public void AddPrefabReferencesUser(JobHandle dependencies)
	{
		m_UserDeps = JobHandle.CombineDependencies(m_UserDeps, dependencies);
	}

	[Preserve]
	public CheckPrefabReferencesSystem()
	{
	}
}
