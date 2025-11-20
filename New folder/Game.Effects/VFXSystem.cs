using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Rendering;
using Game.Rendering.Utilities;
using Game.Serialization;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace Game.Effects;

[CompilerGenerated]
public class VFXSystem : GameSystemBase, IPreDeserialize
{
	private static class VFXIDs
	{
		public static readonly int WindTexture = Shader.PropertyToID("WindTexture");

		public static readonly int MapOffsetScale = Shader.PropertyToID("MapOffsetScale");

		public static readonly int Count = Shader.PropertyToID("Count");

		public static readonly int InstanceData = Shader.PropertyToID("InstanceData");
	}

	private struct EffectInfo
	{
		public VisualEffect m_VisualEffect;

		public Texture2D m_Texture;

		public NativeArray<int> m_Instances;

		public NativeParallelHashMap<int, int> m_Indices;

		public int m_LastCount;

		public bool m_NeedApply;
	}

	[BurstCompile]
	private struct VFXTextureUpdateJob : IJob
	{
		public NativeArray<float4> m_TextureData;

		[ReadOnly]
		public NativeArray<int> m_Instances;

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledData;

		public int m_Count;

		public int m_TextureWidth;

		public void Execute()
		{
			for (int i = 0; i < m_Count; i++)
			{
				int num = m_Instances[i];
				if (num == -1)
				{
					m_TextureData.ElementAt(i).w = 0f;
					continue;
				}
				EnabledEffectData enabledEffectData = m_EnabledData[num];
				Quaternion quaternion = enabledEffectData.m_Rotation;
				m_TextureData[i] = new float4(enabledEffectData.m_Position, enabledEffectData.m_Intensity);
				m_TextureData[i + m_TextureWidth] = new float4(MathF.PI / 180f * quaternion.eulerAngles, 0f);
				m_TextureData[i + 2 * m_TextureWidth] = new float4(enabledEffectData.m_Scale, 0f);
			}
		}
	}

	private Queue<NativeQueue<VFXUpdateInfo>> m_SourceUpdateQueue;

	private JobHandle m_SourceUpdateWriter;

	private EntityQuery m_VFXPrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private bool m_Initialized;

	private EffectInfo[] m_Effects;

	private JobHandle m_TextureUpdate;

	private WindTextureSystem m_WindTextureSystem;

	private TerrainSystem m_TerrainSystem;

	private EffectControlSystem m_EffectControlSystem;

	private RenderingSystem m_RenderingSystem;

	public NativeQueue<VFXUpdateInfo> GetSourceUpdateData()
	{
		NativeQueue<VFXUpdateInfo> nativeQueue = new NativeQueue<VFXUpdateInfo>(Allocator.TempJob);
		m_SourceUpdateQueue.Enqueue(nativeQueue);
		return nativeQueue;
	}

	public void AddSourceUpdateWriter(JobHandle jobHandle)
	{
		m_SourceUpdateWriter = JobHandle.CombineDependencies(m_SourceUpdateWriter, jobHandle);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SourceUpdateQueue = new Queue<NativeQueue<VFXUpdateInfo>>();
		m_VFXPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<VFXData>());
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_WindTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
	}

	private bool Initialize()
	{
		if (!m_Initialized && !m_VFXPrefabQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_VFXPrefabQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<VFXData> nativeArray2 = m_VFXPrefabQuery.ToComponentDataArray<VFXData>(Allocator.TempJob);
			m_Effects = new EffectInfo[nativeArray.Length];
			for (int i = 0; i < nativeArray.Length; i++)
			{
				base.World.EntityManager.GetComponentData<VFXData>(nativeArray[i]);
				EffectPrefab prefab = m_PrefabSystem.GetPrefab<EffectPrefab>(nativeArray[i]);
				VFX component = prefab.GetComponent<VFX>();
				VisualEffect visualEffect = new GameObject("VFX " + prefab.name).AddComponent<VisualEffect>();
				visualEffect.visualEffectAsset = component.m_Effect;
				visualEffect.SetCheckedInt(VFXIDs.Count, 0);
				m_Effects[i].m_VisualEffect = visualEffect;
				VFXData componentData = nativeArray2[i];
				componentData.m_MaxCount = component.m_MaxCount;
				componentData.m_Index = i;
				base.World.EntityManager.SetComponentData(nativeArray[i], componentData);
				Texture2D texture2D = new Texture2D(component.m_MaxCount, 3, GraphicsFormat.R32G32B32A32_SFloat, 1, TextureCreationFlags.None)
				{
					name = "VFXTexture " + prefab.name,
					hideFlags = HideFlags.HideAndDontSave
				};
				m_Effects[i].m_Texture = texture2D;
				visualEffect.SetCheckedTexture(VFXIDs.InstanceData, texture2D);
				m_Effects[i].m_Instances = new NativeArray<int>(componentData.m_MaxCount, Allocator.Persistent);
				m_Effects[i].m_Indices = new NativeParallelHashMap<int, int>(componentData.m_MaxCount, Allocator.Persistent);
			}
			nativeArray2.Dispose();
			nativeArray.Dispose();
			m_Initialized = true;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_Initialized && m_Effects != null)
		{
			for (int i = 0; i < m_Effects.Length; i++)
			{
				UnityEngine.Object.Destroy(m_Effects[i].m_Texture);
				if (m_Effects[i].m_Instances.IsCreated)
				{
					m_Effects[i].m_Instances.Dispose();
				}
				if (m_Effects[i].m_Indices.IsCreated)
				{
					m_Effects[i].m_Indices.Dispose();
				}
			}
		}
		ClearQueue();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_Initialized && !Initialize())
		{
			ClearQueue();
			return;
		}
		m_TextureUpdate.Complete();
		m_SourceUpdateWriter.Complete();
		JobHandle dependencies;
		NativeList<EnabledEffectData> enabledData = m_EffectControlSystem.GetEnabledData(readOnly: true, out dependencies);
		NativeQueue<VFXUpdateInfo> result;
		while (m_SourceUpdateQueue.TryDequeue(out result))
		{
			if (!result.IsEmpty())
			{
				dependencies.Complete();
				VFXUpdateInfo item;
				while (result.TryDequeue(out item))
				{
					EnabledEffectData enabledEffectData = enabledData[item.m_EnabledIndex.x];
					if (!base.EntityManager.TryGetComponent<VFXData>(enabledEffectData.m_Prefab, out var component))
					{
						continue;
					}
					int index = component.m_Index;
					switch (item.m_Type)
					{
					case VFXUpdateType.Add:
						if (!m_Effects[index].m_Indices.ContainsKey(item.m_EnabledIndex.x) && index >= 0 && m_Effects[index].m_Indices.Count() < m_Effects[index].m_Instances.Length)
						{
							int num = m_Effects[index].m_Indices.Count();
							m_Effects[index].m_Instances[num] = item.m_EnabledIndex.x;
							m_Effects[index].m_Indices[item.m_EnabledIndex.x] = num;
							if (m_Effects[index].m_VisualEffect != null)
							{
								m_Effects[index].m_VisualEffect.SetCheckedInt(VFXIDs.Count, m_Effects[index].m_Indices.Count());
							}
						}
						break;
					case VFXUpdateType.Remove:
						if (m_Effects[index].m_Indices.ContainsKey(item.m_EnabledIndex.x))
						{
							int num2 = m_Effects[index].m_Instances[m_Effects[index].m_Indices.Count() - 1];
							int num3 = m_Effects[index].m_Indices[item.m_EnabledIndex.x];
							if (item.m_EnabledIndex.x != num2)
							{
								m_Effects[index].m_Instances[num3] = num2;
								m_Effects[index].m_Indices[num2] = num3;
							}
							m_Effects[index].m_Instances[m_Effects[index].m_Indices.Count() - 1] = -1;
							m_Effects[index].m_Indices.Remove(item.m_EnabledIndex.x);
							if (m_Effects[index].m_VisualEffect != null)
							{
								m_Effects[index].m_VisualEffect.SetCheckedInt(VFXIDs.Count, m_Effects[index].m_Indices.Count());
							}
						}
						break;
					case VFXUpdateType.MoveIndex:
					{
						if (m_Effects[index].m_Indices.TryGetValue(item.m_EnabledIndex.y, out var item2))
						{
							m_Effects[index].m_Indices.Remove(item.m_EnabledIndex.y);
							m_Effects[index].m_Indices[item.m_EnabledIndex.x] = item2;
							m_Effects[index].m_Instances[item2] = item.m_EnabledIndex.x;
						}
						break;
					}
					}
				}
			}
			result.Dispose();
		}
		float playRate = m_RenderingSystem.frameDelta / math.max(1E-06f, base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime * 60f);
		for (int i = 0; i < m_Effects.Length; i++)
		{
			if (m_Effects[i].m_VisualEffect != null)
			{
				m_Effects[i].m_VisualEffect.playRate = playRate;
				m_Effects[i].m_VisualEffect.SetCheckedTexture(VFXIDs.WindTexture, m_WindTextureSystem.WindTexture);
				m_Effects[i].m_VisualEffect.SetCheckedVector4(VFXIDs.MapOffsetScale, m_TerrainSystem.mapOffsetScale);
			}
			int num4 = math.max(m_Effects[i].m_Indices.Count(), m_Effects[i].m_LastCount);
			if (m_Effects[i].m_NeedApply)
			{
				m_Effects[i].m_Texture.Apply();
				m_Effects[i].m_NeedApply = false;
			}
			m_Effects[i].m_VisualEffect.SetCheckedInt(VFXIDs.Count, m_Effects[i].m_LastCount);
			m_Effects[i].m_LastCount = m_Effects[i].m_Indices.Count();
			if (num4 != 0)
			{
				VFXTextureUpdateJob jobData = new VFXTextureUpdateJob
				{
					m_TextureData = m_Effects[i].m_Texture.GetRawTextureData<float4>(),
					m_Instances = m_Effects[i].m_Instances,
					m_EnabledData = enabledData,
					m_Count = num4,
					m_TextureWidth = m_Effects[i].m_Texture.width
				};
				m_Effects[i].m_NeedApply = true;
				m_TextureUpdate = JobHandle.CombineDependencies(m_TextureUpdate, IJobExtensions.Schedule(jobData, dependencies));
			}
		}
		m_EffectControlSystem.AddEnabledDataReader(m_TextureUpdate);
	}

	public void PreDeserialize(Context context)
	{
		if (m_Initialized && m_Effects != null)
		{
			m_TextureUpdate.Complete();
			for (int i = 0; i < m_Effects.Length; i++)
			{
				if (m_Effects[i].m_VisualEffect != null)
				{
					m_Effects[i].m_VisualEffect.SetCheckedInt(VFXIDs.Count, 0);
					UnityEngine.Object.Destroy(m_Effects[i].m_VisualEffect.gameObject);
				}
				if (m_Effects[i].m_Instances.IsCreated)
				{
					m_Effects[i].m_Instances.Dispose();
				}
				if (m_Effects[i].m_Indices.IsCreated)
				{
					m_Effects[i].m_Indices.Dispose();
				}
			}
			m_Initialized = false;
		}
		ClearQueue();
	}

	private void ClearQueue()
	{
		m_SourceUpdateWriter.Complete();
		NativeQueue<VFXUpdateInfo> result;
		while (m_SourceUpdateQueue.TryDequeue(out result))
		{
			result.Dispose();
		}
	}

	[Preserve]
	public VFXSystem()
	{
	}
}
