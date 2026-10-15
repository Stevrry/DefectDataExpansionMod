using System;
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
    /// 过热：层数越高代价越重，但每失去 1 层就返还 1 点能量。
    ///
    /// 1 层：获得 2 层瓦解。
    /// 2 层：获得 2 层瓦解。
    /// 3 层：获得 3 层瓦解。
    /// 4 层：获得 5 层瓦解。
    /// 5 层：再获得能力「孤注一掷」，进入5层过热时抽2张牌。
    ///
    /// 返还能量：任何让过热变少的途径都按失去的层数一层一点返能量，
    /// 唯一的例外是「转化」
    /// </summary>
    [RegisterPower]
    public sealed class OverheatPower : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_OVERHEAT_POWER";

        /// <summary>层数上限：最高档位就是 5 层，再往上没有新效果，所以直接封顶。</summary>
        private const int MaxStacks = 5;

        /// <summary>「孤注一掷」的档位。</summary>
        private const decimal GambitTier = 5;

        /// <summary>
        /// 层数 → 瓦解总量，下标就是过热层数（0 层 = 没有）：
        /// 1~2 层给 2 层，3 层给 3 层，4 层及以上给 5 层。
        /// </summary>
        private static readonly decimal[] DisintegrationByStacks = [0m, 2m, 2m, 3m, 5m, 5m];

        /// <summary>跨进 5 层过热的那一刻抽几张牌。</summary>
        private const decimal GambitEntryDraw = 2;

        /// <summary>「转化」期间为 true，用来拦掉返还的能量。</summary>
        private bool _suppressEnergyGain;

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>过热是拿代价换能量的，能量图标一并显示（原版给能量的能力都这么做）。</summary>
        protected override bool IncludeEnergyHoverTip => true;

        /// <summary>
        /// 悬停时把两个代价都列出来。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<DisintegrationPower>(),
            HoverTipFactory.FromPower<TheGambitPower>()
        ];

        /// <summary>
        /// 「转化」预留的接口：在 <paramref name="changeStacks"/> 执行期间，过热掉层不返还能量。
        /// 本地化里「转化」的提示就是这句话：「过热被转化时，不会给予能量。」
        ///
        /// 「转化」实装后这样用：
        /// <code>
        /// if (creature.GetPower&lt;Overheat&gt;() is { } overheat)
        /// {
        ///     await overheat.WithoutEnergyGain(async () => await PowerCmd.ModifyAmount(ctx, overheat, -2m, null, null));
        /// }
        /// </code>
        /// </summary>
        public async Task WithoutEnergyGain(Func<Task> changeStacks)
        {
            bool previous = _suppressEnergyGain;
            _suppressEnergyGain = true;
            try
            {
                await changeStacks();
            }
            finally
            {
                // 还原而不是直接置 false：万一将来有嵌套调用，别把外层的标记擦掉
                _suppressEnergyGain = previous;
            }
        }

        /// <summary>
        /// 层数变化时补发差额，并按失去的层数退还能量。
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

            // 传进来的 amount 是本次的「增量」（Apply 和 ModifyAmount 两条路都是这样），
            // 而钩子又是在层数已经改完之后才回调的，所以减一下就是变化前的层数
            decimal before = Amount - amount;

            if (Amount > MaxStacks)
            {
                // 直接用 SetAmount 削顶：它不会再触发本钩子（不递归），
                // 也不走 ModifyAmount，所以不会多记一条能力变动历史。
                SetAmount(MaxStacks);
            }

            decimal after = Amount;

            // 实际变化量要在削顶之后再算 —— 用原来的增量算的话，
            // 「已经满层还继续加」会被当成掉层，白送能量
            decimal actualDelta = after - before;

            await ApplyDelta<DisintegrationPower>(choiceContext, DisintegrationFor(after) - DisintegrationFor(before));
            await ApplyDelta<TheGambitPower>(choiceContext, Delta(after >= GambitTier, before >= GambitTier));

            // 跨进 5 层的那一刻抽牌。「进入」而不是「处于」：掉出去再涨回来会再抽一次。
            if (before < GambitTier && after >= GambitTier)
            {
                await DrawOnEnteringGambitTier(choiceContext);
            }

            // 层数真的变了才通知「热量收集」这类观察者（削顶吃掉的不算变化）
            if (actualDelta != 0m)
            {
                await NotifyHeatChangeObservers(choiceContext, actualDelta);
            }

            if (actualDelta < 0m)
            {
                await RefundEnergyForLostStacks(-actualDelta);
            }
        }

        /// <summary>进入 5 层过热时抽牌。</summary>
        private async Task DrawOnEnteringGambitTier(PlayerChoiceContext choiceContext)
        {
            if (Owner.Player is not { } player)
            {
                return;
            }

            Flash();
            await CardPileCmd.Draw(choiceContext, GambitEntryDraw, player);
        }

        private async Task NotifyHeatChangeObservers(PlayerChoiceContext choiceContext, decimal actualDelta)
        {
            // 先快照：观察者有可能在反应里改动能力列表，边枚举边改会抛异常
            foreach (IHeatChangeObserver observer in Owner.Powers.OfType<IHeatChangeObserver>().ToList())
            {
                await observer.OnHeatChanged(choiceContext, Owner, actualDelta);
            }
        }

        /// <summary>按失去的层数返还能量，一层一点。</summary>
        private async Task RefundEnergyForLostStacks(decimal lostStacks)
        {
            // 「转化」走的也是掉层，但那一条不给能量
            if (_suppressEnergyGain || Owner.Player is not { } player)
            {
                return;
            }

            Flash();

            // 和原版「子程序」一样一点一点给
            for (int i = 0; i < lostStacks; i++)
            {
                await PlayerCmd.GainEnergy(1m, player);
            }
        }

        /// <summary>层数 → 瓦解总量，查 <see cref="DisintegrationByStacks"/> 那张表。</summary>
        private static decimal DisintegrationFor(decimal stacks)
        {
            int index = (int)Math.Clamp(stacks, 0m, MaxStacks);
            return DisintegrationByStacks[index];
        }

        /// <summary>差额为 0 时不发命令，免得刷出一堆空的能力变动记录。</summary>
        private async Task ApplyDelta<TPower>(PlayerChoiceContext choiceContext, decimal delta) where TPower : PowerModel
        {
            if (delta == 0m)
            {
                return;
            }

            // 要往下减却找不到实例（比如被「乱码的图标」洗掉过），就别「负着施加」造出一个负层数的能力
            if (delta < 0m && Owner.GetPower<TPower>() is null)
            {
                return;
            }

            await PowerCmd.Apply<TPower>(choiceContext, Owner, delta, Owner, null);
        }

        /// <summary>布尔量变化换算成 ±1 层。</summary>
        private static decimal Delta(bool now, bool before) => (now ? 1m : 0m) - (before ? 1m : 0m);
    }
}
