﻿using AliaaCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public class Province : MongoEntity
    {
        [Display(Name = "نام")]
        [Required]
        public string Name { get; set; }
    }
}
