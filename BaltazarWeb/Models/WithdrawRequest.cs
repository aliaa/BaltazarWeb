using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class WithdrawRequest : MongoEntity
    {
        [Display(Name = "تاریخ")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "آموزگار")]
        public ObjectId TeacherId { get; set; }

        [Display(Name = "شماره کارت")]
        public string CardNumber { get; set; }
    }
}
