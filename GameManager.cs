using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using ComputerUtils.Webserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class GameManager
    {
        public static Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        public static Dictionary<string, Client> clients = new Dictionary<string, Client>();

        public static void HandleWSConnection(SocketServerRequest request, User u, List<string> args)
        {
            if (u == null) return;
            clients[u.nickname] = new Client
            {
                request = request,
                userName = u.nickname,
                received = DateTime.Now
            };
            if (args.Count < 2) return;
            switch(args[0])
            {
                case "join":
                    JoinRoom(u, args[1]);
                    break;
                case "turn":
                    if (args.Count < 3) return;
                    TurnCard(args[2], args[1]);
                    break;
                case "select":
                    if (args.Count < 3) return;
                    SelectCard(u, args[2].Split(';').ToList(), args[1]);
                    break;
                case "next":
                    NextRound(args[1]);
                    break;
                case "vote":
                    if (args.Count < 3) return;
                    Vote(args[1], args[2]);
                    break;
                case "heartbeat":
                    Heartbeat(args[1]);
                    break;
                case "kick":
                    if (args.Count < 3) return;
                    Kick(args[1], args[2], u);
                    break;
            }
        }

        public static void Kick(string id, string user, User u)
        {
            if (!rooms.ContainsKey(id)) return;
            for(int i = 0; i < rooms[id].users.Count; i++)
            {
                if(rooms[id].users[i].nickname == user)
                {
                    // kick this idiot. NOW. but first do checks
                    if(!rooms[id].users[i].kickVotes.Where(x => x.nickname == u.nickname).Any())
                    {
                        // user didn't want to kick them before
                        rooms[id].users[i].kickVotes.Add(u);
                        if(rooms[id].users.Count - 1 <= rooms[id].users[i].kickVotes.Count)
                        {
                            // KICK THEM. FAST
                            clients[rooms[id].users[i].nickname].request.SendString("kicked due to votekick");
                            clients.Remove(rooms[id].users[i].nickname);
                            rooms[id].users.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            SendUpdatedRoomToAllUsers(id);
        }

        public static void Heartbeat(string id)
        {
            if (!rooms.ContainsKey(id)) return;
            DateTime now = DateTime.Now;
            int before = rooms[id].users.Count;
            for (int i = 0; i < rooms[id].users.Count; i++)
            {
                if(!clients.ContainsKey(rooms[id].users[i].nickname) || clients[rooms[id].users[i].nickname].received + new TimeSpan(0, 0, 10) < now)
                {
                    clients.Remove(rooms[id].users[i].nickname);
                    rooms[id].users.RemoveAt(i);
                    i--;
                }

                // kicking
                if (rooms[id].roundStart != DateTime.MinValue && rooms[id].selections.Count < rooms[id].users.Count - 1 && rooms[id].currentAsker != rooms[id].users[i].nickname)
                {
                    // normal player
                    if (rooms[id].roundStart + new TimeSpan(0, 0, rooms[id].selectTime) < now)
                    {
                        // kick that idiot
                        clients[rooms[id].users[i].nickname].request.SendString("kicked due to inactivity");
                        clients.Remove(rooms[id].users[i].nickname);
                        rooms[id].users.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (rooms[id].users.FirstOrDefault(x => x.nickname == rooms[id].currentAsker) == null) NextRound(id);
            if(before != rooms[id].users.Count) SendUpdatedRoomToAllUsers(id);
            if(rooms[id].users.Count <= 0) rooms.Remove(id);
        }

        public static void Vote(string id, string nickname)
        {
            if (!rooms.ContainsKey(id)) return;
            for (int i = 0; i < rooms[id].users.Count; i++)
            {
                if (rooms[id].users[i].nickname == nickname) rooms[id].users[i].points++;
            }
            SendUpdatedRoomToAllUsers(id);
        }

        public static void SelectCard(User u, List<string> contents, string id)
        {
            if (!rooms.ContainsKey(id)) return;
            List<Card> cards = contents.ConvertAll<Card>(x => new Card
            {
                content = x
            });
            rooms[id].selections.Add(new CardSelection
            {
                username = u.nickname,
                cards = cards
            });
            SendUpdatedRoomToAllUsers(id);
        }

        public static Room CreateRoom(string set)
        {
            string id = RandomExtension.CreateRandom(6, "0123456789");
            Room room = new Room();
            room.id = id;
            room.currentSet = set;
            rooms.Add(id, room);
            // User joings via webhook. This ain't needed
            //room.users.Add(creator);
            return room;
        }

        public static Room JoinRoom(User join, string id)
        {
            if (!rooms.ContainsKey(id)) return new Room();
            if (rooms[id].users.FirstOrDefault(x => x.nickname == join.nickname) != null) rooms[id].users.Remove(rooms[id].users.FirstOrDefault(x => x.nickname == join.nickname));
            rooms[id].users.Add(join);
            SendUpdatedRoomToAllUsers(id);
            return rooms[id];
        }


        public static void TurnCard(string content, string id)
        {
            if (!rooms.ContainsKey(id)) return;
            for (int i = 0; i < rooms[id].selections.Count; i++)
            {
                for(int ii = 0; ii < rooms[id].selections[i].cards.Count; ii++)
                {
                    if(rooms[id].selections[i].cards[ii].content == content)
                    {
                        rooms[id].selections[i].cards[ii].visible = true;
                        break;
                    }
                }
            }
            SendUpdatedRoomToAllUsers(id);
        }

        public static void SendUpdatedRoomToAllUsers(string id)
        {
            if (!rooms.ContainsKey(id)) return;
            foreach (User u in rooms[id].users)
            {
                if(clients.ContainsKey(u.nickname))
                {
                    clients[u.nickname].request.SendString(JsonSerializer.Serialize(rooms[id]));
                }
            }
        }

        public static int GetAmountOfRequiredAnswerd(string content)
        {
            int length = 0;
            for(int i = 0; i < content.Length; i++)
            {
                if(content[i] == '_' && (i + 1 >= content.Length || content[i + 1] != '_'))
                {
                    length++;
                }
            }
            return length <= 0 ? 1 : length;
        }

        public static void NextRound(string id)
        {
            if (!rooms.ContainsKey(id)) return;
            CardSet set = MongoDBInteractor.GetCardSet(rooms[id].currentSet);

            // randomly choose a question
            if (rooms[id].notAskedQuestions.Count <= 0) rooms[id].notAskedQuestions = new List<Card>(set.black);
            if (rooms[id].notAskedQuestions.Count > 0)
            {
                int question = RandomExtension.random.Next(0, rooms[id].notAskedQuestions.Count);
                rooms[id].currentQuestion = rooms[id].notAskedQuestions[question];
                rooms[id].notAskedQuestions.RemoveAt(question);
            }
            else rooms[id].currentQuestion = new Card { content = "Heck add some cards to this set, idiot." };
            rooms[id].currentQuestion.selectionCount = GetAmountOfRequiredAnswerd(rooms[id].currentQuestion.content);

            rooms[id].roundStart = DateTime.Now;

            for(int i = 0; i < rooms[id].users.Count; i++)
            {
                rooms[id].users[i].kickVotes = new List<User>();
            }

            // reset newCards
            rooms[id].newCards = new List<CardSelection>();

            // reset selections
            rooms[id].selections = new List<CardSelection>();

            // select random asker
            rooms[id].currentAsker = rooms[id].users[RandomExtension.random.Next(0, rooms[id].users.Count)].nickname;
            if(set.white.Count > 0)
            {
                // give every user new cards to draw
                foreach (User u in rooms[id].users)
                {
                    CardSelection s = new CardSelection
                    {
                        username = u.nickname
                    };
                    for (int i = 0; i < 20; i++)
                    {
                        Card c = set.white[RandomExtension.random.Next(0, set.white.Count)];
                        if (set.white.Count > 20 && s.cards.FirstOrDefault(x => x.content == c.content) != null)
                        {
                            i--;
                            continue;
                        }
                        s.cards.Add(c);
                    }
                    rooms[id].newCards.Add(s);
                }
            }
            SendUpdatedRoomToAllUsers(id);
        }
    }
}
