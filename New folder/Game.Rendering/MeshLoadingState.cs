namespace Game.Rendering;

public enum MeshLoadingState
{
	None,
	Pending,
	Loading,
	Copying,
	Complete,
	Obsolete,
	Unloading,
	Default
}
