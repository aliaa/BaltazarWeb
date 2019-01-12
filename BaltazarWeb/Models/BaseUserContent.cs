using AliaaCommon;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public abstract class BaseUserContent : MongoEntity
    {
        public enum PublishStatus
        {
            WaitForAccept,
            Published
        }

        public ObjectId UserId { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public string Text { get; set; }
        public string Image { get; set; }
        public PublishStatus Status { get; set; }
    }
}
