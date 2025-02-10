using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Helpers
{
    public static class Constants
    {
        public const string REGEX_PHONE_PLUS_NUMLENGTH = @"^0[0-9]+$";
        public const string REGEX_NUMERIC_STRING = @"^[0-9]+$";
        public const string REGEX_MOBILE_PHONE_UK = @"^(?:07|\+?447)(?:\d\s?){9}$";
        public const string REGEX_PHONE_UK = @"^(?:0|\+?44)(?:\d\s?){9,10}$";

        public const int MAX_SECONDS_API_REQUEST_AHEAD = 300;

        public const int HOURS_BEFORE_NEXT_STAGE = 48;

        public const string MINIMUM_REQUEST_DATE = "2021-02-01";
        public const string MAXIMUM_REQUEST_DATE = "2121-02-01";
        public const string MINIMUM_DATE_OF_BIRTH = "1900-01-01";
        public const string UNKNOWN_GP_PRACTICE_NUMBER = "V81999";
        public const string UNKNOWN_GP_PRACTICE_NAME = "Unknown";
        public static string DATE_OF_BIRTH_EXPIRY = "DoB Expiry";
        public const string REGEX_IPv4_ADDRESS =
          "((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.)" +
          "{3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)";
        public const string REGEX_WMS_VALID_EMAIL_DOMAINS = @"^[a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1}([a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1})*[a-zA-Z0-9](@nhs\.net|(?:@(?:[a-zA-Z0-9-]+\.)?[a-zA-Z0-9-]+\.)?nhs\.uk(?<!\.scot\.nhs\.uk|\@scot\.nhs\.uk|\.wales\.nhs\.uk|\@wales\.nhs\.uk))$";
        public const string REGEX_WMS_NHSNET_EMAIL_DOMAINS = @"^[a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1}([a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1})*[a-zA-Z0-9](@nhs\.net)$";
        public const string REGEX_UKPOSTCODE = @"^[A-z]{1,2}\d[A-z\d]?\s*\d[A-z]{2}$";
        public const string REGEX_ODSCODE_PHARMACY = @"^(F|f)[a-zA-Z0-9]{4}$";
        public const string REGEX_ODSCODE_GPPRACTICE = @"^([a-zA-Z]{1})[0-9]{5}$";
        public const string REGEX_WMS_VALID_EMAILADDRESS = @"^[a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1}([a-zA-Z0-9][\-_\.\+\!\#\$\%\&\'\*\/\=\?\^\`\{\|]{0,1})*[a-zA-Z0-9]@[a-zA-Z0-9][-\.]{0,1}([a-zA-Z][-\.]{0,1})*[a-zA-Z0-9]\.[a-zA-Z0-9]{1,}([\.\-]{0,1}[a-zA-Z]){0,}[a-zA-Z0-9]{0,}$";
        public const string REGEX_FAMILYNAMES = @"^[A-Za-zÀ-ÖØ-öø-įĴ-őŔ-žǍ-ǰǴ-ǵǸ-țȞ-ȟȤ-ȳɃɆ-ɏḀ-ẞƀ-ƓƗ-ƚƝ-ơƤ-ƥƫ-ưƲ-ƶẠ-ỿ’'ŒœıĪīĮį][A-Za-zÀ-ÖØ-öø-įĴ-őŔ-žǍ-ǰǴ-ǵǸ-țȞ-ȟȤ-ȳɃɆ-ɏḀ-ẞƀ-ƓƗ-ƚƝ-ơƤ-ƥƫ-ưƲ-ƶẠ-ỿ\-’'ŒœıĪīĮį ]*$";
        public const string REGEX_NUMERICS = @"^(\d*\.)?\d+$";
    }
}
