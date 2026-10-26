# Creating a built-in menu

Built-ins are compiled with HIT. For distribution without rebuilding HIT, use an external [extension](EXTENSIONS.md).
Both follow the shared [menu interaction contract](MENUS.md), but their registration and service access differ.

## Minimal entry and public CSS

Create `Components/HitMenus/HitMenuAgenda.razor`:

```razor
@attribute [HoldItTogether.Services.HitMenus.HitMenuTitle("Agenda")]

<section class="agenda-menu" data-no-drag>
    <h2>@Text.Get("Today")</h2>
    <p>@Text.Get("Your agenda is empty.")</p>
</section>
```

Add those source messages to `Resources/en.json`. The `Text` alias is available through `Components/_Imports.razor`.
Keep the namespace `HoldItTogether.Components.HitMenus` or a subnamespace.
The entry must be public, concrete, implement IComponent, have no open generic parameters and be named `HitMenu{Feature}`.
The title attribute is optional; absent it, the name is split into words.
The stable built-in ID is the full CLR type name, independent of the title.

Create `wwwroot/css/menus/HitMenuAgenda.css`:

```css
:where(.hit-menu-panel[data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAgenda"]) .agenda-menu {
    width: 440px;
    color: var(--hit-menu-text, var(--hit-text));
    font-family: var(--hit-menu-font, var(--hit-font));
}
```

Scope every rule, including grouped selectors and rules inside @media.
Rebuild/run: the catalog discovers the component and MSBuild regenerates `wwwroot/css/menus.css` imports.
Do not hand-edit that generated index or register a new UI root in Program.cs.
Component discovery does not automatically register business services.

## Services and context

Place feature services in `Services/Agenda`, register them in Program.cs and inject them into the entry component.
Keep Windows/HTTP/files/streaming in services; use snapshots and commands at the UI boundary.
Singleton is for shared, synchronized state; a singleton must not retain a view or ElementReference.
Use a disposable lifetime for independent view resources.

```razor
@using HoldItTogether.Services.HitMenus

@code {
    [CascadingParameter(Name = "HitMenu")]
    public HitMenuContext? MenuContext { get; set; }
}
```

Read IsActive/IsVisible/IsPinned and request title, close or layout through that context.
For configuration, implement `HoldItTogether.Services.HitMenus.IHitMenuSettings` (the SDK-compatible interface).
Do not create a second `HitMenu...` entry for settings: put supporting views in `Components/Agenda`.
Use `HitDisclosure` for title/main-control/chevron rows and render expensive expanded fields conditionally.
Use `HitScrollRegion` for local scrolling, and `HitMenuViewport` only where X/Y scaling is part of the feature.

## Host and runtime changes

Menus are discovered initially by type name, then combined with extensions and sorted by saved order.
Settings remains last and protected. Preserve its existing CLR ID when changing display labels.
The host keeps instances by stable ID so reorder never reassigns another menu's state.
Hidden views pause presentation; disabling/removing disposes them.

If a service needs work while the view is hidden, document why, who owns the task and how shutdown cancels it.
Clock's persisted deadline worker and Audio's optional global palette metadata are examples.
AlwaysOnTop/tray/global preferences use HitSettingsService, not direct writes from the component.

## Definition of done

- One clear entry/title with explicit main/detail/settings state and useful empty/error states.
- M2 return, Alt/Ctrl hints, keyboard actions and local wheel behavior follow [Menus](MENUS.md).
- Public, scoped CSS works with all bundled themes and preserves optional music artwork/palette.
- Natural sizing/compact/scaling do not cause resize loops or inaccessible content.
- No view polling/capture while hidden without a documented requirement.
- Settings persist with normalized bounds; stale asynchronous results cannot update a new selection.
- Resource disposal and meaningful behavior checks are complete.
- Update the feature, theme and API guides when their contracts change.
