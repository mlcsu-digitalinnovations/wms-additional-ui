using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business;
using WmsReferral.Business.Models;
using WmsReferral.Business.Shared;
using Xunit;

namespace WmsReferral.Tests.Business.Shared
{
    public class WmsCalculationsTests
    {
        private readonly WmsCalculations _classUnderTest;
        private readonly Mock<ILogger<WmsCalculations>> _loggerMock;

        public WmsCalculationsTests()
        {
            _loggerMock = new Mock<ILogger<WmsCalculations>>();


            _classUnderTest = new WmsCalculations(_loggerMock.Object);
        }

        [Fact]
        public void CalcAge_Eligible()
        {
            //Arrange               
            Referral referral = new()
            {
               DateOfBirth = new DateTime(DateTime.Now.Year-20,01,01)
            };

            //Act
            var result = _classUnderTest.CalcAge(referral);

            //Assert
            Assert.NotEqual(0, result);
        }

        [Fact]
        public void BmiEligibility_NotEligibleWhite() 
        {
            //Arrange               
            Referral referral = new()
            {               
                HeightCm = 155,
                WeightKg = 70,                
                ServiceUserEthnicityGroup = "White"
            };

            //Act
            var result = _classUnderTest.BmiEligibility(referral);

            //Assert
            Assert.Equal(HttpStatusCode.PreconditionFailed, result);
        }
        [Fact]
        public void BmiEligibility_NotEligibleBAME()
        {
            //Arrange               
            Referral referral = new()
            {
                HeightCm = 155,
                WeightKg = 66,
                ServiceUserEthnicityGroup = ""
            };

            //Act
            var result = _classUnderTest.BmiEligibility(referral);

            //Assert
            Assert.Equal(HttpStatusCode.PreconditionFailed, result);
        }
        [Fact]
        public void BmiEligibility_NotEligibleTooHigh()
        {
            //Arrange               
            Referral referral = new()
            {
                HeightCm = 155,
                WeightKg = 300,
                ServiceUserEthnicityGroup = ""
            };

            //Act
            var result = _classUnderTest.BmiEligibility(referral);

            //Assert
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, result);
        }
        [Fact]
        public void BmiEligibility_Eligible()
        {
            //Arrange               
            Referral referral = new()
            {
                HeightCm = 120,
                WeightKg = 100,
                ServiceUserEthnicityGroup = ""
            };

            //Act
            var result = _classUnderTest.BmiEligibility(referral);

            //Assert
            Assert.Equal(HttpStatusCode.OK, result);
        }

        [Fact]
        public void ConvertFeetInches_NotNull()
        {
            //Arrange               
            int? feet = 6;
            decimal? inches = 4.0M;

            //Act
            var result = _classUnderTest.ConvertFeetInches(feet,inches);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ConvertStonesPounds_NotNull()
        {
            //Arrange               
            int? stones = 9;
            decimal? pounds = 10.0M;

            //Act
            var result = _classUnderTest.ConvertStonesPounds(stones, pounds);

            //Assert
            Assert.NotNull(result);
        }
        [Fact]
        public void ConvertKg_NotNull()
        {
            //Arrange                           
            decimal? kg = 90.0M;

            //Act
            var result = _classUnderTest.ConvertKg(kg);

            //Assert
            Assert.NotNull(result);
        }
        [Fact]
        public void ConvertCm_NotNull()
        {
            //Arrange                           
            decimal? cm = 150.0M;

            //Act
            var result = _classUnderTest.ConvertCm(cm);

            //Assert
            Assert.NotNull(result);
        }
    }
}
