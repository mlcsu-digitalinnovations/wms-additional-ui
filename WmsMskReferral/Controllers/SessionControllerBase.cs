using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsMskReferral.Controllers
{
    public class SessionPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseSession();
        }
    }

    [MiddlewareFilter(typeof(SessionPipeline))]
    public class SessionControllerBase : Controller
    {
    }
}
