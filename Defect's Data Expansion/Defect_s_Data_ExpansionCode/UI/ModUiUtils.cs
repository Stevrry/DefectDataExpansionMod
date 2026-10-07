using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.addons.mega_text;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.UI
{
    /// <summary>
    /// 少量界面辅助函数。
    /// </summary>
    internal static class ModUiUtils
    {
        /// <summary>RichTextLabel 会用到的全部字号主题项。</summary>
        private static readonly string[] FontSizeOverrideNames =
        [
            "font_size",
            "normal_font_size",
            "bold_font_size",
            "italics_font_size",
            "bold_italics_font_size",
            "mono_font_size"
        ];

        /// <summary>
        /// 造一个游戏设置页同款样式的富文本标签（字体、主题、对齐都照原版设置行）。
        /// </summary>
        public static MegaRichTextLabel CreateLabel(string labelText, int fontSize)
        {
            Font regular = PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_shared.tres");
            Font bold = PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");

            MegaRichTextLabel label = new()
            {
                Name = "Label",
                Theme = PreloadManager.Cache.GetAsset<Theme>("res://themes/settings_screen_line_header.tres"),
                AutoSizeEnabled = false,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                FocusMode = Control.FocusModeEnum.None,
                BbcodeEnabled = true,
                ScrollActive = false,
                VerticalAlignment = VerticalAlignment.Center,
                Text = labelText
            };

            label.AddThemeFontOverride("normal_font", regular);
            label.AddThemeFontOverride("bold_font", bold);

            // 逐个修改文本字号
            foreach (string name in FontSizeOverrideNames)
            {
                label.AddThemeFontSizeOverride(name, fontSize);
            }

            return label;
        }

        /// <summary>
        /// 把 <paramref name="sourceScene"/> 里的子节点整体搬进 <paramref name="target"/>（本体节点名也一起搬）。
        /// 用于让一个 C# 类复用原版场景的视觉结构。
        /// </summary>
        /// <param name="uniqueNames">
        /// 需要 UniqueNameInOwner = true 的子节点名，方便之后用 %Name 取。
        /// </param>
        public static void TransferAllNodes(Node target, string sourceScene, params string[] uniqueNames)
        {
            Node source = PreloadManager.Cache.GetScene(sourceScene).Instantiate();

            target.Name = source.Name;

            List<string> remaining = uniqueNames.ToList();
            foreach (Node child in source.GetChildren())
            {
                source.RemoveChild(child);

                if (remaining.Remove(child.Name))
                {
                    child.UniqueNameInOwner = true;
                }

                target.AddChild(child);
                child.Owner = target;
                SetOwnerRecursively(target, child);
            }

            source.QueueFree();
        }

        private static void SetOwnerRecursively(Node target, Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                child.Owner = target;
                SetOwnerRecursively(target, child);
            }
        }
    }
}
