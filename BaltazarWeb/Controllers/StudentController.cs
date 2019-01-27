using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
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
        public StudentController(MongoHelper DB)
        {
            this.DB = DB;
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

        public ActionResult<DataResponse<ScoresData>> Scores([FromHeader] Guid token)
        {
            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null)
                return Unauthorized();

            ScoresData data = new ScoresData
            {
                MyPoints = me.Points,
                MyPointsFromLeague = me.PointsFromLeague,
                MyPointsFromOtherQuestions = me.PointsFromOtherQuestions,
                MyTotalScore = DB.Count<Student>(s => s.Points > me.Points) + 1,
                MyScoreOnBase = DB.Count<Student>(s => s.Points > me.Points && s.Grade == me.Grade) + 1,
                TotalTop = DB.Find<Student>(s => true).SortByDescending(s => s.Points).Limit(10).ToEnumerable()
                    .Select(s => new TopStudent { UserName = s.DisplayName, CityId = s.CityId, Points = s.Points, School = s.SchoolName }).ToList(),
                TopOnBase = DB.Find<Student>(s => s.Grade == me.Grade).SortByDescending(s => s.Points).Limit(10).ToEnumerable()
                    .Select(s => new TopStudent { UserName = s.DisplayName, CityId = s.CityId, Points = s.Points, School = s.SchoolName }).ToList(),
            };
            return new DataResponse<ScoresData>
            {
                Success = true,
                Data = data
            };
        }
    }
}