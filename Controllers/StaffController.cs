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
        public async Task<IActionResult> Register([FromBody] RegisterStaffRequest request)
        {
            var result = await _staffRepository.RegisterAsync(request);
            return StatusCode(result.IsSuccessful ? 201 : 400, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _staffRepository.GetAllAsync();
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _staffRepository.GetByIdAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _staffRepository.DeleteAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }
    }
}