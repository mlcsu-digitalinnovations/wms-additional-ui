using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Helpers
{
    public class NhsFormHelper
    {
        public static string GetFormElementClasses(string element, bool error)
        {
            string cssClasses = "nhsuk-input";

            switch (element)
            {
                case "text":
                    break;
                case "textarea":
                    cssClasses = "nhsuk-textarea";
                    break;
                case "form-group":
                    cssClasses = "nhsuk-form-group";
                    break;
                case "date":
                       break;
            }

            if (error)
            {
                cssClasses += " " + cssClasses + "--error";
            }

            if (element == "date")
            {
                cssClasses += " " + "nhsuk-date-input__input";
            }

            cssClasses += " ";

            return cssClasses;
        }
    }

}
