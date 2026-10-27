using Microsoft.AspNetCore.Components;

namespace HoldItTogether.Services.HitMenus;

public sealed record HitMenuContext(bool IsActive, EventCallback CloseRequested, bool IsVisible = true,
    EventCallback<string> TitleRequested = default, bool IsPinned = false, EventCallback<HitMenuLayout> LayoutRequested = default, bool IsHostVisible = true);
