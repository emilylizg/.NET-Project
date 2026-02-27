using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly string _connectionString;

        public TransactionController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        // GET /api/transaction/all
        [HttpGet("all")]
        public async Task<IActionResult> GetUserTransactions()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(@"
                    SELECT Id, Title, Category, Type, Amount, TransactionDate
                    FROM Transactions
                    WHERE UserId = @UserId
                    ORDER BY TransactionDate DESC", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        id             = reader.GetInt32(reader.GetOrdinal("Id")),
                        title          = reader.GetString(reader.GetOrdinal("Title")),
                        category       = reader.GetString(reader.GetOrdinal("Category")),
                        type           = reader.GetString(reader.GetOrdinal("Type")),
                        amount         = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        transactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")).ToString("yyyy-MM-dd")
                    });
                }

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Transaction fetch error: {ex}");
                return StatusCode(500, new { message = "Server error" });
            }
        }

        // GET /api/transaction/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(@"
                    SELECT 
                        SUM(CASE WHEN Type = 'Income'  THEN Amount ELSE 0 END) AS Income,
                        SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END) AS Expense
                    FROM Transactions
                    WHERE UserId = @UserId", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();

                decimal income = 0, expense = 0;
                if (await reader.ReadAsync())
                {
                    income  = reader.IsDBNull(reader.GetOrdinal("Income"))  ? 0 : reader.GetDecimal(reader.GetOrdinal("Income"));
                    expense = reader.IsDBNull(reader.GetOrdinal("Expense")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Expense"));
                }

                return Ok(new
                {
                    income,
                    expense,
                    savings = income - expense
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return StatusCode(500, new { message = "Server error" });
            }
        }

        // POST /api/transaction
        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] TransactionRequest req)
        {
            var userId = GetUserId();
            Console.WriteLine($"[AddTransaction] userId: {userId}");
            Console.WriteLine($"[AddTransaction] body: title={req.Title}, amount={req.Amount}, type={req.Type}, category={req.Category}, date={req.TransactionDate}");

            if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(req.Title)    ||
                req.Amount == null                       ||
                string.IsNullOrWhiteSpace(req.Type)     ||
                string.IsNullOrWhiteSpace(req.Category) ||
                string.IsNullOrWhiteSpace(req.TransactionDate))
                return BadRequest(new { message = "All fields are required" });

            if (req.Type != "Income" && req.Type != "Expense")
                return BadRequest(new { message = "Invalid transaction type" });

            var allowedCategories = new[] { "Food", "Travel", "Medical", "Utilities", "Others" };
            if (!allowedCategories.Contains(req.Category))
                return BadRequest(new { message = $"Invalid category. Allowed: {string.Join(", ", allowedCategories)}" });

            if (!DateTime.TryParse(req.TransactionDate, out var parsedDate))
                return BadRequest(new { message = "Invalid transactionDate format" });

            var sqlDate = parsedDate.ToString("yyyy-MM-dd");

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(@"
                    INSERT INTO Transactions (UserId, Title, Amount, Type, Category, TransactionDate)
                    VALUES (@UserId, @Title, @Amount, @Type, @Category, @TransactionDate)", conn);

                cmd.Parameters.AddWithValue("@UserId",          userId);
                cmd.Parameters.AddWithValue("@Title",           req.Title);
                cmd.Parameters.AddWithValue("@Amount",          req.Amount!.Value);
                cmd.Parameters.AddWithValue("@Type",            req.Type);
                cmd.Parameters.AddWithValue("@Category",        req.Category);
                cmd.Parameters.AddWithValue("@TransactionDate", sqlDate);

                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine($"[AddTransaction] inserted for userId: {userId}");
                return StatusCode(201, new { message = "Transaction added successfully" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AddTransaction] ERROR: {ex}");
                return StatusCode(500, new { message = "Server error", detail = ex.Message });
            }
        }
        // GET /api/transaction/dashboard?period=monthly
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboardData([FromQuery] string period = "monthly")
{
    var userId = GetUserId();
    if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

    var dateFilter = period switch
    {
        "daily"  => "CAST(TransactionDate AS DATE) = CAST(GETDATE() AS DATE)",
        "weekly" => "TransactionDate >= DATEADD(DAY, -7, GETDATE())",
        _        => "TransactionDate >= DATEADD(MONTH, -1, GETDATE())"
    };

    try
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand($@"
            SELECT Category, Type, SUM(Amount) AS Total
            FROM Transactions
            WHERE UserId = @UserId
            AND {dateFilter}
            GROUP BY Category, Type", conn);

        cmd.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        var response = new Dictionary<string, decimal>
        {
            { "Food",      0 },
            { "Medical",   0 },
            { "Utilities", 0 },
            { "Others",    0 },
            { "Travel",    0 },
            { "Income",    0 }
        };

        while (await reader.ReadAsync())
        {
            var type     = reader.GetString(reader.GetOrdinal("Type"));
            var category = reader.GetString(reader.GetOrdinal("Category"));
            var total    = reader.GetDecimal(reader.GetOrdinal("Total"));

            if (type == "Income")
                response["Income"] += total;
            else if (response.ContainsKey(category))
                response[category] += total;
        }

        return Ok(response);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Dashboard error: {ex}");
        return StatusCode(500, new { message = "Server error" });
    }
    }
    
        // GET /api/transaction?page=1&limit=10
        [HttpGet]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

            var offset = (page - 1) * limit;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(@"
                    SELECT *
                    FROM Transactions
                    WHERE UserId = @UserId
                    ORDER BY TransactionDate DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @Limit ROWS ONLY", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Offset", offset);
                cmd.Parameters.AddWithValue("@Limit",  limit);

                await using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        id              = reader.GetInt32(reader.GetOrdinal("Id")),
                        userId          = reader.GetInt32(reader.GetOrdinal("UserId")),
                        title           = reader.GetString(reader.GetOrdinal("Title")),
                        category        = reader.GetString(reader.GetOrdinal("Category")),
                        type            = reader.GetString(reader.GetOrdinal("Type")),
                        amount          = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        transactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")).ToString("yyyy-MM-dd")
                    });
                }

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return StatusCode(500, new { message = "Server error" });
            }
        }

        // PUT /api/transaction/:id
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] TransactionRequest req)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized" });

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(@"
                    UPDATE Transactions
                    SET Title           = @Title,
                        Amount          = @Amount,
                        Type            = @Type,
                        Category        = @Category,
                        TransactionDate = @TransactionDate
                    WHERE Id = @Id AND UserId = @UserId", conn);

                cmd.Parameters.AddWithValue("@Id",              id);
                cmd.Parameters.AddWithValue("@UserId",          userId);
                cmd.Parameters.AddWithValue("@Title",           req.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount",          req.Amount.HasValue ? req.Amount.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Type",            req.Type ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Category",        req.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TransactionDate", req.TransactionDate ?? (object)DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                    return NotFound(new { message = "Transaction not found" });

                return Ok(new { message = "Transaction updated successfully" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return StatusCode(500, new { message = "Server error" });
            }
        }
    }

    public class TransactionRequest
    {
        public string?  Title           { get; set; }
        public decimal? Amount          { get; set; }
        public string?  Type            { get; set; }
        public string?  Category        { get; set; }
        public string?  TransactionDate { get; set; }
    }
}