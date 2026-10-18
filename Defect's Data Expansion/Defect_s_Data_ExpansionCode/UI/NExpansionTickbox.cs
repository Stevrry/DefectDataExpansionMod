using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI
{
    /// <summary>
    /// 选人界面右上角的模组开关（原版勾选框样式）。
    ///
    /// 本体完全复用游戏设置页的勾选框场景
    ///
    /// 尺寸只留勾选框本体（原版场景默认 320×64，是给设置页整行留的宽度，这里不需要），
    /// 所以把聚焦外圈 SelectionReticle 重新锚定成铺满 64×64 的方框。
    ///
    /// 值本身和 RitsuLib 原生设置页共用 <see cref="ExpansionConfig"/>：
    /// 多人游戏里客户端的值是主机同步下来的，此时开关显示为「跟随主机」并禁用。
    ///
    /// 注意：本类是本模组自己的 Godot 脚本
    /// </summary>
    public partial class NExpansionTickbox : NTickbox
    {
        /// <summary>开关打开时显示的文案。</summary>
        public const string EnabledText = "数据扩展：开启";

        /// <summary>开关关闭时显示的文案。</summary>
        public const string DisabledText = "数据扩展：关闭";

        /// <summary>跟随主机且主机开启时显示的文案。</summary>
        public const string FollowingHostEnabledText = "数据扩展：开启（跟随主机）";

        /// <summary>跟随主机且主机关闭时显示的文案。</summary>
        public const string FollowingHostDisabledText = "数据扩展：关闭（跟随主机）";

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

            // 导入当前生效值（联机客户端是主机同步下来的那份）
            IsTicked = ExpansionConfig.EffectiveEnableExpansion;

            // 主机同步、设置页修改都会触发，保证两处界面同步刷新
            ExpansionConfig.Changed += RefreshFromConfig;
            TreeExiting += Unsubscribe;

            RefreshFromConfig();
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
            // 跟随主机时不允许改本地值（控件已经禁用，这里再兜一层）
            if (ExpansionConfig.IsFollowingHost)
            {
                RefreshFromConfig();
                return;
            }

            // 写入会顺手落盘并触发 Changed
            ExpansionConfig.EnableExpansion = enabled;

            // 主机本地改了开关：广播给客户端
            ExpansionSync.Publish();

            RefreshFromConfig();
        }

        /// <summary>按当前生效值刷新勾选状态、可用性和文字。</summary>
        private void RefreshFromConfig()
        {
            if (!GodotObject.IsInstanceValid(this))
            {
                return;
            }

            bool effective = ExpansionConfig.EffectiveEnableExpansion;

            if (IsTicked != effective)
            {
                IsTicked = effective;
            }

            // 跟随主机时只读
            SetEnabled(!ExpansionConfig.IsFollowingHost);

            UpdateLabel();
        }

        private void Unsubscribe()
        {
            ExpansionConfig.Changed -= RefreshFromConfig;
        }

        private void UpdateLabel()
        {
            if (_label == null)
            {
                return;
            }

            bool effective = ExpansionConfig.EffectiveEnableExpansion;

            _label.Text = ExpansionConfig.IsFollowingHost
                ? (effective ? FollowingHostEnabledText : FollowingHostDisabledText)
                : (effective ? EnabledText : DisabledText);
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
