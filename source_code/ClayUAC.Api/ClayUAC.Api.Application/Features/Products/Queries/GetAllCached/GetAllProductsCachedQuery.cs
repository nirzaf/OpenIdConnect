using AspNetCoreHero.Results;
using AutoMapper;
using ClayUAC.Api.Application.Interfaces.CacheRepositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Products.Queries.GetAllCached
{
    public class GetAllProductsCachedQuery : IRequest<Result<List<GetAllProductsCachedResponse>>>
    {
        public GetAllProductsCachedQuery()
        {
        }
    }

    public class GetAllProductsCachedQueryHandler : IRequestHandler<GetAllProductsCachedQuery, Result<List<GetAllProductsCachedResponse>>>
    {
        private readonly IProductCacheRepository _productCache;
        private readonly IMapper _mapper;

        public GetAllProductsCachedQueryHandler(IProductCacheRepository productCache, IMapper mapper)
        {
            _productCache = productCache;
            _mapper = mapper;
        }

        public async Task<Result<List<GetAllProductsCachedResponse>>> Handle(GetAllProductsCachedQuery request, CancellationToken cancellationToken)
        {
            var productList = await _productCache.GetCachedListAsync();
            var mappedProducts = _mapper.Map<List<GetAllProductsCachedResponse>>(productList);
            return Result<List<GetAllProductsCachedResponse>>.Success(mappedProducts);
        }
    }
}