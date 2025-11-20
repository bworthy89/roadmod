using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Rendering;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class ProceduralUploadSystem : GameSystemBase
{
	[CompilerGenerated]
	public class Prepare : GameSystemBase
	{
		private struct TypeHandle
		{
			[ReadOnly]
			public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Emissive> __Game_Rendering_Emissive_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
				__Game_Rendering_Emissive_RO_BufferLookup = state.GetBufferLookup<Emissive>(isReadOnly: true);
			}
		}

		private ProceduralUploadSystem m_ProceduralUploadSystem;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ProceduralUploadSystem = base.World.GetOrCreateSystemManaged<ProceduralUploadSystem>();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			m_ProceduralUploadSystem.m_UploadData = new NativeAccumulator<UploadData>(2, Allocator.TempJob);
			JobHandle dependencies;
			ProceduralPrepareJob jobData = new ProceduralPrepareJob
			{
				m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
				m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RO_BufferLookup, ref base.CheckedStateRef),
				m_MotionBlurEnabled = m_ProceduralUploadSystem.m_ProceduralSkeletonSystem.isMotionBlurEnabled,
				m_ForceHistoryUpdate = m_ProceduralUploadSystem.m_ProceduralSkeletonSystem.forceHistoryUpdate,
				m_CullingData = m_ProceduralUploadSystem.m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies),
				m_UploadData = m_ProceduralUploadSystem.m_UploadData.AsParallelWriter()
			};
			JobHandle jobHandle = jobData.Schedule(jobData.m_CullingData, 16, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_ProceduralUploadSystem.m_PreCullingSystem.AddCullingDataReader(jobHandle);
			m_ProceduralUploadSystem.m_PrepareDeps = jobHandle;
			base.Dependency = jobHandle;
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
		public Prepare()
		{
		}
	}

	internal struct UploadData : IAccumulable<UploadData>
	{
		public int m_OpCount;

		public uint m_DataSize;

		public uint m_MaxOpSize;

		public void Accumulate(UploadData other)
		{
			m_OpCount += other.m_OpCount;
			m_DataSize += other.m_DataSize;
			m_MaxOpSize = math.max(m_MaxOpSize, other.m_MaxOpSize);
		}
	}

	[BurstCompile]
	private struct ProceduralPrepareJob : IJobParallelForDefer
	{
		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Emissive> m_Emissives;

		[ReadOnly]
		public bool m_MotionBlurEnabled;

		[ReadOnly]
		public bool m_ForceHistoryUpdate;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public NativeAccumulator<UploadData>.ParallelWriter m_UploadData;

		public unsafe void Execute(int index)
		{
			PreCullingData preCullingData = m_CullingData[index];
			if ((preCullingData.m_Flags & (PreCullingFlags.Skeleton | PreCullingFlags.Emissive)) == 0 || (preCullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
			{
				return;
			}
			if ((preCullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
			{
				DynamicBuffer<Skeleton> dynamicBuffer = m_Skeletons[preCullingData.m_Entity];
				UploadData value = default(UploadData);
				if (m_MotionBlurEnabled)
				{
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Skeleton skeleton = dynamicBuffer[i];
						if ((skeleton.m_CurrentUpdated || skeleton.m_HistoryUpdated || m_ForceHistoryUpdate) && !skeleton.m_BufferAllocation.Empty)
						{
							uint num = skeleton.m_BufferAllocation.Length * (uint)sizeof(float4x4);
							if (skeleton.m_CurrentUpdated)
							{
								value.Accumulate(new UploadData
								{
									m_OpCount = 1,
									m_DataSize = num,
									m_MaxOpSize = num
								});
							}
							if (skeleton.m_HistoryUpdated || m_ForceHistoryUpdate)
							{
								value.Accumulate(new UploadData
								{
									m_OpCount = 1,
									m_DataSize = num,
									m_MaxOpSize = num
								});
							}
						}
					}
				}
				else
				{
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Skeleton skeleton2 = dynamicBuffer[j];
						if (skeleton2.m_CurrentUpdated && !skeleton2.m_BufferAllocation.Empty)
						{
							uint num2 = skeleton2.m_BufferAllocation.Length * (uint)sizeof(float4x4);
							value.Accumulate(new UploadData
							{
								m_OpCount = 1,
								m_DataSize = num2,
								m_MaxOpSize = num2
							});
						}
					}
				}
				m_UploadData.Accumulate(0, value);
			}
			if ((preCullingData.m_Flags & PreCullingFlags.Emissive) == 0)
			{
				return;
			}
			DynamicBuffer<Emissive> dynamicBuffer2 = m_Emissives[preCullingData.m_Entity];
			UploadData value2 = default(UploadData);
			for (int k = 0; k < dynamicBuffer2.Length; k++)
			{
				Emissive emissive = dynamicBuffer2[k];
				if (emissive.m_Updated && !emissive.m_BufferAllocation.Empty)
				{
					uint num3 = emissive.m_BufferAllocation.Length * (uint)sizeof(float4);
					value2.Accumulate(new UploadData
					{
						m_OpCount = 1,
						m_DataSize = num3,
						m_MaxOpSize = num3
					});
				}
			}
			m_UploadData.Accumulate(1, value2);
		}
	}

	[BurstCompile]
	private struct ProceduralUploadJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

		[ReadOnly]
		public BufferLookup<LightState> m_Lights;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[ReadOnly]
		public BufferLookup<ProceduralLight> m_ProceduralLights;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Skeleton> m_Skeletons;

		[NativeDisableParallelForRestriction]
		public BufferLookup<BoneHistory> m_BoneHistories;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Emissive> m_Emissives;

		[ReadOnly]
		public ThreadedSparseUploader m_BoneUploader;

		[ReadOnly]
		public ThreadedSparseUploader m_LightUploader;

		[ReadOnly]
		public int m_HistoryByteOffset;

		[ReadOnly]
		public bool m_MotionBlurEnabled;

		[ReadOnly]
		public bool m_ForceHistoryUpdate;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public unsafe void Execute(int index)
		{
			PreCullingData preCullingData = m_CullingData[index];
			if ((preCullingData.m_Flags & (PreCullingFlags.Skeleton | PreCullingFlags.Emissive)) == 0 || (preCullingData.m_Flags & PreCullingFlags.NearCamera) == 0)
			{
				return;
			}
			DynamicBuffer<Skeleton> dynamicBuffer = default(DynamicBuffer<Skeleton>);
			DynamicBuffer<Emissive> dynamicBuffer2 = default(DynamicBuffer<Emissive>);
			bool flag = false;
			bool flag2 = false;
			if ((preCullingData.m_Flags & PreCullingFlags.Skeleton) != 0)
			{
				dynamicBuffer = m_Skeletons[preCullingData.m_Entity];
				if (m_MotionBlurEnabled)
				{
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						ref Skeleton reference = ref dynamicBuffer.ElementAt(i);
						if ((reference.m_CurrentUpdated || reference.m_HistoryUpdated || m_ForceHistoryUpdate) && !reference.m_BufferAllocation.Empty)
						{
							flag = true;
						}
					}
				}
				else
				{
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						ref Skeleton reference2 = ref dynamicBuffer.ElementAt(j);
						if (reference2.m_CurrentUpdated && !reference2.m_BufferAllocation.Empty)
						{
							flag = true;
						}
					}
				}
			}
			if ((preCullingData.m_Flags & PreCullingFlags.Emissive) != 0)
			{
				dynamicBuffer2 = m_Emissives[preCullingData.m_Entity];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					ref Emissive reference3 = ref dynamicBuffer2.ElementAt(k);
					if (reference3.m_Updated && !reference3.m_BufferAllocation.Empty)
					{
						flag2 = true;
					}
				}
			}
			if (!flag && !flag2)
			{
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[preCullingData.m_Entity];
			DynamicBuffer<SubMesh> dynamicBuffer3 = m_SubMeshes[prefabRef.m_Prefab];
			if (flag)
			{
				NativeList<float4x4> nativeList = default(NativeList<float4x4>);
				DynamicBuffer<Bone> bones = m_Bones[preCullingData.m_Entity];
				DynamicBuffer<BoneHistory> dynamicBuffer4 = m_BoneHistories[preCullingData.m_Entity];
				if (m_MotionBlurEnabled)
				{
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						ref Skeleton reference4 = ref dynamicBuffer.ElementAt(l);
						if ((!reference4.m_CurrentUpdated && !reference4.m_HistoryUpdated && !m_ForceHistoryUpdate) || reference4.m_BufferAllocation.Empty)
						{
							continue;
						}
						SubMesh subMesh = dynamicBuffer3[l];
						DynamicBuffer<ProceduralBone> proceduralBones = m_ProceduralBones[subMesh.m_SubMesh];
						if (!nativeList.IsCreated)
						{
							nativeList = new NativeList<float4x4>(proceduralBones.Length * 3, Allocator.Temp);
						}
						nativeList.ResizeUninitialized(proceduralBones.Length * 3);
						ProceduralSkeletonSystem.GetSkinMatrices(reference4, in proceduralBones, in bones, nativeList);
						if (m_ForceHistoryUpdate)
						{
							for (int m = 0; m < proceduralBones.Length; m++)
							{
								ProceduralBone proceduralBone = proceduralBones[m];
								int index2 = reference4.m_BoneOffset + m;
								float4x4 float4x = nativeList[proceduralBones.Length + proceduralBone.m_BindIndex];
								nativeList[proceduralBones.Length * 2 + proceduralBone.m_BindIndex] = float4x;
								dynamicBuffer4[index2] = new BoneHistory
								{
									m_Matrix = float4x
								};
							}
						}
						else
						{
							for (int n = 0; n < proceduralBones.Length; n++)
							{
								ProceduralBone proceduralBone2 = proceduralBones[n];
								int index3 = reference4.m_BoneOffset + n;
								float4x4 matrix = dynamicBuffer4[index3].m_Matrix;
								float4x4 matrix2 = nativeList[proceduralBones.Length + proceduralBone2.m_BindIndex];
								nativeList[proceduralBones.Length * 2 + proceduralBone2.m_BindIndex] = matrix;
								dynamicBuffer4[index3] = new BoneHistory
								{
									m_Matrix = matrix2
								};
							}
						}
						int num = proceduralBones.Length * sizeof(float4x4);
						int num2 = (int)reference4.m_BufferAllocation.Begin * sizeof(float4x4);
						if (reference4.m_CurrentUpdated)
						{
							m_BoneUploader.AddUpload((byte*)nativeList.GetUnsafePtr() + num, num, num2, 1);
						}
						if (reference4.m_HistoryUpdated || m_ForceHistoryUpdate)
						{
							m_BoneUploader.AddUpload((byte*)nativeList.GetUnsafePtr() + num * 2, num, num2 + m_HistoryByteOffset, 1);
						}
						reference4.m_HistoryUpdated = reference4.m_CurrentUpdated;
						reference4.m_CurrentUpdated = false;
					}
				}
				else
				{
					for (int num3 = 0; num3 < dynamicBuffer.Length; num3++)
					{
						ref Skeleton reference5 = ref dynamicBuffer.ElementAt(num3);
						if (!reference5.m_CurrentUpdated || reference5.m_BufferAllocation.Empty)
						{
							continue;
						}
						reference5.m_CurrentUpdated = false;
						SubMesh subMesh2 = dynamicBuffer3[num3];
						DynamicBuffer<ProceduralBone> proceduralBones2 = m_ProceduralBones[subMesh2.m_SubMesh];
						if (!nativeList.IsCreated)
						{
							nativeList = new NativeList<float4x4>(proceduralBones2.Length * 2, Allocator.Temp);
						}
						nativeList.ResizeUninitialized(proceduralBones2.Length * 2);
						ProceduralSkeletonSystem.GetSkinMatrices(reference5, in proceduralBones2, in bones, nativeList);
						m_BoneUploader.AddUpload((byte*)nativeList.GetUnsafePtr() + proceduralBones2.Length * sizeof(float4x4), proceduralBones2.Length * sizeof(float4x4), (int)reference5.m_BufferAllocation.Begin * sizeof(float4x4), 1);
						if (reference5.m_RequireHistory)
						{
							for (int num4 = 0; num4 < proceduralBones2.Length; num4++)
							{
								ProceduralBone proceduralBone3 = proceduralBones2[num4];
								float4x4 matrix3 = nativeList[proceduralBones2.Length + proceduralBone3.m_BindIndex];
								dynamicBuffer4[reference5.m_BoneOffset + num4] = new BoneHistory
								{
									m_Matrix = matrix3
								};
							}
						}
					}
				}
				if (nativeList.IsCreated)
				{
					nativeList.Dispose();
				}
			}
			if (!flag2)
			{
				return;
			}
			NativeList<float4> nativeList2 = default(NativeList<float4>);
			DynamicBuffer<LightState> lights = m_Lights[preCullingData.m_Entity];
			for (int num5 = 0; num5 < dynamicBuffer2.Length; num5++)
			{
				ref Emissive reference6 = ref dynamicBuffer2.ElementAt(num5);
				if (reference6.m_Updated && !reference6.m_BufferAllocation.Empty)
				{
					reference6.m_Updated = false;
					SubMesh subMesh3 = dynamicBuffer3[num5];
					DynamicBuffer<ProceduralLight> proceduralLights = m_ProceduralLights[subMesh3.m_SubMesh];
					if (!nativeList2.IsCreated)
					{
						nativeList2 = new NativeList<float4>(proceduralLights.Length + 1, Allocator.Temp);
					}
					nativeList2.ResizeUninitialized(proceduralLights.Length + 1);
					ProceduralEmissiveSystem.GetGpuLights(reference6, in proceduralLights, in lights, nativeList2);
					m_LightUploader.AddUpload(nativeList2.GetUnsafePtr(), nativeList2.Length * sizeof(float4), (int)reference6.m_BufferAllocation.Begin * sizeof(float4), 1);
				}
			}
			if (nativeList2.IsCreated)
			{
				nativeList2.Dispose();
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Bone> __Game_Rendering_Bone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LightState> __Game_Rendering_LightState_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralLight> __Game_Prefabs_ProceduralLight_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RW_BufferLookup;

		public BufferLookup<BoneHistory> __Game_Rendering_BoneHistory_RW_BufferLookup;

		public BufferLookup<Emissive> __Game_Rendering_Emissive_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Rendering_Bone_RO_BufferLookup = state.GetBufferLookup<Bone>(isReadOnly: true);
			__Game_Rendering_LightState_RO_BufferLookup = state.GetBufferLookup<LightState>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Prefabs_ProceduralLight_RO_BufferLookup = state.GetBufferLookup<ProceduralLight>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Rendering_Skeleton_RW_BufferLookup = state.GetBufferLookup<Skeleton>();
			__Game_Rendering_BoneHistory_RW_BufferLookup = state.GetBufferLookup<BoneHistory>();
			__Game_Rendering_Emissive_RW_BufferLookup = state.GetBufferLookup<Emissive>();
		}
	}

	private ProceduralSkeletonSystem m_ProceduralSkeletonSystem;

	private ProceduralEmissiveSystem m_ProceduralEmissiveSystem;

	private PreCullingSystem m_PreCullingSystem;

	private PrefabSystem m_PrefabSystem;

	private RenderPrefabBase m_OverridePrefab;

	private NativeAccumulator<UploadData> m_UploadData;

	private JobHandle m_PrepareDeps;

	private Entity m_OverrideEntity;

	private LightState m_OverrideLightState;

	private int m_OverrideSingleLightIndex;

	private int m_OverrideMultiLightIndex;

	private float m_OverrideTime;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ProceduralSkeletonSystem = base.World.GetOrCreateSystemManaged<ProceduralSkeletonSystem>();
		m_ProceduralEmissiveSystem = base.World.GetOrCreateSystemManaged<ProceduralEmissiveSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
	}

	public void SetOverride(Entity entity, RenderPrefabBase prefab, int singleLightIndex, int multiLightIndex)
	{
		if (entity != m_OverrideEntity || singleLightIndex != m_OverrideSingleLightIndex || multiLightIndex != m_OverrideMultiLightIndex)
		{
			m_OverrideLightState.m_Intensity = -1f;
			m_OverrideTime = 0f;
		}
		m_OverrideEntity = entity;
		m_OverridePrefab = prefab;
		m_OverrideSingleLightIndex = singleLightIndex;
		m_OverrideMultiLightIndex = multiLightIndex;
	}

	private unsafe void UpdateOverride(ref UploadData emissiveData)
	{
		if (!base.EntityManager.TryGetBuffer(m_OverrideEntity, isReadOnly: false, out DynamicBuffer<Emissive> buffer) || !base.EntityManager.TryGetBuffer(m_OverrideEntity, isReadOnly: false, out DynamicBuffer<LightState> buffer2) || !base.EntityManager.TryGetComponent<PrefabRef>(m_OverrideEntity, out var component) || !base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> buffer3) || !m_OverridePrefab.TryGet<EmissiveProperties>(out var component2) || !m_PrefabSystem.TryGetEntity(m_OverridePrefab, out var entity))
		{
			return;
		}
		int num = -1;
		if (m_OverrideMultiLightIndex >= 0)
		{
			num = m_OverrideMultiLightIndex;
		}
		else
		{
			if (m_OverrideSingleLightIndex < 0)
			{
				return;
			}
			num = m_OverrideSingleLightIndex;
			if (component2.hasMultiLights)
			{
				num += component2.m_MultiLights.Count;
			}
		}
		float deltaTime = UnityEngine.Time.deltaTime;
		for (int i = 0; i < buffer.Length; i++)
		{
			ref Emissive reference = ref buffer.ElementAt(i);
			if (reference.m_BufferAllocation.Empty)
			{
				continue;
			}
			SubMesh subMesh = buffer3[i];
			if (!(subMesh.m_SubMesh != entity) && base.EntityManager.TryGetBuffer(subMesh.m_SubMesh, isReadOnly: true, out DynamicBuffer<ProceduralLight> buffer4) && num < buffer4.Length)
			{
				if (!reference.m_Updated)
				{
					uint num2 = reference.m_BufferAllocation.Length * (uint)sizeof(float4);
					emissiveData.Accumulate(new UploadData
					{
						m_OpCount = 1,
						m_DataSize = num2,
						m_MaxOpSize = num2
					});
				}
				ProceduralLight proceduralLight = buffer4[num];
				ref LightState reference2 = ref buffer2.ElementAt(reference.m_LightOffset + num);
				if (m_OverrideLightState.m_Intensity < 0f)
				{
					m_OverrideLightState = reference2;
				}
				float2 target = new float2(1f, 0f);
				if (proceduralLight.m_AnimationIndex >= 0 && base.EntityManager.TryGetBuffer(subMesh.m_SubMesh, isReadOnly: true, out DynamicBuffer<LightAnimation> buffer5))
				{
					LightAnimation lightAnimation = buffer5[proceduralLight.m_AnimationIndex];
					m_OverrideTime += deltaTime * 60f;
					m_OverrideTime %= lightAnimation.m_DurationFrames;
					target.x *= lightAnimation.m_AnimationCurve.Evaluate(m_OverrideTime / (float)lightAnimation.m_DurationFrames);
				}
				ObjectInterpolateSystem.AnimateLight(proceduralLight, ref reference, ref m_OverrideLightState, deltaTime, target, instantReset: false);
				reference2 = m_OverrideLightState;
				reference.m_Updated = true;
			}
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_PrepareDeps.Complete();
		UploadData result = m_UploadData.GetResult();
		UploadData emissiveData = m_UploadData.GetResult(1);
		m_UploadData.Dispose();
		if (m_OverrideEntity != Entity.Null)
		{
			UpdateOverride(ref emissiveData);
		}
		int historyByteOffset;
		JobHandle dependencies;
		ProceduralUploadJob jobData = new ProceduralUploadJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
			m_Lights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_LightState_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralLights = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralLight_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RW_BufferLookup, ref base.CheckedStateRef),
			m_BoneHistories = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_BoneHistory_RW_BufferLookup, ref base.CheckedStateRef),
			m_Emissives = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Emissive_RW_BufferLookup, ref base.CheckedStateRef),
			m_BoneUploader = m_ProceduralSkeletonSystem.BeginUpload(result.m_OpCount, result.m_DataSize, result.m_MaxOpSize, out historyByteOffset),
			m_LightUploader = m_ProceduralEmissiveSystem.BeginUpload(emissiveData.m_OpCount, emissiveData.m_DataSize, emissiveData.m_MaxOpSize),
			m_HistoryByteOffset = historyByteOffset,
			m_MotionBlurEnabled = m_ProceduralSkeletonSystem.isMotionBlurEnabled,
			m_ForceHistoryUpdate = m_ProceduralSkeletonSystem.forceHistoryUpdate,
			m_CullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies)
		};
		JobHandle jobHandle = jobData.Schedule(jobData.m_CullingData, 16, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_ProceduralSkeletonSystem.AddUploadWriter(jobHandle);
		m_ProceduralEmissiveSystem.AddUploadWriter(jobHandle);
		m_PreCullingSystem.AddCullingDataReader(jobHandle);
		base.Dependency = jobHandle;
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
	public ProceduralUploadSystem()
	{
	}
}
