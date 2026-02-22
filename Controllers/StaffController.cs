using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _staffRepository;

        public StaffController(IStaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Register([FromBody] RegisterStaffRequest request)
        {
            var result = await _staffRepository.RegisterAsync(request);
            return StatusCode(result.IsSuccessful ? 201 : 400, result);
        }

        [HttpGet]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _staffRepository.GetAllAsync();
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _staffRepository.GetByIdAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }
    }
}