using Products.Core.Dto.Common;
using Products.Core.Dto.Product;

namespace Products.Business.Product.Interface;

public interface IProductService
{
    Task<PagedResultDto<ProductDto>> SearchAsync(ProductFilterDto filter);
    Task<ProductDto> GetByIdAsync(Guid id);
    Task<List<ProductDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);
    Task<List<string>> GetCategoriesAsync();
    Task<ProductDto> CreateAsync(ProductCreateDto dto);
    Task<ProductDto> UpdateAsync(Guid id, ProductUpdateDto dto);
    Task DeleteAsync(Guid id);
    Task<ProductDto> AdjustStockAsync(Guid id, StockAdjustmentDto dto);
}
