using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.InGame;

public interface ISubsectionProvider : ISectionSource, IJsonWritable
{
	List<ISubsectionSource> subsections { get; }
}
