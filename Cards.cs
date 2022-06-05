using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    [BsonIgnoreExtraElements]
    public class Card
    {
        public string content { get; set; } = "";
        [BsonIgnore]
        public int selectionCount { get; set; } = 0;
        [BsonIgnore]
        public bool visible { get; set; } = false;
    }

    [BsonIgnoreExtraElements]
    public class CardSetMeta
    {
        public List<User> editors { get; set; } = new List<User>();
        public string name { get; set; } = "";
        public string description { get; set; } = "";
    }

    [BsonIgnoreExtraElements]
    public class CardSet : CardSetMeta
    {
        public List<Card> black { get; set; } = new List<Card>();
        public List<Card> white { get; set; } = new List<Card>();
    }
}
