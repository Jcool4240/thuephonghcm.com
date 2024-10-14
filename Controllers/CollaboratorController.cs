using JcoolDevRoom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JcoolDevRoom.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace JcoolDevRoom.Controllers
{
    public class FeedbackModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Message { get; set; }
    }

    [Route("{collaboratorCode}")]
    public class CollaboratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public CollaboratorController(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        [Route("")]
        [Route("home")]
        public async Task<IActionResult> Home(string collaboratorCode)
        {
            var collaborator = await _context.Collaborators
                .Include(c => c.Districts)
                .Include(c => c.CollaboratorImages)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound($"Không tìm thấy cộng tác viên với mã: {collaboratorCode}");
            }

            var districtRoomCounts = await _context.Rooms
                .Where(r => r.CollaboratorId == collaborator.CollaboratorId)
                .GroupBy(r => r.DistrictId)
                .Select(g => new { DistrictId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DistrictId, x => x.Count);

            foreach (var district in collaborator.Districts)
            {
                if (!districtRoomCounts.ContainsKey(district.DistrictId))
                {
                    districtRoomCounts[district.DistrictId] = 0;
                }
            }

            ViewBag.DistrictRoomCounts = districtRoomCounts;

            return View(collaborator);
        }

        [Route("rooms")]
        public async Task<IActionResult> Rooms(string collaboratorCode, int? districtId, string priceRange, string roomType, string sortOrder, int page = 1)
        {
            var viewModel = await GetRoomsViewModel(collaboratorCode, districtId, priceRange, roomType, sortOrder, page, 9);
            return View(viewModel);
        }

        [HttpGet]
        [Route("get-filtered-rooms")]
        public async Task<IActionResult> GetFilteredRooms(string collaboratorCode, int? districtId, string priceRange, string roomType, string sortOrder, int page = 1, int columns = 3)
        {
            int pageSize = columns switch
            {
                1 => 7,
                2 => 8,
                _ => 9
            };

            var viewModel = await GetRoomsViewModel(collaboratorCode, districtId, priceRange, roomType, sortOrder, page, pageSize);
            return PartialView("_RoomsList", viewModel);
        }

        private async Task<RoomsViewModel> GetRoomsViewModel(string collaboratorCode, int? districtId, string priceRange, string roomType, string sortOrder, int page, int pageSize)
        {
            var collaborator = await _context.Collaborators
                .Include(c => c.Districts)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                throw new Exception($"Không tìm thấy cộng tác viên với mã: {collaboratorCode}");
            }

            var allRoomsQuery = _context.Rooms.Where(r => r.CollaboratorId == collaborator.CollaboratorId);

            var filteredQuery = allRoomsQuery;

            if (districtId.HasValue)
            {
                filteredQuery = filteredQuery.Where(r => r.DistrictId == districtId.Value);
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                var (minPrice, maxPrice) = GetPriceRange(priceRange);
                filteredQuery = filteredQuery.Where(r => r.PriceRoom >= minPrice && r.PriceRoom < maxPrice);
            }

            if (!string.IsNullOrEmpty(roomType))
            {
                filteredQuery = filteredQuery.Where(r => r.TypeRoom == roomType);
            }

            filteredQuery = ApplySorting(filteredQuery, sortOrder);

            var totalFilteredRooms = await filteredQuery.CountAsync();
            var totalRooms = await allRoomsQuery.CountAsync();

            var rooms = await filteredQuery
                .Include(r => r.Collaborator)
                .Include(r => r.District)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var districts = collaborator.Districts.ToList();

            var allRooms = await allRoomsQuery.ToListAsync();

            var districtRoomCounts = allRooms
                .GroupBy(r => r.DistrictId)
                .ToDictionary(g => g.Key, g => g.Count());

            var priceRangeCounts = GetPriceRangeCounts(allRooms);

            var roomTypeCounts = allRooms
                .GroupBy(r => r.TypeRoom ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var distinctRoomTypes = allRooms
                .Select(r => r.TypeRoom)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            return new RoomsViewModel
            {
                Rooms = rooms,
                Districts = districts,
                CurrentDistrict = districtId,
                PriceRange = priceRange,
                RoomType = roomType,
                SortOrder = sortOrder,
                TotalRooms = totalRooms,
                TotalFilteredRooms = totalFilteredRooms,
                CurrentPage = page,
                PageSize = pageSize,
                DistinctRoomTypes = distinctRoomTypes,
                DistrictRoomCounts = districtRoomCounts,
                PriceRangeCounts = priceRangeCounts,
                RoomTypeCounts = roomTypeCounts
            };
        }

        private IQueryable<Room> ApplySorting(IQueryable<Room> query, string sortOrder)
        {
            return sortOrder switch
            {
                "price_asc" => query.OrderBy(r => r.PriceRoom),
                "price_desc" => query.OrderByDescending(r => r.PriceRoom),
                "newest" => query.OrderByDescending(r => r.RoomId),
                "oldest" => query.OrderBy(r => r.RoomId),
                _ => query.OrderByDescending(r => r.RoomId),
            };
        }

        private (long minPrice, long maxPrice) GetPriceRange(string priceRange)
        {
            return priceRange switch
            {
                "0-3" => (0, 3000000),
                "3-5" => (3000000, 5000000),
                "5-8" => (5000000, 8000000),
                "8-99" => (8000000, long.MaxValue),
                _ => (0, long.MaxValue)
            };
        }

        private Dictionary<string, int> GetPriceRangeCounts(List<Room> rooms)
        {
            var priceRangeCounts = new Dictionary<string, int>
            {
                {"0-3", 0},
                {"3-5", 0},
                {"5-8", 0},
                {"8-99", 0}
            };

            foreach (var room in rooms)
            {
                var key = GetPriceRangeKey(room.PriceRoom);
                priceRangeCounts[key]++;
            }

            return priceRangeCounts;
        }

        private string GetPriceRangeKey(long? price)
        {
            if (price == null) return "0-3";
            if (price < 3000000) return "0-3";
            if (price < 5000000) return "3-5";
            if (price < 8000000) return "5-8";
            return "8-99";
        }

        [Route("room-details/{id}")]
        public async Task<IActionResult> RoomDetails(string collaboratorCode, int id)
        {
            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound("Collaborator not found");
            }

            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .Include(r => r.Collaborator)
                .Include(r => r.District)
                .FirstOrDefaultAsync(r => r.RoomId == id && r.CollaboratorId == collaborator.CollaboratorId);

            if (room == null)
            {
                return NotFound("Room not found");
            }

            ViewBag.ShowMessengerChat = true;

            return View(room);
        }

        [Route("about")]
        public async Task<IActionResult> About(string collaboratorCode)
        {
            var collaborator = await _context.Collaborators
                .Include(c => c.Districts)
                .Include(c => c.Rooms)
                .Include(c => c.CollaboratorImages)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound($"Không tìm thấy cộng tác viên với mã: {collaboratorCode}");
            }

            return View(collaborator);
        }

        [HttpPost]
        [Route("submit-feedback")]
        public async Task<IActionResult> SubmitFeedback(string collaboratorCode, [FromBody] FeedbackModel feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var feedbackData = new
            {
                userName = feedback.Name,
                email = feedback.Email,
                phone = feedback.Phone,
                content = feedback.Message,
                collaborator_code = collaboratorCode
            };

            var json = JsonSerializer.Serialize(feedbackData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _clientFactory.CreateClient();
            var response = await client.PostAsync("https://66acec7ef009b9d5c733da23.mockapi.io/mailbox", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { message = "Góp ý của bạn đã được gửi thành công!" });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { message = "Có lỗi xảy ra khi gửi góp ý." });
            }
        }
    }
}