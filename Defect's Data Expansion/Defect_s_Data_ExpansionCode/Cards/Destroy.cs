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
    /// 销毁：获得 3(5) 点格挡，消耗手牌中所有的状态牌。升级后额外获得「保留」。
    ///
    /// <para>「消耗手牌里的状态牌」与「加工」(Processing) 是同一套筛选，
    /// 区别只是这边不按张数发格挡、而是固定 8 点，并且有「保留」。</para>
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Destroy : ModCardTemplate
    {
        public Destroy() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        /// <summary>这张牌会给自己加格挡。</summary>
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(3m, ValueProp.Move)
        ];

        /// <summary>
        /// 「消耗」是这张牌对**别的牌**做的事，本体没有这个关键词，
        /// 但悬停时应当能查到它的说明（与「加工」一致）。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

            // 先 ToList 快照再逐个消耗：遍历时手牌会变
            foreach (CardModel card in StatusCardsInHand().ToList())
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }

        /// <summary>
        /// 升级：加上「保留」。获得的格挡+2.
        /// </summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2m);
            AddKeyword(CardKeyword.Retain);
        }

        /// <summary>手牌里的状态牌。直接按卡牌类型判断，战斗里生成的临时状态牌同样算。</summary>
        private IEnumerable<CardModel> StatusCardsInHand() =>
            PileType.Hand.GetPile(Owner).Cards.Where(card => card.Type == CardType.Status);
    }
}
