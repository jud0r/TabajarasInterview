using Microsoft.JSInterop;

namespace TabajarasInterview.Web.Services.Auth
{
    public class CookieService(IJSRuntime js)
    {
        public async Task SetAsync(string name, string value, int expirationInSeconds = 3600)
        { 
            var encoded = Uri.EscapeDataString(value);
            await js.InvokeVoidAsync("eval",
                $"document.cookie = \"{name}={encoded}; path=/; max-age={expirationInSeconds}; samesite=strict\" + (location.protocol === 'https:' ? '; secure' : '')");
        }

        public async Task<string?> GetAsync(string name)
        {
            var result = await js.InvokeAsync<string?>("eval",
                $"(document.cookie.split('; ').find(c => c.startsWith('{name}=')) || '').substring('{name}='.length) || null");

            return result is not null ? Uri.UnescapeDataString(result) : null;
        }

        public async Task RemoveAsync(string name)
        {
            await js.InvokeVoidAsync("eval",
                $"document.cookie = \"{name}=; path=/; max-age=0; samesite=strict\" + (location.protocol === 'https:' ? '; secure' : '')");
        }
    }
}
