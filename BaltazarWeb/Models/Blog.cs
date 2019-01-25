using AliaaCommon;
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
    [MongoIndex(new string[] { nameof(DateAdded)}, new MongoIndexType[] { MongoIndexType.Descending })]
    public class Blog : MongoEntity
    {
        [Display(Name = "تاریخ")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        [Display(Name = "موضوع")]
        public string Title { get; set; }

        [Display(Name = "محتوا")]
        public string HtmlContent { get; set; }
        
        public bool HasImage { get; set; }

        [BsonIgnore][JsonIgnore]
        [Display(Name = "تصویر")]
        public IFormFile ImageFile { get; set; }

        [JsonIgnore]
        [Display(Name = "نمایش در اپ")]
        public bool ShowOnApp { get; set; } = true;

        [JsonIgnore]
        [Display(Name = "نمایش در سایت")]
        public bool ShowOnWeb { get; set; } = true;
    }
}
