using Game.Prefabs;

namespace Game.UI;

public static class TypeNames
{
	public const string kTutorialsGroup = "tutorials";

	public const string kEditorTutorialsGroup = "editorTutorials";

	public static readonly string kAdvisorCategory = "tutorials.AdvisorCategory";

	public static readonly string kAdvisorItem = "tutorials.AdvisorItem";

	public static readonly string kTutorial = "tutorials.Tutorial";

	public static readonly string kTutorialTrigger = "tutorials.Trigger";

	public static readonly string kTutorialPhase = "tutorials.Phase";

	public static readonly string kTutorialList = "tutorials.List";

	public static readonly string kTutorialBalloonUITarget = typeof(TutorialBalloonPrefab.BalloonUITarget).FullName ?? "";

	public const string kPoliciesGroup = "policies";

	public static readonly string kPolicy = "policies.Policy";

	public static readonly string kPolicySlider = "policies.Slider";
}
