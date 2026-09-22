using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Data;

internal static class AppDbSeeder
{
    public static readonly Guid GuestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db, TimeProvider timeProvider, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        // 1. Seed Guest/Demo User
        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == GuestUserId, ct))
        {
            db.Users.Add(new User
            {
                Id = GuestUserId,
                Email = "guest@duongnhan.ai",
                PasswordHash = "AQAAAAIAAYagAAAAEPlaceholderHashForGuestDemoUser1234567890=",
                DisplayName = "Khách dùng thử",
                PhoneNumber = "+84901234567",
                Status = "active",
                CreatedAt = now
            });
        }

        // 2. Seed Plans
        if (!await db.Plans.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.Plans.AddRange([
                new Plan
                {
                    Name = "Gói Cơ Bản (Miễn Phí)",
                    Code = "plan_free",
                    Description = "Trải nghiệm phân tích da AI cơ bản với đầy đủ tính năng chuẩn đoán.",
                    Price = 0,
                    BillingCycle = "monthly",
                    MaxScansPerMonth = 5,
                    FeaturesJson = "[\"5 lượt quét AI / tháng\", \"Chuẩn đoán 10 tình trạng da\", \"Gợi ý sản phẩm cơ bản\"]",
                    IsActive = true,
                    CreatedAt = now
                },
                new Plan
                {
                    Name = "Gói Tiêu Chuẩn (Pro)",
                    Code = "plan_pro",
                    Description = "Dành cho người chăm sóc da thường xuyên, theo dõi tiến trình hồi phục theo tuần.",
                    Price = 99000,
                    BillingCycle = "monthly",
                    MaxScansPerMonth = 30,
                    FeaturesJson = "[\"30 lượt quét AI / tháng\", \"Theo dõi biểu đồ phục hồi da\", \"Gợi ý phác đồ chu trình chuyên sâu\", \"Lưu lịch sử không giới hạn\"]",
                    IsActive = true,
                    CreatedAt = now
                },
                new Plan
                {
                    Name = "Gói VIP Chuyên Sâu",
                    Code = "plan_vip",
                    Description = "Phân tích không giới hạn, kết nối bác sĩ da liễu và cảnh báo rủi ro tương tác mỹ phẩm.",
                    Price = 199000,
                    BillingCycle = "monthly",
                    MaxScansPerMonth = 999,
                    FeaturesJson = "[\"Không giới hạn lượt quét\", \"Đánh giá tương tác thành phần mỹ phẩm\", \"Ưu tiên giải đáp từ chuyên gia da liễu\", \"Báo cáo chuyên sâu hàng tháng\"]",
                    IsActive = true,
                    CreatedAt = now
                }
            ]);
        }

        // 3. Seed Products
        if (!await db.Products.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.Products.AddRange([
                new Product
                {
                    Name = "CeraVe Foaming Facial Cleanser",
                    Brand = "CeraVe",
                    Category = "Cleanser",
                    Price = 340000,
                    ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=500",
                    Description = "Sữa rửa mặt dạng gel tạo bọt nhẹ dịu, chứa Ceramide và Niacinamide giúp làm sạch sâu bã nhờn mà không làm khô căng hay tổn thương hàng rào ẩm.",
                    TargetConditions = "Acne,EnlargedPores,Healthy",
                    UsageInstructions = "Lấy một lượng vừa đủ, tạo bọt nhẹ với nước và massage nhẹ nhàng trong 60 giây, rửa sạch với nước ấm. Dùng sáng và tối.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "La Roche-Posay Effaclar Purifying Foaming Gel",
                    Brand = "La Roche-Posay",
                    Category = "Cleanser",
                    Price = 385000,
                    ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=500",
                    Description = "Gel rửa mặt dành riêng cho da dầu mụn nhạy cảm với nước khoáng khoáng La Roche-Posay và Kẽm PCA giúp điều tiết dầu thừa hiệu quả.",
                    TargetConditions = "Acne,EnlargedPores,SeborrheicDermatitis",
                    UsageInstructions = "Tạo bọt trên lòng bàn tay rồi thoa lên mặt đã làm ướt. Rửa sạch lại với nước và thấm khô.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "Paula's Choice Skin Perfecting 2% BHA Liquid Exfoliant",
                    Brand = "Paula's Choice",
                    Category = "Treatment",
                    Price = 949000,
                    ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=500",
                    Description = "Dung dịch loại bỏ tế bào chết hóa học chứa 2% Salicylic Acid tan trong dầu, đi sâu vào lỗ chân lông để thông thoáng tắc nghẽn, giảm mụn đầu đen và thu nhỏ lỗ chân lông.",
                    TargetConditions = "Acne,EnlargedPores,SeborrheicDermatitis",
                    UsageInstructions = "Thấm đều ra bông tẩy trang hoặc đổ trực tiếp ra lòng bàn tay rồi vỗ nhẹ lên mặt sau bước làm sạch. Bắt đầu với 2-3 lần/tuần.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "The Ordinary Niacinamide 10% + Zinc 1%",
                    Brand = "The Ordinary",
                    Category = "Serum",
                    Price = 210000,
                    ImageUrl = "https://images.unsplash.com/photo-1608248597359-57353f86eb79?w=500",
                    Description = "Tinh chất cô đặc với 10% Niacinamide và 1% Muối Kẽm giúp kháng viêm nốt mụn, kiểm soát bã nhờn vượt trội và làm mờ các vết thâm sau mụn.",
                    TargetConditions = "Acne,EnlargedPores,Hyperpigmentation",
                    UsageInstructions = "Thoa 2-3 giọt lên toàn bộ khuôn mặt vào buổi sáng và buổi tối trước các loại kem đặc hơn.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "Kiehl's Clearly Corrective Dark Spot Solution",
                    Brand = "Kiehl's",
                    Category = "Serum",
                    Price = 1850000,
                    ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=500",
                    Description = "Serum dưỡng sáng da mờ thâm nám với Vitamin C hoạt tính thế hệ mới kết hợp chiết xuất Bạch Dương Trắng và Hoa Mẫu Đơn giúp giảm rõ rệt đốm nâu và sạm nám.",
                    TargetConditions = "Hyperpigmentation,Melasma",
                    UsageInstructions = "Thoa một vài giọt lên vùng da thâm nám hoặc toàn mặt trước khi thoa kem dưỡng ẩm. Sử dụng đều đặn sáng và tối.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "La Roche-Posay Cicaplast Baume B5+ Ultra-Repairing",
                    Brand = "La Roche-Posay",
                    Category = "Moisturizer",
                    Price = 390000,
                    ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500",
                    Description = "Kem dưỡng phục hồi làm dịu da đa công dụng với 5% Panthenol (B5), Madecassoside và phức hợp men vi sinh Tribioma giúp làm dịu tức thì các kích ứng, mẩn đỏ và phục hồi da sau mụn.",
                    TargetConditions = "Eczema,Rosacea,ContactDermatitis,Acne",
                    UsageInstructions = "Thoa 2 lần mỗi ngày lên vùng da cần phục hồi sau khi làm sạch. Thoa lớp mỏng vừa đủ.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "Dear Klairs Rich Moist Soothing Cream",
                    Brand = "Dear Klairs",
                    Category = "Moisturizer",
                    Price = 375000,
                    ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500",
                    Description = "Kem dưỡng ẩm chuyên sâu làm dịu da khô, bong tróc và da nhạy cảm với phức hợp Beta-Glucan, chiết xuất rau má và tinh dầu jojoba giúp duy trì độ ẩm suốt 24h.",
                    TargetConditions = "Healthy,Eczema,Rosacea",
                    UsageInstructions = "Lấy một lượng kem vừa đủ thoa đều khắp mặt và cổ ở bước cuối cùng của chu trình skincare buổi tối.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++",
                    Brand = "Anessa",
                    Category = "Sunscreen",
                    Price = 685000,
                    ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500",
                    Description = "Sữa chống nắng kiềm dầu số 1 Nhật Bản với công nghệ Auto Booster chống trôi nước/mồ hôi vượt trội, bảo vệ quang phổ rộng chống tia UVA/UVB và ánh sáng xanh.",
                    TargetConditions = "Hyperpigmentation,Melasma,Healthy,Acne",
                    UsageInstructions = "Lắc đều trước khi dùng. Thoa đều lên mặt và cổ trước khi ra ngoài 20 phút. Thoa lại sau mỗi 2-3 giờ khi hoạt động ngoài trời.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "Eucerin ProAcne Solution A.I. Clearing Treatment",
                    Brand = "Eucerin",
                    Category = "Treatment",
                    Price = 490000,
                    ImageUrl = "https://images.unsplash.com/photo-1608248597359-57353f86eb79?w=500",
                    Description = "Tinh chất đặc trị mụn chuyên sâu với phức hợp 10% Hydroxy Complex (AHA, BHA, PHA) và Licochalcone A giúp gom cồi mụn, giảm viêm sưng chỉ sau 1 tuần.",
                    TargetConditions = "Acne,EnlargedPores",
                    UsageInstructions = "Sử dụng một lần mỗi ngày vào buổi tối. Thoa một lượng nhỏ lên vùng da bị mụn sau khi rửa mặt sạch.",
                    IsActive = true,
                    CreatedAt = now
                },
                new Product
                {
                    Name = "COSRX Advanced Snail 96 Mucin Power Essence",
                    Brand = "COSRX",
                    Category = "Essence",
                    Price = 310000,
                    ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=500",
                    Description = "Tinh chất chứa 96% dịch nhầy ốc sên tự nhiên giúp cấp nước tức thì, tái tạo độ đàn hồi và làm dịu vùng da sau khi lấy nhân mụn.",
                    TargetConditions = "Healthy,Eczema,Hyperpigmentation",
                    UsageInstructions = "Sau khi làm sạch và dùng toner, thoa một lượng nhỏ lên toàn bộ khuôn mặt rồi vỗ nhẹ để dưỡng chất thẩm thấu.",
                    IsActive = true,
                    CreatedAt = now
                }
            ]);
        }

        await db.SaveChangesAsync(ct);
    }
}
