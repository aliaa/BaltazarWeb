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
                Data = GetScoresData(me)
            };
        }

        private ScoresData GetScoresData(Student student)
        {
            var currentFestival = ScoresData.CurrentFestivalName;
            ScoresData data = new ScoresData();

            data.MyAllTimePoints = student.TotalPoints;
            data.MyAllTimeTotalScore = DB.Count<Student>(s => s.TotalPoints > student.TotalPoints) + 1;

            data.FestivalName = ScoresData.CurrentFestivalDisplayName;
            var myFestival = student.FestivalPoints.FirstOrDefault(f => f.Name == currentFestival);
            data.MyFestivalPoints = myFestival != null ? myFestival.Points : 0;
            data.MyFestivalPointsFromLeague = myFestival != null ? myFestival.PointsFromLeague : 0;
            data.MyFestivalPointsFromOtherQuestions = myFestival != null ? myFestival.PointsFromOtherQuestions : 0;

            data.MyFestivalScore = DB.Count(Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)) + 1;
            data.MyFestivalScoreOnGrade = DB.Count(
                Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(s => s.Grade, student.Grade),
                    Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)
                )) + 1;

            data.FestivalTop = scoresDataProvider.FestivalTopStudents;
            data.TotalTop = scoresDataProvider.TotalTopStudents;
            data.FestivalTopOnGrade = scoresDataProvider.GetFestivalTopStudentsInGrade(student.Grade);
            return data;
        }
    }
}