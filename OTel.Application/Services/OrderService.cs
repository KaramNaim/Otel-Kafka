using System.Diagnostics;
using System.Diagnostics.Metrics;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OTel.Application.DTO.Order;
using OTel.Application.Interfaces;
using OTel.Domain.Common;
using OTel.Domain.Enums;
using OTel.Domain.Interfaces.Common;
using OTel.Domain.Events;
using OTel.Domain.Models;

namespace OTel.Application.Services;

public class OrderService : IOrderService
{
    private static readonly ActivitySource ActivitySource = new("OTel.Api");
    private static readonly Meter Meter = new("OTel.Api");
    private static readonly Counter<long> OrdersCreatedCounter = Meter.CreateCounter<long>("orders.created", "count", "Number of orders created");

    private readonly IAppDbContext _context;
    private readonly IEventProducer _eventProducer;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IAppDbContext context, IEventProducer eventProducer, ILogger<OrderService> logger)
    {
        _context = context;
        _eventProducer = eventProducer;
        _logger = logger;
    }

    public async Task<ResponseModel<List<OrderDetailsDto>>> GetAllAsync()
    {
        using var activity = ActivitySource.StartActivity("OrderService.GetAll");

        _logger.LogInformation("Retrieving all orders");
        var orders = await _context.Orders
            .Include(o => o.Product)
            .Where(o => o.IsActive)
            .ToListAsync();

        activity?.SetTag("order.count", orders.Count);

        var result = orders.Adapt<List<OrderDetailsDto>>();
        return ResponseModel<List<OrderDetailsDto>>.Success(result);
    }

    public async Task<ResponseModel<OrderDetailsDto>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("OrderService.GetById");
        activity?.SetTag("order.id", id);

        _logger.LogInformation("Retrieving order {OrderId}", id);
        var order = await _context.Orders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null || !order.IsActive)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return ResponseModel<OrderDetailsDto>.Failure(Error.NotFound, $"Order with ID {id} not found.");
        }

        var result = order.Adapt<OrderDetailsDto>();
        return ResponseModel<OrderDetailsDto>.Success(result);
    }

    public async Task<ResponseModel<OrderDetailsDto>> CreateAsync(CreateOrderDto dto)
    {
        using var activity = ActivitySource.StartActivity("OrderService.Create");
        activity?.SetTag("order.productId", dto.ProductId);
        activity?.SetTag("order.quantity", dto.Quantity);

        _logger.LogInformation("Creating order for product {ProductId}, quantity {Quantity}", dto.ProductId, dto.Quantity);

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product is null || !product.IsActive)
        {
            _logger.LogWarning("Product {ProductId} not found for order", dto.ProductId);
            return ResponseModel<OrderDetailsDto>.Failure(Error.NotFound, $"Product with ID {dto.ProductId} not found.");
        }

        if (product.Stock < dto.Quantity)
        {
            _logger.LogWarning("Insufficient stock for product {ProductId}. Requested: {Quantity}, Available: {Stock}",
                dto.ProductId, dto.Quantity, product.Stock);
            return ResponseModel<OrderDetailsDto>.Failure(Error.InsufficientStock,
                $"Insufficient stock. Available: {product.Stock}, Requested: {dto.Quantity}");
        }

        var order = new Order
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            TotalPrice = product.Price * dto.Quantity,
            Status = "Pending"
        };

        product.Stock -= dto.Quantity;
        product.ModifiedOn = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        OrdersCreatedCounter.Add(1);
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.totalPrice", order.TotalPrice);

        _logger.LogInformation("Order {OrderId} created successfully. Total: {TotalPrice}", order.Id, order.TotalPrice);

        // Publish OrderCreated event to Kafka
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice,
            CreatedOn = order.CreatedOn
        };

        await _eventProducer.ProduceAsync("order-events", order.Id.ToString(), orderEvent);

        // Re-fetch with navigation property for mapping
        var created = await _context.Orders
            .Include(o => o.Product)
            .FirstAsync(o => o.Id == order.Id);

        var result = created.Adapt<OrderDetailsDto>();
        return ResponseModel<OrderDetailsDto>.Success(result, "Order created successfully.");
    }
}
