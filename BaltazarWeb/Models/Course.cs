using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class Course : MongoEntity
    {
        [Display(Name = "مقطع تحصیلی")]
        [Range(minimum:1, maximum: 12)]
        public int Grade { get; set; }

        [Display(Name = "رشته")]
        public ObjectId StudyFieldId { get; set; }

        [Display(Name = "نام")]
        [Required]
        public string Name { get; set; }
    }
}
