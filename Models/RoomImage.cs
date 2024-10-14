using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class RoomImage
    {
        public int ImageId { get; set; }
        public int? RoomId { get; set; }
        public string? ImageUrl { get; set; }

        public virtual Room? Room { get; set; }
    }
}
