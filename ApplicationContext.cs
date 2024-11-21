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

public class Order
{
    public int? id { get; set; }
    public int? sitter_id { get;set; }
    public int? client_id { get; set; }
    public int? feedback_id { get;set; }
    public int? order_type_id { get; set;}
    public DateOnly? date_start { get; set; }
    public DateOnly? is_verified { get;set; }
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
    public decimal Rating { get; set; }
    public string Comment { get; set; }
}

public class OrderType
{
    public int Id { get; set; }
    public string Name { get; set; }
}