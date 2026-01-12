namespace IntegratedAPI.Auth
{
    public class PermissionAttribute : Attribute
    {
        public string permission_list { get; set; }

        public PermissionAttribute(string permission_list)
        {

            this.permission_list = permission_list;
        }
    }
}
