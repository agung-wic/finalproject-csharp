using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PaymentAPI.Models
{
    public class NewRefreshToken
    {
        public int Id {get; set;}
        public string UserId {get; set;}
        public string Token {get; set;}
        #nullable enable
        public string? refreshToken {get; set;}
        #nullable disable
        public string JwtId {get; set;}
        public bool IsUsed {get; set;}
        public bool IsRevorked {get; set;}
        public DateTime AddedDate {get; set;}
        public DateTime ExpiryDate {get; set;}

        [ForeignKey(nameof(UserId))]
        public IdentityUser User {get;set;}
    }
}