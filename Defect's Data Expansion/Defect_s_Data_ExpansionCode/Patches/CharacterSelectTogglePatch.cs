using System;
using System.Linq;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Patching.Models;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Patches
{
    /// <summary>
    /// 在选人界面注入模组开关（原版设置页同款的一行：左侧文字 + 右侧勾选框）。
    /// 只在选中故障机器人（且该角色未锁定）时显示。</item>
    /// </summary>
    internal static class CharacterSelectToggle
    {
        /// <summary>注入的开关行节点名。</summary>
        public const string RowName = "ExpansionToggleRow";

        /// <summary>原版「确定选择」按钮的节点路径（直接挂在界面根节点下）。</summary>
        private const string ConfirmButtonPath = "ConfirmButton";

        /// <summary>角色按钮容器，用来查「当前选中的是谁」。</summary>
        private const string ButtonContainerPath = "CharSelectButtons/ButtonContainer";

        private const float RowWidth = 320f;

        private const float RowHeight = 64f;

        /// <summary>开关行与按钮之间的空隙。</summary>
        private const float RowGap = 8f;

        /// <summary>开关文字字号。</summary>
        private const int LabelFontSize = 22;

        /// <summary>
        /// 开关行右边缘离屏幕右边的距离。
        /// </summary>
        private const float RightMargin = 40f;

        /// <summary>把开关行插到「确定选择」按钮上方；已经插过就直接返回。</summary>
        public static Control? Inject(NCharacterSelectScreen screen)
        {
            Control? existing = screen.GetNodeOrNull<Control>(RowName);
            if (existing != null)
            {
                return existing;
            }

            Control? button = screen.GetNodeOrNull<Control>(ConfirmButtonPath);
            if (button?.GetParent() is not Node parent)
            {
                GD.PrintErr($"[DDEMod] 选人界面里找不到 {ConfirmButtonPath}，「数据扩展」开关没有注入");
                return null;
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

            parent.AddChild(row);

            SyncRect(screen, row, button);

            // 窗口尺寸/布局变化时按钮会挪位置，跟着走
            button.Connect(Control.SignalName.ItemRectChanged, Callable.From(() => SyncRect(screen, row, button)));
            Callable.From(() => SyncRect(screen, row, button)).CallDeferred();

            GD.Print("[DDEMod] 「数据扩展」开关已注入到「确定选择」按钮上方");
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
        /// 把开关行摆到按钮正上方：横向以<b>屏幕右边</b>为锚点（留 <see cref="RightMargin"/> 的边距），
        /// 纵向底边在按钮上边留 <see cref="RowGap"/> 的空隙。
        /// </summary>
        private static void SyncRect(NCharacterSelectScreen screen, NModToggleRow row, Control button)
        {
            if (!GodotObject.IsInstanceValid(screen)
                || !GodotObject.IsInstanceValid(row)
                || !GodotObject.IsInstanceValid(button))
            {
                return;
            }

            float right = screen.Size.X - RightMargin;
            row.Size = new Vector2(RowWidth, RowHeight);
            row.Position = new Vector2(right - RowWidth, button.GetRect().Position.Y - RowHeight - RowGap);
        }
    }

    /// <summary>补丁一：界面 _Ready 时把开关行插进去。</summary>
    public sealed class CharacterSelectToggleInjectPatch : IPatchMethod
    {
        public static string PatchId => "defect_s_data_expansion_character_select_toggle_inject";

        public static string Description => "在选人界面「确定选择」按钮上方注入「数据扩展」开关";

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
