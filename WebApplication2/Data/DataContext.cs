using Imgriff.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace Imgriff.Data
{
    public class DataContext : DbContext
    {
        public DbSet<UserTeamspaces> UserTeamspaces { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Teamspace> Teamspaces { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
