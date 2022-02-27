using AspNetCoreHero.Results;
using AutoMapper;
using ClayUAC.Api.Application.Interfaces.CacheRepositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Brands.Queries.GetAllCached
{
    public class GetAllBrandsCachedQuery : IRequest<Result<List<GetAllBrandsCachedResponse>>>
    {
        public GetAllBrandsCachedQuery()
        {
        }
    }

    public class GetAllBrandsCachedQueryHandler : IRequestHandler<GetAllBrandsCachedQuery, Result<List<GetAllBrandsCachedResponse>>>
    {
        private readonly IBrandCacheRepository _productCache;
        private readonly IMapper _mapper;

        public GetAllBrandsCachedQueryHandler(IBrandCacheRepository productCache, IMapper mapper)
        {
            _productCache = productCache;
            _mapper = mapper;
        }

        public async Task<Result<List<GetAllBrandsCachedResponse>>> Handle(GetAllBrandsCachedQuery request, CancellationToken cancellationToken)
        {
            var brandList = await _productCache.GetCachedListAsync();
            var mappedBrands = _mapper.Map<List<GetAllBrandsCachedResponse>>(brandList);
            return Result<List<GetAllBrandsCachedResponse>>.Success(mappedBrands);
        }
    }
}