using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class City : MongoEntity
    {
        [Display(Name = "استان")]
        public ObjectId ProvinceId { get; set; }

        [Display(Name= "نام")]
        public string Name { get; set; }
    }
}
