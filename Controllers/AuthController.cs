using Kulturformidling_api.Data.Model;
using Kulturformidling_api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Kulturformidling_api.dtoModels;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace Kulturformidling_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        public AuthController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<User>> registerUser(NewUserDto request)
        {
            //sjekker om brukar med oppgitt email addresse allerie finst i databasen
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                //returner error vis email addresse allerie er i bruk
                return BadRequest("Brukar fints allerie.");
            }

            //crypter oppgitt passord
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            //mapper dto(data tranfer object) til context model
            var user = new User();

            user.Email = request.Email;
            user.Name = request.Name;
            user.Phone = request.Phone;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;


            //oppretter ny brukar i databasen
            _context.Users.Add(user);
            _context.SaveChanges();

            AutoAssignCustomerRole(user);

            return Ok(user);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            var users = _context.Users.ToList();

            var user = users.FirstOrDefault(users => users.Email == request.Email);
            //sjekker om brukaren finst
            if (user == null)
            {
                return BadRequest("Brukernamn eller passord er feil.");
            }

            //sjekker om passordet er rett
            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Brukernamn eller passord er feil.");
            }

            //lager ein JWT token som blir brukt til å autentisere brukaren 
            string token = CreateToken(user);
            return Ok(token);

        }

        [HttpGet("UserClames")]
        [Authorize]
        public async Task<ActionResult<UserClamesDto>> getUserClames()
        {
            var name = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            var claims = User.Identity;

            var UserClames = new UserClamesDto();

            UserClames.UserName = name;
            UserClames.UserEmail = email;
            UserClames.UserRoleName = role;

            return Ok(UserClames);
        }

        [HttpGet("GetRoles")]
        [Authorize]
        public async Task<ActionResult<List<Role>>> GetRoles()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);

            var roles = _context.Roles.Where(r => r.Name != role).ToList();

            return Ok(roles);
        }

        [HttpPost("RoleRequest")]
        [Authorize]
        public async Task<ActionResult<RoleRequest>> RequestNewRole(RoleRequestDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == User.FindFirstValue(ClaimTypes.Email));
            var role = _context.Roles.FirstOrDefault(r =>  r.Id == request.roleId);

            var roleRequest = new RoleRequest();

            roleRequest.User = user;
            roleRequest.Role = role;

            _context.RolesRequest.Add(roleRequest);
            _context.SaveChanges();

            return Ok(roleRequest);
        }

        [HttpGet("NewRoleRequest")]
        [Authorize(Policy = "Administrator")]
        public async Task<ActionResult<List<UserRequestDto>>> getNewRoleRequest()
        {
            var roleRequest = _context.RolesRequest.Where(r => r.DateAccepted == null).ToList();
            var userRequest = new List<UserRequestDto>();

            foreach (var r in roleRequest)
            {
                var request = new UserRequestDto();
                request.RequestId = r.Id;
                request.UserName = r.User.Name;
                request.RoleName = r.Role.Name;
                userRequest.Add(request);
            }

            return Ok(userRequest);
        }

        [HttpPost("AcceptRoleRequest")]
        [Authorize(Policy = "Administrator")]
        public async Task<ActionResult<UserRequestDto>> acceptRequestRole(UserRequestDto request)
        {
            var roleRequest = _context.RolesRequest.FirstOrDefault(r => r.Id == request.RequestId);
            var currentRole = _context.RolesRequest.Where(r => r.User == roleRequest.User && r.DateAccepted != null).ToList();
            foreach (var r in currentRole)
            {
                _context.RolesRequest.Remove(r);
            }
            roleRequest.DateAccepted = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(roleRequest);
        }

        [HttpDelete("DenyRoleRequest/{id:int}")]
        [Authorize(Policy = "Administrator")]
        public async Task<ActionResult<RoleRequest>> denyRequestRole(int id)
        {
            var roleRequest = _context.RolesRequest.FirstOrDefault(r => r.Id == id);

            _context.RolesRequest.Remove(roleRequest);

            _context.SaveChanges();

            return Ok(roleRequest);
        }

        private void AutoAssignCustomerRole(User user)
        {
            var roleRequest = new RoleRequest();
            roleRequest.User = user;
            roleRequest.Role = _context.Roles.FirstOrDefault(role => role.Name == "Kunde");
            roleRequest.DateAccepted = DateTime.UtcNow;

            _context.RolesRequest.Add(roleRequest);
            _context.SaveChanges();
        }

        private string CreateToken(User user)
        {
            var userRole = _context.RolesRequest.FirstOrDefault(u => u.User == user && u.DateAccepted != null).Role;
            //definerer data som skal lagrast i token
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, userRole.Name),
            };

            //henter Token frå appsettins.json og bruker det som ein kryptering nøkkel
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration["Token"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(3),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                //fungerer som ein slaks oppskrift på korleis passordet har blitt kryptert
                passwordSalt = hmac.Key;
                //deller opp passordet i bytes og reknar ut ein hash
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                //bruker salt til å krypter passordet og sammanliknar det med det krypterte passordet i databasen
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
    }
}

