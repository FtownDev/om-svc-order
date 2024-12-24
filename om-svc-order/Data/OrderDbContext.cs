using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using om_svc_order.Models;
using System.Text.Json;

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
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.DeliveryWindow)
                    .IsRequired(false)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<List<DateTime>>>(v) ?? new List<List<DateTime>>()
                    )
                    .HasColumnType("json");

                entity.Property(e => e.PickupWindow)
                    .IsRequired(false)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<List<DateTime>>>(v) ?? new List<List<DateTime>>()
                    )
                    .HasColumnType("json");
            });
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
                                Id = Guid.NewGuid(),
                                OrderId = entry.Entity.Id,
                                PropertyName = property.Metadata.Name,
                                ChangedFrom = property.OriginalValue?.ToString(),
                                ChangedTo = property.CurrentValue?.ToString(),
                                ChangedAt = now,
                                ChangedByUserId = userId
                            };
                            
                            if (property.Metadata.ClrType == typeof(List<List<DateTime>>))
                            {
                                var originalNestedList = property.OriginalValue as List<List<DateTime>>; 
                                var currentNestedList = property.CurrentValue as List<List<DateTime>>;

                                if (originalNestedList != null && currentNestedList != null && !NestedListSequenceEqual(originalNestedList, currentNestedList))
                                {
                                    history = new OrderHistory
                                    {
                                        Id = Guid.NewGuid(),
                                        OrderId = entry.Entity.Id,
                                        PropertyName = property.Metadata.Name,
                                        ChangedFrom = string.Join("; ", originalNestedList.Select(l => string.Join(", ", l.Select(d => d.ToString("o"))))),
                                        ChangedTo = string.Join("; ", currentNestedList.Select(l => string.Join(", ", l.Select(d => d.ToString("o"))))),
                                        ChangedAt = now,
                                        ChangedByUserId = userId
                                    };
                                }
                                else continue;
                            }


                            this.Add(history); 
                        }
                    }
                }
            }


        }

        private static bool NestedListSequenceEqual(List<List<DateTime>> oldNestedList, List<List<DateTime>> newNestedList) 
        { 
            if (oldNestedList.Count != newNestedList.Count) return false; 
            
            for (int i = 0; i < oldNestedList.Count; i++) 
            { 
                if (!oldNestedList[i].SequenceEqual(newNestedList[i])) return false; 
            } 
            return true; 
        }
    }
}
