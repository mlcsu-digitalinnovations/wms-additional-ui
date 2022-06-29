using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WmsReferral.Business;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Models;
using WmsReferral.Business.Shared;
using Xunit;

namespace WmsReferral.Tests.Business.Helpers
{
    public class StaticReferralHelperTests
    {
        [Fact]
        public void ValidateNHSNumber()
        {
            //Arrange               
           

            //Act
            var badnumber1 = StaticReferralHelper.ValidateNHSNumber("1234567890");
            var badnumber2 = StaticReferralHelper.ValidateNHSNumber("4289432928");
            var badnumber3 = StaticReferralHelper.ValidateNHSNumber("1er23d2785");
            var correct1 = StaticReferralHelper.ValidateNHSNumber("9876543210");


            //Assert
            Assert.True(!badnumber1);
            Assert.True(!badnumber2);
            Assert.True(!badnumber3);
            Assert.True(correct1);
        }
    }
}
