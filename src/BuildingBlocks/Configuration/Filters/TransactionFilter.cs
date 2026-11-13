using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
#nullable disable
namespace HautSphere.BuildingBlocks.Configuration.Filters
{
    public class TransactionFilter : IAsyncActionFilter
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionFilter(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // BEFORE action
            await _unitOfWork.StartTransactionAsync();

            ActionExecutedContext resultContext;
            try
            {
                resultContext = await next(); // CALL THE ACTION (controller method)
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            // AFTER action
            if (resultContext.Exception == null)
            {
                await _unitOfWork.CommitAsync();   // ASYNC OK ✅
            }
            else
            {
                await _unitOfWork.RollbackAsync();
            }
        }
    }

}
