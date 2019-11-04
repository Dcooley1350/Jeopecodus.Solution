using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jeopicodus.Models
{
    public class JeopicodusContext : IdentityDbContext<ApplicationUser>
    {
        public JeopicodusContext(DbContextOptions options) : base(options) { }
    }
}