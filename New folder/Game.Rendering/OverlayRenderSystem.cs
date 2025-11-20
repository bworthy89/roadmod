using System;
using System.Collections.Generic;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;
using UnityEngine.TextCore.LowLevel;

namespace Game.Rendering;

public class OverlayRenderSystem : GameSystemBase
{
	public struct CurveData
	{
		public Matrix4x4 m_Matrix;

		public Matrix4x4 m_InverseMatrix;

		public Matrix4x4 m_Curve;

		public Color m_OutlineColor;

		public Color m_FillColor;

		public float2 m_Size;

		public float2 m_DashLengths;

		public float2 m_Roundness;

		public float m_OutlineWidth;

		public float m_FillStyle;

		public float m_DepthFadeStyle;
	}

	public enum CustomMeshType
	{
		Cylinder,
		Arrow,
		Plane,
		NumCustomMeshTypes
	}

	public struct CustomMeshdData
	{
		public Matrix4x4 m_Matrix;

		public Matrix4x4 m_InverseMatrix;

		public Color m_FillColor;

		public float2 m_Size;

		public int m_CustomMeshType;
	}

	public struct TextMeshData
	{
		public Matrix4x4 m_Matrix;

		public int m_textId;

		public bool m_cameraFacing;
	}

	public struct BoundsData
	{
		public Bounds3 m_CurveBounds;
	}

	[Flags]
	public enum StyleFlags
	{
		Grid = 1,
		Projected = 2,
		DepthFadeBelow = 4
	}

	public struct Buffer
	{
		private NativeList<CurveData> m_ProjectedCurves;

		private NativeList<CurveData> m_AbsoluteCurves;

		private NativeList<CustomMeshdData> m_CustomMeshes;

		private NativeList<TextMeshData> m_TextMeshes;

		private NativeValue<BoundsData> m_Bounds;

		private float m_PositionY;

		private float m_ScaleY;

		public Buffer(NativeList<CurveData> projectedCurves, NativeList<CurveData> absoluteCurves, NativeList<CustomMeshdData> custMeshesData, NativeList<TextMeshData> textMeshData, NativeValue<BoundsData> bounds, float positionY, float scaleY)
		{
			m_ProjectedCurves = projectedCurves;
			m_AbsoluteCurves = absoluteCurves;
			m_CustomMeshes = custMeshesData;
			m_TextMeshes = textMeshData;
			m_Bounds = bounds;
			m_PositionY = positionY;
			m_ScaleY = scaleY;
		}

		public void DrawCircle(Color color, float3 position, float diameter)
		{
			DrawCircleImpl(color, color, 0f, (StyleFlags)0, new float2(0f, 1f), position, diameter);
		}

		public void DrawCircle(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, float2 direction, float3 position, float diameter)
		{
			DrawCircleImpl(outlineColor, fillColor, outlineWidth, styleFlags, direction, position, diameter);
		}

		public void DrawCustomMesh(Color fillColor, float3 position, float height, float width, CustomMeshType meshType)
		{
			DrawCustomMesh(fillColor, position, height, width, meshType, Quaternion.identity);
		}

		public void DrawCustomMesh(Color fillColor, float3 position, float height, float width, CustomMeshType meshType, Quaternion rot)
		{
			CustomMeshdData value = default(CustomMeshdData);
			value.m_Size = new float2(height, width);
			value.m_FillColor = fillColor.linear;
			value.m_Matrix = Matrix4x4.TRS(position, rot, new float3(width, height, width));
			value.m_InverseMatrix = value.m_Matrix.inverse;
			value.m_CustomMeshType = (int)meshType;
			m_CustomMeshes.Add(in value);
		}

		private void DrawCircleImpl(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, float2 direction, float3 position, float diameter)
		{
			CurveData value = default(CurveData);
			value.m_Size = new float2(diameter, diameter);
			value.m_DashLengths = new float2(0f, diameter);
			value.m_Roundness = new float2(1f, 1f);
			value.m_OutlineWidth = outlineWidth;
			value.m_FillStyle = (float)(styleFlags & StyleFlags.Grid);
			value.m_DepthFadeStyle = (float)(styleFlags & StyleFlags.DepthFadeBelow);
			value.m_Curve = new Matrix4x4(new float4(position, 0f), new float4(position, 0f), new float4(position, 0f), new float4(position, 0f));
			value.m_OutlineColor = outlineColor.linear;
			value.m_FillColor = fillColor.linear;
			Bounds3 bounds;
			if ((styleFlags & StyleFlags.Projected) != 0)
			{
				value.m_Matrix = FitBox(direction, position, diameter, out bounds);
				value.m_InverseMatrix = value.m_Matrix.inverse;
				m_ProjectedCurves.Add(in value);
			}
			else
			{
				value.m_Matrix = FitQuad(direction, position, diameter, out bounds);
				value.m_InverseMatrix = value.m_Matrix.inverse;
				m_AbsoluteCurves.Add(in value);
			}
			BoundsData value2 = m_Bounds.value;
			value2.m_CurveBounds |= bounds;
			m_Bounds.value = value2;
		}

		public void DrawText(int stringId, float3 pos, bool cameraFace)
		{
			ref NativeList<TextMeshData> textMeshes = ref m_TextMeshes;
			TextMeshData value = new TextMeshData
			{
				m_Matrix = float4x4.TRS(pos, quaternion.identity, new float3(1f)),
				m_textId = stringId,
				m_cameraFacing = cameraFace
			};
			textMeshes.Add(in value);
		}

		public void DrawLine(Color color, Line3.Segment line, float width, bool cameraFacing = false)
		{
			float num = MathUtils.Length(line.xz);
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, NetUtils.StraightCurve(line.a, line.b), width, num + width * 2f, 0f, default(float2), num, cameraFacing);
		}

		public void DrawLine(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Line3.Segment line, float width, float2 roundness)
		{
			float num = MathUtils.Length(line.xz);
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, NetUtils.StraightCurve(line.a, line.b), width, num + width * 2f, 0f, roundness, num);
		}

		public void DrawDashedLine(Color color, Line3.Segment line, float width, float dashLength, float gapLength)
		{
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, NetUtils.StraightCurve(line.a, line.b), width, dashLength, gapLength, default(float2), MathUtils.Length(line.xz));
		}

		public void DrawDashedLine(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Line3.Segment line, float width, float dashLength, float gapLength)
		{
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, NetUtils.StraightCurve(line.a, line.b), width, dashLength, gapLength, default(float2), MathUtils.Length(line.xz));
		}

		public void DrawDashedLine(Color color, Line3.Segment line, float width, float dashLength, float gapLength, float2 roundness)
		{
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, NetUtils.StraightCurve(line.a, line.b), width, dashLength, gapLength, roundness, MathUtils.Length(line.xz));
		}

		public void DrawDashedLine(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Line3.Segment line, float width, float dashLength, float gapLength, float2 roundness)
		{
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, NetUtils.StraightCurve(line.a, line.b), width, dashLength, gapLength, roundness, MathUtils.Length(line.xz));
		}

		public void DrawCurve(Color color, Bezier4x3 curve, float width)
		{
			float num = MathUtils.Length(curve.xz);
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, curve, width, num + width * 2f, 0f, default(float2), num);
		}

		public void DrawCurve(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Bezier4x3 curve, float width)
		{
			float num = MathUtils.Length(curve.xz);
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, curve, width, num + width * 2f, 0f, default(float2), num);
		}

		public void DrawCurve(Color color, Bezier4x3 curve, float width, float2 roundness)
		{
			float num = MathUtils.Length(curve.xz);
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, curve, width, num + width * 2f, 0f, roundness, num);
		}

		public void DrawCurve(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Bezier4x3 curve, float width, float2 roundness)
		{
			float num = MathUtils.Length(curve.xz);
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, curve, width, num + width * 2f, 0f, roundness, num);
		}

		public void DrawDashedCurve(Color color, Bezier4x3 curve, float width, float dashLength, float gapLength)
		{
			DrawCurveImpl(color, color, 0f, (StyleFlags)0, curve, width, dashLength, gapLength, default(float2), MathUtils.Length(curve.xz));
		}

		public void DrawDashedCurve(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Bezier4x3 curve, float width, float dashLength, float gapLength)
		{
			DrawCurveImpl(outlineColor, fillColor, outlineWidth, styleFlags, curve, width, dashLength, gapLength, default(float2), MathUtils.Length(curve.xz));
		}

		private void DrawCurveImpl(Color outlineColor, Color fillColor, float outlineWidth, StyleFlags styleFlags, Bezier4x3 curve, float width, float dashLength, float gapLength, float2 roundness, float length, bool cameraFacing = false)
		{
			if (!(length < 0.01f))
			{
				CurveData value = default(CurveData);
				value.m_Size = new float2(width, length);
				value.m_DashLengths = new float2(gapLength, dashLength);
				value.m_Roundness = roundness;
				value.m_OutlineWidth = outlineWidth;
				value.m_FillStyle = (float)(styleFlags & StyleFlags.Grid);
				value.m_DepthFadeStyle = (float)(styleFlags & StyleFlags.DepthFadeBelow);
				value.m_Curve = BuildCurveMatrix(curve, length);
				value.m_OutlineColor = outlineColor.linear;
				value.m_FillColor = fillColor.linear;
				Bounds3 bounds;
				if ((styleFlags & StyleFlags.Projected) != 0)
				{
					value.m_Matrix = FitBox(curve, width, out bounds);
					value.m_InverseMatrix = value.m_Matrix.inverse;
					m_ProjectedCurves.Add(in value);
				}
				else
				{
					value.m_Matrix = FitQuad(curve, width, out bounds, cameraFacing);
					value.m_InverseMatrix = value.m_Matrix.inverse;
					m_AbsoluteCurves.Add(in value);
				}
				BoundsData value2 = m_Bounds.value;
				value2.m_CurveBounds |= bounds;
				m_Bounds.value = value2;
			}
		}

		private Matrix4x4 FitBox(float2 direction, float3 position, float extend, out Bounds3 bounds)
		{
			bounds = new Bounds3(position, position);
			bounds.min.xz -= extend;
			bounds.max.xz += extend;
			bounds.min.y = m_PositionY;
			bounds.max.y = m_ScaleY;
			position.y = m_PositionY;
			quaternion quaternion = quaternion.RotateY(math.atan2(direction.x, direction.y));
			return Matrix4x4.TRS(s: new float3(extend, m_ScaleY, extend), pos: position, q: quaternion);
		}

		private Matrix4x4 FitQuad(float2 direction, float3 position, float extend, out Bounds3 bounds)
		{
			bounds = new Bounds3(position, position);
			bounds.min.xz -= extend;
			bounds.max.xz += extend;
			quaternion quaternion = quaternion.RotateY(math.atan2(direction.x, direction.y));
			return Matrix4x4.TRS(s: new float3(extend, 1f, extend), pos: position, q: quaternion);
		}

		private Matrix4x4 FitBox(Bezier4x3 curve, float extend, out Bounds3 bounds)
		{
			bounds = MathUtils.Bounds(curve);
			bounds.min.xz -= extend;
			bounds.max.xz += extend;
			bounds.min.y = m_PositionY;
			bounds.max.y = m_ScaleY;
			float3 @float = new float3(0f, m_PositionY, 0f);
			quaternion identity = quaternion.identity;
			float3 float2 = new float3(0f, m_ScaleY, 0f);
			float2 value = curve.d.xz - curve.a.xz;
			if (MathUtils.TryNormalize(ref value))
			{
				float2 y = MathUtils.Right(value);
				float2 x = curve.b.xz - curve.a.xz;
				float2 x2 = curve.c.xz - curve.a.xz;
				float2 x3 = curve.d.xz - curve.a.xz;
				float2 y2 = new float2(math.dot(x, y), math.dot(x, value));
				float2 x4 = new float2(math.dot(x2, y), math.dot(x2, value));
				float2 y3 = new float2(math.dot(x3, y), math.dot(x3, value));
				float2 float3 = math.min(math.min(0f, y2), math.min(x4, y3));
				float2 float4 = math.max(math.max(0f, y2), math.max(x4, y3));
				float2 float5 = math.lerp(float3, float4, 0.5f);
				identity = quaternion.LookRotation(new float3(value.x, 0f, value.y), new float3(0f, 1f, 0f));
				@float.xz = curve.a.xz + math.rotate(identity, new float3(float5.x, 0f, float5.y)).xz;
				float2.xz = (float4 - float3) * 0.5f + extend;
			}
			else
			{
				@float.xz = MathUtils.Center(bounds.xz);
				identity = quaternion.identity;
				float2.xz = MathUtils.Extents(bounds.xz);
			}
			return Matrix4x4.TRS(@float, identity, float2);
		}

		private Matrix4x4 FitQuad(Bezier4x3 curve, float extend, out Bounds3 bounds, bool cameraFacing = false)
		{
			bounds = MathUtils.Bounds(curve);
			bounds.min.xz -= extend;
			bounds.max.xz += extend;
			float3 @float = MathUtils.Center(bounds);
			quaternion quaternion = quaternion.identity;
			float3 float2 = 0f;
			float2.xz = MathUtils.Extents(bounds.xz);
			float2.y = 1f;
			float3 float3 = curve.d - curve.a;
			float num = math.length(float3);
			if (num > 0.1f)
			{
				float3 /= num;
				float3 float4 = math.cross(float3, curve.b - curve.a);
				float3 float5 = math.cross(float3, curve.d - curve.c);
				float4 = math.select(float4, -float4, float4.y < 0f);
				float5 = math.select(float5, -float5, float5.y < 0f);
				float3 float6 = float4 + float5;
				float num2 = math.length(float6);
				float6 = math.lerp(new float3(0f, 1f, 0f), float6, math.saturate(num2 / num * 10f));
				float6 = math.normalize(float6);
				float3 value = math.cross(float6, float3);
				if (MathUtils.TryNormalize(ref value))
				{
					float3 x = curve.b - curve.a;
					float3 x2 = curve.c - curve.a;
					float3 x3 = curve.d - curve.a;
					float2 y = new float2(math.dot(x, value), math.dot(x, float3));
					float2 x4 = new float2(math.dot(x2, value), math.dot(x2, float3));
					float2 y2 = new float2(math.dot(x3, value), math.dot(x3, float3));
					float2 float7 = math.min(math.min(0f, y), math.min(x4, y2));
					float2 float8 = math.max(math.max(0f, y), math.max(x4, y2));
					float2 float9 = math.lerp(float7, float8, 0.5f);
					quaternion = quaternion.LookRotation(float3, float6);
					@float = curve.a + math.rotate(quaternion, new float3(float9.x, 0f, float9.y));
					float2.xz = (float8 - float7) * 0.5f + extend;
				}
			}
			return Matrix4x4.TRS(@float, quaternion, float2);
		}

		private static float4x4 BuildCurveMatrix(Bezier4x3 curve, float length)
		{
			float2 @float = default(float2);
			@float.x = math.distance(curve.a, curve.b);
			@float.y = math.distance(curve.c, curve.d);
			@float /= length;
			return new float4x4
			{
				c0 = new float4(curve.a, 0f),
				c1 = new float4(curve.b, @float.x),
				c2 = new float4(curve.c, 1f - @float.y),
				c3 = new float4(curve.d, 1f)
			};
		}
	}

	private RenderingSystem m_RenderingSystem;

	private TerrainSystem m_TerrainSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_SettingsQuery;

	private Mesh m_BoxMesh;

	private Mesh m_QuadMesh;

	private Mesh[] m_customMeshes = new Mesh[3];

	private Dictionary<int, Mesh> m_textMeshes = new Dictionary<int, Mesh>();

	private Dictionary<string, int> m_registeredTexts = new Dictionary<string, int>();

	private Material m_ProjectedMaterial;

	private Material m_AbsoluteMaterial;

	private Material m_textMaterial;

	private Material[] m_CustomMeshMaterial = new Material[3];

	private ComputeBuffer m_ArgsBuffer;

	private ComputeBuffer m_ProjectedBuffer;

	private ComputeBuffer m_AbsoluteBuffer;

	private ComputeBuffer[] m_CustomMeshBuffer = new ComputeBuffer[3];

	private List<uint> m_ArgsArray;

	private int m_ProjectedInstanceCount;

	private int m_AbsoluteInstanceCount;

	private int m_TextInstanceCount;

	private int[] m_CustomMeshInstanceCount = new int[3];

	private int m_CurveBufferID;

	private int m_CustomMeshBufferID;

	private int m_GradientScaleID;

	private int m_ScaleRatioAID;

	private int m_FaceDilateID;

	private NativeList<CurveData> m_ProjectedData;

	private NativeList<CurveData> m_AbsoluteData;

	private NativeList<TextMeshData> m_TextData;

	private NativeList<CustomMeshdData>[] m_CustomMeshData = new NativeList<CustomMeshdData>[3];

	private NativeList<CustomMeshdData> m_CustomMeshJobData;

	private NativeValue<BoundsData> m_BoundsData;

	private JobHandle m_BufferWriters;

	private TextMeshPro m_TextMesh;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<OverlayConfigurationData>());
		m_CurveBufferID = Shader.PropertyToID("colossal_OverlayCurveBuffer");
		m_CustomMeshBufferID = Shader.PropertyToID("colossal_OverlayCustomMeshBuffer");
		m_GradientScaleID = Shader.PropertyToID("_GradientScale");
		m_ScaleRatioAID = Shader.PropertyToID("_ScaleRatioA");
		m_FaceDilateID = Shader.PropertyToID("_FaceDilate");
		RenderPipelineManager.beginContextRendering += Render;
		GetTextMesh();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		RenderPipelineManager.beginContextRendering -= Render;
		if (m_BoxMesh != null)
		{
			UnityEngine.Object.Destroy(m_BoxMesh);
		}
		Mesh[] customMeshes = m_customMeshes;
		for (int i = 0; i < customMeshes.Length; i++)
		{
			UnityEngine.Object.Destroy(customMeshes[i]);
		}
		foreach (Mesh value in m_textMeshes.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		if (m_QuadMesh != null)
		{
			UnityEngine.Object.Destroy(m_QuadMesh);
		}
		if (m_ProjectedMaterial != null)
		{
			UnityEngine.Object.Destroy(m_ProjectedMaterial);
		}
		if (m_AbsoluteMaterial != null)
		{
			UnityEngine.Object.Destroy(m_AbsoluteMaterial);
		}
		Material[] customMeshMaterial = m_CustomMeshMaterial;
		foreach (Material material in customMeshMaterial)
		{
			if (material != null)
			{
				UnityEngine.Object.Destroy(material);
			}
		}
		if (m_ArgsBuffer != null)
		{
			m_ArgsBuffer.Release();
		}
		if (m_ProjectedBuffer != null)
		{
			m_ProjectedBuffer.Release();
		}
		if (m_AbsoluteBuffer != null)
		{
			m_AbsoluteBuffer.Release();
		}
		ComputeBuffer[] customMeshBuffer = m_CustomMeshBuffer;
		for (int i = 0; i < customMeshBuffer.Length; i++)
		{
			customMeshBuffer[i]?.Release();
		}
		if (m_ProjectedData.IsCreated)
		{
			m_ProjectedData.Dispose();
		}
		if (m_AbsoluteData.IsCreated)
		{
			m_AbsoluteData.Dispose();
		}
		if (m_TextData.IsCreated)
		{
			m_TextData.Dispose();
		}
		if (m_CustomMeshJobData.IsCreated)
		{
			m_CustomMeshJobData.Dispose();
		}
		NativeList<CustomMeshdData>[] customMeshData = m_CustomMeshData;
		for (int i = 0; i < customMeshData.Length; i++)
		{
			NativeList<CustomMeshdData> nativeList = customMeshData[i];
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}
		if (m_BoundsData.IsCreated)
		{
			m_BoundsData.Dispose();
		}
		if (m_TextMesh != null)
		{
			for (int j = 0; j < m_TextMesh.font.fallbackFontAssetTable.Count; j++)
			{
				UnityEngine.Object.Destroy(m_TextMesh.font.fallbackFontAssetTable[j]);
			}
			UnityEngine.Object.Destroy(m_TextMesh.font);
			UnityEngine.Object.Destroy(m_TextMesh.gameObject);
		}
		base.OnDestroy();
	}

	public Buffer GetBuffer(out JobHandle dependencies)
	{
		if (!m_ProjectedData.IsCreated)
		{
			m_ProjectedData = new NativeList<CurveData>(Allocator.Persistent);
		}
		if (!m_AbsoluteData.IsCreated)
		{
			m_AbsoluteData = new NativeList<CurveData>(Allocator.Persistent);
		}
		if (!m_TextData.IsCreated)
		{
			m_TextData = new NativeList<TextMeshData>(Allocator.Persistent);
		}
		if (!m_CustomMeshJobData.IsCreated)
		{
			m_CustomMeshJobData = new NativeList<CustomMeshdData>(Allocator.Persistent);
		}
		for (int i = 0; i < 3; i++)
		{
			if (!m_CustomMeshData[i].IsCreated)
			{
				m_CustomMeshData[i] = new NativeList<CustomMeshdData>(Allocator.Persistent);
			}
		}
		if (!m_BoundsData.IsCreated)
		{
			m_BoundsData = new NativeValue<BoundsData>(Allocator.Persistent);
		}
		dependencies = m_BufferWriters;
		return new Buffer(m_ProjectedData, m_AbsoluteData, m_CustomMeshJobData, m_TextData, m_BoundsData, m_TerrainSystem.heightScaleOffset.y - 50f, m_TerrainSystem.heightScaleOffset.x + 100f);
	}

	public void AddBufferWriter(JobHandle handle)
	{
		m_BufferWriters = JobHandle.CombineDependencies(m_BufferWriters, handle);
	}

	public TextMeshPro GetTextMesh()
	{
		if (m_TextMesh == null)
		{
			OverlayConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<OverlayConfigurationPrefab>(m_SettingsQuery);
			GameObject gameObject = new GameObject("TextMeshPro");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			m_TextMesh = gameObject.AddComponent<TextMeshPro>();
			m_TextMesh.font = CreateFont(singletonPrefab.m_FontInfos[0]);
			m_TextMesh.font.fallbackFontAssetTable = new List<TMP_FontAsset>(singletonPrefab.m_FontInfos.Length - 1);
			for (int i = 1; i < singletonPrefab.m_FontInfos.Length; i++)
			{
				m_TextMesh.font.fallbackFontAssetTable.Add(CreateFont(singletonPrefab.m_FontInfos[i]));
			}
			m_TextMesh.renderer.enabled = false;
		}
		return m_TextMesh;
	}

	private TMP_FontAsset CreateFont(FontInfo info)
	{
		TMP_FontAsset tMP_FontAsset = TMP_FontAsset.CreateFontAsset(info.m_Font, info.m_SamplingPointSize, info.m_AtlasPadding, GlyphRenderMode.SDFAA_HINTED, info.m_AtlasWidth, info.m_AtlasHeight);
		tMP_FontAsset.material.SetFloat(m_FaceDilateID, 1f);
		return tMP_FontAsset;
	}

	public void CopyFontAtlasParameters(Material source, Material target)
	{
		target.SetFloat(m_GradientScaleID, source.GetFloat(m_GradientScaleID) * 2f);
		target.SetFloat(m_ScaleRatioAID, source.GetFloat(m_ScaleRatioAID));
		target.mainTexture = source.mainTexture;
	}

	public int RegisterString(string text)
	{
		int num = -1;
		if (m_registeredTexts.TryGetValue(text, out var value))
		{
			return value;
		}
		num = m_registeredTexts.Count;
		m_registeredTexts.Add(text, num);
		GetOverlayTextMaterial(ref m_textMaterial);
		TextMeshPro textMesh = GetTextMesh();
		textMesh.rectTransform.sizeDelta = new Vector2(250f, 100f);
		textMesh.fontSize = 200f;
		textMesh.alignment = TextAlignmentOptions.Center;
		if (m_textMeshes.TryGetValue(num, out var value2))
		{
			UnityEngine.Object.Destroy(value2);
			m_textMeshes.Remove(num);
		}
		TMP_TextInfo textInfo = textMesh.GetTextInfo(text);
		for (int i = 0; i < textInfo.meshInfo.Length; i++)
		{
			TMP_MeshInfo tMP_MeshInfo = textInfo.meshInfo[i];
			CopyFontAtlasParameters(tMP_MeshInfo.material, m_textMaterial);
			if (tMP_MeshInfo.vertexCount != 0)
			{
				Vector3[] vertices = tMP_MeshInfo.vertices;
				Vector2[] uvs = tMP_MeshInfo.uvs0;
				Vector2[] uvs2 = tMP_MeshInfo.uvs2;
				Color32[] colors = tMP_MeshInfo.colors32;
				Mesh mesh = new Mesh();
				mesh.name = "TextMesh";
				mesh.vertices = vertices;
				mesh.triangles = tMP_MeshInfo.triangles;
				mesh.uv = uvs;
				mesh.uv2 = uvs2;
				mesh.colors32 = colors;
				mesh.RecalculateBounds();
				m_textMeshes.Add(num, mesh);
			}
		}
		return num;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_BufferWriters.Complete();
		m_BufferWriters = default(JobHandle);
		m_ProjectedInstanceCount = 0;
		m_AbsoluteInstanceCount = 0;
		m_TextInstanceCount = 0;
		for (int i = 0; i < 3; i++)
		{
			m_CustomMeshInstanceCount[i] = 0;
		}
		if ((!m_ProjectedData.IsCreated || m_ProjectedData.Length == 0) && (!m_AbsoluteData.IsCreated || m_AbsoluteData.Length == 0))
		{
			return;
		}
		if (m_SettingsQuery.IsEmptyIgnoreFilter)
		{
			if (m_ProjectedData.IsCreated)
			{
				m_ProjectedData.Clear();
			}
			if (m_AbsoluteData.IsCreated)
			{
				m_AbsoluteData.Clear();
			}
			NativeList<CustomMeshdData>[] customMeshData = m_CustomMeshData;
			for (int j = 0; j < customMeshData.Length; j++)
			{
				NativeList<CustomMeshdData> nativeList = customMeshData[j];
				if (nativeList.IsCreated)
				{
					nativeList.Clear();
				}
			}
			return;
		}
		if (m_ProjectedData.IsCreated && m_ProjectedData.Length != 0)
		{
			m_ProjectedInstanceCount = m_ProjectedData.Length;
			GetCurveMaterial(ref m_ProjectedMaterial, projected: true);
			GetCurveBuffer(ref m_ProjectedBuffer, m_ProjectedInstanceCount);
			m_ProjectedBuffer.SetData(m_ProjectedData.AsArray(), 0, 0, m_ProjectedInstanceCount);
			m_ProjectedMaterial.SetBuffer(m_CurveBufferID, m_ProjectedBuffer);
			m_ProjectedData.Clear();
		}
		if (m_AbsoluteData.IsCreated && m_AbsoluteData.Length != 0)
		{
			m_AbsoluteInstanceCount = m_AbsoluteData.Length;
			GetCurveMaterial(ref m_AbsoluteMaterial, projected: false);
			GetCurveBuffer(ref m_AbsoluteBuffer, m_AbsoluteInstanceCount);
			m_AbsoluteBuffer.SetData(m_AbsoluteData.AsArray(), 0, 0, m_AbsoluteInstanceCount);
			m_AbsoluteMaterial.SetBuffer(m_CurveBufferID, m_AbsoluteBuffer);
			m_AbsoluteData.Clear();
		}
		if (m_TextData.IsCreated && m_TextData.Length != 0)
		{
			m_TextInstanceCount = m_TextData.Length;
		}
		foreach (CustomMeshdData customMeshJobDatum in m_CustomMeshJobData)
		{
			CustomMeshdData value = customMeshJobDatum;
			m_CustomMeshData[value.m_CustomMeshType].Add(in value);
		}
		m_CustomMeshJobData.Clear();
		for (int k = 0; k < 3; k++)
		{
			if (m_CustomMeshData[k].IsCreated && m_CustomMeshData[k].Length != 0)
			{
				bool value2 = k == 2;
				m_CustomMeshInstanceCount[k] = m_CustomMeshData[k].Length;
				GetSolidObjectMaterial(ref m_CustomMeshMaterial[k]);
				GetCustomMeshBuffer(ref m_CustomMeshBuffer[k], m_CustomMeshInstanceCount[k]);
				m_CustomMeshBuffer[k].SetData(m_CustomMeshData[k].AsArray(), 0, 0, m_CustomMeshInstanceCount[k]);
				m_CustomMeshMaterial[k].SetBuffer(m_CustomMeshBufferID, m_CustomMeshBuffer[k]);
				m_CustomMeshMaterial[k].SetFloat("_TransparentSortPriority", k);
				LocalKeyword keyword = new LocalKeyword(m_CustomMeshMaterial[k].shader, "DEPTH_FADE_TERRAIN_EDGE");
				m_CustomMeshMaterial[k].SetKeyword(in keyword, value2);
				HDMaterial.ValidateMaterial(m_CustomMeshMaterial[k]);
				m_CustomMeshData[k].Clear();
			}
		}
	}

	private void Render(ScriptableRenderContext context, List<Camera> cameras)
	{
		try
		{
			if (m_RenderingSystem.hideOverlay)
			{
				return;
			}
			int num = 0;
			if (m_ProjectedInstanceCount != 0)
			{
				num += 5;
			}
			if (m_AbsoluteInstanceCount != 0)
			{
				num += 5;
			}
			int[] customMeshInstanceCount = m_CustomMeshInstanceCount;
			for (int i = 0; i < customMeshInstanceCount.Length; i++)
			{
				if (customMeshInstanceCount[i] != 0)
				{
					num += 5;
				}
			}
			if (num == 0)
			{
				return;
			}
			if (m_ArgsBuffer != null && m_ArgsBuffer.count < num)
			{
				m_ArgsBuffer.Release();
				m_ArgsBuffer = null;
			}
			if (m_ArgsBuffer == null)
			{
				m_ArgsBuffer = new ComputeBuffer(num, 4, ComputeBufferType.DrawIndirect);
				m_ArgsBuffer.name = "Overlay args buffer";
			}
			if (m_ArgsArray == null)
			{
				m_ArgsArray = new List<uint>();
			}
			m_ArgsArray.Clear();
			Bounds bounds = RenderingUtils.ToBounds(m_BoundsData.value.m_CurveBounds);
			int num2 = 0;
			int num3 = 0;
			int[] array = new int[3];
			if (m_ProjectedInstanceCount != 0)
			{
				GetMesh(ref m_BoxMesh, box: true);
				GetCurveMaterial(ref m_ProjectedMaterial, projected: true);
				num2 = m_ArgsArray.Count;
				m_ArgsArray.Add(m_BoxMesh.GetIndexCount(0));
				m_ArgsArray.Add((uint)m_ProjectedInstanceCount);
				m_ArgsArray.Add(m_BoxMesh.GetIndexStart(0));
				m_ArgsArray.Add(m_BoxMesh.GetBaseVertex(0));
				m_ArgsArray.Add(0u);
			}
			if (m_AbsoluteInstanceCount != 0)
			{
				GetMesh(ref m_QuadMesh, box: false);
				GetCurveMaterial(ref m_AbsoluteMaterial, projected: false);
				num3 = m_ArgsArray.Count;
				m_ArgsArray.Add(m_QuadMesh.GetIndexCount(0));
				m_ArgsArray.Add((uint)m_AbsoluteInstanceCount);
				m_ArgsArray.Add(m_QuadMesh.GetIndexStart(0));
				m_ArgsArray.Add(m_QuadMesh.GetBaseVertex(0));
				m_ArgsArray.Add(0u);
			}
			for (int j = 0; j < 3; j++)
			{
				if (m_CustomMeshInstanceCount[j] != 0)
				{
					GetCustomMeshMesh(ref m_customMeshes[j], (CustomMeshType)j);
					GetSolidObjectMaterial(ref m_CustomMeshMaterial[j]);
					array[j] = m_ArgsArray.Count;
					m_ArgsArray.Add(m_customMeshes[j].GetIndexCount(0));
					m_ArgsArray.Add((uint)m_CustomMeshInstanceCount[j]);
					m_ArgsArray.Add(m_customMeshes[j].GetIndexStart(0));
					m_ArgsArray.Add(m_customMeshes[j].GetBaseVertex(0));
					m_ArgsArray.Add(0u);
				}
			}
			GetOverlayTextMaterial(ref m_textMaterial);
			foreach (Camera camera in cameras)
			{
				if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
				{
					continue;
				}
				if (m_ProjectedInstanceCount != 0)
				{
					Graphics.DrawMeshInstancedIndirect(m_BoxMesh, 0, m_ProjectedMaterial, bounds, m_ArgsBuffer, num2 * 4, null, ShadowCastingMode.Off, receiveShadows: false, 0, camera);
				}
				if (m_AbsoluteInstanceCount != 0)
				{
					Graphics.DrawMeshInstancedIndirect(m_QuadMesh, 0, m_AbsoluteMaterial, bounds, m_ArgsBuffer, num3 * 4, null, ShadowCastingMode.Off, receiveShadows: false, 0, camera);
				}
				for (int k = 0; k < 3; k++)
				{
					if (m_CustomMeshInstanceCount[k] != 0)
					{
						Graphics.DrawMeshInstancedIndirect(m_customMeshes[k], 0, m_CustomMeshMaterial[k], bounds, m_ArgsBuffer, array[k] * 4, null, ShadowCastingMode.Off, receiveShadows: false, 0, camera);
					}
				}
				if (m_TextInstanceCount <= 0)
				{
					continue;
				}
				foreach (TextMeshData textDatum in m_TextData)
				{
					if (m_textMeshes.ContainsKey(textDatum.m_textId))
					{
						Matrix4x4 matrix = textDatum.m_Matrix;
						if (textDatum.m_cameraFacing)
						{
							Matrix4x4 matrix2 = textDatum.m_Matrix;
							matrix = Matrix4x4.TRS(matrix2.GetPosition(), Quaternion.LookRotation(camera.transform.forward, camera.transform.up), Vector3.one);
						}
						Graphics.DrawMesh(m_textMeshes[textDatum.m_textId], matrix, m_textMaterial, 0, camera, 0, null, castShadows: false, receiveShadows: false);
					}
				}
				m_TextData.Clear();
			}
			m_ArgsBuffer.SetData(m_ArgsArray, 0, 0, m_ArgsArray.Count);
		}
		finally
		{
		}
	}

	private void GetCustomMeshMesh(ref Mesh mesh, CustomMeshType meshType)
	{
		if (mesh != null)
		{
			return;
		}
		switch (meshType)
		{
		case CustomMeshType.Cylinder:
		{
			int num = 64;
			mesh = new Mesh();
			Vector3[] array = new Vector3[num * 2];
			int[] array2 = new int[num * 6];
			float num2 = 360f / (float)num;
			for (int i = 0; i < num; i++)
			{
				float f = MathF.PI / 180f * (float)i * num2;
				float x = Mathf.Sin(f);
				float z = Mathf.Cos(f);
				array[i] = new Vector3(x, 0.5f, z);
				array[i + num] = new Vector3(x, -0.5f, z);
				int num3 = i * 6;
				int num4 = (i + 1) % num;
				array2[num3] = i;
				array2[num3 + 1] = i + num;
				array2[num3 + 2] = num4 + num;
				array2[num3 + 3] = i;
				array2[num3 + 4] = num4 + num;
				array2[num3 + 5] = num4;
			}
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.RecalculateNormals();
			break;
		}
		case CustomMeshType.Plane:
		{
			mesh = new Mesh();
			Vector3[] array = new Vector3[4];
			int[] array2 = new int[6];
			array[0] = new Vector3(-0.5f, 0f, 0.5f);
			array[1] = new Vector3(0.5f, 0f, 0.5f);
			array[2] = new Vector3(0.5f, 0f, -0.5f);
			array[3] = new Vector3(-0.5f, 0f, -0.5f);
			array2[0] = 0;
			array2[1] = 1;
			array2[2] = 2;
			array2[3] = 2;
			array2[4] = 3;
			array2[5] = 0;
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.RecalculateNormals();
			break;
		}
		case CustomMeshType.Arrow:
			GetArrowMesh(ref mesh);
			break;
		}
	}

	private void GetArrowMesh(ref Mesh mesh)
	{
		if (!(mesh != null))
		{
			mesh = new Mesh();
			float num = 0.5f;
			float num2 = 1.5f;
			float num3 = 2f;
			float num4 = 1f;
			Vector3[] vertices = new Vector3[7]
			{
				new Vector3((0f - num) * 0.5f, 0f, 0f),
				new Vector3(num * 0.5f, 0f, 0f),
				new Vector3(num * 0.5f, num3, 0f),
				new Vector3((0f - num) * 0.5f, num3, 0f),
				new Vector3(num2 * 0.5f, num3, 0f),
				new Vector3((0f - num2) * 0.5f, num3, 0f),
				new Vector3(0f, num3 + num4, 0f)
			};
			int[] triangles = new int[15]
			{
				0, 3, 1, 1, 3, 2, 3, 5, 6, 2,
				3, 6, 4, 2, 6
			};
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
		}
	}

	private void GetMesh(ref Mesh mesh, bool box, bool cameraFacing = true)
	{
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.name = "Overlay";
			if (box)
			{
				mesh.vertices = new Vector3[8]
				{
					new Vector3(-1f, 0f, -1f),
					new Vector3(-1f, 0f, 1f),
					new Vector3(1f, 0f, 1f),
					new Vector3(1f, 0f, -1f),
					new Vector3(-1f, 1f, -1f),
					new Vector3(-1f, 1f, 1f),
					new Vector3(1f, 1f, 1f),
					new Vector3(1f, 1f, -1f)
				};
				mesh.triangles = new int[36]
				{
					0, 1, 5, 5, 4, 0, 3, 7, 6, 6,
					2, 3, 0, 3, 2, 2, 1, 0, 4, 5,
					6, 6, 7, 4, 0, 4, 7, 7, 3, 0,
					1, 2, 6, 6, 5, 1
				};
			}
			else
			{
				mesh.vertices = new Vector3[4]
				{
					new Vector3(-1f, 0f, -1f),
					new Vector3(-1f, 0f, 1f),
					new Vector3(1f, 0f, 1f),
					new Vector3(1f, 0f, -1f)
				};
				mesh.triangles = new int[12]
				{
					0, 3, 2, 2, 1, 0, 0, 1, 2, 2,
					3, 0
				};
			}
		}
	}

	private void GetCurveMaterial(ref Material material, bool projected)
	{
		if (material == null)
		{
			OverlayConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<OverlayConfigurationPrefab>(m_SettingsQuery);
			material = new Material(singletonPrefab.m_CurveMaterial);
			material.name = "Overlay curves";
			if (projected)
			{
				material.EnableKeyword("PROJECTED_MODE");
			}
		}
	}

	private void GetSolidObjectMaterial(ref Material material)
	{
		if (material == null)
		{
			OverlayConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<OverlayConfigurationPrefab>(m_SettingsQuery);
			material = new Material(singletonPrefab.m_SolidObjectMaterial);
			material.name = "Overlay Object";
		}
	}

	private void GetOverlayTextMaterial(ref Material material)
	{
		if (material == null)
		{
			OverlayConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<OverlayConfigurationPrefab>(m_SettingsQuery);
			material = new Material(singletonPrefab.m_TextMaterial);
			material.name = "Overlay Text";
		}
	}

	private unsafe void GetCurveBuffer(ref ComputeBuffer buffer, int count)
	{
		if (buffer != null && buffer.count < count)
		{
			count = math.max(buffer.count * 2, count);
			buffer.Release();
			buffer = null;
		}
		if (buffer == null)
		{
			buffer = new ComputeBuffer(math.max(64, count), sizeof(CurveData));
			buffer.name = "Overlay curve buffer";
		}
	}

	private unsafe void GetCustomMeshBuffer(ref ComputeBuffer buffer, int count)
	{
		if (buffer != null && buffer.count < count)
		{
			count = math.max(buffer.count * 2, count);
			buffer.Release();
			buffer = null;
		}
		if (buffer == null)
		{
			buffer = new ComputeBuffer(math.max(64, count), sizeof(CustomMeshdData));
			buffer.name = "Overlay cylinder buffer";
		}
	}

	[Preserve]
	public OverlayRenderSystem()
	{
	}
}
