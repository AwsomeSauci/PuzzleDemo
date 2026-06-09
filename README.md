# PuzzleDemo: Puzzle Flow UI MVP

Unity test assignment MVP for a puzzle-game UI flow: gallery, puzzle preview, start scenarios, continuation, and reusable popup/dialog routing.

The project is intentionally shaped as a first production-minded iteration, not as a full production framework. The goal is to show a clean direction, extension points, and enough working UI to discuss engineering tradeoffs in an interview.

## Quick Start

1. Clone `https://github.com/AwsomeSauci/PuzzleDemo`.
2. Open the repository root in Unity `6000.4.9f1`.
3. Open `Assets/Scenes/Main.unity`.
4. Enter Play Mode.
5. Click a puzzle tile in the gallery.
6. In the preview screen, select a cut and press the start button.

The default demo configuration is currently set to show failure flows for paid starts:

- Coin start fails with `NotEnoughCoins`.
- Rewarded-ad start fails with `WeakInternet`.
- Free start still succeeds.
- Continue is available for the seeded progress entry `sunset-bay-1 / 36`.

## What This MVP Demonstrates

- Reusable UI navigation through typed routes and reusable fragments.
- Universal popup flow with queued display.
- Puzzle gallery with virtualized grid cells and async preview loading.
- Puzzle preview dialog with image, cut selection, dynamic start button state, and continue button.
- Application-layer start flow for `Free`, `Coins`, and `RewardedAd`.
- Demo infrastructure that can simulate success and failure without real SDKs.
- Zenject composition split into service and fragment installers.
- Fail-fast validation for fragment registries, popup definitions, and popup catalogs.
- EditMode tests covering start flow, demo services, gallery preview loading, presenter mapping, and fragment-router edge cases.

## Main Entry Points

| Area | Path |
| --- | --- |
| Main scene | `Assets/Scenes/Main.unity` |
| Service installer asset | `Assets/PuzzleFlowDemo/PuzzleFlowServicesInstaller.asset` |
| Fragment installer asset | `Assets/PuzzleFlowDemo/PuzzleFlowFragmentsInstaller.asset` |
| Puzzle catalog | `Assets/PuzzleFlowDemo/PuzzleCatalog.asset` |
| Fragment registry | `Assets/PuzzleFlowDemo/FragmentRegistry.asset` |
| Popup catalog | `Assets/PuzzleFlowDemo/Popups/PuzzlePopupCatalog.asset` |
| Gallery prefab | `Assets/Prefabs/Fragments/Gallery/GalleryFragment.prefab` |
| Preview prefab | `Assets/Prefabs/Fragments/PuzzlePreview/PuzzlePreviewFragment.prefab` |
| Universal popup fragment | `Assets/Prefabs/Fragments/Common/UniversalPopupFragment.prefab` |
| Universal popup view | `Assets/Prefabs/Popups/UniversalPopupView.prefab` |

## Architecture

The code is split by responsibility rather than by Unity object type.

```text
Domain
  Pure puzzle data models and IDs.

Application
  Use cases and contracts: catalog queries, progress, start commands,
  continue checks, purchase/ad abstractions.

Presentation
  UI presenters, view models, fragments, router contracts, popup service,
  gallery and preview screens.

Runtime
  Zenject composition, fragment factory, bootstrapper, scene wiring.

Infrastructure
  ScriptableObject-backed catalog and demo services.

Media
  Addressables media loading and cache boundary.
```

### Why This Shape

- `Application` does not depend on Unity UI.
- UI code talks to use cases through interfaces.
- SDK-like systems are isolated behind `IPurchaseService` and `IAdService`.
- Dialog rendering is centralized in `UniversalPopupService`.
- Scene setup is data-driven through ScriptableObject installers and registries.
- Public interfaces are kept in dedicated files to make contracts easy to find and review.

## Start Flow

High-level start path:

```text
PuzzlePreviewPresenter
  -> IPuzzleStartCommandService.StartNewAsync(puzzleId, pieceCount)
    -> IPuzzleCatalogQueryService.GetById
    -> IPuzzleStartService.StartNewAsync
      -> IStartOptionResolver.Resolve
      -> IPuzzleStartModeHandler.StartAsync
        -> Free / Coins / RewardedAd handler
```

Important files:

| Responsibility | Path |
| --- | --- |
| Start command facade | `Assets/Scripts/Application/Start/PuzzleStartCommandService.cs` |
| Start use case | `Assets/Scripts/Application/Start/PuzzleStartService.cs` |
| Start mode handlers | `Assets/Scripts/Application/Start/PuzzleStartModeHandlers.cs` |
| Start state query | `Assets/Scripts/Application/Start/PuzzleStartStateService.cs` |
| Preview presenter | `Assets/Scripts/Presentation/PuzzlePreview/PuzzlePreviewPresenter.cs` |
| Preview view | `Assets/Scripts/Presentation/PuzzlePreview/PuzzlePreviewFragmentView.cs` |

## Demo Scenario Controls

Open:

```text
Assets/PuzzleFlowDemo/PuzzleFlowServicesInstaller.asset
```

Fields:

| Field | Meaning |
| --- | --- |
| `Demo Starting Balance` | Wallet balance shown in UI and used by demo purchase service. |
| `Demo Purchase Mode` | Controls coin-purchase behavior. |
| `Demo Rewarded Ad Mode` | Controls rewarded-ad availability and result. |

Current default values:

| Field | Value | Result |
| --- | --- | --- |
| `Demo Starting Balance` | `0` | UI shows zero coins. |
| `Demo Purchase Mode` | `AlwaysInsufficientFunds` | Coin start shows not-enough-coins popup. |
| `Demo Rewarded Ad Mode` | `WeakInternet` | Ad start shows weak-internet popup. |

### Coin Modes

| Mode | Behavior |
| --- | --- |
| `SucceedWhenAffordable` | Deducts coins and starts if balance is enough. |
| `AlwaysInsufficientFunds` | Always returns `InsufficientFunds`. |
| `Unavailable` | Returns purchase-unavailable failure. |
| `Cancelled` | Returns purchase-cancelled failure. |

### Rewarded Ad Modes

| Mode | Behavior |
| --- | --- |
| `Completed` | Ad is available and completes successfully. |
| `Unavailable` | Ad is not ready. |
| `WeakInternet` | Ad is unavailable due to weak connection. |
| `Skipped` | Ad is available, but show result is skipped. |
| `Failed` | Ad is available, but show result fails. |

## Popup Flow

The popup system is intentionally simple:

```text
IUniversalPopupService.ShowAsync
  -> queues request
  -> opens UniversalPopupFragment through IFragmentRouter
  -> instantiates UniversalPopupView
  -> resolves action buttons
  -> returns PopupResult
```

Key files:

| Responsibility | Path |
| --- | --- |
| Popup service | `Assets/Scripts/Presentation/Popups/UniversalPopupService.cs` |
| Popup fragment | `Assets/Scripts/Presentation/Popups/UniversalPopupFragment.cs` |
| Popup view | `Assets/Scripts/Presentation/Popups/UniversalPopupView.cs` |
| Popup definitions | `Assets/Scripts/Presentation/Popups/PopupDefinition.cs` |
| Puzzle popup catalog | `Assets/Scripts/Presentation/PuzzlePreview/PuzzlePopupCatalog.cs` |

Popup body formatting is fail-fast. Invalid format strings now throw configuration errors instead of silently falling back.

### Popup Localization Plan

The production localization model should keep popup identity and rendering separate from localized copy:

- `PopupDefinition` owns a stable popup GUID/key and the prefab used to spawn the popup.
- The popup asset does not become the long-term source of localized title/body/button text.
- Unity Localization tables map popup GUIDs to localized strings, for example `popupGuid.title`, `popupGuid.body`, and `popupGuid.action.ok`.
- Runtime popup creation resolves localized text by popup GUID and action GUID, then fills `NotificationMessage` before the popup is shown.
- The current serialized `title`, `body`, and `actionLabel` fields remain useful as demo data, editor fallback, and validation-friendly defaults.

This keeps popup assets stable for routing, analytics, and prefab selection while allowing copy changes and translations to happen through Unity Localization without touching prefab or routing configuration.

## Fragment Routing

Fragments are registered in:

```text
Assets/PuzzleFlowDemo/FragmentRegistry.asset
```

Registered MVP routes:

| Route | Layer | Purpose |
| --- | --- | --- |
| `fragment.gallery` | Screen | Main gallery screen. |
| `fragment.puzzle-preview` | Overlay | Puzzle start dialog. |
| `fragment.universal-popup` | Overlay | Reusable popup host. |

The router reuses fragment instances and hides them on close. This is enough for the assignment scope and keeps UI creation cost predictable.

## Media Loading

Puzzle previews are loaded through Addressables:

```text
PuzzleMediaQueryService
  -> IPuzzleMediaCatalog
  -> MediaSpriteService
  -> CachedMediaService
  -> AddressablesMediaSource
```

Addressable group:

```text
Assets/AddressableAssetsData/AssetGroups/PuzzleFlow Local Media.asset
```

Catalog media keys use the `puzzle-preview/<puzzle-id>` pattern.

## Tests

EditMode tests live in:

```text
Assets/Tests/EditMode
```

Covered areas:

- Free start success.
- Coin start success/failure mapping.
- Rewarded-ad start success/failure mapping.
- Cancellation before progress mutation.
- Missing/duplicate start handler configuration errors.
- Demo purchase service modes.
- Demo rewarded-ad service modes.
- `PuzzleId` argument validation.
- In-memory progress repository empty/null seed behavior.
- Gallery placeholder rendering and lazy preview loading.
- Fragment router cancellation and broken-open behavior.
- Fragment close lifecycle exceptions do not leave route awaiters hanging.
- Start button presentation mapping.

## Repository Notes

Generated local files are intentionally not committed:

- Unity `Library`, `Temp`, `Obj`, `Logs`, and `UserSettings`.
- IDE/project files such as `.idea`, `.vs`, `.sln`, and `.csproj`.
- Build output and generated Addressables player content.

Unity `.meta` files under `Assets` and embedded packages are part of the project state and should stay committed.

## Verification

Fast C# compile check after Unity/Rider has generated a solution file:

```powershell
dotnet build .\<generated-solution>.sln --no-restore -v:minimal
```

Known warning:

```text
MSB3277 System.Net.Http version conflict
```

This comes from Unity/Addressables generated references and does not block the current C# build.

Unity EditMode tests:

```powershell
$projectPath = (Get-Location).Path

& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath $projectPath `
  -runTests -testPlatform EditMode `
  -testResults "$projectPath\Temp\EditModeResults.xml" `
  -logFile "$projectPath\Temp\EditMode.log"
```

Unity batchmode cannot open the same project while it is already open in another Unity instance. If the project is open, close the Editor first or run the tests from the Editor Test Runner.

If the process returns a non-zero exit code, inspect `Temp/EditModeResults.xml` and `Temp/EditMode.log`.

## MVP Boundaries

This is not pretending to be the final production solution.

Deliberately simplified:

- No real purchase SDK.
- No real ad SDK.
- No backend catalog or persistence.
- No save-game serialization beyond seeded in-memory progress.
- No full animation/state-machine framework for fragment transitions.
- No complex cache eviction strategy.
- No localization package integration yet; the planned GUID-to-Unity-Localization flow is documented above.

Production follow-up would likely add:

- Real wallet and transaction boundary.
- Real ad/purchase adapters.
- Persistent progress repository.
- User-facing Addressables error UI and richer retry/backoff policy.
- Popup localization provider and analytics hooks.
- Richer route/registry validation UI in Editor.
- PlayMode smoke tests for scene composition.
- Performance budget for large catalogs.

## Review Notes

This MVP is optimized for interview review:

- The code is intentionally readable and direct.
- Extension points are explicit.
- Configuration errors fail fast where silent fallback would hide broken wiring.
- Demo modes make success and failure flows reproducible without external services.
- The current default scenario is set to demonstrate failure popups for coin and rewarded-ad starts.
