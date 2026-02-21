using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OTel.Application.DTO.Order;
using OTel.Application.Interfaces;
using OTel.Domain.Common;
using OTel.Domain.Enums;

namespace OTel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderDto> _createValidator;

    public OrderController(IOrderService orderService, IValidator<CreateOrderDto> createValidator)
    {
        _orderService = orderService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _orderService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orderService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return BadRequest(ResponseModel<OrderDetailsDto>.Failure(Error.ValidationError, errors));
        }

        var result = await _orderService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return result.Error == Error.NotFound ? NotFound(result) : BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }
}
