using AutoMapper;
using ClayUAC.Api.Application.Features.Products.Commands.Create;
using ClayUAC.Api.Application.Features.Products.Queries.GetAllCached;
using ClayUAC.Api.Application.Features.Products.Queries.GetAllPaged;
using ClayUAC.Api.Application.Features.Products.Queries.GetById;
using ClayUAC.Api.Domain.Entities.Catalog;

namespace ClayUAC.Api.Application.Mappings
{
    internal class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<CreateProductCommand, Product>().ReverseMap();
            CreateMap<GetProductByIdResponse, Product>().ReverseMap();
            CreateMap<GetAllProductsCachedResponse, Product>().ReverseMap();
            CreateMap<GetAllProductsResponse, Product>().ReverseMap();
        }
    }
}