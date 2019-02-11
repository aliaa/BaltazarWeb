using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ViewModels
{
    public class AccountViewModel
    {
        private AuthUserX _user = null;
        public AuthUserX User
        {
            get { return _user; }
            set
            {
                _user = value;
                foreach (var perm in value.Permissions)
                    SelectablePermissions.First(p => p.Value == perm.ToString()).Selected = true;
            }
        }

        private List<SelectListItem> _selectablePermissions;
        public List<SelectListItem> SelectablePermissions
        {
            get { return _selectablePermissions; }
            set
            {
                _selectablePermissions = value;
                if(User != null)
                    User.Permissions = value.Where(p => p.Selected).Select(p => Enum.Parse<Permission>(p.Value)).ToList();
            }
        }

        private string _password;
        [Display(Name = "رمز عبور")]
        [MinLength(6)]
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                if(User != null && !string.IsNullOrEmpty(value))
                    User.Password = value;
            }
        }
        
        public AccountViewModel()
        {
            SelectablePermissions = Enum.GetValues(typeof(Permission)).Cast<Permission>()
                .Select(p => new SelectListItem(AliaaCommon.Utils.GetDisplayNameOfMember(typeof(Permission), p.ToString()), p.ToString())).ToList();
        }
        
    }
}
