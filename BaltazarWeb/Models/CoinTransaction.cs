using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class CoinTransaction
    {
        public int Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        [JsonIgnore]
        public ObjectId QuestionId { get; set; }

        [JsonIgnore]
        public ObjectId ShopItemId { get; set; }

        [BsonIgnore]
        public Question Question { get; set; }

        [BsonIgnore]
        public ShopItem ShopItem { get; set; }
    }
}
