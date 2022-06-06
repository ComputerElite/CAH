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
            }
        }

        public static void Heartbeat(string id)
        {
            DateTime now = DateTime.Now;
            for(int i = 0; i < rooms[id].users.Count; i++)
            {
                if(clients[rooms[id].users[i].nickname].received + new TimeSpan(0, 0, 10) < now)
                {
                    clients.Remove(rooms[id].users[i].nickname);
                    rooms[id].users.RemoveAt(i);
                    i--;
                }
            }
            if (rooms[id].users.FirstOrDefault(x => x.nickname == rooms[id].currentAsker) == null) NextRound(id);
            SendUpdatedRoomToAllUsers(id);
            if(rooms[id].users.Count <= 0) rooms.Remove(id);
        }

        public static void Vote(string id, string nickname)
        {
            for(int i = 0; i < rooms[id].users.Count; i++)
            {
                if (rooms[id].users[i].nickname == nickname) rooms[id].users[i].points++;
            }
            SendUpdatedRoomToAllUsers(id);
        }

        public static void SelectCard(User u, List<string> contents, string id)
        {
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
            for(int i = 0; i < rooms[id].selections.Count; i++)
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
            foreach(User u in rooms[id].users)
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
            foreach (string s in content.Replace("\\n", " ").Split(new char[] { ' ', '#' }))
            {
                if (s.Contains("_") && s.Trim(new char[] { '_', ':' }) == "") length++;
            }
            return length <= 0 ? 1 : length;
        }

        public static void NextRound(string id)
        {
            if (!rooms.ContainsKey(id)) return;
            CardSet set = MongoDBInteractor.GetCardSet(rooms[id].currentSet);

            // randomly choose a question
            if (rooms[id].notAskedQuestions.Count <= 0) rooms[id].notAskedQuestions = new List<Card>(set.black);
            rooms[id].currentQuestion = rooms[id].notAskedQuestions[RandomExtension.random.Next(0, rooms[id].notAskedQuestions.Count)];
            rooms[id].currentQuestion.selectionCount = GetAmountOfRequiredAnswerd(rooms[id].currentQuestion.content);

            // reset newCards
            rooms[id].newCards = new List<CardSelection>();

            // reset selections
            rooms[id].selections = new List<CardSelection>();

            // select random asker
            rooms[id].currentAsker = rooms[id].users[RandomExtension.random.Next(0, rooms[id].users.Count)].nickname;

            // give every user new cards to draw
            foreach (User u in rooms[id].users)
            {
                CardSelection s = new CardSelection
                {
                    username = u.nickname
                };
                for(int i = 0; i < 20; i++)
                {
                    Card c = set.white[RandomExtension.random.Next(0, set.white.Count)];
                    if(set.white.Count > 20 && s.cards.FirstOrDefault(x => x.content == c.content) != null)
                    {
                        i--;
                        continue;
                    }
                    s.cards.Add(c);
                }
                rooms[id].newCards.Add(s);
            }
            SendUpdatedRoomToAllUsers(id);
        }
    }
}
