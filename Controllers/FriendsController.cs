﻿using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSN.Claims;
using MSN.Data;
using MSN.Models;
using SQLitePCL;

namespace MSN.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FriendsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public FriendsController(UserManager<ApplicationUser> userManager, IMapper mapper, DataContext context)
        {
            _userManager = userManager;
            _mapper = mapper;
            _context = context;
            
        }

        [HttpPost("add-friend/{myUsername}/{username}")]
        public async Task<ActionResult<ApplicationUser>> AddFriend(string myUsername, string username)
        {


            var connectedUser = await _userManager.Users.Include(o=>o.Friends).FirstOrDefaultAsync(o=>o.UserName == myUsername);

            var newFriend = await _userManager.FindByNameAsync(username);

            if (newFriend == null) return NotFound();

            connectedUser?.Friends?.Add(newFriend);
            newFriend?.Friends?.Add(connectedUser);

            await _userManager.UpdateAsync(connectedUser);
            await _userManager.UpdateAsync(newFriend);

            return Ok(connectedUser);
        }

        [HttpGet("all-friends")]
        public async Task<ActionResult<List<ApplicationUser>>> GetAllFriends()
        {
            var connectedUserId = User.GetUserId();

            var connectedUser = await _userManager.Users.Include(p=>p.Friends).FirstOrDefaultAsync(o => o.Id == connectedUserId);

            var allFriends = connectedUser?.Friends;

            return Ok(allFriends);
        }
    }
}
