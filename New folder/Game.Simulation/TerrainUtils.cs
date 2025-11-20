using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Simulation;

public static class TerrainUtils
{
	public static readonly float3 BackDropWorldSizeScale = new float3(4f, 1f, 4f);

	public static float3 ToHeightmapSpace(ref TerrainHeightData data, float3 worldPosition)
	{
		return (worldPosition + data.offset) * data.scale;
	}

	public static Line3.Segment ToHeightmapSpace(ref TerrainHeightData data, Line3.Segment worldLine)
	{
		return new Line3.Segment(ToHeightmapSpace(ref data, worldLine.a), ToHeightmapSpace(ref data, worldLine.b));
	}

	public static Line3.Segment ToBackdropSpace(ref TerrainHeightData data, Line3.Segment worldLine)
	{
		return new Line3.Segment(ToBackdropSpace(ref data, worldLine.a), ToBackdropSpace(ref data, worldLine.b));
	}

	public static float3 ToBackdropSpace(ref TerrainHeightData data, float3 worldPosition)
	{
		return (worldPosition + data.offset * BackDropWorldSizeScale) * data.scale / BackDropWorldSizeScale / new float3(TerrainSystem.kDownScaledHeightmapScale, 1f, TerrainSystem.kDownScaledHeightmapScale);
	}

	public static float ToWorldSpace(ref TerrainHeightData data, float heightmapHeight)
	{
		return heightmapHeight / data.scale.y - data.offset.y;
	}

	public static float3 ToWorldSpace(ref TerrainHeightData data, float3 heightmapSpacePos)
	{
		return heightmapSpacePos / data.scale - data.offset;
	}

	public static float3 ToWorldSpaceFromBackdrop(ref TerrainHeightData data, float3 backdropSpacePos)
	{
		return backdropSpacePos * new float3(TerrainSystem.kDownScaledHeightmapScale, 1f, TerrainSystem.kDownScaledHeightmapScale) / (data.scale / BackDropWorldSizeScale) - data.offset * BackDropWorldSizeScale;
	}

	public static Bounds3 GetBounds(ref TerrainHeightData data)
	{
		return new Bounds3(-data.offset, (data.resolution - 1) / data.scale - data.offset);
	}

	public static Bounds3 GetEditorCameraBounds(TerrainSystem terrainSystem, ref TerrainHeightData data)
	{
		if (terrainSystem?.worldHeightmap != null)
		{
			return new Bounds3(new float3(terrainSystem.worldOffset.x, terrainSystem.heightScaleOffset.y, terrainSystem.worldOffset.y), new float3(terrainSystem.worldOffset.x + terrainSystem.worldSize.x, terrainSystem.heightScaleOffset.y + terrainSystem.heightScaleOffset.x, terrainSystem.worldOffset.y + terrainSystem.worldSize.y));
		}
		return GetBounds(ref data);
	}

	public static Bounds1 GetHeightRange(ref TerrainHeightData data, Bounds3 worldBounds)
	{
		float2 xz = ToHeightmapSpace(ref data, worldBounds.min).xz;
		float2 xz2 = ToHeightmapSpace(ref data, worldBounds.max).xz;
		int4 @int = math.clamp(new int4
		{
			xy = (int2)math.floor(xz),
			zw = (int2)math.ceil(xz2)
		}, 0, data.resolution.xzxz - 1);
		Bounds1 result = new Bounds1(float.MaxValue, float.MinValue);
		for (int i = @int.y; i <= @int.w; i++)
		{
			int2 int2 = i * data.resolution.x + @int.xz;
			for (int j = int2.x; j <= int2.y; j++)
			{
				result |= (float)(int)data.heights[j];
			}
		}
		result.min = ToWorldSpace(ref data, result.min);
		result.max = ToWorldSpace(ref data, result.max);
		return result;
	}

	public static float SampleHeight(ref TerrainHeightData data, float3 worldPosition)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 @int = default(int4);
		@int.xy = (int2)math.floor(xz);
		@int.zw = @int.xy + 1;
		if (!math.clamp(@int, 0, data.resolution.xzxz - 1).Equals(@int) && data.hasBackdrop)
		{
			return SampleHeightBackdrop(ref data, worldPosition);
		}
		return SampleHeightInternal(ref data, worldPosition);
	}

	public static float SampleHeightBackdrop(ref TerrainHeightData data, float3 worldPosition)
	{
		float2 xz = ToBackdropSpace(ref data, worldPosition).xz;
		if (data.hasBackdrop)
		{
			_ = data.downscaledHeights;
			if (data.downscaledHeights.Length != 0)
			{
				int4 valueToClamp = default(int4);
				valueToClamp.xy = (int2)math.floor(xz);
				valueToClamp.zw = valueToClamp.xy + 1;
				int4 @int = math.clamp(valueToClamp, 0, data.downScaledResolution.xzxz - 1);
				int4 int2 = @int.yyww * data.downScaledResolution.x + @int.xzxz;
				float4 @float = default(float4);
				@float.x = (int)data.downscaledHeights[int2.x];
				@float.y = (int)data.downscaledHeights[int2.y];
				@float.z = (int)data.downscaledHeights[int2.z];
				@float.w = (int)data.downscaledHeights[int2.w];
				float2 float2 = math.saturate(xz - @int.xy);
				float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
				return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
			}
		}
		return 0f;
	}

	private static float SampleHeightInternal(ref TerrainHeightData data, float3 worldPosition)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		int4 @int = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 int2 = @int.yyww * data.resolution.x + @int.xzxz;
		float4 @float = default(float4);
		@float.x = (int)data.heights[int2.x];
		@float.y = (int)data.heights[int2.y];
		@float.z = (int)data.heights[int2.z];
		@float.w = (int)data.heights[int2.w];
		float2 float2 = math.saturate(xz - @int.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
	}

	public static float SampleHeight(ref TerrainHeightData data, float3 worldPosition, out float3 normal)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = (int)data.heights[@int.x];
		@float.y = (int)data.heights[@int.y];
		@float.z = (int)data.heights[@int.z];
		@float.w = (int)data.heights[@int.w];
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		float2 float4 = @float.xz - @float.yw;
		normal = math.normalizesafe(new float3(math.lerp(float4.x, float4.y, float2.y), 1f, float3.x - float3.y));
		return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
	}

	public static bool Raycast(ref TerrainHeightData data, Line3.Segment worldLine, bool outside, out float t, out float3 normal, out Bounds3 hitBounds)
	{
		Line3.Segment segment = default(Line3.Segment);
		Bounds3 bounds = default(Bounds3);
		outside &= data.hasBackdrop;
		segment = ToHeightmapSpace(ref data, worldLine);
		bounds = new Bounds3(new float3(0f, -50f, 0f), data.resolution - 1 + new float3(0f, 100f, 0f));
		if (!outside)
		{
			return RaycastInternal(bounds, segment, ref data, outside, out t, out normal, out hitBounds);
		}
		if (RaycastInternal(bounds, segment, ref data, outside: false, out t, out normal, out hitBounds))
		{
			return true;
		}
		segment = ToBackdropSpace(ref data, worldLine);
		bounds = new Bounds3(new float3(0f, -50f, 0f), data.downScaledResolution - 1 + new float3(0f, 100f, 0f));
		return RaycastInternal(bounds, segment, ref data, outside, out t, out normal, out hitBounds);
	}

	private static bool RaycastInternal(Bounds3 terrainBounds, Line3.Segment localLine, ref TerrainHeightData data, bool outside, out float t, out float3 normal, out Bounds3 hitBounds)
	{
		hitBounds = default(Bounds3);
		float2 t2;
		if (outside)
		{
			if (!MathUtils.Intersect(terrainBounds.y, localLine.y, out t2))
			{
				t = 2f;
				normal = default(float3);
				return false;
			}
		}
		else if (!MathUtils.Intersect(terrainBounds, localLine, out t2))
		{
			t = 2f;
			normal = default(float3);
			return false;
		}
		localLine = MathUtils.Cut(localLine, t2);
		float3 x = localLine.b - localLine.a;
		float3 @float = math.abs(x);
		float4 float2 = math.floor(new float4(localLine.a.xz, localLine.b.xz));
		int4 @int = new int4((int)float2.x, (int)float2.z, (int)float2.y, (int)float2.w);
		if (math.all(@int.xz == @int.yw))
		{
			if (RaycastCell(ref data, localLine, @int.xz, outside, out t, out normal, out hitBounds))
			{
				t = math.saturate(math.lerp(t2.x, t2.y, t));
				normal = math.normalizesafe(normal);
				return true;
			}
		}
		else if (@float.x > @float.z)
		{
			int2 int2 = math.select(1, -1, x.xz < 0f);
			@int.y += int2.x;
			float num = (float)math.select(1, 0, x.x < 0f) - localLine.a.x;
			float num2 = 1f / x.x;
			int2 pos = default(int2);
			pos.x = @int.x;
			while (pos.x != @int.y)
			{
				float t3 = ((float)pos.x + num) * num2;
				@int.w = (int)math.floor(math.lerp(localLine.a.z, localLine.b.z, t3)) + int2.y;
				pos.y = @int.z;
				while (pos.y != @int.w)
				{
					if (RaycastCell(ref data, localLine, pos, outside, out t, out normal, out hitBounds))
					{
						t = math.saturate(math.lerp(t2.x, t2.y, t));
						normal = math.normalizesafe(normal);
						return true;
					}
					pos.y += int2.y;
				}
				@int.z = @int.w - int2.y;
				pos.x += int2.x;
			}
		}
		else
		{
			int2 int3 = math.select(1, -1, x.xz < 0f);
			@int.w += int3.y;
			float num3 = (float)math.select(1, 0, x.z < 0f) - localLine.a.z;
			float num4 = 1f / x.z;
			int2 pos2 = default(int2);
			pos2.y = @int.z;
			while (pos2.y != @int.w)
			{
				float t4 = ((float)pos2.y + num3) * num4;
				@int.y = (int)math.floor(math.lerp(localLine.a.x, localLine.b.x, t4)) + int3.x;
				pos2.x = @int.x;
				while (pos2.x != @int.y)
				{
					if (RaycastCell(ref data, localLine, pos2, outside, out t, out normal, out hitBounds))
					{
						t = math.saturate(math.lerp(t2.x, t2.y, t));
						normal = math.normalizesafe(normal);
						return true;
					}
					pos2.x += int3.x;
				}
				@int.x = @int.y - int3.x;
				pos2.y += int3.y;
			}
		}
		t = 2f;
		normal = default(float3);
		return false;
	}

	private static bool RaycastCell(ref TerrainHeightData data, Line3.Segment localLine, int2 pos, bool outside, out float t, out float3 normal, out Bounds3 hitBounds)
	{
		t = 2f;
		normal = default(float3);
		hitBounds = default(Bounds3);
		int4 @int = default(int4);
		float4 x = default(float4);
		if (outside)
		{
			@int = math.clamp(new int4(pos, pos + 1), 0, data.downScaledResolution.xzxz - 1);
			int4 int2 = @int.yyww * data.downScaledResolution.x + @int.xzxz;
			x.x = (int)data.downscaledHeights[int2.x];
			x.y = (int)data.downscaledHeights[int2.y];
			x.z = (int)data.downscaledHeights[int2.z];
			x.w = (int)data.downscaledHeights[int2.w];
		}
		else
		{
			@int = math.clamp(new int4(pos, pos + 1), 0, data.resolution.xzxz - 1);
			int4 int3 = @int.yyww * data.resolution.x + @int.xzxz;
			x.x = (int)data.heights[int3.x];
			x.y = (int)data.heights[int3.y];
			x.z = (int)data.heights[int3.z];
			x.w = (int)data.heights[int3.w];
		}
		Bounds3 bounds = default(Bounds3);
		@int = math.select(@int, new int4(pos, pos + 1), outside);
		bounds.min = new float3(@int.x, math.cmin(x), @int.y);
		bounds.max = new float3(@int.z, math.cmax(x), @int.w);
		if (MathUtils.Intersect(bounds, localLine, out var _))
		{
			float3 @float = new float3(bounds.min.x, x.x, bounds.min.z);
			float3 float2 = new float3(bounds.max.x, x.y, bounds.min.z);
			float3 float3 = new float3(bounds.max.x, x.w, bounds.max.z);
			float3 float4 = new float3(bounds.min.x, x.z, bounds.max.z);
			float3 float5 = MathUtils.Center(bounds);
			if (outside)
			{
				hitBounds.min = ToWorldSpaceFromBackdrop(ref data, bounds.min);
				hitBounds.max = ToWorldSpaceFromBackdrop(ref data, bounds.max);
			}
			else
			{
				hitBounds.min = ToWorldSpace(ref data, bounds.min);
				hitBounds.max = ToWorldSpace(ref data, bounds.max);
			}
			if (MathUtils.Intersect(new Triangle3(@float, float2, float5), localLine, out var t3))
			{
				t = math.min(t, t3.z);
				normal = math.cross(float5 - @float, float2 - @float);
			}
			if (MathUtils.Intersect(new Triangle3(float2, float3, float5), localLine, out t3))
			{
				t = math.min(t, t3.z);
				normal = math.cross(float5 - float2, float3 - float2);
			}
			if (MathUtils.Intersect(new Triangle3(float3, float4, float5), localLine, out t3))
			{
				t = math.min(t, t3.z);
				normal = math.cross(float5 - float3, float4 - float3);
			}
			if (MathUtils.Intersect(new Triangle3(float4, @float, float5), localLine, out t3))
			{
				t = math.min(t, t3.z);
				normal = math.cross(float5 - float4, @float - float4);
			}
			return t != 2f;
		}
		return false;
	}
}
