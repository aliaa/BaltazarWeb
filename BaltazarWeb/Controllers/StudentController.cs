using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            return View();
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
    }
}