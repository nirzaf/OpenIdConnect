using AspNetCoreHero.Results;
using AutoMapper;
using ClayUAC.Api.Application.Interfaces.Repositories;
using ClayUAC.Api.Domain.Entities.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Brands.Commands.Create
{
    public partial class CreateBrandCommand : IRequest<Result<int>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Tax { get; set; }
    }

    public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, Result<int>>
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IMapper _mapper;

        private IUnitOfWork _unitOfWork { get; set; }

        public CreateBrandCommandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _brandRepository = brandRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<int>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
        {
            var product = _mapper.Map<Brand>(request);
            await _brandRepository.InsertAsync(product);
            await _unitOfWork.Commit(cancellationToken);
            return Result<int>.Success(product.Id);
        }
    }
}