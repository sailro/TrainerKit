using UnityEngine;

#nullable enable

namespace TrainerKit.UI;

public class ColorPicker : Picker<Color>
{
	public float H => _h;
	public float S => _s;
	public float V => _v;

	private float _h = 0f;
	private float _s = 0f;
	private float _v = 0f;

	private Rect _windowRect = new(20, 20, 180, 210);

	private readonly GUIStyle _previewStyle;
	private readonly GUIStyle _svStyle;
	private readonly GUIStyle _hueStyle;

	private readonly Texture2D _svTexture;

	private const int HsvPickerSize = 120, HuePickerWidth = 16;
	private const float TitleBarHeight = 24f;
	private const float Padding = 8f;
	private const float PreviewHeight = 12f;
	private const float AlphaHeight = 2f;

	// Manual drag state
	private bool _isDragging;
	private Vector2 _dragOffset;

	public ColorPicker(Color color) : base(color)
	{
		ColorUtil.RgbToHsv(Value, out _h, out _s, out _v);

		_previewStyle = new GUIStyle();
		_previewStyle.normal.background = Texture2D.whiteTexture;

		var hueTexture = CreateHueTexture(20, HsvPickerSize);
		_hueStyle = new GUIStyle();
		_hueStyle.normal.background = hueTexture;

		_svTexture = CreateSvTexture(Value, HsvPickerSize);
		_svStyle = new GUIStyle();
		_svStyle.normal.background = _svTexture;
	}

	public override void SetWindowPosition(float x, float y)
	{
		_windowRect.x = x;
		_windowRect.y = y;
	}

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

		// Calculate layout
		float totalWidth = HsvPickerSize + 10 + HuePickerWidth;
		_windowRect.width = totalWidth + Padding * 2;
		_windowRect.height = TitleBarHeight + Padding + PreviewHeight + 2 + AlphaHeight + 5 + HsvPickerSize + Padding;

		// Draw background and title
		GUI.Box(_windowRect, string.Empty);
		var titleStyle = new GUIStyle();
		titleStyle.alignment = TextAnchor.MiddleCenter;
		titleStyle.fontStyle = FontStyle.Bold;
		titleStyle.normal.textColor = Color.white;
		titleStyle.fontSize = 12;
		GUI.Label(titleRect, title, titleStyle);

		// Content area
		float cx = _windowRect.x + Padding;
		float cy = _windowRect.y + TitleBarHeight + Padding;

		// Preview bar
		var tmp = GUI.backgroundColor;
		GUI.backgroundColor = new Color(Value.r, Value.g, Value.b);
		var previewRect = new Rect(cx, cy, totalWidth, PreviewHeight);
		GUI.Label(previewRect, string.Empty, _previewStyle);
		cy += PreviewHeight + 2;

		// Alpha bar
		var alpha = Value.a;
		GUI.backgroundColor = new Color(alpha, alpha, alpha);
		var alphaRect = new Rect(cx, cy, totalWidth, AlphaHeight);
		GUI.Label(alphaRect, string.Empty, _previewStyle);
		GUI.backgroundColor = tmp;
		cy += AlphaHeight + 5;

		// SV picker — use GUI.Label with styled background (GUI.DrawTexture is stripped)
		var svRect = new Rect(cx, cy, HsvPickerSize, HsvPickerSize);
		_svStyle.normal.background = _svTexture;
		GUI.Label(svRect, string.Empty, _svStyle);
		DrawSvHandler(svRect);

		// Hue picker
		var hueRect = new Rect(cx + HsvPickerSize + 10, cy, HuePickerWidth, HsvPickerSize);
		GUI.Label(hueRect, string.Empty, _hueStyle);
		DrawHueHandler(hueRect);
	}

	private void DrawSvHandler(Rect rect)
	{
		// Circle indicator omitted — Resources.Load textures unavailable in IL2CPP game

		var e = Event.current;
		var p = e.mousePosition;

		if (e.button != 0 || e.type != EventType.MouseDown && e.type != EventType.MouseDrag || !RectContains(rect, p))
			return;

		_s = (p.x - rect.x) / rect.width;
		_v = 1f - (p.y - rect.y) / rect.height;
		Value = ColorUtil.HsvToRgb(_h, _s, _v);

		e.Use();
	}

	private void DrawHueHandler(Rect rect)
	{
		// Arrow indicators omitted — Resources.Load textures unavailable in IL2CPP game

		var e = Event.current;
		var p = e.mousePosition;

		if (e.button != 0 || e.type != EventType.MouseDown && e.type != EventType.MouseDrag || !RectContains(rect, p))
			return;

		_h = 1f - (p.y - rect.y) / rect.height;
		Value = ColorUtil.HsvToRgb(_h, _s, _v);
		UpdateSvTexture(Value, _svTexture);

		e.Use();
	}

	private void UpdateSvTexture(Color c, Texture2D tex)
	{
		ColorUtil.RgbToHsv(c, out var h, out _, out _);

		var size = tex.width;
		for (int y = 0; y < size; y++)
		{
			var v = 1f * y / size;
			for (int x = 0; x < size; x++)
			{
				var s = 1f * x / size;
				var color = ColorUtil.HsvToRgb(h, s, v);
				tex.SetPixel(x, y, color);
			}
		}

		tex.Apply();
	}

	private static Texture2D CreateHueTexture(int width, int height)
	{
		var tex = new Texture2D(width, height);
		for (int y = 0; y < height; y++)
		{
			var h = 1f * y / height;
			var color = ColorUtil.HsvToRgb(h, 1f, 1f);
			for (int x = 0; x < width; x++)
			{
				tex.SetPixel(x, y, color);
			}
		}

		tex.Apply();
		return tex;
	}

	private Texture2D CreateSvTexture(Color c, int size)
	{
		var tex = new Texture2D(size, size);
		UpdateSvTexture(c, tex);
		return tex;
	}

	private static bool RectContains(Rect rect, Vector2 point)
	{
		return point.x >= rect.x && point.x <= rect.x + rect.width
			&& point.y >= rect.y && point.y <= rect.y + rect.height;
	}
}
