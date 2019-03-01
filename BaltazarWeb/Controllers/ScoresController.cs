using System;
using System.Linq;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using BaltazarWeb.Models.ViewModels;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class ScoresController : Controller
    {
        private readonly MongoHelper DB;
        private readonly ScoresDataProvider scoresDataProvider;

        public ScoresController(MongoHelper DB, ScoresDataProvider scoresDataProvider)
        {
            this.DB = DB;
            this.scoresDataProvider = scoresDataProvider;
        }

        public IActionResult Index()
        {
            ScoresViewModel model = new ScoresViewModel();
            model.CurrentFestivalName = ScoresData.CurrentFestivalDisplayName;
            model.TotalTopStudents = scoresDataProvider.TotalTopStudents;
            model.FestivalTopStudents = scoresDataProvider.FestivalTopStudents;
            for (int i = 0; i < 12; i++)
                model.FestivalTopInGrades[i] = scoresDataProvider.GetFestivalTopStudentsInGrade(i + 1);

            return View(model);
        }

        public ActionResult<DataResponse<ScoresData>> Mine([FromHeader] Guid token)
        {
            //PersianDate pDate = PersianDateConverter.ToPersianDate(DateTime.Now);
            //int currentMonth = pDate.Month;
            //int currentSeason0Based = (currentMonth - 1) / 3;
            //DateTime festivalStart = PersianDateConverter.ToGregorianDateTime(new PersianDate(pDate.Year, month: currentSeason0Based * 3 + 1, day: 1));

            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();

            return new DataResponse<ScoresData>
            {
                Success = true,
                Data = scoresDataProvider.GetScoresData(me)
            };
        }
    }
}