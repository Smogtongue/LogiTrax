using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LogiTrax.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogiTrax.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly LogiTraxContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<InventoryController> _logger;

        // Define a static list of allowed locations.
        private static readonly List<string> AllowedLocations = new List<string>
        {
            "Warehouse A",
            "Warehouse B",
            "Central Hub",
            "Secondary Hub"
        };

        public InventoryController(LogiTraxContext context, IMemoryCache cache, ILogger<InventoryController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // GET: /api/inventory
        [HttpGet]
        public async Task<IActionResult> GetInventory()
        {
            // Start Stopwatch to measure execution time.
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            string cacheKey = "InventoryList";

            if (_cache.TryGetValue(cacheKey, out List<InventoryItem>? inventoryList) && inventoryList is not null)
            {
                stopwatch.Stop();
                _logger.LogInformation("GET /api/inventory (from Cache) executed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                return Ok(inventoryList);
            }

            inventoryList = await _context.InventoryItems.AsNoTracking().ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };

            _cache.Set(cacheKey, inventoryList, cacheOptions);

            stopwatch.Stop();
            _logger.LogInformation("GET /api/inventory (from DB) executed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return Ok(inventoryList);
        }


        // POST: /api/inventory
        // This action is restricted to the "Manager" role.
        // It adds a new inventory item or updates the quantity if an item with the same Name and Location exists.
        // It also validates the provided location against a list of allowed locations.
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<IActionResult> AddOrUpdateInventory([FromBody] InventoryItem newItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate the location.
            if (!AllowedLocations.Contains(newItem.Location))
            {
                return BadRequest("Invalid location. Please provide a valid, pre-registered warehouse location.");
            }

            // Check for an existing inventory item with the same Name and Location.
            var existingItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Name == newItem.Name && i.Location == newItem.Location);

            if (existingItem != null)
            {
                // If found, update the quantity.
                existingItem.Quantity += newItem.Quantity;
                _context.InventoryItems.Update(existingItem);
                newItem = existingItem;
            }
            else
            {
                // Otherwise, add the new inventory item.
                _context.InventoryItems.Add(newItem);
            }

            await _context.SaveChangesAsync();

            // Invalidate the cache so that the updated inventory is fetched on subsequent GET requests.
            _cache.Remove("InventoryList");

            // Log the update for monitoring.
            _logger.LogInformation("Inventory record for {ItemName} at {Location} updated. New quantity: {Quantity}.", 
                                     newItem.Name, newItem.Location, newItem.Quantity);

            return Ok(newItem);
        }

        // DELETE: /api/inventory/{id}
        // Deletes an inventory item by its ID. Restricted to users in the "Manager" role.
        [Authorize(Roles = "Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItem(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { Message = "Inventory item not found." });
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            // Invalidate the cached inventory list to ensure fresh data.
            _cache.Remove("InventoryList");

            // Log the deletion for debugging/monitoring purposes.
            _logger.LogInformation("Deleted inventory item: {ItemName} from {Location} (ID: {ItemId})", item.Name, item.Location, item.ItemId);

            return Ok(new { Message = $"Inventory item with ID {id} deleted successfully." });
        }
    }
}
