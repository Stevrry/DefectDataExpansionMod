using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 蓝宝石蠕虫：由事件/被感染遗物给你的诅咒。每当这张牌被消耗，就往[gold]抽牌堆[/gold]里洗一张复制品。
    /// </summary>
    [RegisterCard(typeof(CurseCardPool))]
    public sealed class SapphireWorm : ModCardTemplate
    {
        public SapphireWorm() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
        {
        }

        /// <summary>诅咒不该被任何「随机生成」的效果刷出来，只能由事件给。</summary>
        public override bool CanBeGeneratedByModifiers => false;

        /// <summary>诅咒不能升级。</summary>
        public override int MaxUpgradeLevel => 0;

        /// <summary>虚无。永恒。不可打出。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Eternal,
            CardKeyword.Ethereal,
            CardKeyword.Unplayable
        ];

        /// <summary>卡图路径</summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>
        /// 这张牌被消耗时，往抽牌堆里洗一张复制品。
        /// </summary>
        public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
        {
            // 这个钩子对「每一张被消耗的牌」都会触发，只认自己
            if (card != this)
            {
                return;
            }

            // 用 CreateClone 而不是 CreateCard：克隆会把这张牌当前的状态（升级、附魔）一并复制过去。
            CardModel copy = CreateClone();

            // 洗进抽牌堆（Random 位置）。原版所有「塞进抽牌堆」的牌都是这个写法
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
}
