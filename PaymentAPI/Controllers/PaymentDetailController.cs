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
        public async Task<ActionResult<ResponseResult>> CreateItem(ItemData data)
        {
            if (ModelState.IsValid)
            {
                await _context.Payments.AddAsync(data);
                await _context.SaveChangesAsync();

                // return CreatedAtAction("GetItem", new { data.id }, data);
                return new ResponseResult()
                {
                    Success = true,
                    Method = "Insert",
                    Data = CreatedAtAction("GetItem", new { data.id }, data).Value,
                };
            }

            return BadRequest(new ResponseResult()
            {
                Success = false,
                Method = "Insert",
                Errors = "Failed to insert data"
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseResult>> GetItem(int id)
        {
            var item = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (item == null)
            {
                return NotFound(new ResponseResult()
                {
                    Success = false,
                    Method = "Search",
                    Errors = "data with id " + id + " not found"
                });
            }

            return new ResponseResult()
            {
                Success = true,
                Method = "Search",
                Data = item
            }; ;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseResult>> UpdateItem(int id, ItemData item)
        {
            if (id != item.id)
            {
                return BadRequest(new ResponseResult()
                {
                    Success = false,
                    Method = "Update",
                    Errors = "id on entrypoint is not the same as id on body"
                });
            }
            var existItem = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (existItem == null)
            {
                return NotFound(new ResponseResult()
                {
                    Success = false,
                    Method = "Update",
                    Errors = existItem.id + "Not Found"
                });
            }
            existItem.cardOwnerName = item.cardOwnerName;
            existItem.cardNumber = item.cardNumber;
            existItem.expirationDate = item.expirationDate;
            existItem.securityCode = item.securityCode;

            await _context.SaveChangesAsync();

            return new ResponseResult()
            {
                Success = true,
                Method = "Update",
                Data = existItem
            };

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResponseResult>> DeleteItem(int id)
        {
            var existItem = await _context.Payments.FirstOrDefaultAsync(x => x.id == id);

            if (existItem == null)
            {
                return NotFound(new ResponseResult()
                {
                    Success = false,
                    Method = "Delete",
                    Errors = "data with id " + id + " not found"
                });
            }
            _context.Payments.Remove(existItem);
            await _context.SaveChangesAsync();

            return new ResponseResult()
            {
                Success = true,
                Method = "Delete",
                Data = existItem
            };
        }
    }
}