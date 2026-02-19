using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace TrainerKit.UI;

public class EnumPicker<T>(T value) : Picker<T>(value) where T : struct, IConvertible
{
	private Rect _windowRect = new(20, 20, 200, 500);
	private float _scrollOffset = 0f;

	private T[]? _candidates = null;

	public T[] Candidates
	{
		get
		{
			return _candidates ??=
			[
				.. Enum
					.GetValues(typeof(T))
					.OfType<T>()
					.OrderBy(i => i)
			];
		}
	}

	public override void SetWindowPosition(float x, float y)
	{
		_windowRect.x = x;
		_windowRect.y = y;
	}

	// Manual drag state
	private bool _isDragging;
	private Vector2 _dragOffset;
	private const float TitleBarHeight = 24f;
	private const float RowHeight = 20f;
	private const float Padding = 6f;
	private const float MaxVisibleHeight = 460f;

	public override void DrawWindow(int id, string title)
	{
		// Manual drag
		var evt = Event.current;
		var titleRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, TitleBarHeight);

		if (evt.type == EventType.MouseDown && titleRect.Contains(evt.mousePosition))
		{
			_isDragging = true;
			_dragOffset = evt.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
			evt.Use();
		}
		else if (evt.type == EventType.MouseDrag && _isDragging)
		{
			_windowRect.x = evt.mousePosition.x - _dragOffset.x;
			_windowRect.y = evt.mousePosition.y - _dragOffset.y;
			evt.Use();
		}
		else if (evt.type == EventType.MouseUp && _isDragging)
		{
			_isDragging = false;
			evt.Use();
		}

		// Handle scroll
		if (evt.type == EventType.ScrollWheel && _windowRect.Contains(evt.mousePosition))
		{
			_scrollOffset += evt.delta.y * RowHeight;
			_scrollOffset = Mathf.Max(0, _scrollOffset);
			float maxScroll = Candidates.Length * RowHeight - MaxVisibleHeight;
			if (maxScroll > 0)
				_scrollOffset = Mathf.Min(_scrollOffset, maxScroll);
			else
				_scrollOffset = 0;
			evt.Use();
		}

		// Calculate window height
		float contentHeight = Candidates.Length * RowHeight;
		float visibleHeight = Mathf.Min(contentHeight, MaxVisibleHeight);
		_windowRect.height = TitleBarHeight + Padding + visibleHeight + Padding;

		// Draw background and title
		GUI.Box(_windowRect, string.Empty);
		var titleStyle = new GUIStyle();
		titleStyle.alignment = TextAnchor.MiddleCenter;
		titleStyle.fontStyle = FontStyle.Bold;
		titleStyle.normal.textColor = Color.white;
		titleStyle.fontSize = 12;
		GUI.Label(titleRect, title, titleStyle);

		// Draw items with clipping via BeginGroup
		var listArea = new Rect(_windowRect.x + Padding, _windowRect.y + TitleBarHeight + Padding,
			_windowRect.width - Padding * 2, visibleHeight);
		GUI.BeginGroup(listArea);

		float y = -_scrollOffset;
		foreach (var candidate in Candidates)
		{
			if (y + RowHeight > 0 && y < visibleHeight)
			{
				var itemRect = new Rect(0, y, listArea.width, RowHeight);
				// Inside GUI.BeginGroup, mousePosition is group-local
				bool clicked = evt.type == EventType.MouseDown && evt.button == 0
					&& evt.mousePosition.x >= 0 && evt.mousePosition.x <= listArea.width
					&& evt.mousePosition.y >= y && evt.mousePosition.y < y + RowHeight;

				GUI.Label(itemRect, candidate.ToString(), GUI.skin.label);

				if (clicked)
				{
					IsSelected = true;
					Value = candidate;
					evt.Use();
				}
			}
			y += RowHeight;
		}

		GUI.EndGroup();
	}

	private static bool RectContains(Rect rect, Vector2 point)
	{
		return point.x >= rect.x && point.x <= rect.x + rect.width
			&& point.y >= rect.y && point.y <= rect.y + rect.height;
	}
}
