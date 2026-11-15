using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class DepositsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private const decimal StartBalance = 1000m;
    private const string ValidCard = "4532710938428896";
    private const string Cvv = "737";

    private static DepositCreateDto NewCardDeposit(int userId, decimal amount, bool saveCard = false) => new()
    {
        UserId = userId,
        Amount = amount,
        HolderName = "Test Holder",
        CardNumber = ValidCard,
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 2,
        Cvv = Cvv,
        SaveCard = saveCard
    };

    private static DepositCreateDto SavedCardDeposit(int userId, decimal amount, int cardId) => new()
    {
        UserId = userId,
        Amount = amount,
        SavedCardId = cardId,
        Cvv = Cvv
    };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, DepositCreateDto dto) =>
        client.PostAsJsonAsync("/api/deposits", dto);

    private static async Task<decimal> FiatAsync(TestUser user)
    {
        var portfolio = await user.Client.GetFromJsonAsync<PortfolioDto>($"/api/users/{user.Id}/portfolio");
        return portfolio!.FiatBalance;
    }

    private static async Task<int> SaveCardAsync(TestUser user)
    {
        var response = await PostAsync(user.Client, NewCardDeposit(user.Id, 50m, saveCard: true));
        response.EnsureSuccessStatusCode();
        var cards = await user.Client.GetFromJsonAsync<List<SavedCardGetDto>>($"/api/users/{user.Id}/cards");
        return Assert.Single(cards!).Id;
    }

    [Fact]
    public async Task Post_deposit_returns201_and_raises_balance()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await PostAsync(user.Client, NewCardDeposit(user.Id, 250.50m));
        var body = await response.Content.ReadFromJsonAsync<DepositGetDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(250.50m, body!.Amount);
        Assert.Equal("8896", body.Last4);
        Assert.Equal(StartBalance + 250.50m, body.NewBalance);
        Assert.Equal(StartBalance + 250.50m, await FiatAsync(user));
    }

    [Theory]
    [InlineData("9.99")]
    [InlineData("100000.01")]
    [InlineData("10.001")]
    [InlineData("0")]
    [InlineData("-5")]
    public async Task Post_deposit_returns400_for_each_review_focus_amount(string amount)
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await PostAsync(user.Client, NewCardDeposit(user.Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(StartBalance, await FiatAsync(user));
    }

    [Fact]
    public async Task Post_deposit_returns400_when_card_is_invalid()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var dto = NewCardDeposit(user.Id, 50m);
        dto.CardNumber = "4532710938428897";

        var response = await PostAsync(user.Client, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_deposit_returns400_when_holder_name_is_over_100_chars()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var dto = NewCardDeposit(user.Id, 50m, saveCard: true);
        dto.HolderName = new string('A', 101);

        var response = await PostAsync(user.Client, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_deposit_returns400_when_cvv_is_missing()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var dto = NewCardDeposit(user.Id, 50m);
        dto.Cvv = string.Empty;

        var response = await PostAsync(user.Client, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_deposit_returns403_for_other_users_id()
    {
        var owner = await TestApi.CreateUserAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);

        var response = await PostAsync(other.Client, NewCardDeposit(owner.Id, 50m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(StartBalance, await FiatAsync(owner));
    }

    [Fact]
    public async Task Post_deposit_returns404_for_other_users_card()
    {
        var owner = await TestApi.CreateUserAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(owner);

        var response = await PostAsync(other.Client, SavedCardDeposit(other.Id, 50m, cardId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(StartBalance, await FiatAsync(other));
    }

    [Fact]
    public async Task Post_deposit_returns401_without_token()
    {
        using var client = Factory.CreateClient();

        var response = await PostAsync(client, NewCardDeposit(1, 50m));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_gets_403_on_every_route_for_another_users_id()
    {
        var owner = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(owner);
        var admin = await TestApi.CreateAdminAsync(Factory);

        var post = await PostAsync(admin.Client, NewCardDeposit(owner.Id, 50m));
        var deposits = await admin.Client.GetAsync($"/api/users/{owner.Id}/deposits");
        var cards = await admin.Client.GetAsync($"/api/users/{owner.Id}/cards");
        var delete = await admin.Client.DeleteAsync($"/api/users/{owner.Id}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deposits.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cards.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Get_routes_and_delete_return_403_for_other_users_id()
    {
        var owner = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(owner);
        var other = await TestApi.CreateUserAsync(Factory);

        var deposits = await other.Client.GetAsync($"/api/users/{owner.Id}/deposits");
        var cards = await other.Client.GetAsync($"/api/users/{owner.Id}/cards");
        var delete = await other.Client.DeleteAsync($"/api/users/{owner.Id}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.Forbidden, deposits.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cards.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Get_routes_and_delete_return_401_without_token()
    {
        using var client = Factory.CreateClient();

        var deposits = await client.GetAsync("/api/users/1/deposits");
        var cards = await client.GetAsync("/api/users/1/cards");
        var delete = await client.DeleteAsync("/api/users/1/cards/1");

        Assert.Equal(HttpStatusCode.Unauthorized, deposits.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cards.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task Delete_card_returns404_for_other_users_card()
    {
        var owner = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(owner);
        var other = await TestApi.CreateUserAsync(Factory);

        var response = await other.Client.DeleteAsync($"/api/users/{other.Id}/cards/{cardId}");
        var cards = await owner.Client.GetFromJsonAsync<List<SavedCardGetDto>>($"/api/users/{owner.Id}/cards");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(cards!);
    }

    [Fact]
    public async Task Get_deposits_is_newest_first_and_owner_only()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        (await PostAsync(user.Client, NewCardDeposit(user.Id, 11m))).EnsureSuccessStatusCode();
        (await PostAsync(user.Client, NewCardDeposit(user.Id, 22m))).EnsureSuccessStatusCode();
        (await PostAsync(user.Client, NewCardDeposit(user.Id, 33m))).EnsureSuccessStatusCode();

        var history = await user.Client.GetFromJsonAsync<List<DepositHistoryDto>>($"/api/users/{user.Id}/deposits");

        Assert.Equal(new[] { 33m, 22m, 11m }, history!.Select(d => d.Amount).ToArray());
    }

    [Fact]
    public async Task Get_cards_lists_saved_card_without_full_number()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var cardId = await SaveCardAsync(user);
        var cards = await user.Client.GetFromJsonAsync<List<SavedCardGetDto>>($"/api/users/{user.Id}/cards");

        var card = Assert.Single(cards!);
        Assert.Equal(cardId, card.Id);
        Assert.Equal("8896", card.Last4);
    }

    [Fact]
    public async Task Delete_card_returns204_and_history_keeps_last4()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(user);

        var delete = await user.Client.DeleteAsync($"/api/users/{user.Id}/cards/{cardId}");
        var cards = await user.Client.GetFromJsonAsync<List<SavedCardGetDto>>($"/api/users/{user.Id}/cards");
        var history = await user.Client.GetFromJsonAsync<List<DepositHistoryDto>>($"/api/users/{user.Id}/deposits");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(cards!);
        Assert.Equal("8896", Assert.Single(history!).Last4);
    }

    [Fact]
    public async Task Delete_card_returns404_when_already_deleted()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(user);
        await user.Client.DeleteAsync($"/api/users/{user.Id}/cards/{cardId}");

        var again = await user.Client.DeleteAsync($"/api/users/{user.Id}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Post_deposit_with_saved_card_returns201()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var cardId = await SaveCardAsync(user);

        var response = await PostAsync(user.Client, SavedCardDeposit(user.Id, 100m, cardId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(StartBalance + 150m, await FiatAsync(user));
    }

    [Fact]
    public async Task Response_and_db_contain_no_full_number_or_cvv()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var post = await PostAsync(user.Client, NewCardDeposit(user.Id, 75m, saveCard: true));
        var postBody = await post.Content.ReadAsStringAsync();
        var historyBody = await user.Client.GetStringAsync($"/api/users/{user.Id}/deposits");
        var cardsBody = await user.Client.GetStringAsync($"/api/users/{user.Id}/cards");

        string cardRows;
        string depositRows;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
            var cards = await db.Database
                .SqlQuery<string>($"SELECT row_to_json(c)::text AS \"Value\" FROM \"SavedCards\" c WHERE c.\"UserId\" = {user.Id}")
                .ToListAsync();
            var deposits = await db.Database
                .SqlQuery<string>($"SELECT row_to_json(d)::text AS \"Value\" FROM \"Deposits\" d JOIN \"Wallets\" w ON w.\"Id\" = d.\"WalletId\" WHERE w.\"UserId\" = {user.Id}")
                .ToListAsync();
            Assert.NotEmpty(cards);
            Assert.NotEmpty(deposits);
            cardRows = string.Join("\n", cards);
            depositRows = string.Join("\n", deposits);
        }

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        foreach (var text in new[] { postBody, historyBody, cardsBody, cardRows, depositRows })
        {
            Assert.DoesNotContain(ValidCard, text);
            Assert.DoesNotContain($"\"{Cvv}\"", text);
            Assert.DoesNotContain("cvv", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Buy_works_after_deposit()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 100m);
        (await PostAsync(user.Client, NewCardDeposit(user.Id, 500m))).EnsureSuccessStatusCode();

        var buy = await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 15m);

        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        Assert.Equal(StartBalance + 500m - 1500m, await FiatAsync(user));
    }
}
