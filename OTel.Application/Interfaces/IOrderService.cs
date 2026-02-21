using OTel.Application.DTO.Order;
using OTel.Domain.Common;

namespace OTel.Application.Interfaces;

public interface IOrderService
{
    Task<ResponseModel<List<OrderDetailsDto>>> GetAllAsync();
    Task<ResponseModel<OrderDetailsDto>> GetByIdAsync(int id);
    Task<ResponseModel<OrderDetailsDto>> CreateAsync(CreateOrderDto dto);
}
