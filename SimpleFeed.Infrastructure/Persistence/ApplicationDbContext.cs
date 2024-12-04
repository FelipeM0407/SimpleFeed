using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleFeed.Domain.Entities;

namespace SimpleFeed.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuração adicional para o Identity, se necessário
            builder.Entity<ApplicationUser>().Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            builder.Entity<ApplicationUser>().Property(u => u.LastName).HasMaxLength(100).IsRequired();
        }
    }
}