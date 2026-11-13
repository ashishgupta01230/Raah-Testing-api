using HautSphere.BuildingBlocks.Configuration.Extensions;
using HautSphere.BuildingBlocks.Configuration.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

#nullable disable
namespace HautSphere.BuildingBlocks.Configuration.Controller
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class ApiControllerBase : ControllerBase
    {
        internal string MethodType { get; set; }
        protected string relPath = string.Empty;
        protected PostResponseModel PostResponseModel => ResponseModel as PostResponseModel;

        protected ApiControllerBase()
        {
            ResponseModel = new ResponseModel();
        }

        public ResponseModel ResponseModel { get; private set; }

        [NonAction]
        internal void SetPostResponseModel(string type)
        {
            ResponseModel = new PostResponseModel
            {
                ModelValidation = new ModelValidationOutput { IsValid = true }
            };
            MethodType = type;
        }

        public long UserId => HttpContext.User.Claims.GetUserId();
        public string UserName => HttpContext.User.Claims.GetUserName();
        public int RoleId => HttpContext.User.Claims.GetUserRoleId();
        public long? RegNo => HttpContext.User.Claims.GetUserRegNo();
        public Guid? MemberGraphId => HttpContext.User.Claims.GetMemberGraphId();

        [NonAction]
        internal void SetResponseModel(string type)
        {
            ResponseModel = new ResponseModel();
            MethodType = type;
        }

        [NonAction]
        public dynamic SetData(object data)
        {

            ResponseModel.Data = data;
            return ResponseModel;
        }

        internal void SetActionArguments(IEnumerable<KeyValuePair<string, object>> actionArguments)
        {
            ResponseModel.Data = actionArguments.Count() == 1 ? actionArguments.First().Value : actionArguments.Select(m => Tuple.Create(m.Key, m.Value)).ToArray();
        }

        internal ResponseModel SetValidationResult(ModelStateDictionary state)
        {
            ResponseModel = PostResponseModel == null ? new PostResponseModel() : ResponseModel;
            PostResponseModel.ModelValidation = new ModelValidationOutput
            {
                IsValid = state.IsValid,
                Errors = state.Where(x => x.Value != null && x.Value.Errors.Any())
                        .Select(x =>
                            new ModelValidationModel(x.Key.IndexOf(".") >= 0 ? x.Key[(x.Key.LastIndexOf(".") + 1)..] : x.Key,
                            x.Value.Errors.First().ErrorMessage))
                        .ToList()
            };
            return ResponseModel;
        }
        public string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
