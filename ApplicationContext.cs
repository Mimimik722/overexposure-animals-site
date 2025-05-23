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
    public DbSet<Role> Roles { get; set; }
    public DbSet<Requirement> Requirements { get; set; }
    public DbSet<Coordinate> Coordinates { get; set; }
    public DbSet<UserChatHistory> UserChatHistory { get; set; }
    public DbSet<OrdersHistory> OrdersHistory { get; set; }

    private static ApplicationContext instance = new();
    private ApplicationContext()
    {
        try
        {
            Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            instance = new();
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql("server=192.168.0.104;user=root;password=root;database=pets_db;",
            new MySqlServerVersion(new Version(9, 0)));
    }

    public static ApplicationContext GetInstance()
    {
        return instance;
    }
}

public class Town
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Breed
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ICollection<PetModel>? Pets { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class Feedback
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
}

public class OrderType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Requirement
{
    public int Id { get; set; }
    public SitterModel Sitter_ { get; set; } = null!;
    public int Age_from { get; set; }
    public int Age_to { get; set; }
    public float Weight_from { get; set; }
    public float Weight_to { get; set; }
}

public class Coordinate
{
    public int  Id { get; set; }
    public OrderModel Order_ { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
}

public class UserChatHistory
{
    public int Id { get; set; }
    public UserModel Sender_ { get; set; } = null!;
    public UserModel Receiver_ { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime Created_at { get; set; }
}

public class OrdersHistory
{
    public int Id { get; set; }
    public OrderModel Order_ { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
}