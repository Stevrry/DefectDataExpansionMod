using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Networking.Sidecar;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config
{
    /// <summary>
    /// 多人游戏里的「数据扩展」开关同步：主机说了算，客户端跟随。
    ///
    /// 用的是 RitsuLib Sidecar 自带的配置同步（<see cref="RitsuLibSidecarConfigSyncService"/>），
    /// 不自己造消息：
    /// - 主机：会话建立、有人握手完成（中途加入）、以及本地改开关时，把当前值广播出去；
    /// - 客户端：收到快照后写进 <see cref="ExpansionConfig.SetHostValue"/>，内容隔离改读生效值；
    /// - 离开联机会话：清掉跟随值，回到本地设置。
    ///
    /// 客户端的修改请求一律拒绝（这个开关没有「客户端提议」的语义），所以 canClientRequest 恒为 false。
    /// </summary>
    internal static class ExpansionSync
    {
        /// <summary>配置同步的 topic 名。改名会让旧版本客户端对不上，别随意动。</summary>
        private const string Topic = "defect_s_data_expansion/enabled";

        private static INetGameService? _netService;

        private static bool _isHost;

        private static IDisposable? _sessionBound;

        private static IDisposable? _sessionUnbound;

        private static IDisposable? _handshakeCompleted;

        private static IDisposable? _topicChanged;

        public static void Initialize()
        {
            try
            {
                // 先按本地值把 topic 建起来；主机之后每次改值会重新注册（见 Publish）。
                RegisterTopic(ExpansionConfig.EnableExpansion);

                _sessionBound = RitsuLibSidecarEvents.OnSessionBound(OnSessionBound);
                _sessionUnbound = RitsuLibSidecarEvents.OnSessionUnbound(OnSessionUnbound);
                _handshakeCompleted = RitsuLibSidecarEvents.OnHandshakeCompleted(OnHandshakeCompleted);
                _topicChanged = RitsuLibSidecarEvents.OnConfigTopicChanged(OnTopicChanged);
            }
            catch (Exception e)
            {
                // 同步层不可用时退回本地设置，模组其余功能照常
                MainFile.Logger.Error("Failed to initialize expansion multiplayer sync: " + e);
            }
        }

        /// <summary>主机本地改了开关后调用；非主机调用无效。</summary>
        public static void Publish()
        {
            if (!_isHost || _netService is null)
            {
                return;
            }

            try
            {
                // 配置同步服务没有「改主机状态」的公开入口，主机侧用重新注册来写入新状态，
                // 然后再广播当前快照。版本号会回到 1，但主机是唯一发送方，客户端因此总能接受。
                RegisterTopic(ExpansionConfig.EnableExpansion);
                RitsuLibSidecarConfigSyncService.PublishHostState(_netService, Topic, 0UL, "expansion-enabled");
            }
            catch (Exception e)
            {
                // 网络层出问题不应该影响主机自己的开关
                MainFile.Logger.Error("Failed to broadcast the expansion setting: " + e);
            }
        }

        private static void RegisterTopic(bool value)
        {
            RitsuLibSidecarConfigSyncService.RegisterTopic<bool, bool>(
                Topic,
                value,
                // 客户端不能改这项设置，只能跟随主机
                canClientRequest: (_, _) => false,
                applyDelta: (_, delta) => delta);
        }

        private static void OnSessionBound(SidecarSessionBoundEvent e)
        {
            _netService = e.NetService;
            _isHost = e.NetService is NetHostGameService;

            // 新会话建立时主机先把当前值推给已经连上的对端。
            // 客户端这里不主动清空：主机的快照可能在「会话绑定」之前就被处理了，
            // 在这里清空会把刚收到的值擦掉。上一局的跟随值由 OnSessionUnbound 负责清。
            if (_isHost)
            {
                Publish();
            }
        }

        private static void OnSessionUnbound(SidecarSessionUnboundEvent _)
        {
            _netService = null;
            _isHost = false;
            ExpansionConfig.SetHostValue(null);
        }

        private static void OnHandshakeCompleted(SidecarHandshakeCompletedEvent _)
        {
            // 有对端刚完成握手（中途加入）：主机重发一次当前快照
            Publish();
        }

        private static void OnTopicChanged(SidecarConfigTopicChangedEvent e)
        {
            if (!string.Equals(e.Topic, Topic, StringComparison.Ordinal) || _isHost)
            {
                return;
            }

            if (RitsuLibSidecarConfigSyncService.TryGetTopicState<bool>(Topic, out bool value, out _))
            {
                ExpansionConfig.SetHostValue(value);
            }
        }
    }
}
