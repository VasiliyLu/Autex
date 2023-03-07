using Autex.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Autex.Backend.DAL;

public class AutexContext : DbContext
{
    private readonly IConfiguration _configuration;
    public DbSet<ChannelItem> Channels { get; set; } = null!;

    public AutexContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_configuration.GetConnectionString("Autex"));
    }
}