using GTANetworkAPI;
using NeptuneEvo.Handles;
using NeptuneEvo.VehicleModel;
using Newtonsoft.Json;
using Redage.SDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace NeptuneEvo.VehicleModel
{
    public class VehicleCapacityDump : Script
    {
        private static readonly nLog Log = new nLog("VehicleModel.CapacityDump");

        // Команда: /vehcaps - отправляет игроку список (пагинация)
        [Command("vehcaps")]
        public void CmdVehicleCaps(ExtPlayer player, int page = 1)
        {
            try
            {
                if (player == null) return;

                const int perPage = 20;
                var list = new List<VehicleEntry>();

                foreach (var kv in vMain.VehicleData)
                {
                    var modelHash = kv.Key;
                    var info = kv.Value;
                    list.Add(new VehicleEntry
                    {
                        ModelHash = modelHash,
                        DisplayName = string.IsNullOrEmpty(info.Name) ? ("hash_" + modelHash) : info.Name,
                        Class = info.Class,
                        MaxWeightGrams = info.MaxWeight,
                        MaxWeightKg = Math.Round(info.MaxWeight / 1000.0, 2),
                        Price = info.Price
                    });
                }

                // Сортируем по названию для удобства
                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

                var totalPages = (int)Math.Ceiling(list.Count / (double)perPage);
                if (page < 1) page = 1;
                if (page > totalPages) page = totalPages;

                int start = (page - 1) * perPage;
                int end = Math.Min(start + perPage, list.Count);

                // Шлём заголовок
                Trigger.ClientEvent(player, "chat.message", $"--- Vehicle capacities (page {page}/{totalPages}) ---");

                for (int i = start; i < end; i++)
                {
                    var e = list[i];
                    // формат: Название (hash) — Класс — 150.00 кг — Цена
                    Trigger.ClientEvent(player, "chat.message", $"{e.DisplayName} ({e.ModelHash}) — {e.Class} — {e.MaxWeightKg} kg — ${e.Price}");
                }

                // подсказка
                Trigger.ClientEvent(player, "chat.message", $"Use /vehcaps {page + 1} for next page");
            }
            catch (Exception e)
            {
                Log.Write($"CmdVehicleCaps Exception: {e}");
            }
        }

        // Команда: /dumpvehcaps <path> - записать JSON на диск (сервер)
        [Command("dumpvehcaps")]
        public void CmdDumpVehicleCaps(ExtPlayer player, string path = "vehicle_caps.json")
        {
            try
            {
                var list = new List<VehicleEntry>();

                foreach (var kv in vMain.VehicleData)
                {
                    var modelHash = kv.Key;
                    var info = kv.Value;
                    list.Add(new VehicleEntry
                    {
                        ModelHash = modelHash,
                        DisplayName = string.IsNullOrEmpty(info.Name) ? null : info.Name,
                        Class = info.Class,
                        MaxWeightGrams = info.MaxWeight,
                        MaxWeightKg = Math.Round(info.MaxWeight / 1000.0, 2),
                        Price = info.Price
                    });
                }

                var json = JsonConvert.SerializeObject(list, Formatting.Indented);

                // Пишем в файл (путь относительный к папке с ресурсами/серверу)
                File.WriteAllText(path, json);

                Trigger.ClientEvent(player, "chat.message", $"Vehicle capacities dumped to {path} (items: {list.Count})");
                Log.Write($"Vehicle capacities dumped to {path} (items: {list.Count})");
            }
            catch (Exception e)
            {
                Log.Write($"CmdDumpVehicleCaps Exception: {e}");
                Trigger.ClientEvent(player, "chat.message", $"Dump failed: {e.Message}");
            }
        }

        // (Опционально) Команда отправки JSON прямо в CEF — можно использовать, если в клиенте есть обработчик
        [Command("sendvehcaps")]
        public void CmdSendVehicleCaps(ExtPlayer player)
        {
            try
            {
                var list = new List<VehicleEntry>();
                foreach (var kv in vMain.VehicleData)
                {
                    var modelHash = kv.Key;
                    var info = kv.Value;
                    list.Add(new VehicleEntry
                    {
                        ModelHash = modelHash,
                        DisplayName = string.IsNullOrEmpty(info.Name) ? null : info.Name,
                        Class = info.Class,
                        MaxWeightGrams = info.MaxWeight,
                        MaxWeightKg = Math.Round(info.MaxWeight / 1000.0, 2),
                        Price = info.Price
                    });
                }

                var json = JsonConvert.SerializeObject(list);
                // отправляем в CEF - нужно, чтобы в клиенте был обработчик "client.vehicle.OpenCapacityList"
                Trigger.ClientEvent(player, "client.vehicle.OpenCapacityList", json);
                Trigger.ClientEvent(player, "chat.message", $"Sent vehicle capacities to CEF (count: {list.Count})");
            }
            catch (Exception e)
            {
                Log.Write($"CmdSendVehicleCaps Exception: {e}");
                Trigger.ClientEvent(player, "chat.message", $"Send failed: {e.Message}");
            }
        }

        // вспомогательный класс для сериализации
        private class VehicleEntry
        {
            public uint ModelHash { get; set; }
            public string DisplayName { get; set; }
            public string Class { get; set; }
            public int MaxWeightGrams { get; set; }
            public double MaxWeightKg { get; set; }
            public int Price { get; set; }
        }
    }
}