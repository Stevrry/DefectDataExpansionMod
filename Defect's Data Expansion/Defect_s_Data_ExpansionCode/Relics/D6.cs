using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// D6：拾起时，失去你的所有遗物，然后获得相同数量的随机遗物。
    /// </summary>
    [RegisterRelic(typeof(SharedRelicPool))]
    public sealed class D6 : ModRelicTemplate
    {
        /// <summary>
        /// 遗物袋里会出现的四种稀有度，和引擎RelicGrabBag内部的表一致。
        /// 先古/事件/初始遗物永远不进遗物袋，所以变不出来。
        /// </summary>
        private static readonly RelicRarity[] AllRarities =
        [
            RelicRarity.Common,
            RelicRarity.Uncommon,
            RelicRarity.Rare,
            RelicRarity.Shop
        ];

        public override RelicRarity Rarity => RelicRarity.Rare;

        /// <summary>
        /// 拾起时有一次性的变化效果，所以不能拿去和遗物商人交换。
        /// 用完之后遗物本身留着，只是不再触发。
        /// </summary>
        public override bool HasUponPickupEffect => true;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://Defect_s_Data_Expansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://Defect_s_Data_Expansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://Defect_s_Data_Expansion/images/relics/{GetType().Name}.png");

        public override async Task AfterObtained()
        {
            List<RelicModel> originals = Owner.Relics.Where(relic => relic != this).ToList();
            if (originals.Count == 0)
            {
                return;
            }

            Flash();

            foreach (RelicModel original in originals)
            {
                await RelicCmd.Replace(original, RollReplacement());
            }
        }

        /// <summary>
        /// 任意稀有度随机抽一件，抽走之后本局不会再出现。
        /// </summary>
        private RelicModel RollReplacement()
        {
            // 从随机起点开始轮询这四种稀有度，取第一个还有货的，等价于「在还抽得到的稀有度里等概率挑一个」。
            // 故意不用 RelicFactory.RollRarity：那个只会随机出普通/罕见/稀有（50/33/17），
            int start = Owner.PlayerRng.Rewards.NextInt(AllRarities.Length);
            for (int i = 0; i < AllRarities.Length; i++)
            {
                RelicRarity rarity = AllRarities[(start + i) % AllRarities.Length];
                if (Owner.RelicGrabBag.PullFromFront(rarity, _ => true, Owner.RunState) is { } pulled)
                {
                    // 和 RelicFactory.PullNextRelicFromFront 一样，共享袋里也得删掉
                    Owner.RunState.SharedRelicGrabBag.Remove(pulled);
                    return pulled.ToMutable();
                }
            }

            // 四种稀有度全被抽空了（实际几乎不可能），退回原版的兜底遗物
            return RelicFactory.FallbackRelic.ToMutable();
        }
    }
}
