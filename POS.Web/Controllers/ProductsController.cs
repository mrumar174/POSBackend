using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Catalog;
using POS.DTOs.Common;
using POS.Entities.IServices;
using POS.Entities.IServices.Catalog;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
            => Ok(await _productService.GetAllAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return product is null ? NotFound() : Ok(product);
        }

        [RequirePermission("Products.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
        {
            try
            {
                var created = await _productService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [RequirePermission("Products.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "Route id and body id must match." });

            try
            {
                return Ok(await _productService.UpdateAsync(dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Products.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<ProductDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? name,
        [FromQuery] string? productCode,
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId,
        [FromQuery] int? unitId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        {
            var result = await _productService.SearchAsync(new ProductQueryDto
            {
                Search = search,
                Name = name,
                ProductCode = productCode,
                CategoryId = categoryId,
                BrandId = brandId,
                UnitId = unitId,
                Page = page,
                PageSize = pageSize
            });
            return Ok(result);
        }
    }
}