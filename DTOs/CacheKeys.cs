namespace IntegratedAPI.DTOs
{
    public static class CacheKeys
    {
        public const string Products = "products";
        public const string ProductById = "product_{0}";
        public const string Employees = "employees";
        public const string EmployeeById = "employee_{0}";
        public const string CartByUserId = "cart_{0}";
        public const string CartItemById = "cart_item_{0}";
        public const string UserSession = "session_{0}";
        public const string Configuration = "config_{0}";

        public static string Format(string pattern, params object[] args)
        {
            return string.Format(pattern, args);
        }
    }
}
