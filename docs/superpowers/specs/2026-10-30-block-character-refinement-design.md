# Block Character Refinement

Date: 2026-10-30
Status: Design approved; written specification awaiting user review.

## Goal and approved direction

Replace the placeholder player and NPC visuals with friendly block-style people
whose clothing reflects the game's Muslim cultural setting. Apply this to the
playable character, village traders, and masjid teachers. Religion is not expressed
as a fixed facial appearance or skin tone.

The user approved the block-style concept lineup: kufi and thobe, hijab and long
outfit, everyday tunic and trousers, and a teacher wardrobe variation. The local
concept page is illustrative, not implemented game art.

## Visual requirements

- Square heads, separate arms and legs, shoes, simple eyes and a friendly mouth.
  Child-friendly proportions without caricatures.
- Preserve the existing five supported skin tones.
- Headwear choices: none, kufi, hijab. Hijabs frame the visible face and extend
  over the shoulders; kufis sit visibly on the head.
- Outfit choices retain the existing Casual, Traditional, and Winter enum
  identities. Present them as tunic/trousers, long outfit, and winter outfit.
  Traditional supports the approved thobe/long modest silhouette.
- Headwear and outfit choices are independent; do not infer gender, nationality,
  religious commitment, or NPC occupation from combinations.
- Independent clothing and headwear colors, using a small readable palette.
  Subtle collars, hems, and kufi trim give detail without heavy textures.
- New unsaved players start with a kufi and a sand-colored long outfit. Existing
  saved player choices must not be overwritten by this default.
- NPCs receive a deterministic mix of supported skin tones, outfits, colors,
  and headwear. Do not assign a single outfit or skin tone to a country.
  Preserve trader profiles, teacher names, labels, and interaction ownership.

## Shared character presentation

Introduce one shared block-character visual component in the existing Player
assembly, which the Villager assembly already references. It owns visual parts,
appearance updates, and lightweight limb motion; it does not own locomotion,
physics, inventory, trading, or reading.

Reuse shared meshes/materials where practical. Do not allocate materials or rebuild
visuals each frame. Build each pooled NPC visual once, then reuse it through
chunk unloads and world resets. Release owned rendering resources appropriately.
Keep characters recognizable in shadow without changing global world lighting.

Replace the player's runtime capsule/sphere visual creation, not the existing
CharacterController. Integrate the same presentation into trader and teacher
creation rather than layering it over their old bodies.

Keep the current collision envelope and camera target. Visuals, including clothing
and headwear, must remain usable in existing one-block-wide doorways. Decorative
body parts do not add independent physics colliders.

Animate small arm/leg swings during player movement; idle and stationary NPCs
remain still. Jumping, flight, riding, camera collision, and movement retain their
current behavior. A skeletal rig, animation assets, and NPC pathfinding are out
of scope.

## Appearance panel

Reuse and complete the existing CharacterCustomizerPanel rather than introducing
a competing customization system. Scene assembly must persist a fully wired panel
and a visible Appearance button. C also opens the panel when no other modal owns
gameplay input; it does not open over reading, trading, or another input-blocking
menu.

The panel provides:

- Five skin-tone choices.
- None, kufi, and hijab headwear choices.
- Tunic/trousers, long outfit, and winter outfit choices.
- Separate clothing and headwear color swatches.
- A visible live character preview.
- Save and Cancel controls with readable labels and selected-state indicators.

Opening snapshots the current appearance and acquires gameplay input ownership.
Changes preview immediately but are not persisted until Save. Cancel, Escape, or
an external dismissal restores the snapshot. Closing restores only the input
state this panel owns and respects the existing closing-frame interaction guard.
Repeated opening must not accumulate button callbacks or leak preview resources.
Use the existing LegacyRuntime font and Unity UI conventions.

## Appearance data and persistence

Extend CharacterAppearance with independent headwear color while preserving
existing enum numeric values and saved appearance keys. Update cloning,
CharacterDataBridge, customization application, and serialization together.
Use CharacterData.headCoveringColor for the corresponding profile value.

Legacy local appearance JSON without headwear color derives that color from its
previous clothing color, preserving the old linked-color behavior. Existing child
profile head-covering colors should now be honored rather than discarded.
Do not silently rewrite unsupported saved values as a successful save: validate
new panel data and surface persistence failures using existing notification/log
conventions.

Save persists locally for standalone play. When a child profile is active, update
and persist that profile through the existing backend API rather than only
mutating SessionManager memory. No auth/backend replacement or new dependency is
part of this feature. Report failure visibly and retain the user's working choices
for retry; do not indicate successful remote persistence when it failed.

## Compatibility boundaries

Preserve:

- Mining, placement, inventory, flight, riding, and recovery.
- Existing house and masjid geometry, protection, and doorway clearances.
- Trading offers and atomicity, reading content, and modal ownership.
- Chunk and population pooling, unload activation, and world reset semantics.
- Existing player appearance and active-child restoration paths.

No new prayer/lesson activity, worship mechanics, combat, multiplayer, purchase,
facial customization suite, character selection by gender, imported art package,
or paid dependency.

## Validation and acceptance

Write failing tests before implementation, then run focused existing and new tests:

1. Visual parts, headwear silhouettes, independent color application, and no
   added character-part colliders.
2. All supported appearance combinations serialize and bridge correctly; legacy
   local JSON and existing child profiles retain their intended appearance.
3. Scene has the new visuals and fully wired appearance controls and fonts.
4. Panel controls change the visible preview; Save survives recreation, Cancel
   and Escape restore the original; other modals cannot be stacked.
5. Active-child saves use the backend, including explicit failure feedback.
6. Villagers/teachers use shared visuals, retain their interactions, and reuse
   the same pooled objects across unload/reset.
7. Actual player traversal of existing house and masjid doors still passes;
   flight, movement, camera, trade, reading, and protection regressions pass.
8. A fresh WebGL build loads and displays the redesigned characters. Inspect
   the appearance panel in the browser where pointer-lock constraints permit.
   Record any browser-automation limitations rather than claiming untested input.
9. Measure actual Unity frames for at least 60 seconds in the populated district
   at a recorded resolution/device; target at least 30 FPS. Do not use browser
   animation callbacks as a proxy for game FPS.

## Delivery

Update the relevant phase status with test counts, build result, browser evidence,
and measured performance. Preserve unrelated workspace changes. A merge or push
requires explicit user authorization; prior masjid merge approval does not grant
blanket permission for this feature.
