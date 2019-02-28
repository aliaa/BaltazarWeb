using BaltazarWeb.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ViewModels
{
    public class ScoresViewModel
    {
        public string CurrentFestivalName { get; set; }
        public List<TopStudent> TotalTopStudents { get; set; }
        public List<TopStudent> FestivalTopStudents { get; set; }
        public List<TopStudent>[] FestivalTopInGrades { get; set; } = new List<TopStudent>[12];
    }
}
