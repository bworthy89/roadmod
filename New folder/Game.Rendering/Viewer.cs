using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Rendering.Legacy;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Rendering;

public class Viewer
{
	public float maxFocusLockDistance = 6f;

	private ViewerDistances m_ViewerDistances;

	private float m_TargetFocusDistance;

	private float m_FocusDistanceVelocity;

	private WaterSystem m_waterSYstem;

	private const int kCenterSampleCount = 4;

	private static int[] kSamplePattern32 = new int[64]
	{
		1, 1, 4, 1, 2, -1, -4, 0, -6, 1,
		-7, -1, -2, 2, 7, 2, 2, 3, -5, 4,
		-1, 4, 4, 4, -7, 5, -3, 5, 6, 5,
		1, 6, -4, 7, 5, 7, -1, -2, 6, -2,
		-6, -3, -3, -3, 0, -4, 4, -4, 2, -5,
		7, -5, -7, -6, -3, -6, 5, -6, -5, -7,
		-1, -7, 3, -7
	};

	public ViewerDistances viewerDistances => m_ViewerDistances;

	public float visibilityDistance => camera.farClipPlane;

	public float nearClipPlane => camera.nearClipPlane;

	public float3 position => camera.transform.position;

	public float3 forward => camera.transform.forward;

	public float3 right => camera.transform.right;

	public Camera camera { get; private set; }

	public LegacyFrustumPlanes frustumPlanes => CalculateFrustumPlanes(camera);

	public Bounds bounds => UpdateBounds();

	public bool shadowsAdjustStartDistance { get; set; } = true;

	public float pushCullingNearPlaneMultiplier { get; set; } = 0.9f;

	public float pushCullingNearPlaneValue { get; set; } = 100f;

	public bool shadowsAdjustFarDistance { get; set; } = true;

	public Viewer(Camera camera, WaterSystem waterSystem)
	{
		this.camera = camera;
		m_waterSYstem = waterSystem;
	}

	public bool TryGetLODParameters(out LODParameters lodParameters)
	{
		if (camera.TryGetCullingParameters(out var cullingParameters))
		{
			lodParameters = cullingParameters.lodParameters;
			return true;
		}
		lodParameters = default(LODParameters);
		return false;
	}

	protected Bounds UpdateBounds()
	{
		Transform transform = camera.transform;
		float num = math.tan(math.radians(camera.fieldOfView * 0.5f));
		float num2 = num * camera.aspect;
		float3 @float = transform.forward;
		float3 float2 = transform.right;
		float3 float3 = transform.up;
		float farClipPlane = camera.farClipPlane;
		float num3 = camera.nearClipPlane;
		float3 float4 = position;
		float3 float5 = float4 + @float * farClipPlane - farClipPlane * float2 * num2 + float3 * num * farClipPlane;
		float3 float6 = float4 + @float * farClipPlane + farClipPlane * float2 * num2 - float3 * num * farClipPlane;
		float3 float7 = float4 + @float * farClipPlane - farClipPlane * float2 * num2 - float3 * num * farClipPlane;
		float3 float8 = float4 + @float * farClipPlane + farClipPlane * float2 * num2 + float3 * num * farClipPlane;
		float3 float9 = float4 + @float * num3;
		Bounds result = new Bounds(Vector3.zero, Vector3.one * float.NegativeInfinity);
		result.Encapsulate(float5);
		result.Encapsulate(float6);
		result.Encapsulate(float7);
		result.Encapsulate(float8);
		result.Encapsulate(float9);
		return result;
	}

	private static LegacyFrustumPlanes ExtractProjectionPlanes(float4x4 worldToProjectionMatrix)
	{
		LegacyFrustumPlanes result = default(LegacyFrustumPlanes);
		float4 @float = new float4(worldToProjectionMatrix.c0.w, worldToProjectionMatrix.c1.w, worldToProjectionMatrix.c2.w, worldToProjectionMatrix.c3.w);
		float4 float2 = new float4(worldToProjectionMatrix.c0.x, worldToProjectionMatrix.c1.x, worldToProjectionMatrix.c2.x, worldToProjectionMatrix.c3.x);
		float3 float3 = new float3(float2.x + @float.x, float2.y + @float.y, float2.z + @float.z);
		float num = 1f / math.length(float3);
		result.left.normal = float3 * num;
		result.left.distance = (float2.w + @float.w) * num;
		float3 = new float3(0f - float2.x + @float.x, 0f - float2.y + @float.y, 0f - float2.z + @float.z);
		num = 1f / math.length(float3);
		result.right.normal = float3 * num;
		result.right.distance = (0f - float2.w + @float.w) * num;
		float2 = new float4(worldToProjectionMatrix.c0.y, worldToProjectionMatrix.c1.y, worldToProjectionMatrix.c2.y, worldToProjectionMatrix.c3.y);
		float3 = new Vector3(float2.x + @float.x, float2.y + @float.y, float2.z + @float.z);
		num = 1f / math.length(float3);
		result.bottom.normal = float3 * num;
		result.bottom.distance = (float2.w + @float.w) * num;
		float3 = new Vector3(0f - float2.x + @float.x, 0f - float2.y + @float.y, 0f - float2.z + @float.z);
		num = 1f / math.length(float3);
		result.top.normal = float3 * num;
		result.top.distance = (0f - float2.w + @float.w) * num;
		float2 = new float4(worldToProjectionMatrix.c0.z, worldToProjectionMatrix.c1.z, worldToProjectionMatrix.c2.z, worldToProjectionMatrix.c3.z);
		float3 = new Vector3(float2.x + @float.x, float2.y + @float.y, float2.z + @float.z);
		num = 1f / math.length(float3);
		result.zNear.normal = float3 * num;
		result.zNear.distance = (float2.w + @float.w) * num;
		float3 = new Vector3(0f - float2.x + @float.x, 0f - float2.y + @float.y, 0f - float2.z + @float.z);
		num = 1f / math.length(float3);
		result.zFar.normal = float3 * num;
		result.zFar.distance = (0f - float2.w + @float.w) * num;
		return result;
	}

	private static LegacyFrustumPlanes CalculateFrustumPlanes(Camera camera)
	{
		return ExtractProjectionPlanes(camera.projectionMatrix * camera.worldToCameraMatrix);
	}

	public void UpdateRaycast(EntityManager entityManager, RaycastSystem raycast, float deltaTime)
	{
		float3 x = position;
		NativeArray<RaycastResult> result = raycast.GetResult(this);
		float num = visibilityDistance;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = -1f;
		float num6 = -1f;
		for (int i = 0; i < result.Length; i++)
		{
			RaycastResult raycastResult = result[i];
			if (raycastResult.m_Owner == Entity.Null)
			{
				continue;
			}
			float num7 = math.distance(x, raycastResult.m_Hit.m_HitPosition);
			if (i == 0)
			{
				m_ViewerDistances.ground = num7;
			}
			else if (i == 1)
			{
				m_ViewerDistances.center = num7;
				Entity hitEntity = raycastResult.m_Hit.m_HitEntity;
				if (num7 < maxFocusLockDistance && hitEntity != Entity.Null && entityManager.HasComponent<Creature>(hitEntity))
				{
					num5 = num7;
				}
			}
			else if (i - 2 < 4)
			{
				num6 = math.max(num6, num7);
			}
			else
			{
				num = math.min(num, num7);
				num2 = math.max(num2, num7);
				num3 += num7;
				num4 += 1f;
			}
		}
		m_ViewerDistances.closestSurface = num;
		m_ViewerDistances.farthestSurface = num2;
		m_ViewerDistances.averageSurface = (num + num2) / 2f;
		if (num4 > 0f)
		{
			m_ViewerDistances.averageSurface = num3 / num4;
		}
		if (num6 >= 0f)
		{
			m_TargetFocusDistance = num6;
		}
		if (num5 >= 0f)
		{
			m_TargetFocusDistance = num5;
		}
		m_ViewerDistances.focus = MathUtils.SmoothDamp(m_ViewerDistances.focus, m_TargetFocusDistance, ref m_FocusDistanceVelocity, 0.3f, float.MaxValue, deltaTime);
		if (camera != null)
		{
			camera.focusDistance = m_ViewerDistances.focus;
			UpdatePushNearCullingPlane();
			UpdateDistanceToSeaLevel();
		}
	}

	private void UpdateDistanceToSeaLevel()
	{
		float num = 0f;
		UnityEngine.Plane plane = new UnityEngine.Plane(Vector3.up, 0f - m_waterSYstem.SeaLevel);
		for (int i = 0; i < 4; i++)
		{
			float x = (((i & 1) != 0) ? 1f : 0f);
			float y = (((i & 2) != 0) ? 1f : 0f);
			Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0f));
			num = (plane.Raycast(ray, out var enter) ? math.max(num, enter) : visibilityDistance);
		}
		m_ViewerDistances.maxDistanceToSeaLevel = num;
	}

	private void UpdatePushNearCullingPlane()
	{
		HDCamera orCreate = HDCamera.GetOrCreate(camera);
		if (orCreate != null)
		{
			if (shadowsAdjustStartDistance)
			{
				float valueToClamp = (m_ViewerDistances.closestSurface - pushCullingNearPlaneValue) * pushCullingNearPlaneMultiplier;
				valueToClamp = math.clamp(valueToClamp, nearClipPlane, visibilityDistance * 0.1f);
				orCreate.overrideNearPlaneForCullingOnly = valueToClamp;
			}
			else
			{
				orCreate.overrideNearPlaneForCullingOnly = 0f;
			}
		}
	}

	public void Raycast(RaycastSystem raycast, bool debugRays)
	{
		float3 @float = position;
		RaycastInput input = new RaycastInput
		{
			m_Flags = (RaycastFlags)0u,
			m_CollisionMask = (CollisionMask.OnGround | CollisionMask.Overground),
			m_NetLayerMask = Layer.All
		};
		input.m_TypeMask = TypeMask.Terrain | TypeMask.Water;
		input.m_Line = new Line3.Segment(@float, @float + (float3)Vector3.down * visibilityDistance);
		raycast.AddInput(this, input);
		input.m_TypeMask = TypeMask.Terrain | TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net | TypeMask.Water;
		input.m_Line = new Line3.Segment(@float, @float + forward * visibilityDistance);
		raycast.AddInput(this, input);
		for (int i = 0; i < kSamplePattern32.Length; i += 2)
		{
			float x = (float)kSamplePattern32[i] / 7f * 0.5f + 0.5f;
			float y = (float)kSamplePattern32[i + 1] / 7f * 0.5f + 0.5f;
			Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0f));
			if (i < 8)
			{
				input.m_TypeMask = TypeMask.Terrain | TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net | TypeMask.Water;
			}
			else
			{
				input.m_TypeMask = TypeMask.Terrain | TypeMask.Water;
			}
			input.m_Line = new Line3.Segment(@float, @float + (float3)ray.direction * visibilityDistance);
			if (debugRays)
			{
				Colossal.Gizmos.batcher.DrawLine(@float, @float + (float3)ray.direction * visibilityDistance, Color.green);
			}
			raycast.AddInput(this, input);
		}
	}
}
