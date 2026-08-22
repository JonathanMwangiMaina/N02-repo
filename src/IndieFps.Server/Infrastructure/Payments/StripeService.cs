namespace IndieFps.Server.Infrastructure.Payments;

using IndieFps.Server.Configuration;
using IndieFps.Shared.Constants;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(string email, string username, string userId);
    Task<Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl, string? promoCode = null);
    Task<Session> CreatePortalSessionAsync(string customerId, string returnUrl);
    Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string? paymentMethodId = null, string? promoCode = null);
    Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool atPeriodEnd = true);
    Task<Subscription> GetSubscriptionAsync(string subscriptionId);
    Task<Customer> GetCustomerAsync(string customerId);
    Event ConstructEvent(string json, string signature);
}

public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    
    public StripeService(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }
    
    public async Task<string> CreateCustomerAsync(string email, string username, string userId)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = username,
            Metadata = new Dictionary<string, string>
            {
                [StripeConstants.MetadataKeys.UserId] = userId
            }
        };
        
        var service = new CustomerService();
        var customer = await service.CreateAsync(options);
        return customer.Id;
    }
    
    public async Task<Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl, string? promoCode = null)
    {
        var options = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            AllowPromotionCodes = true,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = SubscriptionConstants.TrialDays,
                Metadata = new Dictionary<string, string>
                {
                    [StripeConstants.MetadataKeys.UserId] = customerId // Will be replaced with actual userId
                }
            }
        };
        
        if (!string.IsNullOrEmpty(promoCode))
        {
            options.PromotionCode = promoCode;
        }
        
        var service = new SessionService();
        return await service.CreateAsync(options);
    }
    
    public async Task<Session> CreatePortalSessionAsync(string customerId, string returnUrl)
    {
        var options = new BillingPortalSessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl
        };
        
        var service = new BillingPortalSessionService();
        return await service.CreateAsync(options);
    }
    
    public async Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string? paymentMethodId = null, string? promoCode = null)
    {
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = priceId }
            },
            TrialPeriodDays = SubscriptionConstants.TrialDays,
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = new List<string> { "latest_invoice.payment_intent" }
        };
        
        if (!string.IsNullOrEmpty(paymentMethodId))
        {
            options.DefaultPaymentMethod = paymentMethodId;
        }
        
        if (!string.IsNullOrEmpty(promoCode))
        {
            options.PromotionCode = promoCode;
        }
        
        var service = new SubscriptionService();
        return await service.CreateAsync(options);
    }
    
    public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool atPeriodEnd = true)
    {
        var service = new SubscriptionService();
        
        if (atPeriodEnd)
        {
            var updateOptions = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };
            return await service.UpdateAsync(subscriptionId, updateOptions);
        }
        else
        {
            return await service.CancelAsync(subscriptionId);
        }
    }
    
    public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
    {
        var service = new SubscriptionService();
        return await service.GetAsync(subscriptionId);
    }
    
    public async Task<Customer> GetCustomerAsync(string customerId)
    {
        var service = new CustomerService();
        return await service.GetAsync(customerId);
    }
    
    public Event ConstructEvent(string json, string signature)
    {
        return EventUtility.ConstructEvent(json, signature, _settings.WebhookSecret);
    }
}