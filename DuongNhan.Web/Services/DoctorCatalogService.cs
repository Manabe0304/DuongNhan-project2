namespace DuongNhan.Web.Services;

public sealed class DoctorCatalogService
{
    public IReadOnlyList<DoctorItem> GetAll() =>
    [
        new(1,"BS. CKII Ngô Thanh Trúc","Da liễu thẩm mỹ & Trị sẹo","TP.HCM","Phòng khám Da liễu Sài Gòn Skin Clinic","215 Điện Biên Phủ, Quận 3, TP.HCM",12,4.9,312,"300.000đ",300000,450000,"https://images.unsplash.com/photo-1594824476967-48c8b964273f?w=400&h=400&fit=crop","Chuyên gia điều trị sẹo rỗ, trẻ hóa da và phục hồi màng bảo vệ da sau xâm lấn.",["09:00","10:30","14:00","15:30","17:00"]),
        new(2,"ThS. BS Lê Minh Khôi","Da liễu lâm sàng & Mụn viêm","Hà Nội","Trung tâm Y khoa Da liễu Thăng Long","18 Phố Huế, Hà Nội",9,4.8,210,"250.000đ",250000,350000,"https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=400&h=400&fit=crop","Hơn 9 năm điều trị mụn trứng cá, viêm da cơ địa và viêm da tiết bã.",["08:30","10:00","13:30","15:00","16:30"]),
        new(3,"BS. Phạm Anh Thư","Điều trị mụn & Phục hồi da","Đà Nẵng","Phòng khám Da liễu & Thẩm mỹ Sông Hàn","86 Bạch Đằng, Đà Nẵng",8,5.0,158,"280.000đ",280000,380000,"https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?w=400&h=400&fit=crop","Thiết kế phác đồ chăm sóc cá nhân hóa cho nhiều nhóm khách hàng.",["09:00","11:00","14:30","16:00","17:30"]),
        new(4,"BS. CKI Trần Gia Huy","Nám, Tàn nhang & Chống lão hóa","TP.HCM","Bệnh viện Da liễu Á Âu","32D Thủ Khoa Huân, Quận 1, TP.HCM",15,4.7,401,"350.000đ",350000,500000,"https://images.unsplash.com/photo-1537368910025-700350fe46c7?w=400&h=400&fit=crop","Chuyên trị nám, tăng sắc tố sau viêm và điều trị nếp nhăn.",["08:00","09:30","14:00","16:00"])
    ];
}

public sealed record DoctorItem(
    int Id,string Name,string Specialty,string City,string Clinic,string Address,int Experience,
    double Rating,int Reviews,string Price,int OnlineFee,int OfflineFee,string Image,string Bio,IReadOnlyList<string> Slots);
