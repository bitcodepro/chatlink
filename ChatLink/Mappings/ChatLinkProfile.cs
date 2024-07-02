using AutoMapper;
using ChatLink.Models.DTOs;
using ChatLink.Models.Models;

namespace ChatLink.Mappings;

public class ChatLinkProfile : Profile
{
    public ChatLinkProfile()
    {
        CreateMap<User, UserDto>();
    }
}
