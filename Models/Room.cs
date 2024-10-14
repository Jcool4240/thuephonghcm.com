using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class Room
    {
        public Room()
        {
            RoomImages = new HashSet<RoomImage>();
        }

        public int RoomId { get; set; }
        public string? NameRoom { get; set; }
        public string? MainImageRoom { get; set; }
        public string? TitleRoom { get; set; }
        public string? DescriptionRoom { get; set; }
        public long? PriceRoom { get; set; }
        public string? AddressRoom { get; set; }
        public string? TypeRoom { get; set; }
        public string? CapacityRoom { get; set; }
        public string? LabelRoom { get; set; }
        public string? NoteRoom { get; set; }
        public string? StatusRoom { get; set; }
        public int? DistrictId { get; set; }
        public int? CollaboratorId { get; set; }

        public virtual Collaborator? Collaborator { get; set; }
        public virtual District? District { get; set; }
        public virtual ICollection<RoomImage> RoomImages { get; set; }
    }
}
