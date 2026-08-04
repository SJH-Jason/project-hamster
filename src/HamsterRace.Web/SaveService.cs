using System.Text.Json;
using Microsoft.JSInterop;

namespace HamsterRace.Web;

/// <summary>玩家存檔讀寫（瀏覽器 localStorage）。</summary>
public class SaveService
{
    private const string Key = "hamster_save_v1";
    private readonly IJSRuntime _js;
    public SaveService(IJSRuntime js) => _js = js;

    public async Task<PlayerSave?> LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<PlayerSave>(json);
        }
        catch { return null; }
    }

    public async Task SaveAsync(PlayerSave s)
    {
        try
        {
            var json = JsonSerializer.Serialize(s);
            await _js.InvokeVoidAsync("localStorage.setItem", Key, json);
        }
        catch { }
    }

    public async Task ClearAsync()
    {
        try { await _js.InvokeVoidAsync("localStorage.removeItem", Key); } catch { }
    }
}
