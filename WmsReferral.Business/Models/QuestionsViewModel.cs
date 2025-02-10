using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
//using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{

    public class QuestionsViewModel
    {
        //[JsonIgnore]
        public int QuestionId { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("a", NullValueHandling = NullValueHandling.Ignore)]
        public string QuestionAresponse { get; set; } = "";
        [JsonIgnore]        
        public List<CheckBox> QuestionAresponses { get; set; }
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("b", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionBresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("c", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionCresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("d", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionDresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("e", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionEresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("f", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionFresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("g", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionGresponse { get; set; } = "";
        [Required(ErrorMessage = "<span class=\"nhsuk-u-visually-hidden\">error: </span>A response is required")]
        [DefaultValue("")]
        [JsonProperty("h", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string QuestionHresponse { get; set; } = "";
        [JsonIgnore]
        public IEnumerable<KeyValuePair<string, string>> Responses { get; set; }
        [JsonIgnore]
        public string ProviderName { get; set; } = "";
        [JsonIgnore]
        public string GoBack { get; set; } = "Q2";
    }

    public class CheckBox
    {
        [JsonIgnore]
        public string Text { get; set; } = "";
        public string Value { get; set; } = "";
        [JsonIgnore]
        public bool Selected { get; set; } = false;

    }

    
}
