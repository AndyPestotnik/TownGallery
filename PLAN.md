# TownGallery Project Plan

*Working name. Last updated 2026-09-30.*

## Vision

TownGallery began as a capstone project and is being developed into a consumer app. Users photograph artwork they drew or painted by hand and place it at real-world locations in AR, where other users can find it. Tapping a piece opens the artist's profile, which has a short bio, a gallery of their placed work, and links to their website and social media accounts.

## Current state (2026-09-30)

- [x] Users can pick an image from the phone's photo gallery and place it in AR.
- [x] Anchoring works well.
- [x] A first version of the database is built (Supabase, plus ARCore Cloud Anchors). It is for testing only; see [Security](#security).
- [ ] No user accounts or artist profiles yet.
- [ ] Cloud anchors expire after 1 day, because API-key authentication limits them to 24 hours.

## Phases

Each task below is tagged with the phase it belongs to:

- **P1 (first public release):** the minimum needed to launch and pass App Store review.
- **P2:** planned soon after launch.
- **P3:** later, nice to have.

Items marked ❓ depend on a decision listed under [Open questions](#open-questions).

---

## 1. Accounts and artist profiles

- [ ] **P1** User accounts (Supabase Auth)
  - [ ] Sign-up and login screens
  - [ ] Sign in with Apple. Apple requires it if any other third-party sign-in is offered.
  - [ ] Password reset
  - [ ] Account deletion inside the app, which also deletes the user's placements and images. Apple requires this.
- [ ] **P1** Database tables for artist profiles
  - [ ] `profiles` table linked to the signed-in user
  - [ ] `placements.artist_id` to record who placed each piece
- [ ] **P1** Creating and editing a profile
  - [ ] Display name, short bio, profile picture
  - [ ] Links to the artist's website and social media accounts
  - [ ] Gallery of the artist's placed work, showing active and expired pieces
- [ ] **P1** Tap a piece in AR to open its artist's profile. This needs a raycast against the placed images.
- [ ] **P1** ❓ Decide whether people can view art without an account (see open questions).
- [ ] **P3** Social features such as likes, following artists, and a "new art from artists you follow" feed.

## 2. Discovering art

Right now a piece only appears if a user points the camera at the exact spot where it was saved. The app needs a way to lead users to art.

- [ ] **P1** Map or "art nearby" list, using the existing `nearby_placements` query
- [ ] **P1** In-app guidance once a user is near a piece, such as a direction and distance, or a "look around here" hint
- [ ] **P2** Optional notification when the user walks near art
- [ ] **P2** ❓ Show approximate rather than exact locations on the map (see [Privacy](#privacy))

## 3. Editing and placing artwork

### Editing
- [ ] **P2** Background removal for drawings
  - Start with contrast-based thresholding, which is fast, runs on the device, and suits ink or pencil on paper.
  - If that isn't good enough, use a segmentation model: on the device (Unity Inference Engine/Sentis) or on a server (e.g. rembg).
- [ ] **P2** Simple animation editor where the artist chooses whether and how the piece moves
  - [ ] Rotation (`OscillatingRotation.cs` is a starting point)
  - [ ] Translation
  - [ ] Scaling
  - [ ] Following a simple hand-drawn path
  - [ ] Loop or ping-pong playback
  - [ ] Save animation settings in the placement record so they play back for other viewers
- [ ] **P2** Save edited drafts until they are ready to place

### Placing
- [ ] **P1** Better placement guidance
  - Explain that the user must walk slowly around the piece before it can be saved.
  - Show progress using ARCore's feature-map quality estimate, e.g. "Scanning… good… ready", and reassure the user while it runs.
  - Explain failures, e.g. "Not enough detail here; try a textured surface" or "Too dark".
- [ ] **P1** Let the artist choose how long a piece stays up, from 1 day to 1 year. This depends on [longer-lasting anchors](#6-backend-and-infrastructure).
- [ ] **P2** Limits on how many pieces fit in one area and how many one user can post, so popular spots don't get cluttered

## 4. Keeping content safe

### Automated checks
- [ ] **P1** Screen uploads for hateful or explicit content, keeping it PG-13 (e.g. Google Cloud Vision SafeSearch, AWS Rekognition, or Hive)
- [ ] **P1** Allow each image to be posted at most 3 times, detected with a perceptual hash compared against the user's earlier uploads
  - ❓ Decide whether an edited or animated version counts as the same image.
- [ ] **P2** Check that artwork is hand-drawn or painted rather than a photograph
  - ⚠️ No off-the-shelf tool does this reliably. Treat it as a written content rule enforced through user reports, or at most a soft warning. A hard automatic block will reject real artists' work.

### Reporting and review (required by App Store guideline 1.2)
- [ ] **P1** Report button on every piece and every profile
- [ ] **P1** Block a user, which hides all of their content for the person blocking them
- [ ] **P1** Moderation queue where you can review reports and take content down. A simple admin page or the Supabase dashboard is enough to start.
- [ ] **P1** Contact address for abuse reports, shown in the app and on the App Store listing

## 5. Legal and privacy

- [ ] **P1** Privacy policy covering location, camera, photos, and account data
- [ ] **P1** Terms of service and content rules, including that only original hand-made art is allowed
- [ ] **P1** Minimum age (probably 13+) and the matching App Store age rating
- [ ] **P1** Process for handling copyright (DMCA) takedown requests
- [ ] **P1** Clear explanations in the camera, location, and photo-library permission prompts

### Privacy
- [ ] **P1** ❓ A placement records where the artist was standing. Decide whether other users see the exact location or an approximate one.
- [ ] **P2** ❓ Consider off-limits areas, such as schools and private homes.

## 6. Backend and infrastructure

- [ ] **P1** Longer-lasting cloud anchors, up to 365 days
  - API-key authentication limits anchors to 1 day. Longer lifetimes need token (JWT) authentication, which on iOS means a small backend that issues tokens (e.g. a Supabase Edge Function).
- [ ] **P1** Lock down Supabase access rules. The current rules let anyone read, insert, and upload.
  - [ ] Only signed-in users can insert rows or upload images, and only as themselves.
  - [ ] Only the owner can update or delete a placement.
- [ ] **P1** Scheduled cleanup that deletes expired placements and their images from storage
- [ ] **P1** Image size limit and compression before upload, plus thumbnails for the map and gallery
- [ ] **P1** ❓ Production hosting: a paid Supabase plan with backups, or another provider?
- [ ] **P2** ⚠️ Anchors can fail to load when a place looks different, e.g. day vs. night, seasons, or construction. Consider the ARCore Geospatial API for long-lived pieces, either as the main approach or as a fallback when an anchor won't load.

## 7. Polish and quality

- [ ] **P1** Branding: final name, icon, and a consistent UI style
- [ ] **P1** Lighting: stop pieces from looking randomly shadowed
  - Quick fix: `ImageQuadUtil.cs` currently uses the lit `Legacy Shaders/Diffuse` shader. Switching to an unlit shader makes artwork always show at full brightness.
  - **P3** Later: match real lighting using ARKit/ARCore light estimation, with a small minimum brightness.
- [ ] **P1** Tutorial on first launch
- [ ] **P1** Clear error states for no GPS, no network, an anchor that won't load, and a failed upload
- [ ] **P2** Profile and improve performance: load time, memory with many images, battery use
- [ ] **P2** Clean up leftover prototype code: `Add_Image_copy.cs`, `GPS_Stuff.cs`, `SampleScene`, `CameraScene`

## 8. Testing and release

- [ ] **P1** Crash reporting and basic analytics
- [ ] **P1** Real-world test plan covering different places, lighting, times of day, and reloading anchors after a reinstall
- [ ] **P1** TestFlight beta with a small group of artists
- [ ] **P1** App Store listing: screenshots, description, age rating, privacy details

---

## Open questions

| # | Question | Notes |
|---|----------|-------|
| 1 | iOS only, or iOS and Android? | The project has ARKit and ARCore set up, but only iOS has a bundle ID. |
| 2 | Can people view art without an account? | Browsing without an account helps growth, but it makes blocking and personalization harder. |
| 3 | Exact or approximate locations on the map? | Privacy for artists versus how easy art is to find. |
| 4 | Does an edited version of an image count toward the 3-post limit? | Affects how duplicates are detected. |
| 5 | Production database hosting? | Paid Supabase plan with backups, or something else. |
| 6 | Cloud Anchors, Geospatial API, or both for long-lived pieces? | See the risk noted in section 6. |
| 7 | Final app name? | "TownGallery" is a working name. |
| 8 | Business model (free, premium features, commissions)? | Affects scope decisions in P2 and P3. |

## Known risks

- **Anchor reliability over time:** a place can look very different weeks or months after a piece was saved.
- **Moderation workload:** reports have to be answered quickly once the app is public.
- **Hand-drawn detection:** there is no reliable automated check (see section 4).
- **Storage and bandwidth costs** will grow with the number of images.
