using AliaaCommon;
using MongoDB.Bson;
using System;
using System.ComponentModel.DataAnnotations;

namespace BaltazarWeb.Models
{
    public class Question : BaseUserContent
    {
        [Display(Name = "نام درس")]
        public ObjectId CourseId { get; set; }

        [Display(Name = "نام سرفصل")]
        public ObjectId SectionId { get; set; }
    }
}
