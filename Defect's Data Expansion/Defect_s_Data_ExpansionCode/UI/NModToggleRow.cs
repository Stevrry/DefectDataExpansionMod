using Godot;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI
{
    /// <summary>
    /// 一行「左边文字 + 右边控件」，样式对齐原版设置页。
    /// </summary>
    public partial class NModToggleRow : HBoxContainer
    {
        /// <summary>声明勾选框</summary>
        public Control SettingControl { get; }

        public NModToggleRow(string rowName, Control label, Control settingControl)
        {
            Name = rowName;
            SettingControl = settingControl;

            AddThemeConstantOverride("separation", 8);
            MouseFilter = MouseFilterEnum.Pass;
            FocusMode = FocusModeEnum.None;

            // 文字占满剩下的空间，控件贴右边
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            settingControl.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            settingControl.SizeFlagsVertical = SizeFlags.ShrinkCenter;

            AddChild(label);
            AddChild(settingControl);
        }
    }
}
