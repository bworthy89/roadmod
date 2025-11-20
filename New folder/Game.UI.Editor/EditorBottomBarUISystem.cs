using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Simulation;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class EditorBottomBarUISystem : UISystemBase
{
	private static readonly string kGroup = "editorBottomBar";

	private ClimateSystem m_ClimateSystem;

	private PlanetarySystem m_PlanetarySystem;

	public override GameMode gameMode => GameMode.Editor;

	private float m_NormalizedTimeBindingValue => MathUtils.Snap(m_PlanetarySystem.normalizedTime, 0.01f);

	private float m_NormalizedDateBindingValue => MathUtils.Snap(m_ClimateSystem.currentDate, 0.01f);

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "timeOfDay", () => m_NormalizedTimeBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "date", () => m_NormalizedDateBindingValue));
		AddUpdateBinding(new GetterValueBinding<float>(kGroup, "cloudiness", () => m_ClimateSystem.cloudiness));
		AddBinding(new TriggerBinding<float>(kGroup, "setTimeOfDay", SetTimeOfDay));
		AddBinding(new TriggerBinding(kGroup, "resetTimeOfDay", ResetTimeOfDay));
		AddBinding(new TriggerBinding<float>(kGroup, "setDate", SetDate));
		AddBinding(new TriggerBinding(kGroup, "resetDate", ResetDate));
		AddBinding(new TriggerBinding<float>(kGroup, "setCloudiness", SetCloudiness));
		AddBinding(new TriggerBinding(kGroup, "resetCloudiness", ResetCloudiness));
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		m_PlanetarySystem.overrideTime = false;
		m_ClimateSystem.currentDate.overrideState = false;
		m_ClimateSystem.cloudiness.overrideState = false;
	}

	private void SetTimeOfDay(float time)
	{
		m_PlanetarySystem.overrideTime = true;
		m_PlanetarySystem.normalizedTime = time;
	}

	private void ResetTimeOfDay()
	{
		m_PlanetarySystem.overrideTime = false;
	}

	private void SetDate(float date)
	{
		m_ClimateSystem.currentDate.overrideValue = date;
	}

	private void ResetDate()
	{
		m_ClimateSystem.currentDate.overrideState = false;
	}

	private void SetCloudiness(float cloudiness)
	{
		m_ClimateSystem.cloudiness.overrideValue = cloudiness;
	}

	private void ResetCloudiness()
	{
		m_ClimateSystem.cloudiness.overrideState = false;
	}

	[Preserve]
	public EditorBottomBarUISystem()
	{
	}
}
