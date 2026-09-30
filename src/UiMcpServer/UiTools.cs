using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Automation;
using ModelContextProtocol.Server;

namespace UiMcpServer;

/// <summary>
/// Windows UI Automation exposed as MCP tools. Every tool addresses a top-level window by a
/// title fragment and an element inside it by AutomationId first, Name second - so buttons
/// of system dialogs (which have no AutomationId) stay reachable by their caption.
/// </summary>
[McpServerToolType]
public sealed class UiTools
{
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    [McpServerTool(Name = "ui_list_windows")]
    [Description("Lists all visible top-level windows: title, process id, automation id.")]
    public static string ListWindows()
    {
        var windows = new List<object>();
        foreach (AutomationElement window in AutomationElement.RootElement
                     .FindAll(TreeScope.Children, Condition.TrueCondition))
        {
            var info = window.Current;
            if (info.IsOffscreen || string.IsNullOrEmpty(info.Name))
            {
                continue;
            }
            windows.Add(new { title = info.Name, processId = info.ProcessId, automationId = info.AutomationId });
        }
        return JsonSerializer.Serialize(windows, _json);
    }

    [McpServerTool(Name = "ui_find_elements")]
    [Description("Walks a window's UI tree and returns every element that has an AutomationId or a Name: control type, automation id, name, value. Use this first to learn the ids the other tools need.")]
    public static string FindElements(
        [Description("Title fragment of the window, case-insensitive")] string windowTitle)
    {
        var window = FindWindow(windowTitle);
        var elements = new List<object>();
        Walk(window, 0, elements);
        return JsonSerializer.Serialize(elements, _json);
    }

    [McpServerTool(Name = "ui_click")]
    [Description("Clicks an element: Invoke pattern first, then SelectionItem, Toggle, and finally a real mouse click on the element's clickable point.")]
    public static string Click(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("AutomationId or Name of the element")] string target)
    {
        var element = FindTarget(FindWindow(windowTitle), target);
        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invoke))
        {
            ((InvokePattern)invoke).Invoke();
            return Ok($"invoked '{target}'");
        }
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var select))
        {
            ((SelectionItemPattern)select).Select();
            return Ok($"selected '{target}'");
        }
        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle))
        {
            ((TogglePattern)toggle).Toggle();
            return Ok($"toggled '{target}'");
        }
        var point = element.GetClickablePoint();
        NativeMethods.ClickAt((int)point.X, (int)point.Y);
        return Ok($"mouse-clicked '{target}' at {(int)point.X},{(int)point.Y}");
    }

    [McpServerTool(Name = "ui_set_text")]
    [Description("Replaces the text of a text box via the Value pattern.")]
    public static string SetText(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("AutomationId or Name of the element")] string target,
        [Description("The new text")] string text)
    {
        var element = FindTarget(FindWindow(windowTitle), target);
        if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out var value))
        {
            throw new InvalidOperationException($"'{target}' does not support the Value pattern.");
        }
        ((ValuePattern)value).SetValue(text);
        return Ok($"set '{target}' to \"{text}\"");
    }

    [McpServerTool(Name = "ui_get_text")]
    [Description("Reads an element's text: Value pattern, then Text pattern, then the Name property.")]
    public static string GetText(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("AutomationId or Name of the element")] string target)
    {
        var element = FindTarget(FindWindow(windowTitle), target);
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var value))
        {
            return ((ValuePattern)value).Current.Value;
        }
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var text))
        {
            return ((TextPattern)text).DocumentRange.GetText(-1);
        }
        return element.Current.Name;
    }

    [McpServerTool(Name = "ui_select_row")]
    [Description("Selects the first row of a list or grid whose cells contain the given text.")]
    public static string SelectRow(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("AutomationId or Name of the list/grid")] string target,
        [Description("Text that identifies the row")] string rowText)
    {
        var container = FindTarget(FindWindow(windowTitle), target);
        foreach (AutomationElement row in container.FindAll(TreeScope.Descendants,
                     new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)))
        {
            if (!RowContainsText(row, rowText) ||
                !row.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var select))
            {
                continue;
            }
            ((SelectionItemPattern)select).Select();
            return Ok($"selected row containing \"{rowText}\"");
        }
        throw new InvalidOperationException($"No row containing \"{rowText}\" found in '{target}'.");
    }

    [McpServerTool(Name = "ui_wait_for")]
    [Description("Waits until an element exists in the window, polling every 250 ms.")]
    public static string WaitFor(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("AutomationId or Name of the element")] string target,
        [Description("Timeout in milliseconds, default 5000")] int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            try
            {
                FindTarget(FindWindow(windowTitle), target);
                return Ok($"'{target}' exists");
            }
            catch (InvalidOperationException)
            {
                Thread.Sleep(250);
            }
        }
        throw new TimeoutException($"'{target}' did not appear within {timeoutMs} ms.");
    }

    [McpServerTool(Name = "ui_screenshot")]
    [Description("Saves a PNG screenshot of the window and returns the file path.")]
    public static string Screenshot(
        [Description("Title fragment of the window")] string windowTitle,
        [Description("Path of the PNG to write; defaults to a temp file")] string? filePath = null)
    {
        var window = FindWindow(windowTitle);
        var handle = new IntPtr(window.Current.NativeWindowHandle);
        NativeMethods.GetWindowRect(handle, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var deviceContext = graphics.GetHdc();
            NativeMethods.PrintWindow(handle, deviceContext, 2); // 2 = PW_RENDERFULLCONTENT
            graphics.ReleaseHdc(deviceContext);
        }
        var path = filePath ?? Path.Combine(Path.GetTempPath(), $"ui-{Environment.TickCount64}.png");
        bitmap.Save(path, ImageFormat.Png);
        return Ok($"saved {path} ({width}x{height})");
    }

    private static AutomationElement FindWindow(string windowTitle)
    {
        foreach (AutomationElement window in AutomationElement.RootElement
                     .FindAll(TreeScope.Children, Condition.TrueCondition))
        {
            if (window.Current.Name.Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
            {
                return window;
            }
        }
        throw new InvalidOperationException($"No window with \"{windowTitle}\" in its title.");
    }

    private static AutomationElement FindTarget(AutomationElement window, string target)
    {
        var byId = window.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, target));
        if (byId is not null)
        {
            return byId;
        }
        var byName = window.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, target));
        if (byName is not null)
        {
            return byName;
        }
        throw new InvalidOperationException($"No element with AutomationId or Name '{target}'.");
    }

    private static void Walk(AutomationElement element, int depth, List<object> results)
    {
        if (depth > 8 || results.Count >= 300)
        {
            return;
        }
        var child = TreeWalker.ControlViewWalker.GetFirstChild(element);
        while (child is not null)
        {
            var info = child.Current;
            if (!string.IsNullOrEmpty(info.AutomationId) || !string.IsNullOrEmpty(info.Name))
            {
                results.Add(new
                {
                    controlType = info.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty),
                    automationId = info.AutomationId,
                    name = info.Name,
                    value = child.TryGetCurrentPattern(ValuePattern.Pattern, out var value)
                        ? ((ValuePattern)value).Current.Value
                        : null,
                });
            }
            Walk(child, depth + 1, results);
            child = TreeWalker.ControlViewWalker.GetNextSibling(child);
        }
    }

    private static bool RowContainsText(AutomationElement row, string rowText)
    {
        foreach (AutomationElement cell in row.FindAll(TreeScope.Descendants, Condition.TrueCondition))
        {
            var cellText = cell.TryGetCurrentPattern(ValuePattern.Pattern, out var value)
                ? ((ValuePattern)value).Current.Value
                : cell.Current.Name;
            if (cellText?.Contains(rowText, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }
        return false;
    }

    private static string Ok(string message)
    {
        return JsonSerializer.Serialize(new { ok = true, message });
    }
}
