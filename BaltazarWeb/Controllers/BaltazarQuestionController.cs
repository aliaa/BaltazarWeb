using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    [Authorize(policy: nameof(Permission.ManageLeagueQuestions))]
    public class BaltazarQuestionController : Controller
    {
        private readonly MongoHelper DB;
        private readonly IPushNotificationProvider pushProvider;
        private readonly string ImageUploadPath;
        public BaltazarQuestionController(MongoHelper DB, IHostingEnvironment env, IPushNotificationProvider pushProvider)
        {
            this.DB = DB;
            this.pushProvider = pushProvider;
            ImageUploadPath = Path.Combine(env.WebRootPath, Consts.UPLOAD_IMAGE_DIR);
            if (!Directory.Exists(ImageUploadPath))
                Directory.CreateDirectory(ImageUploadPath);
        }
        
        public IActionResult Index()
        {
            return View(DB.Find<BaltazarQuestion>(_ => true).SortByDescending(q => q.CreateDate).ToEnumerable());
        }
        
        public IActionResult Add()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Add(BaltazarQuestion item)
        {
            item.HasImage = item.ImageFile != null;
            DB.Save(item);
            if (item.ImageFile != null)
            {
                string filePath = Path.Combine(ImageUploadPath, item.Id + ".jpg");
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    item.ImageFile.CopyTo(fs);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(string id)
        {
            return View(DB.FindById<BaltazarQuestion>(id));
        }
        
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<BaltazarQuestion>(id));
        }
        
        [HttpPost]
        public IActionResult Edit(BaltazarQuestion item, string id)
        {
            item.Id = ObjectId.Parse(id);
            DB.Save(item);
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            if (DB.Any<Answer>(a => a.QuestionId == objId))
            {
                ModelState.AddModelError("", "آیتم قابل حذف نیست!");
            }
            else
                DB.DeleteOne<BaltazarQuestion>(objId);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult AcceptAnswer(string questionId, string answerId)
        {
            BaltazarQuestion question = DB.FindById<BaltazarQuestion>(questionId);
            Answer answer = DB.FindById<Answer>(answerId);
            if (question == null || answer == null || answer.QuestionId != question.Id)
                return RedirectToAction(nameof(Index));

            if (answer.Response == Answer.QuestionerResponseEnum.NotSeen && answer.ToBaltazarQuestion)
            {
                DB.UpdateOne<Answer>(a => a.Id == answer.Id, Builders<Answer>.Update.Set(a => a.Response, Answer.QuestionerResponseEnum.Accepted));
                Student answerer = DB.FindById<Student>(answer.UserId);
                if(answerer != null)
                    AnswerController.AddPointsToStudentForCorrectAnswering(DB, pushProvider, answerer, question);
            }
            return RedirectToAction(nameof(ApproveList));
        }

        public IActionResult RejectAnswer(string questionId, string answerId)
        {
            BaltazarQuestion question = DB.FindById<BaltazarQuestion>(questionId);
            Answer answer = DB.FindById<Answer>(answerId);
            if (question == null || answer == null || answer.QuestionId != question.Id)
                return RedirectToAction(nameof(Index));

            if (answer.Response == Answer.QuestionerResponseEnum.NotSeen && answer.ToBaltazarQuestion)
            {
                DB.UpdateOne<Answer>(a => a.Id == answer.Id, Builders<Answer>.Update.Set(a => a.Response, Answer.QuestionerResponseEnum.Rejected));
            }
            return RedirectToAction(nameof(ApproveList));
        }

        public IActionResult ApproveList()
        {
            var list = DB.Find<Answer>(a => a.ToBaltazarQuestion && a.Response == Answer.QuestionerResponseEnum.NotSeen)
                .SortBy(a => a.CreateDate).Limit(1000).ToList();
            foreach (var item in list)
                item.UserName = DB.FindById<Student>(item.UserId).DisplayName;
            return View(list);
        }
    }
}