using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentAPI.Data;
using PaymentAPI.Models;
using PaymentAPI.Configuration;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PaymentDetailController : ControllerBase
    {
        private readonly ApiDbContext _context;
        public PaymentDetailController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var items = await _context.Payments.ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult> CreateItem(ItemData data)
        {
            if (ModelState.IsValid)
            {
                await _context.Payments.AddAsync(data);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetItem", new { data.id }, data);
            }

            return new JsonResult("Something went wrong") { StatusCode = 500 };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetItem(int id)
        {
            var item = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateItem(int id, ItemData item)
        {
            if (id != item.id)
                return BadRequest();
            var existItem = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (existItem == null)
                return NotFound();
            existItem.cardOwnerName = item.cardOwnerName;
            existItem.cardNumber = item.cardNumber;
            existItem.expirationDate = item.expirationDate;
            existItem.securityCode = item.securityCode;

            await _context.SaveChangesAsync();

            return Ok(existItem);

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteItem(int id)
        {
            var existItem = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (existItem == null)
                return NotFound();

            _context.Payments.Remove(existItem);
            await _context.SaveChangesAsync();

            return new JsonResult("Success Delete " + id) { StatusCode = 200 };
        }
    }
}