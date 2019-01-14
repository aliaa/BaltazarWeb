using AliaaCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class StudyField : MongoEntity
    {
        [Display(Name = "نام")]
        public string Name { get; set; }
    }
}
