using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace AmongUsPCMod;

// ============================================================================
// 📡 THE FAC RPC RADAR: FORENSIC NETWORK INTERCEPTOR (v16.2.0)
// ============================================================================
[HarmonyPatch]
public static class TheFACRpcRadar
{
    private static Dictionary<byte, long> lastRpcTimestamps = new Dictionary<byte, long>();
    private static Dictionary<byte, int> rpcCountsPerSecond = new Dictionary<byte, int>();
    
    // Списък за проследяване на играчи, които твърдят, че имат BAU при ръкостискане
    public static HashSet<byte> PendingBauHandshakes = new HashSet<byte>();

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return from type in typeof(InnerNetObject).Assembly.GetTypes()
               where typeof(InnerNetObject).IsAssignableFrom(type) && !type.IsAbstract
               select type.GetMethod("HandleRpc", BindingFlags.Public | BindingFlags.Instance)
               into method
               where method != null && method.GetBaseDefinition() != method
               select method;
    }

    [HarmonyPrefix]
    public static bool Prefix(InnerNetObject __instance, [HarmonyArgument(0)] ref byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance || reader == null) return true;

        try {
            byte senderId = reader.Tag;
            
            // Защитаваме локалния играч (себе си) от само-засичане
            if (senderId == AmongUsClient.Instance.ClientId) return true;

            var badClient = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd != null && cd.Id == senderId);
            if (badClient == null) return true;

            // ============================================================================
            // 🛡️ ЗАЩИТА НА ЛЕГИТИМНИТЕ МОДОВЕ (BAU / Final Suspect / Client Only)
            // ============================================================================
            
            // АКО Е ЛЕГАЛЕН BETTERAMONGUS ПОТРЕБИТЕЛ: Проверяваме неговия специфичен BetterRPC пакет.
            // Легитимните BAU клиенти изпращат Custom RPC пакет 101 със специфичен вътрешен идентификатор.
            if (callId == 101)
            {
                byte subTag = reader.ReadByte();
                if (subTag == 0xBA) // Специфичният BetterRPC подпис на BAU за легитимност
                {
                    // Играчът успешно доказа, че има истински BetterAmongUs клиент! Премахваме го от съмнителните.
                    PendingBauHandshakes.Remove(senderId);
                    System.Console.WriteLine($"[THE FAC SECURITY]: Legitimate BetterAmongUs user verified via BetterRPC: {senderId}");
                    return true;
                }
            }

            // ПРОВЕРКА ЗА SPOOFING: Ако играчът твърди, че има BAU (в името/FriendCode), но изпрати нормално движение (CallID 1)
            // без изобщо да е пуснал BetterRPC пакет 101, значи неговото чийт меню фалшифицира легалния сигнатур!
            if (callId == 1 && PendingBauHandshakes.Contains(senderId))
            {
                PendingBauHandshakes.Remove(senderId); // Почистваме
                
                // Насочваме известието към вашия мениджър за нотификации
                TheFACNotificationsManager.SendSovereignAlert($"Nuked fake BetterAmongUs spoofer profile! ClientID: {senderId}");
                
                // Екзекутираме твърд BAN за измама и опит за инжектиране
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Legitimate mod signature spoofing detected.", 0);
                return false;
            }

            // ============================================================================
            // 👿 ТОЧНИТЕ RPC ПОДПИСИ (SIGNATURES) ЗА TENKAIMENU, FABMENU И MODMENU
            // ============================================================================

            // 1. ПАКЕТЪТ НА TENKAIMENU (CallID 145 - Използва се за Crash Lobby / RPC Flooding)
            if (callId == 145)
            {
                TheFACNotificationsManager.SendSovereignAlert($"[CRITICAL THREAT]: TenkaiMenu execution blocked from ID: {senderId}");
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Illegal TenkaiMenu network packet signature.", 0);
                return false;
            }

            // 2. ПАКЕТЪТ НА FABMENU (CallID 255 - Мощен флууд за забиване на хоста и Force Start)
            if (callId == 255)
            {
                TheFACNotificationsManager.SendSovereignAlert($"[CRITICAL THREAT]: FabMenu bypass injection blocked from ID: {senderId}");
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Illegal FabMenu network packet signature.", 0);
                return false;
            }

            // 3. ГЛОБАЛНИЯТ ОПТИМИЗИРАН МАСИВ НА MODMENU (CallID 119 и 250 - Нелегални RPC за убийства без Cooldown)
            if (callId == 119 || callId == 250)
            {
                TheFACNotificationsManager.SendSovereignAlert($"[CRITICAL THREAT]: ModMenu exploit buffer blocked from ID: {senderId}");
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Illegal ModMenu exploit packet signature.", 0);
                return false;
            }

            // 4. МРЕЖОВ BUFFER OVERFLOW СКЕНЕР (Засича лошо написани чийт инжектори)
            if (reader.Length > 120 && callId != 4) 
            {
                TheFACNotificationsManager.SendSovereignAlert($"Malicious network overflow blocked from client: {senderId} (Buffer Size: {reader.Length} bytes)");
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Anomalous packet buffer size threat.", 0);
                return false;
            }

            // 5. ВРЕМЕВА ЗАЩИТА НА СЪБРАНИЯТА (State Checker)
            if (callId == 4 && MeetingHud.Instance == null)
            {
                TheFACNotificationsManager.SendSovereignAlert($"Client {senderId} executed illegal Meeting Vote outside valid gameplay states!");
                ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: Illegal network state transaction.", 0);
                return false;
            }

            // 6. СТАНДАРТЕН RPC FLOOD ФИЛТЪР СЪС СИГНАЛ КЪМ МЕНИДЖЪРА ЗА ИЗВЕСТИЯ
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!lastRpcTimestamps.ContainsKey(senderId)) {
                lastRpcTimestamps[senderId] = currentTime;
                rpcCountsPerSecond[senderId] = 1;
            } else {
                if (currentTime - lastRpcTimestamps[senderId] > 1000) {
                    lastRpcTimestamps[senderId] = currentTime;
                    rpcCountsPerSecond[senderId] = 1;
                } else {
                    rpcCountsPerSecond[senderId]++;
                    if (rpcCountsPerSecond[senderId] > 8) {
                        TheFACNotificationsManager.SendSovereignAlert($"Nuked RPC packet flooder: client {senderId} running too fast ({rpcCountsPerSecond[senderId]}/s)");
                        ExpertManagerPlugin.PunishPlayer(badClient, "The FAC: RPC flood attack intercepted.", 0);
                        return false;
                    }
                }
            }
        } catch { }

        return true; // Легалните пакети на вашите потребители преминават свободно
    }
}