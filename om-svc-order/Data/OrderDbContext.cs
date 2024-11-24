using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using om_svc_order.Models;

namespace om_svc_order.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions options) : base(options) { }

        public DbSet<EventType> EventTypes { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<OrderItemHistory> OrderItemHistory { get; set; }

        public DbSet<OrderHistory> OrderHistory {  get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the value converter for the Animal
            modelBuilder.Entity<Order>()
                .Property(x => x.DeliveryWindow)
                .HasConversion(new ValueConverter<List<TimeSpan>, string>(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<TimeSpan>>(v))); 

            modelBuilder.Entity<Order>()
               .Property(x => x.PickupWindow)
               .HasConversion(new ValueConverter<List<TimeSpan>, string>(
                   v => JsonConvert.SerializeObject(v), 
                   v => JsonConvert.DeserializeObject<List<TimeSpan>>(v))); 
        }

        public async Task<int> SaveChangesWithTracking(Guid user)
        {
            TrackOrderChanges<Order>(user);
            return await base.SaveChangesAsync();
        }

        private void TrackOrderChanges<TEntity>(Guid userId) where TEntity : class   
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Order>())
            {
                if (entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties)
                    {
                        if (property.IsModified)
                        {
                            var history = new OrderHistory
                            {
                                Id = entry.Entity.Id,
                                
                                PropertyName = property.Metadata.Name,
                                ChangedFrom = property.OriginalValue?.ToString(),
                                ChangedTo = property.CurrentValue?.ToString(),
                                ChangedAt = now,
                                ChangedByUserId = userId 
                            };

                            this.Add(history); 
                        }
                    }
                }
            }


        }
    }
}
