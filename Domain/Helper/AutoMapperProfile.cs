using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Data.Entites;
using Domain.DTO.Request;
using Domain.DTO.Response;

namespace Domain.Helper
{
   public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
           // CreateMap<ApplicationUser, AccountResponse>();

            CreateMap<ApplicationUser, AuthenticateResponse>().ReverseMap();

            CreateMap<RegisterRequest, ApplicationUser>().ReverseMap();

            //CreateMap<CreateRequest, Account>();

            //CreateMap<UpdateRequest, Account>()
            //    .ForAllMembers(x => x.Condition(
            //        (src, dest, prop) =>
            //        {
            //            // ignore null & empty string properties
            //            if (prop == null) return false;
            //            if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

            //            // ignore null role
            //            if (x.DestinationMember.Name == "Role" && src.Role == null) return false;

            //            return true;
            //        }
            //    ));
        }
    }
}
