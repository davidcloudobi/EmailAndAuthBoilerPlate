using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Data.Entites
{
  public  class ApplicationRole: IdentityRole
    {
        public bool IsActive { get; set; }
       // public virtual ICollection<UserRole> Users { get; set; }
    }
}
