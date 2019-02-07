using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models
{
    public enum Permission
    {
        [Display(Name = "مشاهده محتوای دانش آموزان")]
        ViewContent,
        [Display(Name = "تائید محتوای دانش آموزان")]
        ApproveContent,
        [Display(Name = "مدیریت سوالهای لیگ")]
        ManageLeagueQuestions,
        [Display(Name = "مدیریت بلاگ")]
        ManageBlogs,
        [Display(Name = "تعریف شهرها و استانها")]
        ManageProvincesAndCities,
        [Display(Name = "تعریف دروس")]
        ManageCourses,
        [Display(Name = "مدیریت فروشگاه")]
        ManageShops,
        [Display(Name = "مدیریت دانش آموزان")]
        ManageStudents,
        [Display(Name = "ارسال اعلان")]
        SendPush,
    }
}
