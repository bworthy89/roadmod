using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Animations;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using Colossal.Rendering;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;

namespace Game.Rendering.Debug;

public class RenderPrefabRenderer : MonoBehaviour
{
	public class Instance
	{
		private VTTextureRequester m_VTTexturesRequester;

		private List<int> m_VTTexturesIndices = new List<int>();

		private GameObject m_Root;

		private List<MeshRenderer> m_MeshRenderers = new List<MeshRenderer>();

		private Bounds3 m_Bounds;

		private RenderPrefabRenderer m_Owner;

		private ColorProperties m_ColorProperties;

		private EmissiveProperties m_EmissiveProperties;

		public ProceduralAnimationProperties m_ProceduralAnimationProperties;

		private CharacterGroupRenderer m_CharacterGroupRenderer;

		private PropRenderer m_PropRenderer;

		private DecalProperties m_DecalProperties;

		private ComputeBuffer m_LightBuffer;

		private ComputeBuffer m_AnimationBuffer;

		private RenderPrefab m_Prefab;

		private Dictionary<string, Transform> m_BoneMap = new Dictionary<string, Transform>();

		private Matrix4x4[] m_SkinMatrices;

		public Bounds3 bounds => m_Prefab.bounds;

		public Matrix4x4 localToWorldMatrix => m_Root.transform.localToWorldMatrix;

		public string name => m_Prefab.name;

		public GameObject root => m_Root;

		public bool enabled
		{
			get
			{
				return m_Root.activeInHierarchy;
			}
			set
			{
				m_Root.SetActive(value);
			}
		}

		public string GetStats()
		{
			return $"Vertices: {m_Prefab.vertexCount} Triangles: {m_Prefab.indexCount / 3}";
		}

		private GlobalBufferRenderer.ShapeAllocation[] GetShapeAllocations()
		{
			return GlobalBufferRenderer.Instance.GetOrAddShapeData(m_Prefab);
		}

		private GlobalBufferRenderer.OverlayAllocation[] GetOverlayAllocations()
		{
			return GlobalBufferRenderer.Instance.GetOrAddOverlayData(m_Prefab);
		}

		private void SetShaderPass(Material[] materials, string passName, bool enabled)
		{
			for (int i = 0; i < materials.Length; i++)
			{
				materials[i].SetShaderPassEnabled(passName, enabled);
			}
		}

		private void SetKeyword(Material[] materials, string keywordName, bool enabled)
		{
			foreach (Material material in materials)
			{
				LocalKeyword keyword = material.shader.keywordSpace.FindKeyword(keywordName);
				if (keyword.isValid)
				{
					material.SetKeyword(in keyword, enabled);
				}
			}
		}

		public Instance(RenderPrefabRenderer mpr, RenderPrefab basePrefab, RenderPrefab prefab, bool useVT = true)
		{
			m_Owner = mpr;
			m_Prefab = prefab;
			m_Bounds = prefab.bounds;
			m_Root = new GameObject(prefab.name);
			m_Root.transform.parent = mpr.transform;
			m_Root.transform.localPosition = Vector3.zero;
			m_ColorProperties = basePrefab.GetComponent<ColorProperties>();
			m_EmissiveProperties = basePrefab.GetComponent<EmissiveProperties>();
			m_ProceduralAnimationProperties = prefab.GetComponent<ProceduralAnimationProperties>();
			m_CharacterGroupRenderer = mpr.transform.parent?.parent?.GetComponent<CharacterGroupRenderer>();
			m_PropRenderer = mpr.transform.parent?.parent?.GetComponent<PropRenderer>();
			m_DecalProperties = basePrefab.GetComponent<DecalProperties>();
			Mesh[] array = prefab.ObtainMeshes();
			int num = 0;
			Material[] array2 = prefab.ObtainMaterials(useVT);
			GlobalBufferRenderer.Instance.GetOrAddShapeData(m_Owner.m_Prefab);
			if (useVT)
			{
				TextureStreamingSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
				for (int i = 0; i < array2.Length; i++)
				{
					Material material = array2[i];
					material.SetFloat(ShaderIDs._LodFade, 1f);
					SurfaceAsset surfaceAsset = prefab.GetSurfaceAsset(i);
					VTAtlassingInfo[] array3 = surfaceAsset.VTAtlassingInfos;
					if (array3 == null)
					{
						array3 = surfaceAsset.PreReservedAtlassingInfos;
					}
					if (array3 == null)
					{
						continue;
					}
					if (m_DecalProperties != null)
					{
						Bounds2 bounds = MathUtils.Bounds(m_DecalProperties.m_TextureArea.min, m_DecalProperties.m_TextureArea.max);
						if (m_VTTexturesRequester == null)
						{
							m_VTTexturesRequester = new VTTextureRequester(existingSystemManaged);
						}
						if (array3.Length >= 1 && array3[0].indexInStack >= 0)
						{
							m_VTTexturesIndices.Add(m_VTTexturesRequester.RegisterTexture(0, array3[0].stackGlobalIndex, array3[0].indexInStack, bounds));
						}
						if (array3.Length >= 2 && array3[1].indexInStack >= 0)
						{
							m_VTTexturesIndices.Add(m_VTTexturesRequester.RegisterTexture(1, array3[1].stackGlobalIndex, array3[1].indexInStack, bounds));
						}
					}
					if ((material.IsKeywordEnabled("_ALPHATEST_ON") || prefab.manualVTRequired || prefab.isImpostor) && m_DecalProperties == null)
					{
						Bounds2 bounds2 = MathUtils.Bounds(new float2(0f, 0f), new float2(1f, 1f));
						if (m_VTTexturesRequester == null)
						{
							m_VTTexturesRequester = new VTTextureRequester(existingSystemManaged);
						}
						if (array3.Length >= 1 && array3[0].indexInStack >= 0)
						{
							m_VTTexturesIndices.Add(m_VTTexturesRequester.RegisterTexture(0, array3[0].stackGlobalIndex, array3[0].indexInStack, bounds2));
						}
						if (array3.Length >= 2 && array3[1].indexInStack >= 0)
						{
							m_VTTexturesIndices.Add(m_VTTexturesRequester.RegisterTexture(1, array3[1].stackGlobalIndex, array3[1].indexInStack, bounds2));
						}
					}
					for (int j = 0; j < 2; j++)
					{
						if (array3.Length > j && array3[j].indexInStack >= 0)
						{
							material.EnableKeyword(material.shader.keywordSpace.FindKeyword("ENABLE_VT"));
							existingSystemManaged.BindMaterial(material, array3[j].stackGlobalIndex, j, existingSystemManaged.GetTextureParamBlock(array3[j]));
						}
					}
				}
			}
			else
			{
				Material[] array4 = array2;
				foreach (Material obj in array4)
				{
					obj.DisableKeyword(obj.shader.keywordSpace.FindKeyword("ENABLE_VT"));
				}
			}
			Mesh[] array5 = array;
			foreach (Mesh mesh in array5)
			{
				try
				{
					GameObject gameObject = new GameObject(mesh.name);
					gameObject.transform.parent = m_Root.transform;
					gameObject.transform.localPosition = Vector3.zero;
					MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
					if (m_CharacterGroupRenderer != null)
					{
						Bounds bounds3 = mesh.bounds;
						bounds3.Expand(new Vector3(0f, 5f, 0f));
						mesh.bounds = bounds3;
					}
					meshFilter.sharedMesh = mesh;
					MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
					meshRenderer.sharedMaterials = new ReadOnlySpan<Material>(array2, num, mesh.subMeshCount).ToArray();
					SetShaderPass(meshRenderer.sharedMaterials, "MOTIONVECTORS", (m_ProceduralAnimationProperties != null && m_ProceduralAnimationProperties.active) || m_CharacterGroupRenderer != null);
					SetKeyword(meshRenderer.sharedMaterials, "_GPU_ANIMATION_PROCEDURAL", m_ProceduralAnimationProperties != null && m_ProceduralAnimationProperties.active);
					SetKeyword(meshRenderer.sharedMaterials, "_GPU_ANIMATION_SHAPE", m_CharacterGroupRenderer != null || m_PropRenderer != null);
					num += mesh.subMeshCount;
					m_MeshRenderers.Add(meshRenderer);
				}
				catch (Exception arg)
				{
					UnityEngine.Debug.LogError($"Error with {basePrefab.name} {arg}", basePrefab);
				}
			}
			SetupEmissiveProperties(-1);
			SetupProceduralAnimationProperties();
			SetupAnimationProperties();
		}

		private float4x4 GetBone(string name, ProceduralAnimationProperties.BoneInfo[] bones, Transform root)
		{
			string text = m_Owner.name;
			string text2 = name + "@" + text;
			if (!m_BoneMap.TryGetValue(text2, out var value))
			{
				for (int i = 0; i < bones.Length; i++)
				{
					ProceduralAnimationProperties.BoneInfo boneInfo = bones[i];
					string text3 = boneInfo.name + "@" + text;
					GameObject gameObject = new GameObject(boneInfo.name);
					m_BoneMap.Add(text3, gameObject.transform);
					m_Owner.RegisterForAnimation(text3, i, gameObject.transform, boneInfo);
				}
				foreach (ProceduralAnimationProperties.BoneInfo boneInfo2 in bones)
				{
					string text4 = boneInfo2.name + "@" + text;
					Transform transform = m_BoneMap[text4];
					if (text4 == text2)
					{
						value = transform;
					}
					if (boneInfo2.parentId == -1)
					{
						transform.parent = root;
						transform.localPosition = boneInfo2.position;
						transform.localRotation = boneInfo2.rotation;
						transform.localScale = boneInfo2.scale;
					}
					else
					{
						string key = bones[boneInfo2.parentId].name + "@" + text;
						transform.parent = m_BoneMap[key];
						transform.localPosition = boneInfo2.position;
						transform.localRotation = boneInfo2.rotation;
						transform.localScale = boneInfo2.scale;
					}
				}
			}
			return math.mul(root.transform.worldToLocalMatrix, value.localToWorldMatrix);
		}

		private void SetupAnimationProperties()
		{
		}

		private void SetupProceduralAnimationProperties()
		{
			if (m_ProceduralAnimationProperties != null && m_ProceduralAnimationProperties.active)
			{
				Transform transform = m_MeshRenderers[0].transform;
				int num = m_ProceduralAnimationProperties.m_Bones.Length;
				if (m_AnimationBuffer == null)
				{
					m_AnimationBuffer = new ComputeBuffer(num * 2, 64, ComputeBufferType.Raw);
				}
				if (m_SkinMatrices == null || m_SkinMatrices.Length != num * 2)
				{
					m_SkinMatrices = new Matrix4x4[num * 2];
				}
				_ = (float4x4)transform.localToWorldMatrix;
				for (int i = 0; i < num; i++)
				{
					ProceduralAnimationProperties.BoneInfo boneInfo = m_ProceduralAnimationProperties.m_Bones[i];
					float4x4 bone = GetBone(boneInfo.name, m_ProceduralAnimationProperties.m_Bones, transform);
					m_SkinMatrices[i + num] = m_SkinMatrices[i];
					m_SkinMatrices[i] = math.mul(bone, boneInfo.bindPose);
				}
				m_AnimationBuffer.SetData(m_SkinMatrices);
			}
		}

		private void SetupEmissiveProperties(int lightIndex)
		{
			if (!(m_EmissiveProperties != null) || !m_EmissiveProperties.active)
			{
				return;
			}
			int num = m_EmissiveProperties.lightsCount + 1;
			if (m_LightBuffer == null)
			{
				m_LightBuffer = new ComputeBuffer(num, 16, ComputeBufferType.Raw);
			}
			List<Color> list = new List<Color>(num);
			list.Add(new Color(0f, 0f, 0f, 0f));
			if (m_EmissiveProperties.hasSingleLights)
			{
				foreach (EmissiveProperties.SingleLightMapping singleLight in m_EmissiveProperties.m_SingleLights)
				{
					list.Add(new Color(singleLight.color.r, singleLight.color.g, singleLight.color.b, singleLight.intensity * 100f).linear);
				}
			}
			if (m_EmissiveProperties.hasMultiLights)
			{
				if (lightIndex == -1)
				{
					foreach (EmissiveProperties.MultiLightMapping multiLight in m_EmissiveProperties.m_MultiLights)
					{
						list.Add(new Color(multiLight.color.r, multiLight.color.g, multiLight.color.b, multiLight.intensity * 100f).linear);
					}
				}
				else
				{
					for (int i = 0; i < m_EmissiveProperties.m_MultiLights.Count; i++)
					{
						EmissiveProperties.MultiLightMapping multiLightMapping = m_EmissiveProperties.m_MultiLights[i];
						if (i == lightIndex)
						{
							list.Add(new Color(multiLightMapping.color.r, multiLightMapping.color.g, multiLightMapping.color.b, multiLightMapping.intensity * 100f).linear);
						}
						else
						{
							list.Add(new Color(multiLightMapping.color.r, multiLightMapping.color.g, multiLightMapping.color.b, 0f));
						}
					}
				}
			}
			m_LightBuffer.SetData(list);
		}

		public void Update()
		{
			if (!enabled || m_VTTexturesRequester == null)
			{
				return;
			}
			float maxPixelSize = GetMaxPixelSize();
			for (int i = 0; i < m_VTTexturesIndices.Count; i++)
			{
				if (m_VTTexturesIndices[i] >= 0)
				{
					m_VTTexturesRequester.UpdateMaxPixel(0, m_VTTexturesIndices[i], maxPixelSize);
				}
			}
			m_VTTexturesRequester.UpdateTexturesVTRequests();
		}

		public void Dispose()
		{
			m_Prefab.Release();
			m_VTTexturesRequester?.Dispose();
			m_LightBuffer?.Dispose();
			m_AnimationBuffer?.Dispose();
			CoreUtils.Destroy(m_Root);
		}

		public void SetWindowProperties(float randomWin, ref MaterialPropertyBlock block)
		{
			block.SetVector(ShaderIDs._BuildingState, new Vector4(0f, randomWin, 0f, 0f));
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetColorProperties(int index, ref MaterialPropertyBlock block)
		{
			if (!(m_ColorProperties != null) || !m_ColorProperties.active || index <= -1 || !m_ColorProperties.active)
			{
				return;
			}
			block.SetColor(ShaderIDs._ColorMask0, m_ColorProperties.GetColor(index, 0));
			block.SetColor(ShaderIDs._ColorMask1, m_ColorProperties.GetColor(index, 1));
			block.SetColor(ShaderIDs._ColorMask2, m_ColorProperties.GetColor(index, 2));
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetDecalProperties(ref MaterialPropertyBlock block)
		{
			if (!(m_DecalProperties != null) || !m_DecalProperties.active)
			{
				return;
			}
			block.SetVector(ShaderIDs._TextureArea, new float4(m_DecalProperties.m_TextureArea.min, m_DecalProperties.m_TextureArea.max));
			block.SetVector(ShaderIDs._MeshSize, new float4(MathUtils.Size(m_Bounds), 0f));
			block.SetFloat(ShaderIDs._DecalLayerMask, math.asfloat((int)m_DecalProperties.m_LayerMask));
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetProceduralAnimationProperties(ref MaterialPropertyBlock block)
		{
			SetupProceduralAnimationProperties();
			if (!(m_ProceduralAnimationProperties != null) || !m_ProceduralAnimationProperties.active)
			{
				return;
			}
			int num = m_ProceduralAnimationProperties.m_Bones.Length;
			block.SetBuffer("_BoneTransforms", m_AnimationBuffer);
			block.SetVector("colossal_BoneParameters", new Vector2(0f, num));
			block.SetInt("_BonePreviousTransformsByteOffset", num * 64);
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetEmissiveProperties(int lightIndex, ref MaterialPropertyBlock block)
		{
			SetupEmissiveProperties(lightIndex);
			if (!(m_EmissiveProperties != null) || !m_EmissiveProperties.active)
			{
				return;
			}
			int num = m_EmissiveProperties.lightsCount + 1;
			block.SetBuffer("_LightInfo", m_LightBuffer);
			block.SetVector("colossal_LightParameters", new Vector4(0f, num, 0f, 0f));
			int num2 = 0;
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				block.SetFloat("colossal_SingleLightsOffset", m_EmissiveProperties.GetSingleLightOffset(num2++));
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetCharacterProperties(ref MaterialPropertyBlock block)
		{
			if (!(m_CharacterGroupRenderer != null))
			{
				return;
			}
			m_CharacterGroupRenderer.SetCharacterProperties(ref block);
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		public void SetPropProperties(ref MaterialPropertyBlock block)
		{
			if (!(m_PropRenderer != null))
			{
				return;
			}
			m_PropRenderer.SetPropProperties(ref block);
			GlobalBufferRenderer.ShapeAllocation[] shapeAllocations = GetShapeAllocations();
			for (int i = 0; i < m_MeshRenderers.Count; i++)
			{
				if (shapeAllocations != null)
				{
					GlobalBufferRenderer.ShapeAllocation shapeAllocation = shapeAllocations[i];
					Vector4 zero = Vector4.zero;
					Vector4 zero2 = Vector4.zero;
					if (!shapeAllocation.m_Allocation.Empty)
					{
						zero.x = shapeAllocation.m_PositionExtent.x;
						zero.y = shapeAllocation.m_PositionExtent.y;
						zero.z = shapeAllocation.m_PositionExtent.z;
						zero.w = math.asfloat(shapeAllocation.m_Allocation.Begin);
						zero2.x = shapeAllocation.m_NormalExtent.x;
						zero2.y = shapeAllocation.m_NormalExtent.y;
						zero2.z = shapeAllocation.m_NormalExtent.z;
						zero2.w = math.asfloat(shapeAllocation.m_Stride);
					}
					block.SetVector("colossal_ShapeParameters1", zero);
					block.SetVector("colossal_ShapeParameters2", zero2);
				}
				m_MeshRenderers[i].SetPropertyBlock(block);
			}
		}

		public void SetShapeProperties(ref MaterialPropertyBlock block)
		{
			if (m_CharacterGroupRenderer == null || !m_CharacterGroupRenderer.enabled)
			{
				return;
			}
			GlobalBufferRenderer.ShapeAllocation[] shapeAllocations = GetShapeAllocations();
			GlobalBufferRenderer.OverlayAllocation[] overlayAllocations = GetOverlayAllocations();
			for (int i = 0; i < m_MeshRenderers.Count; i++)
			{
				GlobalBufferRenderer.ShapeAllocation shapeAllocation = shapeAllocations[i];
				Vector4 zero = Vector4.zero;
				Vector4 zero2 = Vector4.zero;
				if (!shapeAllocation.m_Allocation.Empty)
				{
					zero.x = shapeAllocation.m_PositionExtent.x;
					zero.y = shapeAllocation.m_PositionExtent.y;
					zero.z = shapeAllocation.m_PositionExtent.z;
					zero.w = math.asfloat(shapeAllocation.m_Allocation.Begin);
					zero2.x = shapeAllocation.m_NormalExtent.x;
					zero2.y = shapeAllocation.m_NormalExtent.y;
					zero2.z = shapeAllocation.m_NormalExtent.z;
					zero2.w = math.asfloat(shapeAllocation.m_Stride);
				}
				block.SetVector("colossal_ShapeParameters1", zero);
				block.SetVector("colossal_ShapeParameters2", zero2);
				if (overlayAllocations != null)
				{
					GlobalBufferRenderer.OverlayAllocation overlayAllocation = overlayAllocations[i];
					Vector2 zero3 = Vector2.zero;
					if (!shapeAllocation.m_Allocation.Empty)
					{
						zero3.x = math.asfloat(overlayAllocation.m_Allocation.Begin);
					}
					zero3.y = math.asfloat(overlayAllocation.m_Stride);
					block.SetVector(ShaderIDs._OverlayParameters, zero3);
				}
				m_MeshRenderers[i].SetPropertyBlock(block);
			}
		}

		public void SetCullProperties(ref MaterialPropertyBlock block)
		{
			if (!(m_CharacterGroupRenderer != null))
			{
				return;
			}
			block.SetInt("colossal_CullVertices", 0);
			foreach (MeshRenderer meshRenderer in m_MeshRenderers)
			{
				meshRenderer.SetPropertyBlock(block);
			}
		}

		private float GetPixelSize(Camera camera, float radius)
		{
			float num = math.distance(m_Root.transform.position, camera.transform.position);
			return radius / num * 360f / MathF.PI * (float)camera.pixelHeight / camera.fieldOfView;
		}

		private float GetMaxPixelSize()
		{
			float radius = math.length(m_Bounds.max - m_Bounds.min) * 0.5f;
			float num = 0f;
			Camera[] allCameras = Camera.allCameras;
			foreach (Camera camera in allCameras)
			{
				num = Mathf.Max(num, GetPixelSize(camera, radius));
			}
			return num;
		}
	}

	public static class ShaderIDs
	{
		public static readonly int _BuildingState = Shader.PropertyToID("colossal_BuildingState");

		public static readonly int _ColorMask0 = Shader.PropertyToID("colossal_ColorMask0");

		public static readonly int _ColorMask1 = Shader.PropertyToID("colossal_ColorMask1");

		public static readonly int _ColorMask2 = Shader.PropertyToID("colossal_ColorMask2");

		public static readonly int _TextureArea = Shader.PropertyToID("colossal_TextureArea");

		public static readonly int _MeshSize = Shader.PropertyToID("colossal_MeshSize");

		public static readonly int _DecalLayerMask = Shader.PropertyToID("colossal_DecalLayerMask");

		public static readonly int _LodFade = Shader.PropertyToID("colossal_LodFade");

		public static readonly int _LodParameters = Shader.PropertyToID("colossal_LodParameters");

		public static readonly int _OverlayParameters = Shader.PropertyToID("colossal_OverlayParameters");

		public static readonly int _ShapeParameters = Shader.PropertyToID("colossal_ShapeParameters");
	}

	private class AnimationState
	{
		private ProceduralAnimationProperties.BoneInfo boneInfo;

		private int boneIndex;

		private int m_Frame;

		public Transform target { get; }

		public AnimationState(Transform tr, int boneIndex, ProceduralAnimationProperties.BoneInfo boneInfo)
		{
			target = tr;
			this.boneInfo = boneInfo;
			this.boneIndex = boneIndex;
		}

		public virtual void Animate()
		{
			BoneType type = boneInfo.m_Type;
			if (type == BoneType.SteeringTire || type == BoneType.VehicleConnection || type == BoneType.TrainBogie || type == BoneType.SteeringRotation || type == BoneType.SteeringSuspension)
			{
				target.Rotate(Vector3.up, Mathf.Cos(Time.time * 0.5f) * Time.deltaTime * 10f, Space.World);
			}
			if (type == BoneType.RollingTire || type == BoneType.SteeringTire || type == BoneType.FixedTire)
			{
				target.Rotate(Vector3.right, Time.deltaTime * 60f, Space.Self);
			}
			if (type == BoneType.PoweredRotation || type == BoneType.PropellerRotation || type == BoneType.WindTurbineRotation || type == BoneType.WindSpeedRotation)
			{
				target.Rotate(Vector3.up, Time.deltaTime * 180f, Space.Self);
			}
			if (type == BoneType.OperatingRotation || type == BoneType.FixedRotation)
			{
				target.Rotate(Vector3.up, Time.deltaTime * 30f, Space.Self);
			}
			if (type == BoneType.LookAtDirection)
			{
				target.Rotate(Vector3.up, Mathf.Cos(Time.time * 0.8f) * Time.deltaTime * 50f, Space.Self);
			}
		}

		public virtual void Animate(ProceduralAnimationProperties.BoneInfo[] restPoseBones, Colossal.Animations.AnimationClip clip)
		{
			if (this.boneInfo.m_Type < BoneType.PlaybackLayer0)
			{
				return;
			}
			int num = clip.m_Animation.elements.Length / clip.m_Animation.boneIndices.Length;
			int num2 = clip.m_Animation.boneIndices.IndexOf(boneIndex);
			if (num2 != -1)
			{
				Animation.ElementRaw boneSample = clip.m_Animation.GetBoneSample(num2, 0, m_Frame++ % num);
				if (clip.m_Animation.type == Colossal.Animations.AnimationType.Additive && this.boneInfo != null)
				{
					int num3 = boneIndex;
					ProceduralAnimationProperties.BoneInfo boneInfo = restPoseBones[num3];
					boneSample.position += new float3(boneInfo.position);
					boneSample.rotation = math.mul(boneInfo.rotation, boneSample.rotation).value;
				}
				target.localPosition = boneSample.position;
				target.localRotation = new Quaternion(boneSample.rotation.x, boneSample.rotation.y, boneSample.rotation.z, boneSample.rotation.w);
				target.localScale = Vector3.one;
			}
		}

		public virtual void Transfer(AnimationState state)
		{
			state.target.localPosition = target.localPosition;
			state.target.localRotation = target.localRotation;
			state.target.localScale = target.localScale;
		}
	}

	private const bool kRefreshPrefabDataEveryFrame = true;

	public bool m_NoVT;

	public RenderPrefab m_Prefab;

	[Range(0f, 1f)]
	public float m_WindowsLight;

	[Range(-1f, 255f)]
	public int m_EmissiveLight = -1;

	[Range(-1f, 10f)]
	public int m_ColorIndex;

	[Range(0f, 3f)]
	public int m_LODIndex;

	public bool m_Animate = true;

	public int m_AnimationIndex;

	private List<Instance> m_Hierarchies;

	private MaterialPropertyBlock m_MaterialPropertyBlock;

	private Dictionary<string, List<AnimationState>> m_AnimationStates = new Dictionary<string, List<AnimationState>>();

	private ProceduralAnimationProperties.BoneInfo[] m_RestPoseBones;

	private AnimationAsset m_Animation;

	public IReadOnlyList<Instance> hierarchies => m_Hierarchies;

	public GameObject GetActiveRoot()
	{
		if (m_Hierarchies != null && m_LODIndex < m_Hierarchies.Count)
		{
			return m_Hierarchies[m_LODIndex].root;
		}
		return null;
	}

	private void OnEnable()
	{
		if (!(m_Prefab != null))
		{
			return;
		}
		m_MaterialPropertyBlock = new MaterialPropertyBlock();
		m_Hierarchies = new List<Instance>();
		m_Hierarchies.Add(new Instance(this, m_Prefab, m_Prefab, !m_NoVT));
		if (m_Prefab.TryGet<LodProperties>(out var component))
		{
			RenderPrefab[] lodMeshes = component.m_LodMeshes;
			foreach (RenderPrefab prefab in lodMeshes)
			{
				Instance instance = new Instance(this, m_Prefab, prefab, !m_NoVT);
				instance.enabled = false;
				m_Hierarchies.Add(instance);
			}
		}
	}

	private void OnDisable()
	{
		if (m_Hierarchies == null)
		{
			return;
		}
		foreach (Instance hierarchy in m_Hierarchies)
		{
			hierarchy.Dispose();
		}
	}

	private void RegisterForAnimation(string boneName, int boneIndex, Transform target, ProceduralAnimationProperties.BoneInfo boneInfo)
	{
		if (!m_AnimationStates.TryGetValue(boneName, out var value))
		{
			value = new List<AnimationState>();
			m_AnimationStates.Add(boneName, value);
		}
		value.RemoveAll((AnimationState t) => t.target == null);
		if (value.FindIndex((AnimationState x) => x.target == target) == -1)
		{
			value.Add(new AnimationState(target, boneIndex, boneInfo));
		}
	}

	private void UpdateAnimations(ProceduralAnimationProperties proceduralAnimationProperties)
	{
		AnimationAsset animationAsset = null;
		if (proceduralAnimationProperties != null)
		{
			m_RestPoseBones = proceduralAnimationProperties.m_Bones;
			ProceduralAnimationProperties.AnimationInfo[] animations = proceduralAnimationProperties.m_Animations;
			if (animations != null && animations.Length != 0)
			{
				animationAsset = ((m_AnimationIndex < 0) ? null : proceduralAnimationProperties.m_Animations[m_AnimationIndex]?.animationAsset);
			}
		}
		if (animationAsset != m_Animation)
		{
			m_Animation = animationAsset;
			m_Animation?.Load();
			foreach (KeyValuePair<string, List<AnimationState>> kvp in m_AnimationStates)
			{
				AnimationState animationState = kvp.Value[0];
				animationState.target.localPosition = m_RestPoseBones.Single((ProceduralAnimationProperties.BoneInfo b) => kvp.Key.StartsWith(b.name + "@")).position;
				animationState.target.localRotation = m_RestPoseBones.Single((ProceduralAnimationProperties.BoneInfo b) => kvp.Key.StartsWith(b.name + "@")).rotation;
				animationState.Transfer(kvp.Value[0]);
			}
		}
		if (m_Animation != null)
		{
			foreach (KeyValuePair<string, List<AnimationState>> animationState4 in m_AnimationStates)
			{
				AnimationState animationState2 = animationState4.Value[0];
				animationState2.Animate(m_RestPoseBones, m_Animation.data);
				for (int num = 1; num < animationState4.Value.Count; num++)
				{
					animationState2.Transfer(animationState4.Value[num]);
				}
			}
			return;
		}
		foreach (KeyValuePair<string, List<AnimationState>> animationState5 in m_AnimationStates)
		{
			AnimationState animationState3 = animationState5.Value[0];
			animationState3.Animate();
			for (int num2 = 1; num2 < animationState5.Value.Count; num2++)
			{
				animationState3.Transfer(animationState5.Value[num2]);
			}
		}
	}

	private void Update()
	{
		if (m_Animate)
		{
			UpdateAnimations(m_Hierarchies.FirstOrDefault()?.m_ProceduralAnimationProperties);
		}
		for (int i = 0; i < m_Hierarchies.Count; i++)
		{
			Instance instance = m_Hierarchies[i];
			instance.Update();
			instance.SetColorProperties(m_ColorIndex, ref m_MaterialPropertyBlock);
			instance.SetDecalProperties(ref m_MaterialPropertyBlock);
			instance.SetEmissiveProperties(m_EmissiveLight, ref m_MaterialPropertyBlock);
			instance.SetProceduralAnimationProperties(ref m_MaterialPropertyBlock);
			instance.SetShapeProperties(ref m_MaterialPropertyBlock);
			instance.SetWindowProperties(m_WindowsLight, ref m_MaterialPropertyBlock);
			instance.SetCharacterProperties(ref m_MaterialPropertyBlock);
			instance.SetPropProperties(ref m_MaterialPropertyBlock);
			instance.SetShapeProperties(ref m_MaterialPropertyBlock);
			instance.enabled = i == m_LODIndex;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (m_Hierarchies != null)
		{
			for (int i = 0; i < m_Hierarchies.Count; i++)
			{
				Instance instance = m_Hierarchies[i];
				Bounds3 bounds = instance.bounds;
				Matrix4x4 matrix = UnityEngine.Gizmos.matrix;
				UnityEngine.Gizmos.matrix = instance.localToWorldMatrix;
				UnityEngine.Gizmos.color = Colossal.ColorUtils.NiceRandomColor(i);
				UnityEngine.Gizmos.DrawWireCube(MathUtils.Center(bounds), MathUtils.Extents(bounds) * 2f);
				UnityEngine.Gizmos.matrix = matrix;
			}
		}
	}
}
