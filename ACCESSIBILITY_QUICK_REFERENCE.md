# ?? ACCESSIBILITY QUICK REFERENCE
## Developer Cheat Sheet - SMART NOC Commander

---

## ?? ADD ACCESSIBILITY TO NEW UI ELEMENTS

### Quick Template

```xaml
<!-- 1. Simple Button -->
<Button Content="Action"
        AutomationProperties.Name="Button description"
        AutomationProperties.HelpText="What this button does"/>

<!-- 2. TextBox with Label -->
<TextBlock Text="Label Name"
           AutomationProperties.Name="Label description"/>
<TextBox PlaceholderText="Enter..."
         AutomationProperties.Name="Input field name"
         AutomationProperties.HelpText="What to enter and why"/>

<!-- 3. ComboBox with Options -->
<ComboBox AutomationProperties.Name="Dropdown name"
          AutomationProperties.HelpText="What options do">
    <ComboBoxItem Content="Option 1"/>
    <ComboBoxItem Content="Option 2"/>
</ComboBox>

<!-- 4. Dynamic Content with Live Region -->
<TextBlock Text="{Binding Status}"
           AutomationProperties.Name="Status message"
           AutomationProperties.HelpText="Shows current status"
           AutomationProperties.LiveSetting="Polite"/>

<!-- 5. Complex Section with Heading -->
<StackPanel AutomationProperties.Name="Section name"
            AutomationProperties.HelpText="What this section contains">
    <TextBlock Text="Section Title"
               AutomationProperties.HeadingLevel="2"/>
    <!-- ... section content ... -->
</StackPanel>
```

---

## ?? AutomationProperties Reference

### Essential Attributes

| Attribute | Usage | Example |
|-----------|-------|---------|
| **Name** | Control identification | `"Submit button"` |
| **HelpText** | Detailed description | `"Click to submit form"` |
| **HeadingLevel** | Page structure (1-9) | `"1"` for main title |
| **LiveSetting** | Live region type | `"Polite"` or `"Assertive"` |

### LiveSetting Values

```xaml
<!-- Polite: Announce after current speech ends (default) -->
<TextBlock AutomationProperties.LiveSetting="Polite"
           Text="Update available..."/>

<!-- Assertive: Interrupt current speech (important alerts only) -->
<TextBlock AutomationProperties.LiveSetting="Assertive"
           Text="Critical error occurred!"/>

<!-- Off: Don't announce (default for non-changing content) -->
<TextBlock AutomationProperties.LiveSetting="Off"
           Text="Static text"/>
```

---

## ?? Code-Behind Accessibility

### Announce Updates to Screen Readers

```csharp
// In LiveMapPage.xaml.cs
private void AnnounceToScreenReader(string message)
{
    DebugLog($"[A11Y] ?? {message}");
}

// Usage Examples:
AnnounceToScreenReader("Map loaded successfully");
AnnounceToScreenReader("25 incidents now displayed");
AnnounceToScreenReader("Error: Invalid date format");
```

---

## ?? Keyboard Navigation Checklist

When adding new pages or controls:

- [ ] All interactive elements are focusable (Tab key)
- [ ] Focus order is logical (left?right, top?bottom)
- [ ] No focus traps (Tab key can always escape)
- [ ] Visual focus indicator is visible (blue ring)
- [ ] Buttons respond to Enter key
- [ ] Toggles respond to Space key
- [ ] Dropdowns respond to Arrow keys
- [ ] TextBoxes respond to typing
- [ ] Escape key closes dialogs/popups

---

## ?? Color & Contrast Checklist

When adding new colors or styling:

- [ ] Text contrast ? 4.5:1 (WCAG AA)
- [ ] Large text (18pt+) contrast ? 3:1
- [ ] Color not sole differentiator (use icon + text)
- [ ] Status colors: Red (#FF3B30), Green (#34C759)
- [ ] Icons supplement all colored elements
- [ ] High contrast mode support tested

**Tools:**
- WebAIM Contrast Checker: https://webaim.org/resources/contrastchecker/
- Built-in Windows High Contrast Mode

---

## ?? Common Screen Reader Scenarios

### Scenario 1: Button Click Feedback
```xaml
<Button Content="Save"
        Click="BtnSave_Click"
        AutomationProperties.Name="Save button"
        AutomationProperties.HelpText="Save changes and return to list"/>

<!-- In code-behind: -->
private void BtnSave_Click(object sender, RoutedEventArgs e)
{
    // ... save logic ...
    AnnounceToScreenReader("Changes saved successfully");
}
```

### Scenario 2: Form Validation Error
```xaml
<TextBlock x:Name="ErrorMessage"
           Text=""
           Foreground="Red"
           AutomationProperties.Name="Error message"
           AutomationProperties.LiveSetting="Assertive"
           Visibility="Collapsed"/>

<!-- In code-behind: -->
private void ValidateForm()
{
    if (string.IsNullOrEmpty(txtName.Text))
    {
        ErrorMessage.Text = "Name is required";
        ErrorMessage.Visibility = Visibility.Visible;
        AnnounceToScreenReader("Error: Name is required");
    }
}
```

### Scenario 3: Loading State
```xaml
<Grid x:Name="LoadingOverlay" Visibility="Collapsed"
      AutomationProperties.Name="Loading indicator"
      AutomationProperties.LiveSetting="Polite">
    <ProgressRing IsActive="True"/>
    <TextBlock x:Name="LoadingText" Text="Loading..."
               AutomationProperties.LiveSetting="Polite"/>
</Grid>

<!-- In code-behind: -->
private async void LoadData()
{
    LoadingOverlay.Visibility = Visibility.Visible;
    LoadingText.Text = "Retrieving incident data...";
    AnnounceToScreenReader("Loading. Please wait...");
    
    // ... load data ...
    
    LoadingOverlay.Visibility = Visibility.Collapsed;
    AnnounceToScreenReader("Data loaded");
}
```

---

## ?? Quick Testing Checklist

Before submitting code:

### Keyboard Testing (2 min)
```
? Launch application
? Press Tab 10+ times
? Verify focus ring visible
? Press Enter on button ? works?
? Press Space on toggle ? works?
? Try arrow keys in dropdown ? works?
```

### Screen Reader Testing (3 min)
```
? Windows Key + Ctrl + Enter (start Narrator)
? Tab through first 5 controls
? Listen for:
  - Control name announced? ?
  - Help text available? ?
  - Action clear? ?
? Windows Key + Ctrl + Enter (stop Narrator)
```

### Color Testing (2 min)
```
? Check text contrast with WebAIM tool
? Verify color ? 4.5:1 ratio
? Look for icon + color (not color-only)
? Test high contrast mode (Settings > Ease of Access)
```

---

## ?? Common AutomationProperties Patterns

### Pattern 1: Dialog/Modal with Live Region
```xaml
<ContentDialog x:Name="DeleteDialog"
               Title="Confirm Deletion"
               AutomationProperties.Name="Delete confirmation dialog"
               AutomationProperties.HelpText="Confirms if you want to permanently delete this item">
    <StackPanel Spacing="12">
        <TextBlock Text="Are you sure you want to delete?"
                   AutomationProperties.Name="Warning message"/>
        <Button Content="Yes, Delete"
                Click="BtnDelete_Click"
                AutomationProperties.Name="Confirm delete"
                AutomationProperties.HelpText="Click to permanently delete"/>
        <Button Content="Cancel"
                IsCancel="True"
                AutomationProperties.Name="Cancel deletion"
                AutomationProperties.HelpText="Close dialog without deleting"/>
    </StackPanel>
</ContentDialog>
```

### Pattern 2: DataGrid Row Item
```xaml
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Grid AutomationProperties.Name="{Binding Name}"
                  AutomationProperties.HelpText="Item: {Binding Name}, Status: {Binding Status}">
                <TextBlock Text="{Binding Name}"
                           AutomationProperties.Name="Item name"/>
                <TextBlock Text="{Binding Status}"
                           AutomationProperties.Name="Item status"/>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### Pattern 3: Tab Navigation
```xaml
<TabView x:Name="MainTabs"
         AutomationProperties.Name="Main content tabs"
         AutomationProperties.HelpText="Tabs to switch between different views">
    <TabViewItem Header="Overview"
                 AutomationProperties.Name="Overview tab"
                 AutomationProperties.HelpText="Shows summary information">
        <!-- Content -->
    </TabViewItem>
    <TabViewItem Header="Details"
                 AutomationProperties.Name="Details tab"
                 AutomationProperties.HelpText="Shows detailed information">
        <!-- Content -->
    </TabViewItem>
</TabView>
```

---

## ? Common Accessibility Mistakes to Avoid

### Mistake 1: Color-Only Status Indication
```xaml
<!-- ? BAD: Color only, no icon or text -->
<Border Background="#FF3B30" Width="20" Height="20"/>

<!-- ? GOOD: Color + icon + text -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <FontIcon Glyph="&#xE783;" Foreground="#FF3B30"/>
    <TextBlock Text="ERROR" Foreground="#FF3B30"/>
</StackPanel>
```

### Mistake 2: No Help Text on Complex Controls
```xaml
<!-- ? BAD: No help text -->
<AutoSuggestBox PlaceholderText="Search..."/>

<!-- ? GOOD: Helpful description -->
<AutoSuggestBox PlaceholderText="Search..."
                AutomationProperties.Name="Search incidents"
                AutomationProperties.HelpText="Search by Ticket ID or Segment name. Press Enter to search."/>
```

### Mistake 3: Forgetting Heading Levels
```xaml
<!-- ? BAD: Title without structure -->
<TextBlock Text="Dashboard" FontSize="24" FontWeight="Bold"/>

<!-- ? GOOD: Proper heading hierarchy -->
<TextBlock Text="Dashboard"
           FontSize="24" FontWeight="Bold"
           AutomationProperties.Name="Dashboard"
           AutomationProperties.HeadingLevel="1"/>
```

### Mistake 4: Hidden Content Not Announced
```xaml
<!-- ? BAD: Hidden text not accessible -->
<TextBlock Text="Loading..." Visibility="Collapsed"/>

<!-- ? GOOD: Still announces when shown -->
<TextBlock Text="Loading..."
           Visibility="Collapsed"
           AutomationProperties.Name="Loading status"
           AutomationProperties.LiveSetting="Polite"/>
```

---

## ?? Need Help?

### Documentation
- `ACCESSIBILITY_ENHANCEMENTS.md` - Complete reference
- `ACCESSIBILITY_TESTING_GUIDE.md` - Step-by-step testing
- `ACCESSIBILITY_IMPLEMENTATION_SUMMARY.md` - Overview

### Tools & Resources
- **Windows Narrator**: Windows Key + Ctrl + Enter
- **NVDA Screen Reader**: https://www.nvaccess.org/
- **WebAIM Contrast Checker**: https://webaim.org/resources/contrastchecker/
- **WCAG 2.1 Guidelines**: https://www.w3.org/WAI/WCAG21/quickref/

### Testing
Follow the checklist in `ACCESSIBILITY_TESTING_GUIDE.md`

---

**Last Updated**: 2024  
**Maintained By**: Development Team  
**Framework**: WinUI 3 (.NET 10)  
**Target Standard**: WCAG 2.1 Level AA
