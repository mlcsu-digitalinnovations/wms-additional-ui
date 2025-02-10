using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WmsStaffReferral.Data
{
    public class DatabaseContext : DbContext, IDataProtectionKeyContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
           : base(options) { }

        // This maps to the table that stores keys.
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    }
}
