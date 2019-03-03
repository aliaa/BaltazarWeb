using AliaaCommon;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    [MongoIndex(new string[] { nameof(Date) }, new MongoIndexType[] { MongoIndexType.Descending })]
    public class ContactUsMessage : MongoEntity
    {
        [JsonIgnore]
        [Display(Name = "تاریخ")]
        public DateTime Date { get; set; }

        [JsonIgnore]
        [Display(Name = "از گوشی")]
        public bool FromAndroid { get; set; }

        [JsonIgnore]
        [Display(Name = "دانش آموز")]
        public ObjectId StudentId { get; set; }

        [Display(Name = "نام")]
        public string Name { get; set; }

        [Display(Name = "ایمیل")]
        public string Email { get; set; }

        [Display(Name = "پیام")]
        public string Message { get; set; }
    }
}
