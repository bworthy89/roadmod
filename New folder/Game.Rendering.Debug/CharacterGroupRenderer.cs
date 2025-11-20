using System.Collections.Generic;
using System.Linq;
using Game.Objects;
using Game.Prefabs;
using UnityEngine;

namespace Game.Rendering.Debug;

public class CharacterGroupRenderer : MonoBehaviour
{
	public CharacterGroup m_Prefab;

	public bool m_NoVT;

	public int m_CharacterIndex;

	[Range(0f, 16f)]
	public int m_OverlayColorIndex;

	[Range(0f, 3f)]
	public int m_LODIndex;

	public bool m_applyGroupOverrides;

	public ObjectState m_State;

	private ComputeBuffer m_BoneBuffer;

	private ComputeBuffer m_BoneHistoryBuffer;

	private ComputeBuffer m_MetaBuffer;

	private GameObject m_Root;

	private GameObject m_Prop;

	public bool m_Animate;

	public ActivityType m_Activity;

	public ActivityCondition m_Condition;

	public AnimationType m_TransformState;

	[Tooltip("Relevant when deductions based on Activity names are not enough to find the appropriate prop (bicycle or scooter for example)")]
	public string m_PropName;

	private Dictionary<string, GlobalBufferRenderer.MetaInstanceData> m_MetaInstanceData = new Dictionary<string, GlobalBufferRenderer.MetaInstanceData>();

	private string m_characterId => m_Prefab.m_Characters[m_CharacterIndex].m_Style.name;

	private GlobalBufferRenderer.MetaInstanceData m_currentMetaData
	{
		get
		{
			if (m_MetaInstanceData.TryGetValue(m_characterId, out var value))
			{
				return value;
			}
			value = new GlobalBufferRenderer.MetaInstanceData();
			m_MetaInstanceData.TryAdd(m_characterId, value);
			return value;
		}
	}

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
		if (!(m_Prefab != null))
		{
			return;
		}
		m_CharacterIndex = Mathf.Clamp(m_CharacterIndex, 0, m_Prefab.m_Characters.Length - 1);
		CharacterGroup.Character character = m_Prefab.m_Characters[m_CharacterIndex];
		List<RenderPrefab> list = new List<RenderPrefab>();
		CharacterGroup.Character character2 = new CharacterGroup.Character
		{
			m_Style = character.m_Style,
			m_Meta = character.m_Meta,
			m_MeshPrefabs = character.m_MeshPrefabs
		};
		if (m_applyGroupOverrides && m_State != ObjectState.None)
		{
			CharacterGroup.OverrideInfo overrideInfo = m_Prefab.m_Overrides.SingleOrDefault((CharacterGroup.OverrideInfo o) => o.m_RequireState == m_State);
			if (overrideInfo != null)
			{
				CharacterGroup.Character character3 = overrideInfo.m_Group.m_Characters[m_CharacterIndex];
				if (overrideInfo.m_OverrideShapeWeights)
				{
					character2.m_Meta.shapeWeights = character3.m_Meta.shapeWeights;
				}
				if (overrideInfo.m_overrideMaskWeights)
				{
					character2.m_Meta.maskWeights = character3.m_Meta.maskWeights;
				}
				for (int num = 0; num < character.m_MeshPrefabs.Length; num++)
				{
					RenderPrefab renderPrefab = character.m_MeshPrefabs[num];
					if (!renderPrefab.TryGet<CharacterProperties>(out var component) || (component.m_BodyParts & overrideInfo.m_OverrideBodyParts) == 0)
					{
						list.Add(renderPrefab);
					}
				}
				for (int num2 = 0; num2 < character3.m_MeshPrefabs.Length; num2++)
				{
					RenderPrefab renderPrefab2 = character3.m_MeshPrefabs[num2];
					if (!renderPrefab2.TryGet<CharacterProperties>(out var component2) || (component2.m_BodyParts & overrideInfo.m_OverrideBodyParts) != 0)
					{
						list.Add(renderPrefab2);
					}
				}
				character2.m_MeshPrefabs = list.ToArray();
			}
		}
		m_Root = new GameObject($"{m_Prefab.name} index {m_CharacterIndex}");
		m_Root.transform.parent = base.transform;
		m_Root.transform.localPosition = Vector3.zero;
		SetupBuffers(character2);
		RenderPrefab[] meshPrefabs = character2.m_MeshPrefabs;
		foreach (RenderPrefab renderPrefab3 in meshPrefabs)
		{
			GameObject gameObject = new GameObject(renderPrefab3.name);
			gameObject.transform.parent = m_Root.transform;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.SetActive(value: false);
			RenderPrefabRenderer renderPrefabRenderer = gameObject.AddComponent<RenderPrefabRenderer>();
			renderPrefabRenderer.m_NoVT = m_NoVT;
			renderPrefabRenderer.m_Prefab = renderPrefab3;
			gameObject.SetActive(value: true);
			renderPrefabRenderer.m_LODIndex = m_LODIndex;
		}
	}

	private UnityEngine.Color GetBlendColor(RenderPrefab[] renderPrefabs, BlendWeight weight)
	{
		UnityEngine.Color color = UnityEngine.Color.white;
		int num = 0;
		for (int i = 0; i < renderPrefabs.Length; i++)
		{
			if (!renderPrefabs[i].TryGet<CharacterProperties>(out var component))
			{
				continue;
			}
			CharacterOverlay[] overlays = component.m_Overlays;
			for (int j = 0; j < overlays.Length; j++)
			{
				if (overlays[j].TryGet<CharacterOverlay>(out var component2) && component2.m_Index == weight.m_Index && component2.TryGet<ColorProperties>(out var component3))
				{
					if (num == 0)
					{
						color = new UnityEngine.Color(0f, 0f, 0f, 0f);
					}
					color += component3.GetColor(m_OverlayColorIndex, 0).linear * weight.m_Weight;
					num++;
				}
			}
		}
		if (num > 0)
		{
			return color / num;
		}
		return UnityEngine.Color.white;
	}

	private BlendColors GetBlendColors(RenderPrefab[] renderPrefabs, BlendWeights weights)
	{
		return new BlendColors
		{
			m_Color0 = GetBlendColor(renderPrefabs, weights.m_Weight0),
			m_Color1 = GetBlendColor(renderPrefabs, weights.m_Weight1),
			m_Color2 = GetBlendColor(renderPrefabs, weights.m_Weight2),
			m_Color3 = GetBlendColor(renderPrefabs, weights.m_Weight3),
			m_Color4 = GetBlendColor(renderPrefabs, weights.m_Weight4),
			m_Color5 = GetBlendColor(renderPrefabs, weights.m_Weight5),
			m_Color6 = GetBlendColor(renderPrefabs, weights.m_Weight6),
			m_Color7 = GetBlendColor(renderPrefabs, weights.m_Weight7)
		};
	}

	private unsafe void SetupBuffers(CharacterGroup.Character character)
	{
		BlendWeights blendWeights = RenderingUtils.GetBlendWeights(character.m_Meta.overlayWeights);
		BlendColors blendColors = GetBlendColors(character.m_MeshPrefabs, blendWeights);
		GlobalBufferRenderer.MetaInstanceData value;
		MetaBufferData metaBufferData = (m_MetaInstanceData.TryGetValue(m_characterId, out value) ? value.m_MetaData : new MetaBufferData
		{
			m_BoneCount = character.m_Style.m_BoneCount,
			m_ShapeCount = character.m_Style.m_ShapeCount,
			m_MetaIndexLink = -1,
			m_BoneLink = -1
		});
		metaBufferData.m_OverlayWeights = blendWeights;
		metaBufferData.m_OverlayColors1 = blendColors;
		metaBufferData.m_ShapeWeights = RenderingUtils.GetBlendWeights(character.m_Meta.shapeWeights);
		metaBufferData.m_TextureWeights = RenderingUtils.GetBlendWeights(character.m_Meta.textureWeights);
		metaBufferData.m_MaskWeights = RenderingUtils.GetBlendWeights(character.m_Meta.maskWeights);
		m_BoneBuffer = new ComputeBuffer(character.m_Style.m_BoneCount, sizeof(BoneElement), ComputeBufferType.Structured);
		m_BoneHistoryBuffer = new ComputeBuffer(character.m_Style.m_BoneCount, sizeof(BoneElement), ComputeBufferType.Structured);
		m_MetaBuffer = new ComputeBuffer(1, sizeof(MetaBufferData), ComputeBufferType.Structured);
		value = UpdateMetaInstanceData();
		GlobalBufferRenderer.Instance.SetupAnimationBuffers(m_Prefab.m_Characters[m_CharacterIndex].m_Style, metaBufferData, ref value, m_PropName, out var propPrefab);
		if (propPrefab != null)
		{
			SpawnLinkedProp(propPrefab);
		}
		m_MetaInstanceData[m_characterId] = value;
		MetaBufferData[] data = new MetaBufferData[1] { value.m_MetaData };
		m_MetaBuffer.SetData(data);
	}

	private GlobalBufferRenderer.MetaInstanceData UpdateMetaInstanceData()
	{
		GlobalBufferRenderer.MetaInstanceData currentMetaData = m_currentMetaData;
		if (m_Animate)
		{
			currentMetaData.m_Activity = m_Activity;
			currentMetaData.m_Condition = m_Condition;
			currentMetaData.m_TransformState = m_TransformState;
		}
		else
		{
			currentMetaData.m_Activity = ActivityType.None;
			currentMetaData.m_TransformState = AnimationType.None;
		}
		return currentMetaData;
	}

	private void SpawnLinkedProp(GlobalBufferRenderer.PropStyleData propStyleData)
	{
		GameObject gameObject = new GameObject(propStyleData.m_RenderPrefab.name);
		gameObject.transform.parent = base.transform;
		gameObject.transform.position = base.transform.position;
		gameObject.SetActive(value: false);
		PropRenderer propRenderer = gameObject.AddComponent<PropRenderer>();
		propRenderer.SetInheritedAnimationData(m_currentMetaData.m_MetaIndex, propStyleData, m_Activity, m_Condition, m_TransformState, m_Prefab.m_Characters[m_CharacterIndex].m_Style.m_Gender);
		propRenderer.m_Animate = m_Animate;
		propRenderer.m_LODIndex = m_LODIndex;
		gameObject.SetActive(value: true);
		m_Prop = gameObject;
	}

	public void SetCharacterProperties(ref MaterialPropertyBlock block)
	{
		if (m_BoneBuffer != null && m_MetaBuffer != null)
		{
			block.SetBuffer("boneBuffer", m_BoneBuffer);
			block.SetBuffer("boneHistoryBuffer", m_BoneHistoryBuffer);
			block.SetBuffer("metaBuffer", m_MetaBuffer);
		}
	}

	private void ReleaseRenderer()
	{
		if (m_Root != null)
		{
			UnityEngine.Object.Destroy(m_Root);
		}
		if (m_Prop != null)
		{
			UnityEngine.Object.Destroy(m_Prop);
		}
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
