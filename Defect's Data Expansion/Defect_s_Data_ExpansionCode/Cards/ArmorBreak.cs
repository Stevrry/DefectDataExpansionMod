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
    /// 爆甲：对所有敌人造成 13(15) 点伤害，将一张灼伤添加到你的弃牌堆中，获得1层过热。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class ArmorBreak : ModCardTemplate
    {
        public ArmorBreak() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(13m, ValueProp.Move),
            new PowerVar<OverheatPower>(1m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>悬停时说明它塞进来的灼伤、以及它会给自己叠的过热。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromCard<Burn>(),
            HoverTipFactory.FromPower<OverheatPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // CombatState 声明成可空，但打牌时一定在；顺手取出来也给下面塞灼伤用
            if (CombatState is not { } combatState)
            {
                return;
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            CardModel burn = combatState.CreateCard<Burn>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Discard, Owner));
            await Cmd.Wait(0.5f);

            await PowerCmd.Apply<OverheatPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["OverheatPower"].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2m);
        }
    }
}
