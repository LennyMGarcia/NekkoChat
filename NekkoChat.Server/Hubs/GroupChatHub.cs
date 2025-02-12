﻿using Microsoft.AspNetCore.SignalR;
using NekkoChat.Server.Data;
using NekkoChat.Server.Models;
using System.Security.Claims;
using NekkoChat.Server.Constants.Interfaces;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNet.Identity;
using NekkoChat.Server.Constants;
namespace NekkoChat.Server.Hubs
{
    public class GroupChatHub(ApplicationDbContext context, IServiceProvider serviceProvider) : Hub, iCustomChatHubs
    {
        private readonly ApplicationDbContext _context = context;
        public async Task<Task> SendTypingSignal(string sender_id, string group_id)
        {
            if (string.IsNullOrEmpty(sender_id) ||
                string.IsNullOrEmpty(group_id)) return Task.FromResult(TypedResults.Unauthorized);

            int groupID = Convert.ToInt32(group_id);

            var sender = await _context.AspNetUsers.FindAsync(sender_id);
            var group = await _context.groups.FindAsync(groupID);

            if (sender is null || group is null) return Task.FromResult(TypedResults.Unauthorized);

            IQueryable<Groups_Members> filteredMembers = from m in _context.groups_members select m;
            filteredMembers = filteredMembers.Where((m) => m.group_id == groupID);

            if (!filteredMembers.Any()) return Task.FromResult(TypedResults.Unauthorized);

            List<string> connectionIds = new();

            if (group is null || string.IsNullOrEmpty(group.name)) return Task.FromResult(TypedResults.Unauthorized);

            foreach (var member in filteredMembers)
            {
                using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                {
                    AspNetUsers payload = context.AspNetUsers.Find(member.user_id)!;
                    if (payload is not null && !string.IsNullOrEmpty(payload.ConnectionId))
                    {
                        connectionIds.Add(payload.ConnectionId);
                        await Groups.AddToGroupAsync(payload.ConnectionId, group.name);
                    }
                }
            }

            return Clients.Group(group.name).SendAsync("ReceiveTypingSignal", sender?.Id, $"{sender?.Fname} {sender?.Lname}", groupID, group.name);
        }
        public async Task<Task> SendMessage(string sender_id, string group_id, string msj)
        {
            if (string.IsNullOrEmpty(sender_id) ||
                string.IsNullOrEmpty(msj) || 
                string.IsNullOrEmpty(group_id)) return Task.FromResult(TypedResults.Unauthorized);

            int groupID = Convert.ToInt32(group_id);

            var sender = await _context.AspNetUsers.FindAsync(sender_id);
            var group = await _context.groups.FindAsync(groupID);

            if (sender is null || group is null) return Task.FromResult(TypedResults.Unauthorized);

            IQueryable<Groups_Members> filteredMembers = from m in _context.groups_members select m;
            filteredMembers = filteredMembers.Where((m) => m.group_id == groupID);

            if(!filteredMembers.Any()) return Task.FromResult(TypedResults.Unauthorized);

            List<string> connectionIds = new();

            if (group is null || string.IsNullOrEmpty(group.name)) return Task.FromResult(TypedResults.Unauthorized);

            foreach (var member in filteredMembers)
            {
                using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                {
                    AspNetUsers payload = context.AspNetUsers.Find(member.user_id)!;
                    if(payload is not null && !string.IsNullOrEmpty(payload.ConnectionId))
                    {
                        connectionIds.Add(payload.ConnectionId);
                        await Groups.AddToGroupAsync(payload.ConnectionId, group.name);
                    }
                };
            }

            return Clients.Group(group.name).SendAsync("ReceiveSpecificMessage", sender_id, msj, $"{sender?.Fname} {sender?.Lname}", groupID, group.name);
        }
        public async Task<Task> SendNotificationToUser(NotificationRequest data)
        {
            string receiverConnectionId = "";

            if (!string.IsNullOrEmpty(data.user_id)) return Task.FromResult(TypedResults.Unauthorized);

            var receiver = await _context.AspNetUsers.FindAsync(data.user_id)!;

            if (receiver is not null && !string.IsNullOrEmpty(receiver.ConnectionId))
            {
                receiverConnectionId = receiver.ConnectionId;
                return Clients.Clients(receiverConnectionId).SendAsync("ReceiveNotification", data.user_id);
            }

            return Task.FromResult(TypedResults.Unauthorized);
        }
    }
}
