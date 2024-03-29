﻿using AliaaCommon;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaltazarWeb.Models
{
    [MongoIndex(new string[] { nameof(Grade) })]
    [MongoIndex(new string[] { nameof(AcceptedAnswer) })]
    public class Question : BaseUserContent
    {
        [Display(Name = "مقطع تحصیلی")]
        [Range(minimum: 1, maximum: 12)]
        public int Grade { get; set; }

        [Display(Name = "نام درس")]
        public ObjectId CourseId { get; set; }

        [Display(Name = "نام سرفصل")]
        public ObjectId SectionId { get; set; }

        [JsonIgnore]
        public ObjectId AcceptedAnswerId { get; set; }

        [BsonIgnore]
        public Answer AcceptedAnswer { get; set; }

        [BsonIgnore]
        public List<Answer> Answers { get; set; }

        [Display(Name = "جایزه")]
        public int Prize { get; set; }

        [Display(Name = "سوال داغ")]
        [BsonIgnore]
        public bool Hot { get; set; }

        [Display(Name = "سوال بالتازار")]
        [BsonIgnore]
        public bool FromBaltazar { get; protected set; } = false;

        [BsonIgnore]
        public bool IAlreadyAnswered { get; set; } = false;

        public int UnseenAnswersCount { get; set; } = 0;
    }
}
