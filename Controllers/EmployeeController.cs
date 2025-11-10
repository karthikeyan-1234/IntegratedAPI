using IntegratedAPI.Contexts;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("GetEmployees")]
        public IActionResult GetEmployees()
        {
            var employees = _dbContext.Employees.ToList();
            return Ok(employees);
        }
    }
}
