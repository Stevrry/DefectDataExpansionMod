using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「安抚机魂」的延迟效果：在你的下回合开始时获得仪式，随后自身移除。
    ///
    /// <para><c>AmountOnTurnStart != 0</c> 的判断与原版 SummonNextTurnPower 一致：
    /// 只在「本回合开始时就已经存在」的情况下触发，避免本回合被打出后立刻生效。</para>
    /// </summary>
    public sealed class SoothetheMachineSoulRitualPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        /// <summary>仪式本身会持续提供力量，一并显示。</summary>
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromPower<RitualPower>()
        ];

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner.Player && AmountOnTurnStart != 0)
            {
                Flash();
                await PowerCmd.Apply<RitualPower>(choiceContext, Owner, Amount, Owner, null);
                await PowerCmd.Remove(this);
            }
        }
    }
}
