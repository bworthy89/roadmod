using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class BuildingLotRenderSystem : GameSystemBase
{
	[BurstCompile]
	private struct BuildingLotRenderJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Extension> m_ExtensionType;

		[ReadOnly]
		public ComponentTypeHandle<AssetStamp> m_AssetStampType;

		[ReadOnly]
		public ComponentTypeHandle<Tree> m_TreeType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> m_StartGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> m_EndGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Warning> m_WarningType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_PrefabAssetStampData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<GateData> m_PrefabGateData;

		[ReadOnly]
		public BufferLookup<ServiceUpgradeBuilding> m_PrefabServiceUpgradeBuildings;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_ZonesVisible;

		[ReadOnly]
		public RenderingSettingsData m_RenderingSettingsData;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
			bool checkTempFlags;
			bool flag;
			UnityEngine.Color lotColor;
			if (chunk.Has(ref m_ErrorType))
			{
				checkTempFlags = false;
				flag = true;
				lotColor = m_RenderingSettingsData.m_ErrorColor;
				lotColor.a = 0.5f;
			}
			else if (chunk.Has(ref m_WarningType))
			{
				checkTempFlags = false;
				flag = true;
				lotColor = m_RenderingSettingsData.m_WarningColor;
				lotColor.a = 0.5f;
			}
			else if (nativeArray.Length != 0)
			{
				checkTempFlags = true;
				flag = m_EditorMode || !chunk.Has(ref m_TreeType) || !chunk.Has(ref m_OwnerType);
				lotColor = m_RenderingSettingsData.m_HoveredColor;
				lotColor.a = 0.25f;
			}
			else
			{
				checkTempFlags = false;
				flag = false;
				lotColor = default(UnityEngine.Color);
			}
			if (!flag)
			{
				return;
			}
			NativeArray<Composition> nativeArray2 = chunk.GetNativeArray(ref m_CompositionType);
			if (nativeArray2.Length != 0)
			{
				DrawNets(in chunk, nativeArray2, nativeArray, lotColor, checkTempFlags);
				return;
			}
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			if (nativeArray3.Length != 0)
			{
				DrawObjects(in chunk, nativeArray3, nativeArray, lotColor, checkTempFlags);
			}
		}

		private void DrawNets(in ArchetypeChunk chunk, NativeArray<Composition> compositions, NativeArray<Temp> temps, UnityEngine.Color lotColor, bool checkTempFlags)
		{
			NativeArray<Edge> nativeArray = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<EdgeGeometry> nativeArray2 = chunk.GetNativeArray(ref m_EdgeGeometryType);
			NativeArray<StartNodeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_StartGeometryType);
			NativeArray<EndNodeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_EndGeometryType);
			for (int i = 0; i < compositions.Length; i++)
			{
				Composition composition = compositions[i];
				NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
				if ((netCompositionData.m_State & CompositionState.Airspace) == 0 || (netCompositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0)
				{
					continue;
				}
				Edge edge = nativeArray[i];
				EdgeGeometry edgeGeometry = nativeArray2[i];
				UnityEngine.Color color = lotColor;
				if (checkTempFlags)
				{
					Temp temp = temps[i];
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0)
					{
						continue;
					}
					if ((temp.m_Flags & TempFlags.Parent) != 0)
					{
						color = m_RenderingSettingsData.m_OwnerColor;
						color.a = 0.25f;
					}
				}
				DrawSegment(color, edgeGeometry.m_Start);
				DrawSegment(color, edgeGeometry.m_End);
				if (m_NetElevationData.TryGetComponent(edge.m_Start, out var componentData) && math.all(componentData.m_Elevation > 0f))
				{
					EdgeNodeGeometry geometry = nativeArray3[i].m_Geometry;
					geometry.m_Left.m_Right = geometry.m_Right.m_Right;
					DrawSegment(color, geometry.m_Left);
				}
				if (m_NetElevationData.TryGetComponent(edge.m_End, out var componentData2) && math.all(componentData2.m_Elevation > 0f))
				{
					EdgeNodeGeometry geometry2 = nativeArray4[i].m_Geometry;
					geometry2.m_Left.m_Right = geometry2.m_Right.m_Right;
					DrawSegment(color, geometry2.m_Left);
				}
			}
		}

		private void DrawSegment(UnityEngine.Color color, Segment segment)
		{
			UnityEngine.Color color2 = new UnityEngine.Color(color.r, color.g, color.b, math.select(color.a, 1f, !m_EditorMode && m_ZonesVisible));
			float num = MathUtils.Length(segment.m_Left.xz) / 32f;
			float num2 = MathUtils.Length(segment.m_Right.xz) / 32f;
			float num3 = num / math.max(1f, math.round(num)) * 16f;
			float num4 = num2 / math.max(1f, math.round(num2)) * 16f;
			if (num > 0.5f)
			{
				m_OverlayBuffer.DrawDashedCurve(color2, segment.m_Left, 4f, num3, num3);
			}
			if (num2 > 0.5f)
			{
				m_OverlayBuffer.DrawDashedCurve(color2, segment.m_Right, 4f, num4, num4);
			}
		}

		private void DrawObjects(in ArchetypeChunk chunk, NativeArray<Game.Objects.Transform> transforms, NativeArray<Temp> temps, UnityEngine.Color lotColor, bool checkTempFlags)
		{
			bool flag = chunk.Has(ref m_BuildingType);
			bool flag2 = chunk.Has(ref m_ExtensionType);
			bool flag3 = chunk.Has(ref m_AssetStampType);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Objects.Transform transform = transforms[i];
				Entity prefab = nativeArray[i].m_Prefab;
				UnityEngine.Color color = lotColor;
				if (checkTempFlags)
				{
					Temp temp = temps[i];
					if ((temp.m_Flags & TempFlags.Hidden) != 0 || (temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0)
					{
						continue;
					}
					if ((temp.m_Flags & TempFlags.Parent) != 0)
					{
						color = m_RenderingSettingsData.m_OwnerColor;
						color.a = 0.25f;
					}
				}
				if (flag2)
				{
					BuildingExtensionData buildingExtensionData = m_PrefabBuildingExtensionData[prefab];
					bool flag4 = false;
					if (m_EditorMode && m_PrefabServiceUpgradeBuildings.HasBuffer(prefab))
					{
						DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer = m_PrefabServiceUpgradeBuildings[prefab];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							Entity building = dynamicBuffer[j].m_Building;
							if (m_PrefabBuildingData.HasComponent(building))
							{
								BuildingData buildingData = m_PrefabBuildingData[building];
								ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[building];
								float3 position = transform.m_Position - math.mul(transform.m_Rotation, buildingExtensionData.m_Position);
								DrawLot(color, buildingData.m_LotSize, objectGeometryData, position, transform.m_Rotation);
								flag4 = true;
							}
						}
					}
					if (!flag4 && (m_EditorMode || buildingExtensionData.m_External))
					{
						ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefab];
						DrawLot(color, buildingExtensionData.m_LotSize, objectGeometryData2, transform.m_Position, transform.m_Rotation);
					}
				}
				else if (flag3)
				{
					if (m_EditorMode)
					{
						AssetStampData assetStampData = m_PrefabAssetStampData[prefab];
						ObjectGeometryData objectGeometryData3 = m_PrefabObjectGeometryData[prefab];
						DrawLot(color, assetStampData.m_Size, objectGeometryData3, transform.m_Position, transform.m_Rotation);
					}
				}
				else if (flag)
				{
					BuildingData buildingData2 = m_PrefabBuildingData[prefab];
					ObjectGeometryData objectGeometryData4 = m_PrefabObjectGeometryData[prefab];
					if ((objectGeometryData4.m_Flags & Game.Objects.GeometryFlags.ExclusiveGround) != Game.Objects.GeometryFlags.None)
					{
						DrawLot(color, buildingData2.m_LotSize, objectGeometryData4, transform.m_Position, transform.m_Rotation);
					}
					if (!m_EditorMode && m_PrefabGateData.HasComponent(prefab))
					{
						DrawArrow(color, buildingData2.m_LotSize, objectGeometryData4, transform.m_Position, transform.m_Rotation);
					}
				}
				else
				{
					ObjectGeometryData objectGeometryData5 = m_PrefabObjectGeometryData[prefab];
					DrawCollision(color, objectGeometryData5, transform.m_Position, transform.m_Rotation, 2f);
				}
			}
		}

		private void DrawLot(UnityEngine.Color color, int2 lotSize, ObjectGeometryData objectGeometryData, float3 position, quaternion rotation)
		{
			UnityEngine.Color color2;
			UnityEngine.Color fillColor;
			OverlayRenderSystem.StyleFlags styleFlags;
			float outlineWidth;
			float num;
			if (m_EditorMode)
			{
				color2 = color;
				fillColor = new UnityEngine.Color(color.r, color.g, color.b, 0f);
				styleFlags = OverlayRenderSystem.StyleFlags.Grid | OverlayRenderSystem.StyleFlags.Projected;
				outlineWidth = 0.1f;
				num = 0.1f;
			}
			else
			{
				color2 = new UnityEngine.Color(color.r, color.g, color.b, math.select(color.a, 1f, m_ZonesVisible));
				fillColor = new UnityEngine.Color(color.r, color.g, color.b, color2.a * 0.2f);
				styleFlags = OverlayRenderSystem.StyleFlags.Projected;
				outlineWidth = math.select(0.2f, 0.4f, m_ZonesVisible);
				num = 0.5f;
			}
			bool flag;
			float2 @float;
			if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
			{
				flag = (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.CircularLeg) != 0;
				@float = math.min((float2)lotSize * 8f, objectGeometryData.m_LegSize.xz + objectGeometryData.m_LegOffset * 2f);
				if (math.any(objectGeometryData.m_Bounds.min.xz < @float * -0.5f) || math.any(objectGeometryData.m_Bounds.max.xz > @float * 0.5f))
				{
					DrawCollision(color2, objectGeometryData, position, rotation, 8f);
				}
			}
			else
			{
				flag = (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0;
				@float = (float2)lotSize * 8f;
			}
			if (flag)
			{
				float3 float2 = math.forward(rotation);
				m_OverlayBuffer.DrawCircle(color2, fillColor, outlineWidth, styleFlags, float2.xz, position, math.cmax(@float));
			}
			else
			{
				float3 float3 = math.forward(rotation) * (@float.y * 0.5f - num);
				Line3.Segment line = new Line3.Segment(position - float3, position + float3);
				m_OverlayBuffer.DrawLine(color2, fillColor, outlineWidth, styleFlags, line, @float.x, num / @float.x * 2f);
			}
			if (m_EditorMode)
			{
				float3 float4 = math.forward(rotation);
				float3 float5 = position + float4 * (@float.y * 0.5f);
				Line3.Segment line2 = new Line3.Segment(float5, float5 + float4 * 4f);
				m_OverlayBuffer.DrawLine(color2, color2, 0f, OverlayRenderSystem.StyleFlags.Projected, line2, 0.5f, new float2(0f, 1f));
			}
		}

		private void DrawArrow(UnityEngine.Color color, int2 lotSize, ObjectGeometryData objectGeometryData, float3 position, quaternion rotation)
		{
			float2 x = (((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? ((float2)lotSize * 8f) : math.min((float2)lotSize * 8f, objectGeometryData.m_LegSize.xz + objectGeometryData.m_LegOffset * 2f));
			color.a = 1f;
			float2 @float = (math.sqrt(math.length(x)) + 1f) * new float2(2f, 1f);
			float3 float2 = math.forward(rotation);
			float3 float3 = new float3
			{
				xz = MathUtils.Right(float2.xz)
			};
			float num = math.select(0f, 16f, m_ZonesVisible);
			float3 float4 = position + float2 * (x.y * 0.5f + num + @float.y);
			float3 float5 = float4 + (float2 - float3) * @float.x;
			float3 float6 = float5 + float3 * @float.y;
			float3 float7 = float6 + float2 * @float.x;
			float3 float8 = float7 + float3 * @float.x;
			float3 float9 = float8 - float2 * @float.x;
			float3 float10 = float9 + float3 * @float.y;
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float4, float5), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float5, float6), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float6, float7), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float7, float8), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float8, float9), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float9, float10), 0.5f, 1f);
			m_OverlayBuffer.DrawLine(color, color, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float10, float4), 0.5f, 1f);
		}

		private void DrawCollision(UnityEngine.Color color, ObjectGeometryData objectGeometryData, float3 position, quaternion rotation, float size)
		{
			float3 @float = position;
			@float.y += objectGeometryData.m_LegSize.y;
			if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
			{
				float3 float2 = math.rotate(rotation, new float3(objectGeometryData.m_Size.x * 0.5f, 0f, 0f));
				float3 float3 = math.rotate(rotation, new float3(objectGeometryData.m_Size.x * 0.2761424f, 0f, 0f));
				float3 float4 = math.rotate(rotation, new float3(0f, 0f, objectGeometryData.m_Size.z * 0.5f));
				float3 float5 = math.rotate(rotation, new float3(0f, 0f, objectGeometryData.m_Size.z * 0.2761424f));
				Bezier4x3 curve = new Bezier4x3(@float - float2, @float - float2 - float5, @float - float3 - float4, @float - float4);
				Bezier4x3 curve2 = new Bezier4x3(@float - float4, @float + float3 - float4, @float + float2 - float5, @float + float2);
				Bezier4x3 curve3 = new Bezier4x3(@float + float2, @float + float2 + float5, @float + float3 + float4, @float + float4);
				Bezier4x3 curve4 = new Bezier4x3(@float + float4, @float - float3 + float4, @float - float2 + float5, @float - float2);
				float num = MathUtils.Length(curve.xz) / size;
				float num2 = num / math.max(1f, math.round(num)) * (size * 0.5f);
				m_OverlayBuffer.DrawDashedCurve(color, curve, size * 0.125f, num2, num2);
				m_OverlayBuffer.DrawDashedCurve(color, curve2, size * 0.125f, num2, num2);
				m_OverlayBuffer.DrawDashedCurve(color, curve3, size * 0.125f, num2, num2);
				m_OverlayBuffer.DrawDashedCurve(color, curve4, size * 0.125f, num2, num2);
			}
			else
			{
				float3 float6 = math.rotate(rotation, new float3(objectGeometryData.m_Bounds.min.x, 0f, 0f));
				float3 float7 = math.rotate(rotation, new float3(objectGeometryData.m_Bounds.max.x, 0f, 0f));
				float3 float8 = math.rotate(rotation, new float3(0f, 0f, objectGeometryData.m_Bounds.min.z));
				float3 float9 = math.rotate(rotation, new float3(0f, 0f, objectGeometryData.m_Bounds.max.z));
				Line3.Segment line = new Line3.Segment(@float + float6 + float8, @float + float7 + float8);
				Line3.Segment line2 = new Line3.Segment(@float + float7 + float8, @float + float7 + float9);
				Line3.Segment line3 = new Line3.Segment(@float + float7 + float9, @float + float6 + float9);
				Line3.Segment line4 = new Line3.Segment(@float + float6 + float9, @float + float6 + float8);
				float num3 = MathUtils.Length(line.xz) / size;
				float num4 = MathUtils.Length(line2.xz) / size;
				float num5 = num3 / math.max(1f, math.round(num3)) * (size * 0.5f);
				float num6 = num4 / math.max(1f, math.round(num4)) * (size * 0.5f);
				m_OverlayBuffer.DrawDashedLine(color, line, size * 0.125f, num5, num5);
				m_OverlayBuffer.DrawDashedLine(color, line2, size * 0.125f, num6, num6);
				m_OverlayBuffer.DrawDashedLine(color, line3, size * 0.125f, num5, num5);
				m_OverlayBuffer.DrawDashedLine(color, line4, size * 0.125f, num6, num6);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct BuildingTerraformRenderJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

		[ReadOnly]
		public BufferLookup<AdditionalBuildingTerraformElement> m_PrefabAdditionalTerraform;

		[ReadOnly]
		public Entity m_Selected;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			if (m_PrefabRefData.TryGetComponent(m_Selected, out var componentData) && m_PrefabBuildingTerraformData.TryGetComponent(componentData.m_Prefab, out var componentData2))
			{
				Game.Objects.Transform transform = m_TransformData[m_Selected];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[componentData.m_Prefab];
				bool flag = (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != 0;
				bool circular = ((uint)objectGeometryData.m_Flags & (uint)((!flag) ? 1 : 256)) != 0;
				m_PrefabAdditionalTerraform.TryGetBuffer(componentData.m_Prefab, out var bufferData);
				DrawTerraform(componentData2, bufferData, transform.m_Position, transform.m_Rotation, circular);
			}
		}

		private void DrawTerraform(BuildingTerraformData terraformData, DynamicBuffer<AdditionalBuildingTerraformElement> additionalElements, float3 position, quaternion rotation, bool circular)
		{
			UnityEngine.Color magenta = UnityEngine.Color.magenta;
			float3 @float = math.rotate(rotation, math.right());
			float3 float2 = math.rotate(rotation, math.forward());
			if (circular)
			{
				UnityEngine.Color fillColor = new UnityEngine.Color(magenta.r, magenta.g, magenta.b, 0f);
				float diameter = math.cmax(terraformData.m_Smooth.zw - terraformData.m_Smooth.xy);
				m_OverlayBuffer.DrawCircle(magenta, fillColor, 0.25f, (OverlayRenderSystem.StyleFlags)0, float2.xz, position, diameter);
			}
			else
			{
				float3 float3 = position + @float * terraformData.m_Smooth.x + float2 * terraformData.m_Smooth.y;
				float3 float4 = position + @float * terraformData.m_Smooth.x + float2 * terraformData.m_Smooth.w;
				float3 float5 = position + @float * terraformData.m_Smooth.z + float2 * terraformData.m_Smooth.w;
				float3 float6 = position + @float * terraformData.m_Smooth.z + float2 * terraformData.m_Smooth.y;
				m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float3, float4), 0.25f, new float2(1f, 1f));
				m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float4, float5), 0.25f, new float2(1f, 1f));
				m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float5, float6), 0.25f, new float2(1f, 1f));
				m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float6, float3), 0.25f, new float2(1f, 1f));
			}
			if (additionalElements.IsCreated)
			{
				for (int i = 0; i < additionalElements.Length; i++)
				{
					AdditionalBuildingTerraformElement additionalBuildingTerraformElement = additionalElements[i];
					if (additionalBuildingTerraformElement.m_Circular)
					{
						UnityEngine.Color fillColor2 = new UnityEngine.Color(magenta.r, magenta.g, magenta.b, 0f);
						float diameter2 = math.cmax(MathUtils.Size(additionalBuildingTerraformElement.m_Area));
						m_OverlayBuffer.DrawCircle(magenta, fillColor2, 0.25f, (OverlayRenderSystem.StyleFlags)0, float2.xz, position, diameter2);
						continue;
					}
					float3 float7 = position + @float * additionalBuildingTerraformElement.m_Area.min.x + float2 * additionalBuildingTerraformElement.m_Area.min.y;
					float3 float8 = position + @float * additionalBuildingTerraformElement.m_Area.min.x + float2 * additionalBuildingTerraformElement.m_Area.max.y;
					float3 float9 = position + @float * additionalBuildingTerraformElement.m_Area.max.x + float2 * additionalBuildingTerraformElement.m_Area.max.y;
					float3 float10 = position + @float * additionalBuildingTerraformElement.m_Area.max.x + float2 * additionalBuildingTerraformElement.m_Area.min.y;
					m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float7, float8), 0.25f, new float2(1f, 1f));
					m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float8, float9), 0.25f, new float2(1f, 1f));
					m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float9, float10), 0.25f, new float2(1f, 1f));
					m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float10, float7), 0.25f, new float2(1f, 1f));
				}
			}
			float3 float11 = position + @float * terraformData.m_FlatX0.x + float2 * terraformData.m_FlatZ0.y;
			float3 float12 = position + @float * terraformData.m_FlatX0.y + float2 * terraformData.m_FlatZ0.x;
			float3 float13 = position + @float * terraformData.m_FlatX0.y + float2 * terraformData.m_FlatZ1.x;
			float3 float14 = position + @float * terraformData.m_FlatX0.z + float2 * terraformData.m_FlatZ1.y;
			float3 float15 = position + @float * terraformData.m_FlatX1.z + float2 * terraformData.m_FlatZ1.y;
			float3 float16 = position + @float * terraformData.m_FlatX1.y + float2 * terraformData.m_FlatZ1.z;
			float3 float17 = position + @float * terraformData.m_FlatX1.y + float2 * terraformData.m_FlatZ0.z;
			float3 float18 = position + @float * terraformData.m_FlatX1.x + float2 * terraformData.m_FlatZ0.y;
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float11, float12), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float12, float13), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float13, float14), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float14, float15), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float15, float16), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float16, float17), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float17, float18), 0.5f, new float2(1f, 1f));
			m_OverlayBuffer.DrawLine(magenta, magenta, 0f, (OverlayRenderSystem.StyleFlags)0, new Line3.Segment(float18, float11), 0.5f, new float2(1f, 1f));
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Extension> __Game_Buildings_Extension_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AssetStamp> __Game_Objects_AssetStamp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Warning> __Game_Tools_Warning_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GateData> __Game_Prefabs_GateData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AdditionalBuildingTerraformElement> __Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Extension>(isReadOnly: true);
			__Game_Objects_AssetStamp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AssetStamp>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Warning>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_GateData_RO_ComponentLookup = state.GetComponentLookup<GateData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup = state.GetBufferLookup<ServiceUpgradeBuilding>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup = state.GetBufferLookup<AdditionalBuildingTerraformElement>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private EntityQuery m_LotQuery;

	private EntityQuery m_RenderingSettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_LotQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Temp>() },
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Extension>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Taxiway>(),
				ComponentType.ReadOnly<Tree>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Overridden>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Error>() },
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Extension>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Taxiway>(),
				ComponentType.ReadOnly<Tree>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Overridden>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Warning>() },
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Extension>(),
				ComponentType.ReadOnly<AssetStamp>(),
				ComponentType.ReadOnly<Taxiway>(),
				ComponentType.ReadOnly<Tree>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Overridden>()
			}
		});
		m_RenderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<RenderingSettingsData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Entity entity = (m_ToolSystem.actionMode.IsEditor() ? m_ToolSystem.selected : Entity.Null);
		bool flag = !m_LotQuery.IsEmptyIgnoreFilter;
		if (flag || !(entity == Entity.Null))
		{
			RenderingSettingsData renderingSettingsData = new RenderingSettingsData
			{
				m_HoveredColor = new UnityEngine.Color(0.5f, 0.5f, 1f, 1f),
				m_ErrorColor = new UnityEngine.Color(1f, 0.5f, 0.5f, 1f),
				m_WarningColor = new UnityEngine.Color(1f, 1f, 0.5f, 1f),
				m_OwnerColor = new UnityEngine.Color(0.5f, 1f, 0.5f, 1f)
			};
			if (!m_RenderingSettingsQuery.IsEmptyIgnoreFilter)
			{
				RenderingSettingsData singleton = m_RenderingSettingsQuery.GetSingleton<RenderingSettingsData>();
				renderingSettingsData.m_HoveredColor = singleton.m_HoveredColor;
				renderingSettingsData.m_ErrorColor = singleton.m_ErrorColor;
				renderingSettingsData.m_WarningColor = singleton.m_WarningColor;
				renderingSettingsData.m_OwnerColor = singleton.m_OwnerColor;
			}
			JobHandle dependencies;
			OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out dependencies);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, dependencies);
			if (flag)
			{
				bool zonesVisible = m_ToolSystem.activeTool != null && m_ToolSystem.activeTool.requireZones;
				BuildingLotRenderJob jobData = new BuildingLotRenderJob
				{
					m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ExtensionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_AssetStampType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_AssetStamp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_StartGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EndGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_WarningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabAssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabGateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GateData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabServiceUpgradeBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup, ref base.CheckedStateRef),
					m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
					m_ZonesVisible = zonesVisible,
					m_RenderingSettingsData = renderingSettingsData,
					m_OverlayBuffer = buffer
				};
				base.Dependency = JobChunkExtensions.Schedule(jobData, m_LotQuery, base.Dependency);
			}
			if (entity != Entity.Null)
			{
				BuildingTerraformRenderJob jobData2 = new BuildingTerraformRenderJob
				{
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabBuildingTerraformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PrefabAdditionalTerraform = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AdditionalBuildingTerraformElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Selected = entity,
					m_OverlayBuffer = buffer
				};
				base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
			}
			m_OverlayRenderSystem.AddBufferWriter(base.Dependency);
		}
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
	public BuildingLotRenderSystem()
	{
	}
}
