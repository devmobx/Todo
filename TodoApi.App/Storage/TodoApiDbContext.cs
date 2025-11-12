using Microsoft.EntityFrameworkCore;

namespace TodoApi.App.Storage
{
    public class TodoApiDbContext(DbContextOptions<TodoApiDbContext> options) : DbContext(options)
    {
        public DbSet<Models.v1.TodoItem> TodoItemV1 { get; set; }
        public DbSet<Models.v1.Reminder> ReminderV1 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Models.v1.TodoItem.Build(modelBuilder.Entity<Models.v1.TodoItem>());
            Models.v1.Reminder.Build(modelBuilder.Entity<Models.v1.Reminder>());
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
                Console.WriteLine($"❌ Error creating Cosmos DB resources: {ex.Message}");
                throw;
            }
        }
    }

}
