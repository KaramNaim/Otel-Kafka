using System.Diagnostics;
using System.Diagnostics.Metrics;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OTel.Application.DTO.Product;
using OTel.Application.Interfaces;
using OTel.Domain.Common;
using OTel.Domain.Enums;
using OTel.Domain.Interfaces.Common;
using OTel.Domain.Models;

namespace OTel.Application.Services;

public class ProductService : IProductService
{
    private static readonly ActivitySource ActivitySource = new("OTel.Api");
    private static readonly Meter Meter = new("OTel.Api");
    private static readonly Counter<long> ProductsQueriedCounter = Meter.CreateCounter<long>("products.queried", "count", "Number of product queries");
    private static readonly Counter<long> ProductsCreatedCounter = Meter.CreateCounter<long>("products.created", "count", "Number of products created");

    private readonly IAppDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IAppDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseModel<List<ProductDetailsDto>>> GetAllAsync()
    {
        using var activity = ActivitySource.StartActivity("ProductService.GetAll");

        _logger.LogInformation("Retrieving all products");
        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        ProductsQueriedCounter.Add(products.Count);
        activity?.SetTag("product.count", products.Count);

        var result = products.Adapt<List<ProductDetailsDto>>();
        return ResponseModel<List<ProductDetailsDto>>.Success(result);
    }

    public async Task<ResponseModel<ProductDetailsDto>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("ProductService.GetById");
        activity?.SetTag("product.id", id);

        _logger.LogInformation("Retrieving product {ProductId}", id);
        var product = await _context.Products.FindAsync(id);

        if (product is null || !product.IsActive)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return ResponseModel<ProductDetailsDto>.Failure(Error.NotFound, $"Product with ID {id} not found.");
        }

        ProductsQueriedCounter.Add(1);
        var result = product.Adapt<ProductDetailsDto>();
        return ResponseModel<ProductDetailsDto>.Success(result);
    }

    public async Task<ResponseModel<ProductDetailsDto>> CreateAsync(CreateProductDto dto)
    {
        using var activity = ActivitySource.StartActivity("ProductService.Create");

        _logger.LogInformation("Creating product {ProductName}", dto.Name);
        var product = dto.Adapt<Product>();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        ProductsCreatedCounter.Add(1);
        activity?.SetTag("product.id", product.Id);
        _logger.LogInformation("Product {ProductId} created successfully", product.Id);

        var result = product.Adapt<ProductDetailsDto>();
        return ResponseModel<ProductDetailsDto>.Success(result, "Product created successfully.");
    }

    public async Task<ResponseModel<ProductDetailsDto>> UpdateAsync(UpdateProductDto dto)
    {
        using var activity = ActivitySource.StartActivity("ProductService.Update");
        activity?.SetTag("product.id", dto.Id);

        _logger.LogInformation("Updating product {ProductId}", dto.Id);
        var product = await _context.Products.FindAsync(dto.Id);

        if (product is null || !product.IsActive)
        {
            _logger.LogWarning("Product {ProductId} not found for update", dto.Id);
            return ResponseModel<ProductDetailsDto>.Failure(Error.NotFound, $"Product with ID {dto.Id} not found.");
        }

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} updated successfully", product.Id);
        var result = product.Adapt<ProductDetailsDto>();
        return ResponseModel<ProductDetailsDto>.Success(result, "Product updated successfully.");
    }

    public async Task<ResponseModel<bool>> DeleteAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("ProductService.Delete");
        activity?.SetTag("product.id", id);

        _logger.LogInformation("Soft-deleting product {ProductId}", id);
        var product = await _context.Products.FindAsync(id);

        if (product is null || !product.IsActive)
        {
            _logger.LogWarning("Product {ProductId} not found for deletion", id);
            return ResponseModel<bool>.Failure(Error.NotFound, $"Product with ID {id} not found.");
        }

        product.IsActive = false;
        product.ModifiedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product {ProductId} soft-deleted successfully", id);
        return ResponseModel<bool>.Success(true, "Product deleted successfully.");
    }
}
