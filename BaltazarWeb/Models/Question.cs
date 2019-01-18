using AliaaCommon;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaltazarWeb.Models
{
    [MongoIndex(new string[] { nameof(Grade) })]
    public class Question : BaseUserContent
    {
        [Display(Name = "مقطع تحصیلی")]
        [Range(minimum: 1, maximum: 12)]
        public int Grade { get; set; }

        [Display(Name = "نام درس")]
        public ObjectId CourseId { get; set; }

        [Display(Name = "نام سرفصل")]
        public ObjectId SectionId { get; set; }

        public ObjectId AcceptedAnswerId { get; set; }

        [BsonIgnore]
        public List<Answer> Answers { get; set; }
    }
}
