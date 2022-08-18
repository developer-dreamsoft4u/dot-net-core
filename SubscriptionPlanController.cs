using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BodhiApp.Api.Providers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BodhiApp.Api.Helpers;
using BodhiApp.Api.Service.ViewModels;
using BodhiApp.Data.Entities;
using BodhiApp.Data.Helpers;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BodhiApp.Service.Abstractions;
using BodhiApp.Service.ViewModels;
using BodhiApp.Service.SearchModels;

namespace BodhiApp.Api.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class SubscriptionPlanController : ControllerBase
    {
        #region Fields

        private readonly ISubscriptionPlanDataService _subscriptionPlanDataService;
        private readonly AppSettings _appSettings;

        #endregion

        #region Constructor
        public SubscriptionPlanController(ISubscriptionPlanDataService subscriptionPlanDataService, IOptions<AppSettings> appSettings)
        {
            _subscriptionPlanDataService = subscriptionPlanDataService;
            _appSettings = appSettings.Value;

        }
        #endregion


        [HttpPost("CreateUpdateSubscriptionPlan")]
        public async Task<IActionResult> CreateUpdateSubscriptionPlanAsync([FromBody] SubscriptionPlanModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ApiError($"Object is null"));
                }
                bool SubscriptionPlan = _subscriptionPlanDataService.SubscriptionPlanExists(model.Name);

                if (SubscriptionPlan == true)
                {
                    return BadRequest(new ApiError($"SubScription Plan already exist."));

                }
                //var baseUrl = _appSettings.PayLoad;

                var data = await _subscriptionPlanDataService.CreateUpdateSubScriptionPlanAsync(model);
                return Ok(new ApiResult(data, "SubscriptionPlan added successfully"));
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new ApiError(ex.Message));
            }
            // return message for uknown issue
            return BadRequest(new ApiError("Unable to Insert SubscriptionPlan. Please Try Later"));
        }
        [HttpGet("GetSubScriptionPlanList")]
        public IActionResult GetSubScriptionPlanList([FromQuery] SubscriptionPlanSearchModel filter)
        {
            return Ok(new ApiResult(_subscriptionPlanDataService.GetSubScriptionPlanList(filter)));
        }

        //[HttpGet("GetSubScriptionPlanList")]
        //public IActionResult GetSubScriptionPlanList()
        //{
        //    return Ok(new ApiResult(_subscriptionPlanDataService.GetSubScriptionPlanList(), "SuscriptionPlan List"));

        //}

        [HttpGet("GetSubScriptionPlanById")]
        public IActionResult GetSubScriptionPlanById(int id)
        {
            return Ok(new ApiResult(_subscriptionPlanDataService.GetSubScriptionPlanById(id), "SubscriptionPlan By Id"));

        }

        [HttpGet("GetSubScriptionPlanByTxnId")]

        public IActionResult GetSubScriptionPlanByTxnId(Guid TxnId)
        {
            return Ok(new ApiResult(_subscriptionPlanDataService.GetSubScriptionPlanByTransId(TxnId), "SubscriptionPlan By TxnId"));

        }

        [HttpGet("UpdatePlanStatus/{id}")]
        public IActionResult UpdatePlanStatus(int id)
        {
            var userStatus = _subscriptionPlanDataService.UpdatePlanStatus(id);
            string text = userStatus ? "activated" : "deactivated";
            return Ok(new ApiResult($"Subscription plan {text} successfully."));
        }

        [HttpPost("DeleteSubscriptionPlan")]
        public IActionResult DeleteSubscriptionPlan(int id, bool isActive)
        {
            return Ok(new ApiResult(_subscriptionPlanDataService.DeleteSubScriptionPlan(id, isActive), "Deleted Successfully."));
        }

        [HttpGet("GetSubScriptionPlanBySubscriberType")]
        public IActionResult GetSubScriptionPlanBySubscriberType(string subscriberType)
        {
            return Ok(new ApiResult(_subscriptionPlanDataService.GetSubScriptionPlanBySubscriberType(subscriberType), "SubscriptionPlan By " + subscriberType));

        }
    }
}
