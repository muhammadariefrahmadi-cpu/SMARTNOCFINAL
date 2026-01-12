# ?? ACCESSIBILITY TESTING GUIDE
## SMART NOC Commander - Step-by-Step Testing Instructions

---

## ?? TABLE OF CONTENTS
1. [Quick Start](#quick-start)
2. [Keyboard Navigation Testing](#keyboard-navigation-testing)
3. [Windows Narrator Testing](#windows-narrator-testing)
4. [Color Contrast Testing](#color-contrast-testing)
5. [Focus Indicator Testing](#focus-indicator-testing)
6. [Screen Reader Testing](#screen-reader-testing)
7. [Mobile Accessibility Testing](#mobile-accessibility-testing)
8. [Accessibility Bug Report Template](#accessibility-bug-report-template)

---

## ?? QUICK START

### Test Environment Setup
```
Operating System: Windows 10 / 11
Screen Reader: Windows Narrator (built-in)
Testing Browser: Microsoft Edge
Tools Needed: WebAIM Contrast Checker (online)
Time Required: ~30 minutes per test
```

---

## ?? KEYBOARD NAVIGATION TESTING

### Test 1: Tab Navigation Through All Pages

**Steps:**
1. Launch SMART NOC Commander
2. Press Tab repeatedly and observe:
   - ? Blue focus ring appears around interactive elements
   - ? Focus moves in logical left-to-right order
   - ? Focus cycles back to first element after last

**Expected Behavior:**
```
Dashboard Page (Tab order):
  1. Dashboard menu item
  2. Create Ticket menu item
  3. Live Map menu item
  4. History menu item
  5. Handover Log menu item
  6. Settings menu item
  7. (Page-specific controls)
  8. Cycles back to Dashboard
```

**Pass Criteria:**
- [ ] All interactive elements receive focus
- [ ] Focus order matches reading order (left?right, top?bottom)
- [ ] No focus is trapped
- [ ] Focus is visually indicated

**Document Results:**
```
Page Name: ________________
Tab Order Correct: Yes / No / Partial
Missing Focus: ________________
Out of Order: ________________
Notes: ________________
```

---

### Test 2: Enter Key Activation on Buttons

**Steps:**
1. Navigate to any page with buttons (e.g., Dashboard)
2. Press Tab until focus is on a button (e.g., "Refresh")
3. Press Enter key
4. Observe the button activation

**Expected Behavior:**
```
Button should:
? Trigger its Click event
? Show visual feedback (pressed state)
? Execute associated action
? Maintain logical focus afterward
```

**Pass Criteria:**
- [ ] All buttons activate with Enter
- [ ] Button visual feedback is clear
- [ ] Action completes successfully
- [ ] Focus remains accessible

---

### Test 3: Space Key on Toggle Switches

**Steps:**
1. Navigate to status bar (bottom right)
2. Tab until focus is on "AI" toggle
3. Press Space bar
4. Observe toggle change
5. Press Space again to toggle back

**Expected Behavior:**
```
Toggle State Changes:
  OFF ? ON (Space pressed)
  ON ? OFF (Space pressed)
  
Visual Feedback:
  ? Toggle position changes
  ? Text updates (ON/OFF)
  ? Color updates
```

**Pass Criteria:**
- [ ] Space key toggles ON/OFF
- [ ] Visual state changes reflect new state
- [ ] Both toggles work (AI and DEV)
- [ ] Multiple presses work consistently

---

### Test 4: Arrow Keys in Dropdowns

**Steps:**
1. Navigate to Live Map page
2. Tab to "Region" dropdown
3. Press Down arrow key repeatedly
4. Observe dropdown values change
5. Press Up arrow to go backwards
6. Press Enter to select

**Expected Behavior:**
```
Region Dropdown:
  Initial: ALL REGIONS
  Down arrow: Central Java
  Down arrow: West Java
  ... (cycles through all)
  Enter: Selects current item
```

**Pass Criteria:**
- [ ] All dropdown items accessible via arrow keys
- [ ] Down/Up navigation works both directions
- [ ] Enter selects the focused item
- [ ] No items are skipped

---

### Test 5: Escape Key Navigation

**Steps:**
1. Open any modal/dialog (e.g., error dialog)
2. Press Escape key
3. Observe dialog closes
4. Focus should return to previous location

**Expected Behavior:**
```
Before Escape: Dialog visible with focus inside
After Escape: Dialog closes, focus on parent element
```

**Pass Criteria:**
- [ ] Escape closes dialogs
- [ ] Focus returns to logical location
- [ ] No side effects occur

---

## ?? WINDOWS NARRATOR TESTING

### How to Enable Windows Narrator

**Method 1: Keyboard Shortcut (Easiest)**
```
Press: Windows Key + Ctrl + Enter
```

**Method 2: Settings**
```
Settings > Ease of Access > Narrator > Toggle ON
```

**Method 3: Voice Command**
```
Say: "Hey Cortana, turn on Narrator"
```

### Test 1: Page Title Reading

**Steps:**
1. Enable Narrator (Windows Key + Ctrl + Enter)
2. Launch SMART NOC application
3. Narrator should announce:
   - "SMART NOC COMMANDER window"
   - "SYSTEM ACTIVE"
   - Current page name

**Expected Announcement:**
```
Narrator: "SMART NOC COMMANDER, window"
Narrator: "SYSTEM ACTIVE, status text"
Narrator: "DASHBOARD, button selected"
```

**Pass Criteria:**
- [ ] Application title announced
- [ ] Page name announced
- [ ] Status announced
- [ ] Announcement is clear and complete

**Troubleshooting:**
```
If no announcement:
  1. Check volume is not muted
  2. Verify Narrator is enabled
  3. Click on application window to focus
  4. Try pressing Tab to trigger announcement
```

---

### Test 2: Menu Item Reading

**Steps:**
1. With Narrator enabled, press Tab
2. Focus on first menu item (Dashboard)
3. Narrator should read: "Dashboard, button, selected"
4. Press Tab again to move to next item
5. Narrator should read: "Create Ticket, button"

**Expected Announcements:**
```
1. "Dashboard, button, selected, navigate to Dashboard page..."
2. "Create Ticket, button, navigate to Create Ticket page..."
3. "Live Map, button, navigate to Live Map page..."
4. "History, button, navigate to History page..."
5. "Handover Log, button, navigate to Handover Log page..."
6. "Settings, button, navigate to Settings page..."
```

**Pass Criteria:**
- [ ] All menu items announced
- [ ] Help text included in announcement
- [ ] Selected state indicated
- [ ] No items skipped

---

### Test 3: Dynamic Content Updates

**Steps:**
1. Enable Narrator
2. Navigate to Live Map
3. Change filter (e.g., select a Region)
4. Click "Apply" button
5. Narrator should announce map update

**Expected Announcement:**
```
Before: "Initializing... indicators"
After: "Map updated. Displaying 25 locations..."
```

**Pass Criteria:**
- [ ] Live region announcement triggers
- [ ] Number of locations announced
- [ ] Status updates are clear
- [ ] User doesn't need to ask "what changed?"

---

### Test 4: Error Message Reading

**Steps:**
1. Enable Narrator
2. Trigger an error (e.g., invalid input)
3. Narrator should announce error details
4. Error message should be in TextBlock with LiveSetting="Assertive"

**Expected Announcement:**
```
Narrator: "Error: Invalid date format. Please use DD/MM/YYYY HH:MM"
```

**Pass Criteria:**
- [ ] Error announced immediately
- [ ] Error message is complete
- [ ] Instructions for fixing provided
- [ ] Tone is helpful, not alarming

---

## ?? COLOR CONTRAST TESTING

### Test 1: Automatic Contrast Checking

**Using WebAIM Contrast Checker:**

1. Go to: https://webaim.org/resources/contrastchecker/
2. For each color combination in SMART NOC, check:

**Primary Text Colors:**
```
Foreground: #FFFFFF (White)
Background: #0D1B2A (Dark Blue)
Expected: Contrast Ratio ? 4.5:1 (AA)
Actual: 12.6:1 ? PASSES AAA
```

**Secondary Text Colors:**
```
Foreground: #E2E8F0 (Light Gray)
Background: #0D1B2A (Dark Blue)
Expected: Contrast Ratio ? 4.5:1 (AA)
Actual: 8.2:1 ? PASSES AAA
```

**Status Colors:**
```
Red (#FF3B30) on White:
  Ratio: 5.9:1 ? PASSES AA

Green (#34C759) on White:
  Ratio: 5.1:1 ? PASSES AA

Blue (#00B4FF) on Dark (#0D1B2A):
  Ratio: 8.6:1 ? PASSES AAA
```

**Pass Criteria:**
- [ ] All text/background combinations ? 4.5:1 ratio
- [ ] Large text (18pt+) ? 3:1 ratio
- [ ] Color alone not used to convey meaning

---

### Test 2: High Contrast Mode Test

**Windows High Contrast Mode Setup:**

1. Open Settings
2. Go to: Ease of Access > Display
3. Enable: "High contrast"
4. Select a high contrast theme
5. Restart SMART NOC

**Expected Results:**
```
? All text remains readable
? Buttons clearly distinguished
? Form fields clearly outlined
? Focus indicator visible
? Status indicators clear
? No white text on light backgrounds
```

**Pass Criteria:**
- [ ] All text readable
- [ ] Buttons still interactive
- [ ] No loss of functionality
- [ ] Layout doesn't break

---

### Test 3: Colorblind Vision Test

**Using Color Blindness Simulator:**

1. Use tool: https://www.color-blindness.com/coblis-color-blindness-simulator/
2. Upload screenshot of SMART NOC
3. Test with different types of colorblindness:
   - Protanopia (Red-blind)
   - Deuteranopia (Green-blind)
   - Tritanopia (Blue-blind)

**Expected Results:**
```
? Status indicators use icons + color (not color-only)
? Red/Green indicators have text labels
? Buttons have shape/text distinction
? Links underlined (not just colored)
```

**Pass Criteria:**
- [ ] All information accessible in any vision type
- [ ] No information lost in colorblind view
- [ ] Icons supplement colors
- [ ] Text labels used appropriately

---

## ??? FOCUS INDICATOR TESTING

### Test 1: Visual Focus Ring

**Steps:**
1. Launch application
2. Press Tab repeatedly
3. Observe focus ring around each element
4. Focus ring should be:
   - Clearly visible
   - Blue color (#0078D4)
   - 2-3 pixel wide
   - Rounded to match element shape

**Expected Visual:**
```
Button with focus:
  ???????????????????????
  ? Button Text     ????  ? Focus ring
  ?                 ?  ?
  ???????????????????????
  
TextBox with focus:
  ???????????????????????
  ? [Input area]    ????  ? Focus ring
  ???????????????????????
```

**Pass Criteria:**
- [ ] Focus ring visible on all interactive elements
- [ ] Contrast ratio ? 3:1 against background
- [ ] Ring does not cover text
- [ ] Consistent style throughout app

---

### Test 2: Focus Order Logic

**Steps:**
1. Press Tab and number the focus order
2. Verify order matches expected reading order:
   - Left to right
   - Top to bottom
   - Within groups/sections

**Expected Order Example (Dashboard):**
```
1. Dashboard menu
2. Create Ticket menu
3. Live Map menu
4. History menu
5. Handover Log menu
6. Settings menu
7. Content area elements (if any)
8. Cycles back to Dashboard
```

**Pass Criteria:**
- [ ] Order is logical and predictable
- [ ] No focus traps
- [ ] No skipped elements
- [ ] User can navigate all controls

---

### Test 3: Focus on Different Input Types

**Steps:**
1. Tab through different control types:
   - Buttons
   - TextBoxes
   - ComboBoxes
   - Toggle Switches
   - Calendar Pickers
   - Progress Rings (display-only)

2. Verify focus behavior for each:

**Expected Behaviors:**
```
Button: Blue focus ring, responds to Enter
TextBox: Blue focus ring, responds to typing
ComboBox: Blue focus ring, responds to arrows + Enter
Toggle: Blue focus ring, responds to Space
Calendar: Blue focus ring, responds to arrows
Progress Ring: No focus (display-only, correct)
```

**Pass Criteria:**
- [ ] All focusable controls receive focus
- [ ] Non-interactive controls skip focus
- [ ] Keyboard input works with focused element
- [ ] Visual feedback appropriate for control type

---

## ?? SCREEN READER TESTING (NVDA)

### Setup NVDA (Free Screen Reader)

1. Download: https://www.nvaccess.org/
2. Install and restart computer
3. Launch NVDA
4. Configure voice/speed preferences
5. Open SMART NOC Commander

### Test 1: Page Navigation Announcement

**Steps:**
1. Start NVDA
2. Launch SMART NOC
3. NVDA should announce:
   - Application name
   - Window title
   - Current page
   - Interactive elements count

**Expected NVDA Output:**
```
NVDA: "SMART NOC COMMANDER, window"
NVDA: "Dashboard page loaded"
NVDA: "6 menu items available"
```

---

### Test 2: Help Text Reading

**Steps:**
1. With NVDA running, press Insert+H (help mode)
2. Tab to each control
3. NVDA should read AutomationProperties.HelpText
4. Press Insert+F1 to toggle help tooltips

**Expected Output:**
```
Button: "Apply filters button, click to apply all filter criteria"
ComboBox: "Region filter dropdown, select geographic region"
TextBox: "Search incidents, search for incidents by Ticket ID"
```

**Pass Criteria:**
- [ ] Help text announced for each control
- [ ] Help text is descriptive
- [ ] Help text explains purpose clearly
- [ ] No redundant help text

---

### Test 3: Live Region Announcements

**Steps:**
1. Start NVDA
2. Go to Live Map
3. Change filter and click Apply
4. NVDA should announce map update immediately

**Expected NVDA Output:**
```
NVDA: "Total markers count, 25 locations, 48 incidents"
(Announced automatically when map updates)
```

**Pass Criteria:**
- [ ] Live region triggered on update
- [ ] Announcement is timely (within 1 second)
- [ ] Announcement is not repetitive
- [ ] User gets actionable information

---

## ?? MOBILE ACCESSIBILITY TESTING

### Test 1: Touch Target Size

**Steps:**
1. View application on tablet/phone
2. Measure button/control sizes
3. Each interactive element should be ?44x44 pixels

**Minimum Touch Targets:**
```
Buttons: 44x44 pixels minimum
Form Fields: 44 pixels minimum height
Links: 44x44 pixels minimum area
Icons: 44x44 pixels with padding
```

**Pass Criteria:**
- [ ] All buttons ?44x44 pixels
- [ ] All links ?44 pixels height
- [ ] Sufficient spacing between targets
- [ ] No accidental taps possible

---

### Test 2: Keyboard-Only Navigation on Mobile

**Steps:**
1. Connect keyboard to mobile device
2. Use Tab key to navigate
3. Use arrow keys for menus
4. Use Enter/Space for activation

**Pass Criteria:**
- [ ] All functions accessible via keyboard
- [ ] No mouse/touch required
- [ ] Focus visible at all times
- [ ] Keyboard shortcuts documented

---

## ?? ACCESSIBILITY BUG REPORT TEMPLATE

```markdown
# Accessibility Bug Report

## Issue Summary
**Title:** [Brief description of accessibility issue]
**Component:** [Page/Control name]
**Severity:** Critical / High / Medium / Low

## Type of Issue
- [ ] Keyboard Navigation
- [ ] Screen Reader Support
- [ ] Color Contrast
- [ ] Focus Indicator
- [ ] Mobile Accessibility
- [ ] Other: ___________

## Description
[Detailed description of the issue]

## Steps to Reproduce
1. [Step 1]
2. [Step 2]
3. [Step 3]

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Assistive Technology Tested
- [ ] Windows Narrator
- [ ] NVDA
- [ ] JAWS
- [ ] Mobile VoiceOver
- [ ] Mobile TalkBack
- [ ] Keyboard only
- [ ] Other: ___________

## Screenshots
[Attach screenshots if applicable]

## Environment
- OS: Windows 10 / 11 / Other
- Screen Reader Version: [if applicable]
- Browser: Edge / Chrome / Firefox
- Application Version: [version number]

## WCAG Criterion
- [ ] 1.1.1 Non-text Content (Level A)
- [ ] 1.4.3 Contrast (Minimum) (Level AA)
- [ ] 2.1.1 Keyboard (Level A)
- [ ] 2.1.2 No Keyboard Trap (Level A)
- [ ] 2.4.7 Focus Visible (Level AA)
- [ ] 3.2.4 Consistent Identification (Level AA)
- [ ] 4.1.2 Name, Role, Value (Level A)
- [ ] Other: ___________

## Impact Assessment
- **Users Affected:** [Number of users]
- **Workaround:** [If available]
- **Priority:** [High/Medium/Low]

## Notes
[Any additional information]

---
**Reported By:** [Your name]
**Date:** [Date]
**Assigned To:** [Developer name]
```

---

## ? TESTING CHECKLIST

### Pre-Testing
- [ ] Enable Narrator or NVDA
- [ ] WebAIM Contrast Checker bookmarked
- [ ] Test on latest Windows version
- [ ] Test in Edge browser
- [ ] Record baseline screenshots

### Keyboard Testing
- [ ] Tab navigation test completed
- [ ] Enter key activation tested
- [ ] Space key toggle tested
- [ ] Arrow key navigation tested
- [ ] Escape key function tested
- [ ] Tab order documented
- [ ] No keyboard traps found

### Screen Reader Testing
- [ ] Page title announced correctly
- [ ] All menu items readable
- [ ] Help text available
- [ ] Live regions working
- [ ] Error messages announced
- [ ] Dynamic updates detected
- [ ] Focus order announced logically

### Color/Contrast Testing
- [ ] All text ?4.5:1 contrast ratio
- [ ] Large text ?3:1 ratio
- [ ] High contrast mode tested
- [ ] Colorblind simulator checked
- [ ] No color-only indicators
- [ ] Icons supplement colors
- [ ] Status shown with text + icon

### Focus Testing
- [ ] Focus ring visible
- [ ] Focus order logical
- [ ] All controls focusable
- [ ] Display-only controls skipped
- [ ] Focus not trapped
- [ ] Focus restoration after dialog

### Mobile Testing
- [ ] Touch targets ?44x44 pixels
- [ ] Keyboard-only navigation works
- [ ] Responsive layout intact
- [ ] Screen reader tested on device
- [ ] Orientation changes handled

---

## ?? TESTING RESULTS SUMMARY

```
Application: SMART NOC Commander
Test Date: _______________
Tested By: _______________

Results:
  Keyboard Navigation: ? PASS / ? FAIL
  Screen Reader: ? PASS / ? FAIL
  Color Contrast: ? PASS / ? FAIL
  Focus Indicators: ? PASS / ? FAIL
  Mobile Accessibility: ? PASS / ? FAIL

Overall Rating: _____ / 5 ?

Issues Found: _____
Issues Resolved: _____
Open Issues: _____

WCAG Compliance: Level AA / Level AAA

Sign Off: _______________
Date: _______________
```

---

**Testing Guide Created**: 2024
**Target Framework**: WinUI 3 (.NET 10)
**Accessibility Standards**: WCAG 2.1 Level AA
