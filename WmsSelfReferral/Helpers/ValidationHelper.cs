using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System;
using System.Web;

namespace WmsSelfReferral.Helpers
{
    public static class ValidationHelper
    {
        public static bool LinkIdIsValid(string linkId)
        {
          try
          {
            Regex regex = new("^[23456789abcdefghijkmnpqrstuvwxyz]+$", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            return regex.IsMatch(linkId);
          }
          catch (RegexMatchTimeoutException)
          {
            return false;
          }
        }

        public static HtmlString ValidationTextNoTags<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            var result = "";
            Regex regex = new Regex("<span.*?>(.*?)<\\/span>");//regular expression to extract text between span tags

            using (var writer = new StringWriter())
            {
                htmlHelper.ValidationMessageFor(expression).WriteTo(writer, HtmlEncoder.Default);
                result = writer.ToString();
            }
            //get the actual content between tags
            result = regex.Match(result).Groups[1].ToString();

            return new HtmlString(result);
        }
        public static HtmlString ValidationMessageEncodedFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            var result = "";
            
            using (var writer = new StringWriter())
            {
                htmlHelper.ValidationMessageFor(expression).WriteTo(writer, HtmlEncoder.Default);
                result = writer.ToString();
            }
           
            result = HttpUtility.HtmlDecode(result).ToString();

            return new HtmlString(result);
        }
    }
}
