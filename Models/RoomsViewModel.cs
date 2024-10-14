namespace JcoolDevRoom.Models
{
    public class RoomsViewModel
    {
        public List<Room> Rooms { get; set; }
        public List<District> Districts { get; set; }
        public List<Collaborator> Collaborators { get; set; }
        public int? CurrentCollaborator { get; set; }
        public int? CurrentDistrict { get; set; }
        public string PriceRange { get; set; }
        public string RoomType { get; set; }
        public int TotalRooms { get; set; }
        public int TotalFilteredRooms { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string SortOrder { get; set; }
        public List<string> DistinctRoomTypes { get; set; }
        public Dictionary<int?, int> DistrictRoomCounts { get; set; }
        public Dictionary<int?, int> CollaboratorRoomCounts { get; set; }
        public Dictionary<string, int> PriceRangeCounts { get; set; }
        public Dictionary<string, int> RoomTypeCounts { get; set; }
    }
}
