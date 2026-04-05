using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using MyApi.DAL.Repository;

namespace MyApi.BLL.Service;

public class VoiceAssistantService : IVoiceAssistantService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IOpenAiService _openAiService;

    public VoiceAssistantService(IStoreRepository storeRepository, IOpenAiService openAiService)
    {
        _storeRepository = storeRepository;
        _openAiService = openAiService;
    }

    public async Task<VoiceResponseDto> ExecuteCommandAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new VoiceResponseDto
            {
                Action = "None",
                ReplyText = "I did not hear anything. Please try again.",
                ScreenText = "No input received."
            };
        }

        var interpretation = await _openAiService.InterpretCommandAsync(text);

        if (interpretation is null)
            interpretation = RuleBasedFallback(text);

        var action = interpretation.Action ?? "Unknown";
        var query = interpretation.Query?.Trim() ?? string.Empty;

        var products = _storeRepository.GetProducts();
        var cartItems = _storeRepository.GetCartItems();
        var orders = _storeRepository.GetOrders();

        switch (action)
        {
            case "SearchProduct":
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new VoiceResponseDto
                    {
                        Action = "SearchProduct",
                        ReplyText = "You selected product search. Please say the product name.",
                        ScreenText = "Search mode activated. Waiting for product name."
                    };
                }

                var matchedProducts = products
                    .Where(p => p.Translations != null &&
                                p.Translations.Any(t =>
                                    (!string.IsNullOrWhiteSpace(t.Name) &&
                                     t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                    (!string.IsNullOrWhiteSpace(t.Description) &&
                                     t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))))
                    .Take(3)
                    .ToList();

                if (matchedProducts.Count == 0)
                {
                    return new VoiceResponseDto
                    {
                        Action = "SearchProduct",
                        ReplyText = $"I could not find any product matching {query}.",
                        ScreenText = $"No products found for: {query}"
                    };
                }

                var spokenResults = string.Join(" . ", matchedProducts.Select(p =>
                {
                    var name = p.Translations?
                        .FirstOrDefault(t => t.Language == "en")?.Name
                        ?? p.Translations?.FirstOrDefault()?.Name
                        ?? $"Product {p.Id}";

                    return $"{name}, price {p.Price} shekels";
                }));

                var screenResults = string.Join(" | ", matchedProducts.Select(p =>
                {
                    var name = p.Translations?
                        .FirstOrDefault(t => t.Language == "en")?.Name
                        ?? p.Translations?.FirstOrDefault()?.Name
                        ?? $"Product {p.Id}";

                    return $"{name} - {p.Price} NIS";
                }));

                return new VoiceResponseDto
                {
                    Action = "SearchProduct",
                    ReplyText = $"I found {matchedProducts.Count} product(s) for {query}. {spokenResults}.",
                    ScreenText = screenResults,
                    Data = matchedProducts
                };
            }

            case "ViewCart":
            {
                var cartDetails = cartItems
                    .Join(products,
                        c => c.ProductId,
                        p => p.Id,
                        (c, p) => new
                        {
                            ProductId = p.Id,
                            Name = p.Translations?
                                .FirstOrDefault(t => t.Language == "en")?.Name
                                ?? p.Translations?.FirstOrDefault()?.Name
                                ?? $"Product {p.Id}",
                            p.Price,
                            c.Quantity,
                            Total = p.Price * c.Quantity
                        })
                    .ToList();

                if (cartDetails.Count == 0)
                {
                    return new VoiceResponseDto
                    {
                        Action = "ViewCart",
                        ReplyText = "Your cart is empty.",
                        ScreenText = "Cart is empty."
                    };
                }

                var totalAmount = cartDetails.Sum(x => x.Total);

                var spokenCart = string.Join(" . ", cartDetails.Select(x =>
                    $"{x.Name}, quantity {x.Quantity}, total {x.Total} shekels"));

                var screenCart = string.Join(" | ", cartDetails.Select(x =>
                    $"{x.Name} x{x.Quantity} = {x.Total} NIS"));

                return new VoiceResponseDto
                {
                    Action = "ViewCart",
                    ReplyText = $"Your cart contains {cartDetails.Count} item(s). {spokenCart}. Total amount is {totalAmount} shekels.",
                    ScreenText = $"{screenCart} | Total = {totalAmount} NIS",
                    Data = cartDetails
                };
            }

            case "TrackLatestOrder":
            {
                var latestOrder = orders
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefault();

                if (latestOrder is null)
                {
                    return new VoiceResponseDto
                    {
                        Action = "TrackLatestOrder",
                        ReplyText = "No orders were found.",
                        ScreenText = "No latest order found."
                    };
                }

                return new VoiceResponseDto
                {
                    Action = "TrackLatestOrder",
                    ReplyText = $"Your latest order id is {latestOrder.Id}. Its status is {latestOrder.OrderStatus}.",
                    ScreenText = $"Latest order: {latestOrder.Id} - {latestOrder.OrderStatus}",
                    Data = latestOrder
                };
            }

            case "ViewOrders":
            {
                if (orders.Count == 0)
                {
                    return new VoiceResponseDto
                    {
                        Action = "ViewOrders",
                        ReplyText = "You do not have any orders.",
                        ScreenText = "No orders found."
                    };
                }

                var spokenOrders = string.Join(" . ", orders.Select(o =>
                    $"Order {o.Id}, status {o.OrderStatus}, total shekels"));

                var screenOrders = string.Join(" | ", orders.Select(o =>
                    $"{o.Id} - {o.OrderStatus} -  NIS"));

                return new VoiceResponseDto
                {
                    Action = "ViewOrders",
                    ReplyText = $"You have {orders.Count} order(s). {spokenOrders}.",
                    ScreenText = screenOrders,
                    Data = orders
                };
            }

            case "Repeat":
                return new VoiceResponseDto
                {
                    Action = "Repeat",
                    ReplyText = "Repeating the available options. Search for a product, view cart items, track your latest order, or view my order.",
                    ScreenText = "Options repeated."
                };

            case "GoBack":
                return new VoiceResponseDto
                {
                    Action = "GoBack",
                    ReplyText = "Going back to the main menu.",
                    ScreenText = "Back to main menu."
                };

            case "Exit":
                return new VoiceResponseDto
                {
                    Action = "Exit",
                    ReplyText = "Exiting the assistant. Goodbye.",
                    ScreenText = "Session ended."
                };

            default:
                return new VoiceResponseDto
                {
                    Action = "Unknown",
                    ReplyText = "Sorry, I did not understand. Please say search for a product, view cart items, track your latest order, or view my order.",
                    ScreenText = $"Recognized text: {text}"
                };
        }
    }

    private static CommandInterpretation RuleBasedFallback(string text)
    {
        var lower = text.Trim().ToLowerInvariant();

        if (lower.Contains("cart"))
            return new CommandInterpretation("ViewCart", "");

        if (lower.Contains("track") || lower.Contains("latest order"))
            return new CommandInterpretation("TrackLatestOrder", "");

        if (lower.Contains("my order") || lower.Contains("orders"))
            return new CommandInterpretation("ViewOrders", "");

        if (lower.Contains("repeat") || lower.Contains("relisten"))
            return new CommandInterpretation("Repeat", "");

        if (lower.Contains("back"))
            return new CommandInterpretation("GoBack", "");

        if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("goodbye") || lower.Contains("close"))
            return new CommandInterpretation("Exit", "");

        if (lower.Contains("search") || lower.Contains("find") || lower.Contains("look for") || lower.Contains("product"))
        {
            var query = ExtractSearchTerm(lower);
            return new CommandInterpretation("SearchProduct", query);
        }

        return new CommandInterpretation("Unknown", "");
    }

    private static string ExtractSearchTerm(string text)
    {
        var phrasesToRemove = new[]
        {
            "search for",
            "search",
            "find",
            "look for",
            "product",
            "a",
            "an",
            "the"
        };

        var cleaned = text;

        foreach (var phrase in phrasesToRemove)
        {
            cleaned = cleaned.Replace(phrase, "", StringComparison.OrdinalIgnoreCase);
        }

        return cleaned.Trim();
    }
}