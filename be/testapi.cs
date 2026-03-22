using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5254") };
        var registerObj = new { CompanyName = "Test", Subdomain = "test-comp", Email = "test@test.com", Password = "password123", Name = "Test User" };
        var res = await client.PostAsJsonAsync("/api/Auth/register-tenant", registerObj);
        
        string token = "";
        if (res.IsSuccessStatusCode) {
            var content = await res.Content.ReadFromJsonAsync<JsonElement>();
            token = content.GetProperty("token").GetString();
        } else {
            var loginObj = new { Email = "test@test.com", Password = "password123", TenantSubdomain = "test-comp" };
            var res2 = await client.PostAsJsonAsync("/api/Auth/login", loginObj);
            if (res2.IsSuccessStatusCode) {
                var content = await res2.Content.ReadFromJsonAsync<JsonElement>();
                token = content.GetProperty("token").GetString();
            } else {
                Console.WriteLine("Auth failed: " + await res2.Content.ReadAsStringAsync());
                return;
            }
        }
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var endpoints = new[] {
            "/api/AmcContracts",
            "/api/AmcVisits",
            "/api/Auth/me",
            "/api/Customers",
            "/api/Dashboard/summary",
            "/api/Installations",
            "/api/Leads",
            "/api/OpsTasks",
            "/api/Products",
            "/api/Quotations",
            "/api/ServiceRequests",
            "/api/Sites"
        };
        
        foreach (var ep in endpoints) {
            var r = await client.GetAsync(ep);
            Console.WriteLine($"GET {ep} - {(int)r.StatusCode} {r.StatusCode}");
            if (!r.IsSuccessStatusCode) {
                Console.WriteLine(await r.Content.ReadAsStringAsync());
            }
        }
    }
}
