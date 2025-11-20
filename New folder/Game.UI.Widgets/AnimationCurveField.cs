using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class AnimationCurveField : Field<AnimationCurve>, IMutable<AnimationCurve>, IWidget, IJsonWritable
{
	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new CallBinding<IWidget, int, Keyframe, bool, int, int>(group, "moveKeyframe", delegate(IWidget widget, int index, Keyframe key, bool smooth, int curveIndex)
			{
				bool flag = true;
				int result = index;
				if (widget is IMutable<AnimationCurve> mutable)
				{
					if (index > 0 && mutable.GetValue()[index - 1].time == key.time)
					{
						flag = false;
					}
					if (index < mutable.GetValue().length - 1 && mutable.GetValue()[index + 1].time == key.time)
					{
						flag = false;
					}
					if (flag)
					{
						result = mutable.GetValue().MoveKey(index, key);
					}
					if (smooth)
					{
						mutable.GetValue().SmoothTangents(index, (key.inWeight + key.outWeight) / 2f);
					}
					onValueChanged(widget);
					return result;
				}
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IMutable<AnimationCurve>" : "Invalid widget path");
				return result;
			}, pathResolver);
			yield return new CallBinding<IWidget, float, float, int, int>(group, "addKeyframe", delegate(IWidget widget, float time, float value, int curveIndex)
			{
				int result = -1;
				if (widget is IMutable<AnimationCurve> mutable)
				{
					result = mutable.GetValue().AddKey(time, value);
					onValueChanged(widget);
					return result;
				}
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IMutable<AnimationCurve>" : "Invalid widget path");
				return result;
			}, pathResolver);
			yield return new TriggerBinding<IWidget, Keyframe[], int>(group, "setKeyframes", delegate(IWidget widget, Keyframe[] keys, int curveIndex)
			{
				if (widget is IMutable<AnimationCurve> mutable)
				{
					while (mutable.GetValue().length > 0)
					{
						mutable.GetValue().RemoveKey(0);
					}
					for (int i = 0; i < keys.Count(); i++)
					{
						mutable.GetValue().AddKey(keys[i].time, keys[i].value);
						mutable.GetValue().MoveKey(i, keys[i]);
					}
					onValueChanged(widget);
				}
				else
				{
					UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IMutable<AnimationCurve>" : "Invalid widget path");
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, int, int>(group, "removeKeyframe", delegate(IWidget widget, int index, int curveIndex)
			{
				if (widget is IMutable<AnimationCurve> mutable)
				{
					mutable.GetValue().RemoveKey(index);
					onValueChanged(widget);
				}
				else
				{
					UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IMutable<AnimationCurve>" : "Invalid widget path");
				}
			}, pathResolver);
		}
	}

	private List<Keyframe> m_Keys = new List<Keyframe>();

	private WrapMode m_PreWrapMode;

	private WrapMode m_PostWrapMode;

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		Keyframe[] keys = m_Value.keys;
		if (!m_Keys.SequenceEqual(keys))
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Keys.Clear();
			m_Keys.AddRange(keys);
		}
		if (m_Value.preWrapMode != m_PreWrapMode)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_PreWrapMode = m_Value.preWrapMode;
		}
		if (m_Value.postWrapMode != m_PostWrapMode)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_PostWrapMode = m_Value.postWrapMode;
		}
		return widgetChanges;
	}
}
