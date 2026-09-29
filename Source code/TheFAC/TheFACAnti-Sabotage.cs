using Hazel;
using System;
using System.Linq;
using InnerNet;

namespace AmongUsPCMod
{
    // ============================================================================
    // 🛡️ THE FAC GLOBAL SABOTAGE INTERCEPTOR: CORE FIREWALL (v3.2.0 - FIXED)
    // ============================================================================
    public static class FACAntiSabotage
    {
        /// <summary>
        /// 🔒 ЦЕНТРАЛЕН ФИЛТЪР ЗА ГЛОБАЛНИ САБОТАЖИ: Спира Крюмейт-хакери, които пускат саботажи през чийт меню!
        /// </summary>
        public static bool VerifyIncomingSabotageRequest(PlayerControl sender, byte sabotageSystemId)
        {
            if (sender == null || sender.Data == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return true;
            if (sender.AmOwner) return true; // Пропускаме Хоста

            try
            {
                // Проверяваме дали играчът е в активен мач (извън лобито)
                if (!GameState.IsInGamePlay || GameState.IsLobby) return true;

                // ⚡ КРИТИЧНА ПРОВЕРКА: Само играчи от отбора на Импосторите имат легално право да стартират саботаж!
                bool isImpostor = sender.Data.Role != null && sender.Data.Role.IsImpostor;

                if (!isImpostor)
                {
                    string systemName = ((SystemTypes)sabotageSystemId).ToString();
                    string alertReason = $"Crewmate triggered illegal global sabotage injection on system: {systemName}";

                    // 🔊 Активираме Вашата Reactor звукова аларма, конзолен лог и скрит чат до Хоста
                    // Ръчно пренасочено към FACNotificationsManager по Ваше изискване!
                    if (FACNotificationsManager.NotifyCheat(sender, alertReason))
                    {
                        System.Console.WriteLine($"[THE FAC SABOTAGE WALL]: Violation! Player '{sender.Data.PlayerName}' flagged for illegal Sabotage ID {sabotageSystemId}.");
                    }

                    // 🪓 ТВЪРД HARD BAN ПРЕЗ ВАШИЯ ЕДИНСТВЕН ЕКЗЕКУТОР
                    var clientData = AmongUsClient.Instance.allClients.ToArray()
                        .FirstOrDefault(c => c != null && c.Id == sender.PlayerId);
                    
                    if (clientData != null)
                    {
                        ExpertManagerPlugin.PunishPlayer(clientData, $"[THE FAC SABOTAGE SHIELD]: {alertReason}", 0);
                    }

                    return false; // Сриваме и блокираме пакета на хакера на милисекундата!
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[THE FAC SABOTAGE SHIELD ERROR]: Integrity verification crash: {ex.Message}");
            }

            return true; // Пакетът е от истински Импостор – пропуска се свободно
        }
    }
}