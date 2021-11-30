using System.ComponentModel.DataAnnotations;
namespace PaymentAPI.Models
{
    public class ItemData
    {
        [Key]
        public int id {get; set;}
        [Required]
        public string cardOwnerName {get; set;}
        [Required]
        public string cardNumber {get; set;}
        [Required]
        public string expirationDate {get; set;}
        [Required]
        public string securityCode {get; set;}
    }
}