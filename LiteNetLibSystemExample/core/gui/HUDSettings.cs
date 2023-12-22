using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public partial class HUDSettings : Control
{
    [Export] private OptionButton _windowBaseSizeButton;
    [Export] private OptionButton _windowStretchModeButton;
    [Export] private OptionButton _windowStretchAspectButton;
    [ExportGroup("Scale Factor")]
    [Export] private HSlider _scaleFactorSlider;
    [Export] private Label _scaleFactorValue;

    [ExportGroup("Max Aspect Ratio")]
    [Export] private OptionButton _maxAspectRatioButton;

    [ExportGroup("Margin Slider")]
    [Export] private HSlider _guiMarginSlider;
    [Export] private Label _guiMarginValue;

    [ExportGroup("Common")]
    [Export] private Panel _border;
    [Export] private Panel _displaySettings;
    [Export] private AspectRatioContainer _arc;
    [Export] private double _hideBorderTimer = 4.0f;
    [Export] private int _borderSide = 4;
    [Export] private string _settingsPath = "user://presets/hud_settings.dat";
    [Export] private TextureButton _openSettingsButton;
    // The root Control node ("HUD") and AspectRatioContainer nodes are
    // the most important pieces of this demo.
    // Both nodes have their Layout set to Full Rect
    // (with their rect spread across the whole viewport, and Anchor set to Full Rect).
    private Vector2I _baseWindowSize;
    private Window.ContentScaleModeEnum _stretchMode;
    private Window.ContentScaleAspectEnum _stretchAspect;
    private float _scaleFactor;
    private float _guiAspectRatio;
    private float _guiMargin;

    private List<Vector2I> _supportedWindowBaseSize = new List<Vector2I>
    {
       new Vector2I(648, 648), // 1:1
       new Vector2I(640, 480), // 4:3
       new Vector2I(720, 480), // 3:2
       new Vector2I(1920, 1080), // 16:9
       new Vector2I(2556, 1179), // 19:9
    };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _windowBaseSizeButton.ItemSelected += OnWindowBaseSizeItemSelected;
        _windowStretchModeButton.ItemSelected += OnWindowStretchModeItemSelected;
        _windowStretchAspectButton.ItemSelected += OnWindowStretchAspectItemSelected;
        _scaleFactorSlider.DragEnded += OnWindowScaleFactorDragEnded;
        _maxAspectRatioButton.ItemSelected += OnGuiAspectRatioItemSelected;
        _guiMarginSlider.DragEnded += OnGuiMarginDragEnded;

        _openSettingsButton.Pressed += OnOpenSettingsPanel;
        //The `resized` signal will be emitted when the window size changes, as the root Control node
        //is resized whenever the window size changes. This is because the root Control node
        //uses a Full Rect anchor, so its size will always be equal to the window size.
        Resized += OnResized;
        CallDeferred(nameof(UpdateContainer));
        InitBaseWindowSize();
        LoadSettings(true);
        SaveSettings();
    }

    private void OnOpenSettingsPanel()
    {
        if (_displaySettings.IsVisibleInTree())
        {
            _displaySettings.Hide();
        }
        else
        {
            _displaySettings.Show();
        }
    }

    private void InitBaseWindowSize()
    {
        _supportedWindowBaseSize.ForEach(windowSize =>
        {
            string value = string.Format("{0}x{1}", windowSize.X, windowSize.Y);
            _windowBaseSizeButton.AddItem(value);
        });
    }


    public string ConvertToCamelCase(string input)
    {
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        string[] parts = input.Split('_');

        StringBuilder result = new StringBuilder();

        foreach (string part in parts)
        {
            result.Append(textInfo.ToTitleCase(part));
        }

        return result.ToString();
    }

    private void LoadSettings(bool defaultSettings = false)
    {
        FileAccess file = FileAccess.Open(_settingsPath, FileAccess.ModeFlags.Read);
        // Load default settings.
        if (!FileAccess.FileExists(_settingsPath) || null == file || defaultSettings)
        {
            int viewportWidth = ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32();
            int viewportHeight = ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32();
            _baseWindowSize = new Vector2I(viewportWidth, viewportHeight);
            Enum.TryParse(ConvertToCamelCase((string)ProjectSettings.GetSetting("display/window/stretch/mode")), out _stretchMode);
            Enum.TryParse(ConvertToCamelCase((string)ProjectSettings.GetSetting("display/window/stretch/aspect")), out _stretchAspect);
            _scaleFactor = ProjectSettings.GetSetting("display/window/stretch/scale").AsSingle();
            _guiAspectRatio = -1.0f;
            _guiMargin = 30.0f;
        }
        else
        {
            int viewportWidth = (int)file.Get32();
            int viewportHeight = (int)file.Get32();
            int stretchMode = (int)file.Get32();
            int stretchAspect = (int)file.Get32();
            float scaleFactor = (float)file.GetFloat();
            float guiAspectRatio = (float)file.GetFloat();
            int maxAspectRatio = (int)file.Get32();
            float guiMargin = (float)file.GetFloat();

            _baseWindowSize = new Vector2I(viewportWidth, viewportHeight);
            _stretchMode = (Window.ContentScaleModeEnum)stretchMode;
            _stretchAspect = (Window.ContentScaleAspectEnum)stretchAspect;
            _scaleFactor = scaleFactor;
            _guiAspectRatio = guiAspectRatio;
            _maxAspectRatioButton.Select(maxAspectRatio);
            _guiMargin = guiMargin;
        }

        _windowBaseSizeButton.Select(_supportedWindowBaseSize.FindIndex((window) => { return window == _baseWindowSize; }));
        _guiMarginSlider.Value = _guiMargin;
        _guiMarginValue.Text = _guiMargin.ToString();
    }

    private void SaveSettings()
    {
        FileAccess file = FileAccess.Open(_settingsPath, FileAccess.ModeFlags.Write);

        if (null == file)
        {
            GD.Print("Cannot open file to save: ", _settingsPath);
            return;
        }

        file.Store32((uint)_baseWindowSize.X);
        file.Store32((uint)_baseWindowSize.Y);
        file.Store32((uint)_stretchMode);
        file.Store32((uint)_stretchAspect);
        file.StoreFloat(_scaleFactor);
        file.StoreFloat(_guiAspectRatio);
        file.Store32((uint)_guiAspectRatio);
        file.StoreFloat(_guiMargin);

        GD.Print("Successfully store settings for HUD at: ", _settingsPath);
    }

    private void UpdateContainer()
    {
        // The code within this function needs to be run deferred to work around an issue with containers
        // having a 1-frame delay with updates.
        // Otherwise, `panel.size` returns a value of the previous frame, which results in incorrect
        // sizing of the inner AspectRatioContainer when using the Fit to Window setting.
        for (int i = 0; i < 2; ++i)
        {

            if (Mathf.IsEqualApprox(_guiAspectRatio, -1.0))
            {
                // Fit to Window. Tell the AspectRatioContainer to use the same aspect ratio as the window,
                // making the AspectRatioContainer not have any visible effect.
                _arc.Ratio = _border.Size.Aspect();
                // Apply GUI offset on the AspectRatioContainer's parent (Panel).
                // This also makes the GUI offset apply on controls located outside the AspectRatioContainer
                // (such as the inner side label in this demo).
                _border.OffsetTop = _guiMargin;
                _border.OffsetBottom = -_guiMargin;
            }
            else
            {
                // Constrained aspect ratio.
                _arc.Ratio = Mathf.Min(_border.Size.Aspect(), _guiAspectRatio);
                // Adjust top and bottom offsets relative to the aspect ratio when it's constrained.
                // This ensures that GUI offset settings behave exactly as if the window had the
                // original aspect ratio size.
                _border.OffsetTop = _guiMargin / _guiAspectRatio;
                _border.OffsetBottom = -_guiMargin / _guiAspectRatio;
            }

            _border.OffsetLeft = _guiMargin;
            _border.OffsetRight = -_guiMargin;

            {
                StyleBoxFlat theme = _border.Get("theme_override_styles/panel").As<StyleBoxFlat>();
                theme.BorderWidthTop = _borderSide;
                theme.BorderWidthBottom = _borderSide;
                theme.BorderWidthLeft = _borderSide;
                theme.BorderWidthRight = _borderSide;

                GetTree().CreateTimer(_hideBorderTimer).Timeout += () =>
                {
                    theme.BorderWidthTop = 0;
                    theme.BorderWidthBottom = 0;
                    theme.BorderWidthLeft = 0;
                    theme.BorderWidthRight = 0;
                };
            }

            {
                StyleBoxFlat theme = _displaySettings.Get("theme_override_styles/panel").As<StyleBoxFlat>();
                theme.BorderWidthTop = _borderSide;
                theme.BorderWidthBottom = _borderSide;
                theme.BorderWidthLeft = _borderSide;
                theme.BorderWidthRight = _borderSide;

                GetTree().CreateTimer(_hideBorderTimer).Timeout += () =>
                {
                    theme.BorderWidthTop = 0;
                    theme.BorderWidthBottom = 0;
                    theme.BorderWidthLeft = 0;
                    theme.BorderWidthRight = 0;
                };
            }
        }
    }

    public void OnGuiAspectRatioItemSelected(long index)
    {
        switch (index)
        {
            case 0:  // Fit to Window
                _guiAspectRatio = -1.0f;
                break;
            case 1:  // 5:4
                _guiAspectRatio = 5.0f / 4.0f;
                break;
            case 2:  // 4:3
                _guiAspectRatio = 4.0f / 3.0f;
                break;
            case 3:  // 3:2
                _guiAspectRatio = 3.0f / 2.0f;
                break;
            case 4:  // 16:10
                _guiAspectRatio = 16.0f / 10.0f;
                break;
            case 5:  // 16:9
                _guiAspectRatio = 16.0f / 9.0f;
                break;
            case 6:  // 21:9
                _guiAspectRatio = 21.0f / 9.0f;
                break;
        }

        CallDeferred(nameof(UpdateContainer));
    }

    private void OnResized()
    {
        CallDeferred(nameof(UpdateContainer));
    }

    public void OnGuiMarginDragEnded(bool valueChanged)
    {
        _guiMargin = (float)_guiMarginSlider.Value;
        _guiMarginValue.Text = _guiMargin.ToString();
        CallDeferred(nameof(UpdateContainer));
    }

    public void OnWindowBaseSizeItemSelected(long index)
    {
        _baseWindowSize = _supportedWindowBaseSize[(int)index];

        GetViewport().GetWindow().ContentScaleSize = _baseWindowSize;
        CallDeferred(nameof(UpdateContainer));
    }

    public void OnWindowStretchModeItemSelected(long index)
    {
        _stretchMode = (Window.ContentScaleModeEnum)index;
        GetViewport().GetWindow().ContentScaleMode = _stretchMode;

        // Disable irrelevant options when the stretch mode is Disabled.
        _windowBaseSizeButton.Disabled = _stretchMode == Window.ContentScaleModeEnum.Disabled;
        _windowStretchAspectButton.Disabled = _stretchMode == Window.ContentScaleModeEnum.Disabled;
    }

    public void OnWindowStretchAspectItemSelected(long index)
    {
        _stretchAspect = (Window.ContentScaleAspectEnum)index;
        GetTree().Root.ContentScaleAspect = _stretchAspect;
    }

    public void OnWindowScaleFactorDragEnded(bool valueChanged)
    {
        _scaleFactor = (float)_scaleFactorSlider.Value;
        _scaleFactorValue.Text = (_scaleFactor * 100).ToString();

        GetTree().Root.ContentScaleFactor = _scaleFactor;
    }

}
