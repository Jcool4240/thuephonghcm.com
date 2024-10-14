using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class CollaboratorImage
    {
        public int ImageId { get; set; }
        public int? CollaboratorId { get; set; }
        public string? ImageUrl { get; set; }

        public virtual Collaborator? Collaborator { get; set; }
    }
}
