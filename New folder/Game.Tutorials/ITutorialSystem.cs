using Unity.Entities;

namespace Game.Tutorials;

public interface ITutorialSystem
{
	Entity activeTutorial { get; }

	Entity activeTutorialPhase { get; }

	Entity activeTutorialList { get; }

	bool tutorialEnabled { get; set; }

	TutorialMode mode { get; set; }

	Entity tutorialPending { get; }

	Entity nextListTutorial { get; }

	bool showListReminder { get; }

	void CompleteCurrentTutorialPhase();

	void SetTutorial(Entity tutorial, Entity phase);

	void ForceTutorial(Entity tutorial, Entity phase, bool advisorActivation);

	void CompleteTutorial(Entity tutorial);
}
