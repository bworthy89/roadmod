using Game.Prefabs;
using UnityEngine;

namespace Game.Rendering.Debug;

public class PropRenderer : MonoBehaviour
{
	public RenderPrefab m_RenderPrefab;

	public ActivityPropPrefab m_ActivityPropPrefab;

	public bool m_NoVT;

	private ComputeBuffer m_BoneBuffer;

	private ComputeBuffer m_BoneHistoryBuffer;

	private ComputeBuffer m_MetaBuffer;

	private GameObject m_Root;

	[Range(0f, 3f)]
	public int m_LODIndex;

	public bool m_Animate;

	public int m_CharacterMetaIndex = -1;

	public int m_BoneLink = 116;

	public ActivityType m_Activity;

	public ActivityCondition m_Condition;

	public AnimationType m_TransformState;

	public GenderMask m_Gender;

	private GlobalBufferRenderer.MetaInstanceData m_MetaInstanceData = new GlobalBufferRenderer.MetaInstanceData();

	private void OnEnable()
	{
		CreateRenderer();
	}

	private void Update()
	{
		GlobalBufferRenderer.Instance.RetrieveBoneBuffers(out var boneBuffer, out var boneHistoryBuffer);
		m_BoneBuffer = boneBuffer;
		m_BoneHistoryBuffer = boneHistoryBuffer;
	}

	public void Recreate()
	{
		ReleaseRenderer();
		CreateRenderer();
	}

	private void CreateRenderer()
	{
		if (m_RenderPrefab != null)
		{
			m_Root = new GameObject(m_RenderPrefab.name);
			m_Root.transform.parent = base.transform;
			m_Root.transform.localPosition = Vector3.zero;
			SetupBuffers();
			GameObject gameObject = new GameObject(m_RenderPrefab.name);
			gameObject.transform.parent = m_Root.transform;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.SetActive(value: false);
			RenderPrefabRenderer renderPrefabRenderer = gameObject.AddComponent<RenderPrefabRenderer>();
			renderPrefabRenderer.m_NoVT = m_NoVT;
			renderPrefabRenderer.m_Prefab = m_RenderPrefab;
			gameObject.SetActive(value: true);
			renderPrefabRenderer.m_LODIndex = m_LODIndex;
		}
	}

	public void SetInheritedAnimationData(int characterMetaIndex, GlobalBufferRenderer.PropStyleData propStyleData, ActivityType activityType, ActivityCondition condition, AnimationType transformState, GenderMask gender)
	{
		m_RenderPrefab = propStyleData.m_RenderPrefab;
		m_ActivityPropPrefab = propStyleData.m_PrefabData;
		m_CharacterMetaIndex = characterMetaIndex;
		m_Activity = activityType;
		m_Condition = condition;
		m_TransformState = transformState;
		m_Gender = gender;
	}

	private unsafe void SetupBuffers()
	{
		int boneLink = (m_RenderPrefab.Has<ProceduralAnimationProperties>() ? (-1) : m_BoneLink);
		MetaBufferData metaBufferData = new MetaBufferData
		{
			m_BoneCount = m_ActivityPropPrefab.m_BoneCount,
			m_ShapeCount = 1,
			m_MetaIndexLink = m_CharacterMetaIndex,
			m_BoneLink = boneLink
		};
		m_BoneBuffer = new ComputeBuffer(m_ActivityPropPrefab.m_BoneCount, sizeof(BoneElement), ComputeBufferType.Structured);
		m_BoneHistoryBuffer = new ComputeBuffer(m_ActivityPropPrefab.m_BoneCount, sizeof(BoneElement), ComputeBufferType.Structured);
		m_MetaBuffer = new ComputeBuffer(1, sizeof(MetaBufferData), ComputeBufferType.Structured);
		UpdateMetaInstanceData();
		GlobalBufferRenderer.Instance.SetupAnimationBuffers(m_ActivityPropPrefab, ref metaBufferData, ref m_MetaInstanceData, metaBufferData.m_BoneLink != -1);
		MetaBufferData[] data = new MetaBufferData[1] { m_MetaInstanceData.m_MetaData };
		m_MetaBuffer.SetData(data);
	}

	public void SetPropProperties(ref MaterialPropertyBlock block)
	{
		if (m_BoneBuffer != null && m_MetaBuffer != null)
		{
			block.SetBuffer("boneBuffer", m_BoneBuffer);
			block.SetBuffer("boneHistoryBuffer", m_BoneHistoryBuffer);
			block.SetBuffer("metaBuffer", m_MetaBuffer);
		}
	}

	private void UpdateMetaInstanceData()
	{
		if (m_Animate)
		{
			m_MetaInstanceData.m_Activity = m_Activity;
			m_MetaInstanceData.m_Condition = m_Condition;
			m_MetaInstanceData.m_TransformState = m_TransformState;
		}
		else
		{
			m_MetaInstanceData.m_Activity = ActivityType.None;
			m_MetaInstanceData.m_TransformState = AnimationType.None;
		}
		m_MetaInstanceData.m_Gender = m_Gender;
	}

	private void ReleaseRenderer()
	{
		Object.Destroy(m_Root);
		if (m_BoneBuffer != null)
		{
			m_BoneBuffer = null;
		}
		if (m_BoneHistoryBuffer != null)
		{
			m_BoneHistoryBuffer = null;
		}
		if (m_MetaBuffer != null)
		{
			m_MetaBuffer.Release();
			m_MetaBuffer = null;
		}
	}

	private void OnDisable()
	{
		ReleaseRenderer();
	}
}
