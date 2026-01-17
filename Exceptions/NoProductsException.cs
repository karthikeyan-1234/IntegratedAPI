namespace IntegratedAPI.Exceptions
{
    public class NoProductsException: Exception
    {
        public string message;
        public int statusCode;
        public NoProductsException(string message)
        {
            this.message = message;
            this.statusCode = StatusCodes.Status400BadRequest;
        }
    }
}
