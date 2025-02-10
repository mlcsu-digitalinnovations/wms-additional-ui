using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business.Helpers;

namespace WmsReferral.Business.Models
{
    public class Referral
    {
        [Required]
        public DateTime DateOfReferral { get; set; }
        
        [Display(Name = "Family name")]
        [Required, MinLength(1), MaxLength(200)]
        public string FamilyName { get; set; }
        [Display(Name = "Given name")]
        [Required, MinLength(1), MaxLength(200)]
        public string GivenName { get; set; }
        [Display(Name = "Address")]
        [Required, MinLength(1), MaxLength(200)]
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        [Required, MinLength(1), MaxLength(200)]
        public string Postcode { get; set; }
        [RegularExpression(Constants.REGEX_PHONE_PLUS_NUMLENGTH)]
        public string Telephone { get; set; }
        [RegularExpression(Constants.REGEX_PHONE_PLUS_NUMLENGTH)]
        public string Mobile { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Display(Name = "Date of birth")]
        [Required]
        [Range(typeof(DateTimeOffset),
          Constants.MINIMUM_DATE_OF_BIRTH,
          Constants.MAXIMUM_REQUEST_DATE)]
        public DateTimeOffset? DateOfBirth { get; set; }
        [Required]
        public string Sex { get; set; }
        [Display(Name = "Are you happy to take part in future NHS England surveys?")]
        [Required]
        public bool? ConsentForFutureContactForEvaluation { get; set; }
        [Required, MinLength(1), MaxLength(200)]
        [Display(Name = "Ethnic group")]
        public string Ethnicity { get; set; }
        public string ServiceUserEthnicity { get; set; }
        public string ServiceUserEthnicityGroup { get; set; }
        [Display(Name = "Do you have any physical conditions lasting or expected to last 12 months or more ? ")]
        public bool? HasAPhysicalDisability { get; set; }
        [Display(Name = "Do you have a learning disability?")]
        public bool? HasALearningDisability { get; set; }
        [Display(Name = "Do you have a diagnosis of high blood pressure and/or take regular medication for this?")]
        public bool? HasHypertension { get; set; }
        [Display(Name = "Do you have a diagnosis of type 1 diabetes and/or take regular medication for this?")]
        public bool? HasDiabetesType1 { get; set; }
        [Display(Name = "Do you have a diagnosis of type 2 diabetes and/or take regular medication for this?")]
        public bool? HasDiabetesType2 { get; set; }
        [Range(100, 250)]
        [Display(Name = "Height (cm)")]
        public decimal? HeightCm { get; set; }
        public decimal? HeightFeet { get; set; }
        public decimal? HeightInches { get; set; }
        public string HeightUnits { get; set; }
        [Range(35, 300)]
        [Display(Name = "Weight (kg)")]
        public decimal? WeightKg { get; set; }
        public decimal? WeightStones { get; set; }
        public decimal? WeightPounds { get; set; }
        public string WeightUnits { get; set; }
        [Display(Name = "When was this weight measurement taken?")]
        [Required]
        [Range(typeof(DateTimeOffset),
          Constants.MINIMUM_REQUEST_DATE,
          Constants.MAXIMUM_REQUEST_DATE)]
        public DateTimeOffset? DateOfBmiAtRegistration { get; set; }
        
    }
}
