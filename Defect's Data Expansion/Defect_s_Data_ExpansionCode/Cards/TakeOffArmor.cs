using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 卸甲：获得12(15)点格挡，将一张晕眩添加到你的弃牌堆中，获得1层故障。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class TakeOffArmor : ModCardTemplate
    {
        public TakeOffArmor() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        /// <summary>这张牌会给自己加格挡。</summary>
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(12m, ValueProp.Move),
            new PowerVar<Malfunction>(1m)
        ];

        /// <summary>悬停时说明它塞进来的晕眩、以及它会给自己叠的故障。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromCard<Dazed>(),
            HoverTipFactory.FromPower<Malfunction>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

            // 正常打牌时战斗状态一定在（CombatState 声明成可空），这里顺手做一次判空
            if (CombatState is { } combatState)
            {
                CardModel dazed = combatState.CreateCard<Dazed>(Owner);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Discard, Owner));
                await Cmd.Wait(0.5f);
            }

            await PowerCmd.Apply<Malfunction>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Malfunction"].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
        }
    }
}
