using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Effects;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class EffectTransformSystem : GameSystemBase
{
	[BurstCompile]
	private struct EffectTransformJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<EffectData> m_EffectDatas;

		[ReadOnly]
		public ComponentLookup<RandomTransformData> m_RandomTransformDatas;

		[ReadOnly]
		public ComponentLookup<EffectColorData> m_EffectColorDatas;

		[ReadOnly]
		public ComponentLookup<Game.Events.Event> m_EventData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public BufferLookup<BoneHistory> m_BoneHistories;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<Effect> m_Effects;

		[ReadOnly]
		public BufferLookup<EffectAnimation> m_EffectAnimations;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[NativeDisableParallelForRestriction]
		public NativeList<EnabledEffectData> m_EnabledData;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		public void Execute(int index)
		{
			ref EnabledEffectData reference = ref m_EnabledData.ElementAt(index);
			if ((reference.m_Flags & EnabledEffectFlags.IsEnabled) == 0 || (reference.m_Flags & (EnabledEffectFlags.EnabledUpdated | EnabledEffectFlags.DynamicTransform | EnabledEffectFlags.OwnerUpdated)) == 0)
			{
				return;
			}
			PrefabRef prefabRef = m_Prefabs[reference.m_Owner];
			EffectData effectData = m_EffectDatas[reference.m_Prefab];
			Effect prefabEffect;
			if ((reference.m_Flags & EnabledEffectFlags.EditorContainer) != 0)
			{
				Game.Tools.EditorContainer editorContainer = m_EditorContainerData[reference.m_Owner];
				prefabEffect = new Effect
				{
					m_Effect = editorContainer.m_Prefab,
					m_Scale = editorContainer.m_Scale,
					m_Intensity = editorContainer.m_Intensity,
					m_AnimationIndex = editorContainer.m_GroupIndex,
					m_Rotation = quaternion.identity,
					m_BoneIndex = -1
				};
			}
			else
			{
				prefabEffect = m_Effects[prefabRef.m_Prefab][reference.m_EffectIndex];
			}
			Entity entity = reference.m_Owner;
			if ((reference.m_Flags & EnabledEffectFlags.TempOwner) != 0 && m_TempData.TryGetComponent(entity, out var componentData) && componentData.m_Original != Entity.Null)
			{
				entity = componentData.m_Original;
			}
			Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)(entity.Index ^ reference.m_EffectIndex));
			if ((reference.m_Flags & EnabledEffectFlags.RandomTransform) != 0)
			{
				RandomTransformData randomTransformData = m_RandomTransformDatas[prefabEffect.m_Effect];
				float3 xyz = random.NextFloat3(randomTransformData.m_AngleRange.min, randomTransformData.m_AngleRange.max);
				prefabEffect.m_Rotation = math.mul(prefabEffect.m_Rotation, quaternion.Euler(xyz));
				prefabEffect.m_Position += random.NextFloat3(randomTransformData.m_PositionRange.min, randomTransformData.m_PositionRange.max);
			}
			bool num = effectData.m_OwnerCulling || IsNearCamera(reference.m_Owner) || m_EventData.HasComponent(reference.m_Owner);
			bool flag = (reference.m_Flags & EnabledEffectFlags.IsLight) != 0;
			reference.m_Scale = prefabEffect.m_Scale;
			reference.m_Intensity = prefabEffect.m_Intensity;
			if (flag)
			{
				EffectColorData effectColorData = m_EffectColorDatas[prefabEffect.m_Effect];
				UnityEngine.Color color = effectColorData.m_Color;
				if (effectColorData.m_Source != EffectColorSource.Effect && prefabEffect.m_ParentMesh >= 0 && m_MeshColors.TryGetBuffer(reference.m_Owner, out var bufferData) && bufferData.Length > prefabEffect.m_ParentMesh)
				{
					color = bufferData[prefabEffect.m_ParentMesh].m_ColorSet[(int)(effectColorData.m_Source - 1)];
					color *= effectColorData.m_Color.a;
				}
				if (math.any(effectColorData.m_VaritationRanges != 0f))
				{
					Randomize(ref color, ref random, 1f - effectColorData.m_VaritationRanges, 1f + effectColorData.m_VaritationRanges);
				}
				reference.m_Scale = new float3(color.r, color.g, color.b);
			}
			Relative componentData3;
			Curve componentData5;
			Game.Objects.Transform componentData6;
			Node componentData7;
			if (num && m_InterpolatedTransformData.TryGetComponent(reference.m_Owner, out var componentData2))
			{
				Game.Objects.Transform effectTransform = GetEffectTransform(prefabEffect, reference.m_Owner);
				effectTransform = ObjectUtils.LocalToWorld(componentData2.ToTransform(), effectTransform);
				if ((reference.m_Flags & EnabledEffectFlags.OwnerCollapsed) != 0)
				{
					effectTransform.m_Position.y = math.max(effectTransform.m_Position.y, m_TransformData[reference.m_Owner].m_Position.y);
				}
				reference.m_Position = effectTransform.m_Position;
				reference.m_Rotation = effectTransform.m_Rotation;
				if (flag && ((effectData.m_Flags.m_RequiredFlags | effectData.m_Flags.m_ForbiddenFlags) & (EffectConditionFlags.MainLights | EffectConditionFlags.ExtraLights | EffectConditionFlags.WarningLights)) != EffectConditionFlags.None)
				{
					bool test = TestFlags(effectData.m_Flags.m_RequiredFlags, effectData.m_Flags.m_ForbiddenFlags, componentData2.m_Flags);
					reference.m_Intensity = math.select(0f, reference.m_Intensity, test);
				}
			}
			else if (m_RelativeData.TryGetComponent(reference.m_Owner, out componentData3))
			{
				Owner owner = m_OwnerData[reference.m_Owner];
				Game.Objects.Transform relativeTransform = GetRelativeTransform(componentData3, owner.m_Owner);
				Game.Objects.Transform transform = new Game.Objects.Transform(prefabEffect.m_Position, prefabEffect.m_Rotation);
				Game.Objects.Transform componentData4;
				Game.Objects.Transform parentTransform = (m_InterpolatedTransformData.TryGetComponent(owner.m_Owner, out componentData2) ? ObjectUtils.LocalToWorld(componentData2.ToTransform(), relativeTransform) : ((!m_TransformData.TryGetComponent(owner.m_Owner, out componentData4)) ? relativeTransform : ObjectUtils.LocalToWorld(componentData4, relativeTransform)));
				transform = ObjectUtils.LocalToWorld(parentTransform, transform);
				reference.m_Position = transform.m_Position;
				reference.m_Rotation = transform.m_Rotation;
			}
			else if (m_CurveData.TryGetComponent(reference.m_Owner, out componentData5))
			{
				reference.m_Position = MathUtils.Position(componentData5.m_Bezier, 0.5f);
				reference.m_Rotation = quaternion.identity;
			}
			else if (m_TransformData.TryGetComponent(reference.m_Owner, out componentData6))
			{
				Game.Objects.Transform effectTransform2 = GetEffectTransform(prefabEffect, reference.m_Owner);
				effectTransform2 = ObjectUtils.LocalToWorld(componentData6, effectTransform2);
				if ((reference.m_Flags & EnabledEffectFlags.OwnerCollapsed) != 0)
				{
					effectTransform2.m_Position.y = componentData6.m_Position.y;
				}
				reference.m_Position = effectTransform2.m_Position;
				reference.m_Rotation = effectTransform2.m_Rotation;
			}
			else if (m_NodeData.TryGetComponent(reference.m_Owner, out componentData7))
			{
				reference.m_Position = componentData7.m_Position;
				reference.m_Rotation = componentData7.m_Rotation;
			}
			if (prefabEffect.m_AnimationIndex >= 0 && reference.m_Intensity != 0f && m_EffectAnimations.TryGetBuffer(prefabRef.m_Prefab, out var bufferData2) && bufferData2.Length > prefabEffect.m_AnimationIndex)
			{
				Unity.Mathematics.Random random2 = m_PseudoRandomSeedData[reference.m_Owner].GetRandom(PseudoRandomSeed.kLightState);
				EffectAnimation effectAnimation = bufferData2[prefabEffect.m_AnimationIndex];
				float num2 = (float)((m_FrameIndex + random2.NextUInt(effectAnimation.m_DurationFrames)) % effectAnimation.m_DurationFrames) + m_FrameTime;
				reference.m_Intensity *= effectAnimation.m_AnimationCurve.Evaluate(num2 / (float)effectAnimation.m_DurationFrames);
			}
		}

		private void Randomize(ref UnityEngine.Color color, ref Unity.Mathematics.Random random, float3 min, float3 max)
		{
			float3 @float = default(float3);
			UnityEngine.Color.RGBToHSV(color, out @float.x, out @float.y, out @float.z);
			float3 float2 = random.NextFloat3(min, max);
			@float.x = math.frac(@float.x + float2.x);
			@float.yz *= float2.yz;
			@float.y = math.saturate(@float.y);
			@float.z = math.max(0f, @float.z);
			color = UnityEngine.Color.HSVToRGB(@float.x, @float.y, @float.z);
		}

		private bool IsNearCamera(Entity entity)
		{
			if (m_CullingInfoData.TryGetComponent(entity, out var componentData) && componentData.m_CullingIndex != 0)
			{
				return (m_CullingData[componentData.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0;
			}
			return false;
		}

		private Game.Objects.Transform GetEffectTransform(Effect prefabEffect, Entity owner)
		{
			if (prefabEffect.m_BoneIndex.x >= 0)
			{
				DynamicBuffer<BoneHistory> dynamicBuffer = m_BoneHistories[owner];
				if (dynamicBuffer.Length >= prefabEffect.m_BoneIndex.x)
				{
					float4x4 matrix = dynamicBuffer[prefabEffect.m_BoneIndex.x].m_Matrix;
					float3 @float = math.transform(matrix, prefabEffect.m_Position);
					float3 forward = math.rotate(matrix, math.forward(prefabEffect.m_Rotation));
					float3 up = math.rotate(matrix, math.mul(prefabEffect.m_Rotation, math.up()));
					quaternion quaternion = quaternion.LookRotation(forward, up);
					if (prefabEffect.m_BoneIndex.y >= 0)
					{
						PrefabRef prefabRef = m_Prefabs[owner];
						SubMesh subMesh = m_SubMeshes[prefabRef.m_Prefab][prefabEffect.m_BoneIndex.y];
						@float = subMesh.m_Position + math.rotate(subMesh.m_Rotation, @float);
						quaternion = math.mul(subMesh.m_Rotation, quaternion);
					}
					return new Game.Objects.Transform(@float, quaternion);
				}
			}
			return new Game.Objects.Transform(prefabEffect.m_Position, prefabEffect.m_Rotation);
		}

		private Game.Objects.Transform GetRelativeTransform(Relative relative, Entity owner)
		{
			if (relative.m_BoneIndex.y >= 0)
			{
				DynamicBuffer<BoneHistory> dynamicBuffer = m_BoneHistories[owner];
				if (dynamicBuffer.Length > relative.m_BoneIndex.y)
				{
					float4x4 matrix = dynamicBuffer[relative.m_BoneIndex.y].m_Matrix;
					float3 @float = math.transform(matrix, relative.m_Position);
					float3 forward = math.rotate(matrix, math.forward(relative.m_Rotation));
					float3 up = math.rotate(matrix, math.mul(relative.m_Rotation, math.up()));
					quaternion quaternion = quaternion.LookRotation(forward, up);
					if (relative.m_BoneIndex.z >= 0)
					{
						PrefabRef prefabRef = m_Prefabs[owner];
						SubMesh subMesh = m_SubMeshes[prefabRef.m_Prefab][relative.m_BoneIndex.z];
						@float = subMesh.m_Position + math.rotate(subMesh.m_Rotation, @float);
						quaternion = math.mul(subMesh.m_Rotation, quaternion);
					}
					return new Game.Objects.Transform(@float, quaternion);
				}
			}
			return new Game.Objects.Transform(relative.m_Position, relative.m_Rotation);
		}

		private bool TestFlags(EffectConditionFlags requiredFlags, EffectConditionFlags forbiddenFlags, TransformFlags transformFlags)
		{
			int4 @int = new int4(65536, 131072, 262144, 524288);
			int4 int2 = new int4(1, 2, 1024, 4096);
			bool4 @bool = (@int & (int)requiredFlags) != 0;
			bool4 bool2 = (@int & (int)forbiddenFlags) != 0;
			bool4 bool3 = (int2 & (int)transformFlags) != 0;
			return math.any(@bool & bool3) & !math.any(bool2 & bool3);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RandomTransformData> __Game_Prefabs_RandomTransformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectColorData> __Game_Prefabs_EffectColorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Events.Event> __Game_Events_Event_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<BoneHistory> __Game_Rendering_BoneHistory_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<EffectAnimation> __Game_Prefabs_EffectAnimation_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_RandomTransformData_RO_ComponentLookup = state.GetComponentLookup<RandomTransformData>(isReadOnly: true);
			__Game_Prefabs_EffectColorData_RO_ComponentLookup = state.GetComponentLookup<EffectColorData>(isReadOnly: true);
			__Game_Events_Event_RO_ComponentLookup = state.GetComponentLookup<Game.Events.Event>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Rendering_BoneHistory_RO_BufferLookup = state.GetBufferLookup<BoneHistory>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
			__Game_Prefabs_EffectAnimation_RO_BufferLookup = state.GetBufferLookup<EffectAnimation>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
		}
	}

	private RenderingSystem m_RenderingSystem;

	private EffectControlSystem m_EffectControlSystem;

	private PreCullingSystem m_PreCullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		EffectTransformJob jobData = new EffectTransformJob
		{
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EffectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomTransformDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RandomTransformData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EffectColorDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectColorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EventData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Event_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BoneHistories = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_BoneHistory_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
			m_Effects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
			m_EffectAnimations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_EffectAnimation_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_EnabledData = m_EffectControlSystem.GetEnabledData(readOnly: false, out dependencies),
			m_CullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies2),
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime
		};
		JobHandle jobHandle = jobData.Schedule(jobData.m_EnabledData, 16, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		m_EffectControlSystem.AddEnabledDataWriter(jobHandle);
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
	public EffectTransformSystem()
	{
	}
}
