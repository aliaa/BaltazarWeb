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
        public IActionResult Register([FromBody] Student student)
        {
            DataResponse<Student> response = new DataResponse<Student>();
            if (!ModelState.IsValid || student == null || string.IsNullOrWhiteSpace(student.FirstName) || string.IsNullOrWhiteSpace(student.LastName) ||
                string.IsNullOrWhiteSpace(student.Phone) || string.IsNullOrWhiteSpace(student.Password) || !DB.Any<City>(c => c.Id == student.CityId) ||
                student.Grade < 1 || student.Grade > 12 || (student.Grade >= 10 && !DB.Any<StudyField>(f => f.Id == student.StudyFieldId)))
            {
                response.Success = false;
            }
            else if(DB.Any<Student>(s => s.Phone == student.Phone))
            {
                response.Success = false;
                response.Message = "این شماره قبلا ثبت نام نموده است!";
            }
            else
            {
                response.Success = true;
                student.Token = Guid.NewGuid();
                response.Data = student;
                DB.Save(student);
            }
            return CreatedAtAction(nameof(Register), response);
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