using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 熔离：消耗。生成一个熔离充能球，去除 1 名敌人的所有格挡与人工制品，造成 5(7) 点伤害。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class Magma : ModCardTemplate
    {
        public Magma() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(5m, ValueProp.Move)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>消耗。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Exhaust
        ];

        /// <summary>悬停时说明生成的球本身、充能是什么、以及被熔掉的格挡和人工制品是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromOrb<MagmaOrb>(),
            HoverTipFactory.Static(StaticHoverTip.Channeling),
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<ArtifactPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

            await OrbCmd.Channel<MagmaOrb>(choiceContext, Owner);

            // 先熔掉格挡和人工制品再打伤害
            await CreatureCmd.LoseBlock(choiceContext, cardPlay.Target, cardPlay.Target.Block, Owner.Creature);

            if (cardPlay.Target.HasPower<ArtifactPower>())
            {
                await PowerCmd.Remove<ArtifactPower>(cardPlay.Target);
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        /// <summary>升级只加伤害 5 → 7。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2m);
        }
    }
}
