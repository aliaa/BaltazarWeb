using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public abstract class BaseUserContent : MongoEntity
    {
        public enum PublishStatus
        {
            [Display(Name = "منتظر تائید")]
            WaitForAccept,

            [Display(Name = "منتشر شده")]
            Published
        }

        [Display(Name = "کاربر")]
        public ObjectId UserId { get; set; }

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [Display(Name = "متن")]
        public string Text { get; set; }

        [Display(Name = "تصویر")]
        public string Image { get; set; }

        [Display(Name = "وضعیت")]
        public PublishStatus Status { get; set; }
    }
}
