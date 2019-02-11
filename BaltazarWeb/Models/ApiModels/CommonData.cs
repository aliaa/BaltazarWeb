using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ApiModels
{
    public class CommonData
    {
        public class UpgradeData
        {
            public string Message { get; set; }
            public bool ForceUpgrade { get; set; } = false;
        }

        public class Notifications
        {
            public int NewBlogs { get; set; }
            public int NewShops { get; set; }
            public int NewAnswers { get; set; }
        }

        public UpgradeData Upgrade { get; set; }
        public Student Me { get; set; }
        public Notifications Notification { get; set; }
        public List<Province> Provinces { get; set; }
        public List<City> Cities { get; set; }
        public List<Course> Courses { get; set; }
        public List<CourseSection> Sections { get; set; }
        public List<StudyField> StudyFields { get; set; }
    }
}
