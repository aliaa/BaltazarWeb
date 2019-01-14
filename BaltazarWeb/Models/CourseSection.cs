using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class CourseSection : MongoEntity
    {
        [Display(Name = "درس")]
        public ObjectId CourseId { get; set; }

        [Display(Name = "نام سرفصل")]
        public string Name { get; set; }
    }
}
