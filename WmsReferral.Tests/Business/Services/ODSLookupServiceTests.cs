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
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using Xunit;


namespace WmsReferral.Tests.Business.Services
{
    public class ODSLookupServiceTests
    {
        private readonly Mock<IODSLookupService> _wmsODSLookupServiceMock;

        public ODSLookupServiceTests()
        {
            _wmsODSLookupServiceMock = new Mock<IODSLookupService>();
        }


        [Fact]
        public void LookupODSCodeAsync() 
        {
            //Arrange 
            _wmsODSLookupServiceMock.Setup(x => x.LookupODSCodeAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new ODSOrganisation() { APIStatusCode = 200 }));

            //Act
            var _classundertest = _wmsODSLookupServiceMock.Object.LookupODSCodeAsync("");



            //Assert
            Assert.True(_classundertest.Result.APIStatusCode == 200);
            
        }

    }
}
