using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using LinqToDB;
using MySqlConnector;
using Redage.SDK;

namespace NeptuneEvo.Database.Models
{
    public class    Items
    {
        private static readonly nLog Log = new nLog("Database.Items");
                
        public static void Start()
        {
            var thread = new Thread(Worker);
            thread.IsBackground = true;
            thread.Name = "ItemsSave";
            thread.Start();
        }
        private static async void Worker()
        {
            while (true)
            {
                try
                {

                    var updateListData = ItemsUpdate.Values.ToList();
                    ItemsUpdate.Clear();
                    
                    var dellListData = ItemsDelete.ToList();
                    ItemsDelete.Clear();

                    if (updateListData.Count > 0 || dellListData.Count > 0)
                    {
                        await using var db = new ServerBD("MainDB");//В отдельном потоке 

                        await ItemUpdate(db, updateListData);
                        
                        await ItemDelete(db, dellListData);
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Write($"LogsWorker Exception: {e.ToString()}");
                }
                Thread.Sleep(1000 * 30);
            }
        }
        
        //
        
        private static Dictionary<int, List<object>> ItemsUpdate = new Dictionary<int, List<object>>();
        public static bool IsItemUpdate(int sqlId) => ItemsUpdate.ContainsKey(sqlId);

        public static void AddItemUpdate(int sqlId, string locationName, int count, string data, string location, int slotId, bool isTurn = false)
        {
            try
            {
                Log.Write($"[ITEMUPDATE SYNC] Updating SqlId={sqlId}, Count={count}, Location={location}, SlotId={slotId}, IsTurn={isTurn}"); // ✅ ДОБАВИТЬ IsTurn в лог

                // ✅ СИНХРОННОЕ ОБНОВЛЕНИЕ
                using var db = new ServerBD("MainDB");

                db.ItemsData
                    .Where(x => x.AutoId == sqlId)
                    .Set(x => x.DataId, locationName)
                    .Set(x => x.ItemCount, (short)count)
                    .Set(x => x.ItemData, data)
                    .Set(x => x.Location, location)
                    .Set(x => x.SlotId, (short)slotId)
                    .Set(x => x.IsTurn, isTurn ? (sbyte)1 : (sbyte)0)
                    .Update();

                Log.Write($"[ITEMUPDATE SYNC] Updated SqlId={sqlId}, IsTurn={isTurn}"); // ✅ ДОБАВИТЬ IsTurn в лог
            }
            catch (Exception e)
            {
                Log.Write($"AddItemUpdate Exception: {e.ToString()}");
            }
        }

        private static async Task ItemUpdate(ServerBD db, List<List<object>> listData)
        {
            try
            {
                foreach (var saveData in listData)
                {
                    int autoId = Convert.ToInt32(saveData[0]);
                    string dataId = Convert.ToString(saveData[1]);
                    int itemCount = Convert.ToInt32(saveData[2]);
                    string itemData = Convert.ToString(saveData[3]);
                    string location = Convert.ToString(saveData[4]);
                    short slotId = Convert.ToInt16(saveData[5]);
                    sbyte? isTurn = saveData.Count > 6 ? (sbyte?)Convert.ToInt32(saveData[6]) : (sbyte?)0; // ✅ ПРАВИЛЬНЫЙ ТИП
                                                                                                           // ✅ НОВЫЙ ПАРАМЕТР

                    await db.ItemsData
                        .Where(v => v.AutoId == autoId)
                        .Set(v => v.DataId, dataId)
                        .Set(v => v.ItemCount, itemCount)
                        .Set(v => v.ItemData, itemData)
                        .Set(v => v.Location, location)
                        .Set(v => v.SlotId, slotId)
                        .Set(v => v.IsTurn, isTurn) // ✅ НОВЫЙ ПАРАМЕТР
                        .UpdateAsync();
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemUpdate Exception: {e.ToString()}");
            }
        }

        //

        private static List<int> ItemsDelete = new List<int>();
        public static void AddItemDelete(int sqlId)
        {
            try
            {
                Log.Write($"[ITEMDELETE SYNC] Deleting SqlId={sqlId}");

                using var db = new ServerBD("MainDB");

                db.ItemsData
                    .Where(x => x.AutoId == sqlId)
                    .Delete();

                Log.Write($"[ITEMDELETE SYNC] Deleted SqlId={sqlId}");
            }
            catch (Exception e)
            {
                Log.Write($"AddItemDelete Exception: {e.ToString()}");
            }
        }
        private static async Task ItemDelete(ServerBD db, List<int> listData)
        {
            try
            {
                foreach (var sqlId in listData)
                {
                    await db.ItemsData
                        .Where(v => v.AutoId == sqlId)
                        .DeleteAsync();
                    Log.Write($"[ITEMDELETE SYNC] Deleted SqlId={sqlId}");
                    await db.Notes
                        .Where(v => v.ItemId == sqlId)
                        .DeleteAsync();
                }
            }
            catch (Exception e)
            {
                Log.Write($"ItemDelete Exception: {e.ToString()}");
            }
        }
    }
}