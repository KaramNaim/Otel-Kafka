using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OTel.Application.DTO.Product;
using OTel.Application.Interfaces;
using OTel.Domain.Common;
using OTel.Domain.Enums;

namespace OTel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductDto> _createValidator;

    public ProductController(IProductService productService, IValidator<CreateProductDto> createValidator)
    {
        _productService = productService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ResponseModel<ProductDetailsDto>.Failure(Error.ValidationError, errors));
        }

        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductDto dto)
    {
        var result = await _productService.UpdateAsync(dto);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
