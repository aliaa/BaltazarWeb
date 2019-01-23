using AliaaCommon;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class ShopOrder : MongoEntity
    {
        public enum OrderStatus
        {
            WaitForApprove,
            Approved,
            Delivered
        }

        public ObjectId ShopItemId { get; set; }

        [BsonIgnore]
        public string ShopItemName { get; set; }

        public ObjectId UserId { get; set; }
        public int CoinCost { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; }
    }
}
