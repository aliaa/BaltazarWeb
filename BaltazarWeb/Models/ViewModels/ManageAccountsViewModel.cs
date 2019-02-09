using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ViewModels
{
    public class ManageAccountsViewModel
    {
        public string Id { get; set; }

        private AuthUserX _user = null;
        public AuthUserX User
        {
            get { return _user; }
            set
            {
                _user = value;
                if (value == null)
                    return;
                if(value.Id != ObjectId.Empty)
                    Id = _user.Id.ToString();
                foreach (var item in value.Permissions)
                {
                    Permissions.First(p => p.Value == item.ToString()).Selected = true;
                }
            }
        }

        public List<SelectListItem> Permissions { get; set; }

        public void Initialize()
        {
            Permissions = Enum.GetValues(typeof(Permission)).Cast<Permission>()
                .Select(p => new SelectListItem(AliaaCommon.Utils.GetDisplayNameOfMember(typeof(Permission), p.ToString()), p.ToString())).ToList();
        }

        public void SetUserData()
        {
            if (Permissions == null || User == null)
                return;
            User.Permissions = Permissions.Where(p => p.Selected).Select(p => Enum.Parse<Permission>(p.Value)).ToList();
            if (User.Id == ObjectId.Empty && !string.IsNullOrEmpty(Id))
                User.Id = ObjectId.Parse(Id);
        }
    }
}
