using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Config
{
    /// <summary>
    /// 把「数据扩展」开关注册进 RitsuLib 的原生设置页（主菜单 / 暂停菜单）。
    /// 选人界面右上角的开关行读写的是同一个值，两处保持一致。
    /// </summary>
    internal static class ExpansionSettingsPage
    {
        /// <summary>设置页 id；以后用 <see cref="ModSettingsNavigator"/> 打开也靠它定位。</summary>
        public const string PageId = "expansion";

        public static void Register()
        {
            try
            {
                RitsuLibFramework.RegisterModSettings(MainFile.ModId, Configure, PageId);
            }
            catch (Exception e)
            {
                // 设置页只是额外入口，失败了不该连累模组内容
                MainFile.Logger.Error("Failed to register the expansion settings page: " + e);
            }
        }

        private static void Configure(ModSettingsPageBuilder page)
        {
            page.WithTitle(ModSettingsText.Literal("鸡煲：数据扩展"))
                .WithDescription(ModSettingsText.Literal("控制本模组是否参与本局游戏。"))
                .AddSection("general", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("总开关"))
                        // 跟随主机时整段隐藏，改成下面的说明段落
                        .WithVisibleWhen(() => !ExpansionConfig.IsFollowingHost)
                        .AddToggle(
                            "enableExpansion",
                            ModSettingsText.Literal("启用数据扩展"),
                            ModSettingsBindings.Callback(
                                MainFile.ModId,
                                "enableExpansion",
                                () => ExpansionConfig.EnableExpansion,
                                value =>
                                {
                                    ExpansionConfig.EnableExpansion = value;
                                    // 主机改了开关就同步给客户端
                                    ExpansionSync.Publish();
                                },
                                ExpansionConfig.Save),
                            ModSettingsText.Literal("关闭后，本模组的卡牌、遗物与事件不会出现在本局游戏中。"));
                })
                .AddSection("following-host", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("多人游戏"))
                        .WithVisibleWhen(() => ExpansionConfig.IsFollowingHost)
                        .AddParagraph(
                            "followingHostNotice",
                            ModSettingsText.Literal("已跟随主机设置"),
                            ModSettingsText.Literal("当前处于多人游戏，本局是否启用数据扩展以主机的设置为准。"));
                });
        }
    }
}
