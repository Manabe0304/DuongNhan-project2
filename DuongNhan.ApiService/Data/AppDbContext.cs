using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}