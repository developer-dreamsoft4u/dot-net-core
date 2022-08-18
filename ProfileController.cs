using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using BodhiApp.Api.Helpers;
using BodhiApp.Api.Providers;
using BodhiApp.Data.Entities;
using BodhiApp.Service.Abstractions;
using BodhiApp.Api.Service.ViewModels;
using BodhiApp.Service.ViewModels.ProfileViewModel;
using BodhiApp.Api.Services;
using BodhiApp.Service.ViewModels;
using BodhiApp.Service.ViewModels.ProviderGroupViewModel;
using BodhiApp.Service.ViewModels.PatientModel;
using System.Net.Mail;
using System.Collections.Generic;
using System.Net;
using BodhiApp.Service.ViewModels.QueuedEmailViewModel;
using BodhiApp.Data.Helpers;

namespace BC.ApiService.Controllers.Api
{
    [Authorize]
    [Route("api/[Controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        #region Fields
        private readonly ILogger<ProfileController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly AppSettings _appSettings;
        private readonly IEmailSender _emailSender;
        private readonly ISharedDataService _sharedDataService;
        private readonly IAssociationRequestsDataService _associationrequestDataService;

        private readonly IUserDataService _userDataService;
        private readonly IProviderDataService _providerDataService;
        private readonly IProviderGroupService _providerGroupService;
        private readonly IPatientService _patientService;
        #endregion

        #region Constructor
        public ProfileController(ILogger<ProfileController> logger, IPatientService patientService, IAssociationRequestsDataService associationrequestDataService, IProviderGroupService providerGroupService, UserManager<User> userManager, IProviderDataService providerDataService, IUserDataService userDataService, IOptions<AppSettings> appSettings, IEmailSender emailSender, ISharedDataService sharedDataService)
        {
            _logger = logger;
            _userManager = userManager;
            _associationrequestDataService = associationrequestDataService;
            _appSettings = appSettings.Value;
            _emailSender = emailSender;
            _sharedDataService = sharedDataService;
            _userDataService = userDataService;
            _providerDataService = providerDataService;
            _providerGroupService = providerGroupService;
            _patientService = patientService;
        }
        #endregion

        // GET: api/Profile/GetProfile - get user profile        
        [HttpGet("CurrentUser")]
        public IActionResult CurrentUser()
        {
            var user = _userManager.Users.FirstOrDefault(m => m.Id == GetCurrentUserId());

            if (user == null)
                return BadRequest(new ApiError($"User not found"));

            var authUser = new
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePicUrl = FileToUrl(user.ProfilePicUrl),
                ProfileStatus = user.ProfileStatus,
            };

            return Ok(new ApiResult(authUser));
        }

        // GET: api/Profile/GetProfile - get user profile        
        [HttpGet("GetUserByEmailOrPhonenumber")]
        public async Task<IActionResult> GetUserByEmailOrPhonenumberAsync(string userType, string email = "", string phoneNumber = "")
        {
            var userProfile = _userDataService.GetUserByEmailOrPhonenumberAsync(userType, email, phoneNumber);
            return Ok(new ApiResult(userProfile));
        }
        //string UserType = "", string UserId = "", string GroupId = ""
        [HttpGet("SendRequestForAssociation")]
        public async Task<IActionResult> SendRequestForAssociation(string UserType = "", int UserId = 0, int GroupId = 0, string EmailId = "")
        {
            AssociationRequestsCreateModel obj = new AssociationRequestsCreateModel();
            obj.UserId = UserId;
            obj.Groupid = GroupId;
            obj.UserType = UserType;
            var Res = await _sharedDataService.AddAssociationRequest(obj);
            //Send Email  
            var EmailaccountData = _sharedDataService.GetHostEmailAccount();
            User DoctorName = await _userManager.FindByIdAsync(Convert.ToString(UserId));
            User GroupName = await _userManager.FindByIdAsync(Convert.ToString(GroupId));
            var baseUrl = _appSettings.WebUrl;
            string Header = "";
            if (UserType == "Consultant")
                Header = "Send Association Request By Consultant Group.";
            else
                Header = "Send Association Request By Consultee Group.";
            var EmailFormat = @$"Hello Dr." + DoctorName.FirstName + " " + DoctorName.MiddleName + " " + DoctorName.LastName + " <Br/><Br/>" + GroupName.FirstName + " " + GroupName.MiddleName + " " + GroupName.LastName + "  group has send you association request.<Br/> you can accept and decline association by clicking on following buttons. <Br/><br/><a href=\"" + baseUrl + "account/associaterequestverify/" + UserId + "/" + GroupId + "/y\" > Accept</a>                  <a href=\"" + baseUrl + "account/associaterequestverify/" + UserId + "/" + GroupId + "/r\">Reject</a>";
            var res = _emailSender.SendEmailAsso(EmailaccountData, Header, EmailFormat, "", EmailId, "", "", false);
            // Send Email End
            return Ok(new ApiResult(1));
        }

        [HttpGet("CheckUserAssociateorNot")]
        public async Task<IActionResult> CheckUserAssociateorNot(int userId = 0)
        {
            var Res = await _sharedDataService.CheckUserAssociateorNotAsync(userId);
            return Ok(new ApiResult(Res));
        }

        [AllowAnonymous]
        [HttpGet("ChangeStatusRequestForAssociation")]
        public async Task<IActionResult> ChangeStatusRequestForAssociation(int UserId = 0, int GroupId = 0, string Status = "")
        {
            AssociationRequestMappingCreateModel obj = new AssociationRequestMappingCreateModel();
            obj.UserId = Convert.ToInt32(UserId);
            obj.GroupId = Convert.ToInt32(GroupId);
            int flag = 0;
            if (Status.ToLower() == "y")
            {
                flag = 1;
            }
            obj.AssociationRequestStatus = flag;
            obj.CreatedDate = DateTime.UtcNow;
            obj.IsActive = true;
            var Res = await _associationrequestDataService.CreateAsociationRequestMappingAsync(obj);
            if (Res <= 0)
            {
                flag = 0;
            }
            return Ok(new ApiResult(flag));
        }


        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfileAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return BadRequest(new ApiError($"User not found"));

            var userProfile = _userDataService.GetDisplayProfile(user.Id);

            return Ok(new ApiResult(userProfile));
        }


        // GET: api/Profile/GetProfile - get user profile        
        [HttpGet("GetBasicProfile")]
        public async Task<IActionResult> GetBasicProfileAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return BadRequest(new ApiError($"User not found"));
            var provider = _providerDataService.GetProviderByUserId(user.Id);
            var userProfile = new
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DialCode = user.DialCode,
                birthDate = user.BirthDate,
                Gender = user.Gender,
                Address = user.Address1,
                // TimeZone = user.TimeZone,
                TimeZone = provider != null ? provider.TimeZoneId : string.Empty,
                City = user.City,
                State = user.State,
                Country = user.Country,
                ZipCode = user.ZipCode,
                Experience = provider != null ? provider.Experience : string.Empty,
                ProviderType = provider != null ? provider.ProviderType : string.Empty,
                ProviderSkill = provider != null ? provider.ProviderSkill : 0,
                OtherSkill = provider != null ? provider.OtherSkill : 0,
                Language = provider != null ? provider.Language : string.Empty,
                SecondaryLanguage = provider != null ? provider.Secondarylanguage : string.Empty,
            };
            return Ok(new ApiResult(userProfile));
        }

        // GET: api/Profile/GetGroupProfile -         
        [HttpGet("GetGroupProfile")]
        public async Task<IActionResult> GetGroupProfile(int userId)
        {
            var userProfile = _providerGroupService.GetGroupProfile(userId);
            return Ok(new ApiResult(userProfile));
        }
        [HttpGet("GetAssoGroupProfile")]
        public async Task<IActionResult> GetAssoGroupProfile(int userId)
        {
            var userProfile = _providerGroupService.GetAssocitateGroupProfile(userId);
            return Ok(new ApiResult(userProfile));
        }
        [HttpPost("UpdatePatientProfile")]
        public async Task<IActionResult> UpdatePatientProfile([FromBody] PatientCreateModel model)
        {
            try
            {
                var curruser = await GetCurrentUserAsync();
                if (curruser == null)
                    return BadRequest(new ApiError("User not found"));

                var user = _userManager.Users.Where(m => m.Id == model.UserId).FirstOrDefault();

                if (user == null)
                    return BadRequest(new ApiError("Patient not found"));

                user.DialCode = model.DialCode;
                user.PhoneNumber = model.PhoneNumber;
                user.FirstName = model.FirstName;
                user.MiddleName = model.MiddleName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.Gender = model.Gender;
                user.BirthDate = model.BirthDate;
                user.Address1 = model.Address;
                user.Country = model.Country;
                user.State = model.State;
                user.City = model.City;
                user.ZipCode = model.ZipCode;
                user.UpdatedDate = DateTime.UtcNow;
                user.UpdatedById = model.UpdatedBy;
                user.SSN = model.SSN;
                user.TimeZone = model.TimeZone;

                var result = await _userManager.UpdateAsync(user);

                // Verify if user updated successfully
                if (result != null && result.Succeeded)
                {
                    try
                    {
                        await _patientService.UpdatePatientAsync(model);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new ApiError(ex.Message));
                    }
                    return Ok(new ApiResult("Profile is updated successfully!"));
                }
                return BadRequest(new ApiError("Profile is not updated!"));
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new ApiError(ex.Message));
            }
        }

        [HttpPost("UpdateGroupProfile")]
        public async Task<IActionResult> UpdateGroupProfile([FromBody] ProviderGroupViewModel model)
        {
            try
            {
                if (!model.UserId.Equals(GetCurrentUserId()))
                    return BadRequest(new ApiError("Invalid user operation"));

                var user = await GetCurrentUserAsync();
                if (user == null)
                    return BadRequest(new ApiError("User not found"));

                user.FirstName = model.FirstName;
                user.MiddleName = model.MiddleName;
                user.LastName = model.LastName;
                user.DialCode = model.DialCode;
                user.Address1 = model.Address;
                user.City = model.CityId;
                user.State = model.StateId;
                user.Country = model.CountryId;
                user.ZipCode = model.ZipCode;
                user.TimeZone = model.TimeZone;
                user.WebsiteUrl = model.WebsiteUrl;
                var result = await _userManager.UpdateAsync(user);

                // Verify if user updated successfully
                if (result != null && result.Succeeded)
                {
                    try
                    {
                        await _providerGroupService.UpdateProviderGroupAsync(model);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new ApiError(ex.Message));
                    }
                    return Ok(new ApiResult("Profile is updated successfully!"));
                }
                return BadRequest(new ApiError("Profile is not updated!"));
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new ApiError(ex.Message));
            }
        }

        // PUT: api/Profile/UpdateProfile - update user profile
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileModel model)
        {
            try
            {
                if (!model.Id.Equals(GetCurrentUserId()))
                    return BadRequest("Invalid user operation");

                var user = await GetCurrentUserAsync();
                if (user == null)
                    return BadRequest($"User not found");

                //update new user in identity database
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Title = model.Title;

                var result = await _userManager.UpdateAsync(user);

                // Verify if user updated successfully
                if (result != null && result.Succeeded)
                {
                    var result1 = await _userManager.SetPhoneNumberAsync(user, model.Phone);
                    if (result1 != null && result1.Succeeded)
                    {
                        return Ok("Your profile is updated successfully!");
                    }
                }
                // return message for uknown issue
                return BadRequest("Unable to update user profile. Please Try Later");
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }

        private int GetCurrentUserId()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return int.Parse(userId);
        }
        #endregion
    }
}