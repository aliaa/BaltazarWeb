using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ViewModels
{
    public class SendPushViewModel
    {
        [Display(Name = "موضوع")]
        public string Title { get; set; }

        [Display(Name = "پیام")]
        public string Content { get; set; }

        [Display(Name = "آیدی پوش کاربر مشخص")]
        public string SpecificUserPusheId { get; set; }
    }
}
