using AliaaCommon;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class UploadedFile : MongoEntity
    {
        [Display(Name = "تاریخ آپلود")]
        public DateTime Date { get; set; } = new DateTime();

        [Display(Name = "نام فایل")]
        public string FileName { get; set; }

        [BsonIgnore]
        public IFormFile File { get; set; }
    }
}
