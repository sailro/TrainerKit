using TrainerKit.Configuration;
using UnityEngine;

#nullable enable

namespace TrainerKit.Features;

internal abstract class ToggleFeature : Feature
{
	[ConfigurationProperty(Order = 1)]
	public virtual bool Enabled { get; set; } = true;

	[ConfigurationProperty(Order = 2)]
	public virtual KeyCode Key { get; set; } = KeyCode.None;

	public override void DoUpdate()
	{
		if (Key != KeyCode.None && Input.GetKeyUp(Key))
			Enabled = !Enabled;

		if (Enabled)
			UpdateWhenEnabled();

		if (!Enabled)
			UpdateWhenDisabled();
	}

	public override void DoOnGUI()
	{
		if (Enabled)
			OnGUIWhenEnabled();
	}

	protected virtual void UpdateWhenEnabled() { }

	protected virtual void UpdateWhenDisabled() { }

	protected virtual void OnGUIWhenEnabled() { }
}
