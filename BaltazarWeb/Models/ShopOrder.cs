using AliaaCommon;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class ShopOrder : MongoEntity
    {
        public enum OrderStatus
        {
            [Display(Name = "منتظر تائید")]
            WaitForApprove,
            [Display(Name = "تائید شده")]
            Approved,
            [Display(Name = "به مقصد رسیده")]
            Delivered
        }

        [Display(Name = "محصول")]
        public ObjectId ShopItemId { get; set; }

        [BsonIgnore]
        public string ShopItemName { get; set; }

        [Display(Name = "خریدار")]
        public ObjectId UserId { get; set; }
        [Display(Name = "هزینه سکه ای")]
        public int CoinCost { get; set; }
        [Display(Name = "تاریخ سفارش")]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        [Display(Name = "وضعیت")]
        public OrderStatus Status { get; set; }
    }
}
