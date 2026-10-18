using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Events
{
    /// <summary>
    /// 事件：好心人
    /// 一个戴着兜帽、脸覆面具的人朝你走来，问你想不想要强大的力量。
    /// 选项一：同意 —— 失去 12 点最大生命，获得先古遗物「低语耳环」。
    /// 选项二：不同意 —— 失去 8 点生命，获得一件随机的普通遗物。
    ///
    /// 只在一、二阶段出现，且要求血量高于 15 点。
    /// 事件图片暂时使用原版事件「水晶球」的图片。
    /// </summary>
    // 第 1 阶段有两个变体（蔓生 / 码头），两个都要注册，否则随到码头的那一局就碰不到了
    [RegisterActEvent(typeof(Overgrowth))]
    [RegisterActEvent(typeof(Underdocks))]
    [RegisterActEvent(typeof(Hive))]
    public sealed class KindPerson : ModEventTemplate
    {
        /// <summary>选项一给的遗物。</summary>
        private const string RelicVar = "Relic";

        /// <summary>选项一失去的最大生命。默认的 HpLoss 名字留给选项二那 14 点。</summary>
        private const string MaxHpLossVar = "MaxHpLoss";

        /// <summary>血量门槛。</summary>
        private const decimal MinHp = 15m;

        /// <summary>暂时复用原版事件「水晶球」的立绘。</summary>
        public override EventAssetProfile AssetProfile =>
            new(InitialPortraitPath: "res://images/events/crystal_sphere.png");

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new HpLossVar(MaxHpLossVar, 12m),   // 选项一：失去 12 点最大生命
            new HpLossVar(8m),                 // 选项二：失去 8 点生命
            new StringVar(RelicVar, ModelDb.Relic<WhisperingEarring>().Title.GetFormattedText())
        ];

        /// <summary>
        /// 血量高于 15 点才可能遇到；关掉「数据扩展」总开关时本事件也不出现，
        /// 与卡牌 / 遗物 / 另外两个事件保持一致。
        /// </summary>
        public override bool IsAllowed(IRunState runState)
        {
            if (!ExpansionConfig.EnableExpansion)
            {
                return false;
            }

            return runState.Players.All(player => (decimal)player.Creature.CurrentHp > MinHp);
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [
            // 选项一给低语耳环，并且不说明是低语耳环
            new EventOption(this, Agree, InitialOptionKey("AGREE"))
                .ThatDecreasesMaxHp(DynamicVars[MaxHpLossVar].BaseValue),

            // 选项二给的是随机遗物，悬停里说不出具体是哪一件
            new EventOption(this, Decline, InitialOptionKey("DECLINE"))
                .ThatDoesDamage(DynamicVars.HpLoss.BaseValue)
        ];

        /// <summary>选项一：签下契约，拿最大生命换低语耳环。</summary>
        private async Task Agree()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await CreatureCmd.LoseMaxHp(
                new ThrowingPlayerChoiceContext(),
                owner.Creature,
                DynamicVars[MaxHpLossVar].BaseValue,
                isFromCard: false);

            await RelicCmd.Obtain<WhisperingEarring>(owner);

            SetEventFinished(PageDescription("AGREE"));
        }

        /// <summary>
        /// 选项二：拒绝，掉 14 点血，但对方还是硬塞给你一件遗物。
        /// 遗物从本局的遗物袋里抽一件普通的，抽走之后本局不会再出现（PullNextRelicFromFront 的语义）。
        /// </summary>
        private async Task Decline()
        {
            if (Owner is not { } owner)
            {
                return;
            }

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);

            RelicModel relic = RelicFactory.PullNextRelicFromFront(owner, RelicRarity.Common).ToMutable();
            await RelicCmd.Obtain(relic, owner);

            SetEventFinished(PageDescription("DECLINE"));
        }
    }
}
