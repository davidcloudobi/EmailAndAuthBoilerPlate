using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.DTO.Response
{
  public  class UserResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsVerified { get; set; }
    }
}
