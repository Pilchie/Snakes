using Microsoft.JSInterop;

namespace Snakes.BlazorUI;

public class GameJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private DotNetObjectReference<SnakeCanvas>? _instanceRef;

    public GameJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorUI/GameJsInterop.js").AsTask());
    }

    public async ValueTask InitializeGame(SnakeCanvas instance)
    {
        var module = await _moduleTask.Value;
        _instanceRef = DotNetObjectReference.Create(instance);
        await module.InvokeAsync<object>("initGame", _instanceRef);
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_instanceRef is not null)
        {
            _instanceRef.Dispose();
        }

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
