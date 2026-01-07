using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Devmobx.Todo.Storage
{

    public class DataBaseContext(DbContextOptions<DataBaseContext> options) : DbContext(options)
    {
        public DbSet<Models.v1.TodoItem> TodoItemV1 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Models.v1.TodoItem.Build(modelBuilder.Entity<Models.v1.TodoItem>());
        }
    }
}
