using Newtonsoft.Json.Linq;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;

namespace WmsSurveys.Web.Helpers
{
    public static class StaticQuestionnaireHelper
    {
        public static QuestionsViewModel RetrieveAnswers(int q, QuestionnaireViewModel survey, IEnumerable<KeyValuePair<string, string>> responses)
        {

            if (survey.NotificationKey != null)
            {
                if (survey.QuestionAnswers.Where(w => w.QuestionId == q).Any())
                {
                    var surveyquestionvm = survey.QuestionAnswers.Where(w => w.QuestionId == q).First();
                    surveyquestionvm.Responses = responses;
                    return surveyquestionvm;
                }
            }


            return new QuestionsViewModel() { Responses = responses };
        }

        public static QuestionnaireViewModel SaveAnswers(QuestionsViewModel model, QuestionnaireViewModel survey, int q)
        {

            if (survey.NotificationKey != null)
            {
                if (!survey.QuestionAnswers.Any(f => f.QuestionId == q))
                {
                    //if "a" is null, mark it string.empty
                    model.QuestionAresponse ??= string.Empty;

                    model.QuestionId = q;

                    //clean model
                    model.QuestionAresponse = StaticReferralHelper.StringCleaner(model.QuestionAresponse, false) ?? "";
                    model.QuestionBresponse = StaticReferralHelper.StringCleaner(model.QuestionBresponse, false) ?? "";
                    model.QuestionCresponse = StaticReferralHelper.StringCleaner(model.QuestionCresponse, false) ?? "";
                    model.QuestionDresponse = StaticReferralHelper.StringCleaner(model.QuestionDresponse, false) ?? "";
                    model.QuestionEresponse = StaticReferralHelper.StringCleaner(model.QuestionEresponse, false) ?? "";
                    model.QuestionFresponse = StaticReferralHelper.StringCleaner(model.QuestionFresponse, false) ?? "";
                    model.QuestionGresponse = StaticReferralHelper.StringCleaner(model.QuestionGresponse, false) ?? "";
                    model.QuestionHresponse = StaticReferralHelper.StringCleaner(model.QuestionHresponse, false) ?? "";

                    survey.QuestionAnswers.Add(model);

                    
                }
                else
                {

                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionAresponse = StaticReferralHelper.StringCleaner(model.QuestionAresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionBresponse = StaticReferralHelper.StringCleaner(model.QuestionBresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionCresponse = StaticReferralHelper.StringCleaner(model.QuestionCresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionDresponse = StaticReferralHelper.StringCleaner(model.QuestionDresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionEresponse = StaticReferralHelper.StringCleaner(model.QuestionEresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionFresponse = StaticReferralHelper.StringCleaner(model.QuestionFresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionGresponse = StaticReferralHelper.StringCleaner(model.QuestionGresponse, false) ?? "";
                    survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionHresponse = StaticReferralHelper.StringCleaner(model.QuestionHresponse, false) ?? "";
                    if (model.QuestionAresponses != null)
                        survey.QuestionAnswers.First(f => f.QuestionId == q).QuestionAresponses = model.QuestionAresponses;
                }

                //add skipped questions
                if (survey.QuestionnaireType == QuestionnaireType.NotCompleteProT1and2and3
                    || survey.QuestionnaireType == QuestionnaireType.NotCompleteSelfT1and2and3
                    )
                {
                    if (q == 2 && model.QuestionAresponse == "true")
                    {
                        //add empty q3
                        if (!survey.QuestionAnswers.Where(w => w.QuestionId == 3).Any())
                            survey.QuestionAnswers.Add(new QuestionsViewModel() { QuestionAresponse = "", QuestionId = 3 });
                    }
                    if (q == 3)
                    {
                        //add empty q4
                        if (!survey.QuestionAnswers.Where(w => w.QuestionId == 4).Any())
                            survey.QuestionAnswers.Add(new QuestionsViewModel() { QuestionAresponse = "", QuestionId = 4 });
                    }
                }
            }


            return survey;
        }

        public static string ConvertQuestionnaireType(string uiType)
        {

            List<KeyValuePair<string, string>> list = new()
            {
                new KeyValuePair<string, string>("cpl1", "CompleteProT1"),
                new KeyValuePair<string, string>("cpl23", "CompleteProT2and3"),
                new KeyValuePair<string, string>("csrl1", "CompleteSelfT1"),
                new KeyValuePair<string, string>("csrl23", "CompleteSelfT2and3"),
                new KeyValuePair<string, string>("ncp", "NotCompleteProT1and2and3"),
                new KeyValuePair<string, string>("ncsr", "NotCompleteSelfT1and2and3"),
                new KeyValuePair<string, string>("CompleteProT1","cpl1"),
                new KeyValuePair<string, string>("CompleteProT2and3","cpl23"),
                new KeyValuePair<string, string>("CompleteSelfT1","csrl1"),
                new KeyValuePair<string, string>("CompleteSelfT2and3","csrl23"),
                new KeyValuePair<string, string>("NotCompleteProT1and2and3","ncp"),
                new KeyValuePair<string, string>("NotCompleteSelfT1and2and3","ncsr")
            };


            return list.Where(w => w.Key == uiType).FirstOrDefault(new KeyValuePair<string, string>("", "")).Value;
        }

        public static QuestionnaireViewModel FormatforApi(QuestionnaireViewModel questionnaire)
        {
            //make a copy            
            QuestionnaireViewModel apiQuestionnaire = new()
            {
                QuestionnaireType = questionnaire.QuestionnaireType,
                QuestionAnswers = new List<QuestionsViewModel>()
            };

            var answers = questionnaire.QuestionAnswers.ToList();
            foreach (var answer in answers)
                apiQuestionnaire.QuestionAnswers.Add(new QuestionsViewModel()
                {
                    QuestionAresponse = answer.QuestionAresponse,
                    QuestionAresponses = answer.QuestionAresponses != null ? new List<CheckBox>(answer.QuestionAresponses) : null,
                    QuestionBresponse = answer.QuestionBresponse,
                    QuestionCresponse = answer.QuestionCresponse,
                    QuestionDresponse = answer.QuestionDresponse,
                    QuestionEresponse = answer.QuestionEresponse,
                    QuestionFresponse = answer.QuestionFresponse,
                    QuestionGresponse = answer.QuestionGresponse,
                    QuestionHresponse = answer.QuestionHresponse,
                    QuestionId = answer.QuestionId
                });

            foreach (QuestionsViewModel question in apiQuestionnaire.QuestionAnswers)
            {
                if (question.QuestionAresponses != null)
                {
                    //remove any checkbox responses where selected = false
                    question.QuestionAresponses
                        .RemoveAll(r => r.Selected == false);

                    if (question.QuestionAresponses.Where(w => w.Selected && w.Value == "f. Other").Any())
                    {
                        var othertext = question.QuestionAresponses.Where(w => w.Selected && w.Value == "f. Other").First();
                        othertext.Value += " - " + question.QuestionAresponse;
                    }

                    if (question.QuestionAresponses.Where(w => w.Selected && w.Value == "e. Other").Any())
                    {
                        var othertext = question.QuestionAresponses.Where(w => w.Selected && w.Value == "e. Other").First();
                        othertext.Value += " - " + question.QuestionAresponse;
                    }

                    //comma delimit answers into "a"
                    question.QuestionAresponse = string.Join(", ", question.QuestionAresponses.Select(s => s.Value));
                }


                //check telephones
                if ((apiQuestionnaire.QuestionAnswers.Count == 6 && question.QuestionId == 6) || (apiQuestionnaire.QuestionAnswers.Count == 7 && question.QuestionId == 7))
                {
                    //if last question
                    if (question.QuestionAresponse == "true")
                    {
                        if (question.QuestionCresponse.Substring(0, 1) == "0")
                            question.QuestionCresponse = "+44" + question.QuestionCresponse[1..];
                        if (question.QuestionCresponse.Substring(0, 2) == "44")
                            question.QuestionCresponse = "+44" + question.QuestionCresponse[2..];
                        question.QuestionCresponse = question.QuestionCresponse.Replace(" ", "");
                    }
                    else
                    {
                        //null fields
                        question.QuestionBresponse = null;
                        question.QuestionCresponse = null;
                        question.QuestionDresponse = null;
                        question.QuestionEresponse = null;
                    }
                }

            }

            //check user didnt change responses, i.e. go back
            if (apiQuestionnaire.QuestionnaireType == QuestionnaireType.NotCompleteProT1and2and3
                    || apiQuestionnaire.QuestionnaireType == QuestionnaireType.NotCompleteSelfT1and2and3
                    )
            {
                var q2 = apiQuestionnaire.QuestionAnswers.Where(w => w.QuestionId == 2)?.First();
                if (q2 != null)
                {
                    var q3 = apiQuestionnaire.QuestionAnswers.Where(w => w.QuestionId == 3)?.FirstOrDefault();
                    if (q3 != null)
                    {
                        //rule 1. if q2=true, then q3=empty
                        if (q2.QuestionAresponse == "true")
                            q3.QuestionAresponse = "";
                    }
                    var q4 = apiQuestionnaire.QuestionAnswers.Where(w => w.QuestionId == 4)?.FirstOrDefault();
                    if (q4 != null)
                    {
                        //rule 2. if q2=false, then q4=empty
                        if (q2.QuestionAresponse == "false")
                            q4.QuestionAresponse = "";
                    }
                }

            }


            return apiQuestionnaire;
        }



        [Obsolete("No longer used, object is deserialized directly now",true)]
        public static string QuestionnaireJSONOut(QuestionnaireViewModel questionnaire)
        {

            //Add Q1 (same for all Questionnaires)
            JObject jsonOut = new JObject(new JProperty("Q1", (JObject)JToken.FromObject(questionnaire.QuestionAnswers.First(f => f.QuestionId == 1)))
                );

            //Add Q2 + Q3
            if (questionnaire.QuestionnaireRequested == "cpl1" || questionnaire.QuestionnaireRequested == "cpl23"
                || questionnaire.QuestionnaireRequested == "csrl1" || questionnaire.QuestionnaireRequested == "csrl23")
            {
                jsonOut.Add(new JProperty("Q2", (JObject)JToken.FromObject(questionnaire.QuestionAnswers.First(f => f.QuestionId == 2))));
                jsonOut.Add(new JProperty("Q3", (JObject)JToken.FromObject(questionnaire.QuestionAnswers.First(f => f.QuestionId == 3))));
            }
            else
            {
                bool.TryParse(questionnaire.QuestionAnswers.First(f => f.QuestionId == 2).QuestionAresponse, out bool q2);
                jsonOut.Add(new JProperty("Q2", q2));

                //dont do q3 if selected no to Q2
                if (questionnaire.QuestionAnswers.Where(f => f.QuestionId == 3).Any())
                {
                    //add other response to array
                    if (questionnaire.QuestionAnswers.First(f => f.QuestionId == 3).QuestionAresponses.Where(w => w.Selected && w.Value == "e. Other").Any())
                    {
                        var othertext = questionnaire.QuestionAnswers
                            .First(f => f.QuestionId == 3).QuestionAresponse;
                        questionnaire.QuestionAnswers
                            .First(f => f.QuestionId == 3).QuestionAresponses.Add(new CheckBox() { Selected = true, Text = othertext, Value = othertext });
                    }

                    jsonOut.Add(new JProperty("Q3", questionnaire.QuestionAnswers
                        .First(f => f.QuestionId == 3).QuestionAresponses
                        .Where(w => w.Selected)
                        .Select(s => s.Text + ",")
                        ));

                }

            }

            //Add Q4
            if (questionnaire.QuestionnaireRequested == "cpl23"
                || questionnaire.QuestionnaireRequested == "csrl23")
            {
                jsonOut.Add(new JProperty("Q4", (JObject)JToken.FromObject(questionnaire.QuestionAnswers.First(f => f.QuestionId == 4))));

            }
            else if (questionnaire.QuestionnaireRequested == "ncsr"
                || questionnaire.QuestionnaireRequested == "ncp")
            {
                //add other response to array
                if (questionnaire.QuestionAnswers.First(f => f.QuestionId == 4).QuestionAresponses.Where(w => w.Selected && w.Value == "f. Other").Any())
                {
                    var othertext = questionnaire.QuestionAnswers
                        .First(f => f.QuestionId == 4).QuestionAresponse;
                    questionnaire.QuestionAnswers
                        .First(f => f.QuestionId == 4).QuestionAresponses.Add(new CheckBox() { Selected = true, Text = othertext, Value = othertext });
                }

                jsonOut.Add(new JProperty("Q4", questionnaire.QuestionAnswers
                        .First(f => f.QuestionId == 4).QuestionAresponses
                        .Where(w => w.Selected)
                        .Select(s => s.Text + "")
                        ));

            }
            else
            {
                jsonOut.Add(new JProperty("Q4", questionnaire.QuestionAnswers.First(f => f.QuestionId == 4).QuestionAresponse));
            }

            //Add Q5
            jsonOut.Add(new JProperty("Q5", questionnaire.QuestionAnswers.First(f => f.QuestionId == 5).QuestionAresponse));

            //Add Q6
            if (questionnaire.QuestionnaireRequested == "cpl1"
                || questionnaire.QuestionnaireRequested == "csrl1")
            {
                bool.TryParse(questionnaire.QuestionAnswers.First(f => f.QuestionId == 6).QuestionAresponse, out bool q6);
                jsonOut.Add(new JProperty("Q6", new JObject(
                    new JProperty("Consent", q6),
                    new JProperty("Email", q6 ? questionnaire.QuestionAnswers.First(f => f.QuestionId == 6).QuestionBresponse : ""),
                    new JProperty("Telephone", q6 ? questionnaire.QuestionAnswers.First(f => f.QuestionId == 6).QuestionCresponse : "")
                    )));
            }
            else
            {
                jsonOut.Add(new JProperty("Q6", questionnaire.QuestionAnswers.First(f => f.QuestionId == 6).QuestionAresponse));
            }

            //Add Q7
            if (questionnaire.QuestionAnswers.Where(f => f.QuestionId == 7).Any())
            {
                bool.TryParse(questionnaire.QuestionAnswers.First(f => f.QuestionId == 7).QuestionAresponse, out bool q7);
                jsonOut.Add(new JProperty("Q7", new JObject(
                    new JProperty("Consent", q7),
                    new JProperty("Email", q7 ? questionnaire.QuestionAnswers.First(f => f.QuestionId == 7).QuestionBresponse : ""),
                    new JProperty("Telephone", q7 ? questionnaire.QuestionAnswers.First(f => f.QuestionId == 7).QuestionCresponse : "")
                    )));
            }







            return jsonOut.ToString();
        }
    }
}
