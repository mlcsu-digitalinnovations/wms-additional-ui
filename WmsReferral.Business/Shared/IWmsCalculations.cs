using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Shared
{
    public interface IWmsCalculations
    {
        public HttpStatusCode BmiEligibility(Referral referral);
        public decimal? CalcBmi(Referral referral);
        public int CalcAge(Referral referral);
        public decimal? ConvertFeetInches(int? feet, decimal? inches);
        public HeightImperialViewModel ConvertCm(decimal? cm);
        public decimal? ConvertStonesPounds(int? stones, decimal? pounds);
        public WeightImperialViewModel ConvertKg(decimal? kg);
       
    }
}
