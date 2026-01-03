using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Devmobx.Todo.Storage
{
    public class DataBaseContext(DbContextOptions<DataBaseContext> options) : DbContext(options)
    {
        public DbSet<Models.v1.TodoItem> TodoItemV1 { get; set; }
        public DbSet<Models.v1.User> UserV1 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Models.v1.TodoItem.Build(modelBuilder.Entity<Models.v1.TodoItem>());
            Models.v1.User.Build(modelBuilder.Entity<Models.v1.User>());
        }

        public async Task EnsureContainersCreatedAsync()
        {
            try
            {
                await Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Cosmos DB database and containers created/verified successfully");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("total throughput"))
                {
                    Console.WriteLine("⚠️  Throughput limit reached - container likely already exists");
                }
                else
                {
                    Console.WriteLine($"❌ Error creating Cosmos DB resources: {ex.Message}");
                    if (!ex.Message.Contains("1028"))
                    {
                        throw;
                    }
                }
            }
        }
    }
}
