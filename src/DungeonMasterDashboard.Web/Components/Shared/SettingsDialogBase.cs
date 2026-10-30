using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Web.Components.Shared;

// What the game system and world editors have in common: tracking unsaved changes, asking
// before discarding them, Escape handling, and running API calls with an error message.
public abstract class SettingsDialogBase : ComponentBase
{
    private string savedSnapshot = "";

    [Inject]
    protected IJSRuntime JS { get; set; } = default!;

    // Themes that can be copied as a starting point.
    [Parameter]
    public IReadOnlyList<ThemePreset> Presets { get; set; } = [];

    // Closes the dialog.
    [Parameter]
    public EventCallback OnClose { get; set; }

    // Raised after something was saved, reset or deleted, so the page can refresh.
    [Parameter]
    public EventCallback OnChanged { get; set; }

    protected TileImageEditor? ImageEditor { get; set; }

    protected TileImageChange ImageChange { get; set; } = TileImageChange.None;

    protected bool Cropping { get; set; }

    protected bool Busy { get; private set; }

    protected string? Error { get; private set; }

    protected bool IsDirty => Snapshot() != savedSnapshot || ImageChange != TileImageChange.None;

    // A comparable summary of the form, used to detect unsaved changes.
    protected abstract string Snapshot();

    // Makes the current form state count as unchanged.
    protected void MarkSaved()
    {
        savedSnapshot = Snapshot();
        ImageChange = TileImageChange.None;
    }

    // Runs API calls, showing "Could not {action}: ..." if one fails. Returns whether it worked.
    protected async Task<bool> RunAsync(string action, Func<Task> work)
    {
        Busy = true;
        Error = null;
        try
        {
            await work();
            return true;
        }
        catch (HttpRequestException ex)
        {
            Error = $"Could not {action}: {ex.Message}";
            return false;
        }
        finally
        {
            Busy = false;
        }
    }

    // After a successful save or delete: refresh the page and close.
    protected async Task SavedAndCloseAsync()
    {
        MarkSaved();
        await OnChanged.InvokeAsync();
        await OnClose.InvokeAsync();
    }

    // Re-renders the preview after a theme change.
    protected void Refresh()
    {
    }

    // Escape cancels cropping first, then closes the dialog.
    protected async Task OnEscapeAsync()
    {
        if (ImageEditor?.IsCropping == true)
        {
            await ImageEditor.CancelCropAsync();
        }
        else
        {
            await RequestCloseAsync();
        }
    }

    protected async Task RequestCloseAsync()
    {
        if (IsDirty && !await ConfirmDiscardAsync())
        {
            return;
        }

        // Don't ask again on the way out.
        MarkSaved();
        await OnClose.InvokeAsync();
    }

    protected async Task ConfirmNavigationAsync(LocationChangingContext context)
    {
        if (IsDirty && !await ConfirmDiscardAsync())
        {
            context.PreventNavigation();
        }
    }

    private ValueTask<bool> ConfirmDiscardAsync() => JS.InvokeAsync<bool>("confirm", "Discard your unsaved changes?");
}
