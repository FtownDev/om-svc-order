using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using om_svc_order.Data;
using om_svc_order.DTO;
using om_svc_order.Models;
using System.Net;

namespace om_svc_order.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public OrderController(OrderDbContext context)
        {
            this._context = context;
        }

        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllOrders(int pageSize = 50, int currentNumber = 0)
        {
            ActionResult retval;

            var orderList = await this._context.Orders.OrderBy(x => x.EventDate)
           .ThenBy(b => b.Id)
           .Skip(currentNumber)
           .Take(pageSize)
           .ToListAsync();


            if (!orderList.Any() || orderList.Count == 0)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
            }
            else
            {
                var responseData = new OrderRetrieveResponse
                {
                    pageSize = pageSize,
                    totalCount = currentNumber + pageSize,
                    orders = orderList
                };

                retval = Ok(responseData);
            }

            return retval;
        }

        [HttpGet]
        [Route("customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomerId([FromRoute] Guid customerId)
        {
            ActionResult retval;

            var orderList = await this._context.Orders.Where(o => o.BilledToCustomerId == customerId)
           .ToListAsync();

            if (!orderList.Any() || orderList.Count == 0)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
            }
            else
            {
                retval = Ok(orderList.OrderByDescending(x => x.EventDate).ThenBy(b => b.Id));
            }

            return retval;
        }

        [HttpGet]
        [Route("date")]
        public async Task<IActionResult> GetOrdersByDate([FromQuery] DateTime date)
        {
            ActionResult retval;

            var orderList = await this._context.Orders.OrderBy(x => x.Id)
           .Where(o => o.EventDate.Date == date.Date)
           .ToListAsync();

            if (!orderList.Any() || orderList.Count == 0)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
            }
            else
            {
                retval = Ok(orderList);
            }

            return retval;
        }

        [HttpGet]
        [Route("dates")]
        public async Task<IActionResult> GetOrdersByDateRange([FromQuery] DateTime startDate, DateTime endDate)
        {
            ActionResult retval;

            if (endDate < startDate)
            {
                retval = BadRequest("Start date must come before end date");
            }
            else
            {
                var orderList = await this._context.Orders.OrderBy(x => x.Id)
                  .Where(o => o.EventDate.Date > startDate.Date && o.EventDate.Date < endDate.Date)
                  .ToListAsync();

                retval = Ok(orderList.OrderBy(o => o.CurrentStatus));

            }

            return retval;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest orderRequest)
        {
            IActionResult retval;

            DateTime date = (DateTime)orderRequest.order.EventDate;

            orderRequest.order.Id = Guid.NewGuid();
            orderRequest.order.EventDate = date;
            orderRequest.order.Created = DateTime.UtcNow;
            //orderRequest.order.CurrentStatus = OrderStatus.Confirmed;


            this._context.Orders.Add(orderRequest.order);
            List<OrderItem> orderItems = new List<OrderItem>();
            foreach (var item in orderRequest.orderItems)
            {
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderRequest.order.Id,
                    ItemId = item.ItemId,
                    Qty = item.Qty,
                });
            }

            await this._context.OrderItems.AddRangeAsync(orderItems);

            var result = await this._context.SaveChangesAsync() > 0;
            if (!result)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to create order");
            }
            else
            {
                retval = Ok(orderRequest.order);
            }

            return retval;
        }

        [HttpPut]
        public async Task<IActionResult> UpadateOrder([FromBody] Order updatedOrder, [FromQuery] Guid userId)
        {
            IActionResult retval;
            bool result = false;

            var oldOrder = await this._context.Orders.FirstOrDefaultAsync(o => o.Id == updatedOrder.Id);
            if (oldOrder == null)
            {
                retval = this.BadRequest("Order Id does not exist");
            }
            else
            {
                // Update Order will utilize the override TrackChanges method written in DbContext file
                // This is because we want to track Order history changes, but dont want to manage the table and columns, etc
                // hopefully it "just works"
                this._context.Entry(oldOrder).CurrentValues.SetValues(updatedOrder);
                result = await this._context.SaveChangesWithTracking(userId) > 0;
            }

            retval = result ? this.Ok(updatedOrder) : this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to update order");

            return retval;
        }

        [HttpDelete]
        [Route("{orderId}")]
        public async Task<IActionResult> DeleteOrderById([FromRoute] Guid orderId)
        {
            IActionResult retval;

            var orderToDelete = await this._context.Orders.FindAsync(orderId);

            if (orderToDelete == null)
            {
                retval = this.NotFound();
            }
            else
            {
                this._context.Orders.Remove(orderToDelete);

                retval = await this._context.SaveChangesAsync() > 0 ? this.Ok() : this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to delete order");
            }

            return retval;
        }

        [HttpPut]
        [Route("{orderId}/items")]
        public async Task<IActionResult> UpdateOrderItems([FromRoute] Guid orderId, [FromQuery] Guid userId, [FromBody] List<OrderItemUpdateRequest> items)
        {
            IActionResult retval;
            
            var existingItems = await this._context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();

            var itemsToAdd = new List<OrderItem>();
            var itemHistory = new List<OrderItemHistory>();
            var itemsToDelete = new List<OrderItem>();

            foreach (var item in items)
            {
                switch (item.changeType)
                {
                    case OrderItemChangeType.Update:

                        var existingItem = existingItems.FirstOrDefault(e => e.ItemId == item.itemId);
                        if (existingItem != null)
                        {
                            var updateHistory = new OrderItemHistory
                            {
                                Id = Guid.NewGuid(),
                                ChangedByUserId = userId,
                                ItemId = item.itemId,
                                ChangedDate = DateTime.UtcNow,
                                ChangeType = item.changeType,
                                OldQuantity = existingItem.Qty,
                                NewQuantity = item.qty,
                                OrderId = orderId,
                            };

                            existingItem.Qty = item.qty;
                            itemHistory.Add(updateHistory);
                        }
                        else
                        {
                            retval = this.StatusCode((int)HttpStatusCode.InternalServerError, $"Unable to find inventory item {item.itemId} in existing order items");
                        }
                        break;
                    
                    case OrderItemChangeType.Add:
                        var newItem = new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            ItemId = item.itemId,
                            OrderId = orderId,
                            Qty = item.qty,
                        };
                        itemsToAdd.Add(newItem);
                        var addHistory = new OrderItemHistory
                        {
                            Id = Guid.NewGuid(),
                            ChangedByUserId = userId,
                            ItemId = item.itemId,
                            ChangedDate = DateTime.UtcNow,
                            ChangeType = item.changeType,
                            OldQuantity = 0,
                            NewQuantity = item.qty,
                            OrderId = orderId,
                        };

                        itemHistory.Add(addHistory);
                        break;
                    
                    case OrderItemChangeType.Delete:
                        var itemToDelete = existingItems.FirstOrDefault(e => e.ItemId == item.itemId);
                        if(itemToDelete != null)
                        {
                            itemsToDelete.Add(itemToDelete);

                            var deleteHistory = new OrderItemHistory
                            {
                                Id = Guid.NewGuid(),
                                ChangedByUserId = userId,
                                ItemId = item.itemId,
                                ChangedDate = DateTime.UtcNow,
                                ChangeType = item.changeType,
                                OldQuantity = itemToDelete.Qty,
                                NewQuantity = 0,
                                OrderId = orderId,
                            };

                            itemHistory.Add(deleteHistory);
                        }
                        else
                        {
                            retval = this.StatusCode((int)HttpStatusCode.InternalServerError, $"Unable to find inventory item {item.itemId} in existing order items");
                        }

                        break;
                    
                    default:
                        Console.WriteLine("Hit default case in Update Item switch");
                        continue;
                }
            }

            if (itemsToAdd.Any())
            {
                await _context.OrderItems.AddRangeAsync(itemsToAdd);
            }
            if (itemsToDelete.Any())
            {
                _context.OrderItems.RemoveRange(itemsToDelete);
            }
            if (itemHistory.Any())
            {
                await this._context.OrderItemHistory.AddRangeAsync(itemHistory);
            }

            var result = await this._context.SaveChangesAsync() > 0;

            if (result)
            {
                retval = this.Ok();
            }
            else
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, $"Unable to save order item cahnges. Order Id {orderId}");
            }

            return retval;

        }

        [HttpGet]
        [Route("{orderId}/items")]
        public async Task<IActionResult> GetOrderItemsByOrderId([FromRoute] Guid orderId)
        {
            IActionResult retval;

            // verify order exists
            var order = this._context.Orders.FirstOrDefault(o => o.Id == orderId);

            if(order == null) 
            {
                retval = this.BadRequest("Order does not exist");
            }
            else
            {
                var orderItems = await this._context.OrderItems.Where(o => o.OrderId == orderId).ToListAsync();
                retval = this.Ok(orderItems);
            }

            return retval;
        }

        [HttpGet]
        [Route("eventTypes")]
        public async Task<IActionResult> GetEventTypes()
        {
            ActionResult retval;

            var eventList = await this._context.EventTypes.OrderBy(e => e.Name).ToListAsync();

            if (!eventList.Any() || eventList.Count == 0)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find event types");
            }
            else
            {
                retval = Ok(eventList);
            }

            return retval;
        }

        [HttpPost]
        [Route("eventTypes")]
        public async Task<IActionResult> CreateEventType(EventType eventType)
        {
            ActionResult retval;

            eventType.Id = Guid.NewGuid();

            this._context.EventTypes.Add(eventType);

            var result = await this._context.SaveChangesAsync() > 0;

            if (!result)
            {
                retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to create event type");
            }
            else
            {
                retval = Ok(eventType);
            }

            return retval;
        }

        [HttpDelete]
        [Route("eventTypes/{eventType}")]
        public async Task<IActionResult> DeleteEventType([FromRoute] Guid eventTypeId)
        {
            IActionResult retval;

            var type = await this._context.EventTypes.FindAsync(eventTypeId);

            if (type == null)
            {
                retval = this.NotFound();
            }
            else
            {
                this._context.EventTypes.Remove(type);

                retval = await this._context.SaveChangesAsync() > 0 ? this.Ok() : this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to delete event type");
            }

            return retval;
        }



    }
}
