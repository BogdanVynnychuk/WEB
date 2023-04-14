using Microsoft.EntityFrameworkCore;

internal class ApplicationDbContext : DbContext
{
    private readonly IConfiguration configuration;

    public ApplicationDbContext(DbContextOptions options, IConfiguration configuration) : base(options)
    {
        this.configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(configuration.GetConnectionString("Default"));

    public DbSet<Student> Students { get; set; }

}