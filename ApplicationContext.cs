using Microsoft.EntityFrameworkCore;
using Project_site.Models;

public class ApplicationContext : DbContext
{
    public DbSet<UserModel> Users { get; set; } = null!;
    public DbSet<SitterModel> Sitters { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<OrderType> Order_types { get; set; }
    public DbSet<OrderModel> Orders { get; set; }
    public DbSet<Town> Towns { get; set; }
    public DbSet<PetModel> Pets { get; set; } = null!;
    public DbSet<Breed> Breeds { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Requirement> Requirements { get; set; }

    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql("server=192.168.0.104;user=root;password=root;database=pets_db;",
            new MySqlServerVersion(new Version(9, 0)));
    }
}

public class Town
{
    public int id { get; set; }
    public string? name { get; set; }
}

public class Breed
{
    public int id { get; set; }
    public string? name { get; set; }
    public ICollection<PetModel>? pets { get; set; }
}

public class Admin
{
    public int id { get; set; }
    public UserModel user_ { get; set; } = null!;
}

public class Feedback
{
    public int id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
}

public class OrderType
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Requirement
{
    public int id { get; set; }
    public SitterModel Sitter_ { get; set; } = null!;
    public int age_from { get; set; }
    public int age_to { get; set; }
    public float weight_from { get; set; }
    public float weight_to { get; set; }
}