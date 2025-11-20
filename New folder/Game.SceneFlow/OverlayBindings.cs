using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;

namespace Game.SceneFlow;

public class OverlayBindings : CompositeBinding
{
	public struct ScopedScreen : IDisposable
	{
		private OverlayBindings bindings;

		private OverlayScreen screen;

		public ScopedScreen(OverlayScreen screen, OverlayBindings bindings)
		{
			this.screen = screen;
			this.bindings = bindings;
			this.bindings.ActivateScreen(screen);
		}

		public void Dispose()
		{
			bindings.DeactivateScreen(screen);
		}
	}

	private const string kGroup = "overlay";

	private readonly ValueBinding<OverlayScreen> m_ActiveScreen;

	private readonly ValueBinding<float[]> m_Progress;

	private readonly ValueBinding<string[]> m_HintMessages;

	private readonly ValueBinding<string[]> m_CorruptDataMessages;

	private readonly SortedSet<OverlayScreen> m_ActiveScreenList = new SortedSet<OverlayScreen>(new OverlayScreenComparer());

	public OverlayScreen currentlyActiveScreen => m_ActiveScreenList.FirstOrDefault();

	public string[] hintMessages
	{
		get
		{
			return m_HintMessages.value;
		}
		set
		{
			m_HintMessages.Update(value);
		}
	}

	public string[] corruptDataMessages
	{
		get
		{
			return m_CorruptDataMessages.value;
		}
		set
		{
			m_CorruptDataMessages.Update(value);
		}
	}

	public event Action<OverlayScreen> onScreenActivated;

	public OverlayBindings()
	{
		AddBinding(m_ActiveScreen = new ValueBinding<OverlayScreen>("overlay", "activeScreen", OverlayScreen.None, new DelegateWriter<OverlayScreen>(delegate(IJsonWriter writer, OverlayScreen value)
		{
			writer.Write((int)value);
		})));
		AddBinding(m_Progress = new ValueBinding<float[]>("overlay", "progress", new float[3], new ArrayWriter<float>()));
		AddBinding(m_HintMessages = new ValueBinding<string[]>("overlay", "hintMessages", Array.Empty<string>(), new ArrayWriter<string>()));
		AddBinding(m_CorruptDataMessages = new ValueBinding<string[]>("overlay", "corruptDataMessages", null, ValueWriters.Nullable(new ArrayWriter<string>())));
	}

	public ScopedScreen ActivateScreenScoped(OverlayScreen screen)
	{
		return new ScopedScreen(screen, this);
	}

	private void UpdateScreen()
	{
		OverlayScreen overlayScreen = m_ActiveScreenList.FirstOrDefault();
		m_ActiveScreen.Update(overlayScreen);
		CompositeBinding.log.DebugFormat("Screen changed to {0}", overlayScreen);
		this.onScreenActivated?.Invoke(overlayScreen);
	}

	public void ActivateScreen(OverlayScreen screen)
	{
		m_ActiveScreenList.Add(screen);
		UpdateScreen();
	}

	public void DeactivateScreen(OverlayScreen screen)
	{
		m_ActiveScreenList.Remove(screen);
		UpdateScreen();
	}

	public void SwapScreen(OverlayScreen screen1, OverlayScreen screen2)
	{
		DeactivateScreen(screen1);
		ActivateScreen(screen2);
	}

	public void DeactivateAllScreens()
	{
		m_ActiveScreenList.Clear();
		UpdateScreen();
	}

	public float GetProgress(OverlayProgressType type)
	{
		return m_Progress.value[(int)type];
	}

	public void SetProgress(OverlayProgressType type, float progress)
	{
		if (m_Progress.value[(int)type] != progress)
		{
			m_Progress.value[(int)type] = progress;
			m_Progress.TriggerUpdate();
		}
	}
}
