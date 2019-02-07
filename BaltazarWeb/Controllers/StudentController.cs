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
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [Authorize(policy: nameof(Permission.ManageStudents))]
        public IActionResult Index(string city = "", int page = 0)
        {
            ObjectId cityId;
            ObjectId.TryParse(city, out cityId);
            var list = DB.Find<Student>(s => s.CityId == cityId).Limit(1000).Skip(page * 1000).ToList();

            var provinces = DB.All<Province>().ToDictionary(i => i.Id, i => i.Name);
            List<SelectListItem> cities = new List<SelectListItem>();
            cities.Add(new SelectListItem("انتخاب نشده", ""));
            cities.AddRange(DB.Find<City>(_ => true).SortBy(c => c.ProvinceId).ThenBy(c => c.Name).ToEnumerable()
                .Select(c => new SelectListItem(provinces[c.ProvinceId] + " - " + c.Name, c.Id.ToString())));
            ViewBag.Cities = cities;
            ViewBag.SelectedCity = city;
            return View(list);
        }

        [Authorize(policy: nameof(Permission.ManageStudents))]
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

                student.CoinTransactions.Add(new CoinTransaction { Amount = Consts.INVITE_PRIZE, Type = CoinTransaction.TransactionType.InviteFriend });
                student.Coins += Consts.INVITE_PRIZE;
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

            int updateCount = 0;
            if (!string.IsNullOrWhiteSpace(update.Address))
            {
                st.Address = update.Address;
                updateCount++;
            }
            if (update.CityId != ObjectId.Empty)
            {
                st.CityId = update.CityId;
                updateCount++;
            }
            if (!string.IsNullOrWhiteSpace(update.FirstName))
                st.FirstName = update.FirstName;
            if (!string.IsNullOrWhiteSpace(update.LastName))
                st.LastName = update.LastName;
            if (!string.IsNullOrWhiteSpace(update.SchoolName))
            {
                st.SchoolName = update.SchoolName;
                updateCount++;
            }
            if(!string.IsNullOrEmpty(update.SchoolPhone))
            {
                st.SchoolPhone = update.SchoolPhone;
                updateCount++;
            }
            if(update.BirthDate.Year > 1900)
            {
                st.BirthDate = update.BirthDate;
                updateCount++;
            }
            if (!string.IsNullOrWhiteSpace(update.NickName))
                st.NickName = update.NickName;
            if (update.StudyFieldId != ObjectId.Empty)
                st.StudyFieldId = update.StudyFieldId;
            if(update.Grade > 0)
                st.Grade = update.Grade;
            if (update.Gender != Student.GenderEnum.Unspecified)
            {
                updateCount++;
                st.Gender = update.Gender;
            }

            string message = null;
            if (updateCount >= 6 && !st.CoinTransactions.Any(t => t.Type == CoinTransaction.TransactionType.ProfileCompletion))
            {
                st.Coins += Consts.PROFILE_COMPLETE_PRIZE;
                st.CoinTransactions.Add(new CoinTransaction { Amount = Consts.PROFILE_COMPLETE_PRIZE, Type = CoinTransaction.TransactionType.ProfileCompletion });
                message = "اطلاعات با موفقیت ذخیره شد و مقدار " + Consts.PROFILE_COMPLETE_PRIZE + " سکه به شما تعلق یافت!";
            }
            DB.Save(st);
            return new DataResponse<Student> { Success = true, Data = st, Message = message };
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
            int currentSeason0Based = (currentMonth - 1) / 3;
            DateTime festivalStart = PersianDateConverter.ToGregorianDateTime(new PersianDate(pDate.Year, month: currentSeason0Based * 3 + 1, day: 1));

            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();

            var currentFestival = ScoresData.CurrentFestivalName;
            ScoresData data = new ScoresData();

            data.MyAllTimePoints = me.TotalPoints;
            data.MyAllTimeTotalScore = DB.Count<Student>(s => s.TotalPoints > me.TotalPoints) + 1;

            data.FestivalName = SEASONAL_FESTIVAL_NAMES[currentSeason0Based];
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
        
        public ActionResult<DataResponse<Student>> Me([FromHeader] Guid token)
        {
            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();
            return new DataResponse<Student> { Success = true, Data = me };
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