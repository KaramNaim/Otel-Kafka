using System.Diagnostics;
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
    private readonly IEventProducer _eventProducer;

    public OrderController(IOrderService orderService, IValidator<CreateOrderDto> createValidator, IEventProducer eventProducer)
    {
        _orderService = orderService;
        _createValidator = createValidator;
        _eventProducer = eventProducer;
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

    /// <summary>
    /// Publishes a burst of Kafka events to stress test the messaging pipeline.
    /// Does NOT create real orders — only publishes synthetic events to Kafka.
    /// </summary>
    [HttpPost("stress/{count:int}")]
    public async Task<IActionResult> Stress(int count, [FromQuery] int parallelism = 10)
    {
        if (count is < 1 or > 100_000)
            return BadRequest("Count must be between 1 and 100,000.");

        parallelism = Math.Clamp(parallelism, 1, 100);

        var sw = Stopwatch.StartNew();
        var succeeded = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(1, count),
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            async (i, ct) =>
            {
                try
                {
                    var evt = new Domain.Events.OrderCreatedEvent
                    {
                        OrderId = -i,
                        ProductId = 0,
                        ProductName = "StressTest",
                        Quantity = 1,
                        TotalPrice = 0,
                        CreatedOn = DateTime.UtcNow
                    };

                    await _eventProducer.ProduceAsync("order-events", null!, evt, ct);
                    Interlocked.Increment(ref succeeded);
                }
                catch
                {
                    Interlocked.Increment(ref failed);
                }
            });

        sw.Stop();

        return Ok(new
        {
            TotalMessages = count,
            Succeeded = succeeded,
            Failed = failed,
            Parallelism = parallelism,
            ElapsedMs = sw.ElapsedMilliseconds,
            MessagesPerSecond = count / Math.Max(sw.Elapsed.TotalSeconds, 0.001)
        });
    }
}
