using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Patches
{
    /// <summary>
    /// 在选人界面右上角注入模组开关（原版设置页同款的一行：左侧文字 + 右侧勾选框）。
    /// 只在选中故障机器人（且该角色未锁定）时显示。
    /// 同一份值也注册进了 RitsuLib 原生设置页（见 Config/ExpansionSettingsPage）。
    /// </summary>
    internal static class CharacterSelectToggle
    {
        /// <summary>注入的开关行节点名。</summary>
        public const string RowName = "ExpansionToggleRow";

        /// <summary>角色按钮容器，用来查「当前选中的是谁」。</summary>
        private const string ButtonContainerPath = "CharSelectButtons/ButtonContainer";

        private const float RowWidth = 320f;

        private const float RowHeight = 64f;

        /// <summary>开关行的右边缘离屏幕右边的距离。</summary>
        private const float RightMargin = 40f;

        /// <summary>开关行的上边缘离屏幕顶部的距离。</summary>
        private const float TopMargin = 40f;

        /// <summary>开关文字字号。</summary>
        private const int LabelFontSize = 22;

        /// <summary>把开关行插到界面右上角；已经插过就直接返回。</summary>
        public static Control? Inject(NCharacterSelectScreen screen)
        {
            Control? existing = screen.GetNodeOrNull<Control>(RowName);
            if (existing != null)
            {
                return existing;
            }

            MegaRichTextLabel label = ModUiUtils.CreateLabel(NExpansionTickbox.EnabledText, LabelFontSize);
            // 给文字撑满整行高度，它自己会把文字垂直居中（关掉 AutoSize 后最小高度算不出来）
            label.CustomMinimumSize = new Vector2(0f, RowHeight);
            // 文字右对齐，紧贴勾选框；否则它会在整行里左对齐，跟勾选框隔开一大截空白
            label.HorizontalAlignment = HorizontalAlignment.Right;

            NExpansionTickbox tickbox = new();
            tickbox.SetLabel(label);

            NModToggleRow row = new(RowName, label, tickbox)
            {
                // 先藏起来，等 SelectCharacter 告诉我们选的是不是鸡煲
                Visible = false,
                CustomMinimumSize = new Vector2(RowWidth, RowHeight)
            };

            screen.AddChild(row);
            AnchorTopRight(row);

            GD.Print("[DDEMod] 「数据扩展」开关已注入到选人界面右上角");
            return row;
        }

        /// <summary>按「当前选中的角色」决定开关显不显示（_Ready 时用来兜底）。</summary>
        public static void RefreshFromSelection(NCharacterSelectScreen screen)
        {
            if (!GodotObject.IsInstanceValid(screen))
            {
                return;
            }

            NCharacterSelectButton? selected = screen
                .GetNodeOrNull<Node>(ButtonContainerPath)?
                .GetChildren()
                .OfType<NCharacterSelectButton>()
                .FirstOrDefault(button => button.IsSelected);

            SetVisible(screen, selected != null && !selected.IsLocked && IsDefect(selected.Character));
        }

        /// <summary>设置开关行的显隐。</summary>
        public static void SetVisible(NCharacterSelectScreen screen, bool visible)
        {
            if (screen.GetNodeOrNull<Control>(RowName) is { } row && row.Visible != visible)
            {
                row.Visible = visible;
            }
        }

        /// <summary>判断是不是故障机器人（按类型判，同时兜一层 Entry，防止拿到的是可变副本）。</summary>
        private static bool IsDefect(CharacterModel? character) =>
            character is Defect
            || string.Equals(character?.Id.Entry, "DEFECT", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 以界面右上角为锚点摆放：右边缘离屏幕右边 <see cref="RightMargin"/>，
        /// 上边缘离屏幕顶部 <see cref="TopMargin"/>。用锚点而不是绝对坐标，窗口缩放时会自动跟随。
        /// </summary>
        private static void AnchorTopRight(Control row)
        {
            row.AnchorLeft = 1f;
            row.AnchorRight = 1f;
            row.AnchorTop = 0f;
            row.AnchorBottom = 0f;

            row.OffsetLeft = -(RowWidth + RightMargin);
            row.OffsetRight = -RightMargin;
            row.OffsetTop = TopMargin;
            row.OffsetBottom = TopMargin + RowHeight;

            row.GrowHorizontal = Control.GrowDirection.Begin;
            row.GrowVertical = Control.GrowDirection.Begin;
        }
    }

    /// <summary>补丁一：界面 _Ready 时把开关行插进去。</summary>
    public sealed class CharacterSelectToggleInjectPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_character_select_toggle_inject";

        public static string Description => "在选人界面右上角注入「数据扩展」开关";

        // 只是少一个界面开关，不该让整个模组失效
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<NCharacterSelectScreen>("_Ready")
        ];

        public static void Postfix(NCharacterSelectScreen __instance)
        {
            try
            {
                if (CharacterSelectToggle.Inject(__instance) == null)
                {
                    return;
                }

                Callable.From(() => CharacterSelectToggle.RefreshFromSelection(__instance)).CallDeferred();
            }
            catch (Exception e)
            {
                GD.PrintErr("[DDEMod] 添加「数据扩展」开关失败: " + e);
                MainFile.Logger.Error("Failed to add the expansion toggle to the character select screen: " + e);
            }
        }
    }

    /// <summary>
    /// 补丁二：选中角色时决定开关显不显示。
    ///
    /// 原版 SelectCharacter 的注释写明「打开界面时也会调用」（角色按钮获得焦点即触发），
    /// 所以这里是「只有故障机器人显示」唯一可靠的信号源。
    /// </summary>
    public sealed class CharacterSelectToggleVisibilityPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_character_select_toggle_visibility";

        public static string Description => "只在选中故障机器人时显示「数据扩展」开关";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<NCharacterSelectScreen>(
                nameof(NCharacterSelectScreen.SelectCharacter),
                typeof(NCharacterSelectButton),
                typeof(CharacterModel))
        ];

        public static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
        {
            try
            {
                CharacterSelectToggle.SetVisible(
                    __instance,
                    !charSelectButton.IsLocked && charSelectButton.Character is Defect);
            }
            catch (Exception e)
            {
                GD.PrintErr("[DDEMod] 切换「数据扩展」开关显隐失败: " + e);
            }
        }
    }
}
