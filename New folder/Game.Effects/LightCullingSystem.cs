using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Prefabs;
using Game.Prefabs.Effects;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Effects;

[CompilerGenerated]
public class LightCullingSystem : GameSystemBase
{
	private struct DefaultLightParams
	{
		public float shapeWidth;

		public float shapeHeight;

		public float spotIESCutoffPercent01;

		public float shapeRadius;
	}

	private struct LightEffectCullData
	{
		public float m_Range;

		public float m_SpotAngle;

		public float m_InvDistanceFactor;

		public int m_LightEffectPrefabDataIndex;

		public Game.Rendering.LightType m_lightType;
	}

	[BurstCompile]
	private struct LightCullingJob : IJobParallelForDefer
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<float4> m_Planes;

		[ReadOnly]
		public NativeParallelHashMap<Entity, LightEffectCullData> m_LightEffectCullData;

		[ReadOnly]
		public NativeList<EnabledEffectData> m_EnabledEffectData;

		[ReadOnly]
		public float4 m_LodParameters;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public float3 m_CameraDirection;

		[ReadOnly]
		public float m_AutoRejectDistance;

		public NativeQueue<VisibleLightData>.ParallelWriter m_VisibleLights;

		public void Execute(int index)
		{
			EnabledEffectData enabledEffectData = m_EnabledEffectData[index];
			if ((enabledEffectData.m_Flags & (EnabledEffectFlags.IsEnabled | EnabledEffectFlags.IsLight)) != (EnabledEffectFlags.IsEnabled | EnabledEffectFlags.IsLight))
			{
				return;
			}
			float3 position = enabledEffectData.m_Position;
			LightEffectCullData lightEffectCullData = m_LightEffectCullData[enabledEffectData.m_Prefab];
			bool flag = enabledEffectData.m_Intensity != 0f;
			for (int i = 0; i < 6; i++)
			{
				if (math.dot(m_Planes[i].xyz, position) + m_Planes[i].w < 0f - lightEffectCullData.m_Range)
				{
					flag = false;
				}
			}
			if (flag)
			{
				float num = RenderingUtils.CalculateMinDistance(new Bounds3(enabledEffectData.m_Position - lightEffectCullData.m_Range, enabledEffectData.m_Position + lightEffectCullData.m_Range), m_CameraPosition, m_CameraDirection, m_LodParameters) * lightEffectCullData.m_InvDistanceFactor;
				if (num < m_AutoRejectDistance)
				{
					m_VisibleLights.Enqueue(new VisibleLightData
					{
						m_Position = position,
						m_Rotation = enabledEffectData.m_Rotation,
						m_Prefab = enabledEffectData.m_Prefab,
						m_RelativeDistance = num,
						m_Color = enabledEffectData.m_Scale * enabledEffectData.m_Intensity
					});
				}
			}
		}
	}

	public struct VisibleLightData
	{
		public Entity m_Prefab;

		public float3 m_Position;

		public quaternion m_Rotation;

		public float3 m_Color;

		public float m_RelativeDistance;
	}

	[BurstCompile]
	private struct SortAndBuildPunctualLightsJob : IJob
	{
		private struct EffectInstanceDistanceComparer : IComparer<int>
		{
			private unsafe VisibleLightData* m_visibleLights;

			public unsafe EffectInstanceDistanceComparer(VisibleLightData* arrayPtr)
			{
				m_visibleLights = arrayPtr;
			}

			private unsafe ref VisibleLightData GetVisibleLightRef(int dataIndex)
			{
				return ref UnsafeUtility.AsRef<VisibleLightData>(m_visibleLights + dataIndex);
			}

			public int Compare(int x, int y)
			{
				return GetVisibleLightRef(x).m_RelativeDistance.CompareTo(GetVisibleLightRef(y).m_RelativeDistance);
			}
		}

		public NativeQueue<VisibleLightData> m_VisibleLights;

		[ReadOnly]
		public NativeParallelHashMap<Entity, LightEffectCullData> m_LightEffectCullData;

		[WriteOnly]
		public NativeList<HDRPDotsInputs.PunctualLightData> m_PunctualLightsOut;

		[WriteOnly]
		public NativeList<HDRPDotsInputs.LightEffectPrefabData> m_LightEffectPrefabData;

		[WriteOnly]
		public NativeArray<VisibleLight> m_VisibleLightsOut;

		public NativeReference<float> m_MaxDistance;

		public int m_maxLights;

		public float m_minDistanceScale;

		private unsafe ref VisibleLight GetVisibleLightRef(int dataIndex)
		{
			return ref UnsafeUtility.AsRef<VisibleLight>((byte*)m_VisibleLightsOut.GetUnsafePtr() + (nint)dataIndex * (nint)sizeof(VisibleLight));
		}

		private UnityEngine.LightType GetLightType(Game.Rendering.LightType lightType)
		{
			return lightType switch
			{
				Game.Rendering.LightType.Spot => UnityEngine.LightType.Spot, 
				Game.Rendering.LightType.Point => UnityEngine.LightType.Point, 
				Game.Rendering.LightType.Area => UnityEngine.LightType.Area, 
				_ => UnityEngine.LightType.Spot, 
			};
		}

		public unsafe void Execute()
		{
			NativeList<int> list = new NativeList<int>(m_maxLights, Allocator.Temp);
			NativeList<int> nativeList = new NativeList<int>(m_maxLights, Allocator.Temp);
			NativeList<VisibleLightData> nativeList2 = new NativeList<VisibleLightData>(m_maxLights * 2, Allocator.Temp);
			float num = m_MaxDistance.Value * m_minDistanceScale;
			int value = 0;
			VisibleLightData item;
			while (m_VisibleLights.TryDequeue(out item))
			{
				float num2 = item.m_RelativeDistance;
				if (m_MaxDistance.Value > 0f)
				{
					nativeList2.Add(in item);
					if (num2 < num)
					{
						nativeList.Add(in value);
					}
					else
					{
						list.Add(in value);
					}
					value++;
				}
				else
				{
					nativeList2.Add(in item);
					list.Add(in value);
					value++;
				}
			}
			float num3 = -1f;
			if (nativeList2.Length > 0)
			{
				NativeArray<VisibleLightData> nativeArray = nativeList2.AsArray();
				list.Sort(new EffectInstanceDistanceComparer((VisibleLightData*)nativeArray.GetUnsafePtr()));
				int num4 = math.min(list.Length + nativeList.Length, m_maxLights);
				float num5 = 1f;
				if (num4 < list.Length + nativeList.Length)
				{
					num5 = 1f / math.clamp(((num4 >= nativeList.Length) ? nativeArray[list[num4 - nativeList.Length]] : nativeArray[nativeList[num4]]).m_RelativeDistance, 1E-05f, 1f);
				}
				num4 = math.min(num4, m_VisibleLightsOut.Length);
				for (int i = 0; i < num4; i++)
				{
					VisibleLightData visibleLightData = ((i >= nativeList.Length) ? nativeArray[list[i - nativeList.Length]] : nativeArray[nativeList[i]]);
					float3 translation = visibleLightData.m_Position;
					LightEffectCullData lightEffectCullData = m_LightEffectCullData[visibleLightData.m_Prefab];
					float4x4 float4x = float4x4.TRS(translation, visibleLightData.m_Rotation, Vector3.one);
					ref VisibleLight visibleLightRef = ref GetVisibleLightRef(i);
					visibleLightRef.spotAngle = lightEffectCullData.m_SpotAngle;
					visibleLightRef.localToWorldMatrix = float4x;
					float num6 = visibleLightData.m_RelativeDistance * num5;
					float num7 = math.saturate(1f - math.lengthsq(math.max(0f, 5f * num6 - 4f)));
					visibleLightRef.screenRect = new Rect(0f, 0f, 10f, 10f);
					visibleLightRef.lightType = GetLightType(lightEffectCullData.m_lightType);
					visibleLightRef.finalColor = (Vector4)new float4(visibleLightData.m_Color * num7, 1f);
					visibleLightRef.range = lightEffectCullData.m_Range;
					m_PunctualLightsOut.AddNoResize(new HDRPDotsInputs.PunctualLightData
					{
						lightEffectPrefabDataIndex = lightEffectCullData.m_LightEffectPrefabDataIndex
					});
					m_LightEffectPrefabData[lightEffectCullData.m_LightEffectPrefabDataIndex] = new HDRPDotsInputs.LightEffectPrefabData
					{
						cookieMode = CookieMode.Clamp
					};
					num3 = math.max(num3, visibleLightData.m_RelativeDistance);
				}
			}
			m_MaxDistance.Value = num3;
			list.Dispose();
			nativeList.Dispose();
			nativeList2.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LightEffectData> __Game_Prefabs_LightEffectData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_LightEffectData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LightEffectData>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private RenderingSystem m_RenderingSystem;

	private EffectControlSystem m_EffectControlSystem;

	private EntityQuery m_LightEffectPrefabQuery;

	private NativeParallelHashMap<Entity, LightEffectCullData> m_LightEffectCullData;

	private static DefaultLightParams s_DefaultLightParams;

	private NativeQueue<VisibleLightData> m_VisibleLights;

	private NativeReference<float> m_LastFrameMaxPunctualLightDistance;

	public static bool s_enableMinMaxLightCullingOptim = true;

	public static float s_maxLightDistanceScale = 1.5f;

	public static float s_minLightDistanceScale = 0.5f;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_EffectControlSystem = base.World.GetOrCreateSystemManaged<EffectControlSystem>();
		m_LightEffectPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<LightEffectData>(), ComponentType.ReadOnly<PrefabData>());
		m_LightEffectCullData = new NativeParallelHashMap<Entity, LightEffectCullData>(128, Allocator.Persistent);
		m_VisibleLights = new NativeQueue<VisibleLightData>(Allocator.Persistent);
		m_LastFrameMaxPunctualLightDistance = new NativeReference<float>(Allocator.Persistent);
		m_LastFrameMaxPunctualLightDistance.Value = -1f;
		ReadDefaultLightParams();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		HDRPDotsInputs.punctualLightsJobHandle.Complete();
		m_LastFrameMaxPunctualLightDistance.Dispose();
		m_VisibleLights.Dispose();
		m_LightEffectCullData.Dispose();
		HDRPDotsInputs.ClearFrameLightData();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		HDRPDotsInputs.punctualLightsJobHandle.Complete();
		HDRPDotsInputs.MaxPunctualLights = m_RenderingSystem.maxLightCount;
		HDRPDotsInputs.ClearFrameLightData();
		Camera main = Camera.main;
		if (!(main == null))
		{
			m_EffectControlSystem.GetLodParameters(out var lodParameters, out var cameraPosition, out var cameraDirection);
			ComputeLightEffectCullData(lodParameters);
			Plane[] array = GeometryUtility.CalculateFrustumPlanes(main);
			NativeArray<float4> planes = new NativeArray<float4>(6, Allocator.TempJob);
			for (int i = 0; i < array.Length; i++)
			{
				planes[i] = new float4(array[i].normal, array[i].distance);
			}
			if (!s_enableMinMaxLightCullingOptim)
			{
				m_LastFrameMaxPunctualLightDistance.Value = -1f;
			}
			float autoRejectDistance = 1f;
			if (m_LastFrameMaxPunctualLightDistance.Value > 0f)
			{
				autoRejectDistance = m_LastFrameMaxPunctualLightDistance.Value * s_maxLightDistanceScale;
			}
			JobHandle dependencies;
			LightCullingJob jobData = new LightCullingJob
			{
				m_LightEffectCullData = m_LightEffectCullData,
				m_Planes = planes,
				m_EnabledEffectData = m_EffectControlSystem.GetEnabledData(readOnly: true, out dependencies),
				m_LodParameters = lodParameters,
				m_CameraPosition = cameraPosition,
				m_CameraDirection = cameraDirection,
				m_AutoRejectDistance = autoRejectDistance,
				m_VisibleLights = m_VisibleLights.AsParallelWriter()
			};
			JobHandle jobHandle = jobData.Schedule(jobData.m_EnabledEffectData, 16, dependencies);
			m_EffectControlSystem.AddEnabledDataReader(jobHandle);
			HDRPDotsInputs.punctualLightsJobHandle = IJobExtensions.Schedule(new SortAndBuildPunctualLightsJob
			{
				m_maxLights = HDRPDotsInputs.MaxPunctualLights,
				m_minDistanceScale = s_minLightDistanceScale,
				m_PunctualLightsOut = HDRPDotsInputs.s_punctualLightdata,
				m_LightEffectPrefabData = HDRPDotsInputs.s_lightEffectPrefabData,
				m_VisibleLightsOut = HDRPDotsInputs.s_punctualVisibleLights,
				m_LightEffectCullData = m_LightEffectCullData,
				m_VisibleLights = m_VisibleLights,
				m_MaxDistance = m_LastFrameMaxPunctualLightDistance
			}, jobHandle);
		}
	}

	private void ReadDefaultLightParams()
	{
		GameObject gameObject = new GameObject("Default LightSource");
		HDAdditionalLightData hDAdditionalLightData = gameObject.AddHDLight(HDLightTypeAndShape.ConeSpot);
		s_DefaultLightParams.shapeWidth = hDAdditionalLightData.shapeWidth;
		s_DefaultLightParams.shapeHeight = hDAdditionalLightData.shapeHeight;
		s_DefaultLightParams.spotIESCutoffPercent01 = hDAdditionalLightData.spotIESCutoffPercent01;
		s_DefaultLightParams.shapeRadius = hDAdditionalLightData.shapeRadius;
		CoreUtils.Destroy(gameObject);
	}

	private UnityEngine.Rendering.HighDefinition.SpotLightShape GetUnitySpotShape(Game.Rendering.SpotLightShape spotlightShape)
	{
		return spotlightShape switch
		{
			Game.Rendering.SpotLightShape.Pyramid => UnityEngine.Rendering.HighDefinition.SpotLightShape.Pyramid, 
			Game.Rendering.SpotLightShape.Box => UnityEngine.Rendering.HighDefinition.SpotLightShape.Box, 
			Game.Rendering.SpotLightShape.Cone => UnityEngine.Rendering.HighDefinition.SpotLightShape.Cone, 
			_ => UnityEngine.Rendering.HighDefinition.SpotLightShape.Cone, 
		};
	}

	private UnityEngine.Rendering.HighDefinition.AreaLightShape GetUnityAreaShape(Game.Rendering.AreaLightShape arealightShape)
	{
		return arealightShape switch
		{
			Game.Rendering.AreaLightShape.Tube => UnityEngine.Rendering.HighDefinition.AreaLightShape.Tube, 
			Game.Rendering.AreaLightShape.Rectangle => UnityEngine.Rendering.HighDefinition.AreaLightShape.Rectangle, 
			_ => UnityEngine.Rendering.HighDefinition.AreaLightShape.Rectangle, 
		};
	}

	private void GetRenderDataFromLigthEffet(ref HDLightRenderData hdLightRenderData, LightEffectData lightEffectData, LightEffect lightEffect, float4 lodParameters)
	{
		hdLightRenderData.pointLightType = ((lightEffect.m_Type == Game.Rendering.LightType.Area) ? HDAdditionalLightData.PointLightHDType.Area : HDAdditionalLightData.PointLightHDType.Punctual);
		hdLightRenderData.spotLightShape = GetUnitySpotShape(lightEffect.m_SpotShape);
		hdLightRenderData.areaLightShape = GetUnityAreaShape(lightEffect.m_AreaShape);
		hdLightRenderData.lightLayer = LightLayerEnum.Everything;
		hdLightRenderData.fadeDistance = 100000f;
		hdLightRenderData.distance = lightEffect.m_LuxAtDistance;
		hdLightRenderData.angularDiameter = lightEffect.m_SpotAngle;
		hdLightRenderData.volumetricFadeDistance = lightEffect.m_VolumetricFadeDistance;
		hdLightRenderData.includeForRayTracing = false;
		hdLightRenderData.useScreenSpaceShadows = false;
		hdLightRenderData.useRayTracedShadows = false;
		hdLightRenderData.colorShadow = false;
		hdLightRenderData.lightDimmer = lightEffect.m_LightDimmer;
		hdLightRenderData.volumetricDimmer = lightEffect.m_VolumetricDimmer;
		hdLightRenderData.shapeWidth = lightEffect.m_ShapeWidth;
		hdLightRenderData.shapeHeight = lightEffect.m_ShapeHeight;
		hdLightRenderData.aspectRatio = lightEffect.m_AspectRatio;
		hdLightRenderData.innerSpotPercent = lightEffect.m_InnerSpotPercentage;
		hdLightRenderData.spotIESCutoffPercent = 100f;
		hdLightRenderData.shadowDimmer = 1f;
		hdLightRenderData.volumetricShadowDimmer = 1f;
		hdLightRenderData.shadowFadeDistance = 0f;
		hdLightRenderData.shapeRadius = lightEffect.m_ShapeRadius;
		hdLightRenderData.barnDoorLength = lightEffect.m_BarnDoorLength;
		hdLightRenderData.barnDoorAngle = lightEffect.m_BarnDoorAngle;
		hdLightRenderData.flareSize = 0f;
		hdLightRenderData.flareFalloff = 0f;
		hdLightRenderData.affectVolumetric = lightEffect.m_UseVolumetric;
		hdLightRenderData.affectDiffuse = lightEffect.m_AffectDiffuse;
		hdLightRenderData.affectSpecular = lightEffect.m_AffectSpecular;
		hdLightRenderData.applyRangeAttenuation = lightEffect.m_ApplyRangeAttenuation;
		hdLightRenderData.penumbraTint = false;
		hdLightRenderData.interactsWithSky = false;
		hdLightRenderData.surfaceTint = Color.black;
		hdLightRenderData.shadowTint = Color.black;
		hdLightRenderData.flareTint = Color.black;
	}

	private void ComputeLightEffectCullData(float4 lodParameters)
	{
		m_LightEffectCullData.Clear();
		NativeArray<ArchetypeChunk> nativeArray = m_LightEffectPrefabQuery.ToArchetypeChunkArray(Allocator.Temp);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<LightEffectData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_LightEffectData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		CompleteDependency();
		int num = m_LightEffectPrefabQuery.CalculateEntityCount();
		float num2 = 1f / lodParameters.x;
		if (!HDRPDotsInputs.s_HdLightRenderData.IsCreated || num + 8 > HDRPDotsInputs.s_HdLightRenderData.Length)
		{
			ArrayExtensions.ResizeArray(ref HDRPDotsInputs.s_HdLightRenderData, num + 8);
		}
		LightEffectCullData item = default(LightEffectCullData);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ArchetypeChunk archetypeChunk = nativeArray[i];
			NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
			NativeArray<PrefabData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
			NativeArray<LightEffectData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				Entity key = nativeArray2[j];
				PrefabData prefabData = nativeArray3[j];
				LightEffectData lightEffectData = nativeArray4[j];
				LightEffect component = m_PrefabSystem.GetPrefab<EffectPrefab>(prefabData).GetComponent<LightEffect>();
				item.m_LightEffectPrefabDataIndex = HDRPDotsInputs.s_lightEffectPrefabData.Length;
				item.m_lightType = component.m_Type;
				item.m_Range = component.m_Range;
				item.m_SpotAngle = component.m_SpotAngle;
				item.m_InvDistanceFactor = lightEffectData.m_InvDistanceFactor * num2;
				HDRPDotsInputs.s_lightEffectPrefabData.Add(default(HDRPDotsInputs.LightEffectPrefabData));
				HDRPDotsInputs.s_lightEffectPrefabCookies.Add(component.m_Cookie);
				GetRenderDataFromLigthEffet(ref HDRPDotsInputs.s_HdLightRenderData.ElementAt(item.m_LightEffectPrefabDataIndex), lightEffectData, component, lodParameters);
				m_LightEffectCullData.Add(key, item);
			}
		}
	}

	private static GPULightType GetGPULightType(LightEffect lightEffect)
	{
		if (lightEffect.m_Type == Game.Rendering.LightType.Spot)
		{
			if (lightEffect.m_SpotShape == Game.Rendering.SpotLightShape.Cone)
			{
				return GPULightType.Spot;
			}
			if (lightEffect.m_SpotShape == Game.Rendering.SpotLightShape.Pyramid)
			{
				return GPULightType.ProjectorPyramid;
			}
			if (lightEffect.m_SpotShape == Game.Rendering.SpotLightShape.Box)
			{
				return GPULightType.ProjectorBox;
			}
		}
		else
		{
			if (lightEffect.m_Type == Game.Rendering.LightType.Point)
			{
				return GPULightType.Point;
			}
			if (lightEffect.m_Type == Game.Rendering.LightType.Area)
			{
				if (lightEffect.m_AreaShape == Game.Rendering.AreaLightShape.Rectangle)
				{
					return GPULightType.Rectangle;
				}
				if (lightEffect.m_AreaShape == Game.Rendering.AreaLightShape.Tube)
				{
					return GPULightType.Tube;
				}
			}
		}
		throw new NotImplementedException($"Unsupported light type {lightEffect.m_Type}");
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
	public LightCullingSystem()
	{
	}
}
