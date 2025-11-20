using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Rendering;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class BatchMeshSystem : GameSystemBase
{
	private struct LoadingData : IComparable<LoadingData>
	{
		public int m_Priority;

		public int m_BatchIndex;

		public LoadingData(int priority, int batchIndex)
		{
			m_Priority = priority;
			m_BatchIndex = batchIndex;
		}

		public int CompareTo(LoadingData other)
		{
			return other.m_Priority - m_Priority;
		}
	}

	private struct MeshInfo
	{
		public RenderPrefab m_Prefab;

		public ShapeAllocation[] m_ShapeAllocations;

		public OverlayAllocation[] m_OverlayAllocations;

		public CullAllocation[] m_CullAllocations;

		public ulong m_SizeInMemory;

		public int m_GeneratedIndex;

		public int m_BatchCount;
	}

	private struct CacheInfo
	{
		public GeometryAsset m_GeometryAsset;

		public EntityCommandBuffer m_CommandBuffer;

		public JobHandle m_Dependency;
	}

	private struct ShapeAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_Stride;

		public float3 m_PositionExtent;

		public float3 m_NormalExtent;
	}

	private struct CullAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_StartOffset;

		public int m_Stride;

		public int m_Size;

		public int m_Offset;
	}

	private struct OverlayAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_Stride;
	}

	[BurstCompile]
	private struct LoadingPriorityJob : IJob
	{
		[ReadOnly]
		public int m_PriorityLimit;

		public NativeList<int> m_BatchPriority;

		public NativeList<MeshLoadingState> m_LoadingState;

		public NativeList<LoadingData> m_LoadingData;

		public NativeList<LoadingData> m_UnloadingData;

		public void Execute()
		{
			for (int i = 0; i < m_LoadingData.Length; i++)
			{
				ref LoadingData reference = ref m_LoadingData.ElementAt(i);
				ref int reference2 = ref m_BatchPriority.ElementAt(reference.m_BatchIndex);
				ref MeshLoadingState reference3 = ref m_LoadingState.ElementAt(reference.m_BatchIndex);
				if (reference3 == MeshLoadingState.Pending)
				{
					if (reference2 < m_PriorityLimit)
					{
						reference3 = MeshLoadingState.None;
						m_LoadingData.RemoveAtSwapBack(i--);
					}
					else
					{
						reference.m_Priority = reference2;
					}
				}
				else
				{
					reference.m_Priority = 1000256 + reference2;
				}
			}
			for (int j = 0; j < m_UnloadingData.Length; j++)
			{
				ref LoadingData reference4 = ref m_UnloadingData.ElementAt(j);
				ref int reference5 = ref m_BatchPriority.ElementAt(reference4.m_BatchIndex);
				ref MeshLoadingState reference6 = ref m_LoadingState.ElementAt(reference4.m_BatchIndex);
				if (reference6 == MeshLoadingState.Obsolete)
				{
					if (reference5 >= m_PriorityLimit)
					{
						reference6 = MeshLoadingState.Complete;
						m_UnloadingData.RemoveAtSwapBack(j--);
					}
					else
					{
						reference4.m_Priority = -reference5;
					}
				}
				else
				{
					reference4.m_Priority = 1000256 - reference5;
				}
			}
			for (int k = 0; k < m_BatchPriority.Length; k++)
			{
				ref int reference7 = ref m_BatchPriority.ElementAt(k);
				ref MeshLoadingState reference8 = ref m_LoadingState.ElementAt(k);
				if (reference7 >= m_PriorityLimit)
				{
					if (reference8 == MeshLoadingState.None)
					{
						reference8 = MeshLoadingState.Pending;
						m_LoadingData.Add(new LoadingData(reference7, k));
					}
					reference7 -= 256;
				}
				else
				{
					if (reference8 == MeshLoadingState.Complete)
					{
						reference8 = MeshLoadingState.Obsolete;
						m_UnloadingData.Add(new LoadingData(-reference7, k));
					}
					reference7 = math.max(-1000000, reference7 - 1);
				}
			}
			if (m_LoadingData.Length >= 2)
			{
				m_LoadingData.Sort();
			}
			if (m_UnloadingData.Length >= 2)
			{
				m_UnloadingData.Sort();
			}
		}
	}

	public const string kDisableMeshLoadingKey = "bh.devtools.disableMeshLoadingKey";

	public const string kForceMeshUnloadingKey = "bh.devtools.forceMeshUnloadingKey";

	public const uint MAX_LOADING_COUNT = 30u;

	public const int MIN_BATCH_PRIORITY = -1000000;

	public const int SHAPEBUFFER_ELEMENT_SIZE = 8;

	public const uint SHAPEBUFFER_MEMORY_DEFAULT = 33554432u;

	public const uint SHAPEBUFFER_MEMORY_INCREMENT = 8388608u;

	public const int CULLBUFFER_ELEMENT_SIZE = 4;

	public const uint CULLBUFFER_MEMORY_DEFAULT = 33554432u;

	public const uint CULLBUFFER_MEMORY_INCREMENT = 4194304u;

	public const uint OVERLAYBUFFER_MEMORY_DEFAULT = 65536u;

	public uint OVERLAYBUFFER_MEMORY_INCREMENT = (uint)(OverlayAtlasElement.SizeOf * 64 * 1024);

	public const ulong DEFAULT_MEMORY_BUDGET = 1610612736uL;

	public const bool DEFAULT_MEMORY_BUDGET_IS_STRICT = false;

	private GeometryAssetLoadingSystem m_GeometryLoadingSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private PrefabSystem m_PrefabSystem;

	private Mesh m_DefaultObjectMesh;

	private Mesh m_DefaultBaseMesh;

	private Mesh m_DefaultLaneMesh;

	private Mesh m_ZoneBlockMesh;

	private Mesh m_ZoneLodMesh;

	private Mesh m_DefaultEdgeMesh;

	private Mesh m_DefaultNodeMesh;

	private Mesh m_DefaultRoundaboutMesh;

	private List<Mesh> m_GeneratedMeshes;

	private List<int> m_FreeMeshIndices;

	private HashSet<GeometryAsset> m_UnloadGeometryAssets;

	private HashSet<GeometryAsset> m_LoadingGeometries;

	private Dictionary<Entity, CacheInfo> m_CachingMeshes;

	private Dictionary<Entity, MeshInfo> m_MeshInfos;

	private NativeList<int> m_BatchPriority;

	private NativeList<MeshLoadingState> m_LoadingState;

	private NativeList<LoadingData> m_LoadingData;

	private NativeList<LoadingData> m_UnloadingData;

	private NativeList<Entity> m_GenerateMeshEntities;

	private NativeHeapAllocator m_ShapeAllocator;

	private NativeHeapAllocator m_CullAllocator;

	private NativeHeapAllocator m_OverlayAllocator;

	private JobHandle m_PriorityDeps;

	private JobHandle m_StateDeps;

	private JobHandle m_GenerateMeshDeps;

	private GraphicsBuffer m_ShapeBuffer;

	private GraphicsBuffer m_CullBuffer;

	private GraphicsBuffer m_OverlayBuffer;

	private Mesh.MeshDataArray m_GenerateMeshDataArray;

	private int m_ShapeCount;

	private int m_CullCount;

	private int m_OverlayCount;

	private int m_PriorityLimit;

	private bool m_AddMeshes;

	public ulong memoryBudget { get; set; }

	public bool strictMemoryBudget { get; set; }

	public bool enableMeshLoading { get; set; }

	public bool forceMeshUnloading { get; set; }

	public ulong totalSizeInMemory { get; private set; }

	public int loadedMeshCount => m_MeshInfos.Count;

	public int loadingRemaining { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GeometryLoadingSystem = base.World.GetOrCreateSystemManaged<GeometryAssetLoadingSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_GeneratedMeshes = new List<Mesh>();
		m_FreeMeshIndices = new List<int>();
		m_UnloadGeometryAssets = new HashSet<GeometryAsset>();
		m_LoadingGeometries = new HashSet<GeometryAsset>();
		m_CachingMeshes = new Dictionary<Entity, CacheInfo>();
		m_MeshInfos = new Dictionary<Entity, MeshInfo>();
		m_BatchPriority = new NativeList<int>(100, Allocator.Persistent);
		m_LoadingState = new NativeList<MeshLoadingState>(100, Allocator.Persistent);
		m_LoadingData = new NativeList<LoadingData>(100, Allocator.Persistent);
		m_UnloadingData = new NativeList<LoadingData>(100, Allocator.Persistent);
		m_ShapeAllocator = new NativeHeapAllocator(4194304u, 1u, Allocator.Persistent);
		m_CullAllocator = new NativeHeapAllocator(8388608u, 1u, Allocator.Persistent);
		m_OverlayAllocator = new NativeHeapAllocator(65536u / (uint)OverlayAtlasElement.SizeOf, 1u, Allocator.Persistent);
		m_ShapeAllocator.Allocate(1u);
		ResizeShapeBuffer();
		m_CullAllocator.Allocate(1u);
		ResizeCullBuffer();
		m_OverlayAllocator.Allocate(1u);
		ResizeOverlayBuffer();
		memoryBudget = 1610612736uL;
		strictMemoryBudget = false;
		enableMeshLoading = true;
		forceMeshUnloading = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_DefaultObjectMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultObjectMesh);
		}
		if (m_DefaultBaseMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultBaseMesh);
		}
		if (m_DefaultLaneMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultLaneMesh);
		}
		if (m_ZoneBlockMesh != null)
		{
			UnityEngine.Object.Destroy(m_ZoneBlockMesh);
		}
		if (m_ZoneLodMesh != null)
		{
			UnityEngine.Object.Destroy(m_ZoneLodMesh);
		}
		if (m_DefaultEdgeMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultEdgeMesh);
		}
		if (m_DefaultNodeMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultNodeMesh);
		}
		if (m_DefaultRoundaboutMesh != null)
		{
			UnityEngine.Object.Destroy(m_DefaultRoundaboutMesh);
		}
		for (int i = 0; i < m_GeneratedMeshes.Count; i++)
		{
			UnityEngine.Object.Destroy(m_GeneratedMeshes[i]);
		}
		m_GeneratedMeshes.Clear();
		foreach (GeometryAsset item in m_LoadingGeometries)
		{
			item.UnloadPartial();
		}
		m_LoadingGeometries.Clear();
		UnloadMeshAndGeometryAssets();
		m_PriorityDeps.Complete();
		m_StateDeps.Complete();
		foreach (KeyValuePair<Entity, CacheInfo> item2 in m_CachingMeshes)
		{
			item2.Value.m_Dependency.Complete();
			item2.Value.m_CommandBuffer.Dispose();
			if (item2.Value.m_GeometryAsset != null)
			{
				item2.Value.m_GeometryAsset.UnloadPartial();
			}
		}
		foreach (KeyValuePair<Entity, MeshInfo> item3 in m_MeshInfos)
		{
			if (item3.Value.m_Prefab != null)
			{
				for (int j = 0; j < item3.Value.m_BatchCount; j++)
				{
					item3.Value.m_Prefab.ReleaseMeshes();
				}
			}
		}
		if (m_ShapeBuffer != null)
		{
			m_ShapeBuffer.Release();
			m_ShapeBuffer = null;
		}
		if (m_CullBuffer != null)
		{
			m_CullBuffer.Release();
			m_CullBuffer = null;
		}
		if (m_OverlayBuffer != null)
		{
			m_OverlayBuffer.Release();
			m_OverlayBuffer = null;
		}
		if (m_GenerateMeshEntities.IsCreated)
		{
			m_GenerateMeshDeps.Complete();
			m_GenerateMeshEntities.Dispose();
			m_GenerateMeshDataArray.Dispose();
		}
		m_BatchPriority.Dispose();
		m_LoadingState.Dispose();
		m_LoadingData.Dispose();
		m_UnloadingData.Dispose();
		m_ShapeAllocator.Dispose();
		m_CullAllocator.Dispose();
		m_OverlayAllocator.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ulong loadingMemorySize = 0uL;
		ulong neededMemorySize = 0uL;
		LoadMeshes(ref loadingMemorySize, ref neededMemorySize);
		UnloadMeshes(loadingMemorySize, neededMemorySize);
		GenerateMeshes();
	}

	public void ReplaceMesh(Entity oldMesh, Entity newMesh)
	{
		if (m_GenerateMeshEntities.IsCreated)
		{
			for (int i = 0; i < m_GenerateMeshEntities.Length; i++)
			{
				if (m_GenerateMeshEntities[i] == oldMesh)
				{
					m_GenerateMeshEntities[i] = newMesh;
					break;
				}
			}
		}
		CompleteCaching(oldMesh);
		TryCopyBuffer<MeshVertex>(oldMesh, newMesh);
		TryCopyBuffer<MeshIndex>(oldMesh, newMesh);
		TryCopyBuffer<MeshNode>(oldMesh, newMesh);
		TryCopyBuffer<MeshNormal>(oldMesh, newMesh);
		if (m_MeshInfos.TryGetValue(oldMesh, out var value))
		{
			value.m_Prefab = m_PrefabSystem.GetPrefab<RenderPrefab>(newMesh);
			m_MeshInfos.Add(newMesh, value);
			m_MeshInfos.Remove(oldMesh);
		}
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		int batchCount = managedBatches.BatchCount;
		for (int j = 0; j < batchCount; j++)
		{
			((CustomBatch)managedBatches.GetBatch(j))?.ReplaceMesh(oldMesh, newMesh);
		}
	}

	private void TryCopyBuffer<T>(Entity source, Entity target) where T : unmanaged, IBufferElementData
	{
		if (base.EntityManager.HasBuffer<T>(source))
		{
			base.EntityManager.AddBuffer<T>(target).CopyFrom(base.EntityManager.GetBuffer<T>(source));
		}
	}

	public Mesh GetDefaultMesh(MeshType type, BatchFlags flags, GeneratedType generatedType)
	{
		switch (type)
		{
		case MeshType.Object:
			if (generatedType == GeneratedType.ObjectBase)
			{
				if (m_DefaultBaseMesh == null)
				{
					m_DefaultBaseMesh = ObjectMeshHelpers.CreateDefaultBaseMesh();
				}
				return m_DefaultBaseMesh;
			}
			if (m_DefaultObjectMesh == null)
			{
				m_DefaultObjectMesh = ObjectMeshHelpers.CreateDefaultMesh();
			}
			return m_DefaultObjectMesh;
		case MeshType.Net:
			if ((flags & BatchFlags.Roundabout) != 0)
			{
				if (m_DefaultRoundaboutMesh == null)
				{
					m_DefaultRoundaboutMesh = NetMeshHelpers.CreateDefaultRoundaboutMesh();
				}
				return m_DefaultRoundaboutMesh;
			}
			if ((flags & BatchFlags.Node) != 0)
			{
				if (m_DefaultNodeMesh == null)
				{
					m_DefaultNodeMesh = NetMeshHelpers.CreateDefaultNodeMesh();
				}
				return m_DefaultNodeMesh;
			}
			if (m_DefaultEdgeMesh == null)
			{
				m_DefaultEdgeMesh = NetMeshHelpers.CreateDefaultEdgeMesh();
			}
			return m_DefaultEdgeMesh;
		case MeshType.Lane:
			if (m_DefaultLaneMesh == null)
			{
				m_DefaultLaneMesh = NetMeshHelpers.CreateDefaultLaneMesh();
			}
			return m_DefaultLaneMesh;
		case MeshType.Zone:
			if ((flags & BatchFlags.Lod) != 0)
			{
				if (m_ZoneLodMesh == null)
				{
					m_ZoneLodMesh = ZoneMeshHelpers.CreateMesh(new int2(5, 3), 2);
				}
				return m_ZoneLodMesh;
			}
			if (m_ZoneBlockMesh == null)
			{
				m_ZoneBlockMesh = ZoneMeshHelpers.CreateMesh(new int2(10, 6), 1);
			}
			return m_ZoneBlockMesh;
		default:
			return null;
		}
	}

	public NativeList<int> GetBatchPriority(out JobHandle dependencies)
	{
		dependencies = m_PriorityDeps;
		return m_BatchPriority;
	}

	public NativeList<MeshLoadingState> GetLoadingState(out JobHandle dependencies)
	{
		dependencies = m_StateDeps;
		return m_LoadingState;
	}

	public void AddBatchPriorityWriter(JobHandle dependencies)
	{
		m_PriorityDeps = dependencies;
	}

	public void AddLoadingStateReader(JobHandle dependencies)
	{
		m_StateDeps = dependencies;
	}

	public void UpdateMeshes()
	{
		AddMeshes();
		UpdateMeshesForAddedInstances();
		UnloadMeshAndGeometryAssets();
	}

	public void CompleteMeshes()
	{
		if (!m_GenerateMeshEntities.IsCreated)
		{
			return;
		}
		Mesh[] array = new Mesh[m_GenerateMeshEntities.Length];
		for (int i = 0; i < m_GenerateMeshEntities.Length; i++)
		{
			Entity entity = m_GenerateMeshEntities[i];
			if (m_MeshInfos.TryGetValue(entity, out var value))
			{
				Mesh mesh = m_GeneratedMeshes[value.m_GeneratedIndex];
				Bounds bounds = default(Bounds);
				NetCompositionMeshData component2;
				if (base.EntityManager.TryGetComponent<MeshData>(entity, out var component))
				{
					bounds.SetMinMax(component.m_Bounds.min, component.m_Bounds.max);
				}
				else if (base.EntityManager.TryGetComponent<NetCompositionMeshData>(entity, out component2))
				{
					bounds.SetMinMax(new float3(component2.m_Width * -0.5f, component2.m_HeightRange.min, component2.m_Width * -0.5f) - 500f, new float3(component2.m_Width * 0.5f, component2.m_HeightRange.max, component2.m_Width * 0.5f) + 500f);
				}
				mesh.bounds = bounds;
				array[i] = mesh;
			}
			else
			{
				array[i] = new Mesh();
			}
		}
		m_GenerateMeshDeps.Complete();
		Mesh.ApplyAndDisposeWritableMeshData(m_GenerateMeshDataArray, array, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
		for (int j = 0; j < m_GenerateMeshEntities.Length; j++)
		{
			if (m_MeshInfos.ContainsKey(m_GenerateMeshEntities[j]))
			{
				array[j].UploadMeshData(markNoLongerReadable: true);
			}
			else
			{
				UnityEngine.Object.Destroy(array[j]);
			}
		}
		m_GenerateMeshEntities.Dispose();
	}

	public void UpdateBatchPriorities()
	{
		if (enableMeshLoading)
		{
			m_StateDeps = (m_PriorityDeps = IJobExtensions.Schedule(new LoadingPriorityJob
			{
				m_PriorityLimit = m_PriorityLimit,
				m_BatchPriority = m_BatchPriority,
				m_LoadingState = m_LoadingState,
				m_LoadingData = m_LoadingData,
				m_UnloadingData = m_UnloadingData
			}, JobHandle.CombineDependencies(m_PriorityDeps, m_StateDeps)));
		}
	}

	public void CompleteCaching()
	{
		List<Entity> list = null;
		foreach (KeyValuePair<Entity, CacheInfo> item in m_CachingMeshes)
		{
			if (item.Value.m_Dependency.IsCompleted)
			{
				CompleteCaching(item.Key, item.Value);
				if (list == null)
				{
					list = new List<Entity>();
				}
				list.Add(item.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Entity item2 in list)
		{
			m_CachingMeshes.Remove(item2);
		}
	}

	private void CompleteCaching(Entity entity)
	{
		if (m_CachingMeshes.TryGetValue(entity, out var value))
		{
			CompleteCaching(entity, value);
			m_CachingMeshes.Remove(entity);
		}
	}

	private void CompleteCaching(Entity entity, CacheInfo cacheInfo)
	{
		cacheInfo.m_Dependency.Complete();
		if (base.EntityManager.Exists(entity))
		{
			cacheInfo.m_CommandBuffer.Playback(base.EntityManager);
		}
		cacheInfo.m_CommandBuffer.Dispose();
		if (cacheInfo.m_GeometryAsset != null)
		{
			m_UnloadGeometryAssets.Add(cacheInfo.m_GeometryAsset);
		}
	}

	private void AddCaching(Entity entity, CacheInfo cacheInfo)
	{
		CompleteCaching(entity);
		m_CachingMeshes.Add(entity, cacheInfo);
	}

	public void AddBatch(CustomBatch batch, int batchIndex)
	{
		m_PriorityDeps.Complete();
		while (m_BatchPriority.Length <= batchIndex)
		{
			m_BatchPriority.Add(-1000000);
		}
		m_BatchPriority[batchIndex] = -1000000;
		m_StateDeps.Complete();
		while (m_LoadingState.Length <= batchIndex)
		{
			m_LoadingState.Add(MeshLoadingState.None);
		}
		m_LoadingState[batchIndex] = MeshLoadingState.None;
		if ((batch.sourceType & (MeshType.Net | MeshType.Zone)) == 0)
		{
			if ((base.EntityManager.GetComponentData<MeshData>(batch.sourceMeshEntity).m_State & MeshFlags.Default) != 0)
			{
				m_LoadingState[batchIndex] = MeshLoadingState.Default;
			}
		}
		else if ((batch.sourceType & MeshType.Net) != 0 && (base.EntityManager.GetComponentData<NetCompositionMeshData>(batch.sourceMeshEntity).m_State & MeshFlags.Default) != 0)
		{
			m_LoadingState[batchIndex] = MeshLoadingState.Default;
		}
	}

	public void RemoveBatch(CustomBatch batch, int batchIndex)
	{
		m_StateDeps.Complete();
		MeshLoadingState meshLoadingState = m_LoadingState[batchIndex];
		if ((meshLoadingState == MeshLoadingState.Copying || meshLoadingState == MeshLoadingState.Complete || meshLoadingState == MeshLoadingState.Obsolete) && m_MeshInfos.TryGetValue(batch.sharedMeshEntity, out var value))
		{
			if (value.m_BatchCount > 1)
			{
				value.m_BatchCount--;
				m_MeshInfos[batch.sharedMeshEntity] = value;
			}
			else
			{
				if (value.m_GeneratedIndex >= 0)
				{
					UnityEngine.Object.Destroy(m_GeneratedMeshes[value.m_GeneratedIndex]);
					m_GeneratedMeshes[value.m_GeneratedIndex] = null;
					if (value.m_GeneratedIndex == m_GeneratedMeshes.Count - 1)
					{
						m_GeneratedMeshes.RemoveAt(value.m_GeneratedIndex);
					}
					else
					{
						m_FreeMeshIndices.Add(value.m_GeneratedIndex);
					}
				}
				RemoveShapeData(value.m_ShapeAllocations);
				RemoveOverlayData(value.m_OverlayAllocations);
				UncacheMeshData(batch.sharedMeshEntity, batch.sourceType);
				totalSizeInMemory -= value.m_SizeInMemory;
				m_MeshInfos.Remove(batch.sharedMeshEntity);
			}
			if (batch.generatedType == GeneratedType.None && value.m_Prefab != null)
			{
				value.m_Prefab.ReleaseMeshes();
			}
		}
		if (meshLoadingState == MeshLoadingState.Pending || meshLoadingState == MeshLoadingState.Loading || meshLoadingState == MeshLoadingState.Copying)
		{
			for (int i = 0; i < m_LoadingData.Length; i++)
			{
				if (m_LoadingData.ElementAt(i).m_BatchIndex == batchIndex)
				{
					m_LoadingData.RemoveAt(i);
					break;
				}
			}
		}
		if (meshLoadingState == MeshLoadingState.Obsolete)
		{
			for (int j = 0; j < m_UnloadingData.Length; j++)
			{
				if (m_UnloadingData.ElementAt(j).m_BatchIndex == batchIndex)
				{
					m_UnloadingData.RemoveAt(j);
					break;
				}
			}
		}
		if (batchIndex == m_LoadingState.Length - 1)
		{
			m_LoadingState.RemoveAt(batchIndex);
		}
		else
		{
			m_LoadingState[batchIndex] = MeshLoadingState.None;
		}
		m_PriorityDeps.Complete();
		if (batchIndex == m_BatchPriority.Length - 1)
		{
			m_BatchPriority.RemoveAt(batchIndex);
		}
		else
		{
			m_BatchPriority[batchIndex] = -1000000;
		}
	}

	private void LoadMeshes(ref ulong loadingMemorySize, ref ulong neededMemorySize)
	{
		m_PriorityLimit = 0;
		m_StateDeps.Complete();
		loadingRemaining = m_LoadingData.Length;
		if (loadingRemaining == 0)
		{
			return;
		}
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		int num = 0;
		foreach (GeometryAsset item in m_LoadingGeometries)
		{
			if (!item.loading.m_AsyncLoadingDone)
			{
				num++;
			}
		}
		for (int i = 0; i < m_LoadingData.Length; i++)
		{
			ref LoadingData reference = ref m_LoadingData.ElementAt(i);
			ref MeshLoadingState reference2 = ref m_LoadingState.ElementAt(reference.m_BatchIndex);
			if (reference2 != MeshLoadingState.Pending && reference2 != MeshLoadingState.Loading)
			{
				continue;
			}
			CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(reference.m_BatchIndex);
			if (m_CachingMeshes.ContainsKey(customBatch.sharedMeshEntity))
			{
				continue;
			}
			if (m_MeshInfos.TryGetValue(customBatch.sharedMeshEntity, out var value))
			{
				value.m_BatchCount++;
				m_MeshInfos[customBatch.sharedMeshEntity] = value;
				reference2 = MeshLoadingState.Copying;
				m_AddMeshes = true;
				if ((customBatch.sourceFlags & BatchFlags.BlendWeights) != 0)
				{
					SetShapeParameters(customBatch, value.m_ShapeAllocations);
				}
				if ((customBatch.sourceFlags & BatchFlags.Overlay) != 0)
				{
					SetOverlayParameters(customBatch, value.m_OverlayAllocations);
				}
				continue;
			}
			value.m_Prefab = null;
			value.m_ShapeAllocations = null;
			value.m_OverlayAllocations = null;
			value.m_CullAllocations = null;
			value.m_SizeInMemory = 0uL;
			value.m_GeneratedIndex = -1;
			value.m_BatchCount = 1;
			bool flag = false;
			bool flag2 = false;
			string name = null;
			RenderPrefab prefab2;
			if (base.EntityManager.TryGetBuffer(customBatch.sharedMeshEntity, isReadOnly: true, out DynamicBuffer<NetCompositionPiece> buffer))
			{
				flag2 = true;
				if (reference2 == MeshLoadingState.Pending)
				{
					if (m_PriorityLimit > reference.m_Priority)
					{
						loadingRemaining--;
						continue;
					}
					ulong num2 = 0uL;
					for (int j = 0; j < buffer.Length; j++)
					{
						NetCompositionPiece netCompositionPiece = buffer[j];
						MeshInfo value2 = default(MeshInfo);
						if (base.EntityManager.HasComponent<MeshVertex>(netCompositionPiece.m_Piece))
						{
							if (m_MeshInfos.TryGetValue(netCompositionPiece.m_Piece, out value2))
							{
								num2 += value2.m_SizeInMemory;
							}
							continue;
						}
						if (m_MeshInfos.TryGetValue(netCompositionPiece.m_Piece, out value2))
						{
							num2 += value2.m_SizeInMemory;
							continue;
						}
						RenderPrefab prefab = m_PrefabSystem.GetPrefab<RenderPrefab>(netCompositionPiece.m_Piece);
						ulong num3 = EstimateSizeInMemory(prefab);
						num2 += num3;
						if (!m_CachingMeshes.ContainsKey(netCompositionPiece.m_Piece))
						{
							num2 += num3;
						}
					}
					if (strictMemoryBudget && totalSizeInMemory + loadingMemorySize + num2 > memoryBudget)
					{
						neededMemorySize += num2;
						m_PriorityLimit = reference.m_Priority;
						loadingRemaining--;
						continue;
					}
					reference2 = MeshLoadingState.Loading;
				}
				for (int k = 0; k < buffer.Length; k++)
				{
					NetCompositionPiece netCompositionPiece2 = buffer[k];
					MeshInfo value3 = default(MeshInfo);
					if (base.EntityManager.HasComponent<MeshVertex>(netCompositionPiece2.m_Piece))
					{
						if (m_MeshInfos.TryGetValue(netCompositionPiece2.m_Piece, out value3))
						{
							value.m_SizeInMemory += value3.m_SizeInMemory;
						}
						continue;
					}
					flag = true;
					if (m_MeshInfos.TryGetValue(netCompositionPiece2.m_Piece, out value3))
					{
						value.m_SizeInMemory += value3.m_SizeInMemory;
						continue;
					}
					RenderPrefab renderPrefab = (value3.m_Prefab = m_PrefabSystem.GetPrefab<RenderPrefab>(netCompositionPiece2.m_Piece));
					value3.m_SizeInMemory = EstimateSizeInMemory(renderPrefab);
					value3.m_GeneratedIndex = -1;
					value3.m_BatchCount = 0;
					value.m_SizeInMemory += value3.m_SizeInMemory;
					if (m_CachingMeshes.ContainsKey(netCompositionPiece2.m_Piece))
					{
						continue;
					}
					uint mask = 23u;
					GeometryAsset geometryAsset = renderPrefab.geometryAsset;
					if (geometryAsset != null)
					{
						if (!m_LoadingGeometries.Contains(geometryAsset))
						{
							if ((long)num < 30L)
							{
								m_LoadingGeometries.Add(geometryAsset);
								geometryAsset.RequestDataAsync(m_GeometryLoadingSystem, mask);
								num++;
							}
							loadingMemorySize += value3.m_SizeInMemory;
							continue;
						}
						if (!geometryAsset.loading.m_AsyncLoadingDone)
						{
							loadingMemorySize += value3.m_SizeInMemory;
							continue;
						}
						try
						{
							CacheMeshData(renderPrefab, geometryAsset, netCompositionPiece2.m_Piece, customBatch.sourceType);
						}
						catch (Exception ex)
						{
							UnityEngine.Debug.LogError("Error when accessing mesh data (" + renderPrefab.name + "):\n" + ex.Message, renderPrefab);
						}
						value.m_SizeInMemory -= value3.m_SizeInMemory;
						value3.m_SizeInMemory = GetSizeInMemory(geometryAsset);
						value.m_SizeInMemory += value3.m_SizeInMemory;
						m_LoadingGeometries.Remove(geometryAsset);
					}
					if (!m_CachingMeshes.ContainsKey(netCompositionPiece2.m_Piece) && geometryAsset != null)
					{
						m_UnloadGeometryAssets.Add(geometryAsset);
					}
					totalSizeInMemory += value3.m_SizeInMemory;
					m_MeshInfos.Add(netCompositionPiece2.m_Piece, value3);
				}
				if (flag)
				{
					loadingMemorySize += value.m_SizeInMemory;
					continue;
				}
				for (int l = 0; l < buffer.Length; l++)
				{
					NetCompositionPiece netCompositionPiece3 = buffer[l];
					if (m_MeshInfos.TryGetValue(netCompositionPiece3.m_Piece, out var value4))
					{
						value4.m_BatchCount++;
						m_MeshInfos[netCompositionPiece3.m_Piece] = value4;
					}
				}
				if (flag2)
				{
					name = $"Net composition {customBatch.sharedMeshEntity.Index}";
				}
			}
			else if (m_PrefabSystem.TryGetPrefab<RenderPrefab>(customBatch.sharedMeshEntity, out prefab2) && prefab2 != null)
			{
				value.m_Prefab = prefab2;
				value.m_SizeInMemory = EstimateSizeInMemory(prefab2);
				GeometryAsset geometryAsset2 = prefab2.geometryAsset;
				MeshData componentData = base.EntityManager.GetComponentData<MeshData>(customBatch.sharedMeshEntity);
				flag = !base.EntityManager.HasComponent<MeshVertex>(customBatch.sharedMeshEntity);
				flag2 = (componentData.m_State & MeshFlags.Base) != 0;
				if (flag)
				{
					if (geometryAsset2 != null)
					{
						if (!m_LoadingGeometries.Contains(geometryAsset2))
						{
							if (m_PriorityLimit > reference.m_Priority)
							{
								loadingRemaining--;
							}
							else if (strictMemoryBudget && totalSizeInMemory + loadingMemorySize + value.m_SizeInMemory > memoryBudget)
							{
								neededMemorySize += value.m_SizeInMemory;
								m_PriorityLimit = reference.m_Priority;
								loadingRemaining--;
							}
							else if ((long)num < 30L)
							{
								loadingMemorySize += value.m_SizeInMemory;
								m_LoadingGeometries.Add(geometryAsset2);
								geometryAsset2.RequestDataAsync(m_GeometryLoadingSystem, 16383u);
								reference2 = MeshLoadingState.Loading;
								num++;
							}
							continue;
						}
						if (!geometryAsset2.loading.m_AsyncLoadingDone)
						{
							loadingMemorySize += value.m_SizeInMemory;
							continue;
						}
						try
						{
							CacheMeshData(prefab2, geometryAsset2, customBatch.sharedMeshEntity, customBatch.sourceType);
						}
						catch (Exception ex2)
						{
							UnityEngine.Debug.LogError("Error when accessing mesh data (" + value.m_Prefab.name + "):\n" + ex2.Message, value.m_Prefab);
						}
						value.m_SizeInMemory = GetSizeInMemory(geometryAsset2);
						m_LoadingGeometries.Remove(geometryAsset2);
					}
				}
				else if (geometryAsset2 != null)
				{
					if (geometryAsset2.data.attrData.IsCreated)
					{
						value.m_SizeInMemory = GetSizeInMemory(geometryAsset2);
					}
					else
					{
						UnityEngine.Debug.Log("Geometry asset not loaded: " + geometryAsset2.name);
					}
				}
				if (flag2 && flag)
				{
					loadingMemorySize += value.m_SizeInMemory;
					continue;
				}
				if (!m_CachingMeshes.ContainsKey(customBatch.sharedMeshEntity) && geometryAsset2 != null)
				{
					m_UnloadGeometryAssets.Add(geometryAsset2);
				}
				if (flag2 && geometryAsset2 != null)
				{
					name = string.Concat("Generated base (" + geometryAsset2.name, ")");
				}
			}
			if (flag2)
			{
				Mesh mesh = new Mesh
				{
					name = name
				};
				if (m_FreeMeshIndices.Count != 0)
				{
					value.m_GeneratedIndex = m_FreeMeshIndices[m_FreeMeshIndices.Count - 1];
					m_FreeMeshIndices.RemoveAt(m_FreeMeshIndices.Count - 1);
					m_GeneratedMeshes[value.m_GeneratedIndex] = mesh;
				}
				else
				{
					value.m_GeneratedIndex = m_GeneratedMeshes.Count;
					m_GeneratedMeshes.Add(mesh);
				}
				if (!m_GenerateMeshEntities.IsCreated)
				{
					m_GenerateMeshEntities = new NativeList<Entity>(30, Allocator.TempJob);
				}
				m_GenerateMeshEntities.Add(customBatch.sharedMeshEntity);
			}
			if ((customBatch.sourceFlags & BatchFlags.BlendWeights) != 0)
			{
				value.m_ShapeAllocations = AddShapeData(value.m_Prefab);
				SetShapeParameters(customBatch, value.m_ShapeAllocations);
			}
			if ((customBatch.sourceFlags & BatchFlags.Overlay) != 0)
			{
				value.m_OverlayAllocations = AddOverlayData(value.m_Prefab);
				SetOverlayParameters(customBatch, value.m_OverlayAllocations);
			}
			totalSizeInMemory += value.m_SizeInMemory;
			m_MeshInfos.Add(customBatch.sharedMeshEntity, value);
			reference2 = MeshLoadingState.Copying;
			m_AddMeshes = true;
		}
	}

	private ulong EstimateSizeInMemory(RenderPrefab meshPrefab)
	{
		int indexCount = meshPrefab.indexCount;
		int vertexCount = meshPrefab.vertexCount;
		int num = ((vertexCount > 65536) ? 4 : 2);
		int num2 = 32;
		return (ulong)((long)indexCount * (long)num + (long)vertexCount * (long)num2);
	}

	private ulong GetSizeInMemory(GeometryAsset geometryAsset)
	{
		return (ulong)geometryAsset.data.attrData.Length + (ulong)geometryAsset.data.indexData.Length;
	}

	private void AddMeshes()
	{
		if (!m_AddMeshes)
		{
			return;
		}
		m_AddMeshes = false;
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		for (int i = 0; i < m_LoadingData.Length; i++)
		{
			ref LoadingData reference = ref m_LoadingData.ElementAt(i);
			ref MeshLoadingState reference2 = ref m_LoadingState.ElementAt(reference.m_BatchIndex);
			if (reference2 != MeshLoadingState.Copying)
			{
				continue;
			}
			CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(reference.m_BatchIndex);
			try
			{
				if (m_MeshInfos.TryGetValue(customBatch.sharedMeshEntity, out var value))
				{
					if (customBatch.generatedType != GeneratedType.None)
					{
						if (value.m_GeneratedIndex >= 0)
						{
							managedBatches.SetMesh(reference.m_BatchIndex, m_GeneratedMeshes[value.m_GeneratedIndex], customBatch.sourceSubMeshIndex, nativeBatchGroups);
						}
					}
					else if (value.m_Prefab != null)
					{
						int subMeshIndex;
						Mesh mesh = value.m_Prefab.ObtainMesh(customBatch.sourceSubMeshIndex, out subMeshIndex);
						managedBatches.SetMesh(reference.m_BatchIndex, mesh, subMeshIndex, nativeBatchGroups);
					}
				}
				if (customBatch.defaultMaterial != customBatch.loadedMaterial)
				{
					managedBatches.SetMaterial(reference.m_BatchIndex, customBatch.loadedMaterial, nativeBatchGroups);
				}
			}
			catch (Exception exception)
			{
				if (m_MeshInfos.TryGetValue(customBatch.sharedMeshEntity, out var value2) && value2.m_Prefab != null)
				{
					COSystemBase.baseLog.ErrorFormat(value2.m_Prefab, exception, "Error when setting mesh for {0}", value2.m_Prefab.name);
				}
				else
				{
					COSystemBase.baseLog.ErrorFormat(exception, "Error when setting mesh for {0}", customBatch.sourceMeshEntity);
				}
			}
			reference2 = MeshLoadingState.Complete;
			m_LoadingData.RemoveAtSwapBack(i--);
		}
	}

	private void UpdateMeshesForAddedInstances()
	{
		m_StateDeps.Complete();
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		JobHandle dependencies2;
		NativeBatchInstances<CullingData, GroupData, BatchData, InstanceData> nativeBatchInstances = m_BatchManagerSystem.GetNativeBatchInstances(readOnly: false, out dependencies2);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		dependencies2.Complete();
		AddedInstanceEnumerator addedInstances = nativeBatchInstances.GetAddedInstances();
		int groupIndex;
		while (addedInstances.GetNextUpdatedGroup(out groupIndex))
		{
			NativeBatchAccessor<BatchData> batchAccessor = nativeBatchGroups.GetBatchAccessor(groupIndex);
			for (int i = 0; i < batchAccessor.Length; i++)
			{
				int managedBatchIndex = batchAccessor.GetManagedBatchIndex(i);
				if (managedBatchIndex < 0)
				{
					continue;
				}
				ref MeshLoadingState reference = ref m_LoadingState.ElementAt(managedBatchIndex);
				if (reference != MeshLoadingState.None)
				{
					continue;
				}
				CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(managedBatchIndex);
				if (!m_MeshInfos.TryGetValue(customBatch.sharedMeshEntity, out var value))
				{
					continue;
				}
				value.m_BatchCount++;
				m_MeshInfos[customBatch.sharedMeshEntity] = value;
				if ((customBatch.sourceFlags & BatchFlags.BlendWeights) != 0)
				{
					SetShapeParameters(customBatch, value.m_ShapeAllocations);
				}
				if ((customBatch.sourceFlags & BatchFlags.Overlay) != 0)
				{
					SetOverlayParameters(customBatch, value.m_OverlayAllocations);
				}
				try
				{
					if (customBatch.generatedType != GeneratedType.None)
					{
						if (value.m_GeneratedIndex >= 0)
						{
							managedBatches.SetMesh(managedBatchIndex, m_GeneratedMeshes[value.m_GeneratedIndex], customBatch.sourceSubMeshIndex, nativeBatchGroups);
						}
					}
					else if (value.m_Prefab != null)
					{
						int subMeshIndex;
						Mesh mesh = value.m_Prefab.ObtainMesh(customBatch.sourceSubMeshIndex, out subMeshIndex);
						managedBatches.SetMesh(managedBatchIndex, mesh, subMeshIndex, nativeBatchGroups);
					}
					if (customBatch.defaultMaterial != customBatch.loadedMaterial)
					{
						managedBatches.SetMaterial(managedBatchIndex, customBatch.loadedMaterial, nativeBatchGroups);
					}
				}
				catch (Exception exception)
				{
					if (value.m_Prefab != null)
					{
						COSystemBase.baseLog.ErrorFormat(value.m_Prefab, exception, "Error when setting mesh for {0}", value.m_Prefab.name);
					}
					else
					{
						COSystemBase.baseLog.ErrorFormat(exception, "Error when setting mesh for {0}", customBatch.sourceMeshEntity);
					}
				}
				reference = MeshLoadingState.Complete;
			}
		}
		nativeBatchInstances.ClearAddedInstances();
	}

	private void UnloadMeshes(ulong loadingMemorySize, ulong neededMemorySize)
	{
		if (!forceMeshUnloading && totalSizeInMemory + loadingMemorySize + neededMemorySize <= memoryBudget)
		{
			return;
		}
		m_StateDeps.Complete();
		if (m_UnloadingData.Length == 0)
		{
			return;
		}
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		int num = 0;
		bool flag = false;
		dependencies.Complete();
		for (int i = 0; i < m_UnloadingData.Length; i++)
		{
			ref LoadingData reference = ref m_UnloadingData.ElementAt(i);
			ref MeshLoadingState reference2 = ref m_LoadingState.ElementAt(reference.m_BatchIndex);
			if (reference2 != MeshLoadingState.Obsolete)
			{
				continue;
			}
			if ((!forceMeshUnloading && totalSizeInMemory + loadingMemorySize + neededMemorySize <= memoryBudget) || (!forceMeshUnloading && totalSizeInMemory + loadingMemorySize <= memoryBudget && -reference.m_Priority >= m_PriorityLimit))
			{
				break;
			}
			CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(reference.m_BatchIndex);
			if (m_MeshInfos.TryGetValue(customBatch.sharedMeshEntity, out var value))
			{
				if (value.m_BatchCount > 1)
				{
					value.m_BatchCount--;
					m_MeshInfos[customBatch.sharedMeshEntity] = value;
				}
				else
				{
					if ((long)num >= 30L)
					{
						continue;
					}
					num++;
					if (base.EntityManager.TryGetBuffer(customBatch.sharedMeshEntity, isReadOnly: true, out DynamicBuffer<NetCompositionPiece> buffer))
					{
						for (int j = 0; j < buffer.Length; j++)
						{
							NetCompositionPiece netCompositionPiece = buffer[j];
							if (m_MeshInfos.TryGetValue(netCompositionPiece.m_Piece, out var value2))
							{
								if (value2.m_BatchCount > 1)
								{
									value2.m_BatchCount--;
									m_MeshInfos[netCompositionPiece.m_Piece] = value2;
								}
								else
								{
									UncacheMeshData(netCompositionPiece.m_Piece, customBatch.sourceType);
									totalSizeInMemory -= value2.m_SizeInMemory;
									m_MeshInfos.Remove(netCompositionPiece.m_Piece);
								}
							}
						}
					}
					if (value.m_GeneratedIndex >= 0)
					{
						UnityEngine.Object.Destroy(m_GeneratedMeshes[value.m_GeneratedIndex]);
						m_GeneratedMeshes[value.m_GeneratedIndex] = null;
						m_FreeMeshIndices.Add(value.m_GeneratedIndex);
					}
					RemoveShapeData(value.m_ShapeAllocations);
					RemoveOverlayData(value.m_OverlayAllocations);
					UncacheMeshData(customBatch.sharedMeshEntity, customBatch.sourceType);
					totalSizeInMemory -= value.m_SizeInMemory;
					m_MeshInfos.Remove(customBatch.sharedMeshEntity);
				}
				managedBatches.SetMesh(reference.m_BatchIndex, GetDefaultMesh(customBatch.sourceType, customBatch.sourceFlags, customBatch.generatedType), 0, nativeBatchGroups);
				if (customBatch.generatedType == GeneratedType.None && value.m_Prefab != null)
				{
					value.m_Prefab.ReleaseMeshes();
				}
			}
			reference2 = MeshLoadingState.Unloading;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		for (int k = 0; k < m_UnloadingData.Length; k++)
		{
			ref LoadingData reference3 = ref m_UnloadingData.ElementAt(k);
			ref MeshLoadingState reference4 = ref m_LoadingState.ElementAt(reference3.m_BatchIndex);
			if (reference4 == MeshLoadingState.Unloading)
			{
				CustomBatch customBatch2 = (CustomBatch)managedBatches.GetBatch(reference3.m_BatchIndex);
				if (customBatch2.defaultMaterial != customBatch2.loadedMaterial)
				{
					managedBatches.SetMaterial(reference3.m_BatchIndex, customBatch2.defaultMaterial, nativeBatchGroups);
				}
				reference4 = MeshLoadingState.None;
				m_UnloadingData.RemoveAtSwapBack(k--);
			}
		}
	}

	private void UnloadMeshAndGeometryAssets()
	{
		if (m_UnloadGeometryAssets.Count == 0)
		{
			return;
		}
		foreach (GeometryAsset item in m_UnloadGeometryAssets)
		{
			item.UnloadPartial();
		}
		m_UnloadGeometryAssets.Clear();
	}

	private void CacheMeshData(RenderPrefab meshPrefab, GeometryAsset asset, Entity entity, MeshType type)
	{
		switch (type)
		{
		case MeshType.Object:
		case MeshType.Lane:
		{
			int boneCount = 0;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ProceduralBone> buffer))
			{
				boneCount = buffer.Length;
			}
			bool cacheNormals = false;
			if (base.EntityManager.TryGetComponent<MeshData>(entity, out var component))
			{
				cacheNormals = (component.m_State & MeshFlags.Base) != 0;
			}
			CacheInfo cacheInfo2 = default(CacheInfo);
			cacheInfo2.m_GeometryAsset = asset;
			cacheInfo2.m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback);
			cacheInfo2.m_Dependency = ObjectMeshHelpers.CacheMeshData(meshPrefab, asset, entity, boneCount, cacheNormals, cacheInfo2.m_CommandBuffer);
			CompleteCaching(entity);
			AddCaching(entity, cacheInfo2);
			break;
		}
		case MeshType.Net:
		{
			CacheInfo cacheInfo = default(CacheInfo);
			cacheInfo.m_GeometryAsset = asset;
			cacheInfo.m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback);
			cacheInfo.m_Dependency = NetMeshHelpers.CacheMeshData(asset, entity, base.EntityManager, cacheInfo.m_CommandBuffer);
			AddCaching(entity, cacheInfo);
			break;
		}
		case MeshType.Object | MeshType.Net:
			break;
		}
	}

	private void CacheMeshData(Mesh mesh, Entity entity, MeshType type)
	{
		switch (type)
		{
		case MeshType.Object:
		case MeshType.Lane:
		{
			bool cacheNormals = false;
			if (base.EntityManager.TryGetComponent<MeshData>(entity, out var component))
			{
				cacheNormals = (component.m_State & MeshFlags.Base) != 0;
			}
			CacheInfo cacheInfo2 = new CacheInfo
			{
				m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback)
			};
			ObjectMeshHelpers.CacheMeshData(mesh, entity, cacheNormals, cacheInfo2.m_CommandBuffer);
			AddCaching(entity, cacheInfo2);
			break;
		}
		case MeshType.Net:
		{
			CacheInfo cacheInfo = new CacheInfo
			{
				m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback)
			};
			NetMeshHelpers.CacheMeshData(mesh, entity, base.EntityManager, cacheInfo.m_CommandBuffer);
			AddCaching(entity, cacheInfo);
			break;
		}
		case MeshType.Object | MeshType.Net:
			break;
		}
	}

	private void UncacheMeshData(Entity mesh, MeshType type)
	{
		switch (type)
		{
		case MeshType.Object:
		case MeshType.Lane:
		{
			CacheInfo cacheInfo2 = new CacheInfo
			{
				m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback)
			};
			ObjectMeshHelpers.UncacheMeshData(mesh, cacheInfo2.m_CommandBuffer);
			AddCaching(mesh, cacheInfo2);
			break;
		}
		case MeshType.Net:
		{
			CacheInfo cacheInfo = new CacheInfo
			{
				m_CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.SinglePlayback)
			};
			NetMeshHelpers.UncacheMeshData(mesh, cacheInfo.m_CommandBuffer);
			AddCaching(mesh, cacheInfo);
			break;
		}
		case MeshType.Object | MeshType.Net:
			break;
		}
	}

	private void GenerateMeshes()
	{
		if (m_GenerateMeshEntities.IsCreated)
		{
			m_GenerateMeshDataArray = Mesh.AllocateWritableMeshData(m_GenerateMeshEntities.Length);
			m_GenerateMeshDeps = BatchMeshHelpers.GenerateMeshes(this, m_GenerateMeshEntities, m_GenerateMeshDataArray, base.Dependency);
			base.Dependency = m_GenerateMeshDeps;
		}
	}

	private ShapeAllocation[] AddShapeData(RenderPrefab meshPrefab)
	{
		GeometryAsset geometryAsset = meshPrefab.geometryAsset;
		if (geometryAsset == null)
		{
			return null;
		}
		NativeArray<byte> shapeDataBuffer = geometryAsset.shapeDataBuffer;
		if (!shapeDataBuffer.IsCreated)
		{
			return null;
		}
		NativeArray<ulong> data = shapeDataBuffer.Reinterpret<ulong>(1);
		ShapeAllocation[] array = new ShapeAllocation[meshPrefab.meshCount];
		for (int i = 0; i < array.Length; i++)
		{
			int shapeDataSize = geometryAsset.GetShapeDataSize(i);
			if (shapeDataSize != 0)
			{
				array[i].m_Stride = geometryAsset.GetVertexCount(i);
				array[i].m_PositionExtent = geometryAsset.GetShapePositionExtent(i);
				array[i].m_NormalExtent = geometryAsset.GetShapeNormalExtent(i);
				uint num = (uint)shapeDataSize / 8u;
				array[i].m_Allocation = m_ShapeAllocator.Allocate(num);
				if (array[i].m_Allocation.Empty)
				{
					uint num2 = 1048576u;
					num2 = (num2 + num - 1) / num2 * num2;
					m_ShapeAllocator.Resize(m_ShapeAllocator.Size + num2);
					array[i].m_Allocation = m_ShapeAllocator.Allocate(num);
				}
				m_ShapeCount++;
			}
		}
		ResizeShapeBuffer();
		for (int j = 0; j < array.Length; j++)
		{
			int shapeStartOffset = geometryAsset.GetShapeStartOffset(j);
			int shapeDataSize2 = geometryAsset.GetShapeDataSize(j);
			if (shapeDataSize2 != 0)
			{
				m_ShapeBuffer.SetData(data, shapeStartOffset / 8, (int)array[j].m_Allocation.Begin, shapeDataSize2 / 8);
			}
		}
		return array;
	}

	private void RemoveShapeData(ShapeAllocation[] allocations)
	{
		if (allocations == null)
		{
			return;
		}
		for (int i = 0; i < allocations.Length; i++)
		{
			ShapeAllocation shapeAllocation = allocations[i];
			if (!shapeAllocation.m_Allocation.Empty)
			{
				m_ShapeAllocator.Release(shapeAllocation.m_Allocation);
				m_ShapeCount--;
			}
		}
	}

	public void SetShapeParameters(MaterialPropertyBlock customProps, Entity sharedMeshEntity, int subMeshIndex)
	{
		ShapeAllocation allocation = default(ShapeAllocation);
		if (m_MeshInfos.TryGetValue(sharedMeshEntity, out var value) && value.m_ShapeAllocations != null && value.m_ShapeAllocations.Length > subMeshIndex)
		{
			allocation = value.m_ShapeAllocations[subMeshIndex];
		}
		SetShapeParameters(customProps, allocation);
	}

	public void GetShapeStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		allocatedSize = m_ShapeAllocator.UsedSpace * 8;
		bufferSize = m_ShapeAllocator.Size * 8;
		count = (uint)m_ShapeCount;
	}

	private void SetShapeParameters(CustomBatch batch, ShapeAllocation[] allocations)
	{
		ShapeAllocation allocation = default(ShapeAllocation);
		if (allocations != null && allocations.Length > batch.sourceSubMeshIndex)
		{
			allocation = allocations[batch.sourceSubMeshIndex];
		}
		SetShapeParameters(batch.customProps, allocation);
		BatchPropertyUpdated(batch);
	}

	private void SetShapeParameters(MaterialPropertyBlock customProps, ShapeAllocation allocation)
	{
		PropertyData propertyData = m_BatchManagerSystem.GetPropertyData(MaterialProperty.ShapeParameters1);
		PropertyData propertyData2 = m_BatchManagerSystem.GetPropertyData(MaterialProperty.ShapeParameters2);
		Vector4 zero = Vector4.zero;
		Vector4 zero2 = Vector4.zero;
		if (!allocation.m_Allocation.Empty)
		{
			zero.x = allocation.m_PositionExtent.x;
			zero.y = allocation.m_PositionExtent.y;
			zero.z = allocation.m_PositionExtent.z;
			zero.w = math.asfloat(allocation.m_Allocation.Begin);
			zero2.x = allocation.m_NormalExtent.x;
			zero2.y = allocation.m_NormalExtent.y;
			zero2.z = allocation.m_NormalExtent.z;
			zero2.w = math.asfloat(allocation.m_Stride);
		}
		customProps.SetVector(propertyData.m_NameID, zero);
		customProps.SetVector(propertyData2.m_NameID, zero2);
	}

	private OverlayAllocation[] AddOverlayData(RenderPrefab meshPrefab)
	{
		if (meshPrefab.geometryAsset == null)
		{
			return null;
		}
		List<OverlayAtlasElement> list = RenderingUtils.GetOverlayAtlasElements(meshPrefab)?.ToList();
		if (list == null || !list.Any())
		{
			return null;
		}
		OverlayAllocation[] array = new OverlayAllocation[meshPrefab.meshCount];
		for (int i = 0; i < array.Length; i++)
		{
			if (meshPrefab.GetSurfaceAsset(i).textures.ContainsKey("_OverlayAtlas"))
			{
				array[i].m_Stride = list.Count;
				NativeHeapBlock allocation = m_OverlayAllocator.Allocate((uint)list.Count);
				if (allocation.Empty)
				{
					uint num = OVERLAYBUFFER_MEMORY_INCREMENT / (uint)OverlayAtlasElement.SizeOf;
					num = (uint)((int)num + list.Count - 1) / num * num;
					m_OverlayAllocator.Resize(m_OverlayAllocator.Size + num);
					allocation = m_OverlayAllocator.Allocate((uint)list.Count);
				}
				m_OverlayCount++;
				ResizeOverlayBuffer();
				m_OverlayBuffer.SetData(list, 0, (int)allocation.Begin, list.Count);
				array[i].m_Allocation = allocation;
			}
		}
		return array;
	}

	private void RemoveOverlayData(OverlayAllocation[] allocations)
	{
		if (allocations == null)
		{
			return;
		}
		for (int i = 0; i < allocations.Length; i++)
		{
			OverlayAllocation overlayAllocation = allocations[i];
			if (!overlayAllocation.m_Allocation.Empty)
			{
				m_OverlayAllocator.Release(overlayAllocation.m_Allocation);
				m_OverlayCount--;
			}
		}
	}

	public void GetOverlayStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		allocatedSize = m_OverlayAllocator.UsedSpace * (uint)OverlayAtlasElement.SizeOf;
		bufferSize = m_OverlayAllocator.Size * (uint)OverlayAtlasElement.SizeOf;
		count = (uint)m_OverlayCount;
	}

	public void SetOverlayParameters(MaterialPropertyBlock customProps, Entity sharedMeshEntity, int subMeshIndex)
	{
		OverlayAllocation allocation = default(OverlayAllocation);
		if (m_MeshInfos.TryGetValue(sharedMeshEntity, out var value) && value.m_OverlayAllocations != null && value.m_OverlayAllocations.Length > subMeshIndex)
		{
			allocation = value.m_OverlayAllocations[subMeshIndex];
		}
		SetOverlayParameters(customProps, allocation);
	}

	private void SetOverlayParameters(CustomBatch batch, OverlayAllocation[] allocations)
	{
		OverlayAllocation allocation = default(OverlayAllocation);
		if (allocations != null && allocations.Length > batch.sourceSubMeshIndex)
		{
			allocation = allocations[batch.sourceSubMeshIndex];
		}
		SetOverlayParameters(batch.customProps, allocation);
		BatchPropertyUpdated(batch);
	}

	private void SetOverlayParameters(MaterialPropertyBlock customProps, OverlayAllocation allocation)
	{
		PropertyData propertyData = m_BatchManagerSystem.GetPropertyData(MaterialProperty.OverlayParameters);
		Vector2 zero = Vector2.zero;
		if (!allocation.m_Allocation.Empty)
		{
			zero.x = math.asfloat(allocation.m_Allocation.Begin);
		}
		zero.y = math.asfloat(allocation.m_Stride);
		customProps.SetVector(propertyData.m_NameID, zero);
	}

	private void BatchPropertyUpdated(CustomBatch batch)
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		BatchFlags batchFlags = batch.sourceFlags;
		if (!m_BatchManagerSystem.IsMotionVectorsEnabled())
		{
			batchFlags &= ~BatchFlags.MotionVectors;
		}
		if (!m_BatchManagerSystem.IsLodFadeEnabled())
		{
			batchFlags &= ~BatchFlags.LodFade;
		}
		OptionalProperties optionalProperties = new OptionalProperties(batchFlags, batch.sourceType);
		dependencies.Complete();
		NativeBatchProperties batchProperties = managedBatches.GetBatchProperties(batch.material.shader, optionalProperties);
		WriteableBatchDefaultsAccessor batchDefaultsAccessor = nativeBatchGroups.GetBatchDefaultsAccessor(batch.groupIndex, batch.batchIndex);
		if (batch.sourceSurface != null)
		{
			managedBatches.SetDefaults(ManagedBatchSystem.GetTemplate(batch.sourceSurface), batch.sourceSurface.floats, batch.sourceSurface.ints, batch.sourceSurface.vectors, batch.sourceSurface.colors, batch.customProps, batchProperties, batchDefaultsAccessor);
		}
		else
		{
			managedBatches.SetDefaults(batch.sourceMaterial, batch.customProps, batchProperties, batchDefaultsAccessor);
		}
	}

	private void ResizeShapeBuffer()
	{
		int num = ((m_ShapeBuffer != null) ? m_ShapeBuffer.count : 0);
		int size = (int)m_ShapeAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 8)
			{
				name = "Shape buffer"
			};
			Shader.SetGlobalBuffer("shapeBuffer", graphicsBuffer);
			if (m_ShapeBuffer != null)
			{
				ulong[] data = new ulong[num];
				m_ShapeBuffer.GetData(data);
				graphicsBuffer.SetData(data, 0, 0, num);
				m_ShapeBuffer.Release();
			}
			else
			{
				graphicsBuffer.SetData(new ulong[1], 0, 0, 1);
			}
			m_ShapeBuffer = graphicsBuffer;
		}
	}

	private void ResizeCullBuffer()
	{
		int num = ((m_CullBuffer != null) ? m_CullBuffer.count : 0);
		int size = (int)m_CullAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 4)
			{
				name = "Cull buffer"
			};
			Shader.SetGlobalBuffer("cullBuffer", graphicsBuffer);
			if (m_CullBuffer != null)
			{
				ulong[] data = new ulong[num];
				m_CullBuffer.GetData(data);
				graphicsBuffer.SetData(data, 0, 0, num);
				m_CullBuffer.Release();
			}
			else
			{
				graphicsBuffer.SetData(new ulong[1], 0, 0, 1);
			}
			m_CullBuffer = graphicsBuffer;
		}
	}

	private void ResizeOverlayBuffer()
	{
		int num = ((m_OverlayBuffer != null) ? m_OverlayBuffer.count : 0);
		int size = (int)m_OverlayAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, OverlayAtlasElement.SizeOf)
			{
				name = "Overlay buffer"
			};
			Shader.SetGlobalBuffer("overlayBuffer", graphicsBuffer);
			if (m_OverlayBuffer != null)
			{
				OverlayAtlasElement[] data = new OverlayAtlasElement[num];
				m_OverlayBuffer.GetData(data);
				graphicsBuffer.SetData(data, 0, 0, num);
				m_OverlayBuffer.Release();
			}
			else
			{
				graphicsBuffer.SetData(new OverlayAtlasElement[1], 0, 0, 1);
			}
			m_OverlayBuffer = graphicsBuffer;
		}
	}

	[Preserve]
	public BatchMeshSystem()
	{
	}
}
