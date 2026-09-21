using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Dtos.Users;
using Riok.Mapperly.Abstractions;

namespace DuongNhan.ApiService.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal sealed partial class UserMapper
{
    public partial UserDto ToDto(User user);
}