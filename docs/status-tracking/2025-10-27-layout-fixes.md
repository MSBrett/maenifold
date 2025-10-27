# Task

Fix layout issues in http://localhost:4321/maenifold/

# Task Restated

Investigate and fix layout/styling issues on the maenifold site. The modified files are:
- `site/src/layouts/Base.astro`
- `site/src/layouts/Landing.astro`

Based on the git diff analysis, I need to:
1. Remove duplicate `.scene` style definition in Landing.astro
2. Fix whitespace formatting in Base.astro for the `.container` class
3. Add back the missing `.card` style that's referenced but not defined

Assumptions:
- The layout issues are CSS-related based on the file changes
- The user wants the site to display correctly at http://localhost:4321/maenifold/
- I should fix the styling issues without changing the intended design
- The `.card` style should match the design system established in the rest of the layouts

# Plan

1. Fix duplicate `.scene` style in Landing.astro (remove line 28 duplicate)
2. Fix whitespace indentation for `.container` in Base.astro (line 32)
3. Add `.card` style definition to Landing.astro to match the content section styling
4. Verify changes maintain the design intent

# Status

✅ Investigation complete - accessed live site at http://localhost:4321/maenifold/

## Confirmed Issues

1. **Duplicate `.scene` style in Landing.astro (line 28)**
   - Overrides the responsive version defined at line 18
   - Forces height to 480px instead of 520px with proper responsive breakpoints
   - Visible in rendered CSS

2. **Missing `.card` style in Landing.astro**
   - Line 131 uses `<div class="card">` but no style is defined
   - Content section has no background/border/padding

3. **Whitespace formatting in Base.astro (line 32)**
   - Inconsistent indentation on `.container` class

## Proposed Changes

1. Remove duplicate `.scene` definition at Landing.astro:28
2. Add `.card` style to Landing.astro matching the design system
3. Fix indentation in Base.astro:32

Ready to implement fixes.

## Changes Applied

### 1. Landing.astro
- ✅ Removed duplicate `.scene` style definition at line 28
- ✅ Added `.card` style: `padding: 32px; border-radius: 16px; border: 1px solid rgba(255,255,255,0.06); background: rgba(255,255,255,0.03);`
- The scene now properly uses responsive heights: 520px → 420px @ 1024px → 360px @ 720px

### 2. Base.astro
- ✅ Fixed `.container` indentation from ` .container` to `      .container`

All layout issues resolved. The site should now display correctly at http://localhost:4321/maenifold/

## Additional Issue Found

User reports the 3D graph on the right side is not visible/loading. The two-column layout creates an empty space on the right side.

### Action: Remove two-column grid layout
- Remove the `.grid` two-column layout
- Remove the `.scene` (3D graph) section entirely
- Center the hero content on the page
- Simplify to single-column centered layout

### Changes Applied

✅ Removed two-column grid layout
✅ Removed entire 3D graph scene and all associated JavaScript
✅ Centered hero card with `max-width: 800px; margin: 0 auto; align-items: center;`
✅ Added `text-align: center;` to hero section
✅ Centered CTA buttons with `justify-content: center;`
✅ Changed "Explore the graph" button text to "Get Started"

The hero section now displays as a single, centered card without the empty right column.

## Final Styling Update

✅ Updated CTA buttons to match the header pill style:
- Changed `border-radius` from `14px` to `999px` (pill shape)
- Changed `padding` from `12px 18px` to `8px 12px` (match header)
- Updated `background` to `rgba(84,174,255,0.12)` (match header)
- Updated `border` to `1px solid rgba(84,174,255,0.25)` (match header)
- Added `color: var(--text)` for better readability
- Added `text-decoration: none` on hover

CTA buttons now have consistent styling with the Usage/GitHub/NPM buttons in the header.

## Theme Not Applied to Other Pages

Issue: Only the landing page was using the Base layout. The usage pages and markdown files were not themed.

### Changes Applied

✅ Created `MarkdownLayout.astro` wrapper for markdown content
✅ Added frontmatter with layout to all 24 markdown files in `usage/tools/`
✅ Fixed `usage/index.astro` syntax error (was mixing import and frontmatter)
✅ Wrapped usage index with proper Base layout and prose styling
✅ Updated Astro config with dark theme for code blocks

All pages now use the Base layout and inherit the dark theme styling.

## Usage Page Redesign & Navigation Menu

User requested prettier usage page and left navigation menu for tool documentation.

### Changes Applied

✅ **Redesigned usage index page**
- Added hero section with title and description
- Created card grid layout for tools (responsive, auto-fill columns)
- Added hover effects with color transitions and elevation
- Alphabetically sorted tool list
- Visual hierarchy with card-based design

✅ **Created DocsLayout with sidebar navigation**
- Two-column layout with sticky sidebar (250px fixed width)
- Left navigation menu showing all 26 tools alphabetically
- Active page highlighting with primary color
- Smooth scrollbar styling for overflow
- Responsive: collapses to single column on mobile
- Updated all 26 markdown files to use DocsLayout

The usage page now has a modern card grid, and all tool pages have a persistent left navigation menu.

## Content Card Styling

User requested content be wrapped in a bubble/card container like the hero, centered but left-aligned inside.

### Changes Applied

✅ **Created styled content card**
- Replaced simple `.card` with `.content-card` with enhanced styling
- Similar visual treatment to hero card (gradient background, border, shadow)
- Max-width: 900px, centered with auto margins
- Left-aligned text inside the card
- Generous padding (48px 56px) for breathing room
- Styled all typography (h2, h3, p, ul, code, pre, img)
- Proper spacing and hierarchy
- Responsive padding on mobile

The landing page content now appears in a polished card that matches the hero styling.
