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
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Client> Clients { get; set; } // Adicionar o DbSet para Clients

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Mapeamento explícito para a tabela "clients"
            builder.Entity<Client>(entity =>
            {
                entity.ToTable("clients"); // Nome da tabela no banco
                entity.HasOne(c => c.User)
                      .WithOne() // Relacionamento 1:1 com IdentityUser
                      .HasForeignKey<Client>(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Excluir Client ao excluir User
            });
        }

    }
}