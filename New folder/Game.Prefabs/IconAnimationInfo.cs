using System;
using Game.Notifications;
using UnityEngine;

namespace Game.Prefabs;

[Serializable]
public class IconAnimationInfo
{
	public Game.Notifications.AnimationType m_Type;

	public float m_Duration;

	public AnimationCurve m_Scale;

	public AnimationCurve m_Alpha;

	public AnimationCurve m_ScreenY;
}
