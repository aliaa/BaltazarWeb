using Microsoft.AspNetCore.Http;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class BaltazarQuestion : Question
    {
        public BaltazarQuestion()
        {
            FromBaltazar = true;
            UserName = "بالتازار";
            PublishStatus = PublishStatusEnum.Published;
        }

        [JsonIgnore]
        [Display(Name = "حداکثر مقطع")]
        public int MaxGrade { get; set; }

        [JsonIgnore]
        [Display(Name = "تاریخ انقضا")]
        public DateTime ExpireDate { get; set; } = DateTime.Now.AddDays(2);

        [Display(Name = "اجازه آپلود مدیا")]
        public bool AllowUploadOnAnswer { get; set; }

        [JsonIgnore][BsonIgnore]
        [Display(Name = "تصویر")]
        public IFormFile ImageFile { get; set; }
    }
}
