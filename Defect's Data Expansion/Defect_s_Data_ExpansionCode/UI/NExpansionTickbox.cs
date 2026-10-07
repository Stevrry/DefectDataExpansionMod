using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.addons.mega_text;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI
{
    /// <summary>
    /// 选人界面上的模组开关（原版勾选框样式）。
    ///
    /// 本体完全复用游戏设置页的勾选框场景
    ///
    /// 尺寸只留勾选框本体（原版场景默认 320×64，是给设置页整行留的宽度，这里不需要），
    /// 所以把聚焦外圈 SelectionReticle 重新锚定成铺满 64×64 的方框。
    ///
    /// 注意：本类是本模组自己的 Godot 脚本
    /// </summary>
    public partial class NExpansionTickbox : NTickbox
    {
        /// <summary>开关打开时显示的文案。</summary>
        public const string EnabledText = "数据扩展：开启";

        /// <summary>开关关闭时显示的文案。</summary>
        public const string DisabledText = "数据扩展：关闭";

        /// <summary>勾选框本体的边长。</summary>
        private const float BoxSize = 64f;

        private MegaRichTextLabel? _label;

        public NExpansionTickbox()
        {
            SetCustomMinimumSize(new Vector2(BoxSize, BoxSize));
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;

            // 复用原版场景里的勾选框视觉节点（%TickboxVisuals 等）
            ModUiUtils.TransferAllNodes(this, SceneHelper.GetScenePath("screens/settings_tickbox"));
            ShrinkFocusReticle();
        }

        /// <summary>绑定左侧文字（由 CharacterSelectToggle 创建后传入）。</summary>
        public void SetLabel(MegaRichTextLabel label)
        {
            _label = label;
            UpdateLabel();
        }

        public override void _Ready()
        {
            ConnectSignals();
            // 导入默认设置
            IsTicked = ExpansionConfig.EnableExpansion;
            UpdateLabel();
        }

        protected override void OnTick()
        {
            ApplyEnabled(true);
        }

        protected override void OnUntick()
        {
            ApplyEnabled(false);
        }

        private void ApplyEnabled(bool enabled)
        {
            ExpansionConfig.EnableExpansion = enabled;

            // 勾选框的视觉由 IsTicked 驱动，这里同步一次，避免与配置状态不一致
            IsTicked = enabled;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_label != null)
            {
                _label.Text = ExpansionConfig.EnableExpansion ? EnabledText : DisabledText;
            }
        }

        /// <summary>
        /// 原版场景里的聚焦外圈是按 320 宽的整行画的（offsets 131 / -129），
        /// 缩到 64×64 后会算成负宽度；这里把它改成铺满整个控件。
        /// </summary>
        private void ShrinkFocusReticle()
        {
            if (GetNodeOrNull<Control>("SelectionReticle") is not { } reticle)
            {
                return;
            }

            reticle.SetAnchorsPreset(LayoutPreset.FullRect);
            reticle.OffsetLeft = 0f;
            reticle.OffsetTop = 0f;
            reticle.OffsetRight = 0f;
            reticle.OffsetBottom = 0f;
        }
    }
}
