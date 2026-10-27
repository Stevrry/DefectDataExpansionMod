using DefectsDataExpansion.DefectsDataExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 过热飞弹：只有在身上有过热时才能打出；失去1层过热，造成 10(15) 点伤害。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class OverheatedMissile : ModCardTemplate
    {
        public OverheatedMissile() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DamageVar(10m, ValueProp.Move),
            // 掉温的层数。这也是 OnPlay 里索引的那一项，少了它一打出去就 KeyNotFound
            new PowerVar<OverheatPower>(1m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://DefectsDataExpansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>
        /// 身上没有过热就打不出去（同原版「终结技」）。
        /// </summary>
        protected override bool IsPlayable => Owner.Creature.GetPower<OverheatPower>() is { Amount: > 0 };

        /// <summary>有过热、能打出去时卡面泛金光（同原版「终结技」）。</summary>
        protected override bool ShouldGlowGoldInternal => IsPlayable;

        /// <summary>悬停时说明它会烧掉什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

            // 先掉温再打伤害（文案就是按这个顺序写的）。
            // 正常情况下 IsPlayable 已经保证身上有过热；这里再判一次是防自动打出之类的旁路。
            if (Owner.Creature.GetPower<OverheatPower>() is { Amount: > 0 } overheat)
            {
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    overheat,
                    -DynamicVars["OverheatPower"].BaseValue,
                    Owner.Creature,
                    this);
            }

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        /// <summary>升级只加伤害 10 → 15，掉温固定 1 层。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5m);
        }
    }
}
