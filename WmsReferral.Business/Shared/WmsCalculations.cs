using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WmsReferral.Business.Models;


namespace WmsReferral.Business.Shared
{
    public class WmsCalculations : IWmsCalculations
    {
        private readonly ILogger<WmsCalculations> _logger;

        public WmsCalculations(ILogger<WmsCalculations> logger)
        {
            _logger = logger;
        }

        public HttpStatusCode BmiEligibility(Referral referral)
        {
            var bmi = (referral.WeightKg / referral.HeightCm / referral.HeightCm) * 10000;
            var ethnicGroup = referral.ServiceUserEthnicityGroup;

            if (bmi < (decimal)27.5 && (ethnicGroup != "White" || ethnicGroup != "I do not wish to disclose my ethnicity"))
            {
                //not eligble
                return (HttpStatusCode)412; //too low
            }

            if (bmi < (decimal)30.0 && (ethnicGroup == "White" || ethnicGroup == "I do not wish to disclose my ethnicity"))
            {
                //not eligble
                return (HttpStatusCode)412; //too low
            }

            if (bmi > 90)
            {
                //not eligible
                return (HttpStatusCode)413; //too high
            }
            return (HttpStatusCode)200; //meets criteria
        }
        public decimal? CalcBmi(Referral referral)
        {
            var bmi = (referral.WeightKg / referral.HeightCm / referral.HeightCm) * 10000;
            return bmi;
        }
        public int CalcAge(Referral referral)
        {
            int age;
            if (referral.DateOfBirth == null)
                return 0;

            age = DateTime.Today.Year - referral.DateOfBirth.Value.Year;
            if (age > 0)
            {
                return age -= Convert.ToInt32(DateTime.Today < referral.DateOfBirth.Value.Date.AddYears(age));
            }
            else
            {
                return 0;
            }
        }

        public decimal? ConvertFeetInches(int? feet, decimal? inches)
        {
            try
            {
                return Math.Round((decimal)((decimal)(feet * 30.48) + (inches * (decimal)2.54)), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }

        public HeightImperialViewModel ConvertCm(decimal? cm)
        {
            try
            {
                if (cm != null)
                {
                    var length = cm / (decimal)2.54;
                    var feet = (int)Math.Floor((decimal)(length / 12));
                    var inches = length - 12 * feet;

                    inches = Math.Round((decimal)inches, 2, MidpointRounding.AwayFromZero);
                    if (inches == 12)
                    { //rounded up
                        inches = 0;
                        feet += 1;
                    }

                    HeightImperialViewModel output = new HeightImperialViewModel { HeightIn = inches, HeightFt = feet };
                    return output;
                }
                return new HeightImperialViewModel { HeightIn = null, HeightFt = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }

        public decimal? ConvertStonesPounds(int? stones, decimal? pounds)
        {
            decimal toKg = 0.45359237M;
            try
            {
                return Math.Round((decimal)((((decimal)stones * 14) + pounds) * toKg), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }
        public WeightImperialViewModel ConvertKg(decimal? kg)
        {
            try
            {
                if (kg != null)
                {
                    var length = kg / (decimal)0.45359237;
                    var stones = (int)Math.Floor((decimal)(length / 14));

                    var stonesUnr = (decimal)(length / 14) - stones;
                    var pounds = stonesUnr * 14;

                    pounds = Math.Round((decimal)pounds, 2, MidpointRounding.ToZero);

                    WeightImperialViewModel output = new WeightImperialViewModel { WeightSt = stones, WeightLb = pounds };
                    return output;
                }
                return new WeightImperialViewModel { WeightSt = null, WeightLb = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }

        

    }
}
