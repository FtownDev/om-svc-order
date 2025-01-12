using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using om_svc_order.Data;
using om_svc_order.DTO;
using om_svc_order.Models;
using om_svc_order.Services;
using System.Net;

namespace om_svc_order.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ICacheService _cacheService;

        public OrderController(OrderDbContext context, ICacheService cache)
        {
            this._context = context;
            this._cacheService = cache;
        }

        [HttpGet]
        [Route("{orderId}")]
        public async Task<IActionResult> GetOrderById([FromRoute] Guid orderId)
        {
            ActionResult retval;

            var cacheVal = _cacheService.GetData<Models.Order>(key: $"{orderId}");

            if (cacheVal != null)
            {
                retval = this.Ok(cacheVal);
            }
            else
            {
                var order = await this._context.Orders.FirstOrDefaultAsync(f => f.Id == orderId);

                if (order == null)
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
                }
                else
                {
                    _cacheService.SetData($"{orderId}", order, 10);
                    retval = Ok(order);
                }
            }

            return retval;
        }

        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllOrders(int pageSize = 50, int currentNumber = 0)
        {
            ActionResult retval;

            var cacheList = _cacheService.GetData<OrderRetrieveResponse>(key: $"all{pageSize}/{currentNumber}");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
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
                    _cacheService.SetData($"all{pageSize}/{currentNumber}", responseData, 10);

                    retval = Ok(responseData);
                }
            }

            return retval;
        }

        [HttpGet]
        [Route("customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomerId([FromRoute] Guid customerId)
        {
            ActionResult retval;

            var cacheList = _cacheService.GetData<IEnumerable<Models.Order>>(key: $"{customerId}");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
                var orderList = await this._context.Orders.Where(o => o.BilledToCustomerId == customerId)
                    .OrderByDescending(x => x.EventDate)
                    .ThenBy(b => b.Id)
                    .ToListAsync(); ;

                if (!orderList.Any() || orderList.Count == 0)
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
                }
                else
                {
                    _cacheService.SetData($"{customerId}", orderList, 10);
                    retval = Ok(orderList);
                }
            }

            return retval;
        }

        [HttpGet]
        [Route("date")]
        public async Task<IActionResult> GetOrdersByDate([FromQuery] DateTime date)
        {
            ActionResult retval;

            var cacheList = _cacheService.GetData<IEnumerable<Models.Order>>(key: $"date/{date}");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
                var orderList = await this._context.Orders
                    .Where(o => o.EventDate.Date == date.Date)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                if (!orderList.Any() || orderList.Count == 0)
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find orders");
                }
                else
                {
                    _cacheService.SetData($"date/{date}", orderList, 10);
                    retval = Ok(orderList);
                }
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
                var cacheList = _cacheService.GetData<IEnumerable<Models.Order>>(key: $"dates/{startDate}-{endDate}");

                if (cacheList != null)
                {
                    retval = this.Ok(cacheList);
                }
                else
                {
                    var orderList = await this._context.Orders.OrderBy(x => x.Id)
                      .Where(o => o.EventDate.Date > startDate.Date && o.EventDate.Date < endDate.Date)
                      .OrderBy(o => o.CurrentStatus)
                      .ToListAsync();

                    _cacheService.SetData($"dates/{startDate}-{endDate}", orderList, 10);
                    retval = Ok(orderList);
                }
            }

            return retval;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest orderRequest)
        {
            IActionResult retval;

            DateTime date = orderRequest.order.EventDate;

            orderRequest.order.Id = Guid.NewGuid();
            orderRequest.order.EventDate = date;
            orderRequest.order.Created = DateTime.UtcNow;
            orderRequest.order.CurrentStatus = OrderStatus.Confirmed;

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
                    ItemCategory = item.ItemCategory,
                    ItemName = item.ItemName,
                    Price = item.Price
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
                await _cacheService.InvalidateKeys(new List<string> { "all", $"date/{orderRequest.order.EventDate}" });
                retval = Ok(orderRequest.order);
            }

            return retval;
        }

        [HttpPut]
        public async Task<IActionResult> UpadateOrder([FromBody] Models.Order updatedOrder, [FromQuery] Guid userId)
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
                oldOrder.Updated = DateTime.UtcNow;
                this._context.Entry(oldOrder).CurrentValues.SetValues(updatedOrder);
                result = await this._context.SaveChangesWithTracking(userId) > 0;

                if (!result)
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to update order");
                }
                else
                {
                    await this._cacheService.InvalidateKeys(new List<string> { "all", $"date/{updatedOrder.EventDate}", $"{updatedOrder.Id}" });
                    retval = this.Ok(updatedOrder);
                }
            }

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
                DateTime date = orderToDelete.EventDate;
                this._context.Orders.Remove(orderToDelete);

                if(await this._context.SaveChangesAsync() > 0)
                {
                    await this._cacheService.InvalidateKeys(new List<string> { "all", $"date/{date}" });
                    retval = this.Ok();
                }
                else
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to delete order");
                }
            }

            return retval;
        }

        [HttpGet]
        [Route("{orderId}/history")]
        public async Task<IActionResult> GetOrderHistory([FromRoute] Guid orderId)
        {
            IActionResult retval;

            var cacheList = _cacheService.GetData<IEnumerable<OrderHistory>>(key: $"{orderId}/history");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
                var orderHistory = await this._context.OrderHistory
                    .Where(h => h.OrderId == orderId)
                    .OrderByDescending(i => i.ChangedAt.Date)
                    .ToListAsync();

                if(orderHistory.Count > 0)
                {
                    this._cacheService.SetData($"{orderId}/history", orderHistory, 10);
                }

                retval = this.Ok(orderHistory);
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
                                ChangedByUserName = "Bob Smith",
                                ItemId = item.itemId,
                                ChangedDate = DateTime.UtcNow,
                                ChangeType = item.changeType,
                                OldQuantity = existingItem.Qty,
                                NewQuantity = item.qty,
                                OrderId = orderId,
                                ItemCategory = item.itemCategory,
                                ItemName = item.itemName,
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
                            ItemCategory = item.itemCategory,
                            ItemName  = item.itemName,
                            Price = item.price
                        };
                        itemsToAdd.Add(newItem);
                        var addHistory = new OrderItemHistory
                        {
                            Id = Guid.NewGuid(),
                            ChangedByUserId = userId,
                            ChangedByUserName = "Alice Johnson",
                            ItemId = item.itemId,
                            ChangedDate = DateTime.UtcNow,
                            ChangeType = item.changeType,
                            OldQuantity = 0,
                            NewQuantity = item.qty,
                            OrderId = orderId,
                            ItemName= item.itemName,
                            ItemCategory = item.itemCategory,
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
                                ChangedByUserName = "Joe Miller",
                                ItemId = item.itemId,
                                ChangedDate = DateTime.UtcNow,
                                ChangeType = item.changeType,
                                OldQuantity = itemToDelete.Qty,
                                NewQuantity = 0,
                                OrderId = orderId,
                                ItemCategory = item.itemCategory,
                                ItemName = item.itemName,
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
                await this._cacheService.InvalidateKeys(new List<string> { $"{orderId}/items", $"{orderId}/items/history" });
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

            var cacheList = _cacheService.GetData<IEnumerable<OrderItem>>(key: $"{orderId}/items");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            { 
                // verify order exists
                var order = this._context.Orders.FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    retval = this.BadRequest("Order does not exist");
                }
                else
                {
                    var orderItems = await this._context.OrderItems.Where(o => o.OrderId == orderId).ToListAsync();
                    this._cacheService.SetData($"{orderId}/items", orderItems, 10);
                    retval = this.Ok(orderItems);
                }
            }

            return retval;
        }

        [HttpGet]
        [Route("{orderId}/items/history")]
        public async Task<IActionResult> GetOrderItemHistory([FromRoute] Guid orderId)
        {
            IActionResult retval;

            var cacheList = _cacheService.GetData<IEnumerable<OrderItemHistory>>(key: $"{orderId}/items/history");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
                List<OrderItemHistory> itemHistory = new();

                itemHistory = await this._context.OrderItemHistory
                    .Where(h => h.OrderId == orderId)
                    .OrderByDescending(i => i.ChangedDate)
                    .ToListAsync();

                if(itemHistory.Count > 0)
                {
                    this._cacheService.SetData($"{orderId}/items/history", itemHistory, 10);
                }

                retval = this.Ok(itemHistory);
            }

            return retval;
        }

        [HttpGet]
        [Route("eventTypes")]
        public async Task<IActionResult> GetEventTypes()
        {
            ActionResult retval;

            var cacheList = _cacheService.GetData<IEnumerable<EventType>>(key: "eventTypes");

            if (cacheList != null)
            {
                retval = this.Ok(cacheList);
            }
            else
            {
                var eventList = await this._context.EventTypes.OrderBy(e => e.Name).ToListAsync();

                if (!eventList.Any() || eventList.Count == 0)
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to find event types");
                }
                else
                {
                    this._cacheService.SetData("eventTypes", eventList, 10);
                    retval = Ok(eventList);
                }
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
                await this._cacheService.InvalidateKeys(new List<string> { "eventTypes" });
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

                if(await this._context.SaveChangesAsync() > 0)
                {
                    await this._cacheService.InvalidateKeys(new List<string> { "eventTypes" });
                    retval = this.Ok();
                }
                else
                {
                    retval = this.StatusCode((int)HttpStatusCode.InternalServerError, "Unable to delete event type");
                }
            }

            return retval;
        }



    }
}
