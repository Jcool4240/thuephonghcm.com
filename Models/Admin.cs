using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class Admin
    {
        public int AdminId { get; set; }
        public string? UsernameAdmin { get; set; }
        public string? PasswordAdmin { get; set; }
        public int? CollaboratorId { get; set; }

        public virtual Collaborator? Collaborator { get; set; }
    }
}
