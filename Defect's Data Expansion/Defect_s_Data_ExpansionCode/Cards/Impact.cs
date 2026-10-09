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
    /// 撞击：造成 18(23) 点伤害。获得 1 层故障，将 2 张伤口加入你的手牌中。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Impact : ModCardTemplate
    {
        /// <summary>塞进手牌的伤口张数（不随升级变化，所以直接写死，描述里也是字面量）。</summary>
        private const int WoundsAddedToHand = 2;

        public Impact() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(18m, ValueProp.Move),
            new PowerVar<Malfunction>(1m)
        ];

        /// <summary>悬停时说明它塞进来的伤口、以及它会给自己叠的故障。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromCard<Wound>(),
            HoverTipFactory.FromPower<Malfunction>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            await PowerCmd.Apply<Malfunction>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Malfunction"].BaseValue,
                Owner.Creature,
                this);

            // CombatState 声明成可空，但打牌时一定在；判空只是满足可空性检查
            if (CombatState is not { } combatState)
            {
                return;
            }

            for (int i = 0; i < WoundsAddedToHand; i++)
            {
                CardModel wound = combatState.CreateCard<Wound>(Owner);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, Owner));
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5m);
        }
    }
}
