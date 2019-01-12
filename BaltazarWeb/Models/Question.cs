using AliaaCommon;
using MongoDB.Bson;
using System;

namespace BaltazarWeb.Models
{
    public class Question : BaseUserContent
    {
        public ObjectId CourseId { get; set; }
        public ObjectId SectionId { get; set; }
    }
}
