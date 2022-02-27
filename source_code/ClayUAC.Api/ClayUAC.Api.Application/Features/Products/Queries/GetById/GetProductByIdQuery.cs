using AspNetCoreHero.Results;
using AutoMapper;
using ClayUAC.Api.Application.Interfaces.CacheRepositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Products.Queries.GetById
{
    public class GetProductByIdQuery : IRequest<Result<GetProductByIdResponse>>
    {
        public int Id { get; set; }

        public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<GetProductByIdResponse>>
        {
            private readonly IProductCacheRepository _productCache;
            private readonly IMapper _mapper;

            public GetProductByIdQueryHandler(IProductCacheRepository productCache, IMapper mapper)
            {
                _productCache = productCache;
                _mapper = mapper;
            }

            public async Task<Result<GetProductByIdResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
            {
                var product = await _productCache.GetByIdAsync(query.Id);
                var mappedProduct = _mapper.Map<GetProductByIdResponse>(product);
                return Result<GetProductByIdResponse>.Success(mappedProduct);
            }
        }
    }
}