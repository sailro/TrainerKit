using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainerKit.Configuration;
using TrainerKit.Extensions;
using TrainerKit.Properties;
using TrainerKit.UI;
using UnityEngine;

#nullable enable

namespace TrainerKit.Features;

internal abstract class FeatureRenderer : ToggleFeature
{
	public abstract float X { get; set; }
	public abstract float Y { get; set; }

	protected const float DefaultX = 40f;
	protected const float DefaultY = 20f;

	private static GUIStyle MakeLabelStyle()
	{
		var style = new GUIStyle();
		style.wordWrap = false;
		style.normal.textColor = Color.white;
		style.margin = MakeRectOffset(8, 0, 8, 0);
		style.fixedWidth = 150f;
		style.stretchWidth = false;
		return style;
	}

	private static GUIStyle MakeDescriptionStyle()
	{
		var style = new GUIStyle();
		style.wordWrap = true;
		style.normal.textColor = Color.white;
		style.margin = MakeRectOffset(8, 0, 8, 0);
		style.stretchWidth = true;
		return style;
	}

	private static GUIStyle MakeBoxStyle()
	{
		var style = new GUIStyle();
		style.normal.background = Texture2D.whiteTexture;
		style.normal.textColor = Color.white;
		return style;
	}

	private static RectOffset MakeRectOffset(int left, int right, int top, int bottom)
	{
		var offset = new RectOffset();
		offset.left = left;
		offset.right = right;
		offset.top = top;
		offset.bottom = bottom;
		return offset;
	}

	private static GUIStyle? _labelStyle;
	private static GUIStyle LabelStyle => _labelStyle ??= MakeLabelStyle();

	private static GUIStyle? _descriptionStyle;
	private static GUIStyle DescriptionStyle => _descriptionStyle ??= MakeDescriptionStyle();

	private static GUIStyle? _boxStyle;
	private static GUIStyle BoxStyle => _boxStyle ??= MakeBoxStyle();

	private static GUIStyle? _colorButtonFullStyle;
	private static GUIStyle ColorButtonFullStyle
	{
		get
		{
			if (_colorButtonFullStyle == null)
			{
				_colorButtonFullStyle = new GUIStyle();
				_colorButtonFullStyle.normal.background = Texture2D.whiteTexture;
				_colorButtonFullStyle.normal.textColor = Color.white;
				_colorButtonFullStyle.fixedHeight = 22f;
			}
			return _colorButtonFullStyle;
		}
	}

	protected void SetupWindowCoordinates()
	{
		bool needfix = false;
		X = FixCoordinate(X, Screen.width, DefaultX, ref needfix);
		Y = FixCoordinate(Y, Screen.height, DefaultY, ref needfix);

		if (needfix)
			SaveSettings();
	}

	private static float FixCoordinate(float coord, float maxValue, float defaultValue, ref bool needfix)
	{
		if (coord < 0 || coord >= maxValue)
		{
			coord = defaultValue;
			needfix = true;
		}

		return coord;
	}

	internal enum SelectionContextType { Color = 1, KeyCode = 2 }

	internal class SelectionContext
	{
		public SelectionContext(IFeature feature, OrderedProperty orderedProperty, float parentX, float parentY, Func<object, IPicker> builder, SelectionContextType contextType)
		{
			Feature = feature;
			OrderedProperty = orderedProperty;
			Picker = builder(orderedProperty.Property.GetValue(feature));
			ContextType = contextType;

			var position = Event.current.mousePosition;
			Picker.SetWindowPosition(parentX + LabelStyle.fixedWidth * 3 + LabelStyle.margin.left * 6, position.y + parentY - 32f);
		}

		public IFeature Feature { get; }
		public OrderedProperty OrderedProperty { get; }
		public IPicker Picker { get; }
		public SelectionContextType ContextType { get; }
	}

	private Rect _clientWindowRect;
	private readonly Dictionary<SelectionContextType, SelectionContext> _selectionContexts = [];

	// Manual drag state (GUI.Window/GUI.DragWindow don't receive mouse events in IL2CPP)
	private bool _isDragging;
	private Vector2 _dragOffset;
	private const float TitleBarHeight = 24f;

	protected override void OnGUIWhenEnabled()
	{
		_clientWindowRect = new Rect(X, Y, 490, Math.Max(_clientWindowRect.height, 256));

		// Render picker windows FIRST so they get mouse events before main window buttons
		foreach (var key in _selectionContexts.Keys)
		{
			if (HandleSelectionContext(_selectionContexts[key]))
				_selectionContexts.Remove(key);
		}

		// Manual drag handling
		HandleDrag();

		// Draw window background and title
		GUI.Box(_clientWindowRect, string.Empty);
		var titleRect = new Rect(_clientWindowRect.x, _clientWindowRect.y, _clientWindowRect.width, TitleBarHeight);
		GUI.Label(titleRect, Strings.FeatureCommandsTitle, TitleStyle);

		// Render window content with absolute screen coordinates
		RenderWindowContent();

		X = _clientWindowRect.x;
		Y = _clientWindowRect.y;
	}

	private void HandleDrag()
	{
		var evt = Event.current;
		var titleRect = new Rect(_clientWindowRect.x, _clientWindowRect.y, _clientWindowRect.width, TitleBarHeight);

		if (evt.type == EventType.MouseDown && titleRect.Contains(evt.mousePosition))
		{
			_isDragging = true;
			_dragOffset = evt.mousePosition - new Vector2(_clientWindowRect.x, _clientWindowRect.y);
			evt.Use();
		}
		else if (evt.type == EventType.MouseDrag && _isDragging)
		{
			_clientWindowRect.x = evt.mousePosition.x - _dragOffset.x;
			_clientWindowRect.y = evt.mousePosition.y - _dragOffset.y;
			evt.Use();
		}
		else if (evt.type == EventType.MouseUp && _isDragging)
		{
			_isDragging = false;
			evt.Use();
		}
	}

	private bool HandleSelectionContext(SelectionContext? context)
	{
		if (context == null)
			return false;

		var property = context.OrderedProperty.Property;
		var picker = context.Picker;

		picker.DrawWindow((int)context.ContextType, GetPropertyDisplay(property.Name));
		property.SetValue(context.Feature, picker.RawValue);

		return picker.IsSelected;
	}

	private const float TabWidth = 150f;
	private const float ContentMargin = 8f;
	private const float WindowPadding = 10f;
	private const float RowHeight = 22f;
	private const float PropertyLabelWidth = 150f;
	private const float PropertyControlWidth = 150f;

	private static GUIStyle? _titleStyle;
	private static GUIStyle TitleStyle
	{
		get
		{
			if (_titleStyle == null)
			{
				_titleStyle = new GUIStyle();
				_titleStyle.alignment = TextAnchor.MiddleCenter;
				_titleStyle.fontStyle = FontStyle.Bold;
				_titleStyle.normal.textColor = Color.white;
				_titleStyle.fontSize = 13;
			}
			return _titleStyle;
		}
	}

	private int _selectedTabIndex = 0;
	private void RenderWindowContent()
	{
		var wx = _clientWindowRect.x;
		var wy = _clientWindowRect.y;

		var fixedTabs = new[] { Strings.FeatureRendererSummary };

		var tabs = fixedTabs
			.Concat
			(
				Context
					.Features
					.Value
					.Select(RenderFeatureText)
			)
			.ToArray();

		// Tab list on the left — individual buttons
		var tabY = wy + WindowPadding + TitleBarHeight;
		var lastIndex = _selectedTabIndex;
		for (int i = 0; i < tabs.Length; i++)
		{
			var tabRect = new Rect(wx + WindowPadding, tabY, TabWidth, RowHeight);
			if (Il2CppButton(tabRect, tabs[i]))
				_selectedTabIndex = i;
			tabY += RowHeight + 2;
		}

		if (lastIndex != _selectedTabIndex)
			_selectionContexts.Clear();

		// Content on the right
		var contentX = wx + WindowPadding + TabWidth + ContentMargin;
		var contentWidth = 490 - WindowPadding - TabWidth - ContentMargin - WindowPadding;
		var layout = new ImguiLayout(contentX, wy + WindowPadding + TitleBarHeight + 4, contentWidth);

		switch (_selectedTabIndex)
		{
			case 0:
				RenderSummary(layout);
				break;
			default:
				var feature = Context.Features.Value[_selectedTabIndex - fixedTabs.Length];
				RenderFeature(feature, layout);
				break;
		}

		// Auto-resize window height
		var neededHeight = Math.Max(tabY - wy, layout.CurrentY - wy) + WindowPadding;
		_clientWindowRect.height = Math.Max(neededHeight, 256);
	}

	/// <summary>
	/// Rect.Contains may not work in IL2CPP. Manual bounds check.
	/// </summary>
	private static bool RectContains(Rect rect, Vector2 point)
	{
		return point.x >= rect.x && point.x <= rect.x + rect.width
			&& point.y >= rect.y && point.y <= rect.y + rect.height;
	}

	/// <summary>
	/// GUI.Button renders correctly in IL2CPP but never returns true.
	/// Detect clicks manually via Event.current.
	/// Note: Use MouseDown — EventType.MouseUp comparison may be broken in IL2CPP.
	/// </summary>
	private static bool Il2CppButton(Rect rect, string text)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Button(rect, text);
		if (clicked)
			evt.Use();
		return clicked;
	}

	private static bool Il2CppButton(Rect rect, string text, GUIStyle style)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Button(rect, text, style);
		if (clicked)
			evt.Use();
		return clicked;
	}

	/// <summary>
	/// GUI.Toggle return value is broken in IL2CPP. Detect clicks manually.
	/// </summary>
	private static bool Il2CppToggle(Rect rect, bool value, string text)
	{
		var evt = Event.current;
		bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && RectContains(rect, evt.mousePosition);
		GUI.Toggle(rect, value, text);
		if (clicked)
			return !value;
		return value;
	}

	private static string RenderFeatureText(Feature feature)
	{
		if (feature is not ToggleFeature toggleFeature || ConfigurationManager.IsSkippedProperty(feature, nameof(Enabled)))
			return feature.Name;

		return string.Format(Strings.CommandStatusTextFormat, feature.Name, toggleFeature.Enabled ? Strings.TextOn.Green() : Strings.TextOff.Red(), string.Empty);
	}

	private void RenderSummary(ImguiLayout layout)
	{
		GUI.Label(layout.NextRect(30), $"<i><b>{Strings.FeatureRendererWelcome}</b></i>", DescriptionStyle);
		layout.Space(4);

		if (Il2CppButton(layout.NextRect(), Strings.CommandLoadDescription))
			LoadSettings();

		if (Il2CppButton(layout.NextRect(), Strings.CommandSaveDescription))
			SaveSettings();
	}

	protected static void SaveSettings()
	{
		ConfigurationManager.Save(Context.ConfigFile, Context.Features.Value);
	}

	protected void LoadSettings(bool warnIfNotExists = true)
	{
		var cx = X;
		var cy = Y;

		ConfigurationManager.Load(Context.ConfigFile, Context.Features.Value, warnIfNotExists);
		ControlValues.Clear();

		if (!Enabled)
			return;

		X = cx;
		Y = cy;
	}

	private void RenderFeature(Feature feature, ImguiLayout layout)
	{
		var orderedProperties = ConfigurationManager.GetOrderedProperties(feature.GetType());

		GUI.Label(layout.NextRect(30), $"<i><b>{feature.Description}</b></i>", DescriptionStyle);
		layout.Space(4);

		foreach (var property in orderedProperties)
			RenderFeatureProperty(feature, property, layout);
	}

	private static readonly Dictionary<string, string> ControlValues = [];
	private void RenderFeatureProperty(Feature feature, OrderedProperty orderedProperty, ImguiLayout layout)
	{
		if (!orderedProperty.Attribute.Browsable)
			return;

		var property = orderedProperty.Property;

		layout.BeginHorizontal(PropertyLabelWidth);
		GUI.Label(layout.NextRect(), GetPropertyDisplay(property.Name), LabelStyle);

		layout.FlexRemaining();

		var currentValue = property.GetValue(feature);
		var currentBackgroundColor = GUI.backgroundColor;

		if (currentValue == null)
		{
			layout.EndHorizontal();
			return;
		}

		var controlName = $"{feature.Name}.{property.Name}-{property.PropertyType.Name}";
		GUI.SetNextControlName(controlName);

		var newValue = RenderControl(feature, orderedProperty, currentValue, controlName, layout);

		if (currentValue != newValue && property.CanWrite)
			property.SetValue(feature, newValue);

		var focused = GUI.GetNameOfFocusedControl();

		foreach (var key in _selectionContexts.Keys)
		{
			if (ShouldResetSelectionContext(focused, _selectionContexts[key]))
				_selectionContexts.Remove(key);
		}

		GUI.backgroundColor = currentBackgroundColor;
		layout.EndHorizontal();
	}

	protected abstract string GetPropertyDisplay(string propertyName);

	private object RenderControl(IFeature feature, OrderedProperty orderedProperty, object currentValue, string controlName, ImguiLayout layout)
	{
		var property = orderedProperty.Property;
		var propertyType = property.PropertyType;
		var newValue = currentValue;
		var rect = layout.NextRect();

		switch (propertyType.Name)
		{
			case nameof(Boolean):
				var boolValue = (bool)currentValue;
				var newBool = Il2CppToggle(rect, boolValue, string.Empty);
				if (newBool != boolValue) _selectionContexts.Clear();
				newValue = newBool;
				break;

			case nameof(KeyCode):
				if (Il2CppButton(rect, currentValue.ToString()))
				{
					_selectionContexts[SelectionContextType.KeyCode] = new SelectionContext(feature, orderedProperty, X, Y, o => new EnumPicker<KeyCode>((KeyCode)o), SelectionContextType.KeyCode);
					GUI.FocusControl(controlName);
				}
				break;

			case nameof(Single):
				newValue = RenderFloatControl(rect, currentValue, controlName);
				break;

			case nameof(Int32):
				newValue = RenderIntControl(rect, currentValue, controlName);
				break;

			case nameof(Color):
				GUI.backgroundColor = (Color)currentValue;
				if (Il2CppButton(rect, string.Empty, ColorButtonFullStyle))
				{
					_selectionContexts[SelectionContextType.Color] = new SelectionContext(feature, orderedProperty, X, Y, o => new ColorPicker((Color)o), SelectionContextType.Color);
					GUI.FocusControl(controlName);
				}
				break;

			case nameof(String):
				newValue = Il2CppTextField(rect, currentValue.ToString(), controlName);
				break;

			default:
				GUI.Label(rect, string.Format(Strings.ErrorUnsupportedTypeFormat, propertyType.FullName));
				break;
		}

		return newValue;
	}

	private static bool ShouldResetSelectionContext(string focused, SelectionContext? context)
	{
		return !string.IsNullOrEmpty(focused)
			   && context != null
			   && !focused.EndsWith($"-{context.ContextType}");
	}

	private static readonly Color RedColor = new Color(1f, 0f, 0f, 1f);

	// GUI.TextField is stripped in IL2CPP — custom text input using keyboard events
	private static string? _activeControlName;

	private static GUIStyle? _textFieldStyle;
	private static GUIStyle TextFieldStyle
	{
		get
		{
			if (_textFieldStyle == null)
			{
				_textFieldStyle = new GUIStyle();
				_textFieldStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
				_textFieldStyle.fontSize = 12;
				_textFieldStyle.alignment = TextAnchor.MiddleLeft;
				_textFieldStyle.padding.left = 4;
				_textFieldStyle.padding.right = 4;
			}
			return _textFieldStyle;
		}
	}

	private string Il2CppTextField(Rect rect, string text, string controlName)
	{
		bool isActive = _activeControlName == controlName;
		if (!ControlValues.TryGetValue(controlName, out var editText))
			editText = text;

		// Draw semi-transparent background
		var tmp = GUI.backgroundColor;
		GUI.backgroundColor = isActive ? new Color(1f, 1f, 1f, 0.3f) : new Color(1f, 1f, 1f, 0.15f);
		GUI.Box(rect, string.Empty);
		GUI.backgroundColor = tmp;

		// Draw text
		GUI.Label(rect, isActive ? editText + "|" : editText, TextFieldStyle);

		var evt = Event.current;

		// Click to activate — also close any open pickers
		if (evt.type == EventType.MouseDown && evt.button == 0)
		{
			if (RectContains(rect, evt.mousePosition))
			{
				_activeControlName = controlName;
				_selectionContexts.Clear();
				ControlValues[controlName] = editText;
				evt.Use();
			}
			else if (isActive)
			{
				_activeControlName = null;
			}
		}

		// Handle keyboard input when active
		if (isActive && evt.type == EventType.KeyDown)
		{
			if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
			{
				_activeControlName = null;
				evt.Use();
			}
			else if (evt.keyCode == KeyCode.Escape)
			{
				ControlValues[controlName] = text;
				_activeControlName = null;
				evt.Use();
				return text;
			}
			else if (evt.keyCode == KeyCode.Backspace)
			{
				if (editText.Length > 0)
					editText = editText.Substring(0, editText.Length - 1);
				ControlValues[controlName] = editText;
				evt.Use();
			}
			else if (evt.character != 0 && evt.character != '\n' && evt.character != '\r')
			{
				editText += evt.character;
				ControlValues[controlName] = editText;
				evt.Use();
			}
		}

		return ControlValues.TryGetValue(controlName, out var result) ? result : text;
	}

	private object RenderFloatControl(Rect rect, object currentValue, string controlName)
	{
		var culture = CultureInfo.InvariantCulture;
		var text = Il2CppTextField(rect, ((float)currentValue).ToString("G", culture), controlName);
		text = text.Replace(",", ".");
		if (float.TryParse(text, NumberStyles.Float, culture, out var floatVal))
			return floatVal;
		return currentValue;
	}

	private object RenderIntControl(Rect rect, object currentValue, string controlName)
	{
		var text = Il2CppTextField(rect, currentValue.ToString(), controlName);
		if (int.TryParse(text, out var intVal))
			return intVal;
		return currentValue;
	}
}
