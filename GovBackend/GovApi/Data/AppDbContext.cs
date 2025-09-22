using Microsoft.EntityFrameworkCore;
using GovApi.Models;

namespace GovApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Tables
    public DbSet<User> Users { get; set; }
}
