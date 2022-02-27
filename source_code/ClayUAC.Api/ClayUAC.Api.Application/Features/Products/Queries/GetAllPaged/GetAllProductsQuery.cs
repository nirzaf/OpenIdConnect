using AspNetCoreHero.Results;
using ClayUAC.Api.Application.Extensions;
using ClayUAC.Api.Application.Interfaces.Repositories;
using ClayUAC.Api.Domain.Entities.Catalog;
using MediatR;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Products.Queries.GetAllPaged
{
    public class GetAllProductsQuery : IRequest<PaginatedResult<GetAllProductsResponse>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public GetAllProductsQuery(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class GGetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, PaginatedResult<GetAllProductsResponse>>
    {
        private readonly IProductRepository _repository;

        public GGetAllProductsQueryHandler(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginatedResult<GetAllProductsResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<Product, GetAllProductsResponse>> expression = e => new GetAllProductsResponse
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Rate = e.Rate,
                Barcode = e.Barcode
            };
            var paginatedList = await _repository.Products
                .Select(expression)
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);
            return paginatedList;
        }
    }
}