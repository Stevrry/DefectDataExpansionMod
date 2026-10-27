using DefectsDataExpansion.DefectsDataExpansionCode.Config;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Relics
{
    /// <summary>
    /// 空瓶子：拾起时，从牌组中选择一张牌，为其附魔：稳定（这张牌获得「保留」）。
    /// </summary>
    [RegisterRelic(typeof(SharedRelicPool))]
    public sealed class EmptyBottle : ModRelicTemplate
    {
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        /// <summary>
        /// 拾起时有一次性的选择效果，所以不能拿去和遗物商人交换（引擎靠这个属性把 IsTradable 判成 false）。
        /// </summary>
        public override bool HasUponPickupEffect => true;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EffectiveEnableExpansion;

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://DefectsDataExpansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://DefectsDataExpansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://DefectsDataExpansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时说明「稳定」是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips => HoverTipFactory.FromEnchantment<Steady>();

        public override async Task AfterObtained()
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);

            CardModel? card = (await CardSelectCmd.FromDeckForEnchantment(
                player: Owner,
                enchantment: ModelDb.Enchantment<Steady>(),
                amount: 1,
                prefs: prefs)).FirstOrDefault();

            if (card == null)
            {
                return;
            }

            CardCmd.Enchant<Steady>(card, 1m);

            // 附魔时的特效
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }
}
