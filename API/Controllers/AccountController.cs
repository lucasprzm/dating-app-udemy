using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  public class AccountController : BaseApiController
  {
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;
    public UserManager<AppUser> _userManager { get; }
    public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper)
    {
      _userManager = userManager;
      _tokenService = tokenService;
      _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDTO>> Register(RegisterDto registerDto)
    {
      if (await UserExists(registerDto.Username))
      {
        return BadRequest("Username is taken");
      }

      var user = _mapper.Map<AppUser>(registerDto);
    
      user.UserName = registerDto.Username.ToLower();
      
      var result = await _userManager.CreateAsync(user, registerDto.Password);

      if(!result.Succeeded) return BadRequest(result.Errors);

      var roleResult = await _userManager.AddToRoleAsync(user, "Member");

      if(!roleResult.Succeeded) return BadRequest(result.Errors);

      return new UserDTO
      {
        Username = user.UserName,
        Token = await _tokenService.CreateToken(user),
        KnownAs = user.KnownAs,
        Gender = user.Gender
      };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDto loginDto)
    {
      var user = await _userManager.Users.Include(x => x.Photos).SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
      
      if (user == null)
      {
        return Unauthorized("Invalid username");
      }

      var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

      if(!result) return Unauthorized("Invalid password");
      
      return new UserDTO
      {
        Username = user.UserName,
        Token = await _tokenService.CreateToken(user),
        PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
        KnownAs = user.KnownAs,
        Gender = user.Gender
      };
    }
    private async Task<bool> UserExists(string username)
    {
      return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
    }
  }
}