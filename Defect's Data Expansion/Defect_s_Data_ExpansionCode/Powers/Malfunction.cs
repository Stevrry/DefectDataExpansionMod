using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 故障：按自身层数逐档「解锁」一串能力状态，各档效果累积叠加。
    ///
    /// 2 层：失去 2 点力量。
    /// 4 层：获得 4 层瓦解、5 点力量、2 点敏捷。
    /// 6 层：再获得 4 层瓦解，以及能力「机器学习」。
    /// 8 层：再获得 2 点敏捷，以及能力「孤注一掷」。
    ///
    /// 层数上限见 <see cref="MaxStacks"/>：8 层之后再多也只按 8 层算。
    ///
    /// </summary>
    [RegisterPower]
    public sealed class Malfunction : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_MALFUNCTION";

        /// <summary>层数上限：最高档位就是 8 层，再往上没有新效果，所以直接封顶。</summary>
        private const int MaxStacks = 8;

        // 各档位的门槛（与上面 summary 里的说明一一对应）
        private const decimal StrengthPenaltyTier = 2;

        private const decimal StrengthBonusTier = 4;

        private const decimal DisintegrationTier = 4;

        private const decimal DisintegrationTier2 = 6;

        private const decimal DexterityTier = 4;

        private const decimal DexterityTier2 = 8;

        private const decimal MachineLearningTier = 6;

        private const decimal GambitTier = 8;

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>悬停时只列出当前层数已经拿到的那些能力，层数越高提示越多。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips => BuildHoverTips();

        /// <summary>
        /// 层数变化时补发差额。
        /// </summary>
        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature? applier,
            CardModel? cardSource)
        {
            // 这个钩子对场上任何能力的变化都会触发，只处理自己
            if (power != this)
            {
                return;
            }

            // 先按「削顶前」的新层数反推旧层数，再削顶
            decimal oldAmount = Amount - amount;
            if (Amount > MaxStacks)
            {
                // 直接用 SetAmount 削顶：它不会再次触发本钩子（不递归），
                // 也不走 ModifyAmount，所以不会多记一条能力变动历史。
                SetAmount(MaxStacks);
            }

            // 之后一律按削顶后的实际层数结算，超出的部分自然不会发出任何加成
            await ApplyDelta<StrengthPower>(choiceContext, StrengthBonus(Amount) - StrengthBonus(oldAmount));
            await ApplyDelta<DisintegrationPower>(choiceContext, DisintegrationBonus(Amount) - DisintegrationBonus(oldAmount));
            await ApplyDelta<DexterityPower>(choiceContext, DexterityBonus(Amount) - DexterityBonus(oldAmount));

            // 「机器学习」「孤注一掷」是「有没有」而不是「有几层」：跨过门槛给 1 层，退回门槛以下就还回去
            await ApplyDelta<MachineLearningPower>(choiceContext, Delta(HasPassed(Amount, MachineLearningTier), HasPassed(oldAmount, MachineLearningTier)));
            await ApplyDelta<TheGambitPower>(choiceContext, Delta(HasPassed(Amount, GambitTier), HasPassed(oldAmount, GambitTier)));
        }

        /// <summary>按当前层数给出应显示的能力提示框。</summary>
        private IEnumerable<IHoverTip> BuildHoverTips()
        {
            if (Amount >= StrengthPenaltyTier)
            {
                yield return HoverTipFactory.FromPower<StrengthPower>();
            }

            if (Amount >= DisintegrationTier)
            {
                yield return HoverTipFactory.FromPower<DisintegrationPower>();
            }

            if (Amount >= DexterityTier)
            {
                yield return HoverTipFactory.FromPower<DexterityPower>();
            }

            if (Amount >= MachineLearningTier)
            {
                yield return HoverTipFactory.FromPower<MachineLearningPower>();
            }

            if (Amount >= GambitTier)
            {
                yield return HoverTipFactory.FromPower<TheGambitPower>();
            }
        }

        /// <summary>差额为 0 时不发命令，免得刷出一堆空的能力变动记录。</summary>
        private async Task ApplyDelta<TPower>(PlayerChoiceContext choiceContext, decimal delta) where TPower : PowerModel
        {
            if (delta == 0m)
            {
                return;
            }

            await PowerCmd.Apply<TPower>(choiceContext, Owner, delta, Owner, null);
        }

        // ---- 层数 → 累计加成的纯函数（= 所有已跨过档位之和）----

        /// <summary>力量：2 层 −2，4 层再 +5。</summary>
        private static decimal StrengthBonus(decimal amount) =>
            (amount >= StrengthPenaltyTier ? -2m : 0m)
            + (amount >= StrengthBonusTier ? 5m : 0m);

        /// <summary>瓦解：4 层 4 层，6 层再 4 层。</summary>
        private static decimal DisintegrationBonus(decimal amount) =>
            (amount >= DisintegrationTier ? 4m : 0m)
            + (amount >= DisintegrationTier2 ? 4m : 0m);

        /// <summary>敏捷：4 层 2 点，8 层再 2 点。</summary>
        private static decimal DexterityBonus(decimal amount) =>
            (amount >= DexterityTier ? 2m : 0m)
            + (amount >= DexterityTier2 ? 2m : 0m);

        private static bool HasPassed(decimal amount, decimal tier) => amount >= tier;

        /// <summary>布尔量变化换算成 ±1 层。</summary>
        private static decimal Delta(bool now, bool before) => (now ? 1m : 0m) - (before ? 1m : 0m);
    }
}
