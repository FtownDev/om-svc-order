using Microsoft.EntityFrameworkCore;
using om_svc_order.Models;

namespace om_svc_order.Data
{
    public class DbInitializer
    {
        public static void InitDb(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            SeedData(scope.ServiceProvider.GetService<OrderDbContext>());
        }

        private static void SeedData(OrderDbContext context)
        {
            context.Database.Migrate();

            if (context.EventTypes.Any())
            {
                Console.WriteLine("Already have data- skipping seed data");
                return;
            }

            var eventTypes = new List<EventType>
            {
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Birthday",
                },
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Graduation",
                },
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Retirement",
                },
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Wedding",
                },
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Award Ceremony",
                },
                new EventType
                {
                    Id = Guid.NewGuid(),
                    Name = "Corporate Event",
                }
            };

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    BilledToCustomerId = Guid.Parse("cb831f1f-f5c9-4b9d-bd81-207fb33f0e80"),
                    BilledToAddressId = Guid.Parse("92389934-e6f4-4e0e-bf51-76aff0bd3c18"),
                    Created = DateTime.UtcNow,
                    EventTypeId = eventTypes.FirstOrDefault(e => e.Name == "Graduation").Id,
                    EventDate = DateTime.UtcNow.AddDays(1),
                    ShippedToAddressId = Guid.Parse("92389934-e6f4-4e0e-bf51-76aff0bd3c18"),
                    Amount = new decimal(150.32),
                    BalanceDue = new decimal(150.32),
                    TaxRate = new decimal(0.08),
                    DeliveryPickupNotes = "Deliver to side of house",
                    CurrentStatus = OrderStatus.InProgress,
                    PaymentTerms = PaymentTerms.Net30,
                    Deposit = new decimal(0.00),
                    DeliveryWindow = new List<(DateTime Start , DateTime End)>(){
                        new()
                        {
                            Start = DateTime.UtcNow.AddDays(-2),
                            End = DateTime.UtcNow,
                        },
                        new()
                        {
                            Start = DateTime.UtcNow.AddDays(-5),
                            End = DateTime.UtcNow.AddDays(-4),
                        },
                    },
                }
            };

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    Id= Guid.NewGuid(),
                    OrderId = orders.FirstOrDefault().Id,
                    ItemId = new Guid("4c6fda94-34de-4fc8-889d-49c1b8d778c6"),
                    Qty = 1
                },
                new OrderItem
                {
                    Id= Guid.NewGuid(),
                    OrderId = orders.FirstOrDefault().Id,
                    ItemId = new Guid("7081f119-c671-4898-920e-7536248c0753"),
                    Qty = 7
                },
                new OrderItem
                {
                    Id= Guid.NewGuid(),
                    OrderId = orders.FirstOrDefault().Id,
                    ItemId = new Guid("c7e739f3-9f8d-4d20-992d-8e2c6638e947"),
                    Qty = 30
                },
                new OrderItem
                {
                    Id= Guid.NewGuid(),
                    OrderId = orders.FirstOrDefault().Id,
                    ItemId = new Guid("4415f839-31ce-4ecc-b747-d97ba8bd30f9"),
                    Qty = 45
                }
            };

            context.AddRange(eventTypes);
            context.SaveChanges();

            context.AddRange(orders);
            context.SaveChanges();

            context.AddRange(orderItems);
            context.SaveChanges();
        }
    }
}

