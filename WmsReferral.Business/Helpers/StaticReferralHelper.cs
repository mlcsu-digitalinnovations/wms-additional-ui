using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsReferral.Business.Models;
using static System.Net.Mime.MediaTypeNames;

namespace WmsReferral.Business.Helpers
{
    public static class StaticReferralHelper
    {
        public static IEnumerable<KeyValuePair<string, string>> GetSexes()
        {
            List<KeyValuePair<string, string>> sexList = new()
            {
                new KeyValuePair<string, string>("Female", "Female"),
                new KeyValuePair<string, string>("Male", "Male"),
            };

            return sexList;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetYNList()
        {
            List<KeyValuePair<string, string>> ynList = new()
            {
                new KeyValuePair<string, string>("true", "Yes"),
                new KeyValuePair<string, string>("false", "No"),
                new KeyValuePair<string, string>("null", "Don't know / Prefer not to say")
            };

            return ynList;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetSentimentList()
        {
            List<KeyValuePair<string, string>> list = new()
            {
                new KeyValuePair<string, string>(SentimentEnum.StronglyAgree.ToString(), "Strongly agree"),
                new KeyValuePair<string, string>(SentimentEnum.Agree.ToString(), "Agree"),
                new KeyValuePair<string, string>(SentimentEnum.NeitherAgreeOrDisagree.ToString(), "Neither agree or disagree"),
                new KeyValuePair<string, string>(SentimentEnum.Disagree.ToString(), "Disagree"),
                new KeyValuePair<string, string>(SentimentEnum.StronglyDisagree.ToString(), "Strongly disagree")
            };

            return list;
        }
        public static IEnumerable<KeyValuePair<string, string>> GetExperienceList()
        {
            List<KeyValuePair<string, string>> list = new()
            {
                new KeyValuePair<string, string>(ExperienceEnum.VeryGood.ToString(), "Very good"),
                new KeyValuePair<string, string>(ExperienceEnum.Good.ToString(), "Good"),
                new KeyValuePair<string, string>(ExperienceEnum.NeitherGoodNorPoor.ToString(), "Neither good nor poor"),
                new KeyValuePair<string, string>(ExperienceEnum.Poor.ToString(), "Poor"),
                new KeyValuePair<string, string>(ExperienceEnum.VeryPoor.ToString(), "Very poor"),
                new KeyValuePair<string, string>(ExperienceEnum.DoNotKnow.ToString(), "Don't know")
            };

            return list;
        }
        public static IEnumerable<KeyValuePair<string, string>> GetSurveyQ3List()
        {
            List<KeyValuePair<string, string>> list = new()
            {
                new KeyValuePair<string, string>("a. I had difficulty getting set up or logging in", "a. I had difficulty getting set up or logging in"),
                new KeyValuePair<string, string>("b. I didn’t understand what the programme would involve", "b. I didn’t understand what the programme would involve"),
                new KeyValuePair<string, string>("c. I didn’t think the programme would help me to achieve my aims", "c. I didn’t think the programme would help me to achieve my aims"),
                new KeyValuePair<string, string>("d. I wasn’t ready to start the programme at this time", "d. I wasn’t ready to start the programme at this time"),
                new KeyValuePair<string, string>("e. Other", "e. Other")
                
            };

            return list;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetSurveyQ4List()
        {
            List<KeyValuePair<string, string>> list = new()
            {
                new KeyValuePair<string, string>("a. I didn’t have enough time", "a. I didn’t have enough time"),                
                new KeyValuePair<string, string>("b. I didn’t feel that I was making enough progress", "b. I didn’t feel that I was making enough progress"),
                new KeyValuePair<string, string>("c. I found the app/website difficult to use", "c. I found the app/website difficult to use"),
                new KeyValuePair<string, string>("d. I wanted a professional to help me in-person, not through a website or app", "d. I wanted a professional to help me in-person, not through a website or app"),
                new KeyValuePair<string, string>("e. I wanted something else other than a weight management programme to help me achieve my goals", "e. I wanted something else other than a weight management programme to help me achieve my goals"),
                new KeyValuePair<string, string>("f. Other", "f. Other")

            };

            return list;
        }

        public static string StringCleaner(string stringToClean, bool strict=true)
        {
            if (!strict)
                return stringToClean != null ? Regex.Replace(stringToClean, @"[^0-9a-zA-Z,\- '’?!;@.\r\n]+", "").Trim() : null;
            return stringToClean != null ? Regex.Replace(stringToClean, @"[^0-9a-zA-ZÀ-ž,\- '’]+", "").Trim() : null;
        }

        public static bool ValidateNHSNumber(string nhsnumber)
        {

            nhsnumber = Regex.Replace(nhsnumber, @"[\D]+", "");

            if (nhsnumber.Length == 10)
            {
                var sum1 = 0;
                var digits = nhsnumber.ToString().Select(t => int.Parse(t.ToString())).ToArray();
                for (int i = 0; i < 9; i++)
                {
                    int thenmum = digits[i];
                    sum1 += digits[i] * (10 - i);
                }
                var sum2 = sum1 % 11; //get the remainder

                if (sum2 == 0)
                    sum2 = 11;//11 is represented by 0

                if ((11 - sum2) == digits.Last())
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static Dictionary<string, string> GetAPIErrorDictionary(string content)
        {

            var jObj = JObject.Parse(content);
            var title = jObj.SelectToken("$.title")?.Value<string>();
            var traceId = jObj.SelectToken("$.traceId")?.Value<string>();
            var detail = jObj.SelectToken("$.detail")?.Value<string>();
            var errors = jObj.SelectToken("$.errors")?.ToObject<Dictionary<string, List<string>>>();
            var rowerrors = jObj.SelectToken("$.rowerrors")?.ToObject<Dictionary<int, List<string>>>();
            var providerName = jObj.SelectToken("$.providerName")?.Value<string>();
            var errorDescription = jObj.SelectToken("$.errorDescription")?.Value<string>();
            var ubrn = jObj.SelectToken("$.ubrn")?.Value<string>();
            var dateOfReferral = jObj.SelectToken("$.dateOfReferral")?.Value<DateTime?>();
            var errorStatus = jObj.SelectToken("$.error")?.Value<string>();

            Dictionary<string, string> telemErrors = new()
            {
                { "Error", title },
                { "TraceId", traceId }
            };

            if (detail != null)
                telemErrors.Add("Detail", detail);
            if (errors != null)
                foreach (var e in errors.ToDictionary(s => s.Key, s => s.Value.First()))
                    telemErrors.Add(e.Key, e.Value);
            if (providerName != null)
                telemErrors.Add("providerName", providerName);
            if (ubrn != null)
                telemErrors.Add("ubrn", ubrn);
            if (errorDescription != null)
                telemErrors.Add("errorDescription", errorDescription);
            if (dateOfReferral != null)
                telemErrors.Add("dateOfReferral", dateOfReferral.Value.ToUniversalTime().ToString());
            if (errorStatus != null)
                telemErrors.Add("errorStatus", errorStatus);

            return telemErrors;
        }
        public static Dictionary<string, string> GetAPIErrorDictionary(string content, Referral referral)
        {

            var jObj = JObject.Parse(content);
            var title = jObj.SelectToken("$.title").Value<string>();
            var traceId = jObj.SelectToken("$.traceId")?.Value<string>();
            var detail = jObj.SelectToken("$.detail")?.Value<string>();
            var errors = jObj.SelectToken("$.errors")?.ToObject<Dictionary<string, List<string>>>();

            Dictionary<string, string> telemErrors = new()
            {
                { "Error", title },
                { "TraceId", traceId }
            };

            if (detail != null)
                telemErrors.Add("Detail", detail);
            if (errors != null)
                foreach (var e in errors.ToDictionary(s => s.Key, s => s.Value.First()))
                    telemErrors.Add(e.Key, e.Value);

            if (telemErrors.ContainsKey("Email"))
            {
                //specific error relating to email validation
                telemErrors.Add("RefEmail", referral.Email);
            }
            if (telemErrors.ContainsKey("DateOfBirth"))
            {
                //specific error relating to dob validation
                telemErrors.Add("RefDOB", referral.DateOfBirth.Value.ToString());
            }
            if (telemErrors.ContainsKey("Mobile"))
            {
                //specific error relating to mobile validation
                telemErrors.Add("RefMobile", referral.Mobile);
            }
            if (telemErrors.ContainsKey("$.HeightCm"))
            {
                //specific error relating to height validation
                telemErrors.Add("RefHeight", referral.HeightCm.ToString());
            }
            if (telemErrors.ContainsKey("$.WeightKg"))
            {
                //specific error relating to weight validation
                telemErrors.Add("RefWeight", referral.WeightKg.ToString());
            }

            return telemErrors;
        }

        public static Referral FinalCheckAnswerChecks(Referral referral)
        {
            //amend phonenumbers to +44
            if (referral.Telephone != null)
            {
                if (referral.Telephone.Substring(0, 1) == "0")
                    referral.Telephone = "+44" + referral.Telephone[1..];
                referral.Telephone = referral.Telephone.Replace(" ", "");

                if (referral.Telephone.Substring(0, 2) == "44")
                    referral.Telephone = "+44" + referral.Telephone[2..];
            }

            if (referral.Mobile != null)
            {
                if (referral.Mobile.Substring(0, 1) == "0")
                    referral.Mobile = "+44" + referral.Mobile[1..];
                referral.Mobile = referral.Mobile.Replace(" ", "");

                if (referral.Mobile.Substring(0, 2) == "44")
                    referral.Mobile = "+44" + referral.Mobile[2..];

            }
            return referral;
        }

        public static int WordCount(string textIn)
        {
            if (textIn == null)
                return 0;

            int wordsCount = 0, index = 0;

            // skip whitespace until first word
            while (index < textIn.Length && char.IsWhiteSpace(textIn[index]))
                index++;

            while (index < textIn.Length)
            {
                // check if current char is part of a word
                while (index < textIn.Length && !char.IsWhiteSpace(textIn[index]))
                    index++;

                wordsCount++;

                // skip whitespace until next word
                while (index < textIn.Length && char.IsWhiteSpace(textIn[index]))
                    index++;
            }

            return wordsCount;
        }
    }
}
