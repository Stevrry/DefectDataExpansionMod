using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events
{
    /// <summary>
    /// 事件：巨大的响声
    /// 你在丛林深处穿行。潮湿的空气忽然被一声巨响撕开——轰！
    /// 那声音沉闷而遥远，似乎来自海港方向。整片树林猛地一颤，树冠上的果实被震得纷纷坠落。
    /// 灌木深处传来窸窣声与低吼。有些动物已经赶来了。
    /// 选项一：躲开果雨 —— 获得遗物「敏捷的记忆」。
    /// 选项二：冒险拾取 —— 失去 5 点生命，获得 100 金币。
    ///
    /// 事件图片暂时使用原版事件「茂密植被」的图片。
    /// </summary>
    [RegisterActEvent(typeof(Overgrowth))]
    public sealed class AHugeNoise : ModEventTemplate
    {
        /// <summary>选项一给的那个遗物，名字塞进描述里用（同原版「沉没灯塔」）。</summary>
        private const string RelicVar = "Relic";

        /// <summary>暂时复用原版事件「茂密植被」的立绘。</summary>
        public override EventAssetProfile AssetProfile =>
            new(InitialPortraitPath: "res://images/events/dense_vegetation.png");

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new HpLossVar(5m),      // 选项二：失去 5 点生命
            new GoldVar(100),       // 选项二：获得 100 金币
            new StringVar(RelicVar, ModelDb.Relic<AgileMemory>().Title.GetFormattedText())
        ];

        /// <summary>关掉「数据扩展」总开关时本事件也不出现，与卡牌 / 遗物 / 女巫之屋保持一致。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [
            new EventOption(this, DodgeTheFruitRain, InitialOptionKey("DODGE_FRUIT"), HoverTipFactory.FromRelic<AgileMemory>()),

            // ThatDoesDamage 让按钮在「这 5 点会使角色死亡」的时候闪红
            new EventOption(this, GrabTheFruit, InitialOptionKey("GRAB_FRUIT"))
                .ThatDoesDamage(DynamicVars.HpLoss.BaseValue)
        ];

        /// <summary>选项一：白拿遗物。</summary>
        private async Task DodgeTheFruitRain()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await RelicCmd.Obtain<AgileMemory>(owner);
            SetEventFinished(PageDescription("DODGE_FRUIT"));
        }

        /// <summary>选项二：先掉血再拿钱。掉血走的是「无法格挡、不受能力影响」的直接生命损失，和原版一致。</summary>
        private async Task GrabTheFruit()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);

            await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, owner);

            SetEventFinished(PageDescription("GRAB_FRUIT"));
        }
    }
}
