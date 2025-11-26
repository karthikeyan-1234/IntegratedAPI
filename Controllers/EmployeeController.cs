using IntegratedAPI.Contexts;
using IntegratedAPI.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        ProjectDbContext _dbContext;
        public EmployeeController(ProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("GetEmployeesAsync")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _dbContext.Employees.ToListAsync();
            return Ok(employees);
        }

        [HttpPost("AddEmployeeAsync")]
        public async Task<IActionResult> AddEmployeeAsync([FromBody] employee employee)
        {
            _dbContext.Employees.Add(employee);
            _dbContext.SaveChanges();
            return Ok(employee);
        }
    }
}
