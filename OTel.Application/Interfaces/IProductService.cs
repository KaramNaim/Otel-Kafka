using OTel.Application.DTO.Product;
using OTel.Domain.Common;

namespace OTel.Application.Interfaces;

public interface IProductService
{
    Task<ResponseModel<List<ProductDetailsDto>>> GetAllAsync();
    Task<ResponseModel<ProductDetailsDto>> GetByIdAsync(int id);
    Task<ResponseModel<ProductDetailsDto>> CreateAsync(CreateProductDto dto);
    Task<ResponseModel<ProductDetailsDto>> UpdateAsync(UpdateProductDto dto);
    Task<ResponseModel<bool>> DeleteAsync(int id);
}
