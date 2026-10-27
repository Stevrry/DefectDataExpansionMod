using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Powers
{
    /// <summary>
    /// 「身上的过热层数真的变了」的观察者。实现这个接口的能力会被 <see cref="OverheatPower"/> 主动通知。
    ///
    /// 为什么不直接在观察者自己的 AfterPowerAmountChanged 里判断：
    /// 1. 那个钩子拿到的 amount 是**原始增量**。满层（5 层）之后继续叠会被过热削顶吃掉，
    ///    层数其实没变，照增量判断会算成「变化了」而白触发一次。
    /// 2. 钩子的调用顺序取决于能力挂上去的先后。观察者去读 power.Amount 时，
    ///    过热的钩子可能已经跑过（值被削过顶）也可能还没跑，读到的东西是不确定的。
    ///
    /// 所以判定权留在过热自己手里 —— 它算完真实变化量，再回来通知。
    /// </summary>
    public interface IHeatChangeObserver
    {
        /// <param name="owner">过热所在的那只生物（也就是层数变化的那一位）。</param>
        /// <param name="actualDelta">削顶之后真实的变化量，保证不为 0（涨为正、掉为负）。</param>
        Task OnHeatChanged(PlayerChoiceContext choiceContext, Creature owner, decimal actualDelta);
    }
}
