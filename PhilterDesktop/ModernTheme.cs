using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PhilterDesktop
{
    /// <summary>
    /// Gives the classic WinForms UI a modern Windows 11 look: Segoe UI Variable text,
    /// a flat accent-colored toolbar/menu, explorer-style list views, an immersive
    /// (dark-aware) title bar, and a light/dark palette that follows the OS setting.
    ///
    /// Usage: call <see cref="Apply"/> once per form, after InitializeComponent and
    /// before the handle is shown (the constructor is fine).
    /// </summary>
    internal static class ModernTheme
    {
        // --- Win32 interop ----------------------------------------------------

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        // DWMWA_USE_IMMERSIVE_DARK_MODE is 20 on Windows 10 1903+ / Windows 11.
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // --- Palette ----------------------------------------------------------

        /// <summary>True when the OS is set to dark "app" mode.</summary>
        public static readonly bool IsDark = DetectSystemDark();

        // Windows 11 accent-ish blue used for primary actions / selection.
        public static readonly Color Accent = Color.FromArgb(0, 103, 192);

        public static readonly Color Background = IsDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        public static readonly Color Surface = IsDark ? Color.FromArgb(43, 43, 43) : Color.White;
        public static readonly Color Text = IsDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(28, 28, 28);
        public static readonly Color SubtleText = IsDark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(96, 96, 96);
        public static readonly Color Border = IsDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(225, 225, 225);
        public static readonly Color Hover = IsDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(229, 229, 229);
        public static readonly Color Pressed = IsDark ? Color.FromArgb(72, 72, 72) : Color.FromArgb(215, 215, 215);
        public static readonly Color SelectionBack = IsDark ? Color.FromArgb(0, 75, 140) : Color.FromArgb(205, 232, 255);

        /// <summary>Segoe UI Variable is the Windows 11 UI font; fall back to Segoe UI.</summary>
        public static readonly Font UiFont = BuildUiFont(9.75f);

        private static Font BuildUiFont(float size)
        {
            foreach (string family in new[] { "Segoe UI Variable Text", "Segoe UI Variable", "Segoe UI" })
            {
                try
                {
                    var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
                    if (string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase) ||
                        family == "Segoe UI")
                    {
                        return font;
                    }
                }
                catch
                {
                    // Try the next family.
                }
            }
            return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        private static bool DetectSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                // AppsUseLightTheme == 0 means dark mode.
                return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch
            {
                return false;
            }
        }

        // --- Public entry point ----------------------------------------------

        /// <summary>Applies the modern theme to a form and all of its controls.</summary>
        public static void Apply(Form form)
        {
            form.Font = UiFont;
            form.BackColor = Background;
            form.ForeColor = Text;

            // Immersive (dark) title bar once the native window exists.
            form.HandleCreated += (_, _) => ApplyTitleBar(form.Handle);
            if (form.IsHandleCreated)
            {
                ApplyTitleBar(form.Handle);
            }

            ApplyToControls(form.Controls);
        }

        private static void ApplyTitleBar(IntPtr handle)
        {
            int useDark = IsDark ? 1 : 0;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                StyleControl(control);

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }

        private static void StyleControl(Control control)
        {
            switch (control)
            {
                case MenuStrip menu:
                    menu.Renderer = new ModernToolStripRenderer();
                    menu.BackColor = Surface;
                    menu.ForeColor = Text;
                    break;

                // StatusStrip derives from ToolStrip, so it must be matched first.
                case StatusStrip status:
                    status.Renderer = new ModernToolStripRenderer();
                    status.BackColor = Surface;
                    status.ForeColor = SubtleText;
                    break;

                case ToolStrip toolStrip:
                    toolStrip.Renderer = new ModernToolStripRenderer();
                    toolStrip.BackColor = Surface;
                    toolStrip.ForeColor = Text;
                    toolStrip.Padding = new Padding(6, 4, 6, 4);
                    break;

                case ListView listView:
                    StyleListView(listView);
                    break;

                case Button button:
                    StyleButton(button);
                    break;

                case TextBox textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Text;
                    break;

                case Label label:
                    label.ForeColor = Text;
                    label.BackColor = Color.Transparent;
                    break;

                case Panel panel:
                    panel.BackColor = Background;
                    break;

                case CheckBox or RadioButton:
                    control.ForeColor = Text;
                    control.BackColor = Color.Transparent;
                    break;
            }
        }

        private static void StyleListView(ListView listView)
        {
            listView.GridLines = false;
            listView.BorderStyle = BorderStyle.None;
            listView.BackColor = Surface;
            listView.ForeColor = Text;
            listView.FullRowSelect = true;
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            // "Explorer" theme gives modern row hover + selection rendering.
            if (listView.IsHandleCreated)
            {
                SetWindowTheme(listView.Handle, "Explorer", null);
            }
            else
            {
                listView.HandleCreated += (_, _) => SetWindowTheme(listView.Handle, "Explorer", null);
            }
        }

        private static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Hover;
            button.FlatAppearance.MouseDownBackColor = Pressed;
            button.BackColor = Surface;
            button.ForeColor = Text;
            button.Padding = new Padding(10, 4, 10, 4);
        }

        /// <summary>Promotes a button to the accent-colored primary action style.</summary>
        public static void MakePrimary(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Accent, 0.1f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(Accent, 0.1f);
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Padding = new Padding(10, 4, 10, 4);
        }
    }

    /// <summary>
    /// Flat renderer that removes the Office-style gradients from menus, toolbars,
    /// and status bars and replaces them with a clean Fluent-ish flat look.
    /// </summary>
    internal sealed class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ModernToolStripRenderer() : base(new ModernColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Draw only a subtle bottom separator instead of a full 3D border.
            if (e.ToolStrip is StatusStrip)
            {
                return;
            }

            using var pen = new Pen(ModernTheme.Border);
            var bounds = e.AffectedBounds;
            e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? ModernTheme.Text : ModernTheme.SubtleText;
            base.OnRenderItemText(e);
        }
    }

    /// <summary>Color table feeding flat hover/selection colors to the renderer.</summary>
    internal sealed class ModernColorTable : ProfessionalColorTable
    {
        public ModernColorTable()
        {
            UseSystemColors = false;
        }

        public override Color ToolStripGradientBegin => ModernTheme.Surface;
        public override Color ToolStripGradientMiddle => ModernTheme.Surface;
        public override Color ToolStripGradientEnd => ModernTheme.Surface;
        public override Color ToolStripBorder => ModernTheme.Border;
        public override Color ToolStripContentPanelGradientBegin => ModernTheme.Surface;
        public override Color ToolStripContentPanelGradientEnd => ModernTheme.Surface;
        public override Color ToolStripPanelGradientBegin => ModernTheme.Surface;
        public override Color ToolStripPanelGradientEnd => ModernTheme.Surface;

        public override Color MenuStripGradientBegin => ModernTheme.Surface;
        public override Color MenuStripGradientEnd => ModernTheme.Surface;
        public override Color MenuBorder => ModernTheme.Border;
        public override Color MenuItemBorder => ModernTheme.Hover;
        public override Color MenuItemSelected => ModernTheme.Hover;
        public override Color MenuItemSelectedGradientBegin => ModernTheme.Hover;
        public override Color MenuItemSelectedGradientEnd => ModernTheme.Hover;
        public override Color MenuItemPressedGradientBegin => ModernTheme.Surface;
        public override Color MenuItemPressedGradientEnd => ModernTheme.Surface;

        public override Color ImageMarginGradientBegin => ModernTheme.Surface;
        public override Color ImageMarginGradientMiddle => ModernTheme.Surface;
        public override Color ImageMarginGradientEnd => ModernTheme.Surface;

        public override Color ButtonSelectedGradientBegin => ModernTheme.Hover;
        public override Color ButtonSelectedGradientMiddle => ModernTheme.Hover;
        public override Color ButtonSelectedGradientEnd => ModernTheme.Hover;
        public override Color ButtonSelectedBorder => ModernTheme.Border;
        public override Color ButtonPressedGradientBegin => ModernTheme.Pressed;
        public override Color ButtonPressedGradientMiddle => ModernTheme.Pressed;
        public override Color ButtonPressedGradientEnd => ModernTheme.Pressed;
        public override Color ButtonPressedBorder => ModernTheme.Border;
        public override Color ButtonCheckedGradientBegin => ModernTheme.SelectionBack;
        public override Color ButtonCheckedGradientMiddle => ModernTheme.SelectionBack;
        public override Color ButtonCheckedGradientEnd => ModernTheme.SelectionBack;

        public override Color SeparatorDark => ModernTheme.Border;
        public override Color SeparatorLight => ModernTheme.Border;

        public override Color StatusStripGradientBegin => ModernTheme.Surface;
        public override Color StatusStripGradientEnd => ModernTheme.Surface;
    }
}
