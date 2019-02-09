using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AliaaCommon.MongoDB;
using BaltazarWeb.Models;
using BaltazarWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BaltazarWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoHelper DB;

        public AccountController(MongoHelper DB)
        {
            this.DB = DB;
        }

        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                AuthUserX user = DB.CheckAuthentication(model.Username, model.Password);
                if (user != null)
                {
                    List<Claim> claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.DisplayName),
                    };

                    StringBuilder permsStr = new StringBuilder();
                    if (user.IsAdmin)
                    {
                        foreach (string p in Enum.GetNames(typeof(Permission)))
                            permsStr.Append(p).Append(",");
                        claims.Add(new Claim("IsAdmin", "true"));
                    }
                    else
                    {
                        foreach (Permission p in user.Permissions)
                            permsStr.Append(p).Append(",");
                    }
                    claims.Add(new Claim(nameof(Permission), permsStr.ToString()));
                    

                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = model.RememberMe }).Wait();
                    return RedirectToLocal(model.ReturnUrl);
                }
            }
            ModelState.AddModelError("", "نام کاربری یا رمز عبور صحیح نیست!");
            return View(model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (returnUrl != null && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [Authorize(policy: "Admin")]
        public IActionResult Manage(ManageAccountsViewModel model)
        {
            List<SelectListItem> dropdownItems = new List<SelectListItem>();
            dropdownItems.Add(new SelectListItem("- انتخاب کنید -", ""));
            dropdownItems.AddRange(DB.All<AuthUserX>().Select(u => new SelectListItem(u.DisplayName, u.Id.ToString())));
            ViewBag.Users = dropdownItems;

            if (!string.IsNullOrEmpty(model.Id))
            {
                model.Initialize();
                model.User = DB.FindById<AuthUserX>(model.Id);
                return View(model);
            }
            return View();
        }

        [Authorize(policy: "Admin")]
        [HttpPost]
        public IActionResult SaveUser(ManageAccountsViewModel model)
        {
            model.SetUserData();
            var user = model.User;
            DB.UpdateOne(u => u.Id == user.Id,
                Builders<AuthUserX>.Update.Set(u => u.Disabled, user.Disabled)
                    .Set(u => u.FirstName, user.FirstName)
                    .Set(u => u.LastName, user.LastName)
                    .Set(u => u.Username, user.Username)
                    .Set(u => u.Permissions, user.Permissions));
            return RedirectToAction(nameof(Manage), new { user.Id });
        }
    }
}