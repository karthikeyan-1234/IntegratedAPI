using IntegratedAPI.Models.DTOs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Stripe;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly StripeOptions _stripeOptions;

        public PaymentsController(IOptionsSnapshot<StripeOptions> stripeOptions)
        {
            _stripeOptions = stripeOptions.Value;
        }

        // Inside PaymentsMicroService.Controllers.PaymentsController

        //Create Payment Intent
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                Stripe.StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

                // Define the hardcoded customer/shipping data required for RBI/Export compliance.
                // This object (ChargeShippingOptions) is available in Stripe.net v49.0.0
                var shippingOptions = new ChargeShippingOptions
                {
                    // 1. Name is REQUIRED for RBI compliance
                    Name = "TEST CUSTOMER FOR RBI",

                    // 2. Address is REQUIRED for RBI compliance
                    Address = new AddressOptions
                    {
                        Line1 = "Hardcoded Address Line 1",
                        City = "Mumbai",
                        PostalCode = "400001",
                        Country = "IN",          // Country must be set to India
                        State = "MH"             // State code
                    }
                };

                var options = new Stripe.PaymentIntentCreateOptions
                {
                    Amount = request.Amount,
                    Currency = request.Currency,

                    // 3. Add Description for RBI/Export Compliance
                    Description = $"Payment for Order ID: {request.OrderId}",

                    // 4. Attach Customer Data via the stable 'Shipping' property
                    // This property is available on PaymentIntentCreateOptions in v49.0.0
                    Shipping = shippingOptions,

                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    },
                    Metadata = new Dictionary<string, string>
            {
                { "order_id", request.OrderId.ToString() }
            }
                };

                var service = new Stripe.PaymentIntentService();
                Stripe.PaymentIntent paymentIntent = await service.CreateAsync(options);

                return Ok(new
                {
                    clientSecret = paymentIntent.ClientSecret
                });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) as needed
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
