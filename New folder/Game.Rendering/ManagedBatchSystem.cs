using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class ManagedBatchSystem : GameSystemBase
{
	private struct KeywordData
	{
		public string name { get; private set; }

		public bool remove { get; private set; }

		public KeywordData(string name, bool remove)
		{
			this.name = name;
			this.remove = remove;
		}
	}

	private struct TextureData
	{
		public int nameID { get; private set; }

		public Texture texture { get; private set; }

		public TextureData(int nameID, Texture texture)
		{
			this.nameID = nameID;
			this.texture = texture;
		}
	}

	public class MaterialKey : IEquatable<MaterialKey>
	{
		public Shader shader { get; set; }

		public Material template { get; set; }

		public int decalLayerMask { get; set; }

		public int renderQueue { get; set; }

		public HashSet<string> keywords { get; private set; }

		public List<int> vtStacks { get; private set; }

		public Dictionary<int, object> textures { get; private set; }

		public MaterialKey()
		{
			decalLayerMask = -1;
			renderQueue = -1;
			keywords = new HashSet<string>();
			vtStacks = new List<int>();
			textures = new Dictionary<int, object>();
		}

		public MaterialKey(MaterialKey source)
		{
			shader = source.shader;
			template = source.template;
			decalLayerMask = source.decalLayerMask;
			renderQueue = source.renderQueue;
			keywords = new HashSet<string>(source.keywords);
			vtStacks = new List<int>(source.vtStacks);
			textures = new Dictionary<int, object>(source.textures);
		}

		private static int GetSortingPriority(SurfaceAsset surfaceAsset)
		{
			if (!surfaceAsset.TryGetIntProperty("_TransparentSortPriority", out var value))
			{
				return 0;
			}
			return value;
		}

		private static int GetSortingPriority(Material material)
		{
			if (material.HasFloat("_TransparentSortPriority"))
			{
				return (int)material.GetFloat("_TransparentSortPriority");
			}
			return 0;
		}

		public void Initialize(SurfaceAsset surface)
		{
			template = GetTemplate(surface);
			renderQueue = template.renderQueue + GetSortingPriority(surface);
			foreach (string keyword in surface.keywords)
			{
				keywords.Add(keyword);
			}
			foreach (KeyValuePair<string, TextureAsset> texture in surface.textures)
			{
				if (!surface.IsHandledByVirtualTexturing(texture))
				{
					textures.Add(Shader.PropertyToID(texture.Key), texture.Value);
				}
			}
		}

		public void Initialize(Material material)
		{
			shader = material.shader;
			renderQueue = material.renderQueue + GetSortingPriority(material);
			string[] shaderKeywords = material.shaderKeywords;
			foreach (string item in shaderKeywords)
			{
				keywords.Add(item);
			}
			int[] texturePropertyNameIDs = material.GetTexturePropertyNameIDs();
			foreach (int num in texturePropertyNameIDs)
			{
				textures.Add(num, material.GetTexture(num));
			}
		}

		public void Clear()
		{
			shader = null;
			template = null;
			decalLayerMask = -1;
			renderQueue = -1;
			keywords.Clear();
			vtStacks.Clear();
			textures.Clear();
		}

		public bool Equals(MaterialKey other)
		{
			if (shader != other.shader || template != other.template || decalLayerMask != other.decalLayerMask || renderQueue != other.renderQueue || keywords.Count != other.keywords.Count || vtStacks.Count != other.vtStacks.Count || textures.Count != other.textures.Count)
			{
				return false;
			}
			foreach (string keyword in keywords)
			{
				if (!other.keywords.Contains(keyword))
				{
					return false;
				}
			}
			for (int i = 0; i < vtStacks.Count; i++)
			{
				if (vtStacks[i] != other.vtStacks[i])
				{
					return false;
				}
			}
			foreach (KeyValuePair<int, object> texture in textures)
			{
				if (!other.textures.TryGetValue(texture.Key, out var value) || texture.Value != value)
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = decalLayerMask.GetHashCode() ^ renderQueue.GetHashCode();
			if (shader != null)
			{
				num ^= shader.GetHashCode();
			}
			if (template != null)
			{
				num ^= template.GetHashCode();
			}
			foreach (string keyword in keywords)
			{
				num ^= keyword.GetHashCode();
			}
			int count = vtStacks.Count;
			foreach (int vtStack in vtStacks)
			{
				num ^= vtStack.GetHashCode() + count;
			}
			foreach (KeyValuePair<int, object> texture in textures)
			{
				num ^= ((texture.Value != null) ? texture.Value.GetHashCode() : texture.Key.GetHashCode());
			}
			return num;
		}
	}

	private class GroupKey : IEquatable<GroupKey>
	{
		public struct Batch : IEquatable<Batch>
		{
			public Material loadedMaterial { get; set; }

			public BatchFlags flags { get; set; }

			public Batch(CustomBatch batch)
			{
				loadedMaterial = batch.loadedMaterial;
				flags = batch.sourceFlags;
			}

			public bool Equals(Batch other)
			{
				if (loadedMaterial == other.loadedMaterial)
				{
					return flags == other.flags;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return loadedMaterial.GetHashCode() ^ flags.GetHashCode();
			}
		}

		public Entity mesh { get; set; }

		public ushort partition { get; set; }

		public MeshLayer layer { get; set; }

		public MeshType type { get; set; }

		public List<Batch> batches { get; private set; }

		public GroupKey()
		{
			mesh = Entity.Null;
			partition = 0;
			layer = (MeshLayer)0;
			type = (MeshType)0;
			batches = new List<Batch>();
		}

		public void Initialize(Entity sharedMesh, GroupData groupData)
		{
			mesh = sharedMesh;
			partition = groupData.m_Partition;
			layer = groupData.m_Layer;
			type = groupData.m_MeshType;
		}

		public void Clear()
		{
			mesh = Entity.Null;
			partition = 0;
			layer = (MeshLayer)0;
			type = (MeshType)0;
			batches.Clear();
		}

		public bool Equals(GroupKey other)
		{
			if (mesh != other.mesh || partition != other.partition || layer != other.layer || type != other.type || batches.Count != other.batches.Count)
			{
				return false;
			}
			for (int i = 0; i < batches.Count; i++)
			{
				if (!batches[i].Equals(other.batches[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = mesh.GetHashCode() ^ partition.GetHashCode() ^ layer.GetHashCode() ^ type.GetHashCode();
			for (int i = 0; i < batches.Count; i++)
			{
				num ^= batches[i].GetHashCode();
			}
			return num;
		}
	}

	private struct MeshKey : IEquatable<MeshKey>
	{
		public Colossal.Hash128 meshGuid { get; set; }

		public MeshFlags flags { get; set; }

		public MeshKey(RenderPrefab meshPrefab, MeshData meshData)
		{
			AssetData geometryAsset;
			if ((geometryAsset = meshPrefab.geometryAsset) != null)
			{
				meshGuid = geometryAsset.id;
			}
			else
			{
				meshGuid = default(Colossal.Hash128);
			}
			flags = meshData.m_State & MeshFlags.Base;
		}

		public bool Equals(MeshKey other)
		{
			if (meshGuid == other.meshGuid)
			{
				return flags == other.flags;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return meshGuid.GetHashCode() ^ flags.GetHashCode();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private RenderingSystem m_RenderingSystem;

	private BatchMeshSystem m_BatchMeshSystem;

	private BatchManagerSystem m_BatchManagerSystem;

	private TextureStreamingSystem m_TextureStreamingSystem;

	private Dictionary<MaterialKey, Material> m_Materials;

	private Dictionary<GroupKey, Entity> m_Groups;

	private Dictionary<MeshKey, Entity> m_Meshes;

	private List<KeywordData> m_CachedKeywords;

	private List<TextureData> m_CachedTextures;

	private MaterialKey m_CachedMaterialKey;

	private GroupKey m_CachedGroupKey;

	private VTTextureRequester m_VTTextureRequester;

	private JobHandle m_VTRequestDependencies;

	private EntityQuery m_MeshSettingsQuery;

	private bool m_VTRequestsUpdated;

	private int m_TunnelLayer;

	private int m_MovingLayer;

	private int m_PipelineLayer;

	private int m_SubPipelineLayer;

	private int m_WaterwayLayer;

	private int m_OutlineLayer;

	private int m_MarkerLayer;

	private int m_DecalLayerMask;

	private int m_AnimationTexture;

	private int m_UseStack1;

	private int m_ImpostorSize;

	private int m_ImpostorOffset;

	private int m_WorldspaceAlbedo;

	private int m_MaskMap;

	public int materialCount => m_Materials.Count;

	public int groupCount { get; private set; }

	public int batchCount { get; private set; }

	public IReadOnlyDictionary<MaterialKey, Material> materials => m_Materials;

	public VTTextureRequester VTTextureRequester => m_VTTextureRequester;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_BatchMeshSystem = base.World.GetOrCreateSystemManaged<BatchMeshSystem>();
		m_BatchManagerSystem = base.World.GetOrCreateSystemManaged<BatchManagerSystem>();
		m_TextureStreamingSystem = base.World.GetOrCreateSystemManaged<TextureStreamingSystem>();
		m_Materials = new Dictionary<MaterialKey, Material>();
		m_Groups = new Dictionary<GroupKey, Entity>();
		m_Meshes = new Dictionary<MeshKey, Entity>();
		m_CachedKeywords = new List<KeywordData>();
		m_CachedTextures = new List<TextureData>();
		m_VTTextureRequester = new VTTextureRequester(m_TextureStreamingSystem);
		m_MeshSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<MeshSettingsData>());
		m_TunnelLayer = LayerMask.NameToLayer("Tunnel");
		m_MovingLayer = LayerMask.NameToLayer("Moving");
		m_PipelineLayer = LayerMask.NameToLayer("Pipeline");
		m_SubPipelineLayer = LayerMask.NameToLayer("SubPipeline");
		m_WaterwayLayer = LayerMask.NameToLayer("Waterway");
		m_OutlineLayer = LayerMask.NameToLayer("Outline");
		m_MarkerLayer = LayerMask.NameToLayer("Marker");
		m_DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");
		m_AnimationTexture = Shader.PropertyToID("_AnimationTexture");
		m_UseStack1 = Shader.PropertyToID("colossal_UseStack1");
		m_ImpostorSize = Shader.PropertyToID("_ImpostorSize");
		m_ImpostorOffset = Shader.PropertyToID("_ImpostorOffset");
		m_WorldspaceAlbedo = Shader.PropertyToID("_WorldspaceAlbedo");
		m_MaskMap = Shader.PropertyToID("_MaskMap");
	}

	[Preserve]
	protected override void OnDestroy()
	{
		foreach (KeyValuePair<MaterialKey, Material> item in m_Materials)
		{
			CoreUtils.Destroy(item.Value);
		}
		m_VTRequestDependencies.Complete();
		m_VTTextureRequester.Dispose();
		base.OnDestroy();
	}

	public void RemoveMesh(Entity oldMesh, Entity newMesh = default(Entity))
	{
		List<GroupKey> list = new List<GroupKey>();
		List<MeshKey> list2 = new List<MeshKey>();
		foreach (KeyValuePair<GroupKey, Entity> item in m_Groups)
		{
			if (item.Key.mesh == oldMesh || item.Value == oldMesh)
			{
				list.Add(item.Key);
			}
		}
		foreach (KeyValuePair<MeshKey, Entity> item2 in m_Meshes)
		{
			if (item2.Value == oldMesh)
			{
				list2.Add(item2.Key);
			}
		}
		bool flag = false;
		if (base.EntityManager.TryGetComponent<MeshData>(newMesh, out var component) && m_PrefabSystem.TryGetPrefab<RenderPrefab>(newMesh, out var prefab) && prefab != null)
		{
			MeshKey other = new MeshKey(prefab, component);
			foreach (MeshKey item3 in list2)
			{
				if (item3.Equals(other))
				{
					m_Meshes[item3] = newMesh;
					flag = true;
				}
				else
				{
					m_Meshes.Remove(item3);
				}
			}
			if (flag)
			{
				m_BatchMeshSystem.ReplaceMesh(oldMesh, newMesh);
			}
		}
		else
		{
			foreach (MeshKey item4 in list2)
			{
				m_Meshes.Remove(item4);
			}
		}
		foreach (GroupKey item5 in list)
		{
			if (m_Groups[item5] == oldMesh)
			{
				m_Groups.Remove(item5);
				groupCount--;
				batchCount -= item5.batches.Count;
			}
		}
	}

	public void ResetSharedMeshes()
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		int num = nativeBatchGroups.GetGroupCount();
		for (int i = 0; i < num; i++)
		{
			if (!nativeBatchGroups.IsValidGroup(i))
			{
				continue;
			}
			int num2 = nativeBatchGroups.GetBatchCount(i);
			GroupData groupData = nativeBatchGroups.GetGroupData(i);
			GroupKey groupKey = null;
			Entity value = groupData.m_Mesh;
			if (m_PrefabSystem.TryGetPrefab<RenderPrefab>(groupData.m_Mesh, out var prefab) && prefab != null)
			{
				try
				{
					MeshData componentData = base.EntityManager.GetComponentData<MeshData>(groupData.m_Mesh);
					MeshKey key = new MeshKey(prefab, componentData);
					if (!m_Meshes.TryGetValue(key, out value))
					{
						m_Meshes.Add(key, groupData.m_Mesh);
						value = groupData.m_Mesh;
					}
					if (m_CachedGroupKey != null)
					{
						groupKey = m_CachedGroupKey;
						m_CachedGroupKey = null;
					}
					else
					{
						groupKey = new GroupKey();
					}
					groupKey.Initialize(value, groupData);
				}
				catch (Exception exception)
				{
					COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when initializing batches for {0}", prefab.name);
				}
			}
			int num3 = 0;
			while (true)
			{
				if (num3 < num2)
				{
					int managedBatchIndex = nativeBatchGroups.GetManagedBatchIndex(i, num3);
					if (managedBatchIndex >= 0)
					{
						groupKey?.batches.Add(new GroupKey.Batch((CustomBatch)managedBatches.GetBatch(managedBatchIndex)));
						num3++;
						continue;
					}
				}
				else if (groupKey != null && m_Groups.TryAdd(groupKey, groupData.m_Mesh))
				{
					num++;
					batchCount += num2;
					break;
				}
				if (groupKey != null)
				{
					groupKey.Clear();
					m_CachedGroupKey = groupKey;
				}
				break;
			}
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		UpdatedManagedBatchEnumerator updatedManagedBatches = nativeBatchGroups.GetUpdatedManagedBatches();
		int groupIndex;
		while (updatedManagedBatches.GetNextUpdatedGroup(out groupIndex))
		{
			int num = nativeBatchGroups.GetBatchCount(groupIndex);
			GroupData groupData = nativeBatchGroups.GetGroupData(groupIndex);
			GroupKey groupKey = null;
			Entity value = groupData.m_Mesh;
			if (m_PrefabSystem.TryGetPrefab<RenderPrefab>(groupData.m_Mesh, out var prefab) && prefab != null)
			{
				try
				{
					MeshData componentData = base.EntityManager.GetComponentData<MeshData>(groupData.m_Mesh);
					MeshKey key = new MeshKey(prefab, componentData);
					if (!m_Meshes.TryGetValue(key, out value))
					{
						m_Meshes.Add(key, groupData.m_Mesh);
						value = groupData.m_Mesh;
					}
					if (m_CachedGroupKey != null)
					{
						groupKey = m_CachedGroupKey;
						m_CachedGroupKey = null;
					}
					else
					{
						groupKey = new GroupKey();
					}
					groupKey.Initialize(value, groupData);
				}
				catch (Exception exception)
				{
					COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when initializing batches for {0}", prefab.name);
				}
			}
			for (int i = 0; i < num; i++)
			{
				int num2 = nativeBatchGroups.GetManagedBatchIndex(groupIndex, i);
				if (num2 < 0)
				{
					try
					{
						BatchData batchData = nativeBatchGroups.GetBatchData(groupIndex, i);
						PropertyData lodFadeData;
						CustomBatch customBatch = CreateBatch(groupIndex, i, value, ref groupData, ref batchData, out lodFadeData);
						nativeBatchGroups.SetBatchData(groupIndex, i, batchData);
						BatchFlags batchFlags = customBatch.sourceFlags;
						if (!m_BatchManagerSystem.IsMotionVectorsEnabled())
						{
							batchFlags &= ~BatchFlags.MotionVectors;
						}
						if (!m_BatchManagerSystem.IsLodFadeEnabled())
						{
							batchFlags &= ~BatchFlags.LodFade;
						}
						OptionalProperties optionalProperties = new OptionalProperties(batchFlags, customBatch.sourceType);
						num2 = managedBatches.AddBatch(customBatch, i, nativeBatchGroups);
						m_BatchMeshSystem.AddBatch(customBatch, num2);
						NativeBatchProperties batchProperties = managedBatches.GetBatchProperties(customBatch.material.shader, optionalProperties);
						nativeBatchGroups.SetBatchProperties(groupIndex, i, batchProperties);
						if (lodFadeData.m_DataIndex >= 0)
						{
							nativeBatchGroups.SetBatchDataIndex(groupIndex, i, lodFadeData.m_NameID, lodFadeData.m_DataIndex);
						}
						WriteableBatchDefaultsAccessor batchDefaultsAccessor = nativeBatchGroups.GetBatchDefaultsAccessor(groupIndex, i);
						if (customBatch.sourceSurface != null)
						{
							managedBatches.SetDefaults(GetTemplate(customBatch.sourceSurface), customBatch.sourceSurface.floats, customBatch.sourceSurface.ints, customBatch.sourceSurface.vectors, customBatch.sourceSurface.colors, customBatch.customProps, batchProperties, batchDefaultsAccessor);
						}
						else
						{
							managedBatches.SetDefaults(customBatch.sourceMaterial, customBatch.customProps, batchProperties, batchDefaultsAccessor);
						}
					}
					catch (Exception exception2)
					{
						if (prefab != null)
						{
							COSystemBase.baseLog.ErrorFormat(prefab, exception2, "Error when initializing batch {0} for {1}", i, prefab.name);
						}
						else
						{
							COSystemBase.baseLog.ErrorFormat(exception2, "Error when initializing batch {0} for {1}", i, groupData.m_Mesh);
						}
						continue;
					}
				}
				groupKey?.batches.Add(new GroupKey.Batch((CustomBatch)managedBatches.GetBatch(num2)));
			}
			nativeBatchGroups.SetGroupData(groupIndex, groupData);
			if (groupKey != null)
			{
				if (m_Groups.TryGetValue(groupKey, out var value2))
				{
					m_BatchManagerSystem.MergeGroups(value2, groupIndex);
					groupKey.Clear();
					m_CachedGroupKey = groupKey;
				}
				else
				{
					m_Groups.Add(groupKey, groupData.m_Mesh);
					groupCount++;
					batchCount += num;
				}
			}
			else
			{
				groupCount++;
				batchCount += num;
			}
		}
		nativeBatchGroups.ClearUpdatedManagedBatches();
	}

	public void EnabledShadersUpdated()
	{
		JobHandle dependencies;
		NativeBatchGroups<CullingData, GroupData, BatchData, InstanceData> nativeBatchGroups = m_BatchManagerSystem.GetNativeBatchGroups(readOnly: false, out dependencies);
		ManagedBatches<OptionalProperties> managedBatches = m_BatchManagerSystem.GetManagedBatches();
		dependencies.Complete();
		int num = nativeBatchGroups.GetGroupCount();
		for (int i = 0; i < num; i++)
		{
			if (!nativeBatchGroups.IsValidGroup(i))
			{
				continue;
			}
			int num2 = nativeBatchGroups.GetBatchCount(i);
			GroupData groupData = nativeBatchGroups.GetGroupData(i);
			groupData.m_RenderFlags &= ~BatchRenderFlags.IsEnabled;
			for (int j = 0; j < num2; j++)
			{
				int managedBatchIndex = nativeBatchGroups.GetManagedBatchIndex(i, j);
				if (managedBatchIndex >= 0)
				{
					CustomBatch customBatch = (CustomBatch)managedBatches.GetBatch(managedBatchIndex);
					BatchData batchData = nativeBatchGroups.GetBatchData(i, j);
					if (m_RenderingSystem.IsShaderEnabled(customBatch.loadedMaterial.shader))
					{
						groupData.m_RenderFlags |= BatchRenderFlags.IsEnabled;
						batchData.m_RenderFlags |= BatchRenderFlags.IsEnabled;
					}
					else
					{
						batchData.m_RenderFlags &= ~BatchRenderFlags.IsEnabled;
					}
					nativeBatchGroups.SetBatchData(i, j, batchData);
				}
			}
			nativeBatchGroups.SetGroupData(i, groupData);
		}
	}

	public JobHandle GetVTRequestMaxPixels(out NativeList<float> maxPixels0, out NativeList<float> maxPixels1)
	{
		maxPixels0 = m_VTTextureRequester.TexturesMaxPixels[0];
		maxPixels1 = m_VTTextureRequester.TexturesMaxPixels[1];
		return m_VTRequestDependencies;
	}

	public void AddVTRequestWriter(JobHandle dependencies)
	{
		m_VTRequestDependencies = dependencies;
		m_VTRequestsUpdated = true;
	}

	public void ResetVT(int desiredMipBias, UnityEngine.Rendering.VirtualTexturing.FilterMode filterMode)
	{
		if (m_TextureStreamingSystem.ShouldResetVT(desiredMipBias, filterMode))
		{
			m_VTRequestDependencies.Complete();
			m_VTTextureRequester.Clear();
			m_TextureStreamingSystem.Initialize(desiredMipBias, filterMode);
			m_BatchManagerSystem.VirtualTexturingUpdated();
		}
	}

	public void ReloadVT()
	{
		m_VTRequestDependencies.Complete();
		m_VTTextureRequester.Clear();
		m_TextureStreamingSystem.Reload();
		m_BatchManagerSystem.VirtualTexturingUpdated();
	}

	public void CompleteVTRequests()
	{
		if (m_VTRequestsUpdated)
		{
			m_VTRequestDependencies.Complete();
			m_VTTextureRequester.UpdateTexturesVTRequests();
			m_VTRequestsUpdated = false;
		}
		m_TextureStreamingSystem.UpdateWorkingSetMipBias();
	}

	private CustomBatch CreateBatch(int groupIndex, int batchIndex, Entity sharedMesh, ref GroupData groupData, ref BatchData batchData, out PropertyData lodFadeData)
	{
		SurfaceAsset surfaceAsset = null;
		Material material = null;
		Material material2 = null;
		Material value = null;
		Mesh mesh = null;
		MaterialPropertyBlock materialPropertyBlock = null;
		Entity entity = Entity.Null;
		ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
		bool flag = true;
		BatchFlags batchFlags = (BatchFlags)0;
		GeneratedType generatedType = GeneratedType.None;
		int num = 0;
		int num2 = batchData.m_SubMeshIndex;
		MaterialKey materialKey;
		if (m_CachedMaterialKey != null)
		{
			materialKey = m_CachedMaterialKey;
			m_CachedMaterialKey = null;
		}
		else
		{
			materialKey = new MaterialKey();
		}
		lodFadeData = new PropertyData
		{
			m_DataIndex = -1
		};
		m_CachedKeywords.Clear();
		m_CachedTextures.Clear();
		Entity entity2;
		if (batchData.m_LodMesh != Entity.Null)
		{
			entity = batchData.m_LodMesh;
			entity2 = groupData.m_Mesh;
		}
		else
		{
			entity = groupData.m_Mesh;
			entity2 = Entity.Null;
		}
		if (batchData.m_LodIndex > 0)
		{
			batchFlags |= BatchFlags.Lod;
		}
		if (groupData.m_MeshType == MeshType.Zone)
		{
			material = m_PrefabSystem.GetPrefab<ZoneBlockPrefab>(entity).m_Material;
			materialKey.Initialize(material);
			if (groupData.m_Partition >= 1)
			{
				batchFlags |= BatchFlags.Extended1;
			}
			if (groupData.m_Partition >= 2)
			{
				batchFlags |= BatchFlags.Extended2;
			}
			if (groupData.m_Partition >= 3)
			{
				batchFlags |= BatchFlags.Extended3;
			}
			shadowCastingMode = ShadowCastingMode.Off;
			flag = false;
		}
		else if (groupData.m_MeshType == MeshType.Net)
		{
			NetCompositionMeshData componentData = base.EntityManager.GetComponentData<NetCompositionMeshData>(entity);
			DynamicBuffer<MeshMaterial> buffer = base.EntityManager.GetBuffer<MeshMaterial>(entity, isReadOnly: true);
			DynamicBuffer<NetCompositionPiece> buffer2 = base.EntityManager.GetBuffer<NetCompositionPiece>(entity, isReadOnly: true);
			int materialIndex = buffer[num2].m_MaterialIndex;
			for (int i = 0; i < buffer2.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = buffer2[i];
				if (!base.EntityManager.TryGetBuffer(netCompositionPiece.m_Piece, isReadOnly: true, out DynamicBuffer<MeshMaterial> buffer3))
				{
					continue;
				}
				int num3 = 0;
				while (num3 < buffer3.Length)
				{
					if (buffer3[num3].m_MaterialIndex != materialIndex)
					{
						num3++;
						continue;
					}
					goto IL_01c9;
				}
				continue;
				IL_01c9:
				surfaceAsset = m_PrefabSystem.GetPrefab<RenderPrefab>(netCompositionPiece.m_Piece).GetSurfaceAsset(num3);
				surfaceAsset.LoadProperties(useVT: true);
				materialKey.Initialize(surfaceAsset);
				break;
			}
			materialKey.decalLayerMask = 2;
			lodFadeData = m_BatchManagerSystem.GetPropertyData(((batchData.m_LodIndex & 1) == 0) ? NetProperty.LodFade0 : NetProperty.LodFade1);
			batchFlags |= BatchFlags.InfoviewColor | BatchFlags.LodFade;
			generatedType = GeneratedType.NetComposition;
			sharedMesh = entity;
			if ((componentData.m_Flags.m_General & CompositionFlags.General.Node) != 0)
			{
				batchFlags |= BatchFlags.Node;
			}
			if ((componentData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
			{
				batchFlags |= BatchFlags.Roundabout;
			}
			if ((componentData.m_State & MeshFlags.Default) != 0)
			{
				materialKey.textures.Clear();
				materialKey.textures.Add(m_WorldspaceAlbedo, Texture2D.grayTexture);
			}
			batchData.m_ShadowArea = float.PositiveInfinity;
			batchData.m_ShadowHeight = 1f;
			if ((componentData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0 || (componentData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.SoundBarrier)) != 0 || (componentData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered | CompositionFlags.Side.SoundBarrier)) != 0 || (componentData.m_State & MeshFlags.Default) != 0)
			{
				batchData.m_ShadowHeight = componentData.m_Width;
			}
		}
		else
		{
			RenderPrefab renderPrefab = m_PrefabSystem.GetPrefab<RenderPrefab>(entity);
			MeshData componentData2 = base.EntityManager.GetComponentData<MeshData>(entity);
			SharedMeshData componentData3 = base.EntityManager.GetComponentData<SharedMeshData>(entity);
			RenderPrefab renderPrefab2 = null;
			if (entity2 != Entity.Null)
			{
				renderPrefab2 = m_PrefabSystem.GetPrefab<RenderPrefab>(entity2);
			}
			if (batchData.m_LodIndex > 0)
			{
				MeshKey key = new MeshKey(renderPrefab, componentData2);
				if (!m_Meshes.TryGetValue(key, out sharedMesh))
				{
					m_Meshes.Add(key, entity);
					sharedMesh = entity;
				}
			}
			componentData3.m_Mesh = sharedMesh;
			base.EntityManager.SetComponentData(entity, componentData3);
			DecalProperties decalProperties = renderPrefab.GetComponent<DecalProperties>();
			ProceduralAnimationProperties component = renderPrefab.GetComponent<ProceduralAnimationProperties>();
			EmissiveProperties component2 = renderPrefab.GetComponent<EmissiveProperties>();
			ColorProperties component3 = renderPrefab.GetComponent<ColorProperties>();
			CurveProperties component4 = renderPrefab.GetComponent<CurveProperties>();
			DilationProperties component5 = renderPrefab.GetComponent<DilationProperties>();
			OverlayProperties component6 = renderPrefab.GetComponent<OverlayProperties>();
			BaseProperties component7 = renderPrefab.GetComponent<BaseProperties>();
			if (component2 == null && renderPrefab2 != null)
			{
				component2 = renderPrefab2.GetComponent<EmissiveProperties>();
			}
			if (component3 == null && renderPrefab2 != null)
			{
				component3 = renderPrefab2.GetComponent<ColorProperties>();
			}
			if (component7 == null && renderPrefab2 != null)
			{
				component7 = renderPrefab2.GetComponent<BaseProperties>();
			}
			if (decalProperties != null && groupData.m_Layer == MeshLayer.Outline)
			{
				MeshSettingsData singleton = m_MeshSettingsQuery.GetSingleton<MeshSettingsData>();
				renderPrefab = m_PrefabSystem.GetPrefab<RenderPrefab>(singleton.m_MissingObjectMesh);
				num2 = 0;
				surfaceAsset = renderPrefab.GetSurfaceAsset(num2);
				decalProperties = null;
			}
			else if ((componentData2.m_State & MeshFlags.Base) != 0 && num2 == componentData2.m_SubMeshCount)
			{
				if (component7 != null)
				{
					renderPrefab = component7.m_BaseType;
				}
				else
				{
					MeshSettingsData singleton2 = m_MeshSettingsQuery.GetSingleton<MeshSettingsData>();
					renderPrefab = m_PrefabSystem.GetPrefab<RenderPrefab>(singleton2.m_DefaultBaseMesh);
				}
				num2 = 0;
				surfaceAsset = renderPrefab.GetSurfaceAsset(num2);
				generatedType = GeneratedType.ObjectBase;
				batchFlags |= BatchFlags.Base;
			}
			else
			{
				surfaceAsset = renderPrefab.GetSurfaceAsset(num2);
			}
			if (groupData.m_MeshType == MeshType.Object)
			{
				lodFadeData = m_BatchManagerSystem.GetPropertyData(((batchData.m_LodIndex & 1) == 0) ? ObjectProperty.LodFade0 : ObjectProperty.LodFade1);
			}
			else if (groupData.m_MeshType == MeshType.Lane)
			{
				lodFadeData = m_BatchManagerSystem.GetPropertyData(((batchData.m_LodIndex & 1) == 0) ? LaneProperty.LodFade0 : LaneProperty.LodFade1);
			}
			surfaceAsset.LoadProperties(useVT: true);
			materialKey.Initialize(surfaceAsset);
			Bounds3 bounds = RenderingUtils.SafeBounds(componentData2.m_Bounds);
			float3 @float = MathUtils.Center(bounds);
			float3 float2 = MathUtils.Size(bounds);
			float4 float3 = new float4(float2, @float.y);
			batchData.m_ShadowHeight = float2.y;
			batchData.m_ShadowArea = math.sqrt(float2.x * float2.x + float2.z * float2.z) * batchData.m_ShadowHeight;
			VTAtlassingInfo[] array = surfaceAsset.VTAtlassingInfos;
			if (array == null)
			{
				array = surfaceAsset.PreReservedAtlassingInfos;
			}
			int lod = componentData2.m_MinLod;
			if (groupData.m_MeshType == MeshType.Lane)
			{
				lod = groupData.m_Partition;
			}
			PropertyData propertyData = m_BatchManagerSystem.GetPropertyData(MaterialProperty.TextureArea);
			PropertyData propertyData2 = m_BatchManagerSystem.GetPropertyData(MaterialProperty.MeshSize);
			PropertyData propertyData3 = m_BatchManagerSystem.GetPropertyData(MaterialProperty.LodDistanceFactor);
			PropertyData propertyData4 = m_BatchManagerSystem.GetPropertyData(MaterialProperty.SingleLightsOffset);
			PropertyData propertyData5 = m_BatchManagerSystem.GetPropertyData(MaterialProperty.DilationParams);
			if (decalProperties != null)
			{
				materialKey.renderQueue = materialKey.template.shader.renderQueue + decalProperties.m_RendererPriority;
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				materialPropertyBlock.SetVector(propertyData.m_NameID, new float4(decalProperties.m_TextureArea.min, decalProperties.m_TextureArea.max));
				materialPropertyBlock.SetVector(propertyData2.m_NameID, float3);
				materialPropertyBlock.SetFloat(propertyData3.m_NameID, RenderingUtils.CalculateDistanceFactor(lod));
				materialKey.decalLayerMask = (int)decalProperties.m_LayerMask;
				if (array != null)
				{
					Bounds2 bounds2 = MathUtils.Bounds(decalProperties.m_TextureArea.min, decalProperties.m_TextureArea.max);
					m_VTRequestDependencies.Complete();
					if (array.Length >= 1 && array[0].indexInStack >= 0)
					{
						batchData.m_VTIndex0 = m_VTTextureRequester.RegisterTexture(0, array[0].stackGlobalIndex, array[0].indexInStack, bounds2);
					}
					if (array.Length >= 2 && array[1].indexInStack >= 0)
					{
						batchData.m_VTIndex1 = m_VTTextureRequester.RegisterTexture(1, array[1].stackGlobalIndex, array[1].indexInStack, bounds2);
					}
					batchData.m_VTSizeFactor = math.cmax(float2);
				}
				if (groupData.m_MeshType == MeshType.Lane)
				{
					materialPropertyBlock.SetFloat(m_BatchManagerSystem.GetPropertyData(MaterialProperty.SmoothingDistance).m_NameID, componentData2.m_SmoothingDistance);
				}
				if (decalProperties.m_EnableInfoviewColor)
				{
					batchFlags |= BatchFlags.InfoviewColor;
				}
			}
			else
			{
				DecalLayers decalLayerMask = ((componentData2.m_DecalLayer != 0) ? componentData2.m_DecalLayer : DecalLayers.Other);
				materialKey.decalLayerMask = (int)decalLayerMask;
				batchFlags |= BatchFlags.InfoviewColor | BatchFlags.LodFade | BatchFlags.SurfaceState;
			}
			bool flag2 = groupData.m_MeshType == MeshType.Object;
			bool flag3 = groupData.m_MeshType == MeshType.Lane;
			if (component != null && component.m_Bones != null && component.m_Bones.Length != 0)
			{
				PropertyData propertyData6 = m_BatchManagerSystem.GetPropertyData(ObjectProperty.BoneParameters);
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				materialPropertyBlock.SetVector(propertyData6.m_NameID, new Vector2(math.asfloat(0), math.asfloat(component.m_Bones.Length)));
				EnableKeyword(materialKey, "_GPU_ANIMATION_PROCEDURAL");
				batchFlags |= BatchFlags.MotionVectors | BatchFlags.Bones;
				flag2 = false;
			}
			if (component2 != null && component2.hasAnyLights)
			{
				PropertyData propertyData7 = m_BatchManagerSystem.GetPropertyData(ObjectProperty.LightParameters);
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				materialPropertyBlock.SetVector(propertyData7.m_NameID, new Vector2(0f, component2.lightsCount));
				materialPropertyBlock.SetFloat(propertyData4.m_NameID, component2.GetSingleLightOffset(batchData.m_SubMeshIndex));
				batchFlags |= BatchFlags.Emissive;
			}
			if (component3 != null && component3.m_ColorVariations != null && component3.m_ColorVariations.Count != 0)
			{
				batchFlags |= BatchFlags.ColorMask;
			}
			if ((componentData2.m_State & MeshFlags.Character) != 0)
			{
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				m_BatchMeshSystem.SetShapeParameters(materialPropertyBlock, sharedMesh, num2);
				m_BatchMeshSystem.SetOverlayParameters(materialPropertyBlock, sharedMesh, num2);
				EnableKeyword(materialKey, "_GPU_ANIMATION_SHAPE");
				batchFlags |= BatchFlags.Bones | BatchFlags.BlendWeights | BatchFlags.Overlay;
				flag2 = false;
			}
			if ((componentData2.m_State & MeshFlags.Prop) != 0)
			{
				if (component != null)
				{
					EnableKeyword(materialKey, "_GPU_ANIMATION_PROCEDURAL");
				}
				else
				{
					EnableKeyword(materialKey, "_GPU_ANIMATION_NORMAL");
					batchFlags |= BatchFlags.Bones;
				}
				flag2 = false;
			}
			if (component4 != null)
			{
				if (component4.m_GeometryTiling)
				{
					if (materialPropertyBlock == null)
					{
						materialPropertyBlock = new MaterialPropertyBlock();
					}
					float4 float4 = new float4(0.1f, 10f, 0f, 0f);
					if (componentData2.m_TilingCount != 0 && component4.m_StraightTiling)
					{
						float4.x = 1f / (float)componentData2.m_TilingCount;
						float4.y = 0.01f;
					}
					materialPropertyBlock.SetVector(propertyData5.m_NameID, float4);
					EnableKeyword(materialKey, "COLOSSAL_GEOMETRY_TILING");
					flag3 = false;
				}
				if (component4.m_SubFlow)
				{
					batchFlags |= BatchFlags.InfoviewFlow;
				}
				if (component4.m_HangingSwaying)
				{
					batchFlags |= BatchFlags.Hanging;
					float num4 = batchData.m_ShadowArea / batchData.m_ShadowHeight;
					batchData.m_ShadowHeight += 0.5f;
					batchData.m_ShadowArea = num4 * batchData.m_ShadowHeight;
				}
			}
			if (component5 != null)
			{
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				float4 float5 = new float4(component5.m_MinSize, 1f / math.max(1E-05f, math.max(float3.x, float3.y)), component5.m_InfoviewFactor * 2.5f, 2.5f);
				if (component5.m_InfoviewOnly)
				{
					float5.x = math.max(float3.x, float3.y);
					float5.w = 0f;
				}
				materialPropertyBlock.SetVector(propertyData5.m_NameID, float5);
				materialPropertyBlock.SetFloat(propertyData3.m_NameID, RenderingUtils.CalculateDistanceFactor(lod));
				EnableKeyword(materialKey, "COLOSSAL_GEOMETRY_DILATED");
				flag3 = false;
				if (surfaceAsset.keywords.Contains("_SURFACE_TYPE_TRANSPARENT"))
				{
					batchFlags &= ~BatchFlags.LodFade;
				}
			}
			if (flag2)
			{
				EnableKeyword(materialKey, "_GPU_ANIMATION_OFF");
			}
			if (flag3)
			{
				EnableKeyword(materialKey, "COLOSSAL_GEOMETRY_DEFAULT");
			}
			if (component6 != null)
			{
				if (materialKey.template.renderQueue == 3000)
				{
					materialKey.renderQueue = 3900;
				}
				if (materialPropertyBlock == null)
				{
					materialPropertyBlock = new MaterialPropertyBlock();
				}
				materialPropertyBlock.SetVector(propertyData.m_NameID, new float4(component6.m_TextureArea.min, component6.m_TextureArea.max));
				materialPropertyBlock.SetVector(propertyData2.m_NameID, float3);
				materialPropertyBlock.SetFloat(propertyData3.m_NameID, RenderingUtils.CalculateDistanceFactor(lod));
				batchFlags &= ~BatchFlags.SurfaceState;
			}
			else if (decalProperties == null && materialKey.template.renderQueue >= 2000 && materialKey.template.renderQueue <= 2500 && math.any(MathUtils.Size(bounds) < new float3(7f, 3f, 7f)))
			{
				materialKey.renderQueue = materialKey.template.renderQueue + 1;
			}
			if (decalProperties != null || component6 != null)
			{
				shadowCastingMode = ShadowCastingMode.Off;
				flag = false;
			}
			if (renderPrefab.isImpostor)
			{
				ImpostorData componentData4 = base.EntityManager.GetComponentData<ImpostorData>(entity);
				surfaceAsset.vectors.TryGetValue("_ImpostorOffset", out var value2);
				componentData4.m_Offset = ((float4)value2).xyz;
				surfaceAsset.floats.TryGetValue("_ImpostorSize", out componentData4.m_Size);
				base.EntityManager.SetComponentData(entity, componentData4);
				if (batchData.m_LodIndex == groupData.m_LodCount)
				{
					groupData.m_SecondarySize = float2 * float2 / (componentData4.m_Size * math.cmax(float2));
					groupData.m_SecondaryCenter = @float - componentData4.m_Offset;
				}
			}
			if ((renderPrefab.manualVTRequired || renderPrefab.isImpostor) && decalProperties == null && array != null)
			{
				Bounds2 bounds3 = MathUtils.Bounds(new float2(0f, 0f), new float2(1f, 1f));
				m_VTRequestDependencies.Complete();
				if (array.Length >= 1 && array[0].indexInStack >= 0)
				{
					batchData.m_VTIndex0 = m_VTTextureRequester.RegisterTexture(0, array[0].stackGlobalIndex, array[0].indexInStack, bounds3);
				}
				if (array.Length >= 2 && array[1].indexInStack >= 0)
				{
					batchData.m_VTIndex1 = m_VTTextureRequester.RegisterTexture(1, array[1].stackGlobalIndex, array[1].indexInStack, bounds3);
				}
				batchData.m_VTSizeFactor = math.cmax(float2) * 2f;
			}
			if ((componentData2.m_State & MeshFlags.Default) != 0)
			{
				batchData.m_ShadowArea = float.PositiveInfinity;
				batchData.m_ShadowHeight = float.PositiveInfinity;
				DisableKeyword(materialKey, "_TANGENTSPACE_OCTO");
				materialKey.textures.Add(m_MaskMap, Texture2D.blackTexture);
			}
			if (array != null)
			{
				if ((componentData2.m_State & MeshFlags.Default) != 0)
				{
					DisableKeyword(materialKey, "ENABLE_VT");
				}
				else
				{
					for (int j = 0; j < 2; j++)
					{
						if (array.Length > j && array[j].indexInStack >= 0)
						{
							if (materialPropertyBlock == null)
							{
								materialPropertyBlock = new MaterialPropertyBlock();
							}
							materialPropertyBlock.SetTextureParamBlock(m_BatchManagerSystem.GetVTTextureParamBlockID(j), m_TextureStreamingSystem.GetTextureParamBlock(array[j]));
							materialKey.vtStacks.Add(array[j].stackGlobalIndex);
							EnableKeyword(materialKey, "ENABLE_VT");
						}
						else
						{
							materialKey.vtStacks.Add(-1);
						}
					}
				}
			}
		}
		mesh = m_BatchMeshSystem.GetDefaultMesh(groupData.m_MeshType, batchFlags, generatedType);
		if (m_Materials.TryGetValue(materialKey, out value))
		{
			materialKey.Clear();
			m_CachedMaterialKey = materialKey;
		}
		else
		{
			value = CreateMaterial(surfaceAsset, material, materialKey);
			m_Materials.Add(materialKey, value);
		}
		if (value.IsKeywordEnabled("_TRANSPARENT_WRITES_MOTION_VEC"))
		{
			batchFlags |= BatchFlags.MotionVectors;
		}
		if (material2 == null)
		{
			material2 = value;
		}
		switch (groupData.m_Layer)
		{
		case MeshLayer.Moving:
			num = m_MovingLayer;
			batchFlags |= BatchFlags.MotionVectors;
			break;
		case MeshLayer.Tunnel:
			num = m_TunnelLayer;
			break;
		case MeshLayer.Pipeline:
			num = m_PipelineLayer;
			shadowCastingMode = ShadowCastingMode.Off;
			flag = false;
			break;
		case MeshLayer.SubPipeline:
			num = m_SubPipelineLayer;
			shadowCastingMode = ShadowCastingMode.Off;
			flag = false;
			break;
		case MeshLayer.Waterway:
			num = m_WaterwayLayer;
			shadowCastingMode = ShadowCastingMode.Off;
			flag = false;
			break;
		case MeshLayer.Outline:
			num = m_OutlineLayer;
			batchFlags &= ~(BatchFlags.MotionVectors | BatchFlags.Emissive | BatchFlags.ColorMask | BatchFlags.InfoviewColor | BatchFlags.LodFade | BatchFlags.InfoviewFlow | BatchFlags.SurfaceState);
			batchFlags |= BatchFlags.Outline;
			shadowCastingMode = ShadowCastingMode.Off;
			flag = false;
			break;
		case MeshLayer.Marker:
			num = m_MarkerLayer;
			shadowCastingMode = ShadowCastingMode.Off;
			break;
		}
		if ((batchFlags & BatchFlags.MotionVectors) != 0)
		{
			batchData.m_RenderFlags |= BatchRenderFlags.MotionVectors;
		}
		if (flag)
		{
			batchData.m_RenderFlags |= BatchRenderFlags.ReceiveShadows;
		}
		if (shadowCastingMode != ShadowCastingMode.Off)
		{
			batchData.m_RenderFlags |= BatchRenderFlags.CastShadows;
		}
		if (m_RenderingSystem.IsShaderEnabled(value.shader))
		{
			batchData.m_RenderFlags |= BatchRenderFlags.IsEnabled;
		}
		batchData.m_ShadowCastingMode = (byte)shadowCastingMode;
		batchData.m_Layer = (byte)num;
		groupData.m_RenderFlags |= batchData.m_RenderFlags;
		return new CustomBatch(groupIndex, batchIndex, surfaceAsset, material, material2, value, mesh, entity, sharedMesh, batchFlags, generatedType, groupData.m_MeshType, num2, materialPropertyBlock);
	}

	public void SetupVT(RenderPrefab meshPrefab, Material material, int materialIndex)
	{
		SurfaceAsset surfaceAsset = meshPrefab.GetSurfaceAsset(materialIndex);
		VTAtlassingInfo[] array = surfaceAsset.VTAtlassingInfos;
		if (array == null)
		{
			array = surfaceAsset.PreReservedAtlassingInfos;
		}
		if (array == null || meshPrefab.Has<DefaultMesh>())
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			if (array.Length > i && array[i].indexInStack >= 0)
			{
				m_TextureStreamingSystem.BindMaterial(material, array[i].stackGlobalIndex, i, m_TextureStreamingSystem.GetTextureParamBlock(array[i]));
			}
		}
	}

	private Material CreateMaterial(SurfaceAsset sourceSurface, Material sourceMaterial, MaterialKey materialKey)
	{
		Material material;
		if (sourceSurface != null)
		{
			material = new Material(materialKey.template)
			{
				name = "Batch (" + sourceSurface.name + ")",
				hideFlags = HideFlags.HideAndDontSave
			};
			foreach (KeyValuePair<string, float> @float in sourceSurface.floats)
			{
				material.SetFloat(@float.Key, @float.Value);
			}
			foreach (KeyValuePair<string, int> @int in sourceSurface.ints)
			{
				material.SetInt(@int.Key, @int.Value);
			}
			foreach (KeyValuePair<string, Vector4> vector in sourceSurface.vectors)
			{
				material.SetVector(vector.Key, vector.Value);
			}
			foreach (KeyValuePair<string, Color> color in sourceSurface.colors)
			{
				material.SetColor(color.Key, color.Value);
			}
			foreach (KeyValuePair<int, object> texture in materialKey.textures)
			{
				if (texture.Value is TextureAsset textureAsset)
				{
					material.SetTexture(texture.Key, textureAsset.Load());
				}
				else
				{
					material.SetTexture(texture.Key, (Texture)texture.Value);
				}
			}
			foreach (string keyword in sourceSurface.keywords)
			{
				material.EnableKeyword(keyword);
			}
			HDMaterial.ValidateMaterial(material);
		}
		else
		{
			material = new Material(sourceMaterial)
			{
				name = "Batch (" + sourceMaterial.name + ")",
				hideFlags = HideFlags.HideAndDontSave
			};
			foreach (TextureData item in m_CachedTextures)
			{
				material.SetTexture(item.nameID, item.texture);
			}
		}
		if (materialKey.decalLayerMask != -1)
		{
			material.SetFloat(m_DecalLayerMask, math.asfloat(materialKey.decalLayerMask));
		}
		if (materialKey.renderQueue != -1)
		{
			material.renderQueue = materialKey.renderQueue;
		}
		foreach (KeywordData item2 in m_CachedKeywords)
		{
			if (item2.remove)
			{
				material.DisableKeyword(item2.name);
			}
			else
			{
				material.EnableKeyword(item2.name);
			}
		}
		for (int i = 0; i < materialKey.vtStacks.Count; i++)
		{
			int num = materialKey.vtStacks[i];
			if (num >= 0)
			{
				VTAtlassingInfo[] array = sourceSurface?.VTAtlassingInfos;
				VTTextureParamBlock textureParams = ((array != null) ? m_TextureStreamingSystem.GetTextureParamBlock(array[i]) : VTTextureParamBlock.Identity);
				m_TextureStreamingSystem.BindMaterial(material, num, i, textureParams);
			}
		}
		return material;
	}

	private void EnableKeyword(MaterialKey materialKey, string keyword)
	{
		if (materialKey.keywords.Add(keyword))
		{
			m_CachedKeywords.Add(new KeywordData(keyword, remove: false));
		}
	}

	private void DisableKeyword(MaterialKey materialKey, string keyword)
	{
		if (materialKey.keywords.Remove(keyword))
		{
			m_CachedKeywords.Add(new KeywordData(keyword, remove: true));
		}
	}

	private void SetTexture(MaterialKey materialKey, int nameID, Texture texture)
	{
		if (materialKey.textures.TryGetValue(nameID, out var value))
		{
			if (texture != value)
			{
				materialKey.textures[nameID] = texture;
				m_CachedTextures.Add(new TextureData(nameID, texture));
			}
		}
		else
		{
			materialKey.textures.Add(nameID, texture);
			m_CachedTextures.Add(new TextureData(nameID, texture));
		}
	}

	public static Material GetTemplate(SurfaceAsset surfaceAsset)
	{
		Material material = surfaceAsset.GetTemplateMaterial();
		if (material == null)
		{
			material = SurfaceAsset.kDefaultMaterial;
		}
		return material;
	}

	[Preserve]
	public ManagedBatchSystem()
	{
	}
}
