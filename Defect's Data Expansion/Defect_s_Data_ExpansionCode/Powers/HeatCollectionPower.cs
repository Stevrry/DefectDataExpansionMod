using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「热量收集」：自己的过热层数一变，就对所有敌人造成伤害。
    ///
    /// 触发源是 <see cref="IHeatChangeObserver"/> —— 由 <see cref="OverheatPower"/> 在算出真实变化量之后回调，
    /// 所以满层之后再叠（被削顶、其实没变）不会触发。这一点和原版储君的「黑洞」（BlackHolePower）一样：
    /// 都是「某个量发生变化 → 对所有敌人开火」，只是黑洞盯的是星星。伤害写法也是照它抄的。
    /// </summary>
    [RegisterPower]
    public sealed class HeatCollectionPower : ModPowerTemplate, IHeatChangeObserver
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_HEAT_COLLECTION_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        public async Task OnHeatChanged(PlayerChoiceContext choiceContext, Creature owner, decimal actualDelta)
        {
            // 过热只会通知「自己身上」的观察者，这层判断只是保险
            if (owner != Owner)
            {
                return;
            }

            await DealDamageToAllEnemies(choiceContext);
        }

        /// <summary>
        /// 和原版黑洞一样：不经过攻击结算（Unpowered），所以力量之类的加成都不会改这个数。
        /// </summary>
        private async Task DealDamageToAllEnemies(PlayerChoiceContext choiceContext)
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
        }
    }
}
