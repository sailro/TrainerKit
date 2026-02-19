using Newtonsoft.Json;

#nullable enable

namespace TrainerKit.Features;

internal interface IFeature
{
	[JsonIgnore]
	public string Name { get; }
}

internal abstract class Feature : IFeature
{
	public abstract string Name { get; }
	public abstract string Description { get; }

	public virtual void DoUpdate() { }
	public virtual void DoOnGUI() { }

	protected void AddConsoleLog(string log)
	{
		Context.AddConsoleLog(log);
	}
}
