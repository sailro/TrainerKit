using System.Collections.Generic;
using TrainerKit.Configuration;
using UnityEngine;

#nullable enable

namespace TrainerKit.Features;

internal abstract class CachableFeature<T> : ToggleFeature
{
	[ConfigurationProperty(Order = 3)]
	public abstract float CacheTimeInSec { get; set; }

	private readonly List<T> _data = [];
	private bool _refreshing = false;
	private float _lastRefreshTime = float.MinValue;

	private void TryRefreshData()
	{
		if (Time.time - _lastRefreshTime < CacheTimeInSec)
			return;

		_lastRefreshTime = Time.time;

		if (!Enabled)
			return;

		try
		{
			_refreshing = true;

			BeforeRefreshData(_data);
			_data.Clear();
			RefreshData(_data);
		}
		finally
		{
			_refreshing = false;
		}
	}

	protected override void UpdateWhenEnabled()
	{
		TryRefreshData();

		if (_refreshing)
			return;

		if (_data.Count > 0)
			ProcessData(_data);
	}

	protected override void OnGUIWhenEnabled()
	{
		if (_refreshing)
			return;

		if (_data.Count > 0)
			ProcessDataOnGUI(_data);
	}

	protected virtual void BeforeRefreshData(IReadOnlyList<T> data) { }
	public abstract void RefreshData(List<T> data);
	public virtual void ProcessData(IReadOnlyList<T> data) { }
	public virtual void ProcessDataOnGUI(IReadOnlyList<T> data) { }
}
