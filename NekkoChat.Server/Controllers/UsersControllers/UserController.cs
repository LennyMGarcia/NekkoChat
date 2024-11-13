using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using Elasticsearch.Net;
using Nest;
using NekkoChat.Server.Data;
using NekkoChat.Server.Models;
using NekkoChat.Server.Constants.Types;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using NekkoChat.Server.Constants;
using static System.Runtime.InteropServices.JavaScript.JSType;
using NekkoChat.Server.Utils;
using NekkoChat.Server.Constants.Interfaces;
using System.Xml.Linq;

namespace NekkoChat.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(
        ILogger<UserController> logger,
        ApplicationDbContext context,
        IMapper mapper,
        IServiceProvider serviceProvider,
        iFriendRequestService friendProvider) : ControllerBase
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly ApplicationDbContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly iFriendRequestService _friendProvider = friendProvider;


        // /User/users?user_id="user_id"
        [HttpGet("users")]
        public async Task<IActionResult> Get([FromQuery] string user_id, [FromQuery] string sender_id)
        {
            if (string.IsNullOrEmpty(sender_id) || string.IsNullOrEmpty(user_id))
            {
                _logger.LogWarning("Sender id Or User Id is null in User - Get Users Friends Route");
            }
            try
            {
                AspNetUsers user = await _context.AspNetUsers.FindAsync(user_id);
                UserDTO userView = _mapper.Map<UserDTO>(user);
                using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                {
                    Friend_List friendsList = ctx.friend_list.Where((c) =>
                    c.sender_id == userView.Id && c.receiver_id == sender_id && c.isAccepted == true
                    ||
                    c.sender_id == sender_id && c.receiver_id == userView.Id && c.isAccepted == true).FirstOrDefault();

                    if (friendsList is not null)
                    {
                        userView.isFriend = (bool)friendsList!.isAccepted;
                    }else
                    {
                        _logger.LogWarning("Friend List is null in User - Get Users Friends Route");
                    }
                }
                using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                {
                    bool isSender = ctx.friend_list.Any((f) => f.sender_id == userView.Id && f.isAccepted == false);
                    userView.isSender = (bool)isSender || false;
                }
                return Ok(new ResponseDTO<UserDTO> { SingleUser = userView, StatusCode = 200 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Get Users Friends Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Get Users Friends Route");
                }
                return StatusCode(500, new ResponseDTO<UserDTO> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ErrorMessages.ErrorMessage, StatusCode = 500 });
            }
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserByName([FromQuery] string name, [FromQuery] string user_id)
        {
            if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(user_id))
            {
                _logger.LogWarning("Name Or User Id is null in User - Get User By Name Route");
            }

            try
            {
                List<UserDTO> searchRes = new();

                IQueryable<AspNetUsers> results = from c in _context.AspNetUsers select c;
                results = results.Where((c) => c.UserName!.ToLower().Contains(name.ToLower()));

                
                foreach (var search in results)
                {
                    UserDTO userView = _mapper.Map<UserDTO>(search);

                    bool canSend = true;
                    bool alreadySent = false;


                    using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                    {

                        bool alreadyFriend = await ctx.friend_list.AnyAsync((f) => f.sender_id == search.Id && f.receiver_id == user_id && f.isAccepted == true);
                        bool alreadyFriend2 = await ctx.friend_list.AnyAsync((f) => f.sender_id == user_id && f.receiver_id == search.Id && f.isAccepted == true);

                        if (alreadyFriend || alreadyFriend2)
                        {
                            canSend = false;
                        }
                    }

                    using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                    {

                        bool alreadyFriend = await ctx.friend_list.AnyAsync((f) => f.sender_id == search.Id && f.receiver_id == user_id && f.isAccepted == false);
                        bool alreadyFriend2 = await ctx.friend_list.AnyAsync((f) => f.sender_id == user_id && f.receiver_id == search.Id && f.isAccepted == false);

                        if (alreadyFriend || alreadyFriend2)
                        {
                            alreadySent = true;
                        }
                    }
                    userView.canSendRequest = canSend;
                    userView.alreadyRequest = alreadySent;


                    searchRes.Add(userView);
                }

                return Ok(new ResponseDTO<UserDTO> { User = searchRes, StatusCode = 200 });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Get User Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Get User Route");
                }
                return StatusCode(500, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.ErrorMessage, StatusCode = 500 });
            }

        }
        [HttpGet("friends")]
        public IActionResult GetUserFriends([FromQuery] string user_id, [FromQuery] string operation)
        {
            try
            {
                List<UserDTO> friendsList = new();

                if (operation == "requests")
                {
                    IQueryable<Friend_List> friends = from fr in _context.friend_list select fr;
                    friends = friends.Where((fr) => fr.receiver_id == user_id && fr.isAccepted == false);
                    if (friends.Any())
                    {
                        foreach (var friend in friends)
                        {
                            using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                            {
                                AspNetUsers user = ctx.AspNetUsers.Find(friend.sender_id);

                                if (user is null) return StatusCode(403, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.NoExist, StatusCode = 500 });

                                UserDTO userView = _mapper.Map<UserDTO>(user);
                                userView.isFriend = false;
                                userView.isSender = true;
                                userView.canSendRequest = false;
                                userView.alreadyRequest = true;

                                friendsList.Add(userView);

                            }
                        }
                    }
                    IQueryable<Friend_List> altFriends = _context.friend_list.Where((fr) => fr.sender_id == user_id && fr.isAccepted == false);
                    if (altFriends.Any())
                    {
                        foreach (var altFriend in altFriends)
                        {
                            using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                            {
                                AspNetUsers user = ctx.AspNetUsers.Find(altFriend.receiver_id);

                                if (user is null) return StatusCode(403, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.NoExist, StatusCode = 500 });

                                UserDTO userView = _mapper.Map<UserDTO>(user);
                          
                                userView.isFriend = false;
                                userView.isSender = false;
                                userView.canSendRequest = false;
                                userView.alreadyRequest = true;

                                friendsList.Add(userView);

                            }
                        }
                    }
                }
                else
                {
                    IQueryable<Friend_List> friends = from fr in _context.friend_list select fr;
                    friends = friends.Where((fr) => fr.receiver_id == user_id && fr.isAccepted == true);
                    if (friends.Any())
                    {
                        foreach (var friend in friends)
                        {
                            using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                            {
                                AspNetUsers user = ctx.AspNetUsers.Find(friend.sender_id);

                                if (user is null) return StatusCode(403, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.NoExist, StatusCode = 500 });

                                UserDTO userView = _mapper.Map<UserDTO>(user);
                            
                                userView.isFriend = true;
                                userView.isSender = true;
                                userView.canSendRequest = false;

                                friendsList.Add(userView);

                            }
                        }
                    }

                    IQueryable<Friend_List> altFriends2 = _context.friend_list.Where((fr) => fr.sender_id == user_id && fr.isAccepted == true);
                    if (altFriends2.Any())
                    {
                        foreach (var altFriend in altFriends2)
                        {
                            using (var ctx = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                            {
                                AspNetUsers user = ctx.AspNetUsers.Find(altFriend.receiver_id);

                                if (user is null) return StatusCode(403, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.NoExist, StatusCode = 500 });

                                UserDTO userView = _mapper.Map<UserDTO>(user);
                      
                                userView.isFriend = true;
                                userView.isSender = false;
                                userView.canSendRequest = false;


                                friendsList.Add(userView);
                            }
                        }
                    }
                }
                return Ok(new ResponseDTO<UserDTO> { Success = true, User = friendsList, StatusCode = 200 });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In Notification - Read Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In Notification - Read Route");
                }
                return StatusCode(500, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ex.Message, StatusCode = 500 });
            }
        }

        [HttpPost("manage/friendrequest/send")]
        public async Task<IActionResult> PostFriendRequest([FromBody] UserRequest data)
        {
            if (string.IsNullOrEmpty(data.receiver_id) || string.IsNullOrEmpty(data.sender_id))
            {
                _logger.LogWarning("Receiver id OR Sender id is Null In User - Manage Send Friend Request Route");
                return BadRequest(new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ErrorMessages.MissingValues, StatusCode = 400 });
            }

            AspNetUsers sender = await _context.AspNetUsers.FindAsync(data.sender_id);
            AspNetUsers receiver = await _context.AspNetUsers.FindAsync(data.receiver_id);

            if (sender == null || receiver == null) return StatusCode(403, new ResponseDTO<Friend_List> { Success = false, Message = "User " + ErrorMessages.NoExist, Error = ErrorMessages.Invalid + " User." });

            bool alreadyFriend = await _context.friend_list.AnyAsync((f) => f.sender_id == data.sender_id && f.receiver_id == data.receiver_id && f.isAccepted == true);
            bool alreadyFriend2 = await _context.friend_list.AnyAsync((f) => f.sender_id == data.receiver_id && f.receiver_id == data.sender_id && f.isAccepted == true);
            if (alreadyFriend || alreadyFriend2) return StatusCode(403, new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.FriendAlready, Error = ErrorMessages.FriendAlready });


            bool friendRequestAlready = await _context.friend_list.AnyAsync((f) => f.sender_id == data.sender_id && f.receiver_id == data.receiver_id && f.isAccepted == false);
            bool friendRequestAlready2 = await _context.friend_list.AnyAsync((f) => f.sender_id == data.receiver_id && f.receiver_id == data.sender_id && f.isAccepted == false);
            if (friendRequestAlready || friendRequestAlready2) return StatusCode(403, new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.FriendRequestAlready, Error = ErrorMessages.FriendRequestAlready });

            try
            {
                Friend_List friendReq = new Friend_List
                {
                    sender_id = sender.Id,
                    receiver_id = receiver.Id,
                    isAccepted = false
                };
                await _context.friend_list.AddAsync(friendReq);
                await _context.SaveChangesAsync();
                return Ok(new ResponseDTO<UserDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Send Friend Request Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Read Route");
                }
                return StatusCode(500, new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ex.Message });
            }

        }

        [HttpPatch("manage/friendrequest/response")]
        public async Task<IActionResult> PatchFriendRequest([FromBody] UserRequest data)
        {
            if (string.IsNullOrEmpty(data.receiver_id) || string.IsNullOrEmpty(data.sender_id))
            {
                _logger.LogWarning("Receiver id OR Sender id is Null In User - Manage Friend Request Route");
                return BadRequest(new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ErrorMessages.MissingValues, StatusCode = 400 });
            }

            try
            {
                Friend_List friendReq = await _context.friend_list.FirstOrDefaultAsync((fr) =>
                fr.sender_id == data.sender_id && fr.receiver_id == data.receiver_id
                ||
                fr.sender_id == data.receiver_id && fr.receiver_id == data.sender_id);

                if (friendReq is null)
                {
                    _logger.LogWarning("Friend Request Not Found In User - Manage Friend Request Route");
                }

                if (data.operation == "accept" && friendReq!.id > 0)
                {
                    friendReq.isAccepted = true;
                    _context.friend_list.Update(friendReq);
                    await _friendProvider.IncreaseFriendCount(data.sender_id);
                    await _friendProvider.IncreaseFriendCount(data.receiver_id);
                }
                else if (data.operation == "decline" && friendReq!.id > 0)
                {
                    friendReq.isAccepted = false;
                    _context.friend_list.Update(friendReq);
                }
                else if (data.operation == "remove" && friendReq!.id > 0)
                {
                    friendReq.isAccepted = false;
                    _context.friend_list.Update(friendReq);
                    _context.RemoveRange(friendReq);

                    await _friendProvider.DecreaseFriendCount(data.sender_id);
                    await _friendProvider.DecreaseFriendCount(data.receiver_id);
                }
                else
                {
                    _logger.LogWarning("Invalid Operation In User - Manage Friend Request Route");
                    return BadRequest(new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ErrorMessages.ErrorRegular, StatusCode = 400 });
                }

                await _context.SaveChangesAsync();
                return Ok(new ResponseDTO<UserDTO>());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Manage Friend Request Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Manage Friend Request Route");
                }
                return StatusCode(500, new ResponseDTO<Friend_List> { Success = false, Message = ErrorMessages.ErrorMessage, Error = ErrorMessages.ErrorMessage, StatusCode = 500 });
            }
        }

        [HttpPost("manage/connectionid")]
        public async Task<IActionResult> Post([FromBody] UserRequest data)
        {
            if (string.IsNullOrEmpty(data.user_id) || string.IsNullOrEmpty(data.connectionid))
            {
                _logger.LogWarning("User id OR Connection id is Null In User - Manage Connection Id Route");
            }

            try
            {
                AspNetUsers user = await _context.AspNetUsers.FindAsync(data.user_id);
                user!.ConnectionId = data.connectionid;
                user!.Status = ValidStatus.Valid_Status.available;
                _context.AspNetUsers.UpdateRange(user);
                await _context.SaveChangesAsync();
                return Ok(new ResponseDTO<UserDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Manage Connection Id Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Manage Connection Id Route");
                }
                return StatusCode(500, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.ErrorMessage, StatusCode = 500 });
            }
        }

        [HttpPut("manage/status")]
        public async Task<IActionResult> Put([FromBody] UserRequest data, [FromQuery] int status)
        {
            if(status < 0)
            {
                _logger.LogWarning("Staus is Invalid In User - Manage Status Route");
            }

            if (string.IsNullOrEmpty(data.user_id))
            {
                _logger.LogWarning("User Id is Null In User - Manage Status Route");
            }

            try
            {
                AspNetUsers user = await _context.AspNetUsers.FindAsync(data.user_id);
                user!.Status = (ValidStatus.Valid_Status)status;
                _context.AspNetUsers.UpdateRange(user);
                await _context.SaveChangesAsync();
                return Ok(new ResponseDTO<UserDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + " In User - Manage Status Route");
                if (ex.InnerException is not null)
                {
                    _logger.LogError(ex?.InnerException?.Message + " In User - Manage Status Route");
                }
                return StatusCode(500, new ResponseDTO<AspNetUsers> { Success = false, Message = ErrorMessages.ErrorRegular, Error = ErrorMessages.ErrorMessage, StatusCode = 500 });
            }
        }
    }
}
