# Masjid Discovery District

Date: 2026-10-27
Status: Design approved in conversation; written specification awaiting review.

## Goal and approved scope

Create a nearby discovery district containing four original, enterable masjid
complexes inspired by selected architectural traditions from Saudi Arabia,
Iraq, Pakistan, and Turkey. Connect the district to the starting village with
paths and bring the four entrances together around a shared garden.

The user selected exploration first, a discovery district rather than scattered
villages, and protected landmarks rather than editable buildings.

This release includes furnished interiors, friendly teachers, and readable
information boards. Joining lectures, taking classes, and participating in
Salah remain future work, not simulated or advertised as working activities.

## Architectural direction

These are stylized original buildings, not replicas of specific sacred sites.
Each country contains many traditions; labels must describe the particular
inspiration rather than imply one national style.

| Complex | Visual direction |
|---|---|
| Saudi-inspired | Warm earth/stone palette, shaded arcades, courtyard, restrained geometric decoration, with selected Najdi influences |
| Iraqi-inspired | Warm brickwork, rhythmic arches, patterned masonry, blue tile accents used selectively rather than presented as universal |
| Pakistani-inspired | Mughal-inspired red and pale stone contrast, patterned entrance frames, domes, welcoming courtyard |
| Turkish-inspired | Ottoman-inspired central and subsidiary domes, slender minarets, cool tile accents, bright central prayer space |

Use the existing block aesthetic. Distinguish silhouettes and spatial layouts,
not just material colors. All four complexes need recognizable differences
when viewed from the shared garden.

## Layout and exploration

- One district per world, deterministically placed near the starting area.
  Its boundary must be within 128 horizontal blocks of the initial spawn,
  reachable by a signed walking route rather than requiring flight.
- Four separate complexes surround a shared garden and decorative fountain.
  Paths join all entrances and connect back toward the starting village.
- Buildings may span chunks. Do not squeeze a complex into the unused strip
  beside a house or increase the existing 16 x 16 x 256 chunk dimensions.
- Each complex includes a prayer hall with aligned rugs and a mihrab niche,
  a learning room with bookshelves and seating, an entrance area, and a
  decorative washing area. Washing and seating are not functional activities.
- Main entrances have at least three blocks of clear width and four blocks of
  clear height. Interior walking routes have at least two blocks of width.
  Each prayer hall has at least a 7 x 9 block unobstructed usable floor area,
  excluding walls and furniture.
- Level foundations, cleared interiors, and safe approaches are required.
  No unsupported floors, buried doors, impassable terrain, or furniture
  blocking the circulation route.
- Interiors remain readable beneath roofs through bounded, pooled lighting.
  Test camera clearance around doors, walls, domes, and ceilings.
- Prayer rows and mihrabs share a consistent in-world orientation. Do not
  claim this fictional direction is a location-calculated real-world qibla.
- Minarets are exterior architectural features in this release; their interiors
  and stairs are not required.

## Teachers and information

Each complex has one named, friendly teacher and accessible information boards.
Reuse existing interaction reach, wall occlusion, and player-relative targeting.
Aim and press E to open a reading panel; Escape or Close returns to exploration.

Content covers:

1. The selected architectural inspiration and its diversity.
2. The prayer hall, mihrab, and shared community spaces.
3. Welcoming masjid etiquette and an introduction to community learning.

Teachers offer short introductions, not functioning lectures or classes.
Boards must not show nonfunctional Join buttons. Use age-appropriate English
initially, without fabricated Arabic calligraphy or invented religious quotes.
Source-check factual text and keep architectural explanations separate from
religious instruction. No new recitations or instructional prayer sequences.

Reading unlocks the cursor and blocks movement, flight, mining, placement,
and hotbar input. Closing restores the prior input state and preserves the
existing closing-frame guard. Reading panels and the trading panel must not
stack or release each other's input ownership.

## Landmark protection and world lifecycle

- Protected complex footprints include their courtyards, structural foundations,
  interiors, and roof/minaret height. Reject mining and placement within these
  volumes, including placing blocks into their air spaces.
- Display a short, readable protected-landmark message on an attempted edit.
  Do not consume items, award mined items, or silently accept the action.
- Shared outdoor paths and the garden are not automatically protected by a
  single enormous district bounding box; protection follows complex volumes.
- Keep ordinary terrain, village-house editing, animals, trading, and flight
  unchanged outside protected areas.
- Protection must be enforced at the authoritative gameplay block-edit path,
  not solely through collider identity or disabled furniture colliders.
- Placement must not overwrite existing saved player edits or overlap existing
  village houses. Resolve saved edits and reserved footprints before generating
  structures. A conflicting candidate location must be rejected.
- If no safe location satisfies the nearby-district requirement, report a
  clear placement error; never overwrite player work or silently omit a complex.
- Reentering an area or reopening a world restores the same layout without
  duplicate teachers, furniture, or information boards.
- Seed changes and world switching invalidate old district assignments and
  protection state; pooled objects cannot remain attached to replaced chunks.

## Implementation boundaries

Use a small district layout/data layer for deterministic footprints, regional
style assignments, protection volumes, and interaction content identifiers.

Generate structural voxels through the world-generation pipeline, using
world-coordinate placement and consistent cross-chunk sampling. Existing greedy
chunk meshing and collision remain responsible for structural geometry.
Replace the ineffective legacy beside-house mosque placement for the district
path rather than layering duplicate generators.

A bounded runtime population component activates teachers, furniture, labels,
boards, and lights only when their supporting chunks are ready. Reuse pooling
patterns from village content, without coupling district lifetime to house
metadata. Unload content before its supporting collision disappears.

A reading panel presents content through the existing UI/input conventions.
Do not introduce a lesson scheduler or abstract activity framework for deferred
features. Keep future activity additions possible through ordinary interaction
points, not placeholder implementations.

Scene wiring must persist through the existing scene assembler. No manual
scene-YAML edits. Follow current assembly boundaries without circular references.
No new third-party dependencies are expected.

## Performance and error handling

- Generate structures deterministically without synchronous whole-district
  instantiation on every chunk refresh.
- Pool chunk views and district runtime content. No continual Instantiate/Destroy
  loop while traveling.
- Bound active lights, avoid extra shadow-casting point lights, and reuse meshes
  and materials. Verify all four complexes together, not just an isolated room.
- Preserve the browser target of at least 30 FPS during the district walkthrough.
  Record the browser/device and measured result; automated tests alone cannot
  establish this target.
- Missing content, font references, placement failures, or invalid configuration
  must produce explicit diagnostics rather than blank panels or missing landmarks.

## Validation and acceptance

Write failing focused tests before implementation, then verify:

1. Four distinct styles, deterministic placement, separate footprints, no
   out-of-bounds writes, and consistent seams across positive/negative chunk edges.
2. Every entrance, hall, learning room, and approach meets the clearance rules.
   Actual controller traversal succeeds in each complex.
3. Mining and placement are blocked at complex boundaries without inventory
   changes, while ordinary edits outside remain functional.
4. Existing saved edits are preserved; conflicting locations are rejected without
   partial structure writes.
5. Teacher and board targeting respects distance and walls. Reading panels show
   correct content and restore input, including interaction with the trading UI.
6. Streaming, same-coordinate chunk replacement, seed reset, and world reload
   do not leak objects, stale protection, or duplicate populations.
7. Saved scene references, menu fonts, lighting, and required shaders are valid.
8. Existing movement, terrain recovery, flight, animals, houses, trading, and
   hosting focused regressions continue to pass.
9. A fresh WebGL build loads through the existing versioned launcher. Browser
   walkthrough enters all four buildings, reads a teacher/board panel, confirms
   edit protection, checks indoor cameras/lighting, and measures performance.

Update the current phase status with verified results and scope limits. Do not
claim unrelated full-suite failures are resolved.

## Explicit exclusions

Scheduled gatherings; NPC attendance/pathfinding; playable lessons or quizzes;
lesson progress saving; guided Salah or prayer movements; wudu simulation;
worship scores/rewards/penalties; new Adhan audio; multiplayer; purchases;
functional storage, sleeping, or sitting; exact monument replicas.

## Architectural references

These references support regional context, not a claim that every proposed
decorative detail is documented by each page. Verify more specific details
against appropriate references before writing the corresponding board text.

- [UNESCO: At-Turaif District in ad-Dir'iyah](https://whc.unesco.org/en/list/1329/):
  Najdi architecture, adobe construction, geometric decoration.
- [UNESCO: Samarra Archaeological City](https://whc.unesco.org/en/list/276/):
  Abbasid architectural context, brick construction, mosque planning, distinctive
  minarets and decorative traditions.
- [UNESCO tentative-list entry: Badshahi Mosque, Lahore](https://whc.unesco.org/en/tentativelists/1277/):
  identification of the Pakistani reference site. This is a tentative-list entry,
  not a claim of World Heritage inscription.
- [UNESCO: Historic Areas of Istanbul](https://whc.unesco.org/en/list/356/):
  Ottoman mosque complexes, domed skylines, and slender minarets.

The locally reviewed visual preview is a conceptual district diagram, not a
render of completed gameplay.
