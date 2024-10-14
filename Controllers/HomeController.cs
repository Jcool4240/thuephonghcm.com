using JcoolDevRoom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JcoolDevRoom.Models;
using System.Diagnostics;

namespace JcoolDevRoom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("cong-tac-vien-cho-thue-phong")]
        public async Task<IActionResult> Index()
        {
            var collaborators = await _context.Collaborators
                .Include(c => c.Rooms)
                .Include(c => c.Districts)
                .OrderByDescending(c => c.Rooms.Count)
                .ToListAsync();

            var totalRooms = await _context.Rooms.CountAsync();
            var totalDistricts = await _context.Districts.CountAsync();

            var collaboratorDistricts = await _context.Collaborators
                .Include(c => c.Districts)
                .ToDictionaryAsync(c => c.CollaboratorId, c => c.Districts.Select(d => d.DistrictName).ToList());

            ViewBag.TotalRooms = totalRooms;
            ViewBag.TotalDistricts = totalDistricts;
            ViewBag.CollaboratorDistricts = collaboratorDistricts;

            return View(collaborators);
        }

        [Route("gioi-thieu-ve-thue-phong-hcm")]
        public IActionResult About()
        {
            return View();
        }

        [Route("lien-he-thue-phong-hcm")]
        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("thue-phong-hcm-tu-choi-truy-cap")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Route("tat-ca-phong-cho-thue")]
        public async Task<IActionResult> AllRooms(int? districtId, int? collaboratorId, string priceRange, string roomType, string sortOrder, int page = 1)
        {
            var viewModel = await GetRoomsViewModel(districtId, collaboratorId, priceRange, roomType, sortOrder, page, 9); // Mặc định là 9 cho trang đầu tiên
            return View(viewModel);
        }

        [HttpGet]
        [Route("tat-ca-phong-cho-thue/loc-phong")]
        public async Task<IActionResult> FilterRooms(int? districtId, int? collaboratorId, string priceRange, string roomType, string sortOrder, int page = 1, int columns = 3)
        {
            int pageSize = columns switch
            {
                1 => 7,
                2 => 8,
                _ => 9
            };

            var viewModel = await GetRoomsViewModel(districtId, collaboratorId, priceRange, roomType, sortOrder, page, pageSize);
            return PartialView("_AllRoomsList", viewModel);
        }

        private async Task<RoomsViewModel> GetRoomsViewModel(int? districtId, int? collaboratorId, string priceRange, string roomType, string sortOrder, int page, int pageSize)
        {
            var allRoomsQuery = _context.Rooms.AsQueryable();

            var filteredQuery = allRoomsQuery;

            if (districtId.HasValue)
            {
                filteredQuery = filteredQuery.Where(r => r.DistrictId == districtId.Value);
            }

            if (collaboratorId.HasValue)
            {
                filteredQuery = filteredQuery.Where(r => r.CollaboratorId == collaboratorId.Value);
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

            switch (sortOrder)
            {
                case "price_asc":
                    filteredQuery = filteredQuery.OrderBy(r => r.PriceRoom);
                    break;
                case "price_desc":
                    filteredQuery = filteredQuery.OrderByDescending(r => r.PriceRoom);
                    break;
                case "newest":
                    filteredQuery = filteredQuery.OrderByDescending(r => r.RoomId);
                    break;
                case "oldest":
                    filteredQuery = filteredQuery.OrderBy(r => r.RoomId);
                    break;
                default:
                    filteredQuery = filteredQuery.OrderByDescending(r => r.RoomId);
                    break;
            }

            var totalFilteredRooms = await filteredQuery.CountAsync();
            var totalRooms = await allRoomsQuery.CountAsync();

            var rooms = await filteredQuery
        .Include(r => r.Collaborator)
        .Include(r => r.District)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

            var districts = await _context.Districts.ToListAsync();
            var collaborators = await _context.Collaborators.ToListAsync();

            var allRooms = await allRoomsQuery.ToListAsync();

            var districtRoomCounts = allRooms
                .GroupBy(r => r.DistrictId)
                .ToDictionary(g => g.Key, g => g.Count());

            var collaboratorRoomCounts = allRooms
                .GroupBy(r => r.CollaboratorId)
                .ToDictionary(g => g.Key, g => g.Count());

            var priceRangeCounts = new Dictionary<string, int>
    {
        {"0-3", 0},
        {"3-5", 0},
        {"5-8", 0},
        {"8-99", 0}
    };

            foreach (var room in allRooms)
            {
                var key = GetPriceRangeKey(room.PriceRoom);
                priceRangeCounts[key]++;
            }

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
                Collaborators = collaborators,
                CurrentDistrict = districtId,
                CurrentCollaborator = collaboratorId,
                PriceRange = priceRange,
                RoomType = roomType,
                SortOrder = sortOrder,
                TotalRooms = allRooms.Count,
                TotalFilteredRooms = totalFilteredRooms,
                CurrentPage = page,
                PageSize = pageSize,
                DistinctRoomTypes = distinctRoomTypes,
                DistrictRoomCounts = districtRoomCounts,
                CollaboratorRoomCounts = collaboratorRoomCounts,
                PriceRangeCounts = priceRangeCounts,
                RoomTypeCounts = roomTypeCounts
            };
        }

        private int GetPageSizeFromUserAgent(string userAgent)
        {
            userAgent = userAgent.ToLower();
            if (userAgent.Contains("mobi"))
            {
                return 7;
            }
            else if (userAgent.Contains("ipad") || (userAgent.Contains("android") && !userAgent.Contains("mobi")))
            {
                return 8;
            }
            else
            {
                return 9;
            }
        }

        private int GetPageSizeFromScreenWidth(int screenWidth)
        {
            if (screenWidth < 576)
            {
                return 7;
            }
            else if (screenWidth < 992)
            {
                return 8;
            }
            else
            {
                return 9;
            }
        }

        private (decimal minPrice, decimal maxPrice) GetPriceRange(string priceRange)
        {
            return priceRange switch
            {
                "0-3" => (0, 3000001),
                "3-5" => (3000000, 5000001),
                "5-8" => (5000000, 8000001),
                "8-99" => (8000000, 99000001),
                _ => (0, decimal.MaxValue)
            };
        }

        private string GetPriceRangeKey(long? price)
        {
            if (price == null) return "0-3";
            if (price < 3000000) return "0-3";
            if (price < 5000000) return "3-5";
            if (price < 8000000) return "5-8";
            return "8-99";
        }
    }
}