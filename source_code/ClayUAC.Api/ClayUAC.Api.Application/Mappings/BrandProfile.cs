using AutoMapper;
using ClayUAC.Api.Application.Features.Brands.Commands.Create;
using ClayUAC.Api.Application.Features.Brands.Queries.GetAllCached;
using ClayUAC.Api.Application.Features.Brands.Queries.GetById;
using ClayUAC.Api.Domain.Entities.Catalog;

namespace ClayUAC.Api.Application.Mappings
{
    internal class BrandProfile : Profile
    {
        public BrandProfile()
        {
            CreateMap<CreateBrandCommand, Brand>().ReverseMap();
            CreateMap<GetBrandByIdResponse, Brand>().ReverseMap();
            CreateMap<GetAllBrandsCachedResponse, Brand>().ReverseMap();
        }
    }
}