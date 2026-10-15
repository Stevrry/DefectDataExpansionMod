using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// 蓝宝石蠕虫：事件里拿到的诅咒。每当你抽到这张牌，就往手牌里塞一张复制品。
    /// </summary>
    [RegisterCard(typeof(CurseCardPool))]
    public sealed class SapphireWorm : ModCardTemplate
    {
        public SapphireWorm() : base(1, CardType.Curse, CardRarity.Curse, TargetType.None)
        {
        }

        /// <summary>诅咒不该被任何「随机生成」的效果刷出来，只能由事件给。</summary>
        public override bool CanBeGeneratedByModifiers => false;

        /// <summary>诅咒不能升级。</summary>
        public override int MaxUpgradeLevel => 0;

        /// <summary>消耗。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Exhaust
        ];

        /// <summary>卡图路径</summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>
        /// 抽到这张牌时，往手牌里塞一张复制品。
        /// </summary>
        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            // 这个钩子对「每一张被抽到的牌」都会触发，只认自己
            if (card != this)
            {
                return;
            }

            // 用 CreateClone 而不是 CreateCard：克隆会把这张牌当前的状态（升级、附魔）一并复制过去。
            // 复制品是「加入手牌」而不是「抽到」，所以不会再触发这个钩子，不会无限套娃。
            CardModel copy = CreateClone();

            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        }
    }
}
