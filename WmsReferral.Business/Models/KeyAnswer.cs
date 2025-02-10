using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsReferral.Business.Models
{
    public class KeyAnswer
    {
        public bool AnsweredNhsNumberGPConsent { get; set; }
        public bool AnsweredUpdateReferrerCompletionConsent { get; set; }
        public bool AnsweredPhysicalDisability { get; set; }
        public bool AnsweredLearningDisability { get; set; }
        public bool AnsweredHypertension { get; set; }
        public bool AnsweredDiabetesType1 { get; set; }
        public bool AnsweredDiabetesType2 { get; set; }
        public bool ReferralSubmitted { get; set; }
        public bool ProviderChoiceSubmitted { get; set; }
        public bool AnsweredPatientVulnerable { get; set; }
        public bool AnsweredBariatricSurgery { get; set; }
        public bool AnsweredEatingDisorder { get; set; }
        public bool AnsweredPregnant { get; set; }
        public bool AnsweredGivenBirth { get; set; }
        public bool AnsweredBreastFeeding { get; set; }
        public bool AnsweredCaesareanSection { get; set; }
        public bool AnsweredArthritisHip { get; set; }
        public bool AnsweredArthritisKnee { get; set; }
        public bool AnsweredConsentForFurtureContact { get; set; }
        public WeightImperialViewModel WeightImperial { get; set; }
        public HeightImperialViewModel HeightImperial { get; set; }
        public string Identity_proofing_level { get; set; }
        public bool? QueriedReferral { get; set; }
    }
}
