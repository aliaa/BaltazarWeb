using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly MongoHelper DB;

        public CourseController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Index()
        {
            return View(DB.Find<Course>(_ => true).SortBy(c => c.Grade).ThenBy(c => c.StudyFieldId).ThenBy(c => c.Name).ToEnumerable());
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Course course)
        {
            if (course.Grade < 10)
                course.StudyFieldId = ObjectId.Empty;
            if (DB.Any<Course>(i => i.Grade == course.Grade && i.StudyFieldId == course.StudyFieldId && i.Name == course.Name))
            {
                ModelState.AddModelError("", "درس با این نام قبلا وارد شده است!");
                return View(course);
            }
            DB.Save(course);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<CourseSection>(s => s.CourseId == objId) || DB.Any<Question>(q => q.CourseId == objId))
                ModelState.AddModelError("", "درس قابل حذف نیست!");
            else
                DB.DeleteOne<Course>(objId);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            return View(DB.FindById<Course>(id));
        }

        [HttpPost]
        public IActionResult Edit(Course Course, string id)
        {
            var updateDef = Builders<Course>.Update.Set(a => a.Name, Course.Name);
            DB.UpdateOne(a => a.Id == ObjectId.Parse(id), updateDef);
            return RedirectToAction(nameof(Index));
        }
    }
}