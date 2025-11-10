using IntegratedAPI.Contexts;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context (switch between SQL Server and InMemory for testing)
builder.Services.AddDbContext<ProjectDbContext>(opt =>
    // opt.UseInMemoryDatabase("ProductsDb")
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);



builder.Services.AddCors(builder =>
{
    builder.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//swagger

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
