# CLAUDE.md

Context for AI assistants working on this repo. The roadmap and open decisions are in [PLAN.md](PLAN.md).

## Project

**TownGallery** (working name) is a Unity AR app. Users place photos of their hand-drawn art at real-world locations, and other users can find the art in AR. It started as a capstone project and is being developed into a consumer app.

- Unity **6000.4.2f1** (Unity 6), AR Foundation 6 with ARKit and ARCore
- **ARCore Extensions** 1.56.0 for Cloud Anchors, installed from git in `Packages/manifest.json`
- **Supabase** for the backend: Postgres with PostGIS, and Storage for images
- Main target is **iOS** (bundle ID `com.andypestotnik.towngallery`). Android is set up but not configured.
- Active input handling is set to **Both**. Current scripts use the legacy `Input` API (`Input.touchCount` and similar).

## Layout

| Path | Contents |
|------|----------|
| `Assets/Scenes/AR_Scene.unity` | The main scene; everything real happens here. `SampleScene` and `CameraScene` are leftover prototypes. |
| `Assets/Scripts/Add_Image.cs` | Image-placement flow: pick a photo, position it in front of the camera, pinch to scale, then OK or Cancel. Calls `CloudAnchorPlacementManager.SavePlacement` when the user taps OK. |
| `Assets/Scripts/ImageQuadUtil.cs` | Shared material and texture setup for the `double_image_quad` prefab. Use it so saved and loaded images look the same. |
| `Assets/Scripts/Persistence/` | Saving and loading: `CloudAnchorPlacementManager` (host/resolve anchors, overall save/load flow), `SupabasePlacementApi` (REST calls), `LocationProvider` (GPS and compass), `PlacementRecord` (row DTO). |
| `Assets/Editor/PlacementSystemSetup.cs` | Menu item **Tools → Capstone AR → Set Up Placement System** that connects the persistence components in the open scene. |
| `Assets/Prefabs/double_image_quad.prefab` | The prefab for a placed artwork. |
| `Backend/SupabaseConfig.example.json` | Template for the gitignored `Assets/Resources/SupabaseConfig.json`. |
| `Backend/supabase_schema.sql` | `placements` table, the `nearby_placements(lat, lon, radius_m)` function, and the storage bucket. Run it in the Supabase SQL editor. |
| `Assets/Plugins/NativeGallery`, `NativeFilePicker` | Third-party plugins for picking photos. Don't edit them. |
| `Capsotone_Next_Steps.txt` | Detailed Google Cloud, Unity, and Supabase setup steps for the Cloud Anchor system. |

Unused or prototype code: `Add_Image_copy.cs` (entirely commented out), `GPS_Stuff.cs` (replaced by `LocationProvider`).

## How saving and loading work

**Save:** the image is parented to an `ARAnchor`, and its pose relative to that anchor is recorded. The app waits for ARCore's feature-map quality to be good enough, with a 30 s timeout, then hosts a Cloud Anchor. It reads GPS and compass, uploads a PNG to Supabase Storage, and inserts a `placements` row.

**Load (on startup):** the app waits for tracking and GPS, then calls `nearby_placements` to get unexpired rows within 50 m. For each row it downloads the image and resolves the anchor. An image only appears once the camera sees the area where it was saved.

## Conventions

- **Column names in `PlacementRecord.cs` must match `supabase_schema.sql` exactly.** It is serialized with `JsonUtility`. Change both together.
- Components are wired through public fields in the Inspector, not `FindObjectOfType` at runtime.
- Async work uses coroutines with `onSuccess`/`onError` callbacks. Follow that pattern instead of adding async/await.
- Indent with tabs and match the style of the file you're editing.
- Don't edit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`, or the generated `.csproj`/`.slnx` files.
- When changing a scene or prefab, prefer describing Inspector steps or writing an Editor menu script (like `PlacementSystemSetup.cs`) over hand-editing large YAML files.

## Secrets and security

- **Never commit keys.** They live in gitignored local files:
  - Supabase URL and anon key: `Assets/Resources/SupabaseConfig.json`, loaded at runtime by `SupabasePlacementApi.Awake`. To create it, copy `Backend/SupabaseConfig.example.json`. Leave the Inspector fields in `AR_Scene` blank.
  - Google ARCore API key: `ProjectSettings/ARCoreExtensionsProjectSettings.json`, edited through **Project Settings → XR Plug-in Management → ARCore Extensions**. It's untracked, so a fresh clone must re-enter the key there.
  - Never put the Supabase `service_role` key in the client.
- Keys from before this setup are still in the git history of the initial commit, so treat those keys as exposed.
- Never read, print, or commit `Supabase PErsonal Org PAssword.txt`. It is gitignored.
- The current Supabase access rules are **development only**: anyone can read, insert, and upload. Tighten them when auth is added (see PLAN.md §6).

## Building and testing

- AR features only work on a physical device. The Editor can compile but can't test anchoring.
- iOS: build from Unity, then open the generated **`.xcworkspace`**, not the `.xcodeproj`, because ARCore installs through CocoaPods. **Location Usage Description** must be set in Player Settings.
- Cloud anchors expire after **1 day** because of API-key auth (`anchorTtlDays = 1`). Longer lifetimes need token auth; see PLAN.md.
- Basic save/load test: place an image, delete and reinstall the app, relaunch in the same spot, and confirm the image comes back.
- No automated tests exist yet.
