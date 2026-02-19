namespace IntegratedAPI.Models
{
        public class cartItem
        {
            public int id { get; set; }
            public int product_id { get; set; }
            public double quantity { get; set; }

            public product? Product { get; set; }
        }
}
