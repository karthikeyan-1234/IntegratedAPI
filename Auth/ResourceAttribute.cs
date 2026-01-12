namespace IntegratedAPI.Auth
{
    public class ResourceAttribute : Attribute
    {
        public string Name { get; set; }
        public ResourceAttribute(string name)
        {
            //check if name is null or empty
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Resource name cannot be null or empty", nameof(name));
            }

            //if name doesn't have suffix Resource, add it
            if (!name.EndsWith("Resource"))
            {
                name += "Resource";
            }

            Name = name;
        }
    }
}
