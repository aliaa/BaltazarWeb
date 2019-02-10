using AliaaCommon;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    [MongoIndex(new string[] { nameof(UserId) })]
    [MongoIndex(new string[] { nameof(PublishStatus) })]
    [BsonKnownTypes(typeof(Question), typeof(Answer))]
    public abstract class BaseUserContent : MongoEntity
    {
        public enum PublishStatusEnum
        {
            [Display(Name = "منتظر تائید")]
            WaitForApprove,

            [Display(Name = "منتشر شده")]
            Published,

            [Display(Name = "رد شده")]
            Rejected,
        }

        [Display(Name = "کاربر")]
        public ObjectId UserId { get; set; }

        [BsonIgnore]
        [Required]
        public string UserName { get; set; }

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [Display(Name = "متن")]
        public string Text { get; set; }
        
        [Display(Name = "وضعیت انتشار")]
        public PublishStatusEnum PublishStatus { get; set; }

        public bool HasImage { get; set; }
        public bool HasVideo { get; set; }
        public bool HasVoice { get; set; }
    }
}
