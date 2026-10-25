namespace DuongNhan.Shared.Contracts;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string Base = "/api/auth";
        public const string Register = Base + "/register";
        public const string Login = Base + "/login";
        public const string Refresh = Base + "/refresh";
        public const string Logout = Base + "/logout";
        public const string Google = Base + "/google";
        public const string SendOtp = Base + "/send-otp";
        public const string VerifyOtp = Base + "/verify-otp";
    }

    public static class Users
    {
        public const string Base = "/api/users";
        public const string Me = Base + "/me";
        public const string UpdateProfile = Base + "/me";
        public const string ChangePassword = Base + "/me/password";
    }

    public static class Skin
    {
        public const string Base = "/api/skin";
        public const string Upload = Base + "/upload";
        public const string Diagnose = Base + "/{id:guid}/diagnose";
        public const string History = Base + "/history";
        public const string Image = Base + "/{id:guid}";
    }

    public static class Products
    {
        public const string Base = "/api/products";
        public const string List = Base;
        public const string Recommend = Base + "/recommend";
        public const string Detail = Base + "/{id:guid}";
        public const string Import = Base + "/import";
    }

    public static class Subscriptions
    {
        public const string Base = "/api/subscriptions";
        public const string Current = Base + "/current";
        public const string Usage = Base + "/usage";
        public const string Plans = "/api/plans";
    }

    public static class Payments
    {
        public const string Base = "/api/payments";
        public const string Orders = Base + "/orders";
        public const string Order = Base + "/orders/{id:guid}";
        public const string CancelOrder = Base + "/orders/{id:guid}/cancel";
        public const string SepayWebhook = Base + "/sepay-webhook";
    }

    public static class Admin
    {
        public const string Base = "/api/admin";
        public const string Stats = Base + "/stats";
        public const string Users = Base + "/users";
        public const string UserStatus = Base + "/users/{id:guid}/status";
        public const string Products = Base + "/products";
        public const string Product = Base + "/products/{id:guid}";
    }
}
