using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Web.Mvc.Controls;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using User.Management.API.Models;
using User.Management.API.Models.Authentication.Login;
using User.Management.API.Models.Authentication.SignUp;
using User.Management.Service.Models;
using User.Management.Service.SERVICE;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using User.Management.API.ModelRequests;


namespace User.Management.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        public AuthenticationController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole>
            roleManager, IEmailService emailService, IConfiguration configuration, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _signInManager = signInManager;
            _context = context;
        }




        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser registerUser, string role)
        {

            if (string.IsNullOrWhiteSpace(registerUser.Email))
            {
                return BadRequest(new Response { Status = "Error", Message = "Email is required." });
            }

            if (string.IsNullOrWhiteSpace(registerUser.Password))
            {
                return BadRequest(new Response { Status = "Error", Message = "Password is required." });
            }

            var userExist = await _userManager.FindByEmailAsync(registerUser.Email);
            if (userExist != null)
            {
                return BadRequest(new Response { Status = "Error", Message = "User already exists." });
            }

            var user = new IdentityUser
            {
                Email = registerUser.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerUser.UserName,
                TwoFactorEnabled = true
            };

            if (await _roleManager.RoleExistsAsync(role))
            {
                var result = await _userManager.CreateAsync(user, registerUser.Password);

                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                       new Response { Status = "Error", Message = $"User creation failed" });
                }

                await _userManager.AddToRoleAsync(user, role);



                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new[] { user.Email }, "Confirmation email link", confirmationLink!);
                _emailService.SendEmail(message);

                return Ok(new Response { Status = "Success", Message = $"User created & Email sent to {user.Email} successfully." });
            }
            else
            {
                return BadRequest(new Response { Status = "Error", Message = "This role does not exist." });
            }
        }







        [HttpGet("ConfirmEmail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status200OK,
                   new Response { Status = "success", Message = "Email Verified Successfully" });
                }

            }

            return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "This user doesnot exist!" });


        }








        [HttpPost("Login")]

        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {

            var user = await _userManager.FindByNameAsync(loginModel.Username);
            if (user!.TwoFactorEnabled)
            {
                await _signInManager.SignOutAsync();
                await _signInManager.PasswordSignInAsync(user, loginModel.Password, false, true);
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");


                var message = new Message(new string[] { user.Email! }, "OTP Confirmation", token);
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status200OK,
                   new Response { Status = "success", Message = $"We have sent an OTP to your Email {user.Email}" });



            }

            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {

                var authClaims = new List<Claim>
                {
                     new Claim(ClaimTypes.Name,user.UserName!),
                     new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),

                };

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }




                var JwtToken = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(JwtToken),
                    expirations = JwtToken.ValidTo
                });


            }

            return Unauthorized();


        }







        [HttpPost("OTP")]
        public async Task<IActionResult> LoginWithOTP(string code)
        {
            var signIn = await _signInManager.TwoFactorSignInAsync("Email", code, false, false);

            if (signIn.Succeeded)
            {


                var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };



                var jwtToken = GetToken(authClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expirations = jwtToken.ValidTo
                });
            }

            return Unauthorized(new Response { Status = "Error", Message = "Invalid OTP code." });
        }








        //-------------------------------------------------------------------------------------------------










        [HttpPost("CreateUserProfile")]
        public async Task<IActionResult> CreateUserProfile([FromForm] UserProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { Status = "Error", Message = "Invalid user profile data." });
            }



            byte[]? profilePhotoData = null;
            if (request.ProfilePhoto != null && request.ProfilePhoto.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await request.ProfilePhoto.CopyToAsync(memoryStream);
                    profilePhotoData = memoryStream.ToArray();
                }
            }

            var userProfile = new UserProfile
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Height = request.Height,
                Weight = (double)request.Weight,
                Gender = request.Gender,
                ProfilePhoto = profilePhotoData
            };

            try
            {
                await _context.UserProfiles.AddAsync(userProfile);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = $"Failed to create user profile: {ex.Message}" });
            }

            return Ok(new { Status = "Success", Message = "User profile created successfully." });
        }





        [HttpGet("UserProfile/{id}")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var userProfile = await _context.UserProfiles
                .Include(up => up.MedicalHistories)
                .SingleOrDefaultAsync(up => up.Id == id);


            if (userProfile == null)
            {
                return NotFound(new { Status = "Error", Message = $"User profile with id '{id}' does not exist." });
            }

            return Ok(new { Status = "Success", Data = userProfile });
        }







        [HttpPut("UpdateUserProfile/{id}")]
        public async Task<IActionResult> UpdateUserProfile(int id, [FromForm] UserProfileRequest request)
        {

            var userProfile = await _context.UserProfiles.FindAsync(id);
            if (userProfile == null)
            {
                return NotFound(new { Status = "Error", Message = "User profile not found." });
            }


            userProfile.FirstName = request.FirstName ?? userProfile.FirstName;
            userProfile.LastName = request.LastName ?? userProfile.LastName;
            userProfile.DateOfBirth = request.DateOfBirth;
            userProfile.Height = request.Height;
            userProfile.Weight = (double)request.Weight;
            userProfile.Gender = request.Gender;

            if (request.ProfilePhoto != null && request.ProfilePhoto.Length > 0)
            {
                userProfile.ProfilePhoto = await ConvertToByteArrayAsync(request.ProfilePhoto);
            }


            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "User profile updated successfully." });
        }


        private async Task<byte[]> ConvertToByteArrayAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }






        [HttpDelete("DeleteUserProfile/{id}")]
        public async Task<IActionResult> DeleteUserProfile(int id)
        {

            var userProfile = await _context.UserProfiles.SingleOrDefaultAsync(up => up.Id == id);

            if (userProfile == null)
            {
                return NotFound(new { Status = "Error", Message = $"User profile with id '{id}' does not exist." });
            }

            try
            {
              
                _context.UserProfiles.Remove(userProfile);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = $"Failed to delete user profile: {ex.Message}" });
            }

        
            return Ok(new { Status = "Success", Message = "User profile deleted successfully." });
        }





        //-----------------------------------------------------------------------------------------------------------








        [HttpPost("MedicalHistory{userProfileId}")]
        public async Task<IActionResult> CreateMedicalHistory(int userProfileId, [FromBody] MedicalHistoryRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid medical history data.");
            }


            var userProfile = await _context.UserProfiles.FindAsync(userProfileId);
            if (userProfile == null)
            {
                return NotFound(new { Status = "Error", Message = "User profile not found." });
            }

            var existingMedicalHistory = await _context.MedicalHistories
            .FirstOrDefaultAsync(mh => mh.UserProfileId == userProfileId);

            if (existingMedicalHistory != null)
            {
                return Conflict(new
                {
                    Status = "Error",
                    Message = $"Medical history already exists for this user profile."
                });
            }



            var medicalHistory = new MedicalHistory
            {
                Id = userProfileId,
                Allergies = request.Allergies,
                ChronicConditions = request.ChronicConditions,
                Medications = request.Medications,
                Surgeries = request.Surgeries,
                FamilyHistory = request.FamilyHistory,
                LastCheckupDate = (DateTime)request.LastCheckupDate,
                AdditionalNotes = request.AdditionalNotes,
                UserProfileId = userProfileId
            };

            try
            {
                await _context.MedicalHistories.AddAsync(medicalHistory);
                await _context.SaveChangesAsync();

                var responseMessage = new
                {
                    Status = "Success",
                    Message = "Medical history created successfully.",

                };

                return CreatedAtAction(nameof(GetMedicalHistory), new { id = medicalHistory.Id }, responseMessage);
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? "No inner exception";
                return StatusCode(StatusCodes.Status500InternalServerError, $"Database error: {innerException}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }







        [HttpGet("MedicalHistory{userProfileId}")]
        public async Task<IActionResult> GetMedicalHistory(int userProfileId)
        {

            var userProfile = await _context.UserProfiles.FindAsync(userProfileId);
            if (userProfile == null)
            {
                return NotFound(new { Status = "Error", Message = "User profile not found." });
            }

            var medicalHistory = await _context.MedicalHistories
                .FirstOrDefaultAsync(mh => mh.UserProfileId == userProfileId);

            if (medicalHistory == null)
            {
                return NotFound(new { Status = "Error", Message = "No medical history found for this user profile." });
            }

            return Ok(new
            {
                Status = "Success",
                MedicalHistory = new
                {
                    medicalHistory.Allergies,
                    medicalHistory.ChronicConditions,
                    medicalHistory.Medications,
                    medicalHistory.Surgeries,
                    medicalHistory.FamilyHistory,
                    LastCheckupDate = medicalHistory.LastCheckupDate.ToString("yyyy-MM-dd"),
                    medicalHistory.AdditionalNotes,

                }
            });
        }






        [HttpPut("UpdateMedicalHistory{userProfileId}")]


        public async Task<IActionResult> UpdateMedicalHistory(int userProfileId, [FromBody] MedicalHistoryRequest request)
        {
            var MedicalHistory = await _context.MedicalHistories.FindAsync(userProfileId);
            if (MedicalHistory == null)
            {
                return NotFound(new { Status = "Error", Message = "MedicalHistory not found." });
            }

            MedicalHistory.Allergies = request.Allergies;
            MedicalHistory.ChronicConditions = request.ChronicConditions;
            MedicalHistory.Medications = request.Medications;
            MedicalHistory.Surgeries = request.Surgeries;
            MedicalHistory.FamilyHistory = request.FamilyHistory;
            MedicalHistory.LastCheckupDate = (DateTime)request.LastCheckupDate;
            MedicalHistory.AdditionalNotes = request.AdditionalNotes;

            await _context.SaveChangesAsync();

            return Ok(new { Status = "Success", Message = "MedicalHistory updated successfully." });
        }







        //--------------------------------------------------------------------------------------------------








        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var ForgotPasswordLink = Url.Action(nameof(Resetpassword), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new string[] { user.Email! }, "Forgot Password email link", ForgotPasswordLink!);
                _emailService.SendEmail(message);


                return StatusCode(StatusCodes.Status200OK,
                   new Response { Status = "success", Message = $"Password Changed request is sent on Email{user.Email},Please open your email & click the link" });

            }
            return StatusCode(StatusCodes.Status400BadRequest,
                   new Response { Status = "Error", Message = "Couldn't send link to email, Please try again" });

        }








        [HttpGet("ResetPassword")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Resetpassword(string token, string email)
        {
            var model = new ResetPassword { Token = token, Email = email };

            return Ok(new
            {
                model

            });
        }
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));
            return token;

        }









        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword resetPassword)
        {


            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user != null)
            {
                if (resetPassword.Password != resetPassword.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                    return BadRequest(ModelState);
                }

                var resetPassResult = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
                if (!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return BadRequest(ModelState);
                }

                return Ok(new Response { Status = "success", Message = "Password has been changed" });
            }

            return BadRequest(new Response { Status = "Error", Message = "Couldn't send link to email, Please try again" });
        }


        //----------------------------------------------------------------------------------------


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); 
            return Ok(new { Status = "Success", Message = "Logged out successfully." });
        }

    }
}

