using Microsoft.EntityFrameworkCore;

public class ApplicationContext : DbContext
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<Sitter> Sitters { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Town> Towns { get; set; }

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

public class Client
{
    public int? id { get; set; }
    public int? town_id { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? sex { get; set; }
    public DateOnly? birthday { get; set; }
    public string? phone { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }

    public ICollection<Order>? orders { get; set; }
}

public class Sitter
{
    public int? id { get; set; }
    public int? town_id { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? sex { get; set; }
    public float? payment { get; set; }
    public int? expirience { get; set; }
    public bool? is_verified { get; set; }
    public string? password { get; set; }
    
    public ICollection<Order>? orders { get; set; }
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

    public ICollection<Client>? clients { get; set; }
    public ICollection<Order>? sitters { get; set; }
}