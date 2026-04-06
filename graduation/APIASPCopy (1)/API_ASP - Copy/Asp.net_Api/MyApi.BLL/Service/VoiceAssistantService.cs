using System;
using System.Linq;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;
using MyApi.DAL.Repository;

namespace MyApi.BLL.Service;

public class VoiceAssistantService : IVoiceAssistantService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IOpenAiService _openAiService;

    private static string _lastAction = "";
    private static string _lastQuery = "";

    public VoiceAssistantService(IStoreRepository storeRepository, IOpenAiService openAiService)
    {
        _storeRepository = storeRepository;
        _openAiService = openAiService;
    }

    public async Task<VoiceResponseDto> ExecuteCommandAsync(string text, string? language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new VoiceResponseDto
            {
                Action = "None",
                ReplyText = "I did not hear anything. Please try again.",
                ScreenText = "No input received.",
                RecognizedText = "",
                CorrectedText = "",
                NeedsConfirmation = false,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        var interpretation = await _openAiService.InterpretCommandAsync(text, language);

        if (interpretation is null)
            interpretation = RuleBasedFallback(text, language);

        var action = interpretation.Action?.Trim() ?? "Unknown";
        var queryText = interpretation.Query?.Trim() ?? string.Empty;
        var correctedText = interpretation.CorrectedText?.Trim();
        var confidence = interpretation.Confidence;
        var needsConfirmation = interpretation.NeedsConfirmation;

        if (needsConfirmation || confidence < 0.65)
        {
            return new VoiceResponseDto
            {
                Action = "Repeat",
                ReplyText = "I did not understand what you said. Please repeat your request.",
                ScreenText = "I did not understand. Please say it again.",
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = true,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        if (string.IsNullOrWhiteSpace(correctedText))
            correctedText = text;

        if (!string.IsNullOrWhiteSpace(action) && action != "Unknown")
            _lastAction = action;

        if (!string.IsNullOrWhiteSpace(queryText))
            _lastQuery = queryText;

        var products = _storeRepository.GetProducts();
        var cartItems = _storeRepository.GetCartItems();
        var orders = _storeRepository.GetOrders();

        var allowedActions = new[]
        {
            "SearchProduct",
            "ViewCart",
            "TrackLatestOrder",
            "ViewOrders",
            "Repeat",
            "GoBack",
            "Exit",
            "GeneralConversation"
        };

        if (!allowedActions.Contains(action))
        {
            return new VoiceResponseDto
            {
                Action = "Repeat",
                ReplyText = "Your request is not one of the available options. Please repeat your request.",
                ScreenText = "That option is not available. Please say it again.",
                RecognizedText = text,
                CorrectedText = correctedText ?? text,
                NeedsConfirmation = true,
                SuggestedAction = "",
                SuggestedQuery = ""
            };
        }

        switch (action)
        {
            case "SearchProduct":
            {
                if (string.IsNullOrWhiteSpace(queryText))
                {
                    return new VoiceResponseDto
                    {
                        Action = "SearchProduct",
                        ReplyText = "You selected product search. Please say the product name.",
                        ScreenText = "Search mode activated. Waiting for product name.",
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = ""
                    };
                }

                var normalizedQuery = queryText.Trim().ToLowerInvariant();

                var queryWords = normalizedQuery
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 1)
                    .ToList();

                var matchedProducts = products
                    .Where(p => p.Translations != null &&
                                p.Translations.Any(t =>
                                    (!string.IsNullOrWhiteSpace(t.Name) &&
                                     (
                                         t.Name.ToLower().Contains(normalizedQuery) ||
                                         queryWords.Any(word => t.Name.ToLower().Contains(word))
                                     )) ||
                                    (!string.IsNullOrWhiteSpace(t.Description) &&
                                     (
                                         t.Description.ToLower().Contains(normalizedQuery) ||
                                         queryWords.Any(word => t.Description.ToLower().Contains(word))
                                     ))))
                    .Take(3)
                    .ToList();

                if (matchedProducts.Count == 0)
                {
                    return new VoiceResponseDto
                    {
                        Action = "SearchProduct",
                        ReplyText = $"I could not find any product matching {queryText}.",
                        ScreenText = $"No products found for: {queryText}",
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = ""
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
                    ReplyText = $"I found {matchedProducts.Count} product(s) for {queryText}. {spokenResults}.",
                    ScreenText = screenResults,
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = "",
                    Data = matchedProducts
                };
            }

            case "GeneralConversation":
                return new VoiceResponseDto
                {
                    Action = "GeneralConversation",
                    ReplyText = GetNaturalReply(correctedText ?? text, language),
                    ScreenText = GetNaturalReply(correctedText ?? text, language),
                    RecognizedText = text,
                    CorrectedText = correctedText ?? text,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };

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
                        ScreenText = "Cart is empty.",
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = ""
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
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = "",
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
                        ScreenText = "No latest order found.",
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = ""
                    };
                }

                return new VoiceResponseDto
                {
                    Action = "TrackLatestOrder",
                    ReplyText = $"Your latest order id is {latestOrder.Id}. Its status is {latestOrder.OrderStatus}.",
                    ScreenText = $"Latest order: {latestOrder.Id} - {latestOrder.OrderStatus}",
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = "",
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
                        ScreenText = "No orders found.",
                        RecognizedText = text,
                        CorrectedText = correctedText,
                        NeedsConfirmation = false,
                        SuggestedAction = "",
                        SuggestedQuery = ""
                    };
                }

                var spokenOrders = string.Join(" . ", orders.Select(o =>
                    $"Order {o.Id}, status {o.OrderStatus}"));

                var screenOrders = string.Join(" | ", orders.Select(o =>
                    $"{o.Id} - {o.OrderStatus}"));

                return new VoiceResponseDto
                {
                    Action = "ViewOrders",
                    ReplyText = $"You have {orders.Count} order(s). {spokenOrders}.",
                    ScreenText = screenOrders,
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = "",
                    Data = orders
                };
            }

            case "Repeat":
                return new VoiceResponseDto
                {
                    Action = "Repeat",
                    ReplyText = "Repeating the available options. Search for a product, view cart items, track your latest order, or view my order.",
                    ScreenText = "Options repeated.",
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };

            case "GoBack":
                return new VoiceResponseDto
                {
                    Action = "GoBack",
                    ReplyText = "Going back to the main menu.",
                    ScreenText = "Back to main menu.",
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };

            case "Exit":
                return new VoiceResponseDto
                {
                    Action = "Exit",
                    ReplyText = "Exiting the assistant. Goodbye.",
                    ScreenText = "Session ended.",
                    RecognizedText = text,
                    CorrectedText = correctedText,
                    NeedsConfirmation = false,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };

            default:
                return new VoiceResponseDto
                {
                    Action = "Repeat",
                    ReplyText = "I did not understand what you said. Please repeat your request.",
                    ScreenText = "I did not understand. Please say it again.",
                    RecognizedText = text,
                    CorrectedText = correctedText ?? text,
                    NeedsConfirmation = true,
                    SuggestedAction = "",
                    SuggestedQuery = ""
                };
        }
    }

    private static CommandInterpretation RuleBasedFallback(string text, string? language)
    {
        var lower = text.Trim().ToLowerInvariant();
        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language.ToLowerInvariant();

        if (selectedLanguage == "ar")
        {
            if (lower.Contains("مرحبا") || lower.Contains("هلو") || lower.Contains("اهلا") ||
                lower.Contains("كيفك") || lower.Contains("شلونك") || lower.Contains("شكرا"))
            {
                return new CommandInterpretation
                {
                    Action = "GeneralConversation",
                    Query = "",
                    CorrectedText = text,
                    Confidence = 0.95,
                    NeedsConfirmation = false
                };
            }
        }

        if (selectedLanguage == "en")
        {
            if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey") ||
                lower.Contains("how are you") || lower.Contains("thanks") || lower.Contains("thank you"))
            {
                return new CommandInterpretation
                {
                    Action = "GeneralConversation",
                    Query = "",
                    CorrectedText = text,
                    Confidence = 0.95,
                    NeedsConfirmation = false
                };
            }
        }

        if (lower.Contains("cart"))
        {
            return new CommandInterpretation
            {
                Action = "ViewCart",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("track") || lower.Contains("latest order"))
        {
            return new CommandInterpretation
            {
                Action = "TrackLatestOrder",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("my order") || lower.Contains("orders"))
        {
            return new CommandInterpretation
            {
                Action = "ViewOrders",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("repeat") || lower.Contains("relisten"))
        {
            return new CommandInterpretation
            {
                Action = "Repeat",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("back"))
        {
            return new CommandInterpretation
            {
                Action = "GoBack",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("goodbye") || lower.Contains("close"))
        {
            return new CommandInterpretation
            {
                Action = "Exit",
                Query = "",
                CorrectedText = text,
                Confidence = 0.90,
                NeedsConfirmation = false
            };
        }

        if (lower.Contains("search") || lower.Contains("find") || lower.Contains("look for") || lower.Contains("product"))
        {
            var extractedQuery = ExtractSearchTerm(lower);

            return new CommandInterpretation
            {
                Action = "SearchProduct",
                Query = extractedQuery,
                CorrectedText = text,
                Confidence = string.IsNullOrWhiteSpace(extractedQuery) ? 0.60 : 0.85,
                NeedsConfirmation = false
            };
        }

        return new CommandInterpretation
        {
            Action = "Unknown",
            Query = "",
            CorrectedText = text,
            Confidence = 0.30,
            NeedsConfirmation = true
        };
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

    private static string GetNaturalReply(string text, string? language)
    {
        var lower = text.Trim().ToLowerInvariant();
        var selectedLanguage = string.IsNullOrWhiteSpace(language) ? "ar" : language.ToLowerInvariant();

        if (selectedLanguage == "ar")
        {
            if (lower.Contains("مرحبا") || lower.Contains("هلو") || lower.Contains("اهلا"))
                return "أهلا، كيف بقدر أساعدك؟";

            if (lower.Contains("كيفك") || lower.Contains("شلونك"))
                return "أنا بخير، كيف بقدر أساعدك اليوم؟";

            if (lower.Contains("شكرا"))
                return "على الرحب والسعة، كيف بقدر أساعدك كمان؟";

            return "أكيد، كيف بقدر أساعدك؟";
        }

        if (selectedLanguage == "en")
        {
            if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("hey"))
                return "Hello, how can I help you?";

            if (lower.Contains("how are you"))
                return "I am fine. How can I help you today?";

            if (lower.Contains("thanks") || lower.Contains("thank you"))
                return "You're welcome. How else can I help you?";

            return "Sure, how can I help you?";
        }

        return "How can I help you?";
    }
}