using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private readonly IPushNotificationProvider pushProvider;

        public StudentController(MongoHelper DB, IPushNotificationProvider pushProvider)
        {
            this.DB = DB;
            this.pushProvider = pushProvider;
        }

        [Authorize(policy: nameof(Permission.ManageStudents))]
        public IActionResult Index(string city = "", int page = 0)
        {
            ObjectId cityId;
            ObjectId.TryParse(city, out cityId);
            var list = DB.Find<Student>(s => s.CityId == cityId && s.IsTeacher != true).Limit(1000).Skip(page * 1000).SortByDescending(s => s.RegistrationDate).ToList();

            List<SelectListItem> cities = new List<SelectListItem>();
            long cityUnselectedStudentsCount = DB.Count<Student>(s => s.CityId == ObjectId.Empty);
            cities.Add(new SelectListItem("انتخاب نشده (" + cityUnselectedStudentsCount + ")", ""));
            cities.AddRange(GetCitiesItem());
            ViewBag.Cities = cities;
            ViewBag.SelectedCity = city;
            return View(list);
        }
        
        class CityStudentCount
        {
            public ObjectId ProvinceId { get; set; }
            public ObjectId _id { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
        }
        
        private IEnumerable<SelectListItem> GetCitiesItem()
        {
            var provinces = DB.All<Province>().ToDictionary(i => i.Id, i => i.Name);
            var result = DB.Aggregate<Student>()
                .Match(s => s.IsTeacher != true)
                .Lookup(nameof(City), nameof(Student.CityId), "_id", "City")
                .Project("{ City: { $arrayElemAt: [ \"$City\", 0 ] }, Count: { $size: \"$City\" }}")
                .Match("{Count: { $gt: 0 }}")
                .Group("{ _id: \"$City._id\", Name: {$first: \"$City.Name\"}, ProvinceId: {$first: \"$City.ProvinceId\"}, Count: {$sum: 1}}")
                .As<CityStudentCount>()
                .ToList();
            return result.Select(i => new SelectListItem { Text = provinces[i.ProvinceId] + " - " + i.Name + " (" + i.Count + ")", Value = i._id.ToString() });
        }

        [Authorize(policy: nameof(Permission.ManageStudents))]
        public IActionResult Edit(string id)
        {
            return View(DB.FindById<Student>(id));
        }

        [Authorize(policy: nameof(Permission.ManageStudents))]
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

        [Authorize(policy: nameof(Permission.ManageStudents))]
        public IActionResult Details(string id)
        {
            return View(DB.FindById<Student>(id));
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
            if(DB.Any<Student>(s => s.Password == student.Password))
            {
                response.Message = "این کد ملی قبلا ثبت نام شده است!";
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
            if(!string.IsNullOrEmpty(student.PusheId) && DB.Any<Student>(s => s.PusheId == student.PusheId))
            {
                response.Message = "از این دستگاه قبلا ثبت نام شده است! هر دستگاه مجاز است فقط یکبار ثبت نام کند!";
                return response;
            }

            response.Success = true;
            student.Token = Guid.NewGuid();
            student.Coins = Consts.INITIAL_COIN;
            student.CoinTransactions.Add(new CoinTransaction { Amount = Consts.INITIAL_COIN, Type = CoinTransaction.TransactionType.Register });
            student.RegistrationDate = DateTime.Now;
            student.InvitationCode = Student.GenerateNewInvitationCode(DB);
            if (!string.IsNullOrEmpty(student.InvitedFromCode))
            {
                if (student.InvitedFromCode.ToLower() == "teach")
                {
                    student.IsTeacher = true;
                }
                else
                {
                    Student inviteSource = DB.Find<Student>(s => s.InvitationCode == student.InvitedFromCode).FirstOrDefault();
                    inviteSource.Coins += Consts.INVITE_PRIZE;
                    inviteSource.CoinTransactions.Add(new CoinTransaction { Amount = Consts.INVITE_PRIZE, Type = CoinTransaction.TransactionType.InviteFriend });
                    DB.Save(inviteSource);
                    if (inviteSource.PusheId != null)
                        pushProvider.SendMessageToUser("سکه بالتازار!",
                            "تبریک! یکی از دوستان شما با وارد کردن کد دعوت شما عضو بالتازار شد و " + Consts.INVITE_PRIZE + " سکه به شما تعلق یافت!",
                            inviteSource.PusheId);

                    student.CoinTransactions.Add(new CoinTransaction { Amount = Consts.INVITED_PRIZE, Type = CoinTransaction.TransactionType.InviteFriend });
                    student.Coins += Consts.INVITED_PRIZE;
                }
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

        public IActionResult Delete(string id)
        {
            ObjectId objId = ObjectId.Parse(id);
            Student student = DB.FindById<Student>(objId);
            if (student == null)
            {
                ModelState.AddModelError("", "دانش آموز یافت نشد!");
            }
            else
            {
                DeletedItems deletedItems = new DeletedItems { Type = nameof(Student) };
                deletedItems.User = ObjectId.Parse(HttpContext.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                deletedItems.Items.Add(student);
                deletedItems.Items.AddRange(DB.Find<Question>(q => q.UserId == objId).ToEnumerable());
                deletedItems.Items.AddRange(DB.Find<Answer>(a => a.UserId == objId).ToEnumerable());
                deletedItems.Items.AddRange(DB.Find<ShopOrder>(o => o.UserId == objId).ToEnumerable());
                DB.Save(deletedItems);

                DB.DeleteOne<Student>(objId);
                DB.DeleteMany<Question>(q => q.UserId == objId);
                DB.DeleteMany<Answer>(a => a.UserId == objId);
                DB.DeleteMany<ShopOrder>(o => o.UserId == objId);
            }
            return RedirectToAction(nameof(Index));
        }


        [Authorize(policy: nameof(Permission.ManageTeachers))]
        public IActionResult Teachers()
        {
            var list = DB.Find<Student>(s => s.IsTeacher == true).SortByDescending(s => s.RegistrationDate).ToList();
            return View(list);
        }

        [Authorize(policy: nameof(Permission.ManageTeachers))]
        public IActionResult EditTeacher(string id)
        {
            return View(DB.FindById<Student>(id));
        }

        [Authorize(policy: nameof(Permission.ManageTeachers))]
        [HttpPost]
        public IActionResult EditTeacher(Student update, string id)
        {
            DB.UpdateOne<Student>(s => s.Id == ObjectId.Parse(id), Builders<Student>.Update
                .Set(s => s.FirstName, update.FirstName)
                .Set(s => s.LastName, update.LastName)
                .Set(s => s.Password, update.Password)
                .Set(s => s.Phone, update.Phone));
            return RedirectToAction(nameof(Teachers));
        }

        public ActionResult<DataResponse<TeacherStatus>> TeacherStats([FromHeader] Guid token)
        {
            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null || !me.IsTeacher)
                return Unauthorized();
            var stats = new TeacherStatus
            {
                AnswersCount = (int)DB.Count<Answer>(a => a.UserId == me.Id)
            };
            foreach (var id in DB.Find<Answer>(a => a.UserId == me.Id).Project(a => a.Id).ToEnumerable())
            {
                if (DB.Any<Question>(q => q.AcceptedAnswerId == id))
                    stats.AcceptedAnswersCount++;
            }
            return new DataResponse<TeacherStatus> { Success = true, Data = stats };
        }

        public ActionResult<CommonResponse> RequestWithdrawal([FromHeader] Guid token, [FromQuery] string card)
        {
            Student me = DB.Find<Student>(s => s.Token == token).FirstOrDefault();
            if (me == null || !me.IsTeacher)
                return Unauthorized();
            DB.Save(new WithdrawRequest { TeacherId = me.Id, CardNumber = card });
            return new CommonResponse { Success = true, Message = "درخواست شما با موفقیت ذخیره شد!" };
        }

        [Authorize(policy: nameof(Permission.ManageTeachers))]
        public IActionResult WithdrawRequests()
        {
            var list = DB.Find<WithdrawRequest>(_ => true).SortByDescending(s => s.Date).ToList();
            return View(list);
        }
    }
}