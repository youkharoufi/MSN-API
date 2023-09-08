﻿using AutoMapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MSN.Data;
using MSN.Models;
using MSN.Services;
using MSN.Token;

namespace MSN.Controllers
{
    [Route("[controller]")]
    [ApiController]

    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly DataContext _context;
        private readonly IEmailSender _emailSender;
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService, IMapper mapper, IWebHostEnvironment hostEnvironment, DataContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;   
            _mapper = mapper;
            _hostEnvironment = hostEnvironment;
            _context = context;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApplicationUser>> Register([FromForm]RegisterUser registerUser)
        {

            if(registerUser.File == null) {

                return NotFound("No file uploaded");
            }


                string wwwRootPath = _hostEnvironment.WebRootPath;

                MemoryStream memoryStream = new MemoryStream();
                registerUser.File.OpenReadStream().CopyTo(memoryStream);
                string photoUrl = Convert.ToBase64String(memoryStream.ToArray());

                string filename = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"images\users");
                var extension = Path.GetExtension(registerUser.File.FileName);


                Uri domain = new Uri(Request.GetDisplayUrl());



                using (var fileStreams = new FileStream(Path.Combine(uploads, filename + extension), FileMode.Create))
                {
                    registerUser.File.CopyTo(fileStreams);
                }

                photoUrl = domain.Scheme + "://" + domain.Host + (domain.IsDefaultPort ? "" : ":" + domain.Port) + "/images/users/" + filename + extension;


            


            var newUser = new ApplicationUser
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email,
                Role = registerUser.Role,
                Token = "",
                PhotoUrl = photoUrl,


            };

            var result = await _userManager.CreateAsync(newUser, registerUser.Password);

            if(result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, newUser.Role);
             
                var tof = new Photo
                {
                    Url = photoUrl,
                    UserId = newUser.Id,
                };



                _context.Photos.Add(tof);
                await _context.SaveChangesAsync();

                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

                var message = new Message(new string[] { newUser.Email }, "Confirmation email link", registerUser.Link + emailConfirmationToken);

                await _emailSender.SendEmailAsync(message);

                var result2 = await _signInManager.CheckPasswordSignInAsync(newUser, registerUser.Password, false);

                if (!result2.Succeeded) return BadRequest("Invalid Password");

                newUser.Token = await _tokenService.GenerateToken(newUser);

                return Ok(newUser);
            }
            else
            {
              
              return BadRequest(result.Errors.Select(e => e.Description));
   
            }

        }


        [HttpPost("login")]
        public async Task<ActionResult<ApplicationUser>> Login(LoginUser loginUser)
        {
            var userFromDb = await _userManager.Users.FirstOrDefaultAsync(i=> i.Email == loginUser.UserNameOrEmail);



            if (userFromDb == null)
            {
                userFromDb = await _userManager.Users.FirstOrDefaultAsync(i => i.UserName == loginUser.UserNameOrEmail);

                var result = await _signInManager.CheckPasswordSignInAsync(userFromDb, loginUser.Password, false);

                if (!result.Succeeded) return BadRequest("Invalid Password");

                userFromDb.Token = await _tokenService.GenerateToken(userFromDb);

                return Ok(userFromDb);
            }
            else if (userFromDb != null) 
            {

                var result = await _signInManager.CheckPasswordSignInAsync(userFromDb, loginUser.Password, false);

                if (!result.Succeeded) return BadRequest("Invalid Password");

                userFromDb.Token = await _tokenService.GenerateToken(userFromDb);

                return Ok(userFromDb);

            }
            else
            {
                return BadRequest("Invalid username or email");

            }


        }


    }
}