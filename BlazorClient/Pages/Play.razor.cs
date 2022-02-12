using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing;

namespace Snakes.Client.Pages;

public partial class Play
{
    BECanvas? _canvas;
    Canvas2DContext? _context;
    ElementReference _spritesheet;
    Point _spritePosition = Point.Empty;
    Point _spriteDirection = new Point(1, 1);
    Sprite _sprite;
    readonly float _spriteSpeed = 0.25f;
    readonly GameTime _gameTime = new GameTime();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;
        _context = await _canvas.CreateCanvas2DAsync();
        await JSRuntime.InvokeAsync<object>("initGame", DotNetObjectReference.Create(this));

        _sprite = new Sprite()
        {
            SpriteSheet = _spritesheet
        };
    }

    [JSInvokable]
    public async ValueTask GameLoop(float timeStamp, int screenWidth, int screenHeight)
    {
        _gameTime.TotalTime = timeStamp;

        await Update(screenWidth, screenHeight);
        await Render(screenWidth, screenHeight);
    }

    private async ValueTask Update(int screenWidth, int screenHeight)
    {
        _sprite.Size = new Size(screenWidth / 96, screenHeight / 24);
        if (_spritePosition.X + _sprite.Size.Width >= screenWidth && _spriteDirection.X > 0)
            _spriteDirection.X = -Math.Abs(_spriteDirection.X);

        if (_spritePosition.X < 0)
            _spriteDirection.X = Math.Abs(_spriteDirection.X);

        if (_spritePosition.Y + _sprite.Size.Height >= screenHeight)
            _spriteDirection.Y = -Math.Abs(_spriteDirection.Y);

        if (_spritePosition.Y < 0)
            _spriteDirection.Y = Math.Abs(_spriteDirection.Y);

        _spritePosition.X += (int)(_spriteDirection.X * _spriteSpeed * _gameTime.ElapsedTime);
        _spritePosition.Y += (int)(_spriteDirection.Y * _spriteSpeed * _gameTime.ElapsedTime);
    }

    private async ValueTask Render(int width, int height)
    {
        if (_context == null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        await _context.ClearRectAsync(0, 0, width, height);
        await _context.DrawImageAsync(_sprite.SpriteSheet, _spritePosition.X, _spritePosition.Y, _sprite.Size.Width, _sprite.Size.Height);
    }
}

public class Sprite
{
    public Size Size { get; set; }
    public ElementReference SpriteSheet { get; set; }
}

public class GameTime
{
    private float _totalTime = 0;

    /// <summary>
    /// total time elapsed since the beginning of the game
    /// </summary>
    public float TotalTime
    {
        get => _totalTime;
        set
        {
            this.ElapsedTime = value - _totalTime;
            _totalTime = value;

        }
    }

    /// <summary>
    /// time elapsed since last frame
    /// </summary>
    public float ElapsedTime { get; private set; }
}
