using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class District
    {
        public District()
        {
            Rooms = new HashSet<Room>();
            Collaborators = new HashSet<Collaborator>();
        }

        public int DistrictId { get; set; }
        public string? DistrictName { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }

        public virtual ICollection<Collaborator> Collaborators { get; set; }
    }
}
