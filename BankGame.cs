using BotFramework;
using BotFramework.Guilds;
using BotFramework.Room;
using BotFramework.Room.Items;
using HabboBankGame.Banker.Data;
using HabboBankGame.Game;
using MySqlConnector;
using Sulakore.Communication;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace HabboBankGame
{
    public class BankGame
    {
        private Client client;
        private IHConnection Connection;

        public static int FURNITYPE_BANZAI_TELE = 3642;
        public static int FURNITYPE_STACK1X1 = 5103;
        public static int FURNITYPE_COIN_1 = 2064;
        public static int FURNITYPE_COIN_5 = 2067;
        public static int FURNITYPE_COIN_10 = 2063;
        public static int FURNITYPE_COIN_20 = 2065;
        public static int FURNITYPE_COIN_50 = 2066;
        public static int FURNITYPE_DICEMASTER = 284;
        public static int FURNITYPE_GAMELIFE = 8047;

        private int WiredTalkFurniEffectId = 533224427;
        private int WiredQueue_Tele = 481160319;
        private int WiredQueue_GateVIP = 542807110;
        private int WiredQueue_GateNorm = 542807063;
        public static int WiredBopperEffectId = 432664684;
        public int WiredBanzai_Trigger = 564524732;
        public int WiredBanzai_Effect = 589052758;

        public static int GuildVipsId = 548547;

        public List<GamePlayer> Players;
        public List<string> VIPs;
        private Dictionary<int, BotFramework.Room.Items.Point> rareGains;
        public ConcurrentDictionary<int, DateTime> playersLastHumanInteraction = new ConcurrentDictionary<int, DateTime>();

        public BankGame(Client client)
        {
            this.client = client;
            Connection = this.client.GetModule().Connection;
            playersLastHumanInteraction = new ConcurrentDictionary<int, DateTime>();

            Players = new List<GamePlayer>();
            VIPs = new List<string>();
            rareGains = new Dictionary<int, BotFramework.Room.Items.Point>()
            {
                { 706235217, new BotFramework.Room.Items.Point(2, 1) }, // rare gain #01
                { 706235305, new BotFramework.Room.Items.Point(2, 1) }, // rare gain #2
                { 706235353, new BotFramework.Room.Items.Point(2, 1) }, // rare gain #3
                { 706235361, new BotFramework.Room.Items.Point(2, 1) }, // rare gain #4
                { 706235368, new BotFramework.Room.Items.Point(2, 1) }, // rare gain #5
            };

            List<KeyValuePair<string, BotFramework.Room.Items.Point>> spaces = new List<KeyValuePair<string, BotFramework.Room.Items.Point>>() {
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 1", new BotFramework.Room.Items.Point(14, 22)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 2", new BotFramework.Room.Items.Point(18, 22)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 3", new BotFramework.Room.Items.Point(22, 22)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 4", new BotFramework.Room.Items.Point(26, 22)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 5", new BotFramework.Room.Items.Point(30, 22)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 6", new BotFramework.Room.Items.Point(14, 18)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 7", new BotFramework.Room.Items.Point(18, 18)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 8", new BotFramework.Room.Items.Point(22, 18)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 9", new BotFramework.Room.Items.Point(26, 18)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 10", new BotFramework.Room.Items.Point(30, 18)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 11", new BotFramework.Room.Items.Point(14, 14)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 12", new BotFramework.Room.Items.Point(18, 14)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 13", new BotFramework.Room.Items.Point(22, 14)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 14", new BotFramework.Room.Items.Point(26, 14)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 15", new BotFramework.Room.Items.Point(30, 14)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 16", new BotFramework.Room.Items.Point(14, 10)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 17", new BotFramework.Room.Items.Point(18, 10)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 18", new BotFramework.Room.Items.Point(22, 10)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 19", new BotFramework.Room.Items.Point(26, 10)),
                new KeyValuePair<string, BotFramework.Room.Items.Point>("Player 20", new BotFramework.Room.Items.Point(30, 10)),
            };

            foreach (KeyValuePair<string, BotFramework.Room.Items.Point> kvp in spaces)
            {
                int startX = kvp.Value.X;
                int startY = kvp.Value.Y;
                Players.Add(new GamePlayer(kvp.Key)
                {
                    Pos_Chair = new BotFramework.Room.Items.Point(startX + 1, startY),
                    Pos_Balance = new BotFramework.Room.Items.Point(startX, startY + 1),
                    Pos_Banzai = new BotFramework.Room.Items.Point(startX + 1, startY + 1),
                    Pos_Dice = new BotFramework.Room.Items.Point(startX + 2, startY + 1)
                });
            }

            this.client.GetGuildsHandler().GuildMemberRequestedEvent += BankGame_GuildMemberRequestedEvent;
            this.client.GetRoomHandler().CurrentRoomIdChangedEvent += BankGame_CurrentRoomIdChangedEvent;

            //await Connection.SendToServerAsync(client.OUT_ROOMS_LEAVE, new object[] { });
            //await Task.Delay(2000);
            //await Connection.SendToClientAsync(client.GetModule().In.ForwardToRoom, new object[] { 68169673 });
            //await Task.Delay(2000);
            //await this.Connection.SendToServerAsync(Out.RequestInventoryItems, new object[] { });
            //await Connection.SendToServerAsync(client.OUT_ROOMS_ENTER, new object[] { 68169673, "", -1 });
        }

        private void BankGame_CurrentRoomIdChangedEvent(object sender, BotFramework.Room.Events.CurrentRoomIdChangedEventArgs e)
        {
            if(client.GetRoomHandler().CurrentRoom != null)
            {
                this.client.GetRoomHandler().CurrentRoom.RoomUnitAddedEvent -= CurrentRoom_RoomUnitAddedEvent;
                this.client.GetRoomHandler().CurrentRoom.RoomUnitAddedEvent += CurrentRoom_RoomUnitAddedEvent;
                this.client.GetRoomHandler().CurrentRoom.RoomUnitTalkedEvent -= CurrentRoom_RoomUnitTalkedEvent;
                this.client.GetRoomHandler().CurrentRoom.RoomUnitTalkedEvent += CurrentRoom_RoomUnitTalkedEvent;

                foreach (RoomUnit unit in this.client.GetRoomHandler().CurrentRoom.Units.Values)
                {
                    CurrentRoom_RoomUnitAddedEvent(this, new BotFramework.Room.Events.RoomUnitAddedEventArgs(this.client.GetRoomHandler().CurrentRoom, unit));
                }
            }
        }

        private async void CurrentRoom_RoomUnitTalkedEvent(object sender, BotFramework.Room.Events.RoomUnitTalkedEventArgs e)
        {
            if (client.GetRoomHandler().CurrentRoom == null)
                return;

            using (AppDb db = new AppDb())
            {
                await db.TryConnect();

                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO logs_chat (room_id, user_id, username, text, type) VALUES (@room_id, @user_id, @username, @text, @type)";
                    cmd.Parameters.AddWithValue("room_id", client.GetRoomHandler().CurrentRoom.Id);
                    cmd.Parameters.AddWithValue("user_id", e.Unit.UserId);
                    cmd.Parameters.AddWithValue("username", e.Unit.Username);
                    cmd.Parameters.AddWithValue("text", e.ChatMessage.Message);
                    cmd.Parameters.AddWithValue("type", e.ChatMessage.Type.ToString());
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (e.Unit.Type == 1)
            {
                playersLastHumanInteraction.AddOrUpdate(e.Unit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
            }
        }

        ConcurrentStack<RoomUnit> playersToBan = new ConcurrentStack<RoomUnit>();
        bool isRunningBan = false;

        private async void CurrentRoom_RoomUnitAddedEvent(object sender, BotFramework.Room.Events.RoomUnitAddedEventArgs e)
        {
            if (e.unit.Type == 1)
            {
                playersLastHumanInteraction.AddOrUpdate(e.unit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });

                if(e.unit.GroupName.Contains("Amro's"))
                {
                    playersToBan.Push(e.unit);
                }

                if (!isRunningBan && playersToBan.Count > 0) {
                    isRunningBan = true;

                    while(playersToBan.Count > 0)
                    {
                        if(playersToBan.TryPop(out RoomUnit unit1))
                        {
                            await unit1.Kick(this.client);
                            await Task.Delay(1000);
                        }
                    }

                    isRunningBan = false;
                }

                try
                {
                    using (var db = new AppDb())
                    {
                        await db.TryConnect();

                        using (var cmd = db.Connection.CreateCommand())
                        {
                            cmd.CommandText = @"INSERT INTO habbos (habbo_id, username, motto, look) VALUES (@habbo_id, @username, @motto, @look) ON DUPLICATE KEY UPDATE username = VALUES(username), motto = VALUES(motto), look = VALUES(look)";
                            cmd.Parameters.AddWithValue("habbo_id", e.unit.UserId);
                            cmd.Parameters.AddWithValue("username", e.unit.Username);
                            cmd.Parameters.AddWithValue("motto", e.unit.Motto);
                            cmd.Parameters.AddWithValue("look", e.unit.Look);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async void BankGame_GuildMemberRequestedEvent(object sender, BotFramework.Guilds.Events.GuildMemberRequestedEventArgs e)
        {
            if (e.GuildId == GuildVipsId)
            {
                User user = User.GetUser(e.Member.UserId);
                if (user == null || user.Bank_Balance < 150 || user.AccountBusy)
                {
                    await ((GuildsHandler)sender).DeclineMembership(e.GuildId, e.Member.UserId);
                    return;
                }

                BankTransaction txn = new BankTransaction()
                {
                    Action = "remove",
                    Amount = -150,
                    Note = "Purchase of VIP at Bank Game",
                    Timestamp = DateTime.UtcNow
                };

                await user.AddTransaction(txn);
                await ((GuildsHandler)sender).AcceptMembership(e.GuildId, e.Member.UserId);
                VIPs.Add(e.Member.Username);
            }

            if (e.GuildId == 395706)
            {
                User user = User.GetUser(e.Member.UserId);
                if (user == null || user.Bank_Balance < 100 || user.AccountBusy)
                {
                    await ((GuildsHandler)sender).DeclineMembership(e.GuildId, e.Member.UserId);
                    return;
                }

                BankTransaction txn = new BankTransaction()
                {
                    Action = "remove",
                    Amount = -100,
                    Note = "Purchase of VIP at Risk-it",
                    Timestamp = DateTime.UtcNow
                };

                await user.AddTransaction(txn);
                await ((GuildsHandler)sender).AcceptMembership(e.GuildId, e.Member.UserId);
            }
        }

        public BotFramework.Room.Items.Point GetItemDefaultPosition(FloorItem item)
        {
            if (rareGains.ContainsKey(item.Id))
            {
                return rareGains[item.Id];
            }

            return new BotFramework.Room.Items.Point(-1, -1);
        }

        private async void UpdateLeaderboard1()
        {
            try
            {
                DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(DiscordWebhookMessage), new DataContractJsonSerializerSettings
                {
                    DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ss.fff+0000")
                });

                List<string> leaderboard = new List<string>();


                using (var db = new AppDb())
                {
                    await db.TryConnect();

                    using (var cmd = new MySqlCommand("SELECT COUNT(*) AS `wins`, habbos.username FROM `logs_bank_transactions` LEFT JOIN habbos ON habbos.habbo_id = logs_bank_transactions.habbo_id WHERE logs_bank_transactions.description = 'Prize from game room - Bank Game' GROUP BY logs_bank_transactions.habbo_id ORDER BY `wins` DESC LIMIT 20;", db.Connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int count = 1;
                            while (reader.Read())
                            {
                                leaderboard.Add("<:empty:785449989828247612>**#" + count + ":** " + reader.GetString("username") + " (" + string.Format("{0:N0}", reader.GetInt32("wins")) + " wins)");
                                count++;
                            }
                        }
                    }
                }

                string jsonString = "";

                using (var jsonStream = new MemoryStream())
                {
                    _serializer.WriteObject(jsonStream, new DiscordWebhookMessage()
                    {
                        Content = String.Join("\n", leaderboard)
                    });
                    jsonString = Encoding.UTF8.GetString(jsonStream.ToArray());
                }

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await PatchAsync(httpClient, new Uri("https://discordapp.com/api/webhooks/785470916951408672/8V8tQRigonLsGhRPAAp93SI6OhkewsYJDEIV6wsS3yfBzuvIlDiLAf0jkHPZA9WPwQqE/messages/785473597522182145"), content);
            }
            catch (Exception e)
            {
            }
        }

        private async void UpdateLeaderboard2()
        {
            try
            {
                DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(DiscordWebhookMessage), new DataContractJsonSerializerSettings
                {
                    DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ss.fff+0000")
                });

                List<string> leaderboard = new List<string>();


                using (var db = new AppDb())
                {
                    await db.TryConnect();

                    using (var cmd = new MySqlCommand("SELECT SUM(amount) AS `wins`, habbos.username FROM `logs_bank_transactions` LEFT JOIN habbos ON habbos.habbo_id = logs_bank_transactions.habbo_id WHERE logs_bank_transactions.description = 'Prize from game room - Bank Game' GROUP BY logs_bank_transactions.habbo_id ORDER BY `wins` DESC LIMIT 20;", db.Connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int count = 1;
                            while (reader.Read())
                            {
                                leaderboard.Add("<:empty:785449989828247612>**#" + count + ":** " + reader.GetString("username") + " (" + string.Format("{0:N0}", reader.GetInt32("wins")) + " coins)");
                                count++;
                            }
                        }
                    }
                }

                string jsonString = "";

                using (var jsonStream = new MemoryStream())
                {
                    _serializer.WriteObject(jsonStream, new DiscordWebhookMessage()
                    {
                        Content = String.Join("\n", leaderboard)
                    });
                    jsonString = Encoding.UTF8.GetString(jsonStream.ToArray());
                }

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await PatchAsync(httpClient, new Uri("https://discordapp.com/api/webhooks/785470916951408672/8V8tQRigonLsGhRPAAp93SI6OhkewsYJDEIV6wsS3yfBzuvIlDiLAf0jkHPZA9WPwQqE/messages/785473828897423390"), content);
            }
            catch (Exception e)
            {
            }
        }

        private async void UpdateLeaderboard3()
        {
            try
            {
                DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(DiscordWebhookMessage), new DataContractJsonSerializerSettings
                {
                    DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ss.fff+0000")
                });

                List<string> leaderboard = new List<string>();


                using (var db = new AppDb())
                {
                    await db.TryConnect();

                    using (var cmd = new MySqlCommand("SELECT amount AS `wins`, habbos.username FROM `logs_bank_transactions` LEFT JOIN habbos ON habbos.habbo_id = logs_bank_transactions.habbo_id WHERE logs_bank_transactions.description = 'Prize from game room - Bank Game' ORDER BY `wins` DESC LIMIT 20;", db.Connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int count = 1;
                            while (reader.Read())
                            {
                                leaderboard.Add("<:empty:785449989828247612>**#" + count + ":** " + reader.GetString("username") + " (" + string.Format("{0:N0}", reader.GetInt32("wins")) + " coins)");
                                count++;
                            }
                        }
                    }
                }

                string jsonString = "";

                using (var jsonStream = new MemoryStream())
                {
                    _serializer.WriteObject(jsonStream, new DiscordWebhookMessage()
                    {
                        Content = String.Join("\n", leaderboard)
                    });
                    jsonString = Encoding.UTF8.GetString(jsonStream.ToArray());
                }

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await PatchAsync(httpClient, new Uri("https://discordapp.com/api/webhooks/785470916951408672/8V8tQRigonLsGhRPAAp93SI6OhkewsYJDEIV6wsS3yfBzuvIlDiLAf0jkHPZA9WPwQqE/messages/785474053669650483"), content);
            }
            catch (Exception e)
            {
            }
        }

        private async void AddWinLog(string username, int prize)
        {
            try
            {
                DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(DiscordWebhookMessage), new DataContractJsonSerializerSettings
                {
                    DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ss.fff+0000")
                });

                string jsonString = "";

                using (var jsonStream = new MemoryStream())
                {
                    _serializer.WriteObject(jsonStream, new DiscordWebhookMessage()
                    {
                        Content = "**" + username + "** won " + string.Format("{0:N0}", prize) + " coins in Bank Game!"
                    });
                    jsonString = Encoding.UTF8.GetString(jsonStream.ToArray());
                }

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await PostAsync(httpClient, new Uri("https://discordapp.com/api/webhooks/785470215467040778/VLTZG2vkwgAj5qLBO_sPcQb4HVzEX4gwpZmJ9-TWoetMQHghDpS8Mh7jt1GfcM0nQfG5"), content);
            }
            catch (Exception e)
            {
            }
        }

        public async Task<HttpResponseMessage> PostAsync(HttpClient client, Uri requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("POST");

            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (TaskCanceledException e)
            {
            }

            return response;
        }

        public async Task<HttpResponseMessage> PatchAsync(HttpClient client, Uri requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");

            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (TaskCanceledException e)
            {
            }

            return response;
        }

        public string GetRandomText(string[] text)
        {
            return text[new Random().Next(0, text.Length - 1)];
        }

        public async Task MakeBotTalk(string name, string message, bool shout = false)
        {
            await Connection.SendToServerAsync(this.client.GetModule().Out.WiredEffectSaveData, new object[] {
                WiredTalkFurniEffectId, // furni id
                1, // something
                shout ? 1 : 0, // mode
                name + Convert.ToChar(9) + message, // botName + \t (char 9) + message
                0, // items count
                0, // delay in 0.5s
                0 // something
            });
            await Task.Delay(600);
            await Connection.SendToServerAsync(client.OUT_ROOMS_ITEMS_TOGGLE, new object[] { WiredTalkFurniEffectId, 0 });
        }

        private bool queueVipTurn = false;

        public async Task OpenQueue()
        {
            Room room = client.GetRoomHandler().CurrentRoom;
            GamePlayer freePlace = null;
            foreach (GamePlayer player in Players)
            {
                if (player.State != 0)
                    continue;

                if (room.Units.Where(x => (x.Value.Tile.X == player.Pos_Chair.X && x.Value.Tile.Y == player.Pos_Chair.Y)).Count() == 0)
                {
                    freePlace = player;
                    break;
                }
            }

            if (freePlace == null)
                return;

            await Connection.SendToServerAsync(this.client.GetModule().Out.WiredEffectSaveData, new object[] {
                WiredQueue_Tele, // furni id
                0,
                "",
                1, // items count
                room.FloorItems.Where(x => x.Value.Tile.X == freePlace.Pos_Chair.X &&x.Value.Tile.Y == freePlace.Pos_Chair.Y).First().Value.Id,
                0, // delay in 0.5s
                0 // something
            });
            await Task.Delay(600);

            FloorItem gateVIP = room.FloorItems.Where(x => x.Value.Id == WiredQueue_GateVIP).First().Value;
            int usersVIP = room.Units.Where(x => x.Value.Tile.X == gateVIP.Tile.X - 1 && x.Value.Tile.Y == gateVIP.Tile.Y).Count();
            FloorItem gateNorm = room.FloorItems.Where(x => x.Value.Id == WiredQueue_GateNorm).First().Value;
            int usersNorm = room.Units.Where(x => x.Value.Tile.X == gateNorm.Tile.X - 1 && x.Value.Tile.Y == gateNorm.Tile.Y).Count();

            if ((queueVipTurn || usersNorm == 0) && usersVIP > 0 && (string)gateVIP.Stuff[0] != "1")
            {
                await gateVIP.Toggle(client);
                queueVipTurn = false;
            }
            else if (usersNorm > 0 && (string)gateNorm.Stuff[0] != "1")
            {
                await gateNorm.Toggle(client);
                queueVipTurn = true;
            }
        }

        private DateTime nextReloadVIPs = DateTime.Now;

        public bool IsPaused = true;

        public static string CurrentGameId;

        public async Task gameLoop()
        {
            DateTime lastPlayedJoined = DateTime.Now;
            DateTime nextTalk = DateTime.Now;
            DateTime nextQueueOpen = DateTime.Now;
            Room room = client.GetRoomHandler().CurrentRoom;

            bool gameStarted = false;
            while (!gameStarted && !this.client.GetModule().IsDisposed)
            {
                UpdateLeaderboard1();
                UpdateLeaderboard2();
                UpdateLeaderboard3();

                while (IsPaused)
                {
                    await Task.Delay(50);
                }

                if (nextReloadVIPs <= DateTime.Now)
                {
                    nextReloadVIPs = DateTime.Now.AddMinutes(20);
                    Task<List<GuildMember>> t = client.GetGuildsHandler().GetAllGuildMembers(GuildVipsId);
                    await t;

                    VIPs.Clear();
                    foreach (GuildMember m in t.Result)
                    {
                        VIPs.Add(m.Username);
                    }
                }

                int playersMax = Players.Count;
                int playersRequired = 10;
                int playersNow = 0;

                foreach (GamePlayer player in Players)
                {
                    while (IsPaused)
                    {
                        await Task.Delay(50);
                    }

                    RoomUnit playerUnit = player.GetUnit(client);
                    if (player.GetUnit(client) != null)
                    {
                        if (player.State == 0)
                        {
                            lastPlayedJoined = DateTime.Now;
                            await onPlayerJoined(player);
                            player.State = 1;

                            if (playerUnit != null)
                            {
                                playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                            }
                        }
                        playersNow++;
                    }
                    else
                    {
                        player.State = 0;
                        await player.onDeath(client, true);
                    }
                }

                string waitMessage = playersNow + "/" + playersRequired + " players. " + GetRandomText(new string[] { "Invite your friends!", "Game starting soon.", "Waiting for more players..." });
                string promoMessage = GetRandomText(new string[] { "¥ BECOME VIP FOR 150 COINS ¥ 3 LIVES, VIP QUEUE + START WITH 12c ¥", "How to Buy VIP: Put 150c into your bank balance and request the group gate." });

                if (nextTalk <= DateTime.Now)
                {
                    string msg = waitMessage;
                    if (new Random().Next(0, 5) == 1)
                    {
                        msg = promoMessage;
                    }
                    else if (playersNow >= playersRequired)
                    {
                        TimeSpan span = (lastPlayedJoined - DateTime.Now.AddSeconds(-15));
                        msg = playersNow + "/" + playersMax + " players. Starting ";

                        if (span.Seconds > 0)
                        {
                            msg += "in " + span.Seconds + " seconds";
                        }
                        else
                        {
                            msg += "now";
                        }
                    }

                    await MakeBotTalk("HOST", msg, true);
                    nextTalk = DateTime.Now.AddSeconds(15);
                }

                if (playersNow == playersMax || (playersNow >= playersRequired && lastPlayedJoined <= DateTime.Now.AddSeconds(-15) && nextQueueOpen <= DateTime.Now))
                {
                    gameStarted = true;
                }

                if (!gameStarted && nextQueueOpen <= DateTime.Now && playersNow < playersMax)
                {
                    await OpenQueue();
                    nextQueueOpen = DateTime.Now.AddSeconds(5);
                }

                await Task.Delay(550);
            }

            if (this.client.GetModule().IsDisposed)
                return;

            if (gameStarted)
            {
                CurrentGameId = Guid.NewGuid().ToString();

                using (var db = new AppDb())
                {
                    await db.TryConnect();

                    using (var cmd = db.Connection.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                        cmd.Parameters.AddWithValue("habbo_id", 0);
                        cmd.Parameters.AddWithValue("type", "started");
                        cmd.Parameters.AddWithValue("details", CurrentGameId);
                        cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    foreach (GamePlayer player in this.Players.Where(x => x.State == 1))
                    {
                        player.GetUnit(client);
                        if (player.LastKnownUnit != null)
                        {
                            using (var cmd = db.Connection.CreateCommand())
                            {
                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                cmd.Parameters.AddWithValue("type", "joined");
                                cmd.Parameters.AddWithValue("details", player.Nickname);
                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                while (IsPaused)
                {
                    await Task.Delay(50);
                }

                FloorItem gate = room.FloorItems.Where(x => x.Value.Id == WiredQueue_GateVIP).First().Value;
                if ((string)gate.Stuff[0] != "0")
                {
                    await gate.Toggle(client);
                    await Task.Delay(550);
                }

                gate = room.FloorItems.Where(x => x.Value.Id == WiredQueue_GateNorm).First().Value;
                if ((string)gate.Stuff[0] != "0")
                {
                    await gate.Toggle(client);
                    await Task.Delay(550);
                }

                FloorItem dice = room.FloorItems.Where(x => x.Value.TypeId == FURNITYPE_DICEMASTER).First().Value;
                await dice.MoveRotate(client, 8, 10, 0);

                await MakeBotTalk("HOST", "Welcome to Bank Game!", true);
                await MakeBotTalk("HOST", "HTP: Be the last person in the game. Each player takes turns to roll the dice..", true);
                await MakeBotTalk("HOST", "If you lose all your coins you lose a life. Prize is the winner's balance.", true);
                await MakeBotTalk("HOST", "1: Gain coin   2: Lose coin   3: Pick to Kick", true);
                await MakeBotTalk("HOST", "4: Lose a Life   5: Steal coin   6: Donate coin", true);

                int round = 0;

                while (!this.client.GetModule().IsDisposed)
                {
                    while (IsPaused)
                    {
                        await Task.Delay(50);
                    }

                    round++;
                    await cleanBoard();
                    await Task.Delay(550);

                    IEnumerable<GamePlayer> activePlayers = Players.Where(x => x.State == 1);

                    if (activePlayers.Count() == 0)
                    {
                        await dice.MoveRotate(client, 8, 10, 0);
                        await MakeBotTalk("HOST", "No players left. Resetting game.", true);

                        using (var db = new AppDb())
                        {
                            await db.TryConnect();

                            using (var cmd = db.Connection.CreateCommand())
                            {
                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                cmd.Parameters.AddWithValue("habbo_id", 0);
                                cmd.Parameters.AddWithValue("type", "no_winner");
                                cmd.Parameters.AddWithValue("details", "No players left");
                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            using (var cmd = db.Connection.CreateCommand())
                            {
                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                cmd.Parameters.AddWithValue("habbo_id", 0);
                                cmd.Parameters.AddWithValue("type", "ended");
                                cmd.Parameters.AddWithValue("details", "no_winner");
                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        return;
                    }
                    else if (activePlayers.Count() == 1)
                    {
                        await dice.MoveRotate(client, 8, 10, 0);
                        GamePlayer winner = activePlayers.First();
                        await MakeBotTalk("HOST", "We have a winner!", true);
                        await MakeBotTalk("HOST", winner.LastKnownName + ", congratulations. You won the game!", true);
                        winner.State = 0;

                        RoomUnit winnerUnit = winner.GetUnit(client);

                        int winCoins = 0;

                        IEnumerable<KeyValuePair<int, FloorItem>> balanceItems = winner.GetBalanceStack(client);
                        foreach (KeyValuePair<int, FloorItem> item in balanceItems)
                        {
                            while (IsPaused)
                            {
                                await Task.Delay(50);
                            }

                            if (item.Value.TypeId == FURNITYPE_COIN_1)
                            {
                                winCoins += 1;
                                continue;
                            }
                            else if (item.Value.TypeId == FURNITYPE_COIN_5)
                            {
                                winCoins += 5;
                                continue;
                            }
                            else if (item.Value.TypeId == FURNITYPE_COIN_10)
                            {
                                winCoins += 10;
                                continue;
                            }
                            else if (item.Value.TypeId == FURNITYPE_COIN_20)
                            {
                                winCoins += 20;
                                continue;
                            }
                            else if (item.Value.TypeId == FURNITYPE_COIN_50)
                            {
                                winCoins += 50;
                                continue;
                            }
                        }

                        await MakeBotTalk("HOST", "Prize: " + winCoins + " coins. Added to your Bank balance!", true);

                        if (winnerUnit != null)
                        {
                            using (var db = new AppDb())
                            {
                                await db.TryConnect();

                                using (var cmd = db.Connection.CreateCommand())
                                {
                                    cmd.CommandText = @"UPDATE habbos SET bank_balance = bank_balance + @amount WHERE habbo_id = @habbo_id LIMIT 1";
                                    cmd.Parameters.AddWithValue("amount", winCoins);
                                    cmd.Parameters.AddWithValue("habbo_id", winnerUnit.UserId);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = db.Connection.CreateCommand())
                                {
                                    cmd.CommandText = @"INSERT INTO logs_bank_transactions (habbo_id, amount, description) VALUES (@habbo_id, @amount, @description)";
                                    cmd.Parameters.AddWithValue("habbo_id", winnerUnit.UserId);
                                    cmd.Parameters.AddWithValue("amount", winCoins);
                                    cmd.Parameters.AddWithValue("description", "Prize from game room - Bank Game");
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = db.Connection.CreateCommand())
                                {
                                    cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                    cmd.Parameters.AddWithValue("habbo_id", winnerUnit.UserId);
                                    cmd.Parameters.AddWithValue("type", "win");
                                    cmd.Parameters.AddWithValue("details", winCoins.ToString());
                                    cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                using (var cmd = db.Connection.CreateCommand())
                                {
                                    cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                    cmd.Parameters.AddWithValue("habbo_id", winnerUnit.UserId);
                                    cmd.Parameters.AddWithValue("type", "ended");
                                    cmd.Parameters.AddWithValue("details", winnerUnit.Username);
                                    cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await winner.onDeath(client);

                        UpdateLeaderboard1();
                        UpdateLeaderboard2();
                        UpdateLeaderboard3();
                        AddWinLog(winnerUnit.Username, winCoins);

                        return;
                    }

                    await MakeBotTalk("HOST", round == 1 ? "-- ROUND " + round + ". 3/4 NOT ACTIVE. 50% CHANCE OF BIG GAINS --" : "-- ROUND " + round + " --", true);

                    /*foreach (GamePlayer player in activePlayers)
                    {
                        RoomUnit playerUnit = player.GetUnit(this.client);
                        DateTime compareTo = DateTime.Now.AddMinutes(-5);

                        if (playerUnit != null && playersLastHumanInteraction.TryGetValue(playerUnit.UserId, out DateTime dateTime)) {
                            if(dateTime < compareTo)
                            {
                                await playerUnit.Kick(this.client);
                                await Task.Delay(500);
                            }
                        }
                    }*/

                    foreach (GamePlayer player in activePlayers)
                    {
                        while (IsPaused)
                        {
                            await Task.Delay(50);
                        }

                        activePlayers = Players.Where(x => x.State == 1);
                        await cleanBoard();

                        if (activePlayers.Count() < 2)
                            break;

                        if (player.State != 1)
                            continue;

                        int diceValue = 0;
                        bool diceRolled = false;

                        void onItemUpdated(object sender2, BotFramework.Room.Events.FloorItemUpdatedEventArgs args)
                        {
                            if (args.item == dice)
                            {
                                try
                                {
                                    string stateNow = (string)args.item.Stuff[0];

                                    if (int.TryParse(stateNow, out int state))
                                    {
                                        if (state == -1)
                                        {
                                            diceRolled = true;
                                        }
                                        if (diceRolled && state > 0 && state <= 6)
                                        {
                                            if (args.item == dice && diceValue == 0) { diceValue = state; };
                                        }
                                    }
                                }
                                catch (Exception) { }
                            }

                            if (diceValue != 0)
                            {
                                client.GetRoomHandler().FloorItemUpdatedEvent -= onItemUpdated;
                            }
                        };

                        string stateNow1 = (string)dice.Stuff[0];

                        while (stateNow1 == "-1")
                        {
                            await Task.Delay(500);
                            stateNow1 = (string)dice.Stuff[0];
                        }

                        await dice.MoveRotate(client, player.Pos_Banzai.X, player.Pos_Banzai.Y, 0);
                        client.GetRoomHandler().FloorItemUpdatedEvent += onItemUpdated;

                        DateTime botCheck1 = DateTime.Now.AddMilliseconds(1500);
                        DateTime remindAt1 = DateTime.Now.AddSeconds(10);
                        DateTime remindAt2 = DateTime.Now.AddSeconds(20);
                        DateTime remindAt3 = DateTime.Now.AddSeconds(30);
                        DateTime kickAt = DateTime.Now.AddSeconds(40);

                        RoomUnit playerUnit = player.GetUnit(this.client);

                        while (diceValue == 0)
                        {
                            if (this.client.GetModule().IsDisposed)
                                return;

                            await Task.Delay(50);

                            while (IsPaused)
                            {
                                await Task.Delay(50);
                            }

                            if (!diceRolled && botCheck1 <= DateTime.Now)
                            {
                                if (playerUnit != null)
                                {
                                    playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                                }
                            }

                            if (!diceRolled && remindAt1 <= DateTime.Now)
                            {
                                remindAt1 = DateTime.Now.AddYears(1);
                                await MakeBotTalk("HOST", player.LastKnownName + " please roll the dice.");

                                if (playerUnit != null)
                                {
                                    playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                                }
                            }
                            if (!diceRolled && remindAt2 <= DateTime.Now)
                            {
                                remindAt2 = DateTime.Now.AddYears(1);
                                await MakeBotTalk("HOST", player.LastKnownName + " roll the dice or you will be kicked from the game.");

                                if (playerUnit != null)
                                {
                                    playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                                }
                            }
                            if (!diceRolled && remindAt3 <= DateTime.Now)
                            {
                                remindAt3 = DateTime.Now.AddYears(1);
                                await MakeBotTalk("HOST", player.LastKnownName + " this is your final warning.");

                                if (playerUnit != null)
                                {
                                    playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                                }
                            }
                            if (playerUnit == null || (!diceRolled && kickAt <= DateTime.Now))
                            {
                                await dice.MoveRotate(client, 8, 10, 0);
                                client.GetRoomHandler().FloorItemUpdatedEvent -= onItemUpdated;
                                kickAt = DateTime.Now.AddYears(1);
                                await MakeBotTalk("HOST", player.LastKnownName + " failed to roll the dice.");
                                await player.onDeath(client);
                                player.State = 0;

                                if (playerUnit != null)
                                {
                                    using (var db = new AppDb())
                                    {
                                        await db.TryConnect();

                                        using (var cmd = db.Connection.CreateCommand())
                                        {
                                            cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                            cmd.Parameters.AddWithValue("habbo_id", playerUnit.UserId);
                                            cmd.Parameters.AddWithValue("type", "lost");
                                            cmd.Parameters.AddWithValue("details", "failed_to_roll");
                                            cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }

                                    playersLastHumanInteraction.AddOrUpdate(playerUnit.UserId, DateTime.Now, (x, y) => { return DateTime.Now; });
                                }

                                break;
                            }
                        }

                        client.GetRoomHandler().FloorItemUpdatedEvent -= onItemUpdated;

                        if (diceValue == 0)
                            continue;

                        await Task.Delay(1000);

                        if(diceValue >= 1 && diceValue <= 6)
                        {
                            if (player.LastKnownUnit != null)
                            {
                                using (var db = new AppDb())
                                {
                                    await db.TryConnect();

                                    using (var cmd = db.Connection.CreateCommand())
                                    {
                                        cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                        cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                        cmd.Parameters.AddWithValue("type", "rolled");
                                        cmd.Parameters.AddWithValue("details", diceValue + "");
                                        cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        switch (diceValue)
                        {
                            case 1:
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 1 - Gain coin");
                                await player.onGainCoin(client);
                                break;

                            case 2:
                                int lives = player.GetLifes(client).Count();
                                int coins = player.GetBalanceStack(client).Count();
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 2 - Lose coin");
                                await player.onLoseCoin(client);
                                if (coins == 1 && lives != 0)
                                {
                                    await MakeBotTalk("HOST", player.GetLifes(client).Count() + " lives remaining");
                                }
                                break;

                            case 3:
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 3 - Pick to Kick");

                                if (round == 1)
                                {
                                    await MakeBotTalk("HOST", "#3 not active in Round 1");
                                    break;
                                }

                                Task<GamePlayer> picked = PickRandomPlayer(player);
                                await picked;

                                if (picked.Result == null)
                                {
                                    await MakeBotTalk("HOST", player.LastKnownName + " was kicked due to inactivity.");
                                    await player.onDeath(client);
                                    player.State = 0;

                                    if (player.LastKnownUnit != null)
                                    {
                                        using (var db = new AppDb())
                                        {
                                            await db.TryConnect();

                                            using (var cmd = db.Connection.CreateCommand())
                                            {
                                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                                cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                                cmd.Parameters.AddWithValue("type", "lost");
                                                cmd.Parameters.AddWithValue("details", "failed_to_banzai");
                                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }

                                    break;
                                }

                                await MakeBotTalk("HOST", picked.Result.LastKnownName + " lost a life!");
                                await picked.Result.onLifeLost(client);

                                break;

                            case 4:
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 4 - Lose a life");

                                if (round == 1)
                                {
                                    await MakeBotTalk("HOST", "#4 not active in Round 1");
                                    break;
                                }

                                await player.onLifeLost(client);
                                await MakeBotTalk("HOST", player.GetLifes(client).Count() + " lives remaining");
                                break;

                            case 5:
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 5 - Steal coin");

                                Task<GamePlayer> picked2 = PickRandomPlayer(player);
                                await picked2;

                                if (picked2.Result == null)
                                {
                                    await MakeBotTalk("HOST", player.LastKnownName + " was kicked due to inactivity.");
                                    await player.onDeath(client);
                                    player.State = 0;

                                    if (player.LastKnownUnit != null)
                                    {
                                        using (var db = new AppDb())
                                        {
                                            await db.TryConnect();

                                            using (var cmd = db.Connection.CreateCommand())
                                            {
                                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                                cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                                cmd.Parameters.AddWithValue("type", "lost");
                                                cmd.Parameters.AddWithValue("details", "failed_to_banzai");
                                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }

                                    break;
                                }

                                await MakeBotTalk("HOST", "Coin stolen from " + picked2.Result.LastKnownName);
                                await picked2.Result.MoveCoin(client, player.Pos_Balance);

                                break;

                            case 6:
                                //await MakeBotTalk("HOST", player.LastKnownName + " rolled 6 - Donate coin");

                                Task<GamePlayer> picked3 = PickRandomPlayer(player);
                                await picked3;

                                if (picked3.Result == null)
                                {
                                    await MakeBotTalk("HOST", player.LastKnownName + " was kicked due to inactivity.");
                                    await player.onDeath(client);
                                    player.State = 0;

                                    if (player.LastKnownUnit != null)
                                    {
                                        using (var db = new AppDb())
                                        {
                                            await db.TryConnect();

                                            using (var cmd = db.Connection.CreateCommand())
                                            {
                                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                                cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                                cmd.Parameters.AddWithValue("type", "lost");
                                                cmd.Parameters.AddWithValue("details", "failed_to_banzai");
                                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }

                                    break;
                                }

                                await MakeBotTalk("HOST", "Coin donated to " + picked3.Result.LastKnownName);
                                await player.MoveCoin(client, picked3.Result.Pos_Balance);

                                break;
                        }

                    }

                }
            }
        }

        private async Task<GamePlayer> PickRandomPlayer(GamePlayer player)
        {
            RoomUnit unit = player.GetUnit(client);
            FloorItem dice = client.GetRoomHandler().CurrentRoom.FloorItems.Where(x => x.Value.TypeId == FURNITYPE_DICEMASTER).First().Value;
            FloorItem banzai = player.GetBanzai(client).First().Value;
            FloorItem chair = player.GetChairFurni(client).First().Value;

            if(unit == null || banzai == null)
            {
                return null;
            }

            await dice.MoveRotate(client, 8, 10, 0);
            await Task.Delay(200);

            //set wired
            await Connection.SendToServerAsync(this.client.GetModule().Out.WiredEffectSaveData, new object[] {
                WiredBanzai_Effect, // furni id
                0,
                "",
                1, // items count
                chair.Id,
                2, // delay in 0.5s
                0 // something
            });
            await Task.Delay(600);

            await Connection.SendToServerAsync(this.client.GetModule().Out.WiredTriggerSaveData, new object[] {
                WiredBanzai_Trigger, // furni id
                0,
                "",
                1, // items count
                banzai.Id,
                0 // something
            });
            await Task.Delay(600);

            await banzai.MoveRotate(client, banzai.Tile.X, banzai.Tile.Y, 0);
            BotFramework.Room.Items.Point tileNow = unit.Tile;

            DateTime remindAt1 = DateTime.Now.AddSeconds(10);
            DateTime remindAt2 = DateTime.Now.AddSeconds(20);
            DateTime remindAt3 = DateTime.Now.AddSeconds(30);
            DateTime kickAt = DateTime.Now.AddSeconds(40);

            while (unit.Tile == tileNow || unit.Tile == banzai.Tile)
            {
                await Task.Delay(50);

                if (unit.Tile == tileNow && remindAt1 <= DateTime.Now)
                {
                    remindAt1 = DateTime.Now.AddYears(1);
                    await MakeBotTalk("HOST", player.LastKnownName + " please step on the banzai teleport.");
                }
                if (unit.Tile == tileNow && remindAt2 <= DateTime.Now)
                {
                    remindAt2 = DateTime.Now.AddYears(1);
                    await MakeBotTalk("HOST", player.LastKnownName + " step on the banzai teleport or you will be kicked from the game.");
                }
                if (unit.Tile == tileNow && remindAt3 <= DateTime.Now)
                {
                    remindAt3 = DateTime.Now.AddYears(1);
                    await MakeBotTalk("HOST", player.LastKnownName + " this is your final warning.");
                }
                if (unit.Tile == tileNow && kickAt <= DateTime.Now)
                {
                    kickAt = DateTime.Now.AddYears(1);
                    await MakeBotTalk("HOST", player.LastKnownName + " did not step on the banzai.");
                    await player.onDeath(client);
                    player.State = 0;
                    break;
                }
            }

            foreach (GamePlayer p in Players)
            {
                if (unit.Tile.X == p.Pos_Banzai.X && unit.Tile.Y == p.Pos_Banzai.Y)
                {
                    FloorItem stack = client.GetRoomHandler().CurrentRoom.FloorItems.Where(x => x.Value.TypeId == FURNITYPE_STACK1X1).First().Value;
                    await stack.MoveRotate(client, player.Pos_Banzai.X, player.Pos_Banzai.Y, 0);
                    await Task.Delay(250);
                    await banzai.MoveRotate(client, banzai.Tile.X, banzai.Tile.Y, 0);
                    await Task.Delay(600);
                    await stack.MoveRotate(client, 2, 1, 0);
                    await Task.Delay(2000);
                    return p;
                }
            }

            return null;
        }

        private async Task cleanBoard()
        {
            foreach (GamePlayer player in Players)
            {
                if (player.State != 0 && player.GetUnit(client) == null)
                {
                    player.State = 0;
                    await MakeBotTalk("HOST", player.LastKnownName + " left the game.", true);
                    await player.onDeath(client, true);

                    if (player.LastKnownUnit != null)
                    {
                        using (var db = new AppDb())
                        {
                            await db.TryConnect();

                            using (var cmd = db.Connection.CreateCommand())
                            {
                                cmd.CommandText = @"INSERT INTO logs_games_bankgame_activity (habbo_id, type, details, game_id) VALUES (@habbo_id, @type, @details, @game_id)";
                                cmd.Parameters.AddWithValue("habbo_id", player.LastKnownUnit.UserId);
                                cmd.Parameters.AddWithValue("type", "lost");
                                cmd.Parameters.AddWithValue("details", "left_game");
                                cmd.Parameters.AddWithValue("game_id", CurrentGameId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
        }

        private async Task onPlayerJoined(GamePlayer player)
        {
            RoomUnit unit = player.GetUnit(client);

            if (unit != null)
            {
                using (var db = new AppDb())
                {
                    await db.TryConnect();
                    using (var cmd = db.Connection.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO habbos (habbo_id, username, motto, look) VALUES (@habbo_id, @username, @motto, @look) ON DUPLICATE KEY UPDATE username = VALUES(username), motto = VALUES(motto), look = VALUES(look)";
                        cmd.Parameters.AddWithValue("habbo_id", unit.UserId);
                        cmd.Parameters.AddWithValue("username", unit.Username);
                        cmd.Parameters.AddWithValue("motto", unit.Motto);
                        cmd.Parameters.AddWithValue("look", unit.Look);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            await player.onDeath(client, true);
            IEnumerable coinsToPlace;
            IEnumerable<KeyValuePair<int, Sulakore.Habbo.HItem>> livesToPlace = null;

            bool isVip = VIPs.Contains(unit.Username);
            if (isVip)
            {
                coinsToPlace = client.GetInventoryHandler().Items.Where(x => x.Value.TypeId == FURNITYPE_COIN_1).Take(2).Append(
                    client.GetInventoryHandler().Items.Where(x => x.Value.TypeId == FURNITYPE_COIN_10).Take(1).First()
                );
                livesToPlace = client.GetInventoryHandler().Items.Where(x => x.Value.TypeId == FURNITYPE_GAMELIFE).Take(3);
            }
            else
            {
                coinsToPlace = client.GetInventoryHandler().Items.Where(x => x.Value.TypeId == FURNITYPE_COIN_1).Take(2);
            }

            if (this.client.GetRoomHandler().CurrentRoom.FloorItems.Where(x => x.Value.Tile.Is(player.Pos_Balance) && x.Value.TypeId == FURNITYPE_COIN_1).Count() == 0)
            {
                foreach (KeyValuePair<int, Sulakore.Habbo.HItem> item in coinsToPlace)
                {
                    await Connection.SendToServerAsync(this.client.GetModule().Out.RoomPlaceItem, new object[] { -item.Value.Id + " " + player.Pos_Balance.X + " " + player.Pos_Balance.Y + " " + (int)0 });
                    await Task.Delay(750);
                }

                if (livesToPlace != null)
                {
                    foreach (KeyValuePair<int, Sulakore.Habbo.HItem> item in livesToPlace)
                    {
                        await Connection.SendToServerAsync(this.client.GetModule().Out.RoomPlaceItem, new object[] { -item.Value.Id + " " + player.Pos_Banzai.X + " " + player.Pos_Banzai.Y + " " + (int)0 });
                        await Task.Delay(750);
                    }
                }

                Room room = client.GetRoomHandler().CurrentRoom;
                FloorItem stack = room.FloorItems.Where(x => x.Value.TypeId == FURNITYPE_STACK1X1).First().Value;
                await stack.MoveRotate(client, player.Pos_Banzai.X, player.Pos_Banzai.Y, 0);
                await Task.Delay(250);
                await Connection.SendToServerAsync(this.client.GetModule().Out.RoomPlaceItem, new object[] { -client.GetInventoryHandler().Items.Where(x => x.Value.TypeId == FURNITYPE_BANZAI_TELE).First().Value.Id + " " + player.Pos_Banzai.X + " " + player.Pos_Banzai.Y + " " + (int)0 });
                await Task.Delay(600);
                await stack.MoveRotate(client, 2, 1, 0);
            }
        }


    }
}
