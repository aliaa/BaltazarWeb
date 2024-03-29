﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize(policy: nameof(Permission.ManageCourses))]
    public class CourseSectionController : Controller
    {
        private readonly MongoHelper DB;

        public CourseSectionController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Index()
        {
            return View(DB.All<CourseSection>());
        }

        public IEnumerable<SelectListItem> CourseDropdownItems
        {
            get
            {
                var studyFields = DB.All<StudyField>().ToDictionary(i => i.Id, i => i.Name);
                return DB.Find<Course>(_ => true).SortBy(c => c.Grade).ThenBy(c => c.StudyFieldId).ThenBy(c => c.Name).ToEnumerable()
                    .Select(c => new SelectListItem(c.Grade + " - " + (c.StudyFieldId == ObjectId.Empty ? "" : (studyFields[c.StudyFieldId] + " - ")) + c.Name, c.Id.ToString()));
            }
        }

        public IActionResult Add()
        {
            ViewData["courses"] = CourseDropdownItems;
            return View();
        }

        [HttpPost]
        public IActionResult Add(CourseSection CourseSection)
        {
            if (DB.Any<CourseSection>(i => i.Name == CourseSection.Name))
            {
                ModelState.AddModelError("", "سرفصل با این نام قبلا وارد شده است!");
                return View(CourseSection);
            }
            DB.Save(CourseSection);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<Question>(s => s.SectionId == objId))
                ModelState.AddModelError("", "سرفصل قابل حذف نیست!");
            else
                DB.DeleteOne<CourseSection>(objId);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(string id)
        {
            ViewData["courses"] = CourseDropdownItems;
            return View(DB.FindById<CourseSection>(id));
        }

        [HttpPost]
        public IActionResult Edit(CourseSection CourseSection, string id)
        {
            var updateDef = Builders<CourseSection>.Update.Set(a => a.Name, CourseSection.Name);
            DB.UpdateOne(a => a.Id == ObjectId.Parse(id), updateDef);
            return RedirectToAction(nameof(Index));
        }
    }
}