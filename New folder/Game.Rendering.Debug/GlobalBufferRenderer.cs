using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Animations;
using Colossal.Collections;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering.Debug;

public class GlobalBufferRenderer : MonoBehaviour
{
	public struct ShapeAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_Stride;

		public float3 m_PositionExtent;

		public float3 m_NormalExtent;
	}

	public struct CullAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_VertexCount;

		public int m_StartOffset;

		public int m_Stride;

		public int m_Size;

		public int m_Offset;
	}

	public class PropStyleData
	{
		public RenderPrefab m_RenderPrefab;

		public ActivityPropPrefab m_PrefabData;

		public PropStyleData(RenderPrefab renderPrefab, ActivityPropPrefab propPrefab)
		{
			m_RenderPrefab = renderPrefab;
			m_PrefabData = propPrefab;
		}
	}

	public class PrefabClipData
	{
		public int m_Offset;

		public int m_Stride;

		public int m_RestPoseClipIndex;
	}

	public class MetaInstanceData
	{
		public int m_MetaIndex = -1;

		public int m_RestPoseIndex = -1;

		public MetaBufferData m_MetaData;

		public ActivityType m_Activity;

		public ActivityCondition m_Condition;

		public Game.Prefabs.AnimationType m_TransformState;

		public GenderMask m_Gender;

		public int m_BodyIndex = -1;

		public int m_BodyIndex0I = -1;

		public int m_BodyIndex1 = -1;

		public int m_BodyIndex1I = -1;

		public int m_FacialIndex = -1;

		public int m_FacialIndex1 = -1;

		public float m_BodyFrame;

		public float m_BodyRange;

		public float m_FaceFrame;

		public float m_FaceRange;
	}

	public struct OverlayAllocation
	{
		public NativeHeapBlock m_Allocation;

		public int m_Stride;
	}

	private const int SHAPEBUFFER_ELEMENT_SIZE = 8;

	private const uint SHAPEBUFFER_MEMORY_DEFAULT = 33554432u;

	private const uint SHAPEBUFFER_MEMORY_INCREMENT = 8388608u;

	private const uint OVERLAYBUFFER_MEMORY_DEFAULT = 33554432u;

	private uint OVERLAYBUFFER_MEMORY_INCREMENT = (uint)(OverlayAtlasElement.SizeOf * 1024 * 1024);

	private const int CULLBUFFER_ELEMENT_SIZE = 4;

	private const uint CULLBUFFER_MEMORY_DEFAULT = 33554432u;

	private const uint CULLBUFFER_MEMORY_INCREMENT = 4194304u;

	private NativeHeapAllocator m_ShapeAllocator;

	private GraphicsBuffer m_ShapeBuffer;

	private NativeHeapAllocator m_OverlayAllocator;

	private GraphicsBuffer m_OverlayBuffer;

	private NativeHeapAllocator m_CullAllocator;

	private GraphicsBuffer m_CullBuffer;

	private NativeHeapAllocator m_BoneAllocator;

	private ComputeBuffer m_BoneBuffer;

	private ComputeBuffer m_MetaBuffer;

	private Dictionary<Identifier, ShapeAllocation[]> m_ShapeAllocationsCache;

	private Dictionary<Identifier, OverlayAllocation[]> m_OverlayAllocationsCache;

	private Dictionary<Identifier, CullAllocation[]> m_CullAllocationsCache;

	public bool m_PlayAnimations = true;

	public bool m_RemoveRootMotion;

	public List<MetaInstanceData> m_MetaInstanceData;

	private const string ANIMATION_COMPUTE_SHADER_RESOURCE = "Didimo/AnimationBlendCompute";

	private const string SHADER_BLEND_ANIMATION_LAYER0_KERNEL_NAME = "BlendAnimationLayer0";

	private const string SHADER_BLEND_ANIMATION_LAYER1_KERNEL_NAME = "BlendAnimationLayer1";

	private const string SHADER_BLEND_ANIMATION_LAYER2_KERNEL_NAME = "BlendAnimationLayer2";

	private const string SHADER_BLEND_TRANSITION_LAYER0_KERNEL_NAME = "BlendTransitionLayer0";

	private const string SHADER_BLEND_TRANSITION2_LAYER0_KERNEL_NAME = "BlendTransition2Layer0";

	private const string SHADER_BLEND_TRANSITION_LAYER1_KERNEL_NAME = "BlendTransitionLayer1";

	private const string SHADER_BLEND_REST_POSE_KERNEL_NAME = "BlendRestPose";

	private const string SHADER_CONVERT_COORDINATES_KERNEL_NAME = "ConvertLocalCoordinates";

	private const string SHADER_CONVERT_COORDINATES_WITH_HISTORY_KERNEL_NAME = "ConvertLocalCoordinatesWithHistory";

	private bool m_AnimationCollectionsInitialized;

	private Queue<string> m_LoadingQueue = new Queue<string>();

	private Dictionary<string, PrefabClipData> m_StyleClipData = new Dictionary<string, PrefabClipData>();

	private List<Game.Prefabs.AnimationClip> m_AnimationClips = new List<Game.Prefabs.AnimationClip>();

	private List<AnimationMotion> m_AnimationMotions = new List<AnimationMotion>();

	private List<int> m_LoadedAnimations = new List<int>();

	private const uint BONEBUFFER_MEMORY_DEFAULT = 8388608u;

	private const uint BONEBUFFER_MEMORY_INCREMENT = 2097152u;

	private const uint ANIMBUFFER_MEMORY_DEFAULT = 33554432u;

	private const uint ANIMBUFFER_MEMORY_INCREMENT = 8388608u;

	private const uint METABUFFER_MEMORY_DEFAULT = 1048576u;

	private const uint METABUFFER_MEMORY_INCREMENT = 262144u;

	private const uint INDEXBUFFER_MEMORY_DEFAULT = 65536u;

	private const uint INDEXBUFFER_MEMORY_INCREMENT = 16384u;

	private NativeHeapAllocator m_AnimAllocator;

	private NativeHeapAllocator m_IndexAllocator;

	private NativeList<MetaBufferData> m_MetaBufferData;

	private NativeList<RestPoseInstance> m_InstanceIndices;

	private NativeList<AnimatedInstance> m_BodyInstances;

	private NativeList<AnimatedInstance> m_FaceInstances;

	private NativeList<AnimatedInstance> m_CorrectiveInstances;

	private NativeList<AnimatedTransition> m_BodyTransitions;

	private NativeList<AnimatedTransition2> m_BodyTransitions2;

	private NativeList<AnimatedTransition> m_FaceTransitions;

	private NativeList<AnimatedSystem.AnimationClipData> m_AnimationClipData;

	private NativeHashMap<AnimatedSystem.PropClipKey, int> m_PropClipIndex;

	private ComputeShader m_AnimationComputeShader;

	private ComputeBuffer m_LocalTRSBlendPoseBuffer;

	private ComputeBuffer m_LocalTRSBoneBuffer;

	private ComputeBuffer m_AnimInfoBuffer;

	private ComputeBuffer m_AnimBuffer;

	private ComputeBuffer m_IndexBuffer;

	private ComputeBuffer m_InstanceBuffer;

	private ComputeBuffer m_BodyInstanceBuffer;

	private ComputeBuffer m_FaceInstanceBuffer;

	private ComputeBuffer m_CorrectiveInstanceBuffer;

	private ComputeBuffer m_BodyTransitionBuffer;

	private ComputeBuffer m_BodyTransition2Buffer;

	private ComputeBuffer m_FaceTransitionBuffer;

	private int m_MaxBoneCount;

	private int m_MaxActiveBoneCount;

	private bool m_IsAllocating;

	private Dictionary<string, int> m_PropIds;

	private Dictionary<int, PropStyleData> m_PropStyles;

	private int m_BlendAnimationLayer0KernelIx;

	private int m_BlendAnimationLayer1KernelIx;

	private int m_BlendAnimationLayer2KernelIx;

	private int m_BlendTransitionLayer0KernelIx;

	private int m_BlendTransition2Layer0KernelIx;

	private int m_BlendTransitionLayer1KernelIx;

	private int m_BlendRestPoseKernelIx;

	private int m_ConvertLocalCoordinatesKernelIx;

	private int m_ConvertLocalCoordinatesWithHistoryKernelIx;

	private int m_IndexBufferID;

	private int m_MetadataBufferID;

	private int m_MetaIndexBufferID;

	private int m_AnimatedInstanceBufferID;

	private int m_AnimatedTransitionBufferID;

	private int m_AnimatedTransition2BufferID;

	private int m_AnimationInfoBufferID;

	private int m_AnimationBoneBufferID;

	private int m_InstanceCountID;

	private int m_BodyInstanceCountID;

	private int m_BodyTransitionCountID;

	private int m_BodyTransition2CountID;

	private int m_FaceInstanceCountID;

	private int m_FaceTransitionCountID;

	private int m_CorrectiveInstanceCountID;

	private int m_LocalTRSBlendPoseBufferID;

	private int m_LocalTRSBoneBufferID;

	private int m_BoneBufferID;

	private int m_BoneHistoryBufferID;

	private bool m_Initialized;

	private static GlobalBufferRenderer s_Instance;

	private ComputeBuffer m_BoneHistoryBuffer { get; set; }

	public static GlobalBufferRenderer Instance => s_Instance ?? (s_Instance = new GameObject("GlobalBufferRenderer").AddComponent<GlobalBufferRenderer>());

	public void Start()
	{
		m_AnimationComputeShader = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("Didimo/AnimationBlendCompute"));
		m_BlendAnimationLayer0KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer0");
		m_BlendAnimationLayer1KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer1");
		m_BlendAnimationLayer2KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer2");
		m_BlendTransitionLayer0KernelIx = m_AnimationComputeShader.FindKernel("BlendTransitionLayer0");
		m_BlendTransition2Layer0KernelIx = m_AnimationComputeShader.FindKernel("BlendTransition2Layer0");
		m_BlendTransitionLayer1KernelIx = m_AnimationComputeShader.FindKernel("BlendTransitionLayer1");
		m_BlendRestPoseKernelIx = m_AnimationComputeShader.FindKernel("BlendRestPose");
		m_ConvertLocalCoordinatesKernelIx = m_AnimationComputeShader.FindKernel("ConvertLocalCoordinates");
		m_ConvertLocalCoordinatesWithHistoryKernelIx = m_AnimationComputeShader.FindKernel("ConvertLocalCoordinatesWithHistory");
		m_IndexBufferID = Shader.PropertyToID("IndexDataBuffer");
		m_MetadataBufferID = Shader.PropertyToID("MetaDataBuffer");
		m_MetaIndexBufferID = Shader.PropertyToID("MetaIndexBuffer");
		m_AnimatedInstanceBufferID = Shader.PropertyToID("AnimatedInstanceBuffer");
		m_AnimatedTransitionBufferID = Shader.PropertyToID("AnimatedTransitionBuffer");
		m_AnimatedTransition2BufferID = Shader.PropertyToID("AnimatedTransition2Buffer");
		m_AnimationInfoBufferID = Shader.PropertyToID("AnimationInfoBuffer");
		m_AnimationBoneBufferID = Shader.PropertyToID("AnimationBoneBuffer");
		m_InstanceCountID = Shader.PropertyToID("instanceCount");
		m_BodyInstanceCountID = Shader.PropertyToID("bodyInstanceCount");
		m_BodyTransitionCountID = Shader.PropertyToID("bodyTransitionCount");
		m_BodyTransition2CountID = Shader.PropertyToID("bodyTransition2Count");
		m_FaceInstanceCountID = Shader.PropertyToID("faceInstanceCount");
		m_FaceTransitionCountID = Shader.PropertyToID("faceTransitionCount");
		m_CorrectiveInstanceCountID = Shader.PropertyToID("correctiveInstanceCount");
		m_LocalTRSBlendPoseBufferID = Shader.PropertyToID("LocalTRSBlendPoseBuffer");
		m_LocalTRSBoneBufferID = Shader.PropertyToID("LocalTRSBoneBuffer");
		m_BoneBufferID = Shader.PropertyToID("BoneBuffer");
		m_BoneHistoryBufferID = Shader.PropertyToID("BoneHistoryBuffer");
	}

	public void OnDestroy()
	{
		if (m_AnimationClipData.IsCreated)
		{
			m_AnimationClipData.Dispose();
		}
		if (m_AnimAllocator.IsCreated)
		{
			m_AnimAllocator.Dispose();
		}
		if (m_IndexAllocator.IsCreated)
		{
			m_IndexAllocator.Dispose();
		}
		if (m_BoneAllocator.IsCreated)
		{
			m_BoneAllocator.Dispose();
		}
		if (m_BodyInstances.IsCreated)
		{
			m_BodyInstances.Dispose();
		}
		if (m_FaceInstances.IsCreated)
		{
			m_FaceInstances.Dispose();
		}
		if (m_CorrectiveInstances.IsCreated)
		{
			m_CorrectiveInstances.Dispose();
		}
		if (m_BodyTransitions.IsCreated)
		{
			m_BodyTransitions.Dispose();
		}
		if (m_BodyTransitions2.IsCreated)
		{
			m_BodyTransitions2.Dispose();
		}
		if (m_FaceTransitions.IsCreated)
		{
			m_FaceTransitions.Dispose();
		}
		if (m_MetaBufferData.IsCreated)
		{
			m_MetaBufferData.Dispose();
		}
		if (m_PropClipIndex.IsCreated)
		{
			m_PropClipIndex.Dispose();
		}
		if (m_BoneBuffer != null)
		{
			m_BoneBuffer.Release();
			m_BoneBuffer = null;
		}
		if (m_BoneHistoryBuffer != null)
		{
			m_BoneHistoryBuffer.Release();
			m_BoneHistoryBuffer = null;
		}
		if (m_LocalTRSBlendPoseBuffer != null)
		{
			m_LocalTRSBlendPoseBuffer.Release();
			m_LocalTRSBlendPoseBuffer = null;
		}
		if (m_LocalTRSBoneBuffer != null)
		{
			m_LocalTRSBoneBuffer.Release();
			m_LocalTRSBoneBuffer = null;
		}
		if (m_AnimInfoBuffer != null)
		{
			m_AnimInfoBuffer.Release();
			m_AnimInfoBuffer = null;
		}
		if (m_AnimBuffer != null)
		{
			m_AnimBuffer.Release();
			m_AnimBuffer = null;
		}
		if (m_MetaBuffer != null)
		{
			m_MetaBuffer.Release();
			m_MetaBuffer = null;
		}
		if (m_IndexBuffer != null)
		{
			m_IndexBuffer.Release();
			m_IndexBuffer = null;
		}
		if (m_InstanceBuffer != null)
		{
			m_InstanceBuffer.Release();
			m_InstanceBuffer = null;
		}
		if (m_BodyInstanceBuffer != null)
		{
			m_BodyInstanceBuffer.Release();
			m_BodyInstanceBuffer = null;
		}
		if (m_FaceInstanceBuffer != null)
		{
			m_FaceInstanceBuffer.Release();
			m_FaceInstanceBuffer = null;
		}
		if (m_CorrectiveInstanceBuffer != null)
		{
			m_CorrectiveInstanceBuffer.Release();
			m_CorrectiveInstanceBuffer = null;
		}
		if (m_BodyTransitionBuffer != null)
		{
			m_BodyTransitionBuffer.Release();
			m_BodyTransitionBuffer = null;
		}
		if (m_BodyTransition2Buffer != null)
		{
			m_BodyTransition2Buffer.Release();
			m_BodyTransition2Buffer = null;
		}
		if (m_FaceTransitionBuffer != null)
		{
			m_FaceTransitionBuffer.Release();
			m_FaceTransitionBuffer = null;
		}
		UnityEngine.Object.DestroyImmediate(m_AnimationComputeShader);
		s_Instance = null;
	}

	public void OnEnable()
	{
		if (!m_Initialized)
		{
			m_Initialized = true;
			InitializeNativeCollections();
			m_ShapeAllocationsCache = new Dictionary<Identifier, ShapeAllocation[]>();
			m_OverlayAllocationsCache = new Dictionary<Identifier, OverlayAllocation[]>();
			m_CullAllocationsCache = new Dictionary<Identifier, CullAllocation[]>();
			m_ShapeAllocator = new NativeHeapAllocator(4194304u, 1u, Allocator.Persistent);
			m_ShapeAllocator.Allocate(1u);
			ResizeShapeBuffer();
			m_OverlayAllocator = new NativeHeapAllocator(33554432u / (uint)OverlayAtlasElement.SizeOf, 1u, Allocator.Persistent);
			m_OverlayAllocator.Allocate(1u);
			ResizeOverlayBuffer();
			m_CullAllocator = new NativeHeapAllocator(8388608u, 1u, Allocator.Persistent);
			m_CullAllocator.Allocate(1u);
			ResizeCullBuffer();
		}
	}

	public void OnDisable()
	{
		m_ShapeAllocationsCache.Clear();
		m_OverlayAllocationsCache.Clear();
		m_CullAllocationsCache.Clear();
		m_ShapeBuffer?.Dispose();
		m_ShapeAllocator.Dispose();
		m_OverlayBuffer?.Dispose();
		m_OverlayAllocator.Dispose();
		m_CullBuffer?.Dispose();
		m_CullAllocator.Dispose();
		m_Initialized = false;
	}

	public void FixedUpdate()
	{
		if (m_LoadingQueue.Count > 0)
		{
			return;
		}
		List<(int, int, int, AnimatedSystem.AnimationLayerData2, AnimatedSystem.AnimationLayerData)> list = new List<(int, int, int, AnimatedSystem.AnimationLayerData2, AnimatedSystem.AnimationLayerData)>();
		for (int i = 0; i < m_MetaInstanceData.Count; i++)
		{
			MetaInstanceData metaInstanceData = m_MetaInstanceData[i];
			if (metaInstanceData.m_BodyIndex == -1 && metaInstanceData.m_FacialIndex == -1)
			{
				continue;
			}
			AnimatedSystem.AnimationLayerData2 layerData = new AnimatedSystem.AnimationLayerData2
			{
				m_CurrentIndex = -1,
				m_TransitionIndex = -1
			};
			if (metaInstanceData.m_BodyIndex != -1)
			{
				Game.Prefabs.AnimationClip clipData = GetClip(metaInstanceData.m_BodyIndex);
				Game.Prefabs.AnimationClip clipData0I = GetClip(metaInstanceData.m_BodyIndex0I);
				Game.Prefabs.AnimationClip clipData2 = GetClip(metaInstanceData.m_BodyIndex1);
				Game.Prefabs.AnimationClip clipData1I = GetClip(metaInstanceData.m_BodyIndex1I);
				if (m_PlayAnimations)
				{
					metaInstanceData.m_BodyFrame += Time.fixedDeltaTime;
					if (metaInstanceData.m_BodyFrame > metaInstanceData.m_BodyRange)
					{
						metaInstanceData.m_BodyFrame = 0f;
					}
				}
				SetLayerData(ref layerData, in clipData, in clipData0I, in clipData2, in clipData1I, new float2(metaInstanceData.m_BodyFrame, 0f), new float2(0f, 0f), 0f);
			}
			AnimatedSystem.AnimationLayerData layerData2 = new AnimatedSystem.AnimationLayerData
			{
				m_CurrentIndex = -1,
				m_TransitionIndex = -1
			};
			if (metaInstanceData.m_FacialIndex != -1)
			{
				Game.Prefabs.AnimationClip clipData3 = GetClip(metaInstanceData.m_FacialIndex);
				Game.Prefabs.AnimationClip clipData4 = GetClip(metaInstanceData.m_FacialIndex1);
				if (m_PlayAnimations)
				{
					metaInstanceData.m_FaceFrame += Time.fixedDeltaTime;
					if (metaInstanceData.m_FaceFrame > metaInstanceData.m_FaceRange)
					{
						metaInstanceData.m_FaceFrame = 0f;
					}
				}
				SetLayerData(ref layerData2, in clipData3, in clipData4, new float2(metaInstanceData.m_FaceFrame, 0f), 0f);
			}
			Game.Prefabs.AnimationClip clip = GetClip(metaInstanceData.m_RestPoseIndex);
			m_MetaInstanceData[i] = metaInstanceData;
			list.Add((metaInstanceData.m_MetaIndex, metaInstanceData.m_MetaData.m_MetaIndexLink, clip.m_InfoIndex, layerData, layerData2));
		}
		SetInstancesData(list);
		UpdateInstances();
		PlayAnimations();
	}

	private float4 q_conj(float4 q)
	{
		return new float4(0f - q.x, 0f - q.y, 0f - q.z, q.w);
	}

	private float4 q_inverse(float4 q)
	{
		return q_conj(q) / (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
	}

	private Animation.ElementRaw ComputeAnimationData_BlendWeight(int[] indices, Animation.Element[] elements, float3 animPosMin, float3 animPosRange, BlendWeight weight, int baseBufferIndex, int shapeIndexOffset, Animation.ElementRaw currentTRS, float3 templateBonePosition, float4 templateBoneRotation, float4 templateBoneRotationInverse)
	{
		if (weight.m_Weight <= 0f || indices[shapeIndexOffset + weight.m_Index + 1] < 0)
		{
			return currentTRS;
		}
		int num = indices[shapeIndexOffset + weight.m_Index + 1];
		Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(elements[baseBufferIndex + num], animPosMin, animPosRange);
		float3 @float = (elementRaw.position - templateBonePosition) * weight.m_Weight;
		float4 value = math.mul(math.slerp(templateBoneRotation, elementRaw.rotation, weight.m_Weight), templateBoneRotationInverse).value;
		currentTRS.position += @float;
		currentTRS.rotation = math.mul((quaternion)value, (quaternion)currentTRS.rotation).value;
		return currentTRS;
	}

	private Animation.ElementRaw ComputeAnimationData_Blended(int[] indices, Animation.Element[] elements, float3 animPosMin, float3 animPosRange, BlendWeights weights, int baseBufferIndex, int shapeIndexOffset)
	{
		Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(elements[baseBufferIndex], animPosMin, animPosRange);
		float3 position = elementRaw.position;
		float4 rotation = elementRaw.rotation;
		float4 value = math.inverse(rotation).value;
		Animation.ElementRaw currentTRS = default(Animation.ElementRaw);
		currentTRS.position = position;
		currentTRS.rotation = rotation;
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight0, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight1, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight2, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight3, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight4, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight5, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		currentTRS = ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight6, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
		return ComputeAnimationData_BlendWeight(indices, elements, animPosMin, animPosRange, weights.m_Weight7, baseBufferIndex, shapeIndexOffset, currentTRS, position, rotation, value);
	}

	private Animation.ElementRaw BlendRetargetedAnimationFrameData(int[] indices, Animation.Element[] elements, AnimationInfoData animInfo, BlendWeights weights, int boneIx, int frameIx)
	{
		int baseBufferIndex = frameIx * animInfo.m_BoneCount * animInfo.m_ShapeCount + boneIx * animInfo.m_ShapeCount + animInfo.m_Offset;
		return ComputeAnimationData_Blended(indices, elements, animInfo.m_PositionMin, animInfo.m_PositionRange, weights, baseBufferIndex, animInfo.m_Shapes);
	}

	private Animation.ElementRaw BlendSingleAnimationFrameData(Animation.Element[] elements, AnimationInfoData animInfo, int boneIx, int frameIx)
	{
		int num = frameIx * animInfo.m_BoneCount + boneIx + animInfo.m_Offset;
		return AnimationEncoding.DecodeElement(elements[num], animInfo.m_PositionMin, animInfo.m_PositionRange);
	}

	private Animation.ElementRaw BlendRestPoseData(int[] indices, Animation.Element[] elements, BlendWeights weights, AnimationInfoData animInfo, int activeBoneIx)
	{
		if (animInfo.m_ShapeCount == 1)
		{
			return BlendSingleAnimationFrameData(elements, animInfo, activeBoneIx, 0);
		}
		return BlendRetargetedAnimationFrameData(indices, elements, animInfo, weights, activeBoneIx, 0);
	}

	private Animation.ElementRaw BlendTRSMatrices(Animation.ElementRaw matrixA, Animation.ElementRaw matrixB, float weight)
	{
		if (weight <= 0f)
		{
			return matrixA;
		}
		if (weight >= 1f)
		{
			return matrixB;
		}
		Animation.ElementRaw result = default(Animation.ElementRaw);
		result.position = math.lerp(matrixA.position, matrixB.position, weight);
		result.rotation = math.slerp(matrixA.rotation, matrixB.rotation, weight).value;
		return result;
	}

	private Animation.ElementRaw BlendAnimationFrameData(Animation.Element[] elements, BlendWeights weights, AnimationInfoData animInfo, int activeBoneIx, int frameIx, float frameWeight, Animation.ElementRaw restPose)
	{
		Animation.ElementRaw elementRaw;
		if (animInfo.m_ShapeCount == 1)
		{
			elementRaw = BlendSingleAnimationFrameData(elements, animInfo, activeBoneIx, frameIx);
			if (frameWeight > 0f)
			{
				Animation.ElementRaw matrixB = BlendSingleAnimationFrameData(elements, animInfo, activeBoneIx, frameIx + 1);
				elementRaw = BlendTRSMatrices(elementRaw, matrixB, frameWeight);
			}
		}
		else
		{
			elementRaw = default(Animation.ElementRaw);
		}
		return elementRaw;
	}

	private void DebugFinaleLocalCoordinates(int instanceIndex)
	{
		RestPoseInstance[] array = new RestPoseInstance[2];
		m_InstanceBuffer.GetData(array);
		MetaBufferData[] array2 = new MetaBufferData[m_MetaBuffer.count];
		m_MetaBuffer.GetData(array2);
		int metaIndex = array[instanceIndex].m_MetaIndex;
		int restPoseIndex = array[instanceIndex].m_RestPoseIndex;
		int boneCount = array2[metaIndex].m_BoneCount;
		int num = 0;
		if (num >= boneCount)
		{
			return;
		}
		int boneOffset = array2[metaIndex].m_BoneOffset;
		int num2 = boneOffset + num;
		BoneElement[] array3 = new BoneElement[m_LocalTRSBoneBuffer.count];
		m_LocalTRSBoneBuffer.GetData(array3);
		BoneElement[] array4 = new BoneElement[m_LocalTRSBlendPoseBuffer.count];
		m_LocalTRSBlendPoseBuffer.GetData(array4);
		float4x4 float4x = array3[num2].m_Matrix;
		float4x4 float4x2 = array4[num2].m_Matrix;
		AnimationInfoData[] array5 = new AnimationInfoData[m_AnimInfoBuffer.count];
		m_AnimInfoBuffer.GetData(array5);
		int[] array6 = new int[m_IndexBuffer.count];
		m_IndexBuffer.GetData(array6);
		if (restPoseIndex >= 0)
		{
			AnimationInfoData animationInfoData = array5[restPoseIndex];
			for (int num3 = array6[animationInfoData.m_Hierarchy + num]; num3 >= 0; num3 = array6[animationInfoData.m_Hierarchy + num3])
			{
				int num4 = boneOffset + num3;
				float4x = math.mul(array3[num4].m_Matrix, float4x);
				float4x2 = math.mul(array4[num4].m_Matrix, float4x2);
			}
		}
		float4x4 a = math.mul(float4x, math.inverse(float4x2));
		if (array2[metaIndex].m_MetaIndexLink != -1)
		{
			int metaIndexLink = array2[metaIndex].m_MetaIndexLink;
			int boneLink = array2[metaIndex].m_BoneLink;
			int boneCount2 = array2[metaIndexLink].m_BoneCount;
			_ = float4x4.identity;
			if (boneLink >= boneCount2)
			{
				return;
			}
			int num5 = array2[metaIndexLink].m_BoneOffset + boneLink;
			float4x4 matrix = array3[num5].m_Matrix;
			_ = ref array4[num5];
			for (int i = 0; i < 2; i++)
			{
				if (array[i].m_MetaIndex == metaIndexLink && array[i].m_RestPoseIndex >= 0)
				{
					_ = ref array[i];
					AnimationInfoData animationInfoData2 = array5[restPoseIndex];
					for (int num6 = array6[animationInfoData2.m_Hierarchy + boneLink]; num6 >= 0; num6 = array6[animationInfoData2.m_Hierarchy + num6])
					{
						int num7 = boneOffset + num6;
						float4x = math.mul(array3[num7].m_Matrix, float4x);
						float4x2 = math.mul(array4[num7].m_Matrix, float4x2);
					}
				}
			}
			a[0][3] += matrix[0][3];
			a[1][3] += matrix[1][3];
			a[2][3] += matrix[2][3];
		}
		math.mul(a, math.inverse(float4x2));
	}

	private void ResizeShapeBuffer()
	{
		if (!m_Initialized)
		{
			OnEnable();
		}
		int num = m_ShapeBuffer?.count ?? 0;
		int size = (int)m_ShapeAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 8);
			graphicsBuffer.name = "Shape buffer";
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

	public ShapeAllocation[] GetOrAddShapeData(RenderPrefab meshPrefab)
	{
		if (!m_Initialized)
		{
			OnEnable();
		}
		GeometryAsset geometryAsset = meshPrefab.geometryAsset;
		if (geometryAsset == null)
		{
			return null;
		}
		if (m_ShapeAllocationsCache != null && m_ShapeAllocationsCache.TryGetValue(geometryAsset.id, out var value))
		{
			return value;
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
		m_ShapeAllocationsCache.Add(geometryAsset.id, array);
		return array;
	}

	private void ResizeOverlayBuffer()
	{
		int num = m_OverlayBuffer?.count ?? 0;
		int size = (int)m_OverlayAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, OverlayAtlasElement.SizeOf);
			graphicsBuffer.name = "Overlay buffer";
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

	public OverlayAllocation[] GetOrAddOverlayData(RenderPrefab meshPrefab)
	{
		if (!m_Initialized)
		{
			OnEnable();
		}
		GeometryAsset geometryAsset = meshPrefab.geometryAsset;
		if (geometryAsset == null)
		{
			return null;
		}
		if (m_OverlayAllocationsCache != null && m_OverlayAllocationsCache.TryGetValue(geometryAsset.id, out var value))
		{
			return value;
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
				ResizeOverlayBuffer();
				m_OverlayBuffer.SetData(list.ToNativeArray(Allocator.Persistent), 0, (int)allocation.Begin, list.Count);
				array[i].m_Allocation = allocation;
			}
		}
		m_OverlayAllocationsCache.Add(geometryAsset.id, array);
		return array;
	}

	public void RemoveOverlayData(OverlayAllocation[] allocations)
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
			}
		}
	}

	public void ResizeCullBuffer()
	{
		int num = ((m_CullBuffer != null) ? m_CullBuffer.count : 0);
		int size = (int)m_CullAllocator.Size;
		if (num != size)
		{
			GraphicsBuffer graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, 4);
			graphicsBuffer.name = "Cull buffer";
			Shader.SetGlobalBuffer("cullBuffer", graphicsBuffer);
			if (m_CullBuffer != null)
			{
				int[] data = new int[num];
				m_CullBuffer.GetData(data);
				graphicsBuffer.SetData(data, 0, 0, num);
				m_CullBuffer.Release();
			}
			else
			{
				graphicsBuffer.SetData(new int[1], 0, 0, 1);
			}
			m_CullBuffer = graphicsBuffer;
		}
	}

	public void RetrieveBoneBuffers(out ComputeBuffer boneBuffer, out ComputeBuffer boneHistoryBuffer)
	{
		boneBuffer = m_BoneBuffer;
		boneHistoryBuffer = m_BoneHistoryBuffer;
	}

	private Game.Prefabs.AnimationClip GetClip(int clipIndex)
	{
		if (clipIndex != -1)
		{
			return m_AnimationClips[clipIndex];
		}
		return new Game.Prefabs.AnimationClip
		{
			m_InfoIndex = -1,
			m_PropID = new AnimatedPropID(-1)
		};
	}

	private void SetLayerData(out int index, out float frame, in Game.Prefabs.AnimationClip clipData, float time)
	{
		time = Mathf.Clamp(time, 0f, clipData.m_AnimationLength);
		index = clipData.m_InfoIndex;
		frame = time * clipData.m_FrameRate;
	}

	private void SetLayerData(ref AnimatedSystem.AnimationLayerData2 layerData, in Game.Prefabs.AnimationClip clipData0, in Game.Prefabs.AnimationClip clipData0I, in Game.Prefabs.AnimationClip clipData1, in Game.Prefabs.AnimationClip clipData1I, float2 time, float2 interpolation, float transition)
	{
		SetLayerData(out layerData.m_CurrentIndex, out layerData.m_CurrentFrame, in clipData0, time.x);
		if (clipData1.m_InfoIndex >= 0)
		{
			SetLayerData(out layerData.m_TransitionIndex.x, out layerData.m_TransitionFrame.x, in clipData1, time.y);
			layerData.m_TransitionWeight.x = transition;
			if (clipData0I.m_InfoIndex >= 0)
			{
				SetLayerData(out layerData.m_TransitionIndex.y, out layerData.m_TransitionFrame.y, in clipData0I, time.x);
				if (clipData1I.m_InfoIndex == clipData0I.m_InfoIndex)
				{
					layerData.m_TransitionWeight.y = math.csum(interpolation) * 0.5f;
				}
				else
				{
					layerData.m_TransitionWeight.y = interpolation.x * (1f - transition);
				}
			}
			else if (clipData1I.m_InfoIndex >= 0)
			{
				SetLayerData(out layerData.m_TransitionIndex.y, out layerData.m_TransitionFrame.y, in clipData1I, time.y);
				layerData.m_TransitionWeight.y = interpolation.y * transition;
			}
		}
		else if (clipData0I.m_InfoIndex >= 0)
		{
			SetLayerData(out layerData.m_TransitionIndex.x, out layerData.m_TransitionFrame.x, in clipData0I, time.x);
			layerData.m_TransitionWeight.x = interpolation.x;
		}
	}

	private void SetLayerData(ref AnimatedSystem.AnimationLayerData layerData, in Game.Prefabs.AnimationClip clipData0, in Game.Prefabs.AnimationClip clipData1, float2 time, float transition)
	{
		SetLayerData(out layerData.m_CurrentIndex, out layerData.m_CurrentFrame, in clipData0, time.x);
		if (clipData1.m_InfoIndex >= 0)
		{
			SetLayerData(out layerData.m_TransitionIndex, out layerData.m_TransitionFrame, in clipData1, time.y);
			layerData.m_TransitionWeight = transition;
		}
	}

	private void PlayAnimations()
	{
		if (m_InstanceIndices.Length != 0 && m_MaxBoneCount != 0)
		{
			ResizeBoneHistoryBuffer();
			m_AnimationComputeShader.SetInt(m_InstanceCountID, m_InstanceIndices.Length);
			m_AnimationComputeShader.SetInt(m_BodyInstanceCountID, m_BodyInstances.Length);
			m_AnimationComputeShader.SetInt(m_BodyTransitionCountID, m_BodyTransitions.Length);
			m_AnimationComputeShader.SetInt(m_BodyTransition2CountID, m_BodyTransitions2.Length);
			m_AnimationComputeShader.SetInt(m_FaceInstanceCountID, m_FaceInstances.Length);
			m_AnimationComputeShader.SetInt(m_FaceTransitionCountID, m_FaceTransitions.Length);
			m_AnimationComputeShader.SetInt(m_CorrectiveInstanceCountID, m_CorrectiveInstances.Length);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_MetadataBufferID, m_MetaBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_MetaIndexBufferID, m_InstanceBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_IndexBufferID, m_IndexBuffer);
			if (m_BodyInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_BoneBufferID, m_BoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimatedInstanceBufferID, m_BodyInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_BodyTransitions.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimatedTransitionBufferID, m_BodyTransitionBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_BodyTransitions2.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimatedTransition2BufferID, m_BodyTransition2Buffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_FaceInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_BoneBufferID, m_BoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimatedInstanceBufferID, m_FaceInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_FaceTransitions.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimatedTransitionBufferID, m_FaceTransitionBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_CorrectiveInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimatedInstanceBufferID, m_CorrectiveInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			int convertLocalCoordinatesWithHistoryKernelIx = m_ConvertLocalCoordinatesWithHistoryKernelIx;
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_MetadataBufferID, m_MetaBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_MetaIndexBufferID, m_InstanceBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_BoneBufferID, m_BoneBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_IndexBufferID, m_IndexBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
			m_AnimationComputeShader.SetBuffer(convertLocalCoordinatesWithHistoryKernelIx, m_BoneHistoryBufferID, m_BoneHistoryBuffer);
			m_AnimationComputeShader.GetKernelThreadGroupSizes(m_BlendRestPoseKernelIx, out var x, out var y, out var _);
			int threadGroupsX = (m_InstanceIndices.Length + (int)x - 1) / (int)x;
			int threadGroupsY = (m_MaxBoneCount + (int)y - 1) / (int)y;
			int threadGroupsY2 = (m_MaxActiveBoneCount + (int)y - 1) / (int)y;
			m_AnimationComputeShader.Dispatch(m_BlendRestPoseKernelIx, threadGroupsX, threadGroupsY, 1);
			if (m_BodyInstances.Length != 0)
			{
				int threadGroupsX2 = (m_BodyInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer0KernelIx, threadGroupsX2, threadGroupsY2, 1);
			}
			if (m_BodyTransitions.Length != 0)
			{
				int threadGroupsX3 = (m_BodyTransitions.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransitionLayer0KernelIx, threadGroupsX3, threadGroupsY, 1);
			}
			if (m_BodyTransitions2.Length != 0)
			{
				int threadGroupsX4 = (m_BodyTransitions2.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransition2Layer0KernelIx, threadGroupsX4, threadGroupsY, 1);
			}
			if (m_FaceInstances.Length != 0)
			{
				int threadGroupsX5 = (m_FaceInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer1KernelIx, threadGroupsX5, threadGroupsY2, 1);
			}
			if (m_FaceTransitions.Length != 0)
			{
				int threadGroupsX6 = (m_FaceTransitions.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransitionLayer1KernelIx, threadGroupsX6, threadGroupsY, 1);
			}
			if (m_CorrectiveInstances.Length != 0)
			{
				int threadGroupsX7 = (m_CorrectiveInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer2KernelIx, threadGroupsX7, threadGroupsY2, 1);
			}
			m_AnimationComputeShader.Dispatch(convertLocalCoordinatesWithHistoryKernelIx, threadGroupsX, threadGroupsY, 1);
		}
	}

	private void CacheAnimations(CharacterStyle stylePrefab, ref int motionCount, out PrefabClipData styleData)
	{
		if (m_StyleClipData.TryGetValue($"{stylePrefab.name}_{stylePrefab.m_Gender}", out styleData))
		{
			return;
		}
		CharacterStyle.AnimationInfo[] animations = stylePrefab.m_Animations;
		styleData = new PrefabClipData
		{
			m_RestPoseClipIndex = -1,
			m_Offset = m_AnimationClips.Count,
			m_Stride = stylePrefab.m_Animations.Length
		};
		for (int i = 0; i < styleData.m_Stride; i++)
		{
			CharacterStyle.AnimationInfo animationInfo = animations[i];
			Game.Prefabs.AnimationClip item = new Game.Prefabs.AnimationClip
			{
				m_InfoIndex = -1,
				m_RootMotionBone = animationInfo.rootMotionBone
			};
			switch (animationInfo.layer)
			{
			case Colossal.Animations.AnimationLayer.BodyLayer:
				item.m_Layer = Game.Prefabs.AnimationLayer.Body;
				break;
			case Colossal.Animations.AnimationLayer.PropLayer:
				item.m_Layer = Game.Prefabs.AnimationLayer.Prop;
				break;
			case Colossal.Animations.AnimationLayer.FacialLayer:
				item.m_Layer = Game.Prefabs.AnimationLayer.Facial;
				break;
			case Colossal.Animations.AnimationLayer.CorrectiveLayer:
				item.m_Layer = Game.Prefabs.AnimationLayer.Corrective;
				break;
			default:
				item.m_Layer = Game.Prefabs.AnimationLayer.None;
				break;
			}
			if (animationInfo.type == Colossal.Animations.AnimationType.RestPose && animationInfo.target == null)
			{
				styleData.m_RestPoseClipIndex = styleData.m_Offset + i;
			}
			m_AnimationClips.Add(item);
		}
		motionCount = 0;
		float num = float.MaxValue;
		float num2 = 0f;
		for (int j = 0; j < styleData.m_Stride; j++)
		{
			CharacterStyle.AnimationInfo animationInfo2 = animations[j];
			Game.Prefabs.AnimationClip value = m_AnimationClips[styleData.m_Offset + j];
			value.m_Type = animationInfo2.state;
			value.m_Activity = animationInfo2.activity;
			value.m_Conditions = animationInfo2.conditions;
			value.m_Playback = animationInfo2.playback;
			value.m_Gender = stylePrefab.m_Gender;
			value.m_TargetValue = float.MinValue;
			value.m_VariationCount = 1;
			for (int k = 0; k < j; k++)
			{
				Game.Prefabs.AnimationClip value2 = m_AnimationClips[k];
				if (value.m_Activity == value2.m_Activity && value.m_Type == value2.m_Type && value.m_Conditions == value2.m_Conditions && value.m_Layer == value2.m_Layer)
				{
					value.m_VariationIndex++;
					value.m_VariationCount++;
					value2.m_VariationCount++;
				}
				m_AnimationClips[k] = value2;
			}
			if (value.m_Playback == AnimationPlayback.RandomLoop || value.m_Type == Game.Prefabs.AnimationType.Move || value.m_Playback == AnimationPlayback.SyncToRelative)
			{
				value.m_AnimationLength = (float)animationInfo2.frameCount / (float)animationInfo2.frameRate;
				value.m_FrameRate = animationInfo2.frameRate;
			}
			else
			{
				float num3 = (float)(animationInfo2.frameCount - 1) * (60f / (float)animationInfo2.frameRate);
				num3 = math.max(1f, math.round(num3 / 16f)) * 16f;
				value.m_AnimationLength = num3 * (1f / 60f);
				value.m_FrameRate = (float)math.max(1, animationInfo2.frameCount - 1) / value.m_AnimationLength;
				value.m_AnimationLength -= 0.001f;
			}
			if (animationInfo2.rootMotion != null)
			{
				NativeArray<AnimationMotion> array = new NativeArray<AnimationMotion>(animationInfo2.rootMotion.Length, Allocator.Persistent);
				ArrayExtensions.ResizeArray(ref array, animationInfo2.rootMotion.Length);
				AnimatedPrefabSystem.CleanUpRootMotion(animationInfo2.rootMotion, array, animationInfo2.activity);
				m_AnimationMotions.AddRange(array);
				value.m_MotionRange = new int2(motionCount, motionCount + animationInfo2.rootMotion.Length);
				motionCount += animationInfo2.rootMotion.Length;
				if (value.m_Type == Game.Prefabs.AnimationType.Move)
				{
					AnimationMotion animationMotion = array[0];
					value.m_MovementSpeed = math.length(animationMotion.m_EndOffset - animationMotion.m_StartOffset) * value.m_FrameRate / (float)math.max(1, animationInfo2.frameCount - 1);
					if ((double)value.m_MovementSpeed < 0.001)
					{
						value.m_MovementSpeed = (float)math.max(1, animationInfo2.frameCount - 1) / value.m_FrameRate * 3.6f;
					}
					if (value.m_Conditions == (ActivityCondition)0u)
					{
						switch (value.m_Activity)
						{
						case ActivityType.Walking:
							num = value.m_MovementSpeed;
							break;
						case ActivityType.Running:
							num2 = value.m_MovementSpeed;
							break;
						}
					}
				}
			}
			else
			{
				value.m_RootMotionBone = -1;
			}
			m_AnimationClips[styleData.m_Offset + j] = value;
		}
		for (int l = 0; l < animations.Length; l++)
		{
			Game.Prefabs.AnimationClip value3 = m_AnimationClips[styleData.m_Offset + l];
			if (value3.m_Layer == Game.Prefabs.AnimationLayer.Body && value3.m_Type == Game.Prefabs.AnimationType.Move)
			{
				value3.m_SpeedRange = new Bounds1(0f, float.MaxValue);
				switch (value3.m_Activity)
				{
				case ActivityType.Walking:
					value3.m_SpeedRange.max = math.select((num + num2) * 0.5f, float.MaxValue, num2 <= num);
					break;
				case ActivityType.Running:
					value3.m_SpeedRange.min = math.select((num + num2) * 0.5f, 0f, num >= num2);
					break;
				}
			}
			m_AnimationClips[styleData.m_Offset + l] = value3;
		}
		for (int m = 0; m < animations.Length; m++)
		{
			CharacterStyle.AnimationInfo animationInfo3 = animations[m];
			Game.Prefabs.AnimationClip value4 = m_AnimationClips[styleData.m_Offset + m];
			if (animationInfo3.target != null && animationInfo3.target.TryGet<CharacterProperties>(out var component))
			{
				value4.m_PropID = GetPropID(component.m_AnimatedPropName);
				if (AssetDatabase.global.resources.prefabsMap.TryGetObject(component.m_AnimatedPropName, typeof(ActivityPropPrefab), out var obj) && !m_PropStyles.ContainsKey(value4.m_PropID.index))
				{
					m_PropStyles.TryAdd(value4.m_PropID.index, new PropStyleData(animationInfo3.target, (ActivityPropPrefab)obj));
				}
			}
			else
			{
				value4.m_PropID = GetPropID(null);
			}
			m_AnimationClips[styleData.m_Offset + m] = value4;
		}
		m_StyleClipData.Add($"{stylePrefab.name}_{stylePrefab.m_Gender}", styleData);
	}

	private void CacheAnimations(ActivityPropPrefab propPrefab, string propName, ref int motionCount, out PrefabClipData styleData)
	{
		if (m_StyleClipData.TryGetValue(propPrefab.name ?? "", out styleData))
		{
			return;
		}
		styleData = new PrefabClipData
		{
			m_Offset = m_AnimationClips.Count,
			m_Stride = propPrefab.m_Animations.Length,
			m_RestPoseClipIndex = -1
		};
		for (int i = 0; i < styleData.m_Stride; i++)
		{
			ActivityPropPrefab.AnimationInfo animationInfo = propPrefab.m_Animations[i];
			Game.Prefabs.AnimationClip item = new Game.Prefabs.AnimationClip
			{
				m_InfoIndex = -1,
				m_PropID = GetPropID(propName),
				m_RootMotionBone = animationInfo.rootMotionBone,
				m_Layer = Game.Prefabs.AnimationLayer.Prop,
				m_VariationCount = 1,
				m_Gender = animationInfo.gender
			};
			if (animationInfo.rootMotion != null)
			{
				motionCount += animationInfo.rootMotion.Length;
			}
			if (animationInfo.type == Colossal.Animations.AnimationType.RestPose)
			{
				styleData.m_RestPoseClipIndex = styleData.m_Offset + i;
			}
			m_PropClipIndex.TryAdd(new AnimatedSystem.PropClipKey(item.m_PropID, animationInfo.activity, animationInfo.state, animationInfo.gender), styleData.m_Offset + i);
			m_AnimationClips.Add(item);
		}
		motionCount = m_AnimationMotions.Count;
		float num = float.MaxValue;
		float num2 = 0f;
		for (int j = 0; j < styleData.m_Stride; j++)
		{
			ActivityPropPrefab.AnimationInfo animationInfo2 = propPrefab.m_Animations[j];
			Game.Prefabs.AnimationClip value = m_AnimationClips[styleData.m_Offset + j];
			value.m_Type = animationInfo2.state;
			value.m_Activity = animationInfo2.activity;
			value.m_Conditions = animationInfo2.conditions;
			value.m_Playback = animationInfo2.playback;
			value.m_Gender = animationInfo2.gender;
			value.m_TargetValue = float.MinValue;
			if (value.m_Playback == AnimationPlayback.RandomLoop || value.m_Type == Game.Prefabs.AnimationType.Move || value.m_Playback == AnimationPlayback.SyncToRelative)
			{
				value.m_AnimationLength = (float)animationInfo2.frameCount / (float)animationInfo2.frameRate;
				value.m_FrameRate = animationInfo2.frameRate;
			}
			else
			{
				float num3 = (float)(animationInfo2.frameCount - 1) * (60f / (float)animationInfo2.frameRate);
				num3 = math.max(1f, math.round(num3 / 16f)) * 16f;
				value.m_AnimationLength = num3 * (1f / 60f);
				value.m_FrameRate = (float)math.max(1, animationInfo2.frameCount - 1) / value.m_AnimationLength;
				value.m_AnimationLength -= 0.001f;
			}
			if (animationInfo2.rootMotion != null)
			{
				NativeArray<AnimationMotion> array = new NativeArray<AnimationMotion>(animationInfo2.rootMotion.Length, Allocator.Persistent);
				ArrayExtensions.ResizeArray(ref array, animationInfo2.rootMotion.Length);
				AnimatedPrefabSystem.CleanUpRootMotion(animationInfo2.rootMotion, array, animationInfo2.activity);
				m_AnimationMotions.AddRange(array);
				value.m_MotionRange = new int2(motionCount, motionCount + animationInfo2.rootMotion.Length);
				motionCount += animationInfo2.rootMotion.Length;
				if (value.m_Type == Game.Prefabs.AnimationType.Move)
				{
					AnimationMotion animationMotion = array[0];
					value.m_MovementSpeed = math.length(animationMotion.m_EndOffset - animationMotion.m_StartOffset) * value.m_FrameRate / (float)math.max(1, animationInfo2.frameCount - 1);
					if ((double)value.m_MovementSpeed < 0.001)
					{
						value.m_MovementSpeed = (float)math.max(1, animationInfo2.frameCount - 1) / value.m_FrameRate * 3.6f;
					}
					if (value.m_Conditions == (ActivityCondition)0u)
					{
						switch (value.m_Activity)
						{
						case ActivityType.Walking:
							num = value.m_MovementSpeed;
							break;
						case ActivityType.Running:
							num2 = value.m_MovementSpeed;
							break;
						}
					}
				}
			}
			else
			{
				value.m_RootMotionBone = -1;
			}
			m_AnimationClips[styleData.m_Offset + j] = value;
		}
		for (int k = 0; k < styleData.m_Stride; k++)
		{
			Game.Prefabs.AnimationClip value2 = m_AnimationClips[styleData.m_Offset + k];
			if (value2.m_Layer == Game.Prefabs.AnimationLayer.Body && value2.m_Type == Game.Prefabs.AnimationType.Move)
			{
				value2.m_SpeedRange = new Bounds1(0f, float.MaxValue);
				switch (value2.m_Activity)
				{
				case ActivityType.Walking:
					value2.m_SpeedRange.max = math.select((num + num2) * 0.5f, float.MaxValue, num2 <= num);
					break;
				case ActivityType.Running:
					value2.m_SpeedRange.min = math.select((num + num2) * 0.5f, 0f, num >= num2);
					break;
				}
			}
			m_AnimationClips[styleData.m_Offset + k] = value2;
		}
		m_StyleClipData.Add(propPrefab.name, styleData);
	}

	private void LoadAnimation(int clipIndex, int restPoseClipIndex, Colossal.Animations.AnimationClip animation, int shapeCount, int boneCount, bool removeRootDelta = false)
	{
		if (m_LoadedAnimations.Contains(clipIndex))
		{
			return;
		}
		Game.Prefabs.AnimationClip animationClip = m_AnimationClips[clipIndex];
		DynamicBuffer<RestPoseElement> restPose = default(DynamicBuffer<RestPoseElement>);
		uint num = (uint)animation.m_Animation.elements.Length;
		AnimatedSystem.AnimationClipData value = new AnimatedSystem.AnimationClipData
		{
			m_AnimAllocation = m_AnimAllocator.Allocate(num)
		};
		if (restPoseClipIndex == clipIndex)
		{
			value.m_HierarchyAllocation = AllocateIndexData((uint)animation.m_BoneHierarchy.Length);
		}
		value.m_ShapeAllocation = AllocateIndexData((uint)shapeCount);
		value.m_BoneAllocation = AllocateIndexData((uint)animation.m_Animation.boneIndices.Length);
		value.m_InverseBoneAllocation = AllocateIndexData((uint)boneCount);
		m_MaxBoneCount = math.max(m_MaxBoneCount, animation.m_BoneHierarchy.Length);
		m_MaxActiveBoneCount = math.max(m_MaxActiveBoneCount, animation.m_Animation.boneIndices.Length);
		animationClip.m_InfoIndex = m_AnimationClipData.Length;
		m_AnimationClipData.Add(in value);
		ResizeAnimInfoBuffer();
		ResizeAnimBuffer();
		ResizeIndexBuffer();
		NativeArray<AnimationInfoData> data = new NativeArray<AnimationInfoData>(1, Allocator.Temp);
		NativeArray<Animation.Element> nativeArray;
		float3 positionMin;
		float3 positionRange;
		if (removeRootDelta)
		{
			Animation.ElementRaw[] array = new Animation.ElementRaw[animation.m_Animation.elements.Length];
			for (int i = 0; i < animation.m_Animation.boneIndices.Length; i++)
			{
				for (int j = 0; j < animation.m_Animation.shapeIndices.Length; j++)
				{
					int num2 = ((animation.m_Animation.type == Colossal.Animations.AnimationType.RestPose) ? 1 : (animation.m_Animation.frameCount + 1));
					for (int k = 0; k < num2; k++)
					{
						int num3 = (k * boneCount + i) * shapeCount + j;
						Animation.ElementRaw elementRaw = animation.m_Animation.DecodeElement(num3);
						if (i == 0)
						{
							elementRaw.position = float3.zero;
						}
						array[num3] = elementRaw;
					}
				}
			}
			AnimationEncoding.CalcBoundingBox(array, out var positionsMin, out var positionsRange);
			Animation.Element[] array2 = new Animation.Element[array.Length];
			AnimationEncoding.EncodeElements(array, array2, positionsMin, positionsRange);
			nativeArray = new NativeArray<Animation.Element>(array2, Allocator.Temp);
			positionMin = positionsMin;
			positionRange = positionsRange;
		}
		else
		{
			nativeArray = new NativeArray<Animation.Element>(animation.m_Animation.elements, Allocator.Temp);
			positionMin = animation.m_Animation.positionMin;
			positionRange = animation.m_Animation.positionRange;
		}
		data[0] = new AnimationInfoData
		{
			m_Offset = (int)value.m_AnimAllocation.Begin,
			m_Hierarchy = (value.m_HierarchyAllocation.Empty ? (-1) : ((int)value.m_HierarchyAllocation.Begin)),
			m_Shapes = (int)value.m_ShapeAllocation.Begin,
			m_Bones = (int)value.m_BoneAllocation.Begin,
			m_InverseBones = (int)value.m_InverseBoneAllocation.Begin,
			m_ShapeCount = animation.m_Animation.shapeIndices.Length,
			m_BoneCount = animation.m_Animation.boneIndices.Length,
			m_Type = (int)animation.m_Animation.type,
			m_PositionMin = positionMin,
			m_PositionRange = positionRange
		};
		m_AnimInfoBuffer.SetData(data, 0, animationClip.m_InfoIndex, 1);
		data.Dispose();
		if (animationClip.m_RootMotionBone != -1 && animationClip.m_Layer != Game.Prefabs.AnimationLayer.Prop && m_RemoveRootMotion)
		{
			NativeArray<AnimationMotion> motions = m_AnimationMotions.GetRange(animationClip.m_MotionRange.x, animationClip.m_MotionRange.y - animationClip.m_MotionRange.x).ToNativeArray(Allocator.Persistent);
			AnimatedSystem.RemoveRootMotion(animation, animationClip, restPose, motions, nativeArray);
		}
		if (animationClip.m_TargetValue == float.MinValue)
		{
			if (animationClip.m_Layer == Game.Prefabs.AnimationLayer.Prop)
			{
				animationClip.m_TargetValue = FindTargetValue(animation, animationClip, nativeArray);
			}
			else if (animationClip.m_PropID.isValid)
			{
				animationClip.m_TargetValue = FindTargetValue(animation, animationClip, nativeArray);
				if (m_PropClipIndex.TryGetValue(new AnimatedSystem.PropClipKey(animationClip.m_PropID, animationClip.m_Activity, animationClip.m_Type, animationClip.m_Gender), out var item) && m_AnimationClips.Count > item)
				{
					animationClip.m_TargetValue = m_AnimationClips[item].m_TargetValue;
				}
			}
		}
		m_AnimBuffer.SetData(nativeArray, 0, (int)value.m_AnimAllocation.Begin, (int)num);
		nativeArray.Dispose();
		if (!value.m_HierarchyAllocation.Empty)
		{
			m_IndexBuffer.SetData(animation.m_BoneHierarchy, 0, (int)value.m_HierarchyAllocation.Begin, animation.m_BoneHierarchy.Length);
		}
		if (!value.m_ShapeAllocation.Empty)
		{
			NativeArray<int> data2 = new NativeArray<int>(shapeCount, Allocator.Temp);
			for (int l = 0; l < data2.Length; l++)
			{
				data2[l] = -1;
			}
			for (int m = 0; m < animation.m_Animation.shapeIndices.Length; m++)
			{
				data2[animation.m_Animation.shapeIndices[m]] = m;
			}
			m_IndexBuffer.SetData(data2, 0, (int)value.m_ShapeAllocation.Begin, data2.Length);
			data2.Dispose();
		}
		if (!value.m_BoneAllocation.Empty)
		{
			m_IndexBuffer.SetData(animation.m_Animation.boneIndices, 0, (int)value.m_BoneAllocation.Begin, animation.m_Animation.boneIndices.Length);
		}
		if (!value.m_InverseBoneAllocation.Empty)
		{
			NativeArray<int> data3 = new NativeArray<int>(boneCount, Allocator.Temp);
			for (int n = 0; n < data3.Length; n++)
			{
				data3[n] = -1;
			}
			for (int num4 = 0; num4 < animation.m_Animation.boneIndices.Length; num4++)
			{
				data3[animation.m_Animation.boneIndices[num4]] = num4;
			}
			m_IndexBuffer.SetData(data3, 0, (int)value.m_InverseBoneAllocation.Begin, data3.Length);
			data3.Dispose();
		}
		m_AnimationClips[clipIndex] = animationClip;
		m_LoadedAnimations.Add(clipIndex);
	}

	private void UpdateAnimation(CharacterStyle stylePrefab, int offset, int animationIndex, int restPoseClipIndex)
	{
		if (animationIndex == -1 || m_LoadedAnimations.Contains(animationIndex))
		{
			return;
		}
		AnimationAsset animation = stylePrefab.GetAnimation(animationIndex - offset);
		try
		{
			Colossal.Animations.AnimationClip animation2 = animation.Load();
			LoadAnimation(animationIndex, restPoseClipIndex, animation2, stylePrefab.m_ShapeCount, stylePrefab.m_BoneCount);
			animation.Unload();
		}
		catch (Exception)
		{
			animation.Unload();
		}
	}

	private int GetClipIndex(MetaInstanceData metaInstanceData, Game.Prefabs.AnimationLayer layer, PrefabClipData prefabStyleClipData)
	{
		return GetClipIndex(metaInstanceData, layer, prefabStyleClipData, new AnimatedPropID(-1));
	}

	private int GetClipIndex(MetaInstanceData metaInstanceData, Game.Prefabs.AnimationLayer layer, PrefabClipData prefabStyleClipData, AnimatedPropID animatedPropID)
	{
		if (metaInstanceData.m_Activity == ActivityType.None && metaInstanceData.m_TransformState == Game.Prefabs.AnimationType.None)
		{
			return metaInstanceData.m_RestPoseIndex;
		}
		int num = m_AnimationClips.GetRange(prefabStyleClipData.m_Offset, prefabStyleClipData.m_Stride).FindIndex((Game.Prefabs.AnimationClip c) => c.m_Layer == layer && c.m_Activity == metaInstanceData.m_Activity && (c.m_Conditions ^ metaInstanceData.m_Condition) == (ActivityCondition)0u && c.m_Type == metaInstanceData.m_TransformState && (c.m_Gender & metaInstanceData.m_Gender) != 0 && (!animatedPropID.isValid || c.m_PropID == animatedPropID));
		if (num == -1)
		{
			return metaInstanceData.m_RestPoseIndex;
		}
		return prefabStyleClipData.m_Offset + num;
	}

	private void UpdateAnimation(ActivityPropPrefab stylePrefab, int offset, int animationIndex, int restPoseClipIndex, bool cleanRootDeltas)
	{
		if (animationIndex == -1 || m_LoadedAnimations.Contains(animationIndex))
		{
			return;
		}
		AnimationAsset animation = stylePrefab.GetAnimation(animationIndex - offset);
		try
		{
			Colossal.Animations.AnimationClip animation2 = animation.Load();
			LoadAnimation(animationIndex, restPoseClipIndex, animation2, 1, stylePrefab.m_BoneCount, cleanRootDeltas);
			animation.Unload();
		}
		catch (Exception)
		{
			animation.Unload();
		}
	}

	private unsafe void InitializeNativeCollections()
	{
		if (m_AnimationClipData.IsCreated)
		{
			m_AnimationClipData.Dispose();
		}
		if (m_AnimAllocator.IsCreated)
		{
			m_AnimAllocator.Dispose();
		}
		if (m_IndexAllocator.IsCreated)
		{
			m_IndexAllocator.Dispose();
		}
		if (m_BoneAllocator.IsCreated)
		{
			m_BoneAllocator.Dispose();
		}
		if (m_BodyInstances.IsCreated)
		{
			m_BodyInstances.Dispose();
		}
		if (m_FaceInstances.IsCreated)
		{
			m_FaceInstances.Dispose();
		}
		if (m_CorrectiveInstances.IsCreated)
		{
			m_CorrectiveInstances.Dispose();
		}
		if (m_BodyTransitions.IsCreated)
		{
			m_BodyTransitions.Dispose();
		}
		if (m_BodyTransitions2.IsCreated)
		{
			m_BodyTransitions2.Dispose();
		}
		if (m_FaceTransitions.IsCreated)
		{
			m_FaceTransitions.Dispose();
		}
		if (m_MetaBufferData.IsCreated)
		{
			m_MetaBufferData.Dispose();
		}
		m_BoneAllocator = new NativeHeapAllocator(8388608u / (uint)sizeof(BoneElement), 1u, Allocator.Persistent);
		m_AnimAllocator = new NativeHeapAllocator(33554432u / (uint)sizeof(Animation.Element), 1u, Allocator.Persistent);
		m_IndexAllocator = new NativeHeapAllocator(16384u, 1u, Allocator.Persistent);
		m_MetaBufferData = new NativeList<MetaBufferData>(1000, Allocator.Persistent);
		m_InstanceIndices = new NativeList<RestPoseInstance>(1000, Allocator.Persistent);
		m_BodyInstances = new NativeList<AnimatedInstance>(1000, Allocator.Persistent);
		m_FaceInstances = new NativeList<AnimatedInstance>(1000, Allocator.Persistent);
		m_CorrectiveInstances = new NativeList<AnimatedInstance>(100, Allocator.Persistent);
		m_BodyTransitions = new NativeList<AnimatedTransition>(100, Allocator.Persistent);
		m_BodyTransitions2 = new NativeList<AnimatedTransition2>(100, Allocator.Persistent);
		m_FaceTransitions = new NativeList<AnimatedTransition>(100, Allocator.Persistent);
		m_AnimationClipData = new NativeList<AnimatedSystem.AnimationClipData>(10, Allocator.Persistent);
		m_PropClipIndex = new NativeHashMap<AnimatedSystem.PropClipKey, int>(100, Allocator.Persistent);
		m_PropIds = new Dictionary<string, int>();
		m_PropStyles = new Dictionary<int, PropStyleData>();
		m_MetaInstanceData = new List<MetaInstanceData>();
		m_Initialized = true;
	}

	public void SetupAnimationBuffers(CharacterStyle characterStyle, MetaBufferData metaBufferData, ref MetaInstanceData metaInstanceData, string propName, out PropStyleData propPrefab)
	{
		propPrefab = null;
		m_LoadingQueue.Enqueue($"{characterStyle.name}_{characterStyle.m_Gender}");
		if (metaInstanceData.m_MetaIndex == -1)
		{
			metaInstanceData.m_MetaIndex = m_MetaBufferData.Length;
			metaInstanceData.m_Gender = characterStyle.m_Gender;
			metaBufferData.m_BoneOffset = (int)AllocateBones(characterStyle.m_BoneCount).Begin;
			metaInstanceData.m_MetaData = metaBufferData;
			m_MetaBufferData.Add(in metaBufferData);
			int motionCount = 0;
			CacheAnimations(characterStyle, ref motionCount, out var styleData);
			metaInstanceData.m_RestPoseIndex = styleData.m_RestPoseClipIndex;
			ResizeBoneBuffer();
			ResizeMetaBuffer();
			UpdateAnimation(characterStyle, styleData.m_Offset, metaInstanceData.m_RestPoseIndex, metaInstanceData.m_RestPoseIndex);
			m_MetaInstanceData.Add(metaInstanceData);
		}
		else
		{
			metaInstanceData.m_MetaData = metaBufferData;
			m_MetaBufferData[metaInstanceData.m_MetaIndex] = metaBufferData;
		}
		PrefabClipData prefabClipData = m_StyleClipData[$"{characterStyle.name}_{characterStyle.m_Gender}"];
		int metaIndex = metaInstanceData.m_MetaIndex;
		int index = m_MetaInstanceData.FindIndex((MetaInstanceData i) => i.m_MetaIndex == metaIndex);
		AnimatedPropID propID = GetPropID(propName, insert: false);
		int clipIndex = GetClipIndex(metaInstanceData, Game.Prefabs.AnimationLayer.Body, prefabClipData, propID);
		if (clipIndex != metaInstanceData.m_BodyIndex)
		{
			UpdateAnimation(characterStyle, prefabClipData.m_Offset, clipIndex, metaInstanceData.m_RestPoseIndex);
			metaInstanceData.m_BodyIndex = clipIndex;
			metaInstanceData.m_BodyFrame = 0f;
			metaInstanceData.m_BodyRange = ((metaInstanceData.m_BodyIndex != -1) ? m_AnimationClips[metaInstanceData.m_BodyIndex].m_AnimationLength : 0f);
		}
		int clipIndex2 = GetClipIndex(metaInstanceData, Game.Prefabs.AnimationLayer.Facial, prefabClipData);
		if (clipIndex2 != metaInstanceData.m_FacialIndex)
		{
			UpdateAnimation(characterStyle, prefabClipData.m_Offset, clipIndex, metaInstanceData.m_RestPoseIndex);
			metaInstanceData.m_FacialIndex = clipIndex2;
			metaInstanceData.m_FaceFrame = 0f;
			metaInstanceData.m_FaceRange = ((metaInstanceData.m_FacialIndex != -1) ? m_AnimationClips[metaInstanceData.m_FacialIndex].m_AnimationLength : 0f);
		}
		UpdateMetaData();
		Game.Prefabs.AnimationClip clip = GetClip(metaInstanceData.m_BodyIndex);
		if (clip.m_PropID.isValid && m_PropStyles.TryGetValue(clip.m_PropID.index, out var value))
		{
			propPrefab = value;
		}
		m_MetaInstanceData[index] = metaInstanceData;
		m_LoadingQueue.Dequeue();
	}

	public void SetupAnimationBuffers(ActivityPropPrefab propStyle, ref MetaBufferData metaBufferData, ref MetaInstanceData metaInstanceData, bool cleanRootDeltas)
	{
		m_LoadingQueue.Enqueue(propStyle.name);
		if (metaInstanceData.m_MetaIndex == -1)
		{
			metaInstanceData.m_MetaIndex = m_MetaBufferData.Length;
			metaBufferData.m_BoneOffset = (int)AllocateBones(propStyle.m_BoneCount).Begin;
			m_MetaBufferData.Add(in metaBufferData);
			metaInstanceData.m_MetaData = metaBufferData;
			int motionCount = 0;
			CacheAnimations(propStyle, propStyle.name, ref motionCount, out var styleData);
			metaInstanceData.m_RestPoseIndex = styleData.m_RestPoseClipIndex;
			ResizeBoneBuffer();
			ResizeMetaBuffer();
			UpdateAnimation(propStyle, styleData.m_Offset, metaInstanceData.m_RestPoseIndex, metaInstanceData.m_RestPoseIndex, cleanRootDeltas);
			m_MetaInstanceData.Add(metaInstanceData);
		}
		PrefabClipData prefabClipData = m_StyleClipData[propStyle.name];
		int metaIndex = metaInstanceData.m_MetaIndex;
		int index = m_MetaInstanceData.FindIndex((MetaInstanceData i) => i.m_MetaIndex == metaIndex);
		int clipIndex = GetClipIndex(metaInstanceData, Game.Prefabs.AnimationLayer.Prop, prefabClipData);
		if (clipIndex != metaInstanceData.m_BodyIndex)
		{
			UpdateAnimation(propStyle, prefabClipData.m_Offset, clipIndex, metaInstanceData.m_RestPoseIndex, cleanRootDeltas);
			metaInstanceData.m_BodyIndex = clipIndex;
			metaInstanceData.m_BodyFrame = 0f;
			metaInstanceData.m_BodyRange = ((metaInstanceData.m_BodyIndex != -1) ? m_AnimationClips[metaInstanceData.m_BodyIndex].m_AnimationLength : 0f);
		}
		metaInstanceData.m_FacialIndex = 0;
		UpdateMetaData();
		m_MetaInstanceData[index] = metaInstanceData;
		m_LoadingQueue.Dequeue();
	}

	private void SetInstancesData(List<(int metaIndex, int linkIndex, int restPoseClipIndex, AnimatedSystem.AnimationLayerData2 bodyData, AnimatedSystem.AnimationLayerData faceData)> instances)
	{
		m_InstanceIndices.Clear();
		m_BodyInstances.Clear();
		m_FaceInstances.Clear();
		m_BodyTransitions.Clear();
		m_BodyTransitions2.Clear();
		m_FaceTransitions.Clear();
		foreach (var instance in instances)
		{
			m_InstanceIndices.Add(new RestPoseInstance
			{
				m_MetaIndex = instance.metaIndex,
				m_RestPoseIndex = instance.restPoseClipIndex,
				m_ResetHistory = 0
			});
			if (instance.bodyData.m_CurrentIndex != -1)
			{
				if (instance.bodyData.m_TransitionIndex.x >= 0)
				{
					if (instance.bodyData.m_TransitionIndex.y >= 0)
					{
						ref NativeList<AnimatedTransition2> bodyTransitions = ref m_BodyTransitions2;
						AnimatedTransition2 value = new AnimatedTransition2
						{
							m_MetaIndex = instance.metaIndex,
							m_CurrentIndex = instance.bodyData.m_CurrentIndex,
							m_CurrentFrame = instance.bodyData.m_CurrentFrame,
							m_TransitionIndex = instance.bodyData.m_TransitionIndex,
							m_TransitionFrame = instance.bodyData.m_TransitionFrame,
							m_TransitionWeight = instance.bodyData.m_TransitionWeight
						};
						bodyTransitions.Add(in value);
					}
					else
					{
						ref NativeList<AnimatedTransition> bodyTransitions2 = ref m_BodyTransitions;
						AnimatedTransition value2 = new AnimatedTransition
						{
							m_MetaIndex = instance.metaIndex,
							m_CurrentIndex = instance.bodyData.m_CurrentIndex,
							m_TransitionIndex = instance.bodyData.m_TransitionIndex.x,
							m_CurrentFrame = instance.bodyData.m_CurrentFrame,
							m_TransitionFrame = instance.bodyData.m_TransitionFrame.x,
							m_TransitionWeight = instance.bodyData.m_TransitionWeight.x
						};
						bodyTransitions2.Add(in value2);
					}
				}
				else
				{
					ref NativeList<AnimatedInstance> bodyInstances = ref m_BodyInstances;
					AnimatedInstance value3 = new AnimatedInstance
					{
						m_MetaIndex = instance.metaIndex,
						m_CurrentIndex = instance.bodyData.m_CurrentIndex,
						m_CurrentFrame = instance.bodyData.m_CurrentFrame
					};
					bodyInstances.Add(in value3);
				}
			}
			if (instance.faceData.m_CurrentIndex >= 0)
			{
				if (instance.faceData.m_TransitionIndex >= 0)
				{
					ref NativeList<AnimatedTransition> faceTransitions = ref m_FaceTransitions;
					AnimatedTransition value2 = new AnimatedTransition
					{
						m_MetaIndex = instance.metaIndex,
						m_CurrentIndex = instance.faceData.m_CurrentIndex,
						m_TransitionIndex = instance.faceData.m_TransitionIndex,
						m_CurrentFrame = instance.faceData.m_CurrentFrame,
						m_TransitionFrame = instance.faceData.m_TransitionFrame,
						m_TransitionWeight = instance.faceData.m_TransitionWeight
					};
					faceTransitions.Add(in value2);
				}
				else
				{
					ref NativeList<AnimatedInstance> faceInstances = ref m_FaceInstances;
					AnimatedInstance value3 = new AnimatedInstance
					{
						m_MetaIndex = instance.metaIndex,
						m_CurrentIndex = instance.faceData.m_CurrentIndex,
						m_CurrentFrame = instance.faceData.m_CurrentFrame
					};
					faceInstances.Add(in value3);
				}
			}
		}
	}

	private void UpdateMetaData()
	{
		m_MetaBuffer.SetData(m_MetaBufferData.AsArray(), 0, 0, m_MetaBufferData.Length);
	}

	private void UpdateInstances()
	{
		UpdateInstanceBuffer(ref m_InstanceBuffer, m_InstanceIndices, "InstanceBuffer");
		UpdateInstanceBuffer(ref m_BodyInstanceBuffer, m_BodyInstances, "BodyInstanceBuffer");
		UpdateInstanceBuffer(ref m_FaceInstanceBuffer, m_FaceInstances, "FaceInstanceBuffer");
		UpdateInstanceBuffer(ref m_CorrectiveInstanceBuffer, m_CorrectiveInstances, "CorrectiveInstanceBuffer");
		UpdateInstanceBuffer(ref m_BodyTransitionBuffer, m_BodyTransitions, "BodyTransitionBuffer");
		UpdateInstanceBuffer(ref m_BodyTransition2Buffer, m_BodyTransitions2, "BodyTransitionBuffer2");
		UpdateInstanceBuffer(ref m_FaceTransitionBuffer, m_FaceTransitions, "FaceTransitionBuffer");
	}

	private unsafe void UpdateInstanceBuffer<T>(ref ComputeBuffer buffer, NativeList<T> data, string parameterName) where T : unmanaged
	{
		if (buffer == null || buffer.count != data.Capacity)
		{
			if (buffer != null)
			{
				buffer.Release();
			}
			buffer = new ComputeBuffer(data.Capacity, sizeof(T), ComputeBufferType.Structured);
			buffer.name = parameterName;
		}
		if (data.Length != 0)
		{
			buffer.SetData(data.AsArray(), 0, 0, data.Length);
		}
	}

	private NativeHeapBlock AllocateIndexData(uint size)
	{
		NativeHeapBlock result = m_IndexAllocator.Allocate(size);
		while (result.Empty)
		{
			uint num = 4096u;
			num = (num + size - 1) / num * num;
			m_IndexAllocator.Resize(m_IndexAllocator.Size + num);
			result = m_IndexAllocator.Allocate(size);
		}
		return result;
	}

	private unsafe void ResizeAnimInfoBuffer()
	{
		int num = ((m_AnimInfoBuffer != null) ? m_AnimInfoBuffer.count : 0);
		int capacity = m_AnimationClipData.Capacity;
		if (num != capacity)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(capacity, sizeof(AnimationInfoData), ComputeBufferType.Structured);
			computeBuffer.name = "Animation info buffer";
			int num2 = math.min(num, capacity);
			if (num2 > 0)
			{
				AnimationInfoData[] data = new AnimationInfoData[num2];
				m_AnimInfoBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_AnimInfoBuffer != null)
			{
				m_AnimInfoBuffer.Release();
			}
			m_AnimInfoBuffer = computeBuffer;
		}
	}

	private unsafe void ResizeAnimBuffer()
	{
		int num = ((m_AnimBuffer != null) ? m_AnimBuffer.count : 0);
		int size = (int)m_AnimAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(Animation.Element), ComputeBufferType.Structured);
			computeBuffer.name = "Animation buffer";
			int num2 = math.min(num, size);
			if (num2 > 0)
			{
				Animation.Element[] data = new Animation.Element[num2];
				m_AnimBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_AnimBuffer != null)
			{
				m_AnimBuffer.Release();
			}
			m_AnimBuffer = computeBuffer;
		}
	}

	private unsafe void ResizeMetaBuffer()
	{
		int num = m_MetaBuffer?.count ?? 0;
		int num2 = 1048576 / sizeof(MetaBufferData);
		if (m_MetaBufferData.Length > num && m_MetaBufferData.Length > num2)
		{
			num2 += ((m_MetaBufferData.Length - num2) * sizeof(MetaBufferData) + 262144 - 1) / 262144 * 262144 / sizeof(MetaBufferData);
		}
		else if (num > num2)
		{
			num2 = num;
		}
		if (num != num2)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(num2, sizeof(MetaBufferData), ComputeBufferType.Structured);
			computeBuffer.name = "Meta buffer";
			Shader.SetGlobalBuffer("metaBuffer", computeBuffer);
			if (m_MetaBuffer != null)
			{
				computeBuffer.SetData(m_MetaBufferData.AsArray(), 0, 0, num);
				m_MetaBuffer.Release();
			}
			else
			{
				computeBuffer.SetData(m_MetaBufferData.AsArray(), 0, 0, 1);
			}
			m_MetaBuffer = computeBuffer;
		}
	}

	private void ResizeIndexBuffer()
	{
		int num = ((m_IndexBuffer != null) ? m_IndexBuffer.count : 0);
		int size = (int)m_IndexAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, 4, ComputeBufferType.Structured);
			computeBuffer.name = "Index buffer";
			int num2 = math.min(num, size);
			if (num2 > 0)
			{
				int[] data = new int[num2];
				m_IndexBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_IndexBuffer != null)
			{
				m_IndexBuffer.Release();
			}
			m_IndexBuffer = computeBuffer;
		}
	}

	private unsafe NativeHeapBlock AllocateBones(int boneCount)
	{
		NativeHeapBlock result = m_BoneAllocator.Allocate((uint)boneCount);
		if (result.Empty)
		{
			m_BoneAllocator.Resize(m_BoneAllocator.Size + 2097152u / (uint)sizeof(BoneElement));
			result = m_BoneAllocator.Allocate((uint)boneCount);
		}
		return result;
	}

	private unsafe void ResizeBoneBuffer()
	{
		int num = ((m_BoneBuffer != null) ? m_BoneBuffer.count : 0);
		int size = (int)m_BoneAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
			computeBuffer.name = "Bone buffer";
			Shader.SetGlobalBuffer("boneBuffer", computeBuffer);
			if (m_BoneHistoryBuffer == null)
			{
				Shader.SetGlobalBuffer("boneHistoryBuffer", computeBuffer);
			}
			if (m_BoneBuffer != null)
			{
				m_BoneBuffer.Release();
			}
			if (m_LocalTRSBlendPoseBuffer != null)
			{
				m_LocalTRSBlendPoseBuffer.Release();
			}
			if (m_LocalTRSBoneBuffer != null)
			{
				m_LocalTRSBoneBuffer.Release();
			}
			BoneElement[] array = new BoneElement[computeBuffer.count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new BoneElement
				{
					m_Matrix = float4x4.identity
				};
			}
			computeBuffer.SetData(array);
			m_BoneBuffer = computeBuffer;
			m_LocalTRSBlendPoseBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
			m_LocalTRSBoneBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
			m_LocalTRSBlendPoseBuffer.name = "LocalTRSBlendPoseBuffer";
			m_LocalTRSBoneBuffer.name = "LocalTRSBoneBuffer";
		}
	}

	private unsafe void ResizeBoneHistoryBuffer()
	{
		int num = ((m_BoneHistoryBuffer != null) ? m_BoneHistoryBuffer.count : 0);
		int size = (int)m_BoneAllocator.Size;
		if (num == size)
		{
			return;
		}
		if (size == 0)
		{
			if (m_BoneHistoryBuffer != null)
			{
				if (m_BoneHistoryBuffer != null)
				{
					m_BoneHistoryBuffer.Release();
				}
				m_BoneHistoryBuffer = null;
			}
			if (m_BoneBuffer != null)
			{
				Shader.SetGlobalBuffer("boneHistoryBuffer", m_BoneBuffer);
			}
			return;
		}
		ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
		computeBuffer.name = "Bone history buffer";
		Shader.SetGlobalBuffer("boneHistoryBuffer", computeBuffer);
		if (m_BoneHistoryBuffer != null)
		{
			m_BoneHistoryBuffer.Release();
		}
		BoneElement[] array = new BoneElement[computeBuffer.count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new BoneElement
			{
				m_Matrix = float4x4.identity
			};
		}
		computeBuffer.SetData(array);
		m_BoneHistoryBuffer = computeBuffer;
	}

	private AnimatedPropID GetPropID(string propName, bool insert = true)
	{
		int value = -1;
		if (!string.IsNullOrEmpty(propName) && !m_PropIds.TryGetValue(propName, out value))
		{
			value = m_PropIds.Count;
			if (insert)
			{
				m_PropIds.Add(propName, value);
			}
		}
		return new AnimatedPropID(value);
	}

	private float FindTargetRotation(Colossal.Animations.AnimationClip animation, NativeArray<Animation.Element> elements)
	{
		int num = animation.m_Animation.shapeIndices.Length;
		int num2 = animation.m_Animation.boneIndices.Length;
		float num3 = 0f;
		for (int i = 0; i < num2; i++)
		{
			Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(elements.ElementAt(i * num), animation.m_Animation.positionMin, animation.m_Animation.positionRange);
			float y = MathUtils.RotationAngle(quaternion.identity, elementRaw.rotation);
			num3 = math.max(num3, y);
		}
		return num3;
	}

	private float FindTargetValue(Colossal.Animations.AnimationClip animation, Game.Prefabs.AnimationClip animationClip, NativeArray<Animation.Element> elements)
	{
		if (animationClip.m_Activity == ActivityType.Driving)
		{
			Game.Prefabs.AnimationType type = animationClip.m_Type;
			if ((uint)(type - 6) <= 3u)
			{
				return FindTargetRotation(animation, elements);
			}
		}
		return 0f;
	}
}
