﻿using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace MSN.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; }

        public string Token { get; set; }

        public string PhotoUrl { get; set; }

        public List<ApplicationUser> Friends { get; set; } = new List<ApplicationUser>();

        [JsonIgnore]
        public List<ChatMessage> MessagesSent { get; set; }

        [JsonIgnore]
        public List<ChatMessage> MessagesRecieved { get; set; }

        
        public List<FriendRequest> FriendRequests { get; set; } = new List<FriendRequest>();

    }
}
