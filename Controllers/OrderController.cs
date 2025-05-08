using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrax.Models;
using LogiTrax.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace LogiTrax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly LogiTraxContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IMemoryCache _cache;      // Declare IMemoryCache here.
        private const int DefaultPageSize = 10;

        // Update constructor to include IMemoryCache.
        public OrderController(LogiTraxContext context, ILogger<OrderController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // GET: api/order/{id}
        // Retrieve a specific order, including its order items and associated InventoryItem details.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.InventoryItem)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound();
                }

                return Ok(order);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with id {id}", id);
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        // GET: api/order
        // Retrieve all orders with optional pagination.
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                var ordersQuery = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.InventoryItem)
                    .OrderBy(o => o.OrderId);

                var orders = await ordersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(orders);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders (page: {page}, pageSize: {pageSize})", page, pageSize);
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        // GET: api/order/status/{status}
        // Retrieves orders based on their status (e.g., Pending, Completed, Rejected).
        // Regular users only see their own orders, while managers see all orders.
        [Authorize]
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                {
                    return BadRequest("Invalid order status provided. Valid values are: Pending, Complete, Rejected.");
                }

                string cacheKey = $"OrdersByStatus_{parsedStatus}_{page}_{pageSize}";

                // Change to a nullable type in the out parameter.
                if (!_cache.TryGetValue(cacheKey, out List<Order>? orders))
                {
                    IQueryable<Order> ordersQuery = _context.Orders
                        .AsNoTracking()
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.InventoryItem)
                        .Where(o => o.Status == parsedStatus);

                    if (!User.IsInRole("Manager"))
                    {
                        var currentUser = User.Identity?.Name;
                        if (string.IsNullOrEmpty(currentUser))
                        {
                            return Unauthorized("User identity not available.");
                        }
                        ordersQuery = ordersQuery.Where(o => o.CustomerName == currentUser);
                    }

                    orders = await ordersQuery
                        .OrderBy(o => o.OrderId)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

                    _cache.Set(cacheKey, orders, cacheEntryOptions);
                }

                // Check for null or empty list
                if (orders == null || !orders.Any())
                {
                    return Ok(new { Message = $"No orders found with status: {parsedStatus}" });
                }

                return Ok(orders);
                }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders by status '{status}' (page: {page}, pageSize: {pageSize})", status, page, pageSize);
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        // PATCH: /api/order/{id}/status
        // Only a manager can update the order status.
        [Authorize(Roles = "Manager")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto updateDto)
        {
            if (updateDto == null || string.IsNullOrEmpty(updateDto.Status))
            {
                return BadRequest("A status value must be provided in the request body.");
            }

            // Attempt to convert the status string to the enum value.
            // Note: We now expect "Complete" instead of "Completed".
            if (!Enum.TryParse<OrderStatus>(updateDto.Status, true, out var parsedStatus))
            {
                return BadRequest("Invalid order status provided. Valid values are: Pending, Complete, Rejected.");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // FUTURE PAYMENT INTEGRATION:
            // ---------------------------------------------
            // Uncomment and integrate with your payment gateway logic when available.
            /*
            if (parsedStatus == OrderStatus.Complete)
            {
                // Example pseudo-code for processing a payment.
                // var paymentResult = PaymentGateway.ProcessPayment(order);
                // if (!paymentResult.IsSuccess)
                // {
                //     order.Status = OrderStatus.Rejected;
                //     await _context.SaveChangesAsync();
                //     return BadRequest("Payment failed, order rejected.");
                // }
            }
            */
            // ---------------------------------------------
            
            order.Status = parsedStatus;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // POST: api/order
        // Create a new order along with its order items.
        // This validates inventory stock, deducts quantities, and then saves the order.
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order newOrder, [FromQuery] string? sessionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // If a sessionId is provided, assign it to the order.
                if (!string.IsNullOrEmpty(sessionId))
                {
                    newOrder.SessionId = sessionId;
                }

                // Process each order item: validate inventory, and deduct stock
                foreach (var orderItem in newOrder.OrderItems)
                {
                    // Retrieve inventory item by its primary key (ItemId)
                    var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.ItemId == orderItem.InventoryItemId);
                    if (inventoryItem == null)
                    {
                        return BadRequest($"Inventory item with ID {orderItem.InventoryItemId} not found.");
                    }
                    if (inventoryItem.Quantity < orderItem.Quantity)
                    {
                        return BadRequest($"Not enough quantity for {inventoryItem.Name}. Requested: {orderItem.Quantity}, Available: {inventoryItem.Quantity}.");
                    }

                    // Deduct ordered quantity from inventory.
                    inventoryItem.Quantity -= orderItem.Quantity;
                }

                // Add the new order
                _context.Orders.Add(newOrder);

                // Attempt to apply all changes (inventory update and new order) to the database.
                await _context.SaveChangesAsync();

                // Audit log entry creation (placed here, after successful save, but before returning)
                var auditEntry = new AuditLog
                {
                    User = User.Identity?.Name ?? "Unknown",
                    Action = "Order Created",
                    Details = $"Order ID {newOrder.OrderId} created with {newOrder.OrderItems.Count} item(s)."
                };
                _context.AuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync(); // Consider combining with the previous SaveChanges call if desired

                return CreatedAtAction(nameof(GetOrder), new { id = newOrder.OrderId }, newOrder);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // Log the concurrency error for diagnostics:
                _logger.LogError(ex, "A concurrency error occurred while processing the order for customer {CustomerName} (Order ID: {OrderId}).", newOrder.CustomerName, newOrder.OrderId);
                // Return a 409 Conflict response, informing the client of the concurrency problem.
                return Conflict("The inventory was updated by another transaction. Please try placing your order again.");
            }
            catch (Exception ex)
            {
                // Log a general error and return a 500 response.
                _logger.LogError(ex, "Error creating a new order for customer {CustomerName}.", newOrder.CustomerName);
                return StatusCode(500, "An error occurred creating the order. Please try again later.");
            }
        }
    }
}
