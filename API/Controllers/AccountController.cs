using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseAPIController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {

            var user = await _context.Users.Include(p => p.Photos)
                            .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username.ToLower());
            if (user == null) return Unauthorized("Invalid Username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {

            if (await UserExits(registerDTO.Username)) return BadRequest("Username is taken");

            var user = _mapper.Map<AppUser>(registerDTO);

            using var hmac = new HMACSHA512();

            user.UserName = registerDTO.Username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password));
            user.PasswordSalt = hmac.Key;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();


            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                KnownAs = user.KnownAs
            };
        }

        private async Task<bool> UserExits(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}