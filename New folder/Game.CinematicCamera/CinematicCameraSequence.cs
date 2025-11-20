using System;
using System.Collections.Generic;
using Colossal.Json;
using Colossal.UI.Binding;
using Game.Rendering;
using Game.Rendering.CinematicCamera;
using UnityEngine;

namespace Game.CinematicCamera;

public class CinematicCameraSequence : IJsonWritable, IJsonReadable
{
	public enum TransformCurveKey
	{
		PositionX,
		PositionY,
		PositionZ,
		RotationX,
		RotationY,
		Count
	}

	public struct CinematicCameraCurveModifier : IJsonWritable, IJsonReadable
	{
		public string id { get; set; }

		public AnimationCurve curve { get; set; }

		public float min { get; set; }

		public float max { get; set; }

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("curve");
			if (curve == null)
			{
				writer.WriteNull();
			}
			else
			{
				writer.Write(curve);
			}
			writer.PropertyName("min");
			writer.Write(min);
			writer.PropertyName("max");
			writer.Write(max);
			writer.TypeEnd();
		}

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("id");
			reader.Read(out string value);
			id = value;
			reader.ReadProperty("curve");
			reader.Read(out AnimationCurve value2);
			curve = value2;
			reader.ReadProperty("min");
			reader.Read(out float value3);
			min = value3;
			reader.ReadProperty("max");
			reader.Read(out float value4);
			max = value4;
			reader.ReadMapEnd();
		}

		public int AddKey(float t, float value)
		{
			if (curve == null)
			{
				AnimationCurve animationCurve = (curve = new AnimationCurve());
			}
			return curve.AddKey(t, value);
		}
	}

	private bool m_Loop;

	public List<CinematicCameraCurveModifier> modifiers { get; set; } = new List<CinematicCameraCurveModifier>();

	public CinematicCameraCurveModifier[] transforms { get; set; } = new CinematicCameraCurveModifier[5];

	public float playbackDuration { get; set; } = 30f;

	public bool loop
	{
		get
		{
			return m_Loop;
		}
		set
		{
			if (m_Loop != value)
			{
				m_Loop = value;
				if (value)
				{
					AfterModifications(rotationsChanged: true);
				}
			}
		}
	}

	public float timelineLength
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i].curve.length > 0)
				{
					num = Mathf.Max(num, transforms[i].curve[transforms[i].curve.length - 1].time);
				}
			}
			for (int j = 0; j < modifiers.Count; j++)
			{
				if (modifiers[j].curve.length > 0)
				{
					num = Mathf.Max(num, modifiers[j].curve[modifiers[j].curve.length - 1].time);
				}
			}
			return num;
		}
	}

	public int transformCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i].curve != null)
				{
					num = Mathf.Max(num, transforms[i].curve.length);
				}
			}
			return num;
		}
	}

	public CinematicCameraSequence()
	{
		Reset();
	}

	public void Reset()
	{
		modifiers.Clear();
		for (int i = 0; i < transforms.Length; i++)
		{
			CinematicCameraCurveModifier[] array = transforms;
			int num = i;
			CinematicCameraCurveModifier cinematicCameraCurveModifier = default(CinematicCameraCurveModifier);
			TransformCurveKey transformCurveKey = (TransformCurveKey)i;
			cinematicCameraCurveModifier.id = transformCurveKey.ToString();
			cinematicCameraCurveModifier.curve = new AnimationCurve();
			array[num] = cinematicCameraCurveModifier;
		}
	}

	public void RemoveModifier(string id)
	{
		int num = modifiers.FindIndex((CinematicCameraCurveModifier m) => m.id == id);
		if (num >= 0)
		{
			modifiers.RemoveAt(num);
		}
	}

	public bool SampleTransform(IGameCameraController controller, float t, out Vector3 position, out Vector3 rotation)
	{
		if (transformCount == 0)
		{
			position = Vector3.zero;
			rotation = Vector3.zero;
			return false;
		}
		position = controller.position;
		rotation = controller.rotation;
		if (transforms[0].curve.keys.Length != 0)
		{
			position.x = transforms[0].curve.Evaluate(t);
		}
		if (transforms[1].curve.keys.Length != 0)
		{
			position.y = transforms[1].curve.Evaluate(t);
		}
		if (transforms[2].curve.keys.Length != 0)
		{
			position.z = transforms[2].curve.Evaluate(t);
		}
		if (transforms[3].curve.keys.Length != 0)
		{
			rotation.x = transforms[3].curve.Evaluate(t);
		}
		if (transforms[4].curve.keys.Length != 0)
		{
			rotation.y = transforms[4].curve.Evaluate(t);
		}
		rotation.z = 0f;
		return true;
	}

	public void RemoveCameraTransform(int curveIndex, int index)
	{
		if (curveIndex < transforms.Length && curveIndex >= 0 && index < transforms[curveIndex].curve.keys.Length && index >= 0)
		{
			if (transforms[curveIndex].curve.keys.Length == 1)
			{
				CinematicCameraCurveModifier cinematicCameraCurveModifier = default(CinematicCameraCurveModifier);
				TransformCurveKey transformCurveKey = (TransformCurveKey)curveIndex;
				cinematicCameraCurveModifier.id = transformCurveKey.ToString();
				cinematicCameraCurveModifier.curve = new AnimationCurve();
				transforms[curveIndex] = cinematicCameraCurveModifier;
			}
			else
			{
				transforms[curveIndex].curve.RemoveKey(index);
				AfterModifications(curveIndex == 4);
			}
		}
	}

	public void RemoveModifierKey(string id, int idx)
	{
		int num = modifiers.FindIndex((CinematicCameraCurveModifier m) => m.id == id);
		if (num >= 0)
		{
			if (idx < modifiers[num].curve.length)
			{
				modifiers[num].curve.RemoveKey(idx);
			}
			if (modifiers[num].curve.length == 0)
			{
				RemoveModifier(id);
			}
			AfterModifications();
		}
	}

	public int AddModifierKey(string id, float t, float value, float min, float max)
	{
		int num = modifiers.FindIndex((CinematicCameraCurveModifier m) => m.id == id);
		if (num >= 0)
		{
			return modifiers[num].curve.AddKey(t, value);
		}
		CinematicCameraCurveModifier item = new CinematicCameraCurveModifier
		{
			curve = new AnimationCurve(new Keyframe(t, value)),
			id = id,
			min = min,
			max = max
		};
		modifiers.Add(item);
		AfterModifications();
		return 0;
	}

	public int AddModifierKey(string id, float t, float value)
	{
		int num = modifiers.FindIndex((CinematicCameraCurveModifier m) => m.id == id);
		if (num >= 0)
		{
			return modifiers[num].curve.AddKey(t, value);
		}
		modifiers.Add(new CinematicCameraCurveModifier
		{
			curve = new AnimationCurve(new Keyframe(t, value)),
			id = id
		});
		AfterModifications();
		return 0;
	}

	public void Refresh(float t, IDictionary<string, PhotoModeProperty> properties, IGameCameraController controller)
	{
		foreach (CinematicCameraCurveModifier modifier in modifiers)
		{
			if (properties.TryGetValue(modifier.id, out var value))
			{
				float value2 = modifier.curve.Evaluate(t);
				float min = value.min?.Invoke() ?? float.MinValue;
				float max = value.max?.Invoke() ?? float.MaxValue;
				float obj = Math.Clamp(value2, min, max);
				value.setValue(obj);
			}
		}
		if (SampleTransform(controller, t, out var position, out var rotation))
		{
			controller.rotation = rotation;
			controller.position = position;
		}
	}

	public int AddCameraTransform(float t, Vector3 position, Vector3 rotation)
	{
		int result = transforms[0].AddKey(t, position.x);
		transforms[1].AddKey(t, position.y);
		transforms[2].AddKey(t, position.z);
		transforms[3].AddKey(t, (rotation.x > 90f) ? (rotation.x - 360f) : rotation.x);
		transforms[4].AddKey(t, rotation.y);
		AfterModifications(rotationsChanged: true);
		return result;
	}

	public int MoveKeyframe(CinematicCameraCurveModifier modifier, int index, Keyframe keyframe)
	{
		if (modifier.curve == null)
		{
			return -1;
		}
		AnimationCurve curve = modifier.curve;
		if (modifier.min != modifier.max)
		{
			keyframe.value = Mathf.Clamp(keyframe.value, modifier.min, modifier.max);
		}
		keyframe.weightedMode = WeightedMode.Both;
		Keyframe keyframe2 = curve[index];
		if (keyframe2.time != keyframe.time || keyframe2.value != keyframe.value || keyframe2.inTangent != keyframe.inTangent || keyframe2.outTangent != keyframe.outTangent || keyframe2.inWeight != keyframe.inWeight || keyframe2.outWeight != keyframe.outWeight)
		{
			index = curve.MoveKey(index, keyframe);
		}
		AfterModifications(modifier.id.StartsWith("Rotation"));
		return index;
	}

	public void AfterModifications(bool rotationsChanged = false)
	{
		bool flag = EnsureLoop();
		if (rotationsChanged || flag)
		{
			PatchRotations();
		}
	}

	private void PatchRotations()
	{
		for (int i = 1; i < transforms[4].curve.keys.Length; i++)
		{
			float time = transforms[4].curve.keys[i].time;
			float value = transforms[4].curve.keys[i - 1].value;
			float num = (transforms[4].curve.keys[i].value - value + 180f) % 360f - 180f;
			float num2 = ((num < -180f) ? (num + 360f) : num);
			transforms[4].curve.MoveKey(i, new Keyframe(time, value + num2));
		}
	}

	private bool EnsureLoop()
	{
		bool flag = false;
		if (loop)
		{
			CinematicCameraCurveModifier[] array = transforms;
			foreach (CinematicCameraCurveModifier cinematicCameraCurveModifier in array)
			{
				flag |= EnsureLoop(cinematicCameraCurveModifier.curve);
			}
			foreach (CinematicCameraCurveModifier modifier in modifiers)
			{
				flag |= EnsureLoop(modifier.curve);
			}
		}
		return flag;
	}

	private bool EnsureLoop(AnimationCurve curve)
	{
		bool flag = false;
		if (curve.keys.Length != 0)
		{
			float num = curve.Evaluate(0f);
			if (curve.keys[0].time > 0.1f)
			{
				curve.AddKey(0f, num);
				flag = true;
			}
			if (curve.keys[curve.keys.Length - 1].time < playbackDuration)
			{
				flag = true;
				curve.AddKey(playbackDuration, num);
			}
			if (curve.keys[curve.keys.Length - 1].time == playbackDuration)
			{
				Keyframe key = curve.keys[curve.keys.Length - 1];
				flag |= key.value != num;
				key.time = playbackDuration;
				key.value = num;
				curve.MoveKey(curve.keys.Length - 1, key);
			}
		}
		return flag;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("modifiers");
		writer.Write((IList<CinematicCameraCurveModifier>)modifiers);
		writer.PropertyName("transforms");
		writer.Write((IList<CinematicCameraCurveModifier>)transforms);
		writer.TypeEnd();
	}

	public void Read(IJsonReader reader)
	{
		reader.ReadMapBegin();
		reader.ReadProperty("modifiers");
		ulong num = reader.ReadArrayBegin();
		modifiers = new List<CinematicCameraCurveModifier>((int)num);
		for (ulong num2 = 0uL; num2 < num; num2++)
		{
			CinematicCameraCurveModifier item = default(CinematicCameraCurveModifier);
			item.Read(reader);
			modifiers.Add(item);
		}
		reader.ReadArrayEnd();
		reader.ReadProperty("transforms");
		num = reader.ReadArrayBegin();
		transforms = new CinematicCameraCurveModifier[num];
		for (ulong num3 = 0uL; num3 < num; num3++)
		{
			CinematicCameraCurveModifier cinematicCameraCurveModifier = default(CinematicCameraCurveModifier);
			cinematicCameraCurveModifier.Read(reader);
			transforms[num3] = cinematicCameraCurveModifier;
		}
		reader.ReadArrayEnd();
	}

	private static void SupportValueTypesForAOT()
	{
		JSON.SupportTypeForAOT<CinematicCameraSequence>();
		JSON.SupportTypeForAOT<CinematicCameraCurveModifier>();
	}
}
