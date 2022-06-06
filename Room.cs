using ComputerUtils.Webserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class Room
    {
        public string id { get; set; } = "";
        public List<User> users { get; set; } = new List<User>();
        public string currentAsker { get; set; } = "";
        public string currentSet { get; set; } = "";
        public Card currentQuestion { get; set; } = new Card();
        [JsonIgnore]
        public List<Card> notAskedQuestions { get; set; } = new List<Card>();
        public List<CardSelection> newCards { get; set; } = new List<CardSelection>();
        public List<CardSelection> selections { get; set; } = new List<CardSelection>();
    }

    public class CreateRoomResponse
    {
        public string id { get; set; } = "";
        public bool success { get; set; } = false;
        public string msg { get; set; } = "";
    }

    public class CardSelection
    {
        public string username { get; set; } = "";
        public List<Card> cards { get; set; } = new List<Card>();
    }

    public class Client
    {
        [JsonIgnore]
        public SocketServerRequest request { get; set; } = null;
        public DateTime received { get; set; } = DateTime.UtcNow;
        public string userName { get; set; } = "";
    }
}
