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
    /// 加工：消耗你所有的状态牌，每张获得 4(5) 点格挡。
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
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>
        /// 「消耗」是这张牌对**别的牌**做的事，本体没有这个关键词，
        /// 但悬停时应当能查到它的说明（与「重振精神」一致）。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
            HoverTipFactory.Static(StaticHoverTip.Block)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            foreach (CardModel card in StatusCardsInCombat().ToList())
            {
                await CardCmd.Exhaust(choiceContext, card);
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(1m);
        }

        /// <summary>
        /// 战斗中所有牌堆里的状态牌（抽牌堆 / 手牌 / 弃牌堆）。
        /// </summary>
        private IEnumerable<CardModel> StatusCardsInCombat()
        {
            // PlayerCombatState 声明成可空（只有战斗中才非空），但技能牌只在战斗里打得出
            if (Owner.PlayerCombatState is not { } combatState)
            {
                return [];
            }

            return combatState.AllCards.Where(card => card.Type == CardType.Status && card.Pile is { Type: not PileType.Exhaust });
        }
    }
}
