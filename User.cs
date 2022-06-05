using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    [BsonIgnoreExtraElements]
    public class User
    {
        public string nickname { get; set; } = "";
        [JsonIgnore]
        public string id
        {
            get
            {
                return idLong.ToString();
            }
            set
            {
                idLong = Convert.ToInt64(value);
            }
        }
        [JsonIgnore]
        public long idLong { get; set; } = DateTime.UtcNow.Ticks;

        [BsonIgnore]
        public int points { get; set; } = 0;
    }
}
