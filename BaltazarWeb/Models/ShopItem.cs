using AliaaCommon;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class ShopItem : MongoEntity
    {
        [Display(Name = "فعال است")]
        public bool Enabled { get; set; } = true;

        [Display(Name = "تعداد سکه مورد نیاز")]
        public int CoinCost { get; set; }

        [Display(Name = "نام")]
        public string Name { get; set; }

        [Display(Name = "تاریخ افزودن")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public bool HasImage { get; set; }

        [BsonIgnore][JsonIgnore]
        [Display(Name = "تصویر")]
        public IFormFile ImageFile { get; set; }
    }
}
