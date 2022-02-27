using AspNetCoreHero.Results;
using ClayUAC.Api.Application.Exceptions;
using ClayUAC.Api.Application.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Features.Products.Commands.Update
{
    public class UpdateProductImageCommand : IRequest<Result<int>>
    {
        public int Id { get; set; }
        public byte[] Image { get; set; }

        public class UpdateProductImageCommandHandler : IRequestHandler<UpdateProductImageCommand, Result<int>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IProductRepository _productRepository;

            public UpdateProductImageCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
            {
                _productRepository = productRepository;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<int>> Handle(UpdateProductImageCommand command, CancellationToken cancellationToken)
            {
                var product = await _productRepository.GetByIdAsync(command.Id);

                if (product == null)
                {
                    throw new ApiException($"Product Not Found.");
                }
                else
                {
                    product.Image = command.Image;
                    await _productRepository.UpdateAsync(product);
                    await _unitOfWork.Commit(cancellationToken);
                    return Result<int>.Success(product.Id);
                }
            }
        }
    }
}