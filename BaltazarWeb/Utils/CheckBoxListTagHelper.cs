using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BaltazarWeb.Utils
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement(Attributes = "asp-checkboxlist, asp-modelname")]
    public class CheckBoxListTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-checkboxlist")]
        public IEnumerable<SelectListItem> Items { get; set; }

        [HtmlAttributeName("asp-modelname")]
        public string ModelName { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var i = 0;
            foreach (var item in Items)
            {
                var selected = item.Selected ? @"checked=""checked""" : "";
                var disabled = item.Disabled ? @"disabled=""disabled""" : "";

                var html = $@"<label><input type=""checkbox"" {selected} {disabled} id=""{ModelName}_{i}__Selected"" name=""{ModelName}[{i}].Selected"" value=""true"" /> {item.Text}</label>";
                html += $@"<input type=""hidden"" id=""{ModelName}_{i}__Value"" name=""{ModelName}[{i}].Value"" value=""{item.Value}"">";
                html += $@"<input type=""hidden"" id=""{ModelName}_{i}__Text"" name=""{ModelName}[{i}].Text"" value=""{item.Text}"">";

                output.Content.AppendHtml(html);

                i++;
            }

            output.Attributes.SetAttribute("class", "checkboxlist");
        }
    }
}
