using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Atmosphere;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PlanetarySystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct LightData
	{
		private readonly string m_Tag;

		public Transform transform { get; private set; }

		public Light light { get; private set; }

		public HDAdditionalLightData additionalData { get; private set; }

		public float initialIntensity { get; private set; }

		public bool isValid
		{
			get
			{
				if (transform == null)
				{
					GameObject gameObject = GameObject.FindGameObjectWithTag(m_Tag);
					if (gameObject != null)
					{
						transform = gameObject.transform;
						light = gameObject.GetComponent<Light>();
						additionalData = gameObject.GetComponent<HDAdditionalLightData>();
						initialIntensity = additionalData.intensity;
					}
				}
				return transform != null;
			}
		}

		public LightData(string tag)
		{
			m_Tag = tag;
			transform = null;
			light = null;
			additionalData = null;
			initialIntensity = 0f;
		}
	}

	private static class ShaderIDs
	{
		public static readonly int _Camera2World = Shader.PropertyToID("_Camera2World");

		public static readonly int _CameraData = Shader.PropertyToID("_CameraData");

		public static readonly int _SunDirection = Shader.PropertyToID("_SunDirection");

		public static readonly int _Luminance = Shader.PropertyToID("_Luminance");

		public static readonly int _Direction = Shader.PropertyToID("_Direction");

		public static readonly int _Tangent = Shader.PropertyToID("_Tangent");

		public static readonly int _BiTangent = Shader.PropertyToID("_BiTangent");

		public static readonly int _Albedo = Shader.PropertyToID("_Albedo");

		public static readonly int _OrenNayarCoefficients = Shader.PropertyToID("_OrenNayarCoefficients");

		public static readonly int _TexDiffuse = Shader.PropertyToID("_TexDiffuse");

		public static readonly int _TexNormal = Shader.PropertyToID("_TexNormal");

		public static readonly int _Corners = Shader.PropertyToID("_Corners");
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private static readonly float kDefaultLatitude = 41.9028f;

	private static readonly float kDefaultLongitude = 12.4964f;

	private SunMoonData m_SunMoonData;

	private TimeSystem m_TimeSystem;

	private LightData m_SunLight;

	private LightData m_MoonLight;

	private LightData m_NightLight;

	private RenderingSystem m_RenderingSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private PrefabSystem m_PrefabSystem;

	private const float kDaysInYear = 365f;

	private const float kInvDaysInYear = 0.002739726f;

	private const float kHoursInDay = 24f;

	private const float kInvHoursInDay = 1f / 24f;

	private const float kSecsInMin = 60f;

	private const float kInvSecsInMin = 1f / 60f;

	private const float kSecsInHour = 3600f;

	private const float kInvSecsInHour = 0.00027777778f;

	private const float kLunarCyclesPerYear = 12f;

	private const float kInvLunarCyclesPerYear = 1f / 12f;

	private int m_Year = 2020;

	private int m_Day = 127;

	private int m_Hour = 12;

	private int m_Minute;

	private float m_Second;

	private float m_Latitude = kDefaultLatitude;

	private float m_Longitude = kDefaultLongitude;

	private int m_NumberOfLunarCyclesPerYear = 1;

	private RenderTexture m_MoonTexture;

	private Material m_MoonMaterial;

	private int m_ClearPass;

	private int m_LitPass;

	private Vector2 m_OrenNayarCoefficients;

	private float m_SurfaceRoughness;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1383560598_0;

	private EntityQuery __query_1383560598_1;

	private EntityQuery __query_1383560598_2;

	public LightData SunLight => m_SunLight;

	public LightData MoonLight => m_MoonLight;

	public LightData NightLight => m_NightLight;

	public bool overrideTime { get; set; }

	public float latitude
	{
		get
		{
			return m_Latitude;
		}
		set
		{
			m_Latitude = math.clamp(value, -90f, 90f);
		}
	}

	public float longitude
	{
		get
		{
			return m_Longitude;
		}
		set
		{
			m_Longitude = math.clamp(value, -180f, 180f);
		}
	}

	public float debugTimeMultiplier { get; set; } = 1f;

	public int year
	{
		get
		{
			return m_Year;
		}
		set
		{
			m_Year = value;
		}
	}

	public int day
	{
		get
		{
			return m_Day;
		}
		set
		{
			m_Day = value;
		}
	}

	public int hour
	{
		get
		{
			return m_Hour;
		}
		set
		{
			m_Hour = value;
		}
	}

	public int minute
	{
		get
		{
			return m_Minute;
		}
		set
		{
			m_Minute = value;
		}
	}

	public float second
	{
		get
		{
			return m_Second;
		}
		set
		{
			m_Second = value;
		}
	}

	public float time
	{
		get
		{
			return (float)hour + (float)minute * (1f / 60f) + second * 0.00027777778f;
		}
		set
		{
			hour = Mathf.FloorToInt(value);
			value -= (float)hour;
			minute = Mathf.FloorToInt(value * 60f);
			value -= (float)minute * (1f / 60f);
			second = value * 3600f;
		}
	}

	public float dayOfYear
	{
		get
		{
			return (float)day + normalizedTime;
		}
		set
		{
			day = Mathf.FloorToInt(value);
			value -= (float)day;
			normalizedTime = value;
		}
	}

	public float normalizedDayOfYear
	{
		get
		{
			return (dayOfYear - 1f) * 0.002739726f;
		}
		set
		{
			dayOfYear = value * 365f + 1f;
		}
	}

	public float normalizedTime
	{
		get
		{
			return time * (1f / 24f);
		}
		set
		{
			time = value * 24f;
		}
	}

	public int numberOfLunarCyclesPerYear
	{
		get
		{
			return m_NumberOfLunarCyclesPerYear;
		}
		set
		{
			m_NumberOfLunarCyclesPerYear = math.max(0, value);
		}
	}

	public int moonDay => Mathf.FloorToInt((float)day * (1f / 12f) * (float)numberOfLunarCyclesPerYear);

	public float moonSurfaceRoughness
	{
		get
		{
			return m_SurfaceRoughness;
		}
		set
		{
			m_SurfaceRoughness = Mathf.Clamp01(value);
			float num = MathF.PI / 2f * m_SurfaceRoughness;
			float num2 = num * num;
			m_OrenNayarCoefficients = new Vector2(1f - 0.5f * num2 / (num2 + 0.33f), 0.45f * num2 / (num2 + 0.09f));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_MoonTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32)
		{
			name = "MoonTexture",
			hideFlags = HideFlags.DontSave
		};
		m_MoonMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Satellites"));
		m_ClearPass = m_MoonMaterial.FindPass("Clear");
		m_LitPass = m_MoonMaterial.FindPass("LitSatellite");
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_SunLight = new LightData("SunLight");
		m_MoonLight = new LightData("MoonLight");
		m_NightLight = new LightData("NightLight");
		m_SunMoonData = default(SunMoonData);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		CoreUtils.Destroy(m_MoonTexture);
		CoreUtils.Destroy(m_MoonMaterial);
	}

	private static DateTime CreateDateTime(int year, int day, int hour, int minute, float second, float longitude)
	{
		return new DateTime(0L, DateTimeKind.Utc).AddYears(year - 1).AddDays(day - 1).AddHours(hour)
			.AddMinutes(minute)
			.AddSeconds(second)
			.AddSeconds(-43200f * longitude / 180f);
	}

	private void UpdateTime(float date, float time, int year)
	{
		normalizedDayOfYear = date;
		normalizedTime = time;
		m_Year = year;
	}

	public TopocentricCoordinates GetSunPosition(DateTime date, double latitude, double longitude)
	{
		return m_SunMoonData.GetSunPosition(date, latitude, longitude);
	}

	public MoonCoordinate GetMoonPosition(DateTime date, double latitude, double longitude)
	{
		return m_SunMoonData.GetMoonPosition(date, latitude, longitude);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float num = latitude;
		float num2 = longitude;
		if (GameManager.instance.gameMode == GameMode.Game)
		{
			bool flag = overrideTime;
			GameplaySettings gameplaySettings = SharedSettings.instance?.gameplay;
			if (gameplaySettings != null && !overrideTime && !gameplaySettings.dayNightVisual)
			{
				num = 51.2277f;
				num2 = 6.7735f;
				time = 14.5f;
				day = 177;
				year = 2020;
				flag = true;
			}
			if (!flag && __query_1383560598_0.TryGetSingleton<TimeSettingsData>(out var value) && __query_1383560598_1.TryGetSingleton<TimeData>(out var value2))
			{
				double renderingFrame = (double)(m_RenderingSystem.frameIndex - value2.m_FirstFrame) + (double)m_RenderingSystem.frameTime;
				float timeOfYear = m_TimeSystem.GetTimeOfYear(value, value2, renderingFrame);
				float num3 = m_TimeSystem.GetTimeOfDay(value, value2, renderingFrame) * debugTimeMultiplier;
				int num4 = m_TimeSystem.GetYear(value, value2);
				UpdateTime(timeOfYear, num3, num4);
			}
		}
		else
		{
			if (GameManager.instance.gameMode != GameMode.Editor)
			{
				return;
			}
			if (!overrideTime && __query_1383560598_0.TryGetSingleton<TimeSettingsData>(out var value3) && __query_1383560598_1.TryGetSingleton<TimeData>(out var value4))
			{
				double renderingFrame2 = (double)(m_RenderingSystem.frameIndex - value4.m_FirstFrame) + (double)m_RenderingSystem.frameTime;
				float timeOfYear2 = m_TimeSystem.GetTimeOfYear(value3, value4, renderingFrame2);
				float num5 = m_TimeSystem.GetTimeOfDay(value3, value4, renderingFrame2) * debugTimeMultiplier;
				int num6 = m_TimeSystem.GetYear(value3, value4, renderingFrame2);
				UpdateTime(timeOfYear2, num5, num6);
			}
		}
		if (m_SunLight.isValid)
		{
			JulianDateTime date = CreateDateTime(year, day, hour, minute, second, num2);
			float planetTime;
			float3 @float = m_SunMoonData.GetSunPosition(date, num, num2).ToLocalCoordinates(out planetTime);
			float4x4 float4x = float4x4.LookAt(@float, float3.zero, new float3(0f, 1f, 0f));
			float3 float2 = math.rotate(float4x, new float3(0f, 0f, 1f));
			m_SunLight.transform.position = @float;
			m_SunLight.transform.rotation = new quaternion(float4x);
			m_SunLight.additionalData.intensity = m_SunLight.initialIntensity * math.smoothstep(0f, 0.3f, math.abs(math.min(0f, float2.y)));
		}
		if (m_MoonLight.isValid)
		{
			JulianDateTime date2 = CreateDateTime(year, moonDay, hour, minute, second, num2);
			MoonCoordinate moonPosition = m_SunMoonData.GetMoonPosition(date2, num, num2);
			float planetTime2;
			float3 float3 = moonPosition.topoCoords.ToLocalCoordinates(out planetTime2);
			float4x4 float4x2 = float4x4.LookAt(float3, float3.zero, new float3(0f, 1f, 0f));
			math.rotate(float4x2, new float3(0f, 0f, 1f));
			m_MoonLight.transform.position = float3;
			m_MoonLight.transform.rotation = new quaternion(float4x2);
			m_MoonLight.additionalData.distance = (float)moonPosition.distance;
			if (m_SunLight.isValid)
			{
				RenderMoon();
			}
		}
		if (m_NightLight.isValid && m_MoonLight.isValid)
		{
			float3 float4 = m_MoonLight.transform.position;
			float4.y = math.max(float4.y, 0.3f);
			float4x4 m = float4x4.LookAt(float4, float3.zero, new float3(0f, 1f, 0f));
			m_NightLight.transform.position = float4;
			m_NightLight.transform.rotation = new quaternion(m);
		}
	}

	private void RenderMoon()
	{
		if (m_MoonTexture != null && m_MoonMaterial != null && m_CameraUpdateSystem.activeCamera != null)
		{
			moonSurfaceRoughness = 0.8f;
			Camera activeCamera = m_CameraUpdateSystem.activeCamera;
			float num = Mathf.Tan(0.5f * activeCamera.fieldOfView * MathF.PI / 180f);
			Vector4 value = new Vector4(activeCamera.aspect * num, num, activeCamera.nearClipPlane, activeCamera.farClipPlane);
			m_MoonMaterial.SetMatrix(ShaderIDs._Camera2World, activeCamera.cameraToWorldMatrix);
			m_MoonMaterial.SetVector(ShaderIDs._CameraData, value);
			m_MoonMaterial.SetVector(ShaderIDs._SunDirection, m_SunLight.transform.forward);
			m_MoonMaterial.SetVector(ShaderIDs._Direction, m_MoonLight.transform.forward);
			m_MoonMaterial.SetVector(ShaderIDs._Tangent, m_MoonLight.transform.right);
			m_MoonMaterial.SetVector(ShaderIDs._BiTangent, m_MoonLight.transform.up);
			m_MoonMaterial.SetColor(ShaderIDs._Albedo, new Color(1f, 1f, 1f, 1f));
			m_MoonMaterial.SetVector(ShaderIDs._Corners, new Vector4(0f, 0f, 1f, 1f));
			m_MoonMaterial.SetVector(ShaderIDs._OrenNayarCoefficients, m_OrenNayarCoefficients);
			m_MoonMaterial.SetFloat(ShaderIDs._Luminance, 10f);
			if (__query_1383560598_2.TryGetSingleton<AtmosphereData>(out var value2) && m_PrefabSystem.TryGetPrefab<AtmospherePrefab>(value2.m_AtmospherePrefab, out var prefab))
			{
				m_MoonMaterial.SetTexture(ShaderIDs._TexDiffuse, prefab.m_MoonAlbedo);
				m_MoonMaterial.SetTexture(ShaderIDs._TexNormal, prefab.m_MoonNormal);
			}
			Graphics.Blit(null, m_MoonTexture, m_MoonMaterial, m_ClearPass);
			Graphics.Blit(null, m_MoonTexture, m_MoonMaterial, m_LitPass);
			m_MoonTexture.IncrementUpdateCount();
			m_MoonLight.additionalData.surfaceTexture = m_MoonTexture;
		}
	}

	public void SetDefaults(Context context)
	{
		m_Latitude = kDefaultLatitude;
		m_Longitude = kDefaultLongitude;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float value = m_Latitude;
		writer.Write(value);
		float value2 = m_Longitude;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float value = ref m_Latitude;
		reader.Read(out value);
		ref float value2 = ref m_Longitude;
		reader.Read(out value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeSettingsData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1383560598_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1383560598_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<AtmosphereData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1383560598_2 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public PlanetarySystem()
	{
	}
}
