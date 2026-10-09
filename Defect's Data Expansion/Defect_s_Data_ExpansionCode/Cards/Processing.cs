using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 加工：消耗手牌中所有状态牌，每张获得 4(5) 点格挡。
    ///
    /// <para>实现照原版「重振精神」(SecondWind) 写，只把它的筛选条件
    /// 「非攻击牌」换成「状态牌」。</para>
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Processing : ModCardTemplate
    {
        public Processing() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        /// <summary>这张牌会给自己加格挡（原版所有格挡牌都标它，描述变量和卡面会用到）。</summary>
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(4m, ValueProp.Move)
        ];

        /// <summary>
        /// 「消耗」是这张牌对**别的牌**做的事，本体没有这个关键词，
        /// 但悬停时应当能查到它的说明（与「重振精神」一致）。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 先 ToList 快照再逐个消耗：遍历时手牌会变
            foreach (CardModel card in StatusCardsInHand().ToList())
            {
                await CardCmd.Exhaust(choiceContext, card);
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(1m);
        }

        /// <summary>手牌里的状态牌。直接按卡牌类型判断，战斗里生成的临时状态牌同样算。</summary>
        private IEnumerable<CardModel> StatusCardsInHand() =>
            PileType.Hand.GetPile(Owner).Cards.Where(card => card.Type == CardType.Status);
    }
}
