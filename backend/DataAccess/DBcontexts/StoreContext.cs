
using Backend.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.DataAccess.DBcontexts
{
    public partial class StoreContext : DbContext
    {
        public StoreContext(DbContextOptions<StoreContext> options) : base(options)
        {
        }
        public virtual DbSet<Items> Items { get; set; }
        public virtual DbSet<ReadItems> ReadItems { get; set; }
    }
}