using JcoolDevRoom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JcoolDevRoom.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.Data.SqlClient;

namespace JcoolDevRoom.Controllers
{
    [Authorize]
    [Route("{collaboratorCode}/manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const int PageSize = 9;
        public ManagerController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private string GetCollaboratorCodeFromClaims()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "CollaboratorCode")?.Value;
        }

        private bool IsAuthorized(string collaboratorCode)
        {
            var claimCollaboratorCode = User.FindFirst("CollaboratorCode")?.Value;
            return !string.IsNullOrEmpty(claimCollaboratorCode) && collaboratorCode == claimCollaboratorCode;
        }

        [Authorize]
        [Route("")]
        [Route("home")]
        public async Task<IActionResult> Home(string collaboratorCode)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            return View(collaborator);
        }

        [Authorize]
        [Route("districts")]
        public async Task<IActionResult> Districts(string collaboratorCode)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var collaborator = await _context.Collaborators
                .Include(c => c.Districts)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            var allDistricts = await _context.Districts.ToListAsync();
            var collaboratorDistrictIds = collaborator.Districts.Select(d => d.DistrictId).ToList();

            var viewModel = allDistricts.Select(d => new DistrictManagerModel
            {
                DistrictId = d.DistrictId,
                DistrictName = d.DistrictName,
                IsActive = collaboratorDistrictIds.Contains(d.DistrictId)
            }).ToList();

            ViewBag.CollaboratorCode = collaboratorCode;
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [Route("districts/toggle")]
        public async Task<IActionResult> ToggleDistrict(string collaboratorCode, int districtId)
        {
            var collaborator = await _context.Collaborators
                .Include(c => c.Districts)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            var district = await _context.Districts.FindAsync(districtId);
            if (district == null)
            {
                return NotFound();
            }

            var isActive = collaborator.Districts.Any(d => d.DistrictId == districtId);
            if (isActive)
            {
                collaborator.Districts.Remove(district);
            }
            else
            {
                collaborator.Districts.Add(district);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = !isActive });
        }

        [Authorize]
        [Route("rooms")]
        public async Task<IActionResult> Rooms(string collaboratorCode, int? page)
        {
            var pageNumber = page ?? 1;

            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            var rooms = await _context.Rooms
                .Where(r => r.CollaboratorId == collaborator.CollaboratorId)
                .OrderByDescending(r => r.RoomId)
                .ToPagedListAsync(pageNumber, PageSize);

            var currentRoomCount = await _context.Rooms
                .CountAsync(r => r.CollaboratorId == collaborator.CollaboratorId);

            if (!Enum.TryParse<CollaboratorLabel>(collaborator.LabelCollaborator, out var collaboratorLabel))
            {
                collaboratorLabel = CollaboratorLabel.Standard;
            }

            var roomLimit = (int)collaboratorLabel;

            ViewBag.CollaboratorCode = collaboratorCode;
            ViewBag.CurrentRoomCount = currentRoomCount;
            ViewBag.RoomLimit = roomLimit;

            return View(rooms);
        }

        private async Task<bool> CanAddNewRoom(string collaboratorCode)
        {
            var collaborator = await _context.Collaborators
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return false;
            }

            var roomCount = await _context.Rooms
                .CountAsync(r => r.CollaboratorId == collaborator.CollaboratorId);

            if (!Enum.TryParse<CollaboratorLabel>(collaborator.LabelCollaborator, out var collaboratorLabel))
            {
                collaboratorLabel = CollaboratorLabel.Standard;
            }

            var roomLimit = (int)collaboratorLabel;

            return roomCount < roomLimit;
        }

        [Authorize]
        [HttpGet]
        [Route("rooms/add")]
        public async Task<IActionResult> AddRoom(string collaboratorCode)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"/{collaboratorCode}/manager/rooms/add" });
            }

            if (!await CanAddNewRoom(collaboratorCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nâng cấp gói hoặc xóa phòng cũ";
                return RedirectToAction(nameof(Rooms), new { collaboratorCode });
            }

            var districts = await _context.Districts.ToListAsync();
            ViewBag.Districts = new SelectList(districts, "DistrictId", "DistrictName");

            ViewBag.CollaboratorCode = collaboratorCode;
            return View(new Room());
        }

        [Authorize]
        [HttpPost]
        [Route("rooms/add")]
        public async Task<IActionResult> AddRoom(string collaboratorCode, Room newRoom, IFormFile MainImageFile, List<IFormFile> AdditionalImages)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return Unauthorized(new { success = false, message = "Không có quyền thực hiện thao tác này." });
            }

            if (!await CanAddNewRoom(collaboratorCode))
            {
                return Json(new { success = false, message = "Vui lòng nâng cấp gói hoặc xóa phòng cũ" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors });
            }

            if (MainImageFile == null || MainImageFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh chính." });
            }

            try
            {
                var collaborator = await _context.Collaborators
                    .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

                if (collaborator == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy collaborator." });
                }

                newRoom.CollaboratorId = collaborator.CollaboratorId;

                // Handle main image upload
                var mainImageFileName = await SaveImage(MainImageFile, "rooms");
                newRoom.MainImageRoom = mainImageFileName;

                _context.Rooms.Add(newRoom);
                await _context.SaveChangesAsync();

                // Add additional images
                if (AdditionalImages != null && AdditionalImages.Any())
                {
                    foreach (var file in AdditionalImages.Where(f => f != null && f.Length > 0).Take(5))
                    {
                        var fileName = await SaveImage(file, "rooms");
                        var newImage = new RoomImage
                        {
                            RoomId = newRoom.RoomId,
                            ImageUrl = fileName
                        };
                        _context.RoomImages.Add(newImage);
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Phòng đã được thêm thành công.",
                    redirectUrl = Url.Action("Rooms", new { collaboratorCode })
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm phòng mới. Vui lòng thử lại." });
            }
        }

        [Authorize]
        [HttpGet]
        [Route("rooms/edit/{id}")]
        public async Task<IActionResult> EditRoom(string collaboratorCode, int id)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return Forbid();
            }

            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            var districts = await _context.Districts.ToListAsync();
            ViewBag.Districts = new SelectList(districts, "DistrictId", "DistrictName", room.DistrictId);

            ViewBag.CollaboratorCode = collaboratorCode;
            return View(room);
        }

        [Authorize]
        [HttpPost]
        [Route("rooms/edit/{id}")]
        public async Task<IActionResult> EditRoom(string collaboratorCode, int id, Room roomViewModel, IFormFile MainImageFile, List<IFormFile> AdditionalImages, List<int> DeletedImageIds, bool DeleteMainImage)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return Json(new { success = false, message = "Không có quyền thực hiện thao tác này." });
            }

            if (id != roomViewModel.RoomId)
            {
                return Json(new { success = false, message = "ID phòng không hợp lệ." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRoom = await _context.Rooms
                        .Include(r => r.RoomImages)
                        .FirstOrDefaultAsync(r => r.RoomId == id);

                    if (existingRoom == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy phòng." });
                    }

                    // Update the existing room's properties
                    _context.Entry(existingRoom).CurrentValues.SetValues(roomViewModel);

                    // Handle main image
                    if (DeleteMainImage && MainImageFile == null)
                    {
                        return Json(new { success = false, message = "Hình ảnh chính là bắt buộc." });
                    }

                    var oldMainImagePath = existingRoom.MainImageRoom;

                    if (MainImageFile != null && MainImageFile.Length > 0)
                    {
                        // Save the new main image
                        var fileName = await SaveImage(MainImageFile, "rooms");

                        // Delete old main image if it exists
                        if (!string.IsNullOrEmpty(oldMainImagePath))
                        {
                            await DeleteImageFile(oldMainImagePath);
                        }

                        existingRoom.MainImageRoom = fileName;
                    }

                    // Remove deleted additional images
                    if (DeletedImageIds != null && DeletedImageIds.Any())
                    {
                        var imagesToRemove = existingRoom.RoomImages.Where(ri => DeletedImageIds.Contains(ri.ImageId)).ToList();
                        foreach (var imageToRemove in imagesToRemove)
                        {
                            existingRoom.RoomImages.Remove(imageToRemove);
                            _context.RoomImages.Remove(imageToRemove);
                            await DeleteImageFile(imageToRemove.ImageUrl);
                        }
                    }

                    // Add new images
                    if (AdditionalImages != null && AdditionalImages.Any())
                    {
                        foreach (var file in AdditionalImages.Where(f => f != null && f.Length > 0))
                        {
                            var fileName = await SaveImage(file, "rooms");
                            var newImage = new RoomImage
                            {
                                RoomId = existingRoom.RoomId,
                                ImageUrl = fileName
                            };
                            existingRoom.RoomImages.Add(newImage);
                        }
                    }

                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Phòng đã được cập nhật thành công.",
                        redirectUrl = Url.Action("Rooms", new { collaboratorCode })
                    });
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error updating room: {ex.Message}");
                    return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật phòng. Vui lòng thử lại." });
                }
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
        }

        private async Task<string> SaveImage(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var uploadsFolder = Path.Combine("wwwroot", "images", folder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/{folder}/{fileName}";
        }

        private async Task DeleteImageFile(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                    }
                }
            }
        }

        [Authorize]
        [HttpPost]
        [Route("rooms/delete/{id}")]
        public async Task<IActionResult> DeleteRoom(string collaboratorCode, int id)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return Json(new { success = false, message = "Không có quyền thực hiện thao tác này." });
            }

            var room = await _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phòng." });
            }

            try
            {
                // Delete main image
                if (!string.IsNullOrEmpty(room.MainImageRoom))
                {
                    DeleteImageFile(room.MainImageRoom);
                }

                // Delete associated images
                foreach (var image in room.RoomImages)
                {
                    DeleteImageFile(image.ImageUrl);
                    _context.RoomImages.Remove(image);
                }

                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Phòng đã được xóa thành công." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa phòng. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        [Route("about")]
        public async Task<IActionResult> About(string collaboratorCode)
        {
            ViewBag.CollaboratorCode = collaboratorCode;
            var collaborator = await _context.Collaborators
                .Include(c => c.CollaboratorImages)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            return View(collaborator);
        }

        [Authorize]
        [HttpGet]
        [Route("about/edit")]
        public async Task<IActionResult> EditAbout(string collaboratorCode)
        {
            ViewBag.CollaboratorCode = collaboratorCode;
            if (!IsAuthorized(collaboratorCode))
            {
                return Forbid();
            }

            var collaborator = await _context.Collaborators
                .Include(c => c.CollaboratorImages)
                .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

            if (collaborator == null)
            {
                return NotFound();
            }

            return View(collaborator);
        }

        [Authorize]
        [HttpPost]
        [Route("about/edit")]
        public async Task<IActionResult> EditAbout(string collaboratorCode, Collaborator updatedCollaborator, IFormFile AvatarFile, List<IFormFile> AdditionalImages, List<int> DeletedImageIds, bool DeleteAvatar)
        {
            if (!IsAuthorized(collaboratorCode))
            {
                return Json(new { success = false, message = "Không có quyền thực hiện thao tác này." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var collaborator = await _context.Collaborators
                        .Include(c => c.CollaboratorImages)
                        .FirstOrDefaultAsync(c => c.CollaboratorCode == collaboratorCode);

                    if (collaborator == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy cộng tác viên." });
                    }

                    // Update collaborator properties
                    collaborator.FullnameCollaborator = updatedCollaborator.FullnameCollaborator;
                    collaborator.SloganCollaborator = updatedCollaborator.SloganCollaborator;
                    collaborator.ServiceCollaborator = updatedCollaborator.ServiceCollaborator;
                    collaborator.FacebookCollaborator = updatedCollaborator.FacebookCollaborator;
                    collaborator.MessengerCollaborator = updatedCollaborator.MessengerCollaborator;
                    collaborator.ZaloCollaborator = updatedCollaborator.ZaloCollaborator;
                    collaborator.TiktokCollaborator = updatedCollaborator.TiktokCollaborator;
                    collaborator.PhoneNumberCollaborator = updatedCollaborator.PhoneNumberCollaborator;
                    collaborator.CommitCollaborator = updatedCollaborator.CommitCollaborator;

                    // Handle avatar
                    if (DeleteAvatar && AvatarFile == null)
                    {
                        return Json(new { success = false, message = "Hình ảnh đại diện là bắt buộc." });
                    }

                    var oldAvatarPath = collaborator.AvatarCollaborator;

                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        var fileName = await SaveImage(AvatarFile, "collaborators");

                        if (!string.IsNullOrEmpty(oldAvatarPath))
                        {
                            await DeleteImageFile(oldAvatarPath);
                        }

                        collaborator.AvatarCollaborator = fileName;
                    }

                    // Remove deleted additional images
                    if (DeletedImageIds != null && DeletedImageIds.Any())
                    {
                        var imagesToRemove = collaborator.CollaboratorImages.Where(ci => DeletedImageIds.Contains(ci.ImageId)).ToList();
                        foreach (var imageToRemove in imagesToRemove)
                        {
                            collaborator.CollaboratorImages.Remove(imageToRemove);
                            _context.CollaboratorImages.Remove(imageToRemove);
                            await DeleteImageFile(imageToRemove.ImageUrl);
                        }
                    }

                    // Add new images
                    if (AdditionalImages != null && AdditionalImages.Any())
                    {
                        foreach (var file in AdditionalImages.Where(f => f != null && f.Length > 0))
                        {
                            var fileName = await SaveImage(file, "collaborators");
                            var newImage = new CollaboratorImage
                            {
                                CollaboratorId = collaborator.CollaboratorId,
                                ImageUrl = fileName
                            };
                            collaborator.CollaboratorImages.Add(newImage);
                        }
                    }

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Thông tin cộng tác viên đã được cập nhật thành công.",
                        redirectUrl = Url.Action("About", new { collaboratorCode })
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating collaborator: {ex.Message}");
                    return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại." });
                }
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }

        [Authorize]
        [HttpPost]
        [Route("cleanup-images")]
        public async Task<IActionResult> CleanupImages(string securityCode)
        {
            if (!IsAuthorized(HttpContext.Session.GetString("CollaboratorCode")))
            {
                return Json(new { success = false, message = "Không có quyền thực hiện thao tác này." });
            }

            if (securityCode != "cotrinhkhong")
            {
                return Json(new { success = false, message = "Mã bảo mật không chính xác." });
            }

            try
            {
                // Lấy tất cả các URL hình ảnh đang được sử dụng trong database
                var usedImages = new HashSet<string>();

                // Lấy tất cả hình ảnh chính của phòng
                var mainImages = await _context.Rooms
                    .Select(r => r.MainImageRoom)
                    .Where(img => img != null)
                    .ToListAsync();
                usedImages.UnionWith(mainImages);

                // Lấy tất cả hình ảnh bổ sung của phòng
                var additionalImages = await _context.RoomImages
                    .Select(ri => ri.ImageUrl)
                    .Where(img => img != null)
                    .ToListAsync();
                usedImages.UnionWith(additionalImages);

                // Đường dẫn đến thư mục chứa hình ảnh phòng
                var imageFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");

                // Kiểm tra xem thư mục có tồn tại không
                if (!Directory.Exists(imageFolder))
                {
                    return Json(new { success = false, message = "Thư mục hình ảnh không tồn tại." });
                }

                var files = Directory.GetFiles(imageFolder, "*.*", SearchOption.AllDirectories);
                int deletedCount = 0;

                foreach (var file in files)
                {
                    var relativePath = GetRelativePath(file, _environment.WebRootPath);
                    var imageUrl = "/" + relativePath.Replace("\\", "/");

                    if (!usedImages.Contains(imageUrl))
                    {
                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                }

                return Json(new { success = true, message = $"Đã xóa {deletedCount} hình ảnh dư thừa." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi dọn dẹp hình ảnh. Vui lòng thử lại." });
            }
        }

        private string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}