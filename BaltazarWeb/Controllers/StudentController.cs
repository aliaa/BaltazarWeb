using System;
using System.Collections.Generic;
using System.Linq;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using BaltazarWeb.Utils;
using FarsiLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class StudentController : Controller
    {
        private readonly MongoHelper DB;
        private readonly ScoresDataProvider scoresDataProvider;

        public StudentController(MongoHelper DB, ScoresDataProvider scoresDataProvider)
        {
            this.DB = DB;
            this.scoresDataProvider = scoresDataProvider;
        }

        [Authorize]
        public IActionResult Index(string city = null, int page = 0)
        {
            ObjectId cityId;
            ObjectId.TryParse(city, out cityId);
            var list = DB.Find<Student>(s => s.CityId == cityId).Limit(1000).Skip(page * 1000).ToList();

            ViewBag.Cities = DB.Find<City>(_ => true).SortBy(c => c.ProvinceId).ThenBy(c => c.Name).ToEnumerable();
            ViewBag.SelectedCity = city;
            return View(list);
        }

        [Authorize]
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<Student>(id));
        }

        [HttpPost]
        public IActionResult Edit(Student student, string id)
        {
            DB.UpdateOne<Student>(s => s.Id == ObjectId.Parse(id), Builders<Student>.Update
                .Set(s => s.FirstName, student.FirstName)
                .Set(s => s.LastName, student.LastName)
                .Set(s => s.Address, student.Address)
                .Set(s => s.Coins, student.Coins)
                .Set(s => s.Gender, student.Gender)
                .Set(s => s.Password, student.Password)
                .Set(s => s.Phone, student.Phone)
                .Set(s => s.SchoolName, student.SchoolName));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public ActionResult<DataResponse<Student>> Register([FromBody] Student student)
        {
            DataResponse<Student> response = new DataResponse<Student>();
            if (student == null)
                return response;
            if(string.IsNullOrWhiteSpace(student.FirstName) || string.IsNullOrWhiteSpace(student.LastName))
            {
                response.Message = "نام یا نام خانوادگی نباید خالی باشد!";
                return response;
            }
            if(string.IsNullOrWhiteSpace(student.Phone))
            {
                response.Message = "شماره تلفن نباید خالی باشد!";
                return response;
            }
            if(string.IsNullOrWhiteSpace(student.Password))
            {
                response.Message = "رمز عبوز نباید خالی باشد!";
                return response;
            }
            if(student.Grade < 1 || student.Grade > 12)
            {
                response.Message = "مقطع تحصیلی صحیح نیست!";
                return response;
            }
            if(student.CityId != ObjectId.Empty && !DB.Any<City>(c => c.Id == student.CityId))
            {
                response.Message = "شهر یافت نشد!";
                return response;
            }
            if(student.Grade >= 10 && !DB.Any<StudyField>(f => f.Id == student.StudyFieldId))
            {
                response.Message = "رشته یافت نشد یا انتخاب نشده است!";
                return response;
            }
            if(DB.Any<Student>(s => s.Phone == student.Phone))
            {
                response.Message = "این شماره قبلا ثبت نام نموده است!";
                return response;
            }

            response.Success = true;
            student.Token = Guid.NewGuid();
            student.Coins = Consts.INITIAL_COIN;
            student.RegistrationDate = DateTime.Now;
            student.InvitationCode = Student.GenerateNewInvitationCode(DB);
            if (!string.IsNullOrEmpty(student.InvitedFromCode))
            {
                Student inviteSource = DB.Find<Student>(s => s.InvitationCode == student.InvitedFromCode).FirstOrDefault();
                inviteSource.Coins += Consts.INVITE_PRIZE;
                inviteSource.CoinTransactions.Add(new CoinTransaction { Amount = Consts.INVITE_PRIZE, Type = CoinTransaction.TransactionType.InviteFriend });
                DB.Save(inviteSource);
            }
            else
                student.InvitedFromCode = null;
            response.Data = student;
            DB.Save(student);
            return response;
        }

        [HttpPost]
        public ActionResult<DataResponse<Student>> Update([FromHeader] Guid token, [FromBody] Student update)
        {
            Student st = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (st == null)
                return Unauthorized();

            if (!string.IsNullOrWhiteSpace(update.Address))
                st.Address = update.Address;
            if (update.CityId != ObjectId.Empty)
                st.CityId = update.CityId;
            if (!string.IsNullOrWhiteSpace(update.FirstName))
                st.FirstName = update.FirstName;
            if (!string.IsNullOrWhiteSpace(update.LastName))
                st.LastName = update.LastName;
            if (!string.IsNullOrWhiteSpace(update.SchoolName))
                st.SchoolName = update.SchoolName;
            if (update.StudyFieldId != ObjectId.Empty)
                st.StudyFieldId = update.StudyFieldId;
            DB.Save(st);
            return new DataResponse<Student> { Success = true, Data = st };
        }

        public ActionResult<DataResponse<Student>> Login(string phone, string password)
        {
            Student st = DB.Find<Student>(s => s.Phone == phone && s.Password == password).FirstOrDefault();
            if (st == null)
                return Unauthorized();
            
            st.Token = Guid.NewGuid();
            DB.Save(st);
            return new DataResponse<Student> { Success = true, Data = st };
        }

        private readonly string[] SEASONAL_FESTIVAL_NAMES = new string[] { "بهاره", "تابستانه", "پاییزه", "زمستانه" };

        public ActionResult<DataResponse<ScoresData>> Scores([FromHeader] Guid token)
        {
            PersianDate pDate = PersianDateConverter.ToPersianDate(DateTime.Now);
            int currentMonth = pDate.Month;
            int currentSeason = (currentMonth - 1) / 4;
            DateTime festivalStart = PersianDateConverter.ToGregorianDateTime(new PersianDate(pDate.Year, month: currentSeason * 4 + 1, day: 1));

            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();

            var currentFestival = ScoresData.CurrentFestivalName;
            ScoresData data = new ScoresData();

            data.MyAllTimePoints = me.TotalPoints;
            data.MyAllTimeTotalScore = DB.Count<Student>(s => s.TotalPoints > me.TotalPoints) + 1;

            data.FestivalName = SEASONAL_FESTIVAL_NAMES[currentSeason];
            var myFestival = me.FestivalPoints.FirstOrDefault(f => f.FestivalName == currentFestival);
            data.MyFestivalPoints = myFestival != null ? myFestival.Points : 0;
            data.MyFestivalPointsFromLeague = myFestival != null ? myFestival.PointsFromLeague : 0;
            data.MyFestivalPointsFromOtherQuestions = myFestival != null ? myFestival.PointsFromOtherQuestions : 0;

            data.MyFestivalScore = DB.Count(Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)) + 1;
            data.MyFestivalScoreOnGrade = DB.Count(
                Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(s => s.Grade, me.Grade),
                    Builders<Student>.Filter.ElemMatch(s => s.FestivalPoints, f => f.Points > data.MyFestivalPoints)
                )) + 1;

            data.FestivalTop = scoresDataProvider.FestivalTopStudents;
            data.TotalTop = scoresDataProvider.TotalTopStudents;
            data.FestivalTopOnGrade = scoresDataProvider.GetFestivalTopStudentsInGrade(me.Grade);

            return new DataResponse<ScoresData>
            {
                Success = true,
                Data = data
            };
        }



        public ActionResult<DataResponse<List<CoinTransaction>>> MyCoinTransactions([FromHeader] Guid token)
        {
            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();
            return new DataResponse<List<CoinTransaction>>
            {
                Success = true,
                Data = me.CoinTransactions.OrderByDescending(t => t.Date).Take(100).ToList()
            };
        }
    }
}