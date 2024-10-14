using System;
using System.Collections.Generic;

namespace JcoolDevRoom.Models
{
    public partial class Collaborator
    {
        public Collaborator()
        {
            Admins = new HashSet<Admin>();
            CollaboratorImages = new HashSet<CollaboratorImage>();
            Rooms = new HashSet<Room>();
            Districts = new HashSet<District>();
        }

        public int CollaboratorId { get; set; }
        public string? FullnameCollaborator { get; set; }
        public string? CollaboratorCode { get; set; }
        public string? SloganCollaborator { get; set; }
        public string? LabelCollaborator { get; set; }
        public string? ServiceCollaborator { get; set; }
        public string? AvatarCollaborator { get; set; }
        public string? FacebookCollaborator { get; set; }
        public string? MessengerCollaborator { get; set; }
        public string? ZaloCollaborator { get; set; }
        public string? TiktokCollaborator { get; set; }
        public string? PhoneNumberCollaborator { get; set; }
        public string? CommitCollaborator { get; set; }

        public virtual ICollection<Admin> Admins { get; set; }
        public virtual ICollection<CollaboratorImage> CollaboratorImages { get; set; }
        public virtual ICollection<Room> Rooms { get; set; }

        public virtual ICollection<District> Districts { get; set; }
    }
}
