using Microsoft.AspNetCore.Components;

namespace HoldItTogether.Services.HitMenus;

public sealed record HitMenuContext(bool IsActive, EventCallback CloseRequested);
