namespace Game.Modding.Toolchain;

public enum DependencyState
{
	Unknown = -1,
	Installed,
	NotInstalled,
	Outdated,
	Installing,
	Downloading,
	Removing,
	Queued
}
