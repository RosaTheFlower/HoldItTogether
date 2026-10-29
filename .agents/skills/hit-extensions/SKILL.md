---
name: hit-extensions
description: "Author or evolve HIT SDK contracts, independent .hitmenu extensions, package loading/updates, storage isolation, capabilities, events, actions or asset boundaries. Use for Modules/HIT.Sdk and the host extension bridge; use hit-ui for built-in menu-only changes."
---

# HIT SDK and extension workflow

## Establish the public contract first

Read [API](../../../Docs/API.md) and [Extensions](../../../Docs/EXTENSIONS.md).
Use [Architecture](../../../Docs/ARCHITECTURE.md) for host ownership and
[Menus](../../../Docs/MENUS.md) for extension view behavior. Check current versions in
[Docs/README](../../../Docs/README.md) and the relevant project files; package version,
SDK assembly identity, theme contract and `.hitmenu` format are distinct.

## Implement through the independent boundary

1. Classify the change as authoring a package, adding an optional SDK capability, changing a
   host adapter, or altering installation/update behavior. Trace the contract, capability gate,
   host implementation, example and regression coverage together.
2. Keep `HIT.Sdk` independent of the executable. Extensions use SDK contracts, their own service
   scope and isolated `Api.Storage`; never expose the host root provider or require a host
   project reference. Use `HIT.Example` as the independent compilation reference.
3. Preserve existing public interfaces and contract-1 loading. Extend optional/versioned surfaces
   such as `IHitApiV2` where appropriate, with explicit absence/unsupported handling. Do not
   make an in-process C# feature look like a network/HTTP API.
4. Gate each operation/subscription by its documented capability and validate package/menu/action
   identities. Preserve namespacing, quotas and duplicate rejection. Do not broaden permissions
   because a related read capability already exists.
5. For events, preserve immutable snapshots, bounded latest-only delivery and off-renderer
   callbacks. Link subscriptions to the appropriate visible/module lifetime; marshal UI updates
   through `InvokeAsync`. Disabled packages must pause delivery and hide their actions while
   retaining registrations needed for re-enable. Module work and view work have different lifetimes.
6. For shared media/spectrum, reuse host state and explicit consumer leases. Do not add per-package
   media pollers/capture sessions or let one disposer stop another owner. Keep sampling asleep
   when no eligible consumer needs it, according to the current API contract.
7. For archives, assets and storage, enforce documented limits, traversal/link/collision checks,
   capability/contract rejection and package isolation. Preserve atomic/idempotent installation,
   rollback, retained user state and loaded-code restart semantics. Treat installed C# as trusted
   in-process code; capability checks are not an operating-system security sandbox.
8. For actions, preserve namespaced discovery, disable/re-enable behavior and execution errors.
   Disposing a registration does not forcibly interrupt an already running trusted callback.

## Verify compatibility and usable examples

Follow [Testing and release](../../../Docs/TESTING-RELEASE.md) to pack the example fixture before
running the full `HIT.Example.Tests` harness. Compile the affected SDK-only example, test new
capability denial as well as success, and retain old-interface/package coverage. Use the focused
real-device spectrum flag only when relevant; `extensions.mjs` consumes the harness preview.

For lifecycle changes, check hidden/visible, disable/re-enable, disposal and pending callbacks.
For packages, check rejected inputs, update/rollback and preserved state. Document signatures,
capability names, lifetime, limits and an independently compilable example before delivery.

## Current contract checkpoints

For GitHub updates, preserve optional manifest updates metadata, legacy Chess discovery, stable numeric asset selection, bounded downloads and exact package identity/permission checks before installation. Use the documented extension updater fixture; do not claim publication from a local package build.

Use SDK 1.2.0 IHitMenuPages only as an optional presentation contract, with stable IDs and no game/timer/reset side effects during restore. Keep older packages compatible. Confirm downgrades through the host dialog, and allow removed-package cleanup only for unloaded code while retaining saved data and locked/linked trees.
